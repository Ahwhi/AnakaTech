using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 소프트 키보드가 입력창을 가리지 않도록 패널을 비켜 주는 공용 컴포넌트.
/// 입력창이 있는 패널에 붙이기만 하면 되고, 붙인 쪽은 키보드에 대해 아무것도 알 필요가 없다.
///
/// 비키는 방법이 두 가지인 이유는 패널의 생김새가 다르기 때문이다.
///   Move   : 화면 가운데 뜨는 모달(쿠폰/닉네임/검색 등). 팝업을 통째로 위로 옮긴다.
///            뒤가 가려져도 상관없는 모달이라 레이아웃을 다시 계산할 필요가 없고, 입력창과 함께
///            확인 버튼/오류 문구/글자수도 같이 올라온다.
///   Resize : 화면 한쪽을 세로로 가득 채우는 패널(채팅). 통째로 올리면 위쪽이 화면 밖으로 나가므로,
///            아래 변만 키보드 위로 끌어올려 패널을 줄인다. 입력창은 패널 바닥에 붙어 있으므로
///            레이아웃이 알아서 따라오고, 목록만 짧아진다(메신저들이 하는 그 동작이다).
///
/// 이동량은 "키보드 높이"가 아니라 "실제로 가려진 만큼"이다. 입력창이 이미 키보드 위에 있으면 0이라
/// 아무 일도 일어나지 않는다.
///
/// 안전장치를 둔 이유: 키보드 높이는 기기/IME(툴바, 추천어 줄, 이모지 행)마다 크게 다르고 실시간으로도
/// 변한다. 값을 잘못 받아도 화면이 무너지지 않고 "비키지 않은 상태"로 떨어지게 상한과 최소 높이를 둔다.
/// </summary>
[DisallowMultipleComponent]
public sealed class KeyboardAvoidingPanel : MonoBehaviour {
    public enum AvoidMode {
        /// <summary>패널을 통째로 위로 옮긴다(화면 가운데 모달용).</summary>
        Move,
        /// <summary>패널의 아래 변을 끌어올려 줄인다(세로로 가득 찬 패널용).</summary>
        Resize,
    }

    [Header("Behaviour")]
    [Tooltip("Move = 팝업을 통째로 이동(가운데 모달). Resize = 아래 변만 올려 축소(세로로 긴 패널).")]
    [SerializeField] private AvoidMode mode = AvoidMode.Move;

    [Tooltip("움직이거나 줄일 대상. 비우면 이 오브젝트의 RectTransform 을 쓴다.\n" +
             "Resize 대상은 세로로 늘어난(Stretch) 앵커여야 한다.\n" +
             "부모에 Layout Group 이 있으면 그쪽이 위치/크기를 다시 잡아 버리므로 그 영향을 받지 않는 것을 지정할 것.\n" +
             "Move 는 팝업 루트에 슬라이드 연출(IViewTransitionElement)이 걸려 있으면 서로 같은 값을 " +
             "건드려 싸우므로, 그런 팝업은 연출이 걸리지 않은 안쪽 컨테이너를 지정할 것.")]
    [SerializeField] private RectTransform target;

    [Tooltip("감시할 입력창들. 비우면 자식에서 전부 찾는다. 이 중 하나라도 포커스를 잡고 있을 때만 비킨다.")]
    [SerializeField] private TMP_InputField[] inputFields;

    [Header("Move")]
    [Tooltip("입력창 아래와 키보드 윗변 사이에 남길 여백(캔버스 단위). Move 에서만 쓴다.")]
    [SerializeField] private float margin = 16f;

    [Header("Resize")]
    [Tooltip("줄어든 뒤에도 남겨 둘 최소 높이(화면 높이 대비 비율). 키보드 높이를 크게 잘못 읽어도 " +
             "패널이 찌그러지지 않게 막는 하한이다. Resize 에서만 쓴다.")]
    [Range(0.2f, 0.9f)]
    [SerializeField] private float minHeightRatio = 0.35f;

    [Tooltip("선택. 줄어든 뒤 맨 아래로 다시 내릴 목록. 넣지 않으면 보던 위치가 그대로 남아 " +
             "마지막 대화가 화면 밖으로 밀려난 것처럼 보인다.")]
    [SerializeField] private ScrollRect scrollToBottom;

    [Header("Safety")]
    [Tooltip("키보드 높이로 인정할 상한(화면 높이 대비 비율). 이보다 큰 값이 오면 잘못 읽은 것으로 보고 " +
             "여기서 자른다. 가로 모드는 키보드가 화면 높이의 절반 가까이 차지하므로 넉넉히 둔다.")]
    [Range(0.3f, 0.9f)]
    [SerializeField] private float maxKeyboardRatio = 0.6f;

    [Header("Animation")]
    [Tooltip("비키는 데 걸리는 시간(초). 키보드가 올라오는 속도와 비슷해야 화면이 튀지 않는다.")]
    [SerializeField] private float duration = 0.18f;

    [Header("Editor Preview")]
    [Tooltip("에디터에서 키보드 높이를 흉내 낸다(화면 높이 대비 비율). 빌드에는 영향이 없다.\n" +
             "반드시 플레이 모드에서 끌 것. 편집 모드에서는 동작하지 않는데, 이 컴포넌트가 RectTransform 값을 " +
             "직접 바꾸기 때문에 편집 중에 움직이면 비킨 상태가 프리팹에 그대로 저장되어 버린다.\n" +
             "Move 는 '가려진 만큼만' 움직이므로, 입력창이 화면 가운데에 있으면 0.5 근처까지 올려야 움직이기 " +
             "시작한다(그 전에는 아직 가려지지 않은 것이라 안 움직이는 것이 정상이다).")]
    [Range(0f, 0.7f)]
    [SerializeField] private float editorPreviewRatio;

    private RectTransform _target;
    private Canvas _canvas;
    private Tween _tween;

    // 비키기 전의 원래 값. 여기로 되돌리는 것이 곧 "원상복구"다.
    private float _restBottom;       // Resize: offsetMin.y
    private Vector2 _restPosition;   // Move: anchoredPosition
    private bool _captured;

    // 현재 적용 중인 이동량(캔버스 단위). 목표와 같으면 트윈을 다시 걸지 않는다.
    private float _applied;
    private float _desired;

    // 입력창 모서리를 받는 버퍼. 매 프레임 쓰는 자리라 새로 만들지 않고 재사용한다.
    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>이 정도 차이는 같은 목표로 본다(캔버스 단위). 눈에 보이지 않는 차이로 연출이 다시 걸리지 않게 한다.</summary>
    private const float OffsetTolerance = 1f;

    // 비키기 전의 높이(Resize 전용). 최소 높이 판정의 기준이라 줄어들기 전 값을 잡아 둔다.
    private float _restHeight;

    private void Awake() {
        _target = target != null ? target : (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null) _canvas = _canvas.rootCanvas;

        if (inputFields == null || inputFields.Length == 0)
            inputFields = GetComponentsInChildren<TMP_InputField>(true);
    }

    private void OnEnable() {
        // 원래 값은 켜질 때마다 다시 잡는다. 팝업은 재사용되므로 이전에 비킨 상태가 남아 있을 수 있다.
        Capture();
        _desired = 0f;
        ApplyImmediate(0f);
    }

    private void OnDisable() {
        _tween?.Kill();
        _tween = null;
        _desired = 0f;
        ApplyImmediate(0f);
        _captured = false;
    }

    private void Capture() {
        if (_captured || _target == null) return;
        _restBottom = _target.offsetMin.y;
        _restPosition = _target.anchoredPosition;
        _restHeight = _target.rect.height;
        _captured = true;
    }

    private void Update() {
        if (_target == null) return;

        // 캔버스는 Awake 때 못 찾을 수 있다(부모를 나중에 붙여 인스턴스화하는 경로가 있다).
        // 한 번 찾으면 그대로 두고, 못 찾은 동안에는 아무것도 하지 않는다.
        if (_canvas == null) {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) return;
            _canvas = _canvas.rootCanvas;
        }

        // 목표가 실질적으로 같으면 트윈을 다시 걸지 않는다. Approximately 로 보면 트윈이 도는 동안의
        // 미세한 부동소수 흔들림에도 매 프레임 다시 걸려 연출이 끝나지 않는다(완료 콜백도 오지 않는다).
        float next = ResolveOffset();
        if (Mathf.Abs(next - _desired) < OffsetTolerance) return;

        _desired = next;
        _tween?.Kill();

        // 즉시 반영해야 하는 경우(시간이 0 이하)까지 트윈으로 돌리면 한 프레임 늦는다.
        if (duration <= 0f) { ApplyImmediate(next); AfterSettle(); return; }

        // 트윈 정리는 OnDisable 이 맡는다(오브젝트가 파괴될 때도 OnDisable 이 먼저 불린다).
        float from = _applied;
        _tween = DOTween.To(() => from, v => ApplyImmediate(v), next, duration)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true)      // 일시정지(타임스케일 0)에서도 화면은 움직여야 한다.
                        .OnComplete(AfterSettle);
    }

    /// <summary>지금 비켜야 할 양(캔버스 단위). 비킬 필요가 없으면 0.</summary>
    private float ResolveOffset() {
        // 포커스를 먼저 본다. 안드로이드에서 키보드 높이를 구하는 일은 JNI 호출이라 공짜가 아닌데,
        // 입력 중이 아니면 어차피 비킬 일이 없다. 순서를 뒤집으면 팝업이 떠 있는 내내 매 프레임 묻게 된다.
        //
        // 에디터 미리보기는 인스펙터 슬라이더를 끄는 순간 게임 뷰가 포커스를 잃어 입력창도 풀리므로,
        // 그때까지 포커스를 요구하면 미리보기가 영영 동작하지 않는다. 그래서 미리보기는 건너뛴다.
        if (!IsPreviewing && !AnyInputFocused()) return 0f;

        float keyboard = KeyboardHeight();
        if (keyboard <= 0f) return 0f;

        return mode == AvoidMode.Resize ? ResizeOffset(keyboard) : MoveOffset(keyboard);
    }

    /// <summary>에디터 미리보기로 키보드 높이를 흉내 내는 중인지.</summary>
    private bool IsPreviewing {
        get {
#if UNITY_EDITOR
            return editorPreviewRatio > 0f;
#else
            return false;
#endif
        }
    }

    // 아래 변을 키보드 윗변까지 끌어올린다. 이미 그보다 위에 있으면 움직이지 않는다.
    private float ResizeOffset(float keyboard) {
        float offset = keyboard - _restBottom;
        if (offset <= 0f) return 0f;

        // 줄어든 높이가 하한을 밑돌면 하한까지만 줄인다. 키보드를 덜 피하더라도 패널이 남아 있는 편이 낫다.
        // 기준은 "줄어들기 전 높이"다. 지금 높이를 쓰면 줄어들 때마다 하한 판정도 같이 내려간다.
        // 켜지는 시점에 크기가 아직 정해지지 않았으면(0) 하한을 판단할 근거가 없으므로 그냥 넘어간다.
        if (_restHeight > 0f) {
            float minHeight = _canvas.pixelRect.height / _canvas.scaleFactor * minHeightRatio;
            if (_restHeight - offset < minHeight) offset = Mathf.Max(0f, _restHeight - minHeight);
        }

        return offset;
    }

    // 포커스를 잡은 입력창이 키보드 위로 올라올 만큼만 옮긴다.
    // 미리보기 중에는 포커스가 없을 수 있으므로, 그때는 이 패널의 첫 입력창을 기준으로 삼는다.
    private float MoveOffset(float keyboard) {
        var field = FocusedField() ?? (IsPreviewing ? FirstField() : null);
        if (field == null) return 0f;

        // 이미 적용된 이동량을 빼서 "원래 자리 기준"으로 환산한다. 그러지 않으면 올라간 만큼을 다시
        // 더해 계산해 매 프레임 조금씩 더 올라간다.
        float restBottom = EdgeY((RectTransform)field.transform, bottom: true) - _applied;
        float offset = Mathf.Max(0f, keyboard + margin - restBottom);
        if (offset <= 0f) return 0f;

        // 위로 넘치지 않게 막는다. 키가 큰 팝업에서 입력창이 아래쪽에 있으면 필요한 이동량이 커지는데,
        // 그대로 올리면 제목/닫기 버튼이 화면 밖으로 나간다. 가로 모드는 화면이 짧고 키보드가 절반 가까이
        // 차지해서 특히 그렇다. 입력창이 조금 가려지는 편이 팝업 윗부분이 잘리는 것보다 낫다.
        //
        // 한계선은 부모(보통 세이프에리어)의 윗변이다. 부모가 없으면 화면 위쪽을 기준으로 삼는다.
        var bounds = _target.parent as RectTransform;
        float limitTop = bounds != null
            ? EdgeY(bounds, bottom: false)
            : _canvas.pixelRect.height / _canvas.scaleFactor;
        float restTop = EdgeY(_target, bottom: false) - _applied;

        return Mathf.Min(offset, Mathf.Max(0f, limitTop - restTop));
    }

    /// <summary>지정 사각형의 위/아래 변 위치(화면 아래를 0으로 하는 캔버스 단위).</summary>
    private float EdgeY(RectTransform rt, bool bottom) {
        rt.GetWorldCorners(_corners);   // 0 = 좌하단, 1 = 좌상단. 배열을 재사용해 매 프레임 할당하지 않는다.
        var cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        return RectTransformUtility.WorldToScreenPoint(cam, _corners[bottom ? 0 : 1]).y / _canvas.scaleFactor;
    }

    /// <summary>
    /// 키보드가 차지한 높이(캔버스 단위). 없거나 알 수 없으면 0.
    /// 플랫폼별로 어디에 물어야 하는지는 SoftKeyboard 가 안다(안드로이드는 유니티가 답하지 못해 윈도우에 직접 묻는다).
    /// </summary>
    private float KeyboardHeight() {
        float screenHeight = _canvas.pixelRect.height;

        // 미리보기가 실제 키보드보다 우선이다. 기기 시뮬레이터를 켜면 TouchScreenKeyboard 가
        // "지원한다"고 답하면서 정작 키보드는 뜨지 않아, 우선순위를 두지 않으면 미리보기가 먹지 않는다.
        float raw = IsPreviewing ? screenHeight * editorPreviewRatio : SoftKeyboard.Height;

        if (raw <= 0f) return 0f;
        raw = Mathf.Min(raw, screenHeight * maxKeyboardRatio);
        return raw / _canvas.scaleFactor;
    }

    private bool AnyInputFocused() => FocusedField() != null;

    private TMP_InputField FocusedField() {
        if (inputFields == null) return null;
        foreach (var f in inputFields)
            if (f != null && f.isFocused) return f;
        return null;
    }

    // 켜져 있는 첫 입력창(미리보기 기준점). 꺼져 있는 것은 화면에 없으므로 기준이 될 수 없다.
    private TMP_InputField FirstField() {
        if (inputFields == null) return null;
        foreach (var f in inputFields)
            if (f != null && f.gameObject.activeInHierarchy) return f;
        return null;
    }

    private void ApplyImmediate(float offset) {
        if (!_captured || _target == null) return;
        _applied = offset;

        if (mode == AvoidMode.Resize) {
            var min = _target.offsetMin;
            min.y = _restBottom + offset;
            _target.offsetMin = min;
        } else {
            _target.anchoredPosition = _restPosition + new Vector2(0f, offset);
        }
    }

    // 크기가 바뀌면 보던 위치의 의미도 달라진다. 목록은 맨 아래로 다시 내려 마지막 줄이 보이게 한다.
    private void AfterSettle() {
        if (mode != AvoidMode.Resize || scrollToBottom == null) return;
        scrollToBottom.velocity = Vector2.zero;
        scrollToBottom.verticalNormalizedPosition = 0f;
    }
}
