using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 화면을 터치/클릭한 자리에 짧은 피드백 이펙트를 띄우는 전용 레이어.
/// 연출용 UIVfxLayer(UIParticle 풀링)와 목적이 달라 분리했다. 탭은 세션 내내 수천 번 발생하는 상시 입력이라
/// "가끔 화려하게"가 아니라 "항상 일정하고 싸게"가 요구사항이다. 그래서 파티클을 쓰지 않는다.
///
/// 비용 설계:
///  - 이미지 1장(쿼드)만 쓰고 크기/알파를 커브로 보간한다. 파티클 메시 재생성이 없다.
///  - 시작할 때 poolSize 만큼 미리 만들고 링 버퍼로 재사용한다(런타임 인스턴스화 0, 상한 보장).
///  - 코루틴/트윈 없이 이 컴포넌트의 Update 한 곳에서 수명을 감산한다(재생당 할당 0).
///  - 반드시 메인 UI 와 분리된 캔버스 아래에 둘 것. 같은 캔버스면 이펙트가 뜰 때마다 그 캔버스 전체가 리빌드된다.
///
/// 입력은 EventSystem 레이캐스트를 거치지 않고 포인터를 직접 읽는다.
/// 풀스크린 레이캐스트 대상을 깔면 그 아래 버튼들의 입력을 먹어버리기 때문이다.
/// 이 레이어와 이펙트 프리팹의 Raycast Target 은 모두 꺼둘 것.
/// </summary>
public sealed class TouchVfxLayer : MonoBehaviour {
    [Header("Refs")]
    [Tooltip("이펙트가 배치될 컨테이너(비우면 이 오브젝트). 분리된 VFX 캔버스 아래에 둘 것.")]
    [SerializeField] private RectTransform container;
    [Tooltip("이펙트 1장(Image). 링/원형 스프라이트 권장. Raycast Target 은 꺼둘 것.")]
    [SerializeField] private Image effectPrefab;

    [Header("Pool")]
    [Tooltip("미리 만들어 둘 이펙트 수. 다 쓰면 가장 오래된 것을 재사용한다(런타임 생성 없음).")]
    [SerializeField] private int poolSize = 12;

    [Header("Animation")]
    [Tooltip("이펙트 1회 지속 시간(초). 탭 피드백은 짧을수록 좋다(0.2~0.35 권장).")]
    [SerializeField] private float duration = 0.28f;
    [Tooltip("시작 지름(px, 캔버스 단위).")]
    [SerializeField] private float startSize = 40f;
    [Tooltip("종료 지름(px, 캔버스 단위). 너무 크면 오버드로우가 늘고 버튼 피드백을 가린다.")]
    [SerializeField] private float endSize = 140f;
    [Tooltip("진행도(0~1) -> 크기 보간 비율. 기본은 빠르게 퍼졌다가 완만해지는 형태.")]
    [SerializeField] private AnimationCurve sizeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("진행도(0~1) -> 알파. 기본은 1에서 0으로 사라진다.")]
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("Input")]
    [Tooltip("멀티터치를 각각 표시한다. 끄면 주 포인터 1개만 표시.")]
    [SerializeField] private bool multiTouch = true;

    private RectTransform[] _rects;
    private Image[] _images;
    private float[] _remain;   // 남은 시간(0 이하 = 비활성)
    private int _next;         // 링 버퍼 커서
    private Canvas _canvas;
    private Color _baseColor = Color.white;

    private RectTransform Container => container != null ? container : (RectTransform)transform;

    // 설정에서 끈 경우 새 이펙트를 내지 않는다(이미 떠 있는 것은 자연스럽게 끝까지 재생).
    private static bool SettingEnabled => UserDataManager.Data == null || UserDataManager.Data.ShowTouchVfx;

    private void Awake() {
        _canvas = GetComponentInParent<Canvas>(true);
        BuildPool();
    }

    private void BuildPool() {
        int count = Mathf.Max(1, poolSize);
        _rects = new RectTransform[count];
        _images = new Image[count];
        _remain = new float[count];
        if (effectPrefab == null) return;

        _baseColor = effectPrefab.color;
        for (int i = 0; i < count; i++) {
            var img = Instantiate(effectPrefab, Container);
            img.raycastTarget = false;   // 입력을 절대 가로채지 않는다.
            var rt = (RectTransform)img.transform;
            img.gameObject.SetActive(false);
            _images[i] = img;
            _rects[i] = rt;
        }
    }

    private void Update() {
        if (_rects == null) return;
        if (SettingEnabled) PollPressed();
        Tick(Time.unscaledDeltaTime);   // 타임스케일 0(팝업/연출)에서도 정상 재생.
    }

    // 이번 프레임에 새로 눌린 지점만 모은다. 터치 기기면 터치를, 아니면 주 포인터(마우스 등)를 읽는다.
    private void PollPressed() {
        var screen = Touchscreen.current;
        if (multiTouch && screen != null) {
            var touches = screen.touches;
            for (int i = 0; i < touches.Count; i++) {
                if (touches[i].press.wasPressedThisFrame)
                    Spawn(touches[i].position.ReadValue());
            }
            return;   // 터치가 있는 기기에서 마우스까지 읽으면 같은 입력이 두 번 잡힌다.
        }

        var pointer = Pointer.current;
        if (pointer != null && pointer.press.wasPressedThisFrame)
            Spawn(pointer.position.ReadValue());
    }

    private void Spawn(Vector2 screenPoint) {
        var rt = _rects[_next];
        if (rt == null) return;

        Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Container, screenPoint, cam, out var local)) return;

        int index = _next;
        _next = (_next + 1) % _rects.Length;   // 다 쓰면 가장 오래된 것을 덮어쓴다(상한 보장).

        rt.anchoredPosition = local;
        _remain[index] = duration;
        if (!rt.gameObject.activeSelf) rt.gameObject.SetActive(true);
        rt.SetAsLastSibling();
        Apply(index, 0f);
    }

    private void Tick(float dt) {
        for (int i = 0; i < _rects.Length; i++) {
            if (_remain[i] <= 0f) continue;

            _remain[i] -= dt;
            if (_remain[i] <= 0f) {
                _remain[i] = 0f;
                if (_rects[i] != null) _rects[i].gameObject.SetActive(false);
                continue;
            }
            Apply(i, 1f - Mathf.Clamp01(_remain[i] / Mathf.Max(0.01f, duration)));
        }
    }

    // 진행도 t(0~1)에 맞춰 크기/알파를 적용한다.
    private void Apply(int index, float t) {
        var rt = _rects[index];
        var img = _images[index];
        if (rt == null || img == null) return;

        float size = Mathf.LerpUnclamped(startSize, endSize, sizeCurve.Evaluate(t));
        rt.sizeDelta = new Vector2(size, size);

        var c = _baseColor;
        c.a = _baseColor.a * alphaCurve.Evaluate(t);
        img.color = c;
    }
}
