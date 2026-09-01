using UnityEngine;

/// <summary>
/// RectTransform 을 단말기 Safe Area(노치/홈 인디케이터/펀치홀 제외 영역)에 맞춰 자동으로 줄여주는 컴포넌트.
/// 사용법: Canvas 바로 아래에 화면 전체를 덮는 빈 패널(SafeArea)을 만들고 여기에 이 컴포넌트를 붙인 뒤,
/// 실제 UI 는 모두 그 SafeArea 패널의 자식으로 둔다. 새 씬/새 UI 도 같은 패턴만 반복하면 된다.
///
/// 동작 원리: Screen.safeArea(픽셀)를 0~1 앵커로 환산해 RectTransform 에 적용한다.
/// 앵커만 사용하므로 CanvasScaler 의 스케일/해상도 설정과 무관하게 정확히 맞는다.
/// 화면 회전/해상도 변경 시에만 재계산하므로 매 프레임 비용은 비교 몇 번뿐이다.
/// </summary>
// ExecuteAlways: Play 모드뿐 아니라 에디트 모드에서도 실행되게 한다.
// 이렇게 해야 Game 뷰를 Simulator 뷰로 바꾸고 기기를 고르면(Play 안 눌러도) 레이아웃이 즉시 safe area 에 맞춰 미리보기 된다.
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour {
    [Tooltip("좌우(가로) 인셋을 적용한다. 전체화면 배경처럼 가로로는 꽉 채우고 싶으면 끈다.")]
    [SerializeField] private bool applyHorizontal = true;

    [Tooltip("상하(세로) 인셋을 적용한다. 노치/홈바를 피하려면 보통 켜둔다.")]
    [SerializeField] private bool applyVertical = true;

    private RectTransform _rect;

    // 마지막으로 적용한 화면 상태. 바뀐 경우에만 재계산한다.
    private Rect _lastSafeArea;
    private Vector2Int _lastResolution;
    private ScreenOrientation _lastOrientation;
    private float _lastSideInset;

    private void OnEnable() => Apply();

    private void Update() {
        if (HasScreenChanged()) Apply();
    }

    // 유저 여백(DisplaySettings.UiSideInset)도 화면 상태의 일부로 함께 본다.
    // 이 컴포넌트는 아주 많은 오브젝트에 붙고 씬 전환마다 생겼다 사라지므로, 값이 바뀌었다고 알려 주는
    // 구독 방식은 쓰지 않는다. 구독은 해제를 한 번만 놓쳐도 새는 데다, 나중에 만들어진 오브젝트나
    // 꺼져 있다 켜진 오브젝트가 알림을 놓쳐 혼자 예전 여백으로 남는 사고가 난다.
    // 이미 매 프레임 도는 비교에 float 하나를 더하는 쪽이 비용도 위험도 더 낮다.
    private bool HasScreenChanged() {
        return _lastSafeArea != Screen.safeArea
            || _lastResolution.x != Screen.width
            || _lastResolution.y != Screen.height
            || _lastOrientation != Screen.orientation
            || _lastSideInset != DisplaySettings.UiSideInset;
    }

    private void Apply() {
        // 에디트 모드 도메인 리로드 등으로 캐시가 비어 있을 수 있어 지연 초기화한다.
        if (_rect == null) _rect = GetComponent<RectTransform>();

        // 초기 한두 프레임에 화면 크기가 0 으로 들어오는 경우가 있어 0 나눗셈을 막는다.
        if (Screen.width == 0 || Screen.height == 0) return;

        _lastSafeArea = Screen.safeArea;
        _lastResolution = new Vector2Int(Screen.width, Screen.height);
        _lastOrientation = Screen.orientation;
        _lastSideInset = DisplaySettings.UiSideInset;

        Rect area = Screen.safeArea;
        Vector2 anchorMin = area.position;
        Vector2 anchorMax = area.position + area.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // 선택한 축만 인셋을 적용하고, 끈 축은 0~1 로 꽉 채운다.
        if (!applyHorizontal) { anchorMin.x = 0f; anchorMax.x = 1f; }
        if (!applyVertical)   { anchorMin.y = 0f; anchorMax.y = 1f; }

        _rect.anchorMin = anchorMin;
        _rect.anchorMax = anchorMax;

        // 영역은 앵커가 잡으므로 오프셋은 유저 여백에만 쓴다(Left = +inset, Right = -inset).
        // 여백이 음수면 부호가 뒤집혀 Safe Area 바깥으로 넓어진다. 계산은 그대로라 분기가 필요 없다.
        // 가로 인셋을 끈 대상(전체화면 배경 등)은 애초에 가장자리까지 채우는 것이 목적이라 여백도 주지 않는다.
        // 그렇게 하지 않으면 배경만 안쪽으로 딸려 들어와 화면 양옆에 빈 띠가 생긴다.
        float inset = applyHorizontal ? DisplaySettings.UiSideInset : 0f;
        _rect.offsetMin = new Vector2(inset, 0f);
        _rect.offsetMax = new Vector2(-inset, 0f);
    }
}
