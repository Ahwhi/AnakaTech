using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "여기를 가리켜도 된다"고 알리는 표식. 버튼/슬롯/탭 등 유도 대상이 되는 오브젝트에 붙이고 Id 만 고른다.
///
/// 역할은 그것뿐이다. 어떤 퀘스트가 이 지점을 쓰는지, 언제 가리키는지는 전혀 모른다(TutorialGuideRoutes 가 정한다).
/// 그래서 같은 지점을 여러 퀘스트가 함께 써도 되고, 유도 순서를 바꿔도 이 컴포넌트는 손대지 않는다.
///
/// 켜질 때 등록하고 꺼질 때 해제하므로, 팝업이 열리면 그 안의 앵커가 저절로 후보가 되고 닫히면 사라진다.
/// 클릭을 가로채지 않고 이벤트도 걸지 않아, 붙였다는 이유로 원래 동작이 달라지는 일이 없다.
///
/// 예외로 "관문"만은 이 컴포넌트가 안다(satisfiedWhenActive). 탭처럼 선택 버튼과 실행 버튼이 한 화면에
/// 나란히 있으면 둘 다 동시에 보여 순서가 무너지는데, 통과 여부를 UI 가 이미 표시(선택 표시/페이지 전환)하고
/// 있으므로 그 오브젝트를 가리키는 것만으로 순서를 지킬 수 있다.
///
/// 한 오브젝트에 여러 개를 붙여도 된다. 같은 버튼/카드가 경로마다 다른 조건으로 쓰이는 경우
/// (예: 육성 유도는 아무 카드나, 편성 유도는 아직 편성하지 않은 카드만)를 Id 와 조건이 다른 앵커
/// 두 개로 표현하기 위해서다. 각 앵커는 자기 Id 로만 등록되므로 서로 간섭하지 않는다.
/// </summary>
public sealed class TutorialAnchor : MonoBehaviour {
    [Tooltip("이 지점의 식별자. 경로(TutorialGuideRoutes)에서 같은 값을 고르면 여기를 가리킨다.")]
    [SerializeField] private TutorialAnchorId id;

    [Tooltip("가이드 프레임이 감쌀 영역(비우면 이 오브젝트의 RectTransform). " +
             "아이콘만 있는 버튼처럼 실제 눌리는 범위와 보이는 범위가 다를 때 지정한다.")]
    [SerializeField] private RectTransform area;

    [Tooltip("영역에 더할 여백(px). 프레임이 버튼에 너무 붙거나 너무 뜰 때 이 지점만 조절한다.")]
    [SerializeField] private Vector2 padding;

    [Tooltip("체크하면 눌 수 없는 상태(Button 의 Interactable = off)여도 가리킨다. " +
             "기본은 끔이라, 조건이 안 되는 버튼을 가리켜 유저를 헤매게 하지 않는다.")]
    [SerializeField] private bool pointWhenDisabled;

    [Tooltip("이 오브젝트가 켜져 있으면 이 지점을 가리키지 않는다(선택). 같은 Id 를 여러 항목이 공유할 때 " +
             "이미 처리한 항목을 후보에서 빼는 데 쓴다. 예: 카드의 '편성됨' 표시를 넣으면 아직 편성하지 않은 " +
             "카드만 가리킨다. 통과 판정(아래 satisfiedWhenActive)과는 목적이 다르다. 이쪽은 '이 항목은 대상이 " +
             "아니다', 저쪽은 '이 단계는 끝났으니 다음으로 간다' 이다.")]
    [SerializeField] private GameObject skipWhenActive;

    [Tooltip("이 오브젝트가 꺼져 있으면 이 지점을 가리키지 않는다(선택). 위 제외 조건의 반대다. " +
             "예: 편성 슬롯의 '빈 칸' 표시를 넣으면 아직 비어 있는 슬롯만 가리킨다.")]
    [SerializeField] private GameObject requireActive;

    [Tooltip("이 오브젝트의 CanvasGroup 이 흐려져 있으면 가리키지 않는다(선택). 편성 드래그처럼 " +
             "'집어 든 동안 원본이 흐려지는' 대상에만 체크한다. 부모의 CanvasGroup(팝업 페이드 등)은 보지 않는다.")]
    [SerializeField] private bool skipWhenFaded;

    [Tooltip("이 지점을 '통과했다'고 알려 주는 오브젝트(선택). 켜져 있으면 통과, 꺼져 있으면 미통과로 본다. " +
             "보통 탭의 선택 표시나 그 탭이 여는 페이지 루트를 넣는다.\n" +
             "할당하면 이 지점은 관문이 되어, 통과하기 전에는 뒤 단계로 넘어가지 않는다. " +
             "탭(선택)과 실행 버튼이 한 화면에 같이 있어 둘 다 동시에 보이는 경우를 위한 것이다. " +
             "비워 두면 관문이 아니며, 순서는 화면 깊이대로만 정해진다.")]
    [SerializeField] private GameObject satisfiedWhenActive;

    private RectTransform _rect;
    private Selectable _selectable;
    private bool _selectableResolved;
    private CanvasGroup _group;

    private static readonly Vector3[] Corners = new Vector3[4];

    // skipWhenFaded 판정 기준. 이보다 흐려지면 "지금 이 자리에 없는 것"으로 본다.
    private const float MinVisibleAlpha = 0.5f;

    // 이 이하면 화면에 그려지지 않는 것으로 본다(닫힌 내장 패널 = alpha 0).
    private const float InvisibleAlpha = 0.01f;

    // 자기 자신에게 걸린 CanvasGroup 만 본다. 부모의 CanvasGroup 은 팝업 페이드처럼 화면 전체의 연출이라
    // 잠깐 흐려졌다고 대상에서 빼면 열리는 동안 엉뚱한 앞 단계가 잠깐 보인다.
    // 런타임에 붙는 경우가 있어(드래그가 CanvasGroup 을 추가한다) 찾을 때까지 다시 조회한다.
    private CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>());

    public TutorialAnchorId Id => id;

    /// <summary>가이드 프레임이 감쌀 영역.</summary>
    public RectTransform Area => area != null ? area : (_rect != null ? _rect : (_rect = transform as RectTransform));

    public Vector2 Padding => padding;

    /// <summary>이 지점이 관문인지(통과 여부를 알려 주는 오브젝트가 배선돼 있는지).</summary>
    public bool IsGate => satisfiedWhenActive != null;

    /// <summary>관문을 통과했는지. 관문이 아니면 항상 true(막지 않는다).</summary>
    public bool IsSatisfied => satisfiedWhenActive == null || IsShown(satisfiedWhenActive);

    /// <summary>
    /// 지금 이 지점을 가리켜도 되는지. 켜져 있고, 제외 대상이 아니고, 실제로 화면 안에 있고,
    /// 누를 수 있는 상태여야 한다. 스크롤 밖으로 밀려난 항목이나 잠긴 버튼, 이미 처리한 항목을
    /// 가리키면 유도가 아니라 혼란이 되므로 여기서 걸러 낸다.
    /// </summary>
    public bool IsPointable {
        get {
            if (!isActiveAndEnabled) return false;
            if (skipWhenActive != null && IsShown(skipWhenActive)) return false;
            if (requireActive != null && !IsShown(requireActive)) return false;

            var rect = Area;
            if (rect == null) return false;

            if (!IsVisible(transform)) return false;

            if (skipWhenFaded) {
                var group = Group;
                if (group != null && group.alpha < MinVisibleAlpha) return false;
            }

            if (!pointWhenDisabled) {
                if (!_selectableResolved) {
                    _selectable = GetComponent<Selectable>();
                    _selectableResolved = true;
                }
                if (_selectable != null && !_selectable.IsInteractable()) return false;
            }

            return OverlapsScreen(rect);
        }
    }

    /// <summary>
    /// 완전히 투명하지 않은지(부모의 CanvasGroup 까지 거슬러 올라가 확인).
    ///
    /// 덱 팝업의 육성/장비/코스튬처럼 내장 패널로 도킹해 여는 화면은 닫혀 있어도 오브젝트가 살아 있다
    /// (UIView 가 파괴 대신 alpha 0 으로 숨겨 재사용한다). 이 검사가 없으면 아직 열지도 않은 패널 안의
    /// 버튼이 후보로 잡혀, 화면에는 아무것도 안 보이는데 가이드는 "가리키는 중"이 된다.
    ///
    /// 기준을 0 에 가깝게 두는 이유는 흐릿하게 그려지는 UI(선택 안 된 탭 등)를 없는 것으로 치지 않기 위해서다.
    /// 페이드로 잠깐 0 을 지나는 순간은 다음 판단에서 곧바로 회복되므로 문제가 되지 않는다.
    /// </summary>
    /// <summary>
    /// 조건 오브젝트가 지금 화면에 나와 있는지. 조건(제외/필요/통과) 판정은 전부 이 하나를 쓴다.
    ///
    /// 켜져 있는지(activeInHierarchy)만 보면 안 된다. 덱 팝업의 육성/장비/코스튬처럼 도킹해 여는 화면은
    /// 한 번도 닫힌 적이 없으면 "켜진 채 투명하게" 대기한다(UIView 가 파괴 대신 alpha 0 으로 숨기고,
    /// 처음 닫힐 때 비로소 SetActive(false) 가 된다). 그 상태를 열린 것으로 세면
    /// "패널이 열리면 이 버튼은 가리지 않는다" 같은 조건이 처음부터 켜져 있어 유도가 통째로 어긋난다.
    /// </summary>
    private static bool IsShown(GameObject go) =>
        go != null && go.activeInHierarchy && IsVisible(go.transform);

    private static bool IsVisible(Transform start) {
        var node = start;
        while (node != null) {
            var group = node.GetComponent<CanvasGroup>();
            if (group != null) {
                if (group.alpha <= InvisibleAlpha) return false;
                if (group.ignoreParentGroups) break;   // 위쪽은 이 오브젝트에 영향을 주지 않는다.
            }
            node = node.parent;
        }
        return true;
    }

    // 화면과 겹치는지. 스크롤뷰 밖으로 나간 항목은 오브젝트가 켜져 있어도 보이지 않으므로 대상이 아니다.
    private static bool OverlapsScreen(RectTransform rect) {
        var canvas = rect.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        rect.GetWorldCorners(Corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, Corners[0]);
        Vector2 max = min;
        for (int i = 1; i < 4; i++) {
            Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, Corners[i]);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        if (max.x - min.x < 1f || max.y - min.y < 1f) return false;
        return max.x > 0f && max.y > 0f && min.x < Screen.width && min.y < Screen.height;
    }

    private void OnEnable() => TutorialAnchorRegistry.Register(this);
    private void OnDisable() => TutorialAnchorRegistry.Unregister(this);
}
