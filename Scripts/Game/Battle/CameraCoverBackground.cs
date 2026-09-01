using UnityEngine;

/// <summary>
/// Orthographic 전투 카메라의 가시 영역을 배경 SpriteRenderer 가 덮도록(cover) 맞추되, 두 가지를 자연스럽게 살린다.
///  1) 줌 인/아웃 느낌: "가장 많이 줌아웃한 기준 orthoSize"로 한 번만 커버 스케일을 잡아 월드 스케일을 고정한다.
///     매 프레임 현재 뷰에 맞추면 줌아웃 시 배경도 같이 커져 화면상 크기가 그대로라 줌한 맛이 죽기 때문이다.
///  2) 이동 느낌: 배경을 카메라에 붙이지 않고 배치 위치(anchor)를 기준으로 카메라를 parallaxFactor 만큼만 따라가게 한다.
///     0 이면 월드에 고정되어 카메라가 캐릭터를 추적할 때 배경이 화면을 가로질러 흘러가 캐릭터의 이동감이 살고,
///     1 이면 카메라에 완전히 붙어 이동감이 사라진다(중간값은 원경 패럴랙스).
/// 카메라가 anchor 에서 벗어난 최대 거리를 추적해 그만큼 커버 범위를 넓히므로, 월드 고정(0)이어도 가장자리가 드러나지 않는다.
///
/// 원본 종횡비를 유지한 채 등비 스케일하므로 화면비가 달라도 여백 없이 채워진다(넘치는 방향은 잘리므로
/// 배경 원화 가장자리에는 안전 여백을 둘 것).
///
/// 카메라의 구동 주체를 모른 채 결과 Camera 만 읽는다. 따라서 SimpleBattleCamera 든 훗날 Cinemachine(Brain)
/// 이든 동일 Camera 를 지정하기만 하면 교체에 무관하게 동작한다(IBattleCamera 추상화와 결이 같다).
/// 배경 스프라이트가 던전마다 교체(BattleDirector.SetBackground)되어도 크기 변화를 감지해 자동 재적용한다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(1000)] // 카메라(및 Cinemachine Brain)의 LateUpdate 이후에 배치되도록 늦게 실행한다.
public sealed class CameraCoverBackground : MonoBehaviour {
    [Header("Refs")]
    [Tooltip("덮을 대상 Orthographic 카메라(비우면 Camera.main). Cinemachine 교체 시에도 Brain 이 붙은 이 카메라를 그대로 지정한다.")]
    [SerializeField] private Camera targetCamera;

    [Header("Fit")]
    [Tooltip("커버 스케일을 잡을 기준 orthographicSize(= 카메라가 가장 많이 줌아웃한 값, BattleSettings.CameraMaxZoom). " +
             "이 값으로 한 번만 크기를 고정해 줌 인/아웃이 배경에 자연스럽게 반영된다. 0 이면 재생 중 관측한 최대 줌아웃을 자동 추적한다.")]
    [SerializeField] private float referenceOrthographicSize = 0f;

    [Tooltip("배경이 카메라를 따라가는 정도. 0 = 월드 고정(캐릭터 이동감 최대), 1 = 카메라에 고정(이동감 없음), " +
             "중간값 = 원경 패럴랙스. 배치 위치(anchor)를 기준으로 카메라 이동분의 이 비율만큼만 따라간다.")]
    [SerializeField, Range(0f, 1f)] private float parallaxFactor = 0f;

    [Tooltip("가시 영역 대비 여유 배율. 카메라 추적 1프레임 지연이나 흔들림에도 가장자리가 드러나지 않도록 살짝 크게 덮는다.")]
    [SerializeField, Range(0f, 0.5f)] private float padding = 0.04f;

    private SpriteRenderer _renderer;

    // 배경의 배치 기준점(월드 XY). parallax 위치와 카메라 이탈량 계산의 원점.
    private Vector2 _anchor;
    private bool _anchorCaptured;

    // referenceOrthographicSize 가 0 일 때 자동 추적하는 최대 줌아웃(단조 증가).
    private float _autoMaxOrtho;
    // 카메라가 anchor 에서 벗어난 최대 거리(축별, 단조 증가). 월드 고정 시에도 이만큼 더 덮어 가장자리 노출을 막는다.
    private float _maxOffsetX;
    private float _maxOffsetY;

    // 마지막으로 스케일을 계산한 입력 캐시. 값이 그대로면 재계산을 건너뛴다.
    private float _lastRefOrtho = -1f;
    private float _lastAspect = -1f;
    private float _lastPadding = -1f;
    private float _lastMaxOffsetX = -1f;
    private float _lastMaxOffsetY = -1f;
    private Vector2 _lastSpriteSize = Vector2.zero;

    private void Awake() {
        _renderer = GetComponent<SpriteRenderer>();
        if (targetCamera == null) targetCamera = Camera.main;
        CaptureAnchor();
    }

    private void LateUpdate() => Apply();

    /// <summary>기준 줌아웃 값을 코드로 주입한다(예: BattleSettings.CameraMaxZoom). 인스펙터 수동 세팅 대신 사용 가능.</summary>
    public void SetReferenceOrthographicSize(float orthoSize) {
        referenceOrthographicSize = Mathf.Max(0f, orthoSize);
    }

    // 현재 위치를 배치 기준점으로 기록한다. parallax 이동은 이 지점을 원점으로 삼는다.
    private void CaptureAnchor() {
        _anchor = transform.position;
        _anchorCaptured = true;
    }

    /// <summary>현재 카메라 상태에 맞춰 스케일/위치를 즉시 반영한다(배경 교체 직후 등 외부 강제 갱신에도 사용 가능).</summary>
    public void Apply() {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        if (_renderer.sprite == null) return;

        if (targetCamera == null) {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }
        if (!targetCamera.orthographic) return;
        if (!_anchorCaptured) CaptureAnchor();

        // 카메라 이탈량 -> 위치 -> 스케일 순. 스케일이 이탈량을 반영하도록 먼저 이탈량을 갱신한다(같은 프레임 내 반영).
        Vector2 delta = (Vector2)targetCamera.transform.position - _anchor;
        Vector2 offset = delta * (1f - parallaxFactor); // 배경과 카메라 뷰 중심의 실제 어긋남.
        _maxOffsetX = Mathf.Max(_maxOffsetX, Mathf.Abs(offset.x));
        _maxOffsetY = Mathf.Max(_maxOffsetY, Mathf.Abs(offset.y));

        UpdateScaleIfNeeded();
        UpdatePosition(delta);
    }

    // 스케일 기준이 되는 orthoSize. 지정값이 있으면 그것을, 없으면 관측한 최대 줌아웃을 쓴다.
    private float ResolveReferenceOrtho() {
        if (referenceOrthographicSize > 0f) return referenceOrthographicSize;
        _autoMaxOrtho = Mathf.Max(_autoMaxOrtho, targetCamera.orthographicSize);
        return _autoMaxOrtho;
    }

    private void UpdateScaleIfNeeded() {
        float refOrtho = ResolveReferenceOrtho();
        float aspect = targetCamera.aspect;
        Vector2 spriteSize = _renderer.sprite.bounds.size; // 스케일 1 기준 월드 크기(PPU 반영).

        if (Mathf.Approximately(refOrtho, _lastRefOrtho)
            && Mathf.Approximately(aspect, _lastAspect)
            && Mathf.Approximately(padding, _lastPadding)
            && Mathf.Approximately(_maxOffsetX, _lastMaxOffsetX)
            && Mathf.Approximately(_maxOffsetY, _lastMaxOffsetY)
            && spriteSize == _lastSpriteSize) {
            return;
        }
        _lastRefOrtho = refOrtho;
        _lastAspect = aspect;
        _lastPadding = padding;
        _lastMaxOffsetX = _maxOffsetX;
        _lastMaxOffsetY = _maxOffsetY;
        _lastSpriteSize = spriteSize;

        if (refOrtho <= 0f || spriteSize.x <= 0f || spriteSize.y <= 0f) return;

        // 기준 뷰의 반경(half extent) + 카메라 최대 이탈량 만큼을 덮어야 안전하다.
        float viewHalfH = refOrtho * (1f + padding);
        float viewHalfW = viewHalfH * aspect;
        float requiredHalfW = viewHalfW + _maxOffsetX;
        float requiredHalfH = viewHalfH + _maxOffsetY;

        // 가로/세로 중 더 큰 배율을 취해 덮는다(cover). 등비 유지, 최대 이탈/줌아웃 기준이라 이후엔 사실상 고정된다.
        float scale = Mathf.Max(requiredHalfW / (spriteSize.x * 0.5f), requiredHalfH / (spriteSize.y * 0.5f));
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void UpdatePosition(Vector2 cameraDelta) {
        // anchor 를 원점으로 카메라 이동분의 parallaxFactor 만큼만 따라간다(0 이면 anchor 에 고정).
        Vector2 pos = _anchor + cameraDelta * parallaxFactor;
        // z 는 배경 자신의 값을 유지해 정렬 깊이를 보존한다.
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

#if UNITY_EDITOR
    // 에디터에서 재생하지 않고도 현재 게임뷰 카메라 기준으로 한 번 맞춰보기 위한 수동 프리뷰.
    [ContextMenu("Fit Now")]
    private void FitNowInEditor() {
        _renderer = GetComponent<SpriteRenderer>();
        if (targetCamera == null) targetCamera = Camera.main;
        CaptureAnchor();
        _maxOffsetX = _maxOffsetY = 0f;
        _lastSpriteSize = Vector2.zero; // 캐시 무효화 후 강제 재계산.
        Apply();
    }
#endif
}
