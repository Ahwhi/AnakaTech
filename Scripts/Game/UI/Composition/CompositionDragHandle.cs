using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 편성 화면의 드래그 손잡이. 덱 카드(DeckCharacterCard) 프리팹에 붙여, 보유 목록에 있든 편성 슬롯에 붙어 있든
/// 같은 카드가 같은 방식으로 집히게 한다. 손잡이는 "무엇을 집었는가"(캐릭터 id + 출발 슬롯)만 알려 주고
/// 실제 판단은 전부 화면(IHost = CompositionPanel)이 한다. 카드와 슬롯이 각자 드래그 규칙을 들고 있으면
/// 장착/해제/맞바꿈이 여러 벌로 갈라지기 때문이다.
///
/// 스크롤뷰 안에서의 드래그: 보유 목록은 스크롤뷰라 카드에 드래그 핸들러가 붙는 순간 스크롤이 막힌다
/// (자식이 이벤트를 먹어 ScrollRect 까지 가지 않는다). 그래서 두 경우에 제스처를 통째로 ScrollRect 에 넘긴다.
///   1) 지금 집을 수 없을 때(편성 화면이 닫혀 있어 host 가 없는 평소의 덱 화면) - 목록은 평소처럼 넘어가야 한다.
///   2) 집을 수 있어도 스크롤 축 쪽으로 움직였을 때 - 목록 넘기기로 본다.
/// 이걸 빠뜨리면 덱 화면에서 목록이 아예 스크롤되지 않는다.
/// </summary>
public sealed class CompositionDragHandle : MonoBehaviour,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {

    /// <summary>드래그 판단과 처리를 담당하는 화면. 편성 패널이 구현한다.</summary>
    public interface IHost {
        /// <summary>지금 이 손잡이를 집을 수 있는지.</summary>
        bool CanDrag(CompositionDragHandle handle);
        void OnDragBegan(CompositionDragHandle handle, PointerEventData eventData);
        void OnDragMoved(CompositionDragHandle handle, PointerEventData eventData);
        void OnDragEnded(CompositionDragHandle handle, PointerEventData eventData);
    }

    /// <summary>보유 목록에서 집었다는 표시(편성 슬롯이 아님).</summary>
    public const int RosterSlot = -1;

    [Tooltip("드래그 중 복제해 띄울 대상(카드 겉모습 전체). 비우면 이 오브젝트를 복제한다. " +
             "손잡이를 카드 안쪽 자식에 붙였다면 여기에 카드 루트를 지정해야 복제본이 잘리지 않는다.")]
    [SerializeField] private RectTransform ghostSource;
    [Tooltip("집어 든 동안 원래 자리에 남는 카드의 투명도(자리를 비워 보이게 한다).")]
    [Range(0f, 1f)]
    [SerializeField] private float draggingAlpha = 0.25f;

    /// <summary>집은 캐릭터. 0 이면 집을 것이 없는 상태다(빈 슬롯).</summary>
    public int CharDataId { get; private set; }

    /// <summary>출발 슬롯 번호. RosterSlot(-1)이면 보유 목록에서 집은 것이다.</summary>
    public int SlotIndex { get; private set; } = RosterSlot;

    /// <summary>편성 슬롯에서 집었는지(해제/맞바꿈 대상인지).</summary>
    public bool IsFromSlot => SlotIndex >= 0;

    /// <summary>드래그 중 띄울 복제본의 원본. 지정하지 않았으면 손잡이 자신이다.</summary>
    public RectTransform GhostSource => ghostSource != null ? ghostSource : (RectTransform)transform;

    private IHost _host;
    private ScrollRect _scroll;
    private bool _scrollSearched;
    private CanvasGroup _group;
    private bool _forwardingToScroll;   // 이번 제스처를 스크롤뷰에 넘겼는지
    private bool _dragging;

    /// <summary>
    /// 이 손잡이가 무엇을 가리키는지 설정한다. 목록/슬롯이 내용을 바꿀 때마다 다시 부른다.
    /// host 를 null 로 주면 드래그가 꺼진다(편성 화면이 닫혀 있는 평소 상태).
    /// </summary>
    public void Configure(IHost host, int charDataId, int slotIndex) {
        _host = host;
        CharDataId = charDataId;
        SlotIndex = slotIndex;
    }

    /// <summary>집어 든 동안 원래 자리를 비워 보이게 한다(화면이 켜고 끈다).</summary>
    public void SetDimmed(bool on) {
        if (_group == null) {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        }
        _group.alpha = on ? draggingAlpha : 1f;
    }

    // 부모 스크롤뷰는 계층이 바뀌지 않으므로 1회만 찾는다(없으면 null 그대로 둔다).
    private ScrollRect Scroll {
        get {
            if (_scrollSearched) return _scroll;
            _scrollSearched = true;
            _scroll = GetComponentInParent<ScrollRect>();
            return _scroll;
        }
    }

    // 스크롤뷰의 관성 정지 처리를 그대로 살린다(이걸 넘기지 않으면 목록이 손가락에 붙지 않는다).
    public void OnInitializePotentialDrag(PointerEventData eventData) {
        Scroll?.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData) {
        _forwardingToScroll = false;
        _dragging = false;

        bool canPick = _host != null && CharDataId != 0 && _host.CanDrag(this);
        if (!canPick || ShouldForwardToScroll(eventData)) {
            if (Scroll == null) return;
            _forwardingToScroll = true;
            Scroll.OnBeginDrag(eventData);
            return;
        }

        _dragging = true;
        _host.OnDragBegan(this, eventData);
    }

    public void OnDrag(PointerEventData eventData) {
        if (_forwardingToScroll) { Scroll?.OnDrag(eventData); return; }
        if (_dragging) _host.OnDragMoved(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (_forwardingToScroll) {
            Scroll?.OnEndDrag(eventData);
            _forwardingToScroll = false;
            return;
        }
        if (!_dragging) return;
        _dragging = false;
        _host.OnDragEnded(this, eventData);
    }

    // 집을 수 있는 상태에서, 이번 제스처가 목록 넘기기인지 카드 집기인지. 스크롤 가능한 축으로 더 많이 움직였으면 목록 넘기기다.
    private bool ShouldForwardToScroll(PointerEventData eventData) {
        var scroll = Scroll;
        if (scroll == null) return false;

        var delta = eventData.position - eventData.pressPosition;
        bool horizontalDominant = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
        return horizontalDominant ? scroll.horizontal : scroll.vertical;
    }

    // 드래그 도중 카드가 파괴되면(목록 재구성 등) 화면이 유령을 든 채 남지 않도록 정리한다.
    private void OnDisable() {
        if (!_dragging) return;
        _dragging = false;
        _host?.OnDragEnded(this, null);
    }
}
