using UnityEngine;

/// <summary>
/// 화면 밖에 있는 월드 대상을 가리키는 화살표. 대상이 화면 밖이면 화면 가장자리(안쪽으로 Edge Padding 만큼)에
/// 화살표를 놓고 대상 쪽으로 돌린다. 대상이 화면 안에 들어오거나 없으면 숨긴다.
/// 무엇을 가리킬지는 쓰는 쪽이 SetTarget 으로 정한다(대상이 무엇인지 모른다).
///
/// 배치 규약:
///   이 컴포넌트   화살표가 움직일 영역(보통 화면 전체로 늘린 빈 RectTransform). Screen Space - Overlay 캔버스 기준.
///   Arrow         화살표 이미지(이 오브젝트의 자식, 가운데 앵커/피벗). Raycast Target 은 끈다.
/// </summary>
public sealed class OffscreenTargetIndicator : MonoBehaviour {
    [SerializeField] private RectTransform arrow;
    [Tooltip("대상을 비추는 월드 카메라. 비우면 Camera.main 을 쓴다.")]
    [SerializeField] private Camera worldCamera;
    [Tooltip("화면 가장자리에서 안쪽으로 띄울 거리(캔버스 단위).")]
    [SerializeField, Min(0f)] private float edgePadding = 90f;
    [Tooltip("화살표 그림이 가리키는 방향(도). 오른쪽 = 0, 위 = 90, 아래 = -90. 기본 화살표 스프라이트(아래 방향)는 -90.")]
    [SerializeField] private float spritePointAngle = -90f;

    private RectTransform _area;
    private Transform _target;

    private void Awake() {
        _area = (RectTransform)transform;
        if (arrow != null) arrow.gameObject.SetActive(false);
    }

    /// <summary>가리킬 대상. null 이면 숨긴다.</summary>
    public void SetTarget(Transform target) => _target = target;

    private void LateUpdate() {
        if (arrow == null) return;
        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (_target == null || cam == null) { Show(false); return; }

        Vector3 vp = cam.WorldToViewportPoint(_target.position);
        bool onScreen = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
        if (onScreen) { Show(false); return; }

        // 카메라 뒤(원근 카메라)면 방향이 뒤집히므로 되돌린다. 직교 카메라는 해당 없다.
        Vector2 d = new(vp.x - 0.5f, vp.y - 0.5f);
        if (vp.z < 0f) d = -d;

        Vector2 size = _area.rect.size;
        d = new Vector2(d.x * size.x, d.y * size.y);
        if (d.sqrMagnitude < 0.0001f) { Show(false); return; }

        // 영역 가장자리(안쪽 여백 제외)에 닿을 때까지 같은 방향으로 늘이거나 줄인다.
        Vector2 half = new(Mathf.Max(1f, size.x * 0.5f - edgePadding), Mathf.Max(1f, size.y * 0.5f - edgePadding));
        float kx = Mathf.Abs(d.x) > 0.0001f ? half.x / Mathf.Abs(d.x) : float.MaxValue;
        float ky = Mathf.Abs(d.y) > 0.0001f ? half.y / Mathf.Abs(d.y) : float.MaxValue;
        Vector2 pos = d * Mathf.Min(kx, ky);

        // 화살표는 영역 가운데 앵커라 anchoredPosition 이 곧 영역 중심 기준 위치다.
        arrow.anchoredPosition = pos;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        arrow.localRotation = Quaternion.Euler(0f, 0f, angle - spritePointAngle);
        Show(true);
    }

    private void Show(bool on) {
        if (arrow.gameObject.activeSelf != on) arrow.gameObject.SetActive(on);
    }
}
