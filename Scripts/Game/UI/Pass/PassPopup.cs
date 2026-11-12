using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 패스 화면(슬라이드 메뉴의 패스 버튼). 기력을 쓴 만큼 패스 레벨이 오르고, 도달한 칸의 보상을 받는다.
///
/// 구성
///   탭     : 사이클(50레벨)마다 하나. 패스 1 = 시작 ~ 50, 패스 2 = 51 ~ 100 ... 한 사이클을 다 채우면 다음 탭이 열린다.
///            지난 탭도 남아 있어 언제든 다시 볼 수 있고, 탭마다 받을 칸이 남았는지 배지로 알린다.
///   트랙   : 고른 탭의 칸(PassRewardNode)을 가로로 늘어놓고 그 뒤로 게이지가 차오른다(멸망전 보상 트랙과 같은 모양).
///            칸마다 무료 줄과 패스 줄 두 줄이 있다.
///   논패스 : 평생 패스가 없을 때만 켜진다. 지금 사면 바로 더 받을 수 있는 보상을 합쳐 보여 주고 구매 버튼을 둔다.
///
/// 칸을 눌러 받는 것이 아니라 수령 버튼 하나로 모든 탭의 도달한 칸을 두 줄 모두 받는다.
/// 현황은 PassService 가 들고 있으며, 서버 푸시(새 레벨 도달, 평생 패스 구매)로도 바뀌므로 화면은 그 값을 구독한다.
/// </summary>
public sealed class PassPopup : UIView, IHudPopup {
    [Header("Status")]
    [Tooltip("현재 패스 레벨. \"패스 레벨: n\" 형식(n 은 강조색).")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("소모한 기력 진행. \"소모한 기력: 쓴 기력 / 다음 레벨 기력\" 형식(쓴 기력은 강조색).")]
    [SerializeField] private TMP_Text staminaText;
    [Tooltip("레벨/소모 기력 숫자의 강조색.")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [Tooltip("보상 트랙 진행 게이지. 칸의 실제 위치에 맞춰 채운다(0~1 정규화).")]
    [SerializeField] private Slider trackSlider;

    [Header("Tabs")]
    [Tooltip("사이클 탭이 생성될 부모. 정렬(역순 등)은 레이아웃에서 정한다.")]
    [SerializeField] private Transform tabParent;
    [Tooltip("사이클 탭 프리팹. 안에 BadgeView(Source=Manual)를 두면 그 탭에 받을 칸이 있을 때 켜진다.")]
    [SerializeField] private UITabButton tabPrefab;
    [Tooltip("탭 이름 형식. {0} 에 사이클 번호(1부터)가 들어간다.")]
    [SerializeField] private string tabLabelFormat = "패스 {0}";

    [Header("Reward Track")]
    [Tooltip("칸이 생성될 부모(Horizontal Layout Group). 가로 스크롤뷰의 Content 로 둔다.")]
    [SerializeField] private Transform trackContent;
    [SerializeField] private PassRewardNode nodePrefab;
    [Tooltip("트랙 스크롤뷰(선택). 탭을 열 때 받을 칸 또는 지금 진행한 칸으로 자동 스크롤한다.")]
    [SerializeField] private ScrollRect trackScroll;

    [Header("Non Pass")]
    [Tooltip("평생 패스가 없을 때만 켜는 그룹.")]
    [SerializeField] private GameObject nonPassGroup;
    [Tooltip("평생 패스를 사면 지금 바로 더 받을 수 있는 보상 슬롯이 생성될 부모.")]
    [SerializeField] private Transform nonPassRewardParent;
    [SerializeField] private RewardSlotView nonPassRewardPrefab;
    [Tooltip("평생 패스 구매 버튼(선택). 상점의 평생 패스 자리로 이동한다. 평생 패스를 가진 계정에는 보이지 않는다.")]
    [SerializeField] private Button buyPassButton;

    [Header("Buttons")]
    [Tooltip("도달한 칸의 보상을 한 번에 받는다(모든 탭, 두 줄). 받을 것이 없으면 잠긴다.")]
    [SerializeField] private Button claimButton;
    [Tooltip("선택. 받을 것이 있을 때 켜지는 배지(Source=Manual).")]
    [SerializeField] private BadgeView claimBadge;
    [SerializeField] private Button closeButton;

    [Header("Currency")]
    [Tooltip("선택. 상단 재화 패널(받은 보상이 바로 보이도록).")]
    [SerializeField] private CurrencyPanel currencyPanel;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private readonly List<PassRewardNode> _nodes = new();
    private readonly List<UITabButton> _tabs = new();
    private readonly List<RewardSlotView> _nonPassSlots = new();
    private int _nonPassLevel = -1;   // 논패스 보상 목록을 마지막으로 그린 레벨(같으면 다시 만들지 않는다)
    private int _selectedCycle;       // 0 = 아직 고르지 않음(첫 현황이 오면 정한다)
    private int _boundCycle;          // 트랙에 지금 그려진 탭(탭이 바뀔 때만 스크롤을 맞춘다)
    private bool _busy;
    private bool _wired;

    private PassInfoDto Info => _ctx != null ? _ctx.Pass.Info.Value : null;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (currencyPanel != null) currencyPanel.Initialize(ctx);

        if (!_wired) {
            if (claimButton != null)
                claimButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); ClaimAsync().Forget(); });
            if (buyPassButton != null)
                buyPassButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); MoveToShopAsync().Forget(); });
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });

            // 현황은 조회 응답뿐 아니라 서버 푸시로도 바뀐다(화면을 연 채로 기력을 써 레벨이 오르는 경우 등).
            _ctx.Pass.Info.Subscribe(_ => Refresh()).AddTo(this);

            // 서버 푸시는 새 레벨에 닿을 때만 오므로, 레벨 사이의 기력 진행(게이지/소모 기력)은 이 화면이 따라간다.
            // 던전을 깰 때마다 기력이 바뀌므로 화면이 열려 있는 동안만 그때 현황을 다시 받는다(연속 변화는 한 번으로 묶는다).
            _ctx.Store.Stamina.Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Where(_ => isActiveAndEnabled)
                .Subscribe(_ => _ctx.Pass.RefreshAsync(_ct).Forget())
                .AddTo(this);
            _wired = true;
        }

        BuildNodes();
        Refresh();          // 응답을 기다리는 동안에도 보상 표는 보이게 한다.
        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        try {
            await _ctx.Pass.RequestInfoAsync(_ct);   // 반영은 Info 구독이 한다.
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("패스 정보를 불러오지 못했습니다.");
        }
    }

    // 한 탭의 칸 수는 많아야 LevelsPerCycle + 1(첫 탭의 시작 칸)이라 그만큼만 만들어 두고 탭마다 다시 칠한다.
    private void BuildNodes() {
        if (_nodes.Count > 0 || trackContent == null || nodePrefab == null) return;

        for (int i = 0; i <= PassRules.LevelsPerCycle; i++) {
            var node = Instantiate(nodePrefab, trackContent);
            node.gameObject.SetActive(false);
            _nodes.Add(node);
        }
    }

    private void Refresh() {
        if (_ctx == null) return;
        var info = Info;
        int level = info != null ? info.Level : 0;

        if (_selectedCycle == 0 && info != null) _selectedCycle = DefaultCycle(info);
        int visible = PassRules.VisibleCycles(level);
        int cycle = Mathf.Clamp(_selectedCycle == 0 ? 1 : _selectedCycle, 1, visible);

        RefreshStatus(info);
        RefreshTabs(info, visible, cycle);
        RefreshNodes(info, cycle);
        RefreshNonPass(info);

        bool canClaim = info != null && info.CanClaim;
        if (claimButton != null) claimButton.interactable = canClaim && !_busy;
        if (claimBadge != null) claimBadge.SetOn(canClaim);
        // 평생 패스를 가진 계정에는 구매 버튼 자체를 숨긴다(현황을 받기 전에도 산 사람에게 잠깐 보이지 않도록 받은 뒤에만 켠다).
        if (buyPassButton != null) {
            buyPassButton.gameObject.SetActive(info != null && !info.HasPass);
            buyPassButton.interactable = !_busy;
        }

        if (_boundCycle != cycle) {
            _boundCycle = cycle;
            ScrollToFocusAsync(cycle).Forget();
        }
    }

    // 처음 열 때 보여 줄 탭: 받을 칸이 남은 첫 탭, 없으면 지금 진행 중인 탭.
    private static int DefaultCycle(PassInfoDto info) {
        int visible = PassRules.VisibleCycles(info.Level);
        for (int c = 1; c <= visible; c++)
            if (PassRules.CycleHasClaimable(c, info)) return c;
        return visible;
    }

    private void RefreshStatus(PassInfoDto info) {
        int level = info != null ? info.Level : 0;
        long spent = info != null ? info.StaminaSpent : 0;

        string hex = ColorUtility.ToHtmlStringRGB(highlightColor);
        if (levelText != null) levelText.text = $"패스 레벨: <color=#{hex}>{level:N0}</color>";
        // 누적값이 아니라 지금 레벨 안에서 쓴 기력(0 ~ 500)만 보여 준다. 최대 레벨이면 더 오를 칸이 없으므로 가득 찬 것으로 둔다.
        long inLevel = level >= PassRules.MaxLevel ? PassRules.StaminaPerLevel : spent % PassRules.StaminaPerLevel;
        if (staminaText != null)
            staminaText.text = $"소모한 기력: <color=#{hex}>{inLevel:N0}</color> / {PassRules.StaminaPerLevel:N0}";
    }

    // 탭은 열린 사이클 수만큼만 만든다. 레벨이 올라 새 사이클이 열리면 그때 하나씩 늘어난다.
    private void RefreshTabs(PassInfoDto info, int visible, int selected) {
        if (tabParent == null || tabPrefab == null) return;

        while (_tabs.Count < visible) {
            int cycle = _tabs.Count + 1;
            var tab = Instantiate(tabPrefab, tabParent);
            tab.gameObject.SetActive(true);
            tab.Bind(string.Format(tabLabelFormat, cycle), () => SelectCycle(cycle));
            _tabs.Add(tab);
        }

        for (int i = 0; i < _tabs.Count; i++) {
            int cycle = i + 1;
            _tabs[i].SetSelected(cycle == selected);
            var badge = _tabs[i].GetComponentInChildren<BadgeView>(true);
            if (badge != null) badge.SetOn(PassRules.CycleHasClaimable(cycle, info));
        }
    }

    private void SelectCycle(int cycle) {
        if (_busy || cycle == _selectedCycle) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        _selectedCycle = cycle;
        Refresh();
    }

    private void RefreshNodes(PassInfoDto info, int cycle) {
        int first = PassRules.FirstLevelOf(cycle);
        int count = PassRules.LastLevelOf(cycle) - first + 1;

        for (int i = 0; i < _nodes.Count; i++) {
            bool used = i < count;
            if (_nodes[i].gameObject.activeSelf != used) _nodes[i].gameObject.SetActive(used);
            if (used) _nodes[i].Bind(_ctx, first + i, info);
        }
    }

    // 평생 패스가 없을 때만 켠다. 보여 줄 보상은 레벨이 바뀔 때만 다시 만든다.
    private void RefreshNonPass(PassInfoDto info) {
        bool show = info != null && !info.HasPass;
        if (nonPassGroup != null) nonPassGroup.SetActive(show);
        if (!show || nonPassRewardParent == null || nonPassRewardPrefab == null) return;
        if (_nonPassLevel == info.Level) return;
        _nonPassLevel = info.Level;

        foreach (var s in _nonPassSlots) if (s != null) Destroy(s.gameObject);
        _nonPassSlots.Clear();

        foreach (var reward in PassRules.PassRowRewardsUpTo(info.Level)) {
            var slot = Instantiate(nonPassRewardPrefab, nonPassRewardParent);
            slot.gameObject.SetActive(true);
            slot.Bind(RewardVisualResolver.Resolve(_ctx, reward));
            _nonPassSlots.Add(slot);
        }
    }

    /// <summary>
    /// 고른 탭에서 볼 자리로 트랙을 맞춘다. 우선순위는 "받을 수 있는 첫 칸" > "도달한 마지막 칸" 이다.
    /// 칸이 실제로 배치된 뒤라야 위치를 계산할 수 있어 한 프레임 뒤에 적용한다(멸망전 트랙과 같은 방식).
    /// </summary>
    private async UniTaskVoid ScrollToFocusAsync(int cycle) {
        var info = Info;
        int first = PassRules.FirstLevelOf(cycle);
        int last = PassRules.LastLevelOf(cycle);
        if (trackScroll == null || last <= first) return;

        int level = info != null ? info.Level : 0;
        int focus = -1;
        if (info != null)
            for (int l = first; l <= Math.Min(last, level) && focus < 0; l++)
                if (PassRules.IsClaimable(l, level, info.FreeClaimedCount)
                    || (info.HasPass && PassRules.IsClaimable(l, level, info.PassClaimedCount)))
                    focus = l;
        if (focus < 0) focus = Mathf.Clamp(level, first, last);

        try {
            await UniTask.NextFrame(_ct);
        } catch (OperationCanceledException) {
            return;
        }
        if (trackScroll == null) return;
        Canvas.ForceUpdateCanvases();

        // 트랙은 가로로 늘어서므로 수평 위치를 쓴다(0 = 맨 왼쪽).
        trackScroll.horizontalNormalizedPosition = Mathf.Clamp01((float)(focus - first) / (last - first));
    }

    /// <summary>
    /// 게이지를 실제 칸 위치에 맞춘다(멸망전 트랙과 같은 이유). 칸의 중심은 칸의 한가운데라 레벨 비율로
    /// 채우면 눈금과 칸이 어긋나므로, 칸의 실제 위치를 재서 역산한다. 레이아웃이 확정되는 시점을 알 수 없어
    /// 매 프레임 확인하며, 계산은 칸 두 개의 좌표 변환뿐이다.
    /// </summary>
    private void LateUpdate() {
        if (trackSlider == null || _nodes.Count == 0 || _boundCycle == 0) return;

        float value = ResolveFillValue(_boundCycle);
        if (!Mathf.Approximately(trackSlider.value, value)) trackSlider.SetValueWithoutNotify(value);
    }

    // 쓴 기력을 소수 레벨로 보고(예: 7,600 = 15.2), 그 레벨이 걸친 두 칸 사이를 비율로 잇는다.
    private float ResolveFillValue(int cycle) {
        var info = Info;
        if (info == null) return 0f;

        int first = PassRules.FirstLevelOf(cycle);
        int last = PassRules.LastLevelOf(cycle);
        float progress = Mathf.Min(PassRules.MaxLevel, (float)info.StaminaSpent / PassRules.StaminaPerLevel);

        if (progress >= last) return 1f;
        if (progress < first - 1) return 0f;

        int lower = Mathf.FloorToInt(progress);
        // 탭의 첫 칸 앞은 "트랙 왼쪽 끝" 으로 본다(앞 탭의 마지막 칸은 이 트랙에 없다).
        float lowerValue = lower < first ? 0f : NodeFillValue(lower - first, last - first + 1);
        float upperValue = NodeFillValue(lower + 1 - first, last - first + 1);
        return Mathf.Clamp01(Mathf.Lerp(lowerValue, upperValue, progress - lower));
    }

    /// <summary>
    /// 그 칸에 도달했을 때 게이지가 닿아야 하는 지점(0~1). 칸의 오른쪽 끝을 잰다(칸이 "다 찬" 것으로 보이도록).
    /// 레이아웃이 아직 잡히지 않았으면 칸 수 비율로 떨어진다.
    /// </summary>
    private float NodeFillValue(int index, int count) {
        var fillArea = trackSlider.fillRect != null ? trackSlider.fillRect.parent as RectTransform : null;
        var node = index >= 0 && index < _nodes.Count && _nodes[index] != null
            ? _nodes[index].transform as RectTransform
            : null;

        if (fillArea == null || node == null || fillArea.rect.width <= 0f || count <= 0)
            return Mathf.Clamp01((float)(index + 1) / Mathf.Max(1, count));

        var rect = node.rect;
        float localX = fillArea.InverseTransformPoint(node.TransformPoint(new Vector2(rect.xMax, rect.center.y))).x;
        return Mathf.Clamp01((localX - fillArea.rect.xMin) / fillArea.rect.width);
    }

    private async UniTaskVoid ClaimAsync() {
        if (_busy) return;
        _busy = true;
        Refresh();

        try {
            var res = await _ctx.Pass.ClaimAsync(_ct);
            if (!res.IsSuccess) { _ctx.Ui.ShowToast(FailMessage(res.FailReason)); return; }

            // 아이템/조각/코스튬은 공용 경로가, 범용 재화(티켓/한계돌파권/마일리지)는 더하는 경로가 처리한다.
            // 골드/다이아/기력은 서버가 최신 잔액을 돌려준다.
            _ctx.Store.ApplyGrantedRewards(res.Granted);
            _ctx.Store.AddGrantedCurrencies(res.Granted);
            _ctx.Store.Gold.Value = res.NewGold;
            _ctx.Store.Diamond.Value = res.NewDiamond;
            _ctx.Store.Stamina.Value = res.NewStamina;

            // 장비는 개체 정보(굴린 능력치)가 함께 오므로 가방에 바로 넣는다.
            _ctx.Store.ApplyEquipRolls(res.EquipRolls);

            _ctx.Sound.PlaySfx(SfxKey.Claim);
            await ResyncInventoryAsync(res.Granted);
            // 장비는 능력치가 개체마다 달라 보상 표의 아이콘만으로는 무엇을 받았는지 알 수 없다.
            // 장비 가챠와 같은 연출로 받은 장비를 전부 보여 준 뒤, 나머지 보상을 보상 표로 보여 준다.
            await ShowEquipRollsAsync(res.EquipRolls);
            await ShowRewardsAsync(WithoutEquips(res.Granted, res.EquipRolls.Count > 0));
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("보상을 받지 못했습니다.");
        } finally {
            _busy = false;
            Refresh();
        }
    }

    // 만능조각은 가방(인벤토리) 스냅샷으로만 들어오므로 받았으면 서버 기준으로 다시 맞춘다(우편 수령과 같은 방식).
    // 장비는 수령 결과의 개체 정보로 이미 넣었으므로 여기서 다시 받지 않는다.
    private async UniTask ResyncInventoryAsync(IReadOnlyList<RewardItem> granted) {
        bool needInventory = false;
        foreach (var r in granted)
            if (r.Kind == RewardItemKind.UniversalShard) { needInventory = true; break; }
        if (!needInventory || _ctx.Inventory == null) return;

        var inv = await _ctx.Inventory.RequestInventoryAsync(_ct);
        _ctx.Store.ApplyInventory(inv);
    }

    // 받은 장비를 장비 가챠 연출로 한 장씩 보여 준다(상점 패키지의 장비 획득과 같은 팝업). 닫힐 때까지 기다린다.
    private async UniTask ShowEquipRollsAsync(List<EquipRollDto> rolls) {
        if (rolls == null || rolls.Count == 0) return;

        var reference = _ctx.Popups != null ? _ctx.Popups.EquipGachaPopup : null;
        if (reference == null || !reference.RuntimeKeyIsValid()) {
            _ctx.Ui.ShowToast("장비를 획득했습니다.");
            return;
        }
        var popup = await _ctx.Ui.ShowPopupAsync<EquipGachaPopup>(reference, _ct);
        popup.Play(rolls, _ctx);
        await popup.WaitUntilClosedAsync();
    }

    // 장비 연출로 이미 보여 준 장비 줄을 보상 표에서 뺀다(같은 것을 두 번 보여 주지 않게). 연출이 없었으면 그대로 둔다.
    private static List<RewardItem> WithoutEquips(IReadOnlyList<RewardItem> granted, bool equipsShown) {
        var list = new List<RewardItem>(granted);
        if (equipsShown) list.RemoveAll(r => r.Kind == RewardItemKind.Equip);
        return list;
    }

    // 받은 것을 공용 보상 표(RewardInfoPopup)로 한 줄에 몰아 보여 준다(장비는 앞의 장비 연출이 따로 보여 준다).
    private async UniTask ShowRewardsAsync(IReadOnlyList<RewardItem> granted) {
        if (granted == null || granted.Count == 0) return;

        var reference = _ctx.Popups != null ? _ctx.Popups.RewardInfoPopup : null;
        if (reference == null || !reference.RuntimeKeyIsValid()) {
            _ctx.Ui.ShowToast("패스 보상을 받았습니다.");
            return;
        }

        var popup = await _ctx.Ui.ShowPopupAsync<RewardInfoPopup>(reference, _ct);
        popup.Show(_ctx, "패스 보상", "도달한 레벨의 보상을 받았습니다.",
                   new List<RewardInfoPopup.Row> { new("획득", granted, true) });
    }

    /// <summary>
    /// 평생 패스 구매 버튼. 여기서 바로 결제하지 않고 상점의 평생 패스 자리(한정 > 계정)로 보낸다
    /// (한정 판매 안내창과 같은 방식). 구매 확인과 청약철회 안내는 상점 흐름이 맡는다.
    ///
    /// 이 화면은 닫지 않고 상점을 위에 띄운다. 상점에서 사고 돌아오면 서버 푸시로 패스 줄이 열린 화면이 그대로 보인다.
    /// </summary>
    private async UniTaskVoid MoveToShopAsync() {
        var reference = _ctx.Popups != null ? _ctx.Popups.ShopPopup : null;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("상점을 열 수 없습니다."); return; }

        try {
            var shop = await _ctx.Ui.ShowPopupAsync<ShopPopup>(reference, _ct);
            shop.SetEntry(PassRules.ShopMajor, PassRules.ShopSub);
            shop.Initialize(_ctx);
            await shop.ShowAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("상점을 열지 못했습니다.");
        }
    }

    private static string FailMessage(int fail) => fail switch {
        1 => "받을 수 있는 보상이 없습니다.",
        _ => "보상을 받지 못했습니다."
    };
}
