using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 노드 두 개를 잇는 연결선 1개. 단순한 Image 를 두 점 사이에 맞춰 회전/신축시켜 "선"으로 쓴다.
///
/// 별도의 선 렌더러나 외부 라이브러리 없이 UI 이미지만으로 그리므로, 프리팹은 가로로 긴 흰색 Image 하나면 된다
/// (색과 두께는 코드/인스펙터에서 정한다). 트리 화면이 노드 배치를 정한 뒤 이 컴포넌트로 선을 이어 준다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class HqNodeLinkView : MonoBehaviour {
    [Tooltip("선 이미지. 비우면 이 오브젝트의 Image 를 사용한다. Pivot 은 (0, 0.5) 여야 시작점 기준으로 늘어난다.")]
    [SerializeField] private Image line;
    [Tooltip("선 두께(px, 기준 해상도 기준).")]
    [SerializeField, Min(1f)] private float thickness = 6f;

    private RectTransform _rect;

    private RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

    private void Awake() {
        if (line == null) line = GetComponent<Image>();
        // 시작점에서 목표점 방향으로 늘어나야 하므로 피벗을 왼쪽 중앙으로 고정한다(프리팹 설정 실수 방지).
        Rect.pivot = new Vector2(0f, 0.5f);
        Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// 부모(트리 컨테이너) 로컬 좌표 기준으로 두 점을 잇는다. 두 노드가 같은 컨테이너의 자식이므로
    /// anchoredPosition 을 그대로 넘기면 된다.
    /// </summary>
    public void Connect(Vector2 from, Vector2 to, Color color) {
        Vector2 delta = to - from;
        float length = delta.magnitude;

        Rect.anchoredPosition = from;
        Rect.sizeDelta = new Vector2(length, thickness);
        // 길이가 0이면 방향을 정의할 수 없으므로 회전을 건드리지 않는다(같은 좌표에 노드를 둔 설정 실수 방어).
        if (length > 0.001f)
            Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        if (line != null) line.color = color;
    }
}
