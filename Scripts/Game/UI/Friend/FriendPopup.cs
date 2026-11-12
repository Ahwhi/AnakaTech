using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UniRx;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 친구 팝업. 메인 화면의 친구 버튼(HudMenuButton)에서 연다.
///
/// 탭 세 개로 나뉘며 내용은 서버 응답 하나(FriendHomeDto)가 모두 담고 있어 진입 시 왕복이 한 번이면 된다.
///   0 친구 목록   : 누르면 프로필 팝업(거기서 친구 삭제/차단)
///   1 친구 신청   : 내가 받은 신청. 수락/거절
///   2 차단 유저   : 차단 해제
///
/// 상태가 바뀌는 작업(수락/거절/해제 등)은 결과를 받은 뒤 전체를 다시 받아 화면을 갱신한다
/// (부분 갱신을 손으로 맞추면 목록/인원수/배지가 어긋난다. 길드 팝업과 같은 방침이다).
/// 배지는 BadgeCenter 가 단일 소스라, 신청 탭 배지도 메인 친구 버튼 배지도 여기서 따로 켜고 끄지 않는다.
/// </summary>
public sealed class FriendPopup : UIView, IHudPopup, IHudPopupTab {
    /// <summary>친구 신청 탭 인덱스. 배지를 보고 들어온 유저를 그 탭으로 바로 보낼 때 쓴다.</summary>
    public const int RequestTabIndex = 1;

    [Header("Tabs")]
    [Tooltip("탭 버튼(친구 목록 / 친구 신청 / 차단 유저 순).")]
    [SerializeField] private UITabButton[] tabButtons;
    [Tooltip("탭 페이지(탭 버튼과 같은 순서).")]
    [SerializeField] private GameObject[] tabPages;

    [Header("Friends")]
    [SerializeField] private Transform friendRoot;
    [Tooltip("친구 한 줄 프리팹(프로필 테두리 + 대표 캐릭터 일러 + 닉네임).")]
    [SerializeField] private FriendSlot friendSlotPrefab;
    [Tooltip("선택. 친구 수 표시(n / 20). 정원은 서버가 내려준 값을 쓴다.")]
    [SerializeField] private TMP_Text friendCountText;
    [Tooltip("선택. 친구가 하나도 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyFriendLabel;

    [Header("Requests")]
    [SerializeField] private Transform requestRoot;
    [Tooltip("선택. 수락/거절 버튼이 달린 프리팹. 비우면 친구 프리팹을 쓴다.")]
    [SerializeField] private FriendSlot requestSlotPrefab;
    [Tooltip("선택. 받은 신청이 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyRequestLabel;

    [Header("Blocked")]
    [SerializeField] private Transform blockedRoot;
    [Tooltip("선택. 차단 해제 버튼이 달린 프리팹. 비우면 친구 프리팹을 쓴다.")]
    [SerializeField] private FriendSlot blockedSlotPrefab;
    [Tooltip("선택. 차단한 유저가 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyBlockedLabel;

    [Header("Stamina Gift")]
    [Tooltip("기력 선물 버튼(보내기/받기 겸용). 보낼 친구가 남아 있으면 일괄 전송, 다 보냈으면 기력 받기.")]
    [SerializeField] private Button staminaButton;
    [Tooltip("받을 기력 숫자. 받을 수 있을 때만 켠다.")]
    [SerializeField] private TMP_Text staminaAmountText;
    [Tooltip("버튼 문구(일괄 전송 / 기력 받기).")]
    [SerializeField] private TMP_Text staminaLabelText;
    [Tooltip("버튼의 알림 배지(BadgeView, Source=Manual). 보낼 친구나 받을 기력이 있으면 켠다.")]
    [SerializeField] private BadgeView staminaBadge;

    [Header("Catalog")]
    [Tooltip("길드 마크 카탈로그. 슬롯에서 여는 프로필 팝업이 상대의 길드 마크를 그리는 데 쓴다.")]
    [SerializeField] private GuildMarkCatalog markCatalog;

    [Header("Loading")]
    [SerializeField] private GameObject loadingGroup;

    [Tooltip("친구 검색 팝업 열기. 목록에는 이미 친구인 사람만 있어, 새 친구를 찾으려면 이 경로가 필요하다.")]
    [SerializeField] private Button searchButton;
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private FriendHomeDto _home;
    private bool _busy;
    private IDisposable _staminaSub;

    private readonly List<FriendSlot> _slots = new();

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { Click(); CloseAsync().Forget(); });
        }

        if (searchButton != null) {
            searchButton.onClick.RemoveAllListeners();
            searchButton.onClick.AddListener(OpenSearch);
        }

        if (staminaButton != null) {
            staminaButton.onClick.RemoveAllListeners();
            staminaButton.onClick.AddListener(() => { Click(); StaminaAsync().Forget(); });
        }
        // 기력 선물 상태는 서비스가 단일 소스다(목록 조회/보내기/받기 응답/친구의 선물 푸시가 모두 이 값을 갱신한다).
        _staminaSub?.Dispose();
        _staminaSub = _ctx.Friend.StaminaState.Subscribe(RefreshStamina);

        BindTabs();
        ReloadAsync().Forget();
    }

    /// <summary>친구 검색 팝업을 연다. 닫히면 목록을 다시 받는다(거기서 신청을 보냈을 수 있다).</summary>
    private void OpenSearch() {
        Click();
        OpenSearchAsync().Forget();
    }

    private async UniTaskVoid OpenSearchAsync() {
        var reference = _ctx.Popups.FriendSearchPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) {
            _ctx.Ui.ShowToast("검색 화면을 열 수 없습니다.");
            return;
        }
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<FriendSearchPopup>(reference, _ct);
            popup.Initialize(_ctx);
            await popup.ShowAsync();
            await popup.WaitUntilClosedAsync();

            // 검색 화면에서 신청을 보냈으면 "보낸 신청"이 늘어난다. 목록을 최신 상태로 되돌린다.
            ReloadAsync().Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // ── 로드 / 표시 ───────────────────────────────────
    /// <summary>서버 상태를 다시 받아 세 탭을 모두 구성한다. 상태가 바뀌는 작업 뒤에는 항상 이걸 부른다.</summary>
    private async UniTaskVoid ReloadAsync() {
        SetLoading(true);
        try {
            _home = await _ctx.Friend.RequestHomeAsync(_ct);
            Build();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("친구 정보를 불러오지 못했습니다.");
        } finally {
            SetLoading(false);
        }
    }

    private void Build() {
        if (_home == null) return;
        ClearSlots();

        BuildFriends();
        BuildRequests();
        BuildBlocked();
    }

    private void BuildFriends() {
        if (friendCountText != null) {
            int max = _home.MaxFriends > 0 ? _home.MaxFriends : FriendRules.MaxFriends;
            friendCountText.text = $"{_home.Friends.Count} / {max}";
        }
        if (emptyFriendLabel != null) emptyFriendLabel.SetActive(_home.Friends.Count == 0);

        if (friendRoot == null || friendSlotPrefab == null) return;
        foreach (var f in _home.Friends) {
            var slot = Instantiate(friendSlotPrefab, friendRoot);
            slot.BindFriend(f, _ctx, OpenUserProfile);
            _slots.Add(slot);
        }
    }

    private void BuildRequests() {
        if (emptyRequestLabel != null) emptyRequestLabel.SetActive(_home.Requests.Count == 0);

        var prefab = requestSlotPrefab != null ? requestSlotPrefab : friendSlotPrefab;
        if (requestRoot == null || prefab == null) return;
        foreach (var r in _home.Requests) {
            var slot = Instantiate(prefab, requestRoot);
            slot.BindRequest(r, _ctx, OpenUserProfile,
                             id => Act(FriendActionType.Accept, id),
                             id => Act(FriendActionType.Decline, id));
            _slots.Add(slot);
        }
    }

    private void BuildBlocked() {
        if (emptyBlockedLabel != null) emptyBlockedLabel.SetActive(_home.Blocked.Count == 0);

        var prefab = blockedSlotPrefab != null ? blockedSlotPrefab : friendSlotPrefab;
        if (blockedRoot == null || prefab == null) return;
        foreach (var b in _home.Blocked) {
            var slot = Instantiate(prefab, blockedRoot);
            slot.BindBlocked(b, _ctx, OpenUserProfile, id => Act(FriendActionType.Unblock, id));
            _slots.Add(slot);
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

    /// <summary>지정한 탭을 연다(IHudPopupTab).</summary>
    public void SelectTab(int index) {
        if (tabButtons == null || index < 0 || index >= tabButtons.Length) return;
        for (int i = 0; i < tabButtons.Length; i++)
            if (tabButtons[i] != null) tabButtons[i].SetSelected(i == index);
        if (tabPages != null)
            for (int i = 0; i < tabPages.Length; i++)
                if (tabPages[i] != null) tabPages[i].SetActive(i == index);
    }

    // ── 동작 ─────────────────────────────────────────
    private void Act(FriendActionType action, int targetAccountId) {
        Click();
        ActAsync(action, targetAccountId).Forget();
    }

    private async UniTaskVoid ActAsync(FriendActionType action, int targetAccountId) {
        // 앞선 요청이 아직 안 끝났다. 조용히 무시하면 눌렀는데 아무 일도 안 일어난 것처럼 보이므로 이유를 알린다.
        if (_busy) { _ctx.Ui.ShowToast("처리 중입니다. 잠시 후 다시 시도해 주세요."); return; }
        _busy = true;
        try {
            var res = await _ctx.Friend.ActAsync(action, targetAccountId, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(FriendRules.FailMessage(res.FailReason));
                return;
            }
            ReloadAsync().Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("요청을 처리하지 못했습니다.");
        } finally {
            _busy = false;
        }
    }

    // ── 기력 선물 ────────────────────────────────────
    // 보낼 친구가 남아 있으면 보내기가 우선이다. 다 보낸 뒤 받을 기력이 있으면 받기, 둘 다 없으면 버튼만 막는다.
    private void RefreshStamina(FriendStaminaState state) {
        if (staminaLabelText != null) staminaLabelText.text = FriendRules.StaminaButtonLabel(state);
        if (staminaAmountText != null) {
            staminaAmountText.gameObject.SetActive(state.CanClaim);
            staminaAmountText.text = state.CanClaim ? state.Claimable.ToString("N0") : "";
        }
        if (staminaButton != null) staminaButton.interactable = state.HasAction && !_busy;
        if (staminaBadge != null) staminaBadge.SetOn(state.CanSend || state.CanClaim);
    }

    private async UniTaskVoid StaminaAsync() {
        if (_busy) { _ctx.Ui.ShowToast("처리 중입니다. 잠시 후 다시 시도해 주세요."); return; }
        var state = _ctx.Friend.StaminaState.Value;
        if (!state.HasAction) return;

        _busy = true;
        RefreshStamina(state);
        try {
            if (state.CanSend) {
                var res = await _ctx.Friend.SendStaminaAsync(_ct);
                if (!res.IsSuccess) { _ctx.Ui.ShowToast(FriendRules.FailMessage(res.FailReason)); return; }
                _ctx.Ui.ShowToast($"친구 {res.Amount}명에게 기력을 보냈습니다.");
            } else {
                var res = await _ctx.Friend.ClaimStaminaAsync(_ct);
                if (!res.IsSuccess) { _ctx.Ui.ShowToast(FriendRules.FailMessage(res.FailReason)); return; }
                _ctx.Store.SetStamina(res.NewStamina);
                _ctx.Sound.PlaySfx(SfxKey.Claim);
                _ctx.Ui.ShowToast($"기력 {res.Amount:N0}을 받았습니다.");
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("요청을 처리하지 못했습니다.");
        } finally {
            _busy = false;
            RefreshStamina(_ctx.Friend.StaminaState.Value);
        }
    }

    /// <summary>슬롯을 눌렀을 때 그 유저의 프로필 팝업을 연다(친구 삭제/차단은 거기서 한다).</summary>
    private void OpenUserProfile(int accountId) {
        Click();
        OpenUserProfileAsync(accountId).Forget();
    }

    private async UniTaskVoid OpenUserProfileAsync(int accountId) {
        var reference = _ctx.Popups.UserProfilePopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<UserProfilePopup>(reference, _ct);
            // 프로필 팝업에서 관계를 바꿨으면(친구 삭제/차단 등) 목록이 이미 달라졌으므로 다시 받아 온다.
            bool changed = await popup.ShowAsync(_ctx, accountId, markCatalog);
            if (changed) ReloadAsync().Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // ── 표시 헬퍼 ────────────────────────────────────
    private void SetLoading(bool on) {
        if (loadingGroup != null) loadingGroup.SetActive(on);
    }

    private void ClearSlots() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
    }

    private void Click() => _ctx?.Sound.PlaySfx(SfxKey.ButtonClick);

    protected override void OnClosed() {
        ClearSlots();
        _staminaSub?.Dispose();
        _staminaSub = null;
    }
}
