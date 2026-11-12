using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 터치 조이스틱(플로팅). 터치 영역 안 아무 곳이나 누르면 그 자리에 조이스틱이 나타나고, 끌면 손잡이가 따라가며
/// 방향(크기 0~1)을 낸다. 손을 떼면 0 으로 돌아가고 숨는다(Always Visible 이면 원래 자리로 돌아간다).
///
/// 배치 규약:
///   이 컴포넌트   터치를 받을 영역(투명 Image, Raycast Target 켬). 보통 화면 왼쪽 절반을 덮는다.
///   Base          조이스틱 바닥 원(영역의 자식). 시작 위치가 곧 Always Visible 일 때의 기본 자리다.
///   Knob          손잡이(Base 의 자식, 가운데 앵커).
/// 다른 버튼(메뉴 등)은 이 영역보다 하이어라키 아래에 두어 터치를 먼저 받게 한다.
/// </summary>
public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler {
    [SerializeField] private RectTransform baseRect;
    [SerializeField] private RectTransform knob;
    [Tooltip("손잡이가 움직일 수 있는 반경(Base 로컬 단위). 이만큼 끌면 크기 1 이다.")]
    [SerializeField, Min(1f)] private float radius = 100f;
    [Tooltip("이 비율 안쪽의 미세한 끌림은 0 으로 본다(손가락 떨림 무시).")]
    [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.1f;
    [Tooltip("켜면 손을 떼도 조이스틱을 기본 자리에 보여 둔다. 끄면 누를 때만 나타난다.")]
    [SerializeField] private bool alwaysVisible = true;

    private RectTransform _area;
    private Vector2 _homePosition;
    private int _pointerId = int.MinValue;

    /// <summary>지금 입력 방향(크기 0~1). 누르고 있지 않으면 0.</summary>
    public Vector2 Direction { get; private set; }

    /// <summary>손가락이 조이스틱을 잡고 있는지.</summary>
    public bool IsHeld => _pointerId != int.MinValue;

    private void Awake() {
        _area = (RectTransform)transform;
        if (baseRect != null) _homePosition = baseRect.anchoredPosition;
        Release();
    }

    // 화면이 꺼지거나 씬이 멈춰 PointerUp 을 못 받으면 입력이 눌린 채로 남는다. 꺼질 때 반드시 놓는다.
    private void OnDisable() => Release();

    public void OnPointerDown(PointerEventData eventData) {
        if (IsHeld || baseRect == null) return;
        _pointerId = eventData.pointerId;

        // 누른 자리로 바닥 원을 옮긴다(플로팅).
        var parent = (RectTransform)baseRect.parent;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var local))
            baseRect.anchoredPosition = local - AnchorOffset(parent);
        baseRect.gameObject.SetActive(true);
        UpdateKnob(eventData);
    }

    public void OnDrag(PointerEventData eventData) {
        if (eventData.pointerId != _pointerId) return;
        UpdateKnob(eventData);
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (eventData.pointerId != _pointerId) return;
        Release();
    }

    private void UpdateKnob(PointerEventData eventData) {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out var local))
            return;

        // Base 의 pivot 이 가운데가 아니어도 원의 중심 기준으로 계산한다.
        Vector2 center = (new Vector2(0.5f, 0.5f) - baseRect.pivot) * baseRect.rect.size;
        Vector2 offset = Vector2.ClampMagnitude(local - center, radius);
        if (knob != null) knob.anchoredPosition = offset;

        Vector2 dir = offset / radius;
        Direction = dir.magnitude < deadZone ? Vector2.zero : dir;
    }

    private void Release() {
        _pointerId = int.MinValue;
        Direction = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        if (baseRect == null) return;
        baseRect.anchoredPosition = _homePosition;
        baseRect.gameObject.SetActive(alwaysVisible);
    }

    // anchoredPosition 은 앵커 기준이라, 부모 로컬 좌표(피벗 기준)를 넣으려면 앵커 위치만큼 빼야 한다.
    private Vector2 AnchorOffset(RectTransform parent) {
        Vector2 anchor = (baseRect.anchorMin + baseRect.anchorMax) * 0.5f;
        return (anchor - parent.pivot) * parent.rect.size;
    }
}
