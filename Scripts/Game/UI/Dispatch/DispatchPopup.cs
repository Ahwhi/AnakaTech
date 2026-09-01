using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 파견 팝업. 보유 소녀 전체 목록과 파견 슬롯 5칸을 함께 두고, 목록에서 카드를 끌어다 슬롯의 자리에 놓아 편성한다.
///
/// 덱과는 완전히 별개다. 이 화면은 덱을 읽지도 쓰지도 않으므로 같은 소녀를 덱과 파견에 동시에 쓸 수 있고,
/// 파견을 보내도 덱 편성은 변하지 않는다. 대신 파견 안에서는 한 소녀가 한 자리에만 들어간다
/// (이미 어느 자리에 들어와 있는 소녀는 목록에서 흐리게 표시되고 다시 집을 수 없다).
///
/// 조작 규칙(편성 화면과 같은 규약이라 부품도 그대로 쓴다):
///   목록 카드 -> 빈 자리    : 편성
///   목록 카드 -> 찬 자리    : 그 자리의 소녀와 교체
///   자리     -> 다른 자리   : 자리 맞바꿈(다른 파견 슬롯으로도 옮겨진다)
///   자리     -> 자리 밖     : 편성 해제
///
/// 조건 판정과 보상 지급은 서버가 한다. 화면의 [파견 조건 만족] 표시는 같은 규칙(DispatchRules)으로
/// 미리 보여 주는 안내일 뿐이고, 보내기 요청은 서버가 다시 확인한다.
/// </summary>
public sealed class DispatchPopup : UIView, IHudPopup, CompositionDragHandle.IHost {
    [Header("Roster (보유 소녀 목록)")]
    [Tooltip("카드가 생성될 스크롤뷰 콘텐츠.")]
    [SerializeField] private Transform rosterContent;
    [Tooltip("보유 소녀 카드 프리팹. 슬롯 자리에도 같은 프리팹을 넣어야 집은 카드가 그대로 보인다.")]
    [SerializeField] private DeckCharacterCard rosterCardPrefab;

    [Header("Slots")]
    [Tooltip("파견 슬롯 5칸. 배열 순서가 곧 슬롯 번호다.")]
    [SerializeField] private DispatchSlotView[] slots;

    [Header("Header")]
    [Tooltip("남은 파견 갱신 횟수. 'N/N' 형태로만 표기한다(라벨은 옆에 따로 배치).")]
    [SerializeField] private TMP_Text refreshChargeText;
    [Tooltip("갱신 횟수가 남아 있을 때의 색.")]
    [SerializeField] private Color refreshAvailableColor = new Color(1f, 0.84f, 0.29f);
    [Tooltip("갱신 횟수가 0일 때의 색.")]
    [SerializeField] private Color refreshEmptyColor = Color.gray;

    [Header("Drag")]
    [Tooltip("드래그 중인 카드 복제본이 올라갈 레이어. 슬롯/목록보다 위에 그려지도록 하이어라키 마지막에 두고, " +
             "Raycast Target 이 꺼진 빈 RectTransform 으로 만든다. 비우면 이 팝업 루트를 쓴다.")]
    [SerializeField] private RectTransform dragLayer;
    [Range(0.2f, 1f)]
    [SerializeField] private float dragGhostAlpha = 0.8f;
    [SerializeField] private float dragGhostScale = 1.05f;

    [Tooltip("재화 패널(파견 증명서 등). 다른 팝업과 같은 공용 부품이라 배치만 하면 스스로 갱신된다. 선택.")]
    [SerializeField] private CurrencyPanel currencyPanel;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private MainHudContext _hud;
    private CancellationToken _ct;
    private DispatchInfoDto _info;
    private bool _wired;
    private bool _busy;

    // 편집 중인 편성. [파견슬롯][자리] = 캐릭터 id(0 = 빈 자리).
    // 서버가 준 상태를 그대로 두고 편집분만 여기에 담아, 보내기 전까지는 서버에 아무것도 반영되지 않는다.
    private int[][] _members;

    private readonly List<DeckCharacterCard> _rosterCards = new();

    // 드래그 상태(편성 화면과 같은 구조).
    private CompositionDragHandle _dragging;
    private GameObject _ghost;
    private RectTransform _ghostRt;
    private CompositionSlotView _hoveredSlot;
    private readonly List<RaycastResult> _raycastBuffer = new();

    private int MembersPerSlot => _info != null && _info.MembersPerSlot > 0 ? _info.MembersPerSlot : 5;

    public void Initialize(MainHudContext hud) {
        _hud = hud;
        _ct = this.GetCancellationTokenOnDestroy();

        if (!_wired) {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });

            if (slots != null) {
                for (int i = 0; i < slots.Length; i++) {
                    if (slots[i] == null) continue;
                    slots[i].SendRequested += OnSendRequested;
                    slots[i].RefreshRequested += OnRefreshRequested;
                    slots[i].ClaimRequested += OnClaimRequested;
                }
            }
            // 서버가 파견 상태를 바꿔 알려 오면(운영 도구/개발 명령) 열려 있는 화면도 그 자리에서 따라온다.
            _hud.Dispatch.ChangedByServer.Subscribe(_ => LoadAsync().Forget()).AddTo(this);
            _wired = true;
        }

        if (currencyPanel != null) currencyPanel.Initialize(hud);

        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        using (_hud.Ui.ShowLoading()) {
            try {
                _info = await _hud.Dispatch.RequestInfoAsync(_ct);
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
                _hud.Ui.ShowToast("파견 정보를 불러오지 못했습니다.");
                return;
            }
        }

        InitializeSlots();
        AdoptServerMembers();
        BuildRoster();   // 목록은 진입 시 1회만 만든다(이후 편집은 상태만 갈아 끼운다).
        RefreshAll();
    }

    private void InitializeSlots() {
        if (slots == null) return;
        _members = new int[slots.Length][];
        for (int i = 0; i < slots.Length; i++) {
            _members[i] = new int[MembersPerSlot];
            if (slots[i] != null) slots[i].Initialize(i, MembersPerSlot);
        }
    }

    // 서버가 알려 준 "파견 중인 편성"을 편집 버퍼에 옮긴다. 대기 중인 슬롯은 비워 둔다
    // (편성은 보내기 전까지 서버에 남지 않으므로, 팝업을 다시 열면 대기 슬롯은 빈 상태가 정상이다).
    private void AdoptServerMembers() {
        if (_info == null || _members == null) return;
        foreach (var slot in _info.Slots) {
            if (slot.SlotIndex < 0 || slot.SlotIndex >= _members.Length) continue;
            var row = _members[slot.SlotIndex];
            Array.Clear(row, 0, row.Length);
            for (int i = 0; i < row.Length && i < slot.Members.Count; i++) row[i] = slot.Members[i];
        }
    }

    // ── 표시 갱신 ────────────────────────────────────────
    private void RefreshAll() {
        RefreshHeader();
        RefreshSlots();
        RefreshRosterState();
    }

    private void RefreshHeader() {
        if (refreshChargeText == null || _info == null) return;
        // 라벨은 프리팹에서 옆에 따로 두므로 여기서는 숫자만 만든다.
        refreshChargeText.text = $"{_info.RefreshCharges}/{_info.MaxRefreshCharges}";
        refreshChargeText.color = _info.RefreshCharges > 0 ? refreshAvailableColor : refreshEmptyColor;
    }

    private void RefreshSlots() {
        if (slots == null || _info == null) return;
        long now = KstTime.NowUnix;

        for (int i = 0; i < slots.Length; i++) {
            var view = slots[i];
            if (view == null) continue;

            var state = SlotState(i);
            var condition = state != null ? _info.Condition(state.ConditionId) : null;
            bool running = state != null && state.IsRunning;
            bool finished = state != null && state.IsFinished(now);
            var conditionState = DispatchRules.StateOf(condition, _members[i], _hud.Store, MembersPerSlot);
            bool met = !running && conditionState == DispatchRules.ConditionState.Met;

            view.BindCondition(DispatchRules.ConditionText(condition), conditionState,
                               DispatchRules.Progress(condition, _members[i], _hud.Store),
                               condition?.Threshold ?? 0, showState: !running);
            view.BindRewards(_hud, state != null ? _info.RewardsFor(state.ConditionId) : null);
            view.BindState(running, finished, state?.EndUnix ?? 0,
                           canSend: met && !_busy,
                           canRefresh: !running && _info.RefreshCharges > 0 && !_busy);

            for (int m = 0; m < MembersPerSlot; m++)
                view.BindMember(m, _hud, _members[i][m], this,
                                DispatchSlotView.PositionOf(i, m, MembersPerSlot), draggable: !running);
        }
    }

    private DispatchSlotDto SlotState(int slotIndex) {
        if (_info == null) return null;
        foreach (var s in _info.Slots) if (s.SlotIndex == slotIndex) return s;
        return null;
    }

    // 보유 소녀 목록을 한 번만 만든다. 편성을 만질 때마다 다시 만들면 스크롤 위치가 처음으로 튀고
    // 드래그 중이던 카드가 파괴되므로, 생성은 진입 시 1회이고 이후에는 상태만 갈아 끼운다.
    private void BuildRoster() {
        foreach (var c in _rosterCards) if (c != null) Destroy(c.gameObject);
        _rosterCards.Clear();

        if (rosterContent == null || rosterCardPrefab == null || _hud.CharDb == null) return;

        var owned = new List<CharacterData>();
        foreach (var data in _hud.CharDb.All) {
            if (data == null) continue;
            // 보유하지 않은 소녀는 목록에 올리지 않는다(진행도가 없으면 미보유다).
            if (_hud.Store.GetProgress(data.Id) == null) continue;
            owned.Add(data);
        }
        owned.Sort(Compare);

        foreach (var data in owned) {
            var card = Instantiate(rosterCardPrefab, rosterContent);
            card.Bind(_hud.Appearance.Mine(data), _hud.Store.GetProgress(data.Id), null);
            card.SetUpgradeBadge(false);
            card.SetGuideTarget(false);
            _rosterCards.Add(card);
        }
        RefreshRosterState();
    }

    // 덱 화면과 같은 정렬 기준(등급 높은 순 -> 레벨 높은 순 -> id)을 쓴다. 두 화면에서 같은 소녀가
    // 다른 자리에 있으면 찾는 감각이 매번 어긋난다. 덱의 "대표/편성 먼저"는 덱에만 있는 축이라 빼고,
    // 파견의 배치 여부도 정렬에 넣지 않는다(편성을 만질 때마다 목록이 뒤바뀌면 집던 카드를 놓친다).
    private int Compare(CharacterData a, CharacterData b) {
        int gradeCmp = b.Grade.CompareTo(a.Grade);           // 높은 등급 먼저
        if (gradeCmp != 0) return gradeCmp;

        int la = _hud.Store.GetProgress(a.Id)?.Level ?? 0;
        int lb = _hud.Store.GetProgress(b.Id)?.Level ?? 0;
        int levelCmp = lb.CompareTo(la);                     // 높은 레벨 먼저
        if (levelCmp != 0) return levelCmp;

        return a.Id.CompareTo(b.Id);                         // 동률 시 안정적 순서 보장
    }

    // 목록의 소녀는 세 상태 중 하나다.
    //   파견 나감(진행 중 슬롯) : 목록에서 감춘다. 돌아올 때까지 어디에도 쓸 수 없어 자리만 차지한다.
    //   대기 슬롯에 올려 둠     : 목록에 남기고 표식만 켠다(자리를 옮기거나 되돌릴 수 있어야 한다).
    //   비어 있음               : 평소대로 집을 수 있다.
    // 카드를 파괴하지 않고 켜고 끄기만 하므로, 파견을 보내고 받는 사이에 목록 순서가 흔들리지 않는다.
    private void RefreshRosterState() {
        foreach (var card in _rosterCards) {
            if (card == null || card.Data == null) continue;
            int id = card.Data.Id;

            bool dispatched = IsDispatched(id);
            card.gameObject.SetActive(!dispatched);
            if (dispatched) continue;

            bool placed = IsPlaced(id);
            card.SetState(inDeck: placed, isRepresentative: false, isSelected: false);
            card.SetDragSource(placed ? null : this, CompositionDragHandle.RosterSlot);
        }
    }

    // 지금 파견을 나가 있는 소녀인지(서버가 시간을 재고 있는 슬롯의 편성). 편집 중인 대기 슬롯은 해당하지 않는다.
    private bool IsDispatched(int charDataId) {
        if (_info == null || charDataId == 0) return false;
        foreach (var slot in _info.Slots)
            if (slot.IsRunning && slot.Members.Contains(charDataId)) return true;
        return false;
    }

    private bool IsPlaced(int charDataId) {
        if (_members == null || charDataId == 0) return false;
        foreach (var row in _members)
            foreach (int id in row)
                if (id == charDataId) return true;
        return false;
    }

    // ── 버튼 ─────────────────────────────────────────────
    private void OnSendRequested(int slotIndex) {
        if (_busy) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        ActAsync(DispatchActionType.Send, slotIndex).Forget();
    }

    private void OnRefreshRequested(int slotIndex) {
        if (_busy) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        ActAsync(DispatchActionType.Refresh, slotIndex).Forget();
    }

    private void OnClaimRequested(int slotIndex) {
        if (_busy) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        ActAsync(DispatchActionType.Claim, slotIndex).Forget();
    }

    private async UniTaskVoid ActAsync(DispatchActionType action, int slotIndex) {
        if (_busy || _info == null) return;
        _busy = true;
        RefreshSlots();   // 처리 중에는 버튼을 잠근다

        try {
            var members = action == DispatchActionType.Send ? _members[slotIndex] : null;
            var res = await _hud.Dispatch.ActAsync(action, slotIndex, members, _ct);
            if (!res.IsSuccess) {
                _hud.Ui.ShowToast(DispatchRules.FailMessage(res.FailReason));
                return;
            }

            ApplyResult(res);
            _hud.Sound.PlaySfx(action == DispatchActionType.Claim ? SfxKey.Claim : SfxKey.ButtonClick);

            if (action == DispatchActionType.Claim) {
                // 아이템/코스튬 등은 공용 경로가, 범용 재화(각인석/파견 증명서)는 잔액을 따로 주지 않으므로
                // 더하는 경로가 함께 처리한다. 골드만 서버가 최신 잔액을 돌려준다.
                _hud.Store.ApplyGrantedRewards(res.Granted);
                _hud.Store.AddGrantedCurrencies(res.Granted);
                if (res.NewGold > 0) _hud.Store.Gold.Value = res.NewGold;
                _hud.Ui.ShowToast("파견 보상을 받았습니다.");
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("파견을 처리하지 못했습니다.");
        } finally {
            _busy = false;
            RefreshAll();
        }
    }

    // 응답에 담긴 슬롯 상태를 그대로 반영한다(작업 하나에 슬롯 하나가 바뀐다).
    private void ApplyResult(DispatchActionResultDto res) {
        _info.RefreshCharges = res.RefreshCharges;

        var state = SlotState(res.SlotIndex);
        if (state == null) {
            state = new DispatchSlotDto { SlotIndex = res.SlotIndex };
            _info.Slots.Add(state);
        }
        state.ConditionId = res.ConditionId;
        state.Members.Clear();
        state.Members.AddRange(res.Members);
        state.StartUnix = res.StartUnix;
        state.EndUnix = res.EndUnix;

        // 완료 수령/새로고침으로 대기 상태가 된 슬롯은 편집 버퍼도 비운다(다음 편성을 처음부터 짠다).
        if (!state.IsRunning && res.SlotIndex >= 0 && res.SlotIndex < _members.Length)
            Array.Clear(_members[res.SlotIndex], 0, _members[res.SlotIndex].Length);

        // 배지는 서비스가 완료 시각으로 판단하므로 최신 슬롯 목록을 그대로 넘긴다(서버를 다시 두드리지 않는다).
        _hud.Dispatch.TrackEndTimes(_info.Slots);
    }

    // ── 편성 편집 ────────────────────────────────────────
    private void Place(int position, int charDataId) {
        int slot = DispatchSlotView.SlotOfPosition(position, MembersPerSlot);
        int member = DispatchSlotView.MemberOfPosition(position, MembersPerSlot);
        if (!IsEditable(slot)) return;

        // 다른 자리에 이미 있는 소녀를 다시 놓으면 그 자리를 비운다(같은 소녀가 두 자리에 남지 않게).
        RemoveEverywhere(charDataId);
        _members[slot][member] = charDataId;
    }

    private void Swap(int fromPosition, int toPosition) {
        int fromSlot = DispatchSlotView.SlotOfPosition(fromPosition, MembersPerSlot);
        int toSlot = DispatchSlotView.SlotOfPosition(toPosition, MembersPerSlot);
        if (!IsEditable(fromSlot) || !IsEditable(toSlot)) return;

        int fromMember = DispatchSlotView.MemberOfPosition(fromPosition, MembersPerSlot);
        int toMember = DispatchSlotView.MemberOfPosition(toPosition, MembersPerSlot);

        (_members[fromSlot][fromMember], _members[toSlot][toMember]) =
            (_members[toSlot][toMember], _members[fromSlot][fromMember]);
    }

    private void RemoveAt(int position) {
        int slot = DispatchSlotView.SlotOfPosition(position, MembersPerSlot);
        if (!IsEditable(slot)) return;
        _members[slot][DispatchSlotView.MemberOfPosition(position, MembersPerSlot)] = 0;
    }

    private void RemoveEverywhere(int charDataId) {
        if (charDataId == 0) return;
        for (int s = 0; s < _members.Length; s++) {
            if (!IsEditable(s)) continue;
            for (int m = 0; m < _members[s].Length; m++)
                if (_members[s][m] == charDataId) _members[s][m] = 0;
        }
    }

    // 파견 중인 슬롯은 편성을 바꿀 수 없다(서버가 이미 그 편성으로 시간을 재고 있다).
    private bool IsEditable(int slotIndex) {
        if (_members == null || slotIndex < 0 || slotIndex >= _members.Length) return false;
        var state = SlotState(slotIndex);
        return state == null || !state.IsRunning;
    }

    // ── 드래그(CompositionDragHandle.IHost) ──────────────
    public bool CanDrag(CompositionDragHandle handle) =>
        !_busy && _dragging == null && handle != null && handle.CharDataId != 0;

    public void OnDragBegan(CompositionDragHandle handle, PointerEventData eventData) {
        _dragging = handle;
        _hud.Sound.PlaySfx(SfxKey.CardPick);
        CreateGhost(handle, eventData);
        handle.SetDimmed(true);
        UpdateHoveredSlot(eventData);
    }

    public void OnDragMoved(CompositionDragHandle handle, PointerEventData eventData) {
        MoveGhost(eventData);
        UpdateHoveredSlot(eventData);
    }

    public void OnDragEnded(CompositionDragHandle handle, PointerEventData eventData) {
        var target = _hoveredSlot;

        if (handle != null) handle.SetDimmed(false);
        SetHoveredSlot(null);
        DestroyGhost();
        _dragging = null;

        // eventData 가 없으면 카드가 파괴되어 드래그가 끊긴 경우다(편집하지 않고 조용히 정리).
        if (eventData == null || handle == null || _members == null) return;

        if (target != null) {
            if (handle.IsFromSlot) Swap(handle.SlotIndex, target.SlotIndex);
            else Place(target.SlotIndex, handle.CharDataId);
        } else if (handle.IsFromSlot) {
            RemoveAt(handle.SlotIndex);   // 자리 밖에 놓으면 편성 해제(목록에서 집어 밖에 놓은 것은 취소)
        } else {
            return;
        }

        _hud.Sound.PlaySfx(SfxKey.CardPlace);
        RefreshAll();
    }

    // ── 드래그 복제본(고스트) ────────────────────────────
    private void CreateGhost(CompositionDragHandle handle, PointerEventData eventData) {
        DestroyGhost();

        var layer = dragLayer != null ? dragLayer : (RectTransform)transform;
        var source = handle.GhostSource;

        _ghost = Instantiate(source.gameObject, layer);
        _ghost.name = "DragGhost";
        _ghostRt = (RectTransform)_ghost.transform;

        // 복제본은 표시 전용이다. 입력을 받으면 아래의 자리가 드롭 대상으로 잡히지 않는다.
        foreach (var h in _ghost.GetComponentsInChildren<CompositionDragHandle>(true)) Destroy(h);
        foreach (var a in _ghost.GetComponentsInChildren<TutorialAnchor>(true)) Destroy(a);
        var group = _ghost.GetComponent<CanvasGroup>();
        if (group == null) group = _ghost.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = dragGhostAlpha;

        _ghostRt.anchorMin = _ghostRt.anchorMax = _ghostRt.pivot = new Vector2(0.5f, 0.5f);
        _ghostRt.sizeDelta = source.rect.size;
        float layerScale = layer.lossyScale.x;
        float ratio = layerScale > 0f ? source.lossyScale.x / layerScale : 1f;
        _ghostRt.localScale = Vector3.one * (ratio * dragGhostScale);
        MoveGhost(eventData);
    }

    private void MoveGhost(PointerEventData eventData) {
        if (_ghostRt == null || eventData == null) return;

        var layer = dragLayer != null ? dragLayer : (RectTransform)transform;
        var canvas = layer.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, eventData.position, camera, out var local)) return;

        // 레이어를 어떻게 잡아 두든 손끝에 정확히 붙도록 pivot 차이를 보정한다.
        _ghostRt.anchoredPosition = local + (layer.pivot - new Vector2(0.5f, 0.5f)) * layer.rect.size;
    }

    private void DestroyGhost() {
        if (_ghost != null) Destroy(_ghost);
        _ghost = null;
        _ghostRt = null;
    }

    // ── 드롭 대상 판정 ───────────────────────────────────
    private void UpdateHoveredSlot(PointerEventData eventData) => SetHoveredSlot(RaycastSlot(eventData));

    private CompositionSlotView RaycastSlot(PointerEventData eventData) {
        if (eventData == null || EventSystem.current == null) return null;

        _raycastBuffer.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastBuffer);
        for (int i = 0; i < _raycastBuffer.Count; i++) {
            var slot = _raycastBuffer[i].gameObject.GetComponentInParent<CompositionSlotView>();
            // 파견 중인 슬롯의 자리는 드롭 대상이 아니다(편성을 바꿀 수 없다).
            if (slot != null && IsEditable(DispatchSlotView.SlotOfPosition(slot.SlotIndex, MembersPerSlot))) return slot;
        }
        return null;
    }

    private void SetHoveredSlot(CompositionSlotView slot) {
        if (_hoveredSlot == slot) return;
        if (_hoveredSlot != null) _hoveredSlot.SetDropHighlight(false);
        _hoveredSlot = slot;
        if (_hoveredSlot != null) _hoveredSlot.SetDropHighlight(true);
    }

    // ── 정리 ─────────────────────────────────────────────
    protected override void OnClosed() {
        SetHoveredSlot(null);
        DestroyGhost();
        _dragging = null;

        foreach (var c in _rosterCards) if (c != null) Destroy(c.gameObject);
        _rosterCards.Clear();
        if (slots != null)
            foreach (var s in slots) if (s != null) s.Clear();
    }
}
