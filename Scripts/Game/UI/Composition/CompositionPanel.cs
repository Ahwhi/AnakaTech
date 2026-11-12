using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 편성 화면(DeckPopup 내부 패널, 육성/장비/코스튬 패널과 같은 방식으로 도킹해 열린다).
///
/// 보유 캐릭터 목록을 따로 갖지 않는다. 도킹해도 덱 팝업의 카드 그리드는 화면에 그대로 남으므로,
/// 그 카드를 집어 이 패널의 진형 슬롯에 끌어다 놓는 구조다. 목록을 하나 더 만들면 같은 캐릭터가
/// 두 목록에 그려지고 정렬/갱신 규칙도 두 벌이 된다. 목록의 주인은 계속 덱 팝업이며, 이 패널은
/// 드래그 판정(IHost)과 진형 슬롯만 책임진다.
///
/// 조작 규칙(상용 게임의 파티 편성과 같은 규약):
///   덱 카드 -> 빈 슬롯    : 편성
///   덱 카드 -> 찬 슬롯    : 그 자리의 캐릭터와 교체
///   슬롯   -> 다른 슬롯   : 자리 맞바꿈(순서 변경 = 진형 변경)
///   슬롯   -> 슬롯 밖     : 편성 해제
///   덱 카드 -> 슬롯 밖    : 취소(아무 일도 일어나지 않음)
/// 제일 왼쪽(0번) 슬롯이 대표 자리다. 거기로 끌어다 놓는 것이 곧 대표 지정이라 대표 버튼이 따로 없다.
///
/// 드래그 표현: 집은 카드를 그대로 복제해 전용 레이어에 띄우고 살짝 크게/반투명하게 손끝을 따라가게 한다
/// (원본은 자리에 흐리게 남는다). 복제본이라 카드 모양이 훼손되지 않으며, 복제본은 레이캐스트를 받지 않으므로
/// 그 아래의 슬롯이 정상적으로 드롭 대상이 된다.
///
/// 시너지: 편집 중인 편성으로 활성 시너지를 계산해 목록으로 보여 주고, 이번 편집으로 "새로" 켜진 것만
/// 전용 토스트로 알린다. 조건은 화면 어디에도 적지 않는다(유저가 직접 찾아내는 것이 이 시스템의 설계).
/// </summary>
public sealed class CompositionPanel : UIView, CompositionDragHandle.IHost {
    [Header("Slots (진형)")]
    [Tooltip("편성 슬롯 5칸. 배열 순서가 곧 슬롯 번호이며, 0번이 대표 슬롯이다. " +
             "슬롯을 생성하지 않고 화면에 직접 배치해 두는 이유는 진형 배치를 눈으로 잡기 위해서다.")]
    [SerializeField] private CompositionSlotView[] slots;
    [Tooltip("슬롯에 붙일 덱 카드 프리팹. 덱 팝업의 그리드 카드와 같은 프리팹을 넣어야 " +
             "목록에서 집은 카드가 슬롯에서도 같은 모습으로 보인다.")]
    [SerializeField] private DeckCharacterCard slotCardPrefab;

    [Header("Drag")]
    [Tooltip("드래그 중인 카드 복제본이 올라갈 레이어. 슬롯/목록보다 위에 그려지도록 하이어라키 마지막에 두고, " +
             "Raycast Target 이 꺼진 빈 RectTransform 으로 만든다. 비우면 이 패널 루트를 쓴다.")]
    [SerializeField] private RectTransform dragLayer;
    [Tooltip("드래그 중 복제본의 투명도. 아래에 있는 슬롯이 비쳐 보일 정도로만 낮춘다.")]
    [Range(0.2f, 1f)]
    [SerializeField] private float dragGhostAlpha = 0.8f;
    [Tooltip("드래그 중 복제본의 크기 배율. 살짝 키워 '손에 들렸다'는 느낌을 준다.")]
    [SerializeField] private float dragGhostScale = 1.05f;

    [Header("Synergy")]
    [Tooltip("활성 시너지 줄이 생성될 부모. 비우면 시너지 목록을 표시하지 않는다.")]
    [SerializeField] private Transform synergyContent;
    [Tooltip("활성 시너지 줄 프리팹(SynergyRowView).")]
    [SerializeField] private SynergyRowView synergyRowPrefab;
    [Tooltip("활성 시너지가 하나도 없을 때 켜는 안내 묶음. 선택.")]
    [SerializeField] private GameObject synergyEmptyGroup;

    [Header("Summary")]
    [Tooltip("편성 인원 표시(예: 3 / 5). 선택.")]
    [SerializeField] private TMP_Text memberCountText;
    [Tooltip("편성 전체 전투력 합계. 선택.")]
    [SerializeField] private TMP_Text totalPowerText;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private MainHudContext _hud;
    private DeckEditSession _session;
    private bool _wired;   // 리스너 1회 구성 가드(내장 패널로 재사용되므로 중복 리스너 방지).
    private bool _open;    // 이 패널이 열려 있는 동안에만 시너지 알림을 띄운다.

    // 슬롯에 붙어 있는 덱 카드(슬롯 번호와 같은 인덱스). 빈 슬롯은 null.
    private DeckCharacterCard[] _slotCards;
    private readonly List<SynergyRowView> _synergyRows = new();

    // 활성 시너지 판정 결과 버퍼. 편집할 때마다 도는 경로라 매번 새로 만들지 않는다.
    private readonly List<SynergyData> _activeSynergies = new();
    private readonly HashSet<int> _announcedSynergyIds = new();

    // 드래그 상태.
    private CompositionDragHandle _dragging;
    private GameObject _ghost;
    private RectTransform _ghostRt;
    private CompositionSlotView _hoveredSlot;
    private readonly List<RaycastResult> _raycastBuffer = new();

    /// <summary>편성이 바뀌었을 때 발생(덱 팝업이 카드 표식/상세를 갱신한다).</summary>
    public event Action Changed;

    public void Initialize(MainHudContext hud, DeckEditSession session) {
        _hud = hud;
        _session = session;

        if (!_wired) {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            _wired = true;
        }

        // 이번에 열기 전부터 켜져 있던 시너지는 "새로 켜진 것"이 아니므로 알림 대상에서 제외한다.
        RecomputeSynergies();
        _announcedSynergyIds.Clear();
        foreach (var s in _activeSynergies) if (s != null) _announcedSynergyIds.Add(s.Id);

        // 시너지가 하나도 등록돼 있지 않으면 활성화될 일이 없다. 조용히 아무 일도 없으면 데이터 미할당인지
        // 조합이 안 맞은 것인지 구분할 수 없으므로 여기서 한 번 알려 준다.
        if (_hud.SynergyDb == null || _hud.SynergyDb.All.Count == 0)
            Debug.LogWarning("[CompositionPanel] 시너지 데이터가 비어 있습니다. LifetimeScope 의 " +
                             "Synergy Database 할당과 그 안의 SynergyData 등록을 확인하세요.", this);

        _open = true;
        InitializeSlots();
        RefreshAll();
    }

    private void InitializeSlots() {
        if (slots == null) return;
        _slotCards ??= new DeckCharacterCard[slots.Length];
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] != null) slots[i].Initialize(i);
        }
    }

    // ── 표시 갱신 ────────────────────────────────────────
    private void RefreshAll() {
        RefreshSlots();
        RefreshSummary();
        RefreshSynergyList();
    }

    // 슬롯마다 덱 카드를 붙이고 내용을 채운다. 카드는 슬롯당 하나를 만들어 재사용하며, 캐릭터가 빠지면 파괴한다.
    private void RefreshSlots() {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++) {
            var slot = slots[i];
            if (slot == null) continue;

            int charId = _session.At(i);
            var data = charId != 0 ? _hud.CharDb.Get(charId) : null;

            if (data == null) {
                if (_slotCards[i] != null) { Destroy(_slotCards[i].gameObject); _slotCards[i] = null; }
                slot.SetFilled(false);
                continue;
            }

            var card = _slotCards[i];
            if (card == null && slotCardPrefab != null) {
                card = Instantiate(slotCardPrefab);
                slot.Attach((RectTransform)card.transform);
                _slotCards[i] = card;
            }
            if (card == null) continue;

            // 슬롯 카드는 목록 카드와 같은 프리팹이라 표시가 저절로 같다. 클릭 선택은 목록의 몫이라 넘기지 않는다.
            card.Bind(_hud.Appearance.Mine(data), _hud.Store.GetProgress(charId), null);
            // 슬롯에서는 편성/대표 배지를 쓰지 않는다. 슬롯에 있다는 것 자체가 편성됐다는 뜻이고,
            // 대표는 슬롯 자신의 표식(0번 슬롯)이 보여 준다. 명시하지 않으면 프리팹 기본값이 그대로 남아
            // 다섯 칸에 배지가 전부 켜진 채로 보인다. 육성 배지도 같은 이유로 꺼 둔다(진형을 보는 자리다).
            card.SetState(inDeck: false, isRepresentative: false, isSelected: false);
            card.SetUpgradeBadge(false);
            // 유도(튜토리얼) 대상에서도 뺀다. 이 카드는 목록 카드의 사본이라, 켜 두면 "아직 편성하지 않은
            // 소녀" 를 가리켜야 할 때 이미 슬롯에 들어와 있는 이 카드가 대신 잡힌다.
            card.SetGuideTarget(false);
            card.SetDragSource(this, i);
            slot.SetFilled(true);
        }
    }

    private void RefreshSummary() {
        if (memberCountText != null)
            memberCountText.text = $"{_session.Count} / {DeckEditSession.Capacity}";

        if (totalPowerText == null) return;

        double total = 0d;   // 21억을 넘는 합계도 깨지지 않게 double 로 더하고 그대로 표시한다
        foreach (int id in _session.Members) {
            var data = _hud.CharDb.Get(id);
            var progress = _hud.Store.GetProgress(id);
            if (data == null || progress == null) continue;
            // 편집 중인 편성으로 시너지를 판정한다(활성 목록에 뜬 시너지가 합계 전투력에도 그대로 반영되도록).
            total += _hud.Store.RuntimeFor(data, progress, _hud.Settings, _hud.EquipDb, _session.Members).CombatPower;
        }
        totalPowerText.text = total.ToString("N0");
    }

    // ── 시너지 ───────────────────────────────────────────
    private void RecomputeSynergies() {
        if (_hud.SynergyDb != null) _hud.SynergyDb.CollectActive(_session.Members, _activeSynergies);
        else _activeSynergies.Clear();
    }

    private void RefreshSynergyList() {
        ClearSynergyRows();
        if (synergyEmptyGroup != null) synergyEmptyGroup.SetActive(_activeSynergies.Count == 0);
        if (synergyContent == null || synergyRowPrefab == null) return;

        foreach (var synergy in _activeSynergies) {
            if (synergy == null) continue;
            var row = Instantiate(synergyRowPrefab, synergyContent);
            row.Bind(synergy);
            _synergyRows.Add(row);
        }
    }

    // 편집 결과 새로 켜진 시너지만 알린다. 껐다 다시 켜면 다시 알리도록 꺼진 것은 기록에서 지운다.
    //
    // 알림은 이 패널을 보고 있는 동안에만 띄운다. 시너지 알림은 "지금 이 조합으로 뭔가 걸렸다"는 신호라
    // 편성을 만지고 있지 않을 때(덱 팝업을 닫으며 저장하는 순간 등) 뜨면 무엇 때문인지 알 수 없다.
    // 이미 떠 있는 토스트는 그대로 두므로, 마지막에 맞춘 시너지는 패널을 닫아도 끝까지 보인다.
    private void AnnounceNewSynergies() {
        _announcedSynergyIds.RemoveWhere(id => !IsActive(id));
        if (!_open) return;

        foreach (var synergy in _activeSynergies) {
            if (synergy == null || !_announcedSynergyIds.Add(synergy.Id)) continue;
            _hud.Ui.ShowSynergyActivated(synergy.DisplayName, synergy.EffectSummary());
        }
    }

    private bool IsActive(int synergyId) {
        foreach (var s in _activeSynergies) if (s != null && s.Id == synergyId) return true;
        return false;
    }

    // ── 편집 반영 ────────────────────────────────────────
    // 편성이 실제로 바뀌었을 때만 부른다. 시너지 판정 -> 표시 갱신 -> 새 시너지 알림 순서를 지켜야
    // 목록에 이미 나타난 시너지에 대해 알림이 뜬다(반대로 하면 알림만 먼저 뜨고 목록이 비어 보인다).
    private void ApplyEdit() {
        RecomputeSynergies();
        RefreshAll();
        AnnounceNewSynergies();
        Changed?.Invoke();
    }

    // ── 드래그(CompositionDragHandle.IHost) ──────────────
    // 이미 하나를 들고 있으면 새 드래그를 받지 않는다(멀티터치로 두 장을 동시에 집으면 고스트/원본 상태가 어긋난다).
    public bool CanDrag(CompositionDragHandle handle) =>
        _dragging == null && handle != null && handle.CharDataId != 0;

    public void OnDragBegan(CompositionDragHandle handle, PointerEventData eventData) {
        _dragging = handle;
        _hud.Sound.PlaySfx(SfxKey.CardPick);   // 카드를 집는 순간(스크롤 넘기기와 구분되는 시점)
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
        if (eventData == null || handle == null) return;

        if (target != null) DropOnSlot(handle, target.SlotIndex);
        else DropOutside(handle);
    }

    private void DropOnSlot(CompositionDragHandle handle, int slotIndex) {
        bool changed = handle.IsFromSlot
            ? _session.MoveSlot(handle.SlotIndex, slotIndex)          // 슬롯 -> 슬롯: 자리 맞바꿈
            : _session.Place(slotIndex, handle.CharDataId);           // 목록 -> 슬롯: 편성/교체

        if (!changed) {
            // 정원이 찬 상태에서 새 캐릭터를 빈 슬롯에 놓으려 한 경우만 안내가 필요하다
            // (같은 자리에 도로 놓은 경우처럼 "바뀔 것이 없던" 상황은 조용히 넘어간다).
            if (!handle.IsFromSlot && !_session.Contains(handle.CharDataId) && _session.IsFull)
                _hud.Ui.ShowToast("최대 5명까지 편성할 수 있습니다.");
            return;
        }
        _hud.Sound.PlaySfx(SfxKey.CardPlace);   // 슬롯에 실제로 꽂혔을 때만(자리 맞바꿈 포함)
        ApplyEdit();
    }

    private void DropOutside(CompositionDragHandle handle) {
        // 목록에서 집어 밖에 놓은 것은 취소다(아무 일도 하지 않는다).
        if (!handle.IsFromSlot) return;

        if (!_session.RemoveAt(handle.SlotIndex)) {
            _hud.Ui.ShowToast("최소 1명은 편성해야 합니다.");
            return;
        }
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        ApplyEdit();
    }

    // ── 드래그 복제본(고스트) ────────────────────────────
    // 집은 카드를 그대로 복제해 쓴다. 전용 고스트 프리팹을 따로 두면 카드 구성이 바뀔 때마다 두 곳을
    // 맞춰야 하고, 조금만 어긋나도 "집는 순간 모양이 바뀌는" 어색함이 생긴다.
    private void CreateGhost(CompositionDragHandle handle, PointerEventData eventData) {
        DestroyGhost();

        var layer = dragLayer != null ? dragLayer : (RectTransform)transform;
        var source = handle.GhostSource;

        _ghost = Instantiate(source.gameObject, layer);
        _ghost.name = "DragGhost";
        _ghostRt = (RectTransform)_ghost.transform;

        // 복제본은 표시 전용이다. 입력을 받으면 아래의 슬롯이 드롭 대상으로 잡히지 않는다.
        foreach (var h in _ghost.GetComponentsInChildren<CompositionDragHandle>(true)) Destroy(h);
        // 유도(튜토리얼) 표식도 같은 이유로 뗀다. 남겨 두면 손끝을 따라다니는 복제본이 유도 대상으로 잡혀
        // 가이드 프레임이 함께 끌려다닌다.
        foreach (var a in _ghost.GetComponentsInChildren<TutorialAnchor>(true)) Destroy(a);
        var group = _ghost.GetComponent<CanvasGroup>();
        if (group == null) group = _ghost.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = dragGhostAlpha;

        // 원본과 같은 크기로 띄운 뒤 살짝만 키운다(레이아웃 그룹 밖으로 나오므로 크기를 직접 잡아 준다).
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

        // local 은 레이어의 pivot 기준이고 고스트는 중앙 앵커라, 레이어 pivot 이 중앙이 아니면 그만큼 어긋난다.
        // 레이어를 어떻게 잡아 두든 손끝에 정확히 붙도록 pivot 차이를 여기서 보정한다.
        _ghostRt.anchoredPosition = local + (layer.pivot - new Vector2(0.5f, 0.5f)) * layer.rect.size;
    }

    private void DestroyGhost() {
        if (_ghost != null) Destroy(_ghost);
        _ghost = null;
        _ghostRt = null;
    }

    // ── 드롭 대상 판정 ───────────────────────────────────
    // 슬롯에 IDropHandler 를 두지 않고 포인터 아래를 직접 훑는다. 그래야 "슬롯 밖에 놓았다"(= 해제)를
    // 별도 처리 없이 같은 자리에서 판정할 수 있고, 드래그 중 강조 표시도 같은 결과를 그대로 쓴다.
    private void UpdateHoveredSlot(PointerEventData eventData) {
        SetHoveredSlot(RaycastSlot(eventData));
    }

    private CompositionSlotView RaycastSlot(PointerEventData eventData) {
        if (eventData == null || EventSystem.current == null) return null;

        _raycastBuffer.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastBuffer);
        for (int i = 0; i < _raycastBuffer.Count; i++) {
            var slot = _raycastBuffer[i].gameObject.GetComponentInParent<CompositionSlotView>();
            if (slot != null) return slot;
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
    private void ClearSlotCards() {
        if (_slotCards == null) return;
        for (int i = 0; i < _slotCards.Length; i++) {
            if (_slotCards[i] != null) Destroy(_slotCards[i].gameObject);
            _slotCards[i] = null;
        }
    }

    private void ClearSynergyRows() {
        foreach (var r in _synergyRows) if (r != null) Destroy(r.gameObject);
        _synergyRows.Clear();
    }

    protected override void OnClosed() {
        _open = false;
        SetHoveredSlot(null);
        DestroyGhost();
        _dragging = null;
        ClearSlotCards();
        ClearSynergyRows();
    }
}
