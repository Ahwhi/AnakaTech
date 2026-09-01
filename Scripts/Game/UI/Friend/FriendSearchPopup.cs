using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 친구 검색 팝업. 닉네임으로 유저를 찾아 그 자리에서 친구 신청을 보낸다.
///
/// 친구 목록은 이미 친구인 사람만 보여 주므로, 이 화면이 없으면 원하는 상대에게 신청할 방법이 아예 없다.
/// 목록 한 줄은 친구 탭과 같은 FriendSlot 을 쓴다(같은 사람을 두 화면에서 다르게 그리지 않기 위해서다).
///
/// 이미 친구이거나 신청 중인 상대도 결과에 남긴다. 걸러 내면 "분명 있는 사람인데 검색이 안 된다"가 되어
/// 닉네임을 잘못 안 것인지 이미 친구인 것인지 유저가 구분할 수 없다. 대신 그 줄은 신청 버튼 대신 상태를 보여 준다.
///
/// 신청을 보낸 뒤에는 목록을 다시 받지 않고 그 줄만 상태를 바꾼다. 다시 받으면 스크롤이 처음으로 돌아가
/// 여러 명에게 이어서 신청하는 흐름이 끊긴다.
/// </summary>
public sealed class FriendSearchPopup : UIView, IHudPopup {
    [Header("Search")]
    [Tooltip("닉네임 입력 칸. 엔터로도 검색된다.")]
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button searchButton;

    [Header("Result")]
    [Tooltip("검색 결과 줄이 생성될 부모.")]
    [SerializeField] private Transform resultRoot;
    [Tooltip("결과 한 줄 프리팹. 친구 탭과 같은 프리팹을 쓰되 신청 버튼/상태 문구가 배선되어 있어야 한다.")]
    [SerializeField] private FriendSlot resultSlotPrefab;
    [Tooltip("검색 결과가 없을 때 켜는 안내. 선택.")]
    [SerializeField] private GameObject emptyLabel;
    [Tooltip("아직 한 번도 검색하지 않았을 때 켜는 안내(검색어를 입력하세요 등). 선택.")]
    [SerializeField] private GameObject guideLabel;

    [Header("Profile")]
    [Tooltip("결과 줄을 눌러 여는 프로필 팝업에 넘길 길드 마크 카탈로그. 친구 팝업과 같은 값을 넣는다.")]
    [SerializeField] private GuildMarkCatalog markCatalog;

    [Header("Common")]
    [SerializeField] private GameObject loadingGroup;
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private bool _busy;
    private bool _searched;

    private readonly List<FriendSlot> _slots = new();
    // 지금 화면에 떠 있는 결과. 신청 뒤 그 줄만 다시 그리기 위해 들고 있는다.
    private readonly List<FriendSearchEntry> _entries = new();

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { Click(); CloseAsync().Forget(); });
        }
        if (searchButton != null) {
            searchButton.onClick.RemoveAllListeners();
            searchButton.onClick.AddListener(Search);
        }
        // 모바일 키보드의 완료/엔터로도 검색되게 한다(버튼을 다시 찾아 누르게 하지 않는다).
        if (nicknameInput != null) {
            nicknameInput.onSubmit.RemoveAllListeners();
            nicknameInput.onSubmit.AddListener(_ => Search());
        }

        Build();
    }

    // ── 검색 ──────────────────────────────────────────
    private void Search() {
        if (_busy) return;

        string keyword = (nicknameInput != null ? nicknameInput.text : "").Trim();
        if (keyword.Length < FriendSearchRules.MinLength) {
            _ctx.Ui.ShowToast(FriendSearchRules.FailMessage(1));
            return;
        }

        Click();
        SearchAsync(keyword).Forget();
    }

    private async UniTaskVoid SearchAsync(string keyword) {
        _busy = true;
        SetLoading(true);
        try {
            var res = await _ctx.Friend.SearchAsync(keyword, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(FriendSearchRules.FailMessage(res.FailReason));
                return;
            }

            _entries.Clear();
            _entries.AddRange(res.Users);
            _searched = true;
            Build();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("검색에 실패했습니다.");
        } finally {
            _busy = false;
            SetLoading(false);
        }
    }

    private void Build() {
        ClearSlots();

        if (guideLabel != null) guideLabel.SetActive(!_searched);
        if (emptyLabel != null) emptyLabel.SetActive(_searched && _entries.Count == 0);
        if (resultSlotPrefab == null || resultRoot == null) return;

        foreach (var entry in _entries) {
            var slot = Instantiate(resultSlotPrefab, resultRoot);
            slot.BindSearch(entry, _ctx, OpenUserProfile, RequestFriend);
            _slots.Add(slot);
        }
    }

    // ── 친구 신청 ─────────────────────────────────────
    private void RequestFriend(int accountId) {
        if (_busy) return;
        Click();
        RequestFriendAsync(accountId).Forget();
    }

    private async UniTaskVoid RequestFriendAsync(int accountId) {
        _busy = true;
        try {
            var res = await _ctx.Friend.ActAsync(FriendActionType.Request, accountId, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(FriendRules.FailMessage(res.FailReason));
                return;
            }

            _ctx.Ui.ShowToast("친구 신청을 보냈습니다.");
            // 서버가 확정한 관계로 그 줄만 바꾼다(목록을 다시 받으면 스크롤이 처음으로 돌아간다).
            ApplyRelation(accountId, res.Relation, res.Blocked);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("친구 신청에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    // 해당 계정의 줄만 새 관계로 다시 그린다. 슬롯과 결과 목록은 순서가 같으므로 인덱스로 짝지을 수 있다.
    private void ApplyRelation(int accountId, FriendRelation relation, bool blocked) {
        for (int i = 0; i < _entries.Count; i++) {
            if (_entries[i].User.AccountId != accountId) continue;

            _entries[i].Relation = relation;
            _entries[i].Blocked = blocked;
            if (i < _slots.Count && _slots[i] != null)
                _slots[i].BindSearch(_entries[i], _ctx, OpenUserProfile, RequestFriend);
            return;
        }
    }

    // ── 프로필 ────────────────────────────────────────
    private void OpenUserProfile(int accountId) {
        Click();
        OpenUserProfileAsync(accountId).Forget();
    }

    private async UniTaskVoid OpenUserProfileAsync(int accountId) {
        var reference = _ctx.Popups.UserProfilePopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<UserProfilePopup>(reference, _ct);
            // 프로필 팝업에서 관계를 바꿨을 수 있다(거기서도 신청/차단이 가능하다). 그 결과를 결과 줄에 반영한다.
            bool changed = await popup.ShowAsync(_ctx, accountId, markCatalog);
            if (changed && !string.IsNullOrEmpty(nicknameInput != null ? nicknameInput.text : null))
                SearchAsync(nicknameInput.text.Trim()).Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // ── 표시 헬퍼 ────────────────────────────────────
    private void SetLoading(bool on) {
        if (loadingGroup != null) loadingGroup.SetActive(on);
        if (searchButton != null) searchButton.interactable = !on;
    }

    private void ClearSlots() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
    }

    private void Click() => _ctx?.Sound.PlaySfx(SfxKey.ButtonClick);

    protected override void OnClosed() => ClearSlots();
}
