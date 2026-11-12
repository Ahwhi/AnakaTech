using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 멸망전 입구(슬라이드 메뉴의 멸망전 버튼). 최고 점수와 보상 트랙을 보여 주고, 도전을 시작하거나
/// 도달한 구간의 보상을 받는다.
///
/// 보상 트랙은 가로 스크롤 위에 점(DoomRewardNode)을 늘어놓고 그 뒤로 게이지가 차오르는 모양이다.
/// 점을 눌러서 받는 것이 아니라 수령 버튼 하나로 도달한 구간 전부를 받는다. 서버가 "어디까지 받았는가"를
/// 점수로 기억하므로, 최고 점수를 갱신한 만큼만 다음에 더 받게 된다.
/// </summary>
public sealed class DoomPopup : UIView, IHudPopup {
    [Header("Score")]
    [Tooltip("역대 최고 점수.")]
    [SerializeField] private TMP_Text bestScoreText;
    [Tooltip("보상 트랙 진행 게이지. 마지막 보상 점수를 가득 찬 것으로 본다(0~1 정규화).")]
    [SerializeField] private Slider trackSlider;

    [Header("Reward Track")]
    [Tooltip("보상 점이 생성될 부모(Horizontal Layout Group). 가로 스크롤뷰의 Content 로 둔다.")]
    [SerializeField] private Transform trackContent;
    [Tooltip("보상 점 프리팹(DoomRewardNode).")]
    [SerializeField] private DoomRewardNode nodePrefab;
    [Tooltip("트랙 스크롤뷰(선택). 열 때 지금 진행한 지점으로 자동 스크롤한다.")]
    [SerializeField] private ScrollRect trackScroll;

    [Header("Buttons")]
    [Tooltip("도전 시작. 누르면 멸망전 씬으로 넘어간다. 유료 참여면 확인을 받는다.")]
    [SerializeField] private Button startButton;
    [Tooltip("시작 버튼의 비용 표시(무료 라벨 / 다이아 아이콘 + 비용 묶음).")]
    [SerializeField] private EntryCostView startCost;
    [Tooltip("선택. 시작 버튼 배지(Manual). 오늘 무료 참여가 남았을 때만 켜진다.")]
    [SerializeField] private BadgeView startBadge;
    [Tooltip("도달한 구간의 보상을 한 번에 받는다. 받을 것이 없으면 잠긴다.")]
    [SerializeField] private Button claimButton;
    [Tooltip("선택. 받을 것이 있을 때 켜지는 배지.")]
    [SerializeField] private BadgeView claimBadge;
    [SerializeField] private Button closeButton;

    [Header("Currency")]
    [Tooltip("선택. 상단 재화 패널(받은 보상이 바로 보이도록).")]
    [SerializeField] private CurrencyPanel currencyPanel;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private readonly List<DoomRewardNode> _nodes = new();
    // 보상표가 100 구간을 넘어 점을 전부 만들면 열 때 끊긴다. 지금 볼 지점 앞뒤로만 점을 두고(창),
    // 점은 한 번만 만들어 창이 옮겨질 때 다른 구간으로 다시 칠한다.
    private const int WindowRadius = 10;
    private int _windowStart;   // 창의 첫 구간 인덱스(DoomRewardRules.Tiers 기준)
    private DoomInfoDto _info;
    private DoomEntryDto _entry;
    private bool _busy;
    private bool _wired;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (currencyPanel != null) currencyPanel.Initialize(ctx);

        if (!_wired) {
            if (startButton != null)
                startButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); StartDoomAsync().Forget(); });
            if (claimButton != null)
                claimButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); ClaimAsync().Forget(); });
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            _wired = true;
        }

        BuildTrack();
        Refresh();          // 응답을 기다리는 동안에도 보상 표는 보이게 한다.
        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        try {
            _info = await _ctx.Doom.RequestInfoAsync(_ct);
            _entry = await _ctx.Doom.RequestEntryAsync(_ct);
            Refresh();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("멸망전 정보를 불러오지 못했습니다.");
        }
    }

    // 점은 창 크기(앞뒤 WindowRadius + 자기 자리)만큼만 한 번 만든다. 이후 갱신은 어느 구간을 칠할지만 바꾼다.
    private void BuildTrack() {
        if (_nodes.Count > 0 || trackContent == null || nodePrefab == null) return;

        int count = Mathf.Min(DoomRewardRules.Tiers.Count, WindowRadius * 2 + 1);
        for (int i = 0; i < count; i++) {
            var node = Instantiate(nodePrefab, trackContent);
            node.gameObject.SetActive(true);
            _nodes.Add(node);
        }
    }

    /// <summary>
    /// 지금 보여 줄 기준 구간. 우선순위는 "받을 수 있는 첫 구간" > "도달한 마지막 구간" > 맨 앞이다.
    /// 받을 것이 있으면 그것이 가장 급하고, 없으면 다음에 노릴 자리를 보여 주는 셈이다.
    /// </summary>
    private static int FocusIndex(int bestScore, int claimedScore) {
        var tiers = DoomRewardRules.Tiers;
        for (int i = 0; i < tiers.Count; i++)
            if (bestScore >= tiers[i].Score && claimedScore < tiers[i].Score) return i;
        for (int i = tiers.Count - 1; i >= 0; i--)
            if (bestScore >= tiers[i].Score) return i;
        return 0;
    }

    // 기준 구간이 창 가운데 오도록 창을 잡는다. 양 끝에서는 창이 보상표 밖으로 나가지 않게 민다.
    private void PlaceWindow(int focus) {
        int total = DoomRewardRules.Tiers.Count;
        _windowStart = Mathf.Clamp(focus - WindowRadius, 0, Mathf.Max(0, total - _nodes.Count));
    }

    private void Refresh() {
        int best = _info != null ? _info.BestScore : 0;
        int claimed = _info != null ? _info.ClaimedScore : 0;
        bool canClaim = _info != null && _info.CanClaim;

        if (bestScoreText != null) bestScoreText.text = best.ToString("N0");
        if (trackSlider != null) {
            trackSlider.minValue = 0f;
            trackSlider.maxValue = 1f;
        }

        int focus = FocusIndex(best, claimed);
        PlaceWindow(focus);
        for (int k = 0; k < _nodes.Count; k++) {
            int i = _windowStart + k;
            if (i >= DoomRewardRules.Tiers.Count) break;
            var tier = DoomRewardRules.Tiers[i];
            _nodes[k].Bind(_ctx, tier.Score, tier.Reward, best, claimed);
        }

        if (claimButton != null) claimButton.interactable = canClaim && !_busy;
        if (claimBadge != null) claimBadge.SetOn(canClaim);

        RefreshStart();

        ScrollToFocusAsync(focus - _windowStart).Forget();
    }

    // 시작 버튼: 무료면 무료 표시 + 배지, 유료면 다이아 비용. 하루 제한이 없으므로 현황을 받은 뒤에는 늘 누를 수 있고,
    // 다이아가 모자라면 비용 문구 색으로 알리고 누를 때 안내한다.
    // 현황을 받기 전에는 잠근다(비용을 모른 채 씬으로 넘어가지 않게).
    private void RefreshStart() {
        bool known = _entry != null;
        if (startCost != null) {
            if (known) startCost.Bind(_ctx.Currencies, CurrencyType.Diamond, _entry.NextEntryCost, CanAfford(_entry));
            else startCost.Clear();
        }
        if (startBadge != null) startBadge.SetOn(known && _entry.IsFree);
        if (startButton != null) startButton.interactable = known && !_busy;
    }

    private bool CanAfford(DoomEntryDto entry) => entry.IsFree || _ctx.Store.Diamond.Value >= entry.NextEntryCost;

    /// <summary>
    /// 기준 구간이 보이도록 창 안에서 트랙을 맞춘다(nodeIndex 는 창 안의 점 번호).
    /// 점이 실제로 배치된 뒤라야 위치를 계산할 수 있어 한 프레임 뒤에 적용한다(업적 보상 화면과 같은 방식).
    /// </summary>
    private async UniTaskVoid ScrollToFocusAsync(int nodeIndex) {
        if (trackScroll == null || _nodes.Count <= 1) return;

        try {
            await UniTask.NextFrame(_ct);
        } catch (OperationCanceledException) {
            return;
        }
        if (trackScroll == null) return;
        Canvas.ForceUpdateCanvases();

        // 트랙은 가로로 늘어서므로 수평 위치를 쓴다(0 = 맨 왼쪽).
        trackScroll.horizontalNormalizedPosition = Mathf.Clamp01((float)nodeIndex / (_nodes.Count - 1));
    }

    /// <summary>
    /// 게이지를 실제 보상 점 위치에 맞춘다.
    ///
    /// 점수를 그냥 0~1 로 환산하면(점수/1200) 눈금이 점과 어긋난다. 레이아웃 그룹이 놓은 점의 중심은
    /// 칸의 한가운데라 트랙 양 끝에서 반 칸씩 안쪽에 있기 때문이다. 그 차이는 화면 폭/여백/칸 수에 따라
    /// 달라지므로 인스펙터 숫자로 맞출 수가 없다.
    ///
    /// 그래서 점의 실제 위치를 재서 역산한다. 폭이 늘어나든 줄어들든, 스크롤되든, 칸 수가 바뀌든
    /// 언제나 맞는다. 매 프레임 도는 이유는 레이아웃이 확정되는 시점(열림 연출/회전/폰트 로드)을
    /// 미리 알 수 없기 때문이며, 계산은 점 두 개의 좌표 변환뿐이라 부담이 없다.
    /// </summary>
    private void LateUpdate() {
        if (trackSlider == null || _nodes.Count == 0) return;

        float value = ResolveFillValue(_info != null ? _info.BestScore : 0);
        if (!Mathf.Approximately(trackSlider.value, value)) trackSlider.SetValueWithoutNotify(value);
    }

    // 게이지는 지금 보이는 창 기준이다. 창 앞 구간 점수 = 왼쪽 끝, 창 마지막 구간 점수 = 오른쪽 끝.
    private float ResolveFillValue(int bestScore) {
        var tiers = DoomRewardRules.Tiers;
        if (bestScore <= 0 || tiers.Count == 0 || _nodes.Count == 0) return 0f;

        int first = _windowStart;
        int last = Mathf.Min(tiers.Count, _windowStart + _nodes.Count) - 1;
        if (bestScore >= tiers[last].Score) return 1f;

        // 창 앞쪽(창보다 이전 구간)의 점수는 "트랙 왼쪽 끝" 이다. 맨 앞 창이면 0점이 왼쪽 끝이다.
        int startScore = first > 0 ? tiers[first - 1].Score : 0;
        if (bestScore <= startScore) return 0f;

        // bestScore 가 아직 닿지 못한 창 안의 첫 구간. 그 앞 구간과의 사이를 점수 비율로 잇는다.
        int upper = first;
        while (upper < last && tiers[upper].Score < bestScore) upper++;

        int lowerScore = upper > first ? tiers[upper - 1].Score : startScore;
        float lowerValue = upper > first ? NodeFillValue(upper - 1 - first) : 0f;
        float upperValue = NodeFillValue(upper - first);

        int span = tiers[upper].Score - lowerScore;
        if (span <= 0) return upperValue;

        return Mathf.Clamp01(Mathf.Lerp(lowerValue, upperValue, (float)(bestScore - lowerScore) / span));
    }

    /// <summary>
    /// 그 구간의 점수를 채웠을 때 게이지가 닿아야 하는 지점(0~1).
    ///
    /// 점의 중심이 아니라 오른쪽 끝을 잰다. 한 구간(40점)을 채웠으면 그 칸이 "다 찬" 것으로 보여야지,
    /// 칸 절반만 차면 40점을 벌고도 덜 번 것처럼 읽힌다. 이러면 1,115점이 1,120 칸을 거의 다 채운 모습이 된다.
    ///
    /// 두 오브젝트의 부모가 달라도 되도록 월드 좌표를 거쳐 잰다.
    /// </summary>
    private float NodeFillValue(int index) {
        var fillArea = trackSlider.fillRect != null ? trackSlider.fillRect.parent as RectTransform : null;
        var node = index >= 0 && index < _nodes.Count && _nodes[index] != null
            ? _nodes[index].transform as RectTransform
            : null;

        // 아직 레이아웃이 잡히지 않았거나 Fill Area 가 없으면 칸 순번 비율로 떨어진다.
        // 칸이 고르게 놓였을 때 (순번 + 1) / 칸 수는 곧 그 칸의 오른쪽 끝이라 기준이 어긋나지 않는다.
        if (fillArea == null || node == null || fillArea.rect.width <= 0f)
            return _nodes.Count > 0 ? Mathf.Clamp01((float)(index + 1) / _nodes.Count) : 0f;

        var rect = node.rect;
        float localX = fillArea.InverseTransformPoint(node.TransformPoint(new Vector2(rect.xMax, rect.center.y))).x;
        return Mathf.Clamp01((localX - fillArea.rect.xMin) / fillArea.rect.width);
    }

    private async UniTaskVoid ClaimAsync() {
        if (_busy) return;
        _busy = true;
        if (claimButton != null) claimButton.interactable = false;

        try {
            var res = await _ctx.Doom.ClaimAsync(_ct);
            if (!res.IsSuccess) { _ctx.Ui.ShowToast(FailMessage(res.FailReason)); return; }

            _info = new DoomInfoDto { BestScore = res.BestScore, ClaimedScore = res.ClaimedScore, CanClaim = false };

            // 아이템/조각/코스튬은 공용 경로가, 범용 재화(티켓/증명서/마일리지)는 잔액을 따로 주지 않으므로
            // 더하는 경로가 함께 처리한다. 골드/다이아만 서버가 최신 잔액을 돌려준다.
            _ctx.Store.ApplyGrantedRewards(res.Granted);
            _ctx.Store.AddGrantedCurrencies(res.Granted);
            _ctx.Store.Gold.Value = res.NewGold;
            _ctx.Store.Diamond.Value = res.NewDiamond;

            _ctx.Sound.PlaySfx(SfxKey.Claim);
            await ShowRewardsAsync(res.Granted);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("보상을 받지 못했습니다.");
        } finally {
            _busy = false;
            Refresh();
        }
    }

    // 받은 것을 공용 보상 표(RewardInfoPopup)로 한 줄에 몰아 보여 준다. 전용 연출을 따로 두지 않는다.
    private async UniTask ShowRewardsAsync(IReadOnlyList<RewardItem> granted) {
        if (granted == null || granted.Count == 0) return;

        var reference = _ctx.Popups != null ? _ctx.Popups.RewardInfoPopup : null;
        if (reference == null || !reference.RuntimeKeyIsValid()) {
            _ctx.Ui.ShowToast("멸망전 보상을 받았습니다.");
            return;
        }

        var popup = await _ctx.Ui.ShowPopupAsync<RewardInfoPopup>(reference, _ct);
        // Show 가 표를 그리고 여는 것까지 맡는다(반환값 없음).
        popup.Show(_ctx, "멸망전 보상", "도달한 구간의 보상을 받았습니다.",
                   new List<RewardInfoPopup.Row> { new("획득", granted, true) });
    }

    private async UniTaskVoid StartDoomAsync() {
        if (_busy || _entry == null) return;

        // 무료분/다이아는 씬에 들어선 뒤 입장 요청(C_DoomEnter)이 쓴다. 여기서는 유료일 때 잔액과 동의만 확인한다
        // (서버도 잔액을 다시 검사하므로 이 확인은 안내용이다).
        if (!_entry.IsFree) {
            if (!CanAfford(_entry)) { _ctx.Ui.ShowToast(DoomEntryRules.NotEnoughDiamondMessage); return; }

            _busy = true;
            RefreshStart();
            bool ok;
            try {
                var confirm = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(_ctx.Popups.ConfirmCancelPopup, _ct);
                ok = await confirm.ShowAsync(DoomEntryRules.PaidEntryTitle,
                                             DoomEntryRules.PaidEntryConfirmMessage(_entry.NextEntryCost));
            } catch (OperationCanceledException) {
                return;
            } finally {
                _busy = false;
                if (this != null) RefreshStart();
            }
            if (!ok) return;
        }

        if (_ctx.DoomDb == null || _ctx.DoomDb.Get(1) == null) {
            _ctx.Ui.ShowToast("멸망전 데이터가 준비되지 않았습니다.");
            return;
        }
        if (_ctx.Scenes == null || _ctx.Scenes.DoomScene == null || !_ctx.Scenes.DoomScene.RuntimeKeyIsValid()) {
            Debug.LogError("[DoomPopup] SceneCatalog 에 멸망전 씬이 할당되지 않았습니다.");
            _ctx.Ui.ShowToast("멸망전 씬을 열 수 없습니다.");
            return;
        }

        // CloseAllPopups 는 이 팝업 자신도 파괴하므로, 그 뒤에 쓸 것들을 먼저 지역 변수로 확보한다.
        var ui = _ctx.Ui;
        var sceneService = _ctx.SceneService;
        var sceneKey = _ctx.Scenes.DoomScene.RuntimeKey;

        // 돌아왔을 때 이 팝업이 다시 열리도록 예약한 뒤 닫는다(쟁탈전/무한 타워와 같은 방식).
        // 다시 열 때 Initialize 가 최고 점수/보상/참여 현황을 서버에서 새로 받아 오므로 그 판의 결과가 반영된다.
        MainPopupReturn.Request(_ctx.Popups.DoomPopup);

        // UIRoot 는 씬 전환에도 살아남는다. 메인 씬에서 열려 있던 팝업을 닫지 않으면 멸망전 씬 위에 남는다.
        ui.CloseAllPopups();

        // 전환 중 이 팝업은 이미 파괴된 상태이므로 await 하지 않고 던진다(매뉴얼 6-3).
        sceneService.LoadAddressableAsync(sceneKey).Forget();
    }

    private static string FailMessage(int fail) => fail switch {
        1 => "받을 수 있는 보상이 없습니다.",
        _ => "보상을 받지 못했습니다."
    };
}
