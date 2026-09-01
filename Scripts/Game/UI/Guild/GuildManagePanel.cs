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
/// 길드 관리 탭(길드장/부길드장 전용). 안에 하위 탭 세 장이 있다.
///   길드관리   : 이름/소개/공지/마크/가입방식 변경 + 해체
///   길드원관리 : 역할 변경(드롭다운) + 추방 + 길드장 위임
///   가입신청   : 신청 목록 승인/거절 (대기 건이 있으면 배지)
///
/// 요청/실패 처리와 새로고침은 부모 GuildPopup 의 공용 창구(RunAsync)를 그대로 쓴다.
/// 이 패널은 "무엇을 요청할지"만 정하고 결과 반영 규칙을 따로 갖지 않는다.
/// </summary>
public sealed class GuildManagePanel : MonoBehaviour {
    [Header("Sub Tabs")]
    [Tooltip("하위 탭 버튼(길드관리 / 길드원관리 / 가입신청 순).")]
    [SerializeField] private UITabButton[] subTabButtons;
    [SerializeField] private GameObject[] subTabPages;
    [Tooltip("가입신청 하위 탭 배지. 대기 건이 있으면 켜진다.")]
    [SerializeField] private BadgeView applicationBadge;

    [Header("길드관리")]
    [SerializeField] private TMP_InputField introInput;
    [SerializeField] private Button introSaveButton;
    [SerializeField] private TMP_Text introCountText;
    [SerializeField] private TMP_InputField noticeInput;
    [SerializeField] private Button noticeSaveButton;
    [SerializeField] private TMP_Text noticeCountText;
    [Tooltip("가입 방식 라디오 버튼(즉시 가입, 가입 신청 순).")]
    [SerializeField] private Toggle[] joinTypeToggles;
    [Tooltip("고른 가입 방식을 실제로 적용하는 저장 버튼. 고른 것이 지금 적용된 방식과 같으면 잠긴다.")]
    [SerializeField] private Button joinTypeSaveButton;
    [Header("이름 변경")]
    [Tooltip("새 길드 이름 입력칸. 중복확인을 통과해야 변경 버튼이 열린다.")]
    [SerializeField] private TMP_InputField renameInput;
    [SerializeField] private Button renameCheckButton;
    [Tooltip("실제 변경 버튼(다이아 소모).")]
    [SerializeField] private Button renameButton;
    [SerializeField] private TMP_Text renameHintText;
    [Tooltip("이름 변경 비용 표시.")]
    [SerializeField] private TMP_Text renameCostText;
    [Tooltip("비용 재화 아이콘. 스프라이트는 재화 카탈로그(CurrencyCatalog)에서 타입으로 가져오므로 직접 할당하지 않는다.")]
    [SerializeField] private Image renameCostIcon;
    [Tooltip("다이아가 부족할 때 비용 텍스트에 적용할 색.")]
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.20f, 0.20f, 1f);
    [SerializeField] private Color okColor = new Color(0.30f, 0.80f, 0.40f, 1f);
    [SerializeField] private Color errorColor = new Color(0.90f, 0.20f, 0.20f, 1f);

    [SerializeField] private Button disbandButton;
    [Tooltip("길드장 위임 버튼. 길드장만, 그리고 넘길 길드원이 있을 때만 눌린다.")]
    [SerializeField] private Button transferMasterButton;

    [Header("길드 마크")]
    [Tooltip("마크 선택 슬롯이 생성될 부모.")]
    [SerializeField] private Transform markRoot;
    [SerializeField] private GuildMarkSlot markSlotPrefab;
    [Tooltip("고른 마크를 실제로 적용하는 저장 버튼. 고른 것이 지금 적용된 마크와 같으면 잠긴다.")]
    [SerializeField] private Button markSaveButton;

    [Header("길드원관리")]
    [SerializeField] private Transform manageMemberRoot;
    [SerializeField] private GuildMemberSlot manageMemberSlotPrefab;

    [Header("가입신청")]
    [SerializeField] private Transform applicationRoot;
    [SerializeField] private GuildMemberSlot applicationSlotPrefab;
    [Tooltip("신청이 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyApplicationLabel;

    private static readonly GuildJoinType[] JoinTypes = { GuildJoinType.Instant, GuildJoinType.Application };

    private MainHudContext _ctx;
    private GuildPopup _owner;
    private GuildHomeDto _home;
    private Action _onChanged;
    private bool _wired;
    private bool _busy;
    private bool _nameChecked;    // 이름 변경: 중복확인을 통과했는지
    private string _checkedName = "";
    // 비용 텍스트의 평상시 색. 프리팹에서 정한 색을 그대로 쓰려고 첫 표시 때 한 번만 기억한다.
    private Color _renameCostDefaultColor = Color.white;
    private bool _renameCostColorCaptured;
    private CancellationToken _ct;
    private bool _ctCaptured;
    // 입력칸에 마지막으로 채워 넣은 서버 값. 사용자가 고치는 중인 글을 갱신이 덮어쓰지 않게 판단하는 기준이다.
    private string _appliedIntro = "";
    private string _appliedNotice = "";
    // 마크와 가입 방식도 같은 규칙. 고르는 것과 저장하는 것이 분리되어 있으므로
    // "고른 것"과 "서버에 적용된 것"을 따로 들고 있는다.
    private int _pendingMarkId;
    private int _appliedMarkId;
    private GuildJoinType _pendingJoinType;
    private GuildJoinType _appliedJoinType;

    private readonly List<GuildMemberSlot> _memberSlots = new();
    private readonly List<GuildMemberSlot> _applicationSlots = new();
    private readonly List<GuildMarkSlot> _markSlots = new();

    /// <summary>
    /// 파괴 취소 토큰. 파괴된 뒤에 처음 꺼내면 예외가 나므로 살아 있는 동안 한 번만 받아 두고 계속 쓴다.
    /// 이 패널은 자기 탭이 닫힌 채로 초기화될 수 있어 Awake 가 아직 안 불렸을 수 있으므로, 처음 쓰는 시점에 챙긴다.
    /// </summary>
    private CancellationToken Ct {
        get {
            if (!_ctCaptured) { _ct = this.GetCancellationTokenOnDestroy(); _ctCaptured = true; }
            return _ct;
        }
    }

    /// <summary>부모가 길드 정보를 받아올 때마다 호출한다.</summary>
    public void Initialize(MainHudContext ctx, GuildPopup owner, GuildHomeDto home, Action onChanged) {
        _ctx = ctx;
        _owner = owner;
        _home = home;
        _onChanged = onChanged;

        WireOnce();
        SyncPendingSelections();
        BuildSettings();
        BuildMembers();
        BuildMarks();
        RefreshApplicationBadge();
        LoadApplicationsAsync().Forget();
    }

    // 버튼/입력 리스너는 한 번만 건다(부모가 갱신할 때마다 다시 걸면 중복 호출된다).
    private void WireOnce() {
        if (_wired) return;
        _wired = true;

        if (introSaveButton != null)
            introSaveButton.onClick.AddListener(() => Request(GuildActionType.SetIntro, textValue: introInput != null ? introInput.text : ""));
        if (noticeSaveButton != null)
            noticeSaveButton.onClick.AddListener(() => Request(GuildActionType.SetNotice, textValue: noticeInput != null ? noticeInput.text : ""));
        if (renameCheckButton != null)
            renameCheckButton.onClick.AddListener(() => CheckNameAsync().Forget());
        if (renameButton != null)
            renameButton.onClick.AddListener(() => RenameAsync().Forget());
        if (renameInput != null)
            renameInput.onValueChanged.AddListener(OnRenameInputChanged);
        if (disbandButton != null)
            disbandButton.onClick.AddListener(() => DisbandAsync().Forget());
        if (transferMasterButton != null)
            transferMasterButton.onClick.AddListener(() => TransferMasterAsync().Forget());
        if (markSaveButton != null)
            markSaveButton.onClick.AddListener(() => Request(GuildActionType.SetMark, intValue: _pendingMarkId));
        if (joinTypeSaveButton != null)
            joinTypeSaveButton.onClick.AddListener(() => Request(GuildActionType.SetJoinType, intValue: (int)_pendingJoinType));

        if (introInput != null) introInput.onValueChanged.AddListener(_ => RefreshCounts());
        if (noticeInput != null) noticeInput.onValueChanged.AddListener(_ => RefreshCounts());

        // 다이아 보유량이 밖에서 바뀌어도(마크 해금/상점 구매 등) 비용 색과 버튼 활성이 즉시 따라오게 한다.
        _ctx.Store.Diamond.Subscribe(_ => OnDiamondChanged()).AddTo(this);

        if (joinTypeToggles != null) {
            for (int i = 0; i < joinTypeToggles.Length && i < JoinTypes.Length; i++) {
                if (joinTypeToggles[i] == null) continue;
                var type = JoinTypes[i];
                var toggle = joinTypeToggles[i];
                // 고르기만 하고 적용은 저장 버튼이 한다.
                toggle.onValueChanged.AddListener(on => {
                    if (!on) {
                        // ToggleGroup 이 붙어 있으면 이 알림은 "다른 토글이 켜지는 중"이라는 뜻이므로 손대지 않는다.
                        // 여기서 이 토글을 되살리면 그룹이 그 변경을 다시 전파해 정작 켜지던 토글을 꺼 버리고,
                        // 그 토글의 알림이 뒤늦게 off 로 도착해 선택이 아예 바뀌지 않는다.
                        // 그룹이 없을 때만, 켜져 있던 것을 다시 눌러 꺼진 경우를 되돌린다(하나는 반드시 켜져 있어야 한다).
                        if (toggle.group == null && _pendingJoinType == type) toggle.SetIsOnWithoutNotify(true);
                        return;
                    }
                    if (_pendingJoinType == type) return;
                    Click();
                    _pendingJoinType = type;
                    RefreshJoinTypes();
                });
            }
        }

        if (subTabButtons != null) {
            for (int i = 0; i < subTabButtons.Length; i++) {
                if (subTabButtons[i] == null) continue;
                int index = i;
                subTabButtons[i].Bind(null, () => { Click(); SelectSubTab(index); });
            }
            SelectSubTab(0);
        }
    }

    private void SelectSubTab(int index) {
        if (subTabButtons != null)
            for (int i = 0; i < subTabButtons.Length; i++)
                if (subTabButtons[i] != null) subTabButtons[i].SetSelected(i == index);
        if (subTabPages != null)
            for (int i = 0; i < subTabPages.Length; i++)
                if (subTabPages[i] != null) subTabPages[i].SetActive(i == index);
    }

    // ── 길드관리 ─────────────────────────────────────
    private void BuildSettings() {
        if (introInput != null) introInput.characterLimit = GuildRules.MaxIntroLength;
        if (noticeInput != null) noticeInput.characterLimit = GuildRules.MaxNoticeLength;
        ApplyServerText(introInput, _home.Summary.Intro, ref _appliedIntro);
        ApplyServerText(noticeInput, _home.Notice, ref _appliedNotice);
        RefreshCounts();

        CurrencyIcon.Apply(renameCostIcon, _ctx.Currencies, CurrencyType.Diamond);
        RefreshRenameCost();
        if (renameInput != null) renameInput.characterLimit = GuildRules.NameMaxWidth;
        RefreshRenameHint();

        RefreshJoinTypes();

        // 해체는 길드장만, 그리고 혼자 남았을 때만 시도할 수 있다(서버도 같은 규칙으로 막는다).
        if (disbandButton != null)
            disbandButton.interactable = _home.MyRole == GuildRole.Master && _home.Summary.MemberCount <= 1;

        // 위임은 해체와 정반대다. 넘길 사람이 있어야 하므로 나 혼자면 누를 수 없다.
        if (transferMasterButton != null)
            transferMasterButton.interactable = _home.MyRole == GuildRole.Master && _home.Summary.MemberCount > 1;
    }

    // 다이아 변동 알림. 아직 길드 정보를 못 받았거나 패널이 닫혀 있으면 할 일이 없다.
    private void OnDiamondChanged() {
        if (_home == null || !gameObject.activeInHierarchy) return;
        RefreshRenameCost();
        RefreshRenameButton();
        BuildMarks();   // 마크 가격 색/활성만 바뀌면 되지만, 목록이 작고 선택 상태는 서버 값에서 다시 온다.
    }

    /// <summary>
    /// 서버 값을 입력칸에 채운다. 단, 아직 저장하지 않은 편집이 들어 있으면 건드리지 않는다.
    /// 이 패널은 길드 정보를 다시 받을 때마다 통째로 갱신되는데(저장/승인/추방 등 어떤 작업이든 갱신을 부른다),
    /// 그때 무조건 덮어쓰면 왕복이 도는 동안 다른 칸에 쓰던 글이 사라진다.
    /// </summary>
    private static void ApplyServerText(TMP_InputField input, string serverValue, ref string applied) {
        string value = serverValue ?? "";
        if (input != null && input.text == applied) input.SetTextWithoutNotify(value);
        applied = value;
    }

    private void RefreshCounts() {
        if (introCountText != null && introInput != null)
            introCountText.text = $"{introInput.text.Length} / {GuildRules.MaxIntroLength}";
        if (noticeCountText != null && noticeInput != null)
            noticeCountText.text = $"{noticeInput.text.Length} / {GuildRules.MaxNoticeLength}";
    }

    /// <summary>
    /// 마크와 가입 방식처럼 "고르기 + 저장 버튼"으로 나뉜 항목을 서버 값에 맞춘다.
    /// 소개/공지 입력칸과 같은 규칙으로, 아직 저장하지 않은 선택이 있으면 갱신이 그것을 되돌리지 않는다
    /// (내가 고른 게 없으면 다른 임원이 바꾼 값을 그대로 따라온다).
    /// </summary>
    private void SyncPendingSelections() {
        int serverMarkId = _home.Summary.MarkId;
        if (_pendingMarkId == _appliedMarkId) _pendingMarkId = serverMarkId;
        _appliedMarkId = serverMarkId;

        var serverJoinType = _home.Summary.JoinType;
        if (_pendingJoinType == _appliedJoinType) _pendingJoinType = serverJoinType;
        _appliedJoinType = serverJoinType;
    }

    // 라디오 버튼은 고른 값 하나만 켜 둔다(ToggleGroup 없이도 서로 배타적으로 동작한다).
    private void RefreshJoinTypes() {
        if (joinTypeToggles != null)
            for (int i = 0; i < joinTypeToggles.Length && i < JoinTypes.Length; i++)
                if (joinTypeToggles[i] != null)
                    joinTypeToggles[i].SetIsOnWithoutNotify(_pendingJoinType == JoinTypes[i]);

        // 고른 방식이 지금 적용된 것과 다를 때만 저장할 것이 있다.
        if (joinTypeSaveButton != null)
            joinTypeSaveButton.interactable = _pendingJoinType != _appliedJoinType && !_busy;
    }

    private void BuildMarks() {
        foreach (var s in _markSlots) if (s != null) Destroy(s.gameObject);
        _markSlots.Clear();
        RefreshMarkSave();
        if (markRoot == null || markSlotPrefab == null || _owner == null || _owner.Marks == null) return;

        foreach (var entry in _owner.Marks.Entries) {
            if (entry == null) continue;
            bool unlocked = GuildRules.IsFreeMark(entry.markId) || _home.UnlockedMarkIds.Contains(entry.markId);
            var slot = Instantiate(markSlotPrefab, markRoot);
            // 강조는 "고른 것" 기준이고, 지금 길드에 적용된 마크는 따로 표시한다(저장 전후를 구분할 수 있어야 한다).
            slot.Bind(entry, _owner.Marks, _ctx.Currencies, unlocked, CanAfford(GuildRules.MarkUnlockCostDiamond),
                      entry.markId == _pendingMarkId, entry.markId == _appliedMarkId, OnMarkClicked);
            _markSlots.Add(slot);
        }
    }

    // 고른 마크가 지금 적용된 것과 다를 때만 저장할 것이 있다.
    private void RefreshMarkSave() {
        if (markSaveButton != null) markSaveButton.interactable = _pendingMarkId != _appliedMarkId && !_busy;
    }

    // 해금된 마크는 고르기만 하고(저장 버튼으로 적용), 잠긴 마크는 해금 확인을 먼저 받는다.
    private void OnMarkClicked(int markId, bool unlocked) {
        if (!unlocked) { UnlockMarkAsync(markId).Forget(); return; }
        Click();
        _pendingMarkId = markId;
        BuildMarks();
    }

    private async UniTaskVoid UnlockMarkAsync(int markId) {
        Click();
        bool ok = await _owner.ConfirmAsync("길드 마크 해금",
            $"다이아 {GuildRules.MarkUnlockCostDiamond:N0}을 사용해 해금하시겠습니까?");
        if (!ok) return;
        if (!await _owner.RunAsync(() => _ctx.Guild.ActAsync(GuildActionType.UnlockMark, intValue: markId, ct: Ct)))
            return;

        // 돈을 낸 마크는 바로 고른 상태로 둔다(적용은 저장 버튼이 한다).
        // 목록을 여기서 다시 그리지는 않는다. 아직 해금 결과가 반영되지 않은 정보라 자물쇠가 잠깐 남는데,
        // 곧 도착하는 길드 정보 갱신이 올바른 상태로 그려 준다.
        _pendingMarkId = markId;
        RefreshMarkSave();
    }

    // 이름 변경은 생성과 같은 규칙(중복확인 통과한 이름만 적용)을 쓴다. 이름을 고치면 확인 상태가 풀린다.
    private void OnRenameInputChanged(string value) {
        if (_nameChecked && value != _checkedName) _nameChecked = false;
        RefreshRenameHint();
    }

    private async UniTaskVoid CheckNameAsync() {
        if (_busy || renameInput == null) return;
        string name = renameInput.text.Trim();
        // 문자 종류/길이는 왕복 없이 여기서 걸러 준다. 중복과 금지어는 서버만 알 수 있어 물어봐야 한다.
        if (!GuildRules.IsValidName(name)) { ShowRenameHint(GuildRules.NameFormatMessage(name), false); return; }

        _busy = true;
        try {
            Click();
            int fail = await _ctx.Guild.CheckNameAsync(name, Ct);
            bool available = fail == 0;
            _nameChecked = available;
            _checkedName = available ? name : "";
            ShowRenameHint(available ? "사용할 수 있는 이름입니다." : GuildRules.FailMessage(fail), available);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            ShowRenameHint("확인에 실패했습니다.", false);
        } finally {
            _busy = false;
            RefreshRenameButton();
        }
    }

    private async UniTaskVoid RenameAsync() {
        if (!_nameChecked || _busy) return;
        Click();
        if (!await _owner.ConfirmAsync("길드 이름 변경",
            $"다이아 {GuildRules.RenameCostDiamond:N0}을 사용해 '{_checkedName}' 으로 변경하시겠습니까?")) return;

        bool ok = await _owner.RunAsync(() => _ctx.Guild.ActAsync(GuildActionType.Rename, textValue: _checkedName,
                                                                  ct: Ct));
        if (ok) {
            _nameChecked = false;
            _checkedName = "";
            if (renameInput != null) renameInput.SetTextWithoutNotify("");
        }
        RefreshRenameHint();
    }

    private void RefreshRenameHint() {
        if (_nameChecked) { ShowRenameHint("사용할 수 있는 이름입니다.", true); RefreshRenameButton(); return; }

        string name = renameInput != null ? renameInput.text.Trim() : "";
        if (name.Length == 0) ShowRenameHint(GuildRules.NameHint, true);
        else if (!GuildRules.IsValidName(name)) ShowRenameHint($"사용할 수 없는 이름입니다. ({GuildRules.NameHint})", false);
        else ShowRenameHint("중복확인이 필요합니다.", false);
        RefreshRenameButton();
    }

    private void ShowRenameHint(string message, bool ok) {
        if (renameHintText == null) return;
        renameHintText.text = message;
        renameHintText.color = ok ? okColor : errorColor;
    }

    // 다이아가 모자라면 비용을 빨갛게 물들이고 변경 버튼을 잠근다(서버도 같은 규칙으로 막지만,
    // 누를 수 없는 버튼을 보여 주지 않는 편이 낫다).
    private void RefreshRenameCost() {
        if (renameCostText == null) return;

        if (!_renameCostColorCaptured) { _renameCostDefaultColor = renameCostText.color; _renameCostColorCaptured = true; }
        renameCostText.text = GuildRules.RenameCostDiamond.ToString("N0");
        renameCostText.color = CanAfford(GuildRules.RenameCostDiamond) ? _renameCostDefaultColor : insufficientColor;
    }

    private bool CanAfford(int diamond) => _ctx != null && _ctx.Store.Diamond.Value >= diamond;

    private void RefreshRenameButton() {
        if (renameButton != null)
            renameButton.interactable = _nameChecked && !_busy && CanAfford(GuildRules.RenameCostDiamond);
    }

    private async UniTaskVoid DisbandAsync() {
        Click();
        bool ok = await _owner.ConfirmAsync("길드 해체",
            "길드를 해체하면 되돌릴 수 없습니다.\n정말 해체하시겠습니까?");
        if (!ok) return;
        await _owner.RunAsync(() => _ctx.Guild.ActAsync(GuildActionType.Disband, ct: Ct));
    }

    // 길드장 위임. 대상을 고르고 한 번 더 확인받는다.
    //
    // 확인을 두 번 받는 이유: 넘기는 순간 나는 평길드원이 되고, 되돌리려면 새 길드장이 다시 넘겨 줘야 한다.
    // 상대가 넘겨 주지 않으면 방법이 없으므로 해체와 같은 무게로 다룬다.
    private async UniTaskVoid TransferMasterAsync() {
        Click();

        var reference = _ctx.Popups.GuildTransferMasterPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<GuildTransferMasterPopup>(reference, Ct);
            int targetId = await popup.ShowAsync(_ctx, _home.Members);
            if (targetId == 0) return;   // 고르지 않고 닫았다

            string nickname = NicknameOf(targetId);
            bool ok = await _owner.ConfirmAsync("길드장 위임",
                $"{nickname} 님에게 길드장을 넘깁니다.\n나는 길드원이 되며 되돌릴 수 없습니다.\n정말 위임하시겠습니까?");
            if (!ok) return;

            await _owner.RunAsync(() => _ctx.Guild.ActAsync(GuildActionType.TransferMaster,
                                                           targetAccountId: targetId, ct: Ct));
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // ── 길드원관리 ───────────────────────────────────
    private void BuildMembers() {
        foreach (var s in _memberSlots) if (s != null) Destroy(s.gameObject);
        _memberSlots.Clear();
        if (manageMemberRoot == null || manageMemberSlotPrefab == null) return;

        foreach (var m in _home.Members) {
            var slot = Instantiate(manageMemberSlotPrefab, manageMemberRoot);
            slot.BindManage(m, _ctx, _home.MyRole, _owner.OpenUserProfile, OnRoleChange, OnKick);
            _memberSlots.Add(slot);
        }
    }

    private void OnRoleChange(int accountId, GuildRole role) =>
        Request(GuildActionType.ChangeRole, targetAccountId: accountId, intValue: (int)role);

    private void OnKick(int accountId) => KickAsync(accountId).Forget();

    private async UniTaskVoid KickAsync(int accountId) {
        Click();
        string nickname = NicknameOf(accountId);
        if (!await _owner.ConfirmAsync("길드원 추방", $"{nickname} 님을 추방하시겠습니까?")) return;
        await _owner.RunAsync(() => _ctx.Guild.ActAsync(GuildActionType.Kick, targetAccountId: accountId,
                                                        ct: Ct));
    }

    // ── 가입신청 ─────────────────────────────────────
    private async UniTaskVoid LoadApplicationsAsync() {
        if (!_home.CanHandleApplications) return;
        try {
            var list = await _ctx.Guild.RequestApplicationsAsync(Ct);
            BuildApplications(list);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void BuildApplications(List<GuildApplicationDto> list) {
        foreach (var s in _applicationSlots) if (s != null) Destroy(s.gameObject);
        _applicationSlots.Clear();

        if (applicationRoot != null && applicationSlotPrefab != null) {
            foreach (var a in list) {
                var slot = Instantiate(applicationSlotPrefab, applicationRoot);
                slot.BindApplication(a, _ctx, _owner.OpenUserProfile, OnApprove, OnReject);
                _applicationSlots.Add(slot);
            }
        }
        if (emptyApplicationLabel != null) emptyApplicationLabel.SetActive(list.Count == 0);
        if (applicationBadge != null) applicationBadge.SetOn(list.Count > 0);
    }

    private void OnApprove(int accountId) => Request(GuildActionType.Approve, targetAccountId: accountId);
    private void OnReject(int accountId) => Request(GuildActionType.Reject, targetAccountId: accountId);

    private void RefreshApplicationBadge() {
        if (applicationBadge != null) applicationBadge.SetOn(_home.PendingApplications > 0);
    }

    // ── 공통 ─────────────────────────────────────────
    // 모든 변경 요청은 부모의 공용 창구를 거친다(성공 시 화면 전체 갱신 + 실패 문구가 한 곳에서 처리된다).
    private void Request(GuildActionType action, int targetAccountId = 0, int intValue = 0, string textValue = "") {
        Click();
        _owner.RunAsync(() => _ctx.Guild.ActAsync(action, targetAccountId: targetAccountId, intValue: intValue,
                                                  textValue: textValue, ct: Ct)).Forget();
    }

    private string NicknameOf(int accountId) {
        foreach (var m in _home.Members) if (m.AccountId == accountId) return m.Nickname;
        return "해당 길드원";
    }

    private void Click() => _ctx?.Sound.PlaySfx(SfxKey.ButtonClick);
}
