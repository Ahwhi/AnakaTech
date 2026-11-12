using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 편성 드래그의 표시부(복제본 고스트 + 드롭 대상 슬롯 판정/강조). CompositionDragHandle.IHost 를 구현하는 화면이
/// 하나씩 들고 쓰는 부품이다. 화면은 "무엇을 어디에 놓았을 때 무슨 일이 일어나는가"만 정하고,
/// 손끝을 따라다니는 카드와 슬롯 강조는 전부 여기서 처리한다.
///
/// 고스트: 집은 카드를 그대로 복제해 전용 레이어에 띄운다(살짝 크게/반투명). 복제본은 입력을 받지 않으므로
/// 그 아래의 슬롯이 정상적으로 드롭 대상이 된다.
/// 드롭 대상: 포인터 아래를 직접 훑어 이 화면의 슬롯만 고른다(뒤에 깔린 다른 화면의 슬롯은 잡지 않는다).
/// </summary>
public sealed class CompositionDragGhost {
    private readonly RectTransform _layer;
    private readonly float _alpha;
    private readonly float _scale;
    private readonly IReadOnlyList<CompositionSlotView> _slots;
    private readonly List<RaycastResult> _raycastBuffer = new();

    private GameObject _ghost;
    private RectTransform _ghostRt;

    /// <summary>지금 포인터 아래의 슬롯(없으면 null). 드래그를 끝낼 때 드롭 대상으로 쓴다.</summary>
    public CompositionSlotView HoveredSlot { get; private set; }

    /// <param name="layer">복제본이 올라갈 레이어(Raycast Target 이 꺼진 빈 RectTransform).</param>
    /// <param name="slots">드롭 대상이 될 슬롯들. 이 목록 밖의 슬롯은 판정에서 빠진다.</param>
    public CompositionDragGhost(RectTransform layer, IReadOnlyList<CompositionSlotView> slots,
                                float alpha = 0.8f, float scale = 1.05f) {
        _layer = layer;
        _slots = slots;
        _alpha = alpha;
        _scale = scale;
    }

    public void Begin(CompositionDragHandle handle, PointerEventData eventData) {
        Clear();
        if (handle == null || _layer == null) return;

        var source = handle.GhostSource;
        _ghost = Object.Instantiate(source.gameObject, _layer);
        _ghost.name = "DragGhost";
        _ghostRt = (RectTransform)_ghost.transform;

        // 복제본은 표시 전용이다. 입력을 받거나 유도 대상으로 잡히면 아래의 슬롯/가이드가 어긋난다.
        foreach (var h in _ghost.GetComponentsInChildren<CompositionDragHandle>(true)) Object.Destroy(h);
        foreach (var a in _ghost.GetComponentsInChildren<TutorialAnchor>(true)) Object.Destroy(a);
        var group = _ghost.GetComponent<CanvasGroup>();
        if (group == null) group = _ghost.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = _alpha;

        // 원본과 같은 크기로 띄운 뒤 살짝만 키운다(레이아웃 그룹 밖으로 나오므로 크기를 직접 잡아 준다).
        _ghostRt.anchorMin = _ghostRt.anchorMax = _ghostRt.pivot = new Vector2(0.5f, 0.5f);
        _ghostRt.sizeDelta = source.rect.size;
        float layerScale = _layer.lossyScale.x;
        float ratio = layerScale > 0f ? source.lossyScale.x / layerScale : 1f;
        _ghostRt.localScale = Vector3.one * (ratio * _scale);
        Move(eventData);
    }

    /// <summary>고스트를 손끝으로 옮기고 아래의 슬롯을 강조한다.</summary>
    public void Move(PointerEventData eventData) {
        SetHovered(RaycastSlot(eventData));
        if (_ghostRt == null || eventData == null) return;

        var canvas = _layer.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, eventData.position, camera, out var local)) return;

        // 레이어를 어떻게 잡아 두든 손끝에 정확히 붙도록 pivot 차이를 보정한다.
        _ghostRt.anchoredPosition = local + (_layer.pivot - new Vector2(0.5f, 0.5f)) * _layer.rect.size;
    }

    /// <summary>고스트와 강조를 치운다. 드래그가 끝나거나 화면이 닫힐 때 부른다.</summary>
    public void Clear() {
        SetHovered(null);
        if (_ghost != null) Object.Destroy(_ghost);
        _ghost = null;
        _ghostRt = null;
    }

    private CompositionSlotView RaycastSlot(PointerEventData eventData) {
        if (eventData == null || EventSystem.current == null || _slots == null) return null;

        _raycastBuffer.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastBuffer);
        for (int i = 0; i < _raycastBuffer.Count; i++) {
            var slot = _raycastBuffer[i].gameObject.GetComponentInParent<CompositionSlotView>();
            if (slot != null && Contains(slot)) return slot;
        }
        return null;
    }

    private bool Contains(CompositionSlotView slot) {
        for (int i = 0; i < _slots.Count; i++) if (_slots[i] == slot) return true;
        return false;
    }

    private void SetHovered(CompositionSlotView slot) {
        if (HoveredSlot == slot) return;
        if (HoveredSlot != null) HoveredSlot.SetDropHighlight(false);
        HoveredSlot = slot;
        if (HoveredSlot != null) HoveredSlot.SetDropHighlight(true);
    }
}
