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
/// 길드 팝업. 메인 화면의 길드 버튼(HudMenuButton)에서 연다.
///
/// 화면은 소속 여부로 크게 둘로 갈린다.
///   길드 없음: 즉시 가입 추천 목록(새로고침) + 검색/생성 버튼
///   길드 있음: 정보 / 길드원 / 컨텐츠 / (길드장ㆍ부길드장만)관리 탭
///
/// 서버 응답 하나(GuildHomeDto)가 두 그룹의 표시 근거를 모두 담고 있어, 진입 시 왕복이 한 번이면 된다.
/// 상태가 바뀌는 작업(가입/탈퇴/추방 등)은 결과를 받은 뒤 다시 받아와 화면 전체를 갱신한다
/// (부분 갱신을 손으로 맞추면 인원수/권한/배지가 어긋난다).
/// </summary>
public sealed class GuildPopup : UIView, IHudPopup, IHudPopupTab {
    // 탭 순서 규약: 정보(0) / 길드원(1) / 컨텐츠(2) / 관리(3) / 상점(4).
    // 인덱스로 여는 자리가 있으므로 탭을 늘릴 때는 반드시 뒤에만 붙인다(중간에 끼우면 아래 상수가 전부 어긋난다).

    /// <summary>컨텐츠 탭 인덱스(레이드/대항전). 전투 씬을 다녀온 뒤 이 탭으로 되돌아온다.</summary>
    public const int ContentTabIndex = 2;

    /// <summary>관리 탭 인덱스. 길드장/부길드장만 볼 수 있어 권한에 따라 버튼을 숨긴다.</summary>
    private const int ManageTabIndex = 3;

    /// <summary>상점 탭 인덱스. 이 탭에서만 길드 포인트 재화 패널을 띄운다.</summary>
    private const int ShopTabIndex = 4;

    [Header("Group")]
    [Tooltip("길드가 없을 때 보여줄 그룹.")]
    [SerializeField] private GameObject noGuildGroup;
    [Tooltip("길드가 있을 때 보여줄 그룹.")]
    [SerializeField] private GameObject hasGuildGroup;
    [Tooltip("응답을 기다리는 동안 표시할 로딩 그룹(선택).")]
    [SerializeField] private GameObject loadingGroup;

    [Header("No Guild")]
    [Tooltip("추천 길드 카드가 생성될 부모.")]
    [SerializeField] private Transform recommendRoot;
    [SerializeField] private GuildCard guildCardPrefab;
    [Tooltip("추천 목록 새로고침 버튼. 누르면 쿨다운 동안 잠긴다.")]
    [SerializeField] private Button refreshButton;
    [Tooltip("선택. 새로고침 쿨다운 남은 시간 표시.")]
    [SerializeField] private TMP_Text refreshCooldownText;
    [SerializeField] private Button searchButton;
    [SerializeField] private Button createButton;
    [Tooltip("선택. 추천 결과가 하나도 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyRecommendLabel;

    [Header("Has Guild - Tabs")]
    [Tooltip("탭 버튼(정보/길드원/컨텐츠/관리 순). 관리 탭은 권한이 없으면 자동으로 숨긴다.")]
    [SerializeField] private UITabButton[] tabButtons;
    [Tooltip("탭 페이지(탭 버튼과 같은 순서).")]
    [SerializeField] private GameObject[] tabPages;
    [Tooltip("가입 신청 배지(관리 탭 버튼 위). 대기 중인 신청이 있으면 켜진다.")]
    [SerializeField] private BadgeView manageTabBadge;

    [Header("Has Guild - Info")]
    [SerializeField] private TMP_Text guildNameText;
    [SerializeField] private Image guildMarkImage;
    [SerializeField] private TMP_Text guildLevelText;
    [SerializeField] private TMP_Text guildExpText;
    [SerializeField] private Slider guildExpSlider;
    [SerializeField] private TMP_Text guildCombatPowerText;
    [SerializeField] private TMP_Text guildMemberCountText;
    [SerializeField] private TMP_Text guildJoinTypeText;
    [SerializeField] private TMP_Text guildMasterText;
    [SerializeField] private TMP_Text guildCreatedText;
    [SerializeField] private TMP_Text guildIntroText;
    [SerializeField] private TMP_Text guildNoticeText;
    [SerializeField] private TMP_Text myRoleText;
    [Tooltip("길드 탈퇴 버튼. 길드장은 위임하거나 해체해야 하므로 서버가 거절한다.")]
    [SerializeField] private Button leaveButton;
    [Tooltip("공헌도 상위 길드원이 생성될 부모(정보 탭).")]
    [SerializeField] private Transform topContributorRoot;
    [Tooltip("공헌도 상위 슬롯 프리팹. 목록용과 다른 모양을 쓰려고 따로 둔다. 비우면 길드원 목록 프리팹을 쓴다.")]
    [SerializeField] private GuildMemberSlot topContributorSlotPrefab;
    [Tooltip("정보 탭에 보여줄 공헌도 상위 인원 수.")]
    [SerializeField, Min(1)] private int topContributorCount = 3;

    [Header("Has Guild - Members")]
    [SerializeField] private Transform memberRoot;
    [SerializeField] private GuildMemberSlot memberSlotPrefab;

    [Header("Content Tab")]
    [Tooltip("길드 레이드 패널(컨텐츠 탭). 비우면 레이드 표시를 생략한다.")]
    [SerializeField] private GuildRaidPanel raidPanel;
    [Tooltip("길드 대항전 패널(컨텐츠 탭). 비우면 대항전 표시를 생략한다.")]
    [SerializeField] private GuildWarPanel warPanel;

    [Header("Shop Tab")]
    [Tooltip("길드 상점 페이지(상점 탭). 일반 상점과 같은 컴포넌트를 쓰며 길드 포인트로 결제한다. 비우면 상점 표시를 생략한다.")]
    [SerializeField] private ShopOfferPage shopPage;
    [Tooltip("길드 포인트 재화 패널. 상점 탭에서만 켜지므로 프리팹에서는 꺼 두어도 된다. " +
             "어떤 재화를 보여줄지는 자식 CurrencyView 구성으로 정한다.")]
    [SerializeField] private CurrencyPanel shopCurrencyPanel;

    [Header("Sub Popups")]
    [Tooltip("관리 탭 패널(선택). 있으면 길드 정보와 함께 초기화한다.")]
    [SerializeField] private GuildManagePanel managePanel;

    [Header("Catalog")]
    [SerializeField] private GuildMarkCatalog markCatalog;

    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private GuildHomeDto _home;
    private bool _busy;
    private float _refreshReadyAt;   // 새로고침이 다시 가능해지는 시각(언스케일드)
    private int _selectedTab;

    private readonly List<GuildCard> _cards = new();
    private readonly List<GuildMemberSlot> _memberSlots = new();
    private readonly List<GuildMemberSlot> _topContributorSlots = new();

    public GuildMarkCatalog Marks => markCatalog;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { Click(); CloseAsync().Forget(); });
        }
        if (refreshButton != null) {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(() => { Click(); RefreshRecommendAsync().Forget(); });
        }
        if (searchButton != null) {
            searchButton.onClick.RemoveAllListeners();
            searchButton.onClick.AddListener(() => { Click(); OpenSearchAsync().Forget(); });
        }
        if (createButton != null) {
            createButton.onClick.RemoveAllListeners();
            createButton.onClick.AddListener(() => { Click(); OpenCreateAsync().Forget(); });
        }
        if (leaveButton != null) {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(() => { Click(); ConfirmLeaveAsync().Forget(); });
        }

        // 재화 위젯은 스토어를 구독하므로 팝업이 열릴 때 한 번만 초기화한다(표시 여부는 탭이 정한다).
        if (shopCurrencyPanel != null) shopCurrencyPanel.Initialize(_ctx);

        BindTabs();
        ReloadAsync().Forget();
    }

    private void Update() {
        if (refreshButton == null) return;
        // 쿨다운이 끝나면 버튼을 다시 열고, 남은 시간이 있으면 표시한다.
        float remain = _refreshReadyAt - Time.unscaledTime;
        bool ready = remain <= 0f;
        if (refreshButton.interactable != ready && !_busy) refreshButton.interactable = ready;
        if (refreshCooldownText != null) {
            refreshCooldownText.gameObject.SetActive(!ready);
            if (!ready) refreshCooldownText.text = $"{Mathf.CeilToInt(remain)}초";
        }
    }

    // ── 로드 / 표시 ───────────────────────────────────
    /// <summary>서버 상태를 다시 받아 화면 전체를 구성한다. 상태가 바뀌는 작업 뒤에는 항상 이걸 부른다.</summary>
    private async UniTaskVoid ReloadAsync() {
        SetLoading(true);
        try {
            _home = await _ctx.Guild.RequestHomeAsync(_ct);
            Build();
            // 메인 화면 길드 버튼의 배지도 방금 받은 값으로 맞춘다(신청을 처리하면 그 자리에서 불이 꺼진다).
            _ctx.Badges?.SetGuildPendingApplications(_home.PendingApplications);
            _ctx.Badges?.SetGuildRaidFreeEntries(_home.RaidFreeEntries);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("길드 정보를 불러오지 못했습니다.");
        } finally {
            SetLoading(false);
        }
    }

    private void Build() {
        if (_home == null) return;

        bool has = _home.HasGuild;
        if (noGuildGroup != null) noGuildGroup.SetActive(!has);
        if (hasGuildGroup != null) hasGuildGroup.SetActive(has);

        if (has) BuildGuild();
        else BuildRecommends(_home.Recommends);
    }

    private void BuildRecommends(List<GuildSummaryDto> list) {
        ClearCards();
        if (recommendRoot == null || guildCardPrefab == null) return;

        foreach (var g in list) {
            var card = Instantiate(guildCardPrefab, recommendRoot);
            card.Bind(g, markCatalog, _home.AppliedGuildIds.Contains(g.GuildId), OnGuildCardAction);
            _cards.Add(card);
        }
        if (emptyRecommendLabel != null) emptyRecommendLabel.SetActive(list.Count == 0);
    }

    private void BuildGuild() {
        var s = _home.Summary;

        if (guildNameText != null) guildNameText.text = s.Name;
        if (guildMarkImage != null) {
            guildMarkImage.sprite = markCatalog != null ? markCatalog.Icon(s.MarkId) : null;
            guildMarkImage.enabled = guildMarkImage.sprite != null;
        }
        if (guildLevelText != null) guildLevelText.text = $"Lv.{s.Level}";
        if (guildExpText != null) guildExpText.text = $"{_home.ExpInLevel} / {_home.ExpPerLevel}";
        if (guildExpSlider != null) {
            guildExpSlider.minValue = 0f;
            guildExpSlider.maxValue = Mathf.Max(1, _home.ExpPerLevel);
            guildExpSlider.SetValueWithoutNotify(_home.ExpInLevel);
        }
        if (guildCombatPowerText != null) guildCombatPowerText.text = s.CombatPower.ToString("N0");
        if (guildMemberCountText != null) guildMemberCountText.text = $"{s.MemberCount} / {GuildRules.MaxMembers}";
        if (guildJoinTypeText != null) guildJoinTypeText.text = GuildRules.JoinTypeName(s.JoinType);
        if (guildMasterText != null) guildMasterText.text = _home.MasterNickname;
        if (guildCreatedText != null) guildCreatedText.text = DateText(_home.CreatedUnix);
        if (guildIntroText != null) guildIntroText.text = s.Intro;
        if (guildNoticeText != null) guildNoticeText.text = _home.Notice;
        if (myRoleText != null) myRoleText.text = GuildRules.RoleName(_home.MyRole);

        BuildMembers();
        BuildTopContributors();
        RefreshTabAccess();

        // 컨텐츠 패널은 각자 자기 현황을 서버에서 받아 오므로 여기서는 초기화만 걸어 준다.
        if (raidPanel != null) raidPanel.Initialize(_ctx, this);
        if (warPanel != null) warPanel.Initialize(_ctx, this);

        if (managePanel != null) managePanel.Initialize(_ctx, this, _home, () => ReloadAsync().Forget());

        BuildShopAsync().Forget();
    }

    /// <summary>
    /// 길드 상점 탭. 상품/결제/구매 한도가 일반 상점과 같은 구조라 상점 페이지 컴포넌트를 그대로 재사용하고,
    /// 카탈로그에서 길드 대분류 상품만 걸러 넘긴다(길드 전용 구매 경로를 따로 만들지 않는다).
    /// 상점 카탈로그는 길드 정보와 별개 응답이라 여기서 따로 받아 온다.
    /// </summary>
    private async UniTaskVoid BuildShopAsync() {
        if (shopPage == null) return;
        try {
            var catalog = await _ctx.Shop.RequestShopAsync(_ct);

            // 응답에 실려 온 재화 잔량을 스토어에 반영한다(재화 패널이 스토어를 구독하므로 이걸 빠뜨리면 값이 0 으로 남는다).
            // 상점 화면이라면 어디서든 하는 처리라 상점 팝업/가챠 팝업과 같은 자리에서 같은 방식으로 한다.
            _ctx.Store.Diamond.Value = catalog.Diamond;
            _ctx.Store.ApplyShopCurrencies(catalog.Currencies);

            var offers = new List<ShopOfferDto>();
            foreach (var o in catalog.Offers) if (o.Major == GuildRules.ShopMajor) offers.Add(o);

            // 주간/일간 앞에 "전체" 탭을 둔다(진입하자마자 파는 물건 전부가 한 화면에 보이도록. 다른 상점 페이지와 같은 구성이다).
            shopPage.Initialize(_ctx, offers, hasAllTab: true, catalog);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("길드 상점을 불러오지 못했습니다.");
        }
    }

    private void BuildMembers() {
        ClearMemberSlots();
        if (memberRoot == null || memberSlotPrefab == null) return;

        foreach (var m in _home.Members) {
            var slot = Instantiate(memberSlotPrefab, memberRoot);
            slot.Bind(m, _ctx, OpenUserProfile);
            _memberSlots.Add(slot);
        }
    }

    // ── 탭 ───────────────────────────────────────────
    private void BindTabs() {
        if (tabButtons == null) return;
        for (int i = 0; i < tabButtons.Length; i++) {
            if (tabButtons[i] == null) continue;
            int index = i;
            tabButtons[i].Bind(null, () => { Click(); SelectTab(index); });
        }
        SelectTab(0);
    }

    /// <summary>지정한 탭을 연다(IHudPopupTab). 전투 씬 복귀 시 보던 탭으로 되돌리는 데 쓴다.</summary>
    public void SelectTab(int index) {
        if (tabButtons == null || index < 0 || index >= tabButtons.Length) return;
        _selectedTab = index;
        if (tabButtons != null)
            for (int i = 0; i < tabButtons.Length; i++)
                if (tabButtons[i] != null) tabButtons[i].SetSelected(i == index);
        if (tabPages != null)
            for (int i = 0; i < tabPages.Length; i++)
                if (tabPages[i] != null) tabPages[i].SetActive(i == index);

        // 재화 패널은 상점 탭에서만 필요하다. 탭 페이지 밖(팝업 헤더 등)에 두어도 되도록 여기서 직접 켜고 끈다.
        if (shopCurrencyPanel != null) shopCurrencyPanel.gameObject.SetActive(index == ShopTabIndex);
    }

    // 관리 탭은 길드장/부길드장만 볼 수 있다. 권한이 없으면 버튼 자체를 숨기고, 그 탭에 있었으면 정보 탭으로 되돌린다.
    private void RefreshTabAccess() {
        if (tabButtons == null || tabButtons.Length <= ManageTabIndex) return;

        bool canManage = _home.CanManage;
        if (tabButtons[ManageTabIndex] != null) tabButtons[ManageTabIndex].gameObject.SetActive(canManage);
        if (manageTabBadge != null) manageTabBadge.SetOn(canManage && _home.PendingApplications > 0);
        if (!canManage && _selectedTab == ManageTabIndex) SelectTab(0);
    }

    // ── 동작 ─────────────────────────────────────────
    // 카드의 버튼 하나가 길드 상태에 따라 가입/신청/신청취소 세 가지로 갈린다.
    private void OnGuildCardAction(GuildSummaryDto guild) {
        if (guild == null || _busy) return;
        Click();

        bool applied = _home != null && _home.AppliedGuildIds.Contains(guild.GuildId);
        var action = applied ? GuildActionType.CancelApply
                   : guild.JoinType == GuildJoinType.Instant ? GuildActionType.Join
                   : GuildActionType.Apply;
        RunAsync(() => _ctx.Guild.ActAsync(action, guildId: guild.GuildId, combatPower: MyCombatPower(), ct: _ct)).Forget();
    }

    private async UniTaskVoid RefreshRecommendAsync() {
        if (_busy || Time.unscaledTime < _refreshReadyAt) return;
        _refreshReadyAt = Time.unscaledTime + GuildRules.RecommendRefreshCooldown;
        if (refreshButton != null) refreshButton.interactable = false;

        try {
            var list = await _ctx.Guild.RequestListAsync(search: false, ct: _ct);
            BuildRecommends(list);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("목록을 불러오지 못했습니다.");
        }
    }

    private async UniTaskVoid OpenSearchAsync() {
        var reference = _ctx.Popups.GuildSearchPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<GuildSearchPopup>(reference, _ct);
            bool joined = await popup.ShowAsync(_ctx, this, _home);
            if (joined) ReloadAsync().Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private async UniTaskVoid OpenCreateAsync() {
        var reference = _ctx.Popups.GuildCreatePopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<GuildCreatePopup>(reference, _ct);
            bool created = await popup.ShowAsync(_ctx, MyCombatPower());
            if (created) ReloadAsync().Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private async UniTaskVoid ConfirmLeaveAsync() {
        if (_busy) return;
        // 길드장은 위임/해체가 먼저다. 눌러 보고 서버에서 거절당하는 것보다 미리 알려 주는 편이 낫다.
        if (_home != null && _home.MyRole == GuildRole.Master && _home.Summary.MemberCount > 1) {
            _ctx.Ui.ShowToast("길드장은 위임 후 탈퇴할 수 있습니다.");
            return;
        }
        if (!await ConfirmAsync("길드 탈퇴", "정말 길드를 탈퇴하시겠습니까?")) return;
        await RunAsync(() => _ctx.Guild.ActAsync(GuildActionType.Leave, ct: _ct));
    }

    /// <summary>다른 유저의 프로필 팝업을 연다(본인용 프로필 팝업과 다른 프리팹).</summary>
    public void OpenUserProfile(int accountId) {
        Click();
        OpenUserProfileAsync(accountId).Forget();
    }

    private async UniTaskVoid OpenUserProfileAsync(int accountId) {
        var reference = _ctx.Popups.UserProfilePopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<UserProfilePopup>(reference, _ct);
            await popup.ShowAsync(_ctx, accountId, markCatalog);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// 길드 상태를 바꾸는 요청의 공통 실행부. 성공하면 화면 전체를 다시 받아 오고, 실패하면 사유를 알린다.
    /// 관리 패널도 이 창구를 쓰므로 처리 규칙이 한 곳에만 있다.
    /// </summary>
    public async UniTask<bool> RunAsync(Func<UniTask<GuildActionResultDto>> request) {
        // 앞선 요청이 아직 안 끝났다. 조용히 무시하면 저장을 눌렀는데 아무 일도 안 일어난 것처럼 보이므로 이유를 알린다.
        if (_busy) { _ctx.Ui.ShowToast("처리 중입니다. 잠시 후 다시 시도해 주세요."); return false; }
        _busy = true;
        try {
            var res = await request();
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(GuildRules.FailMessage(res.FailReason));
                return false;
            }
            if (res.NewDiamond > 0) _ctx.Store.Diamond.Value = res.NewDiamond;
            ReloadAsync().Forget();
            return true;
        } catch (OperationCanceledException) {
            return false;
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("요청을 처리하지 못했습니다.");
            return false;
        } finally {
            _busy = false;
        }
    }

    /// <summary>확인/취소 팝업. 되돌릴 수 없는 조작(탈퇴/추방/해체) 앞에 쓴다.</summary>
    public async UniTask<bool> ConfirmAsync(string title, string message) {
        var reference = _ctx.Popups.ConfirmCancelPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return true;
        var popup = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(reference, _ct);
        return await popup.ShowAsync(title, message);
    }

    /// <summary>현재 내 덱 전투력. 가입/신청 시 서버에 함께 보내 길드 전투력에 반영한다.</summary>
    public long MyCombatPower() => (long)AccountStats.TotalDeckCombatPower(_ctx);

    // ── 표시 헬퍼 ────────────────────────────────────
    private void SetLoading(bool on) {
        if (loadingGroup != null) loadingGroup.SetActive(on);
    }

    private void ClearCards() {
        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();
    }

    /// <summary>
    /// 정보 탭의 공헌도 상위 길드원. 길드원 목록을 이미 받아 왔으므로 서버를 다시 두드리지 않고 여기서 추린다.
    /// 슬롯 프리팹은 목록용과 따로 지정할 수 있다(좁은 자리에 순위만 보여 주는 등 모양이 다르기 때문).
    /// </summary>
    private void BuildTopContributors() {
        ClearTopContributorSlots();

        var prefab = topContributorSlotPrefab != null ? topContributorSlotPrefab : memberSlotPrefab;
        if (topContributorRoot == null || prefab == null) return;

        // 원본 순서(역할/전투력 정렬)를 흐트러뜨리지 않도록 복사본을 정렬한다.
        var ranked = new List<GuildMemberDto>(_home.Members);
        ranked.Sort((a, b) => b.Contribution.CompareTo(a.Contribution));

        int count = Mathf.Min(topContributorCount, ranked.Count);
        for (int i = 0; i < count; i++) {
            var slot = Instantiate(prefab, topContributorRoot);
            slot.Bind(ranked[i], _ctx, OpenUserProfile);
            _topContributorSlots.Add(slot);
        }
    }

    private void ClearTopContributorSlots() {
        foreach (var s in _topContributorSlots) if (s != null) Destroy(s.gameObject);
        _topContributorSlots.Clear();
    }

    private void ClearMemberSlots() {
        foreach (var s in _memberSlots) if (s != null) Destroy(s.gameObject);
        _memberSlots.Clear();
    }

    private void Click() => _ctx?.Sound.PlaySfx(SfxKey.ButtonClick);

    private static string DateText(long unix) =>
        unix <= 0 ? "-" : DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().ToString("yyyy. MM. dd");

    protected override void OnClosed() {
        ClearCards();
        ClearMemberSlots();
        ClearTopContributorSlots();
    }
}
