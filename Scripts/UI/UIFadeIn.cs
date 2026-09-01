using DG.Tweening;
using UnityEngine;

/// <summary>
/// 붙어 있는 오브젝트가 "나타날 때" 알파 페이드인시키는 범용 연출 컴포넌트.
/// 방향(slideFrom)을 주면 그 방향에서 제자리로 살짝 밀려 들어오는 이동 연출이 페이드와 함께 재생된다.
///
/// CanvasGroup 하나만 건드리므로 대상이 Image 든 TMP_Text 든 여러 자식을 가진 그룹이든 상관없이 동작한다
/// (표시 내용을 채우는 쪽과 나타나는 연출을 분리해, 데이터 바인딩 코드가 연출을 몰라도 되게 한다).
/// CanvasGroup 은 RequireComponent 로 자동 부착되므로 인스펙터에서 따로 추가하지 않아도 된다.
///
/// 이동은 RectTransform 의 anchoredPosition 을 인스펙터에 배치된 원래 위치 기준으로 흔들었다 되돌리는 방식이라
/// 레이아웃을 건드리지 않는다. 다만 부모가 Layout Group 이면 레이아웃이 매 프레임 위치를 다시 잡아 이동이 상쇄되므로,
/// 그때는 이 컴포넌트를 레이아웃이 배치하는 오브젝트의 자식(내용 래퍼)에 붙인다.
///
/// 재생 시점은 OnEnable 이다. 즉 프리팹 생성 시는 물론이고 SetActive(true) 로 다시 켤 때마다 재생되므로,
/// 탭 전환처럼 같은 오브젝트를 껐다 켜는 UI 에서도 매번 새로 나타나는 느낌이 난다.
///
/// 팝업 안에 두는 경우: 팝업 자체(UIView)가 먼저 페이드인하므로 startDelay 를 팝업의 Fade Duration 정도로
/// 주면 팝업이 다 뜬 뒤에 내용이 떠오르는 순서가 된다. 0 이면 팝업 페이드와 동시에 진행된다.
///
/// 타임스케일 0(로딩/일시정지) 에서도 재생되도록 SetUpdate(true) 를 쓴다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class UIFadeIn : MonoBehaviour {
    /// <summary>어느 쪽에서 제자리로 들어올지. None 이면 알파만 페이드한다.</summary>
    public enum SlideFrom {
        None,
        Left,
        Right,
        Up,
        Down,
    }

    [Tooltip("페이드 시간(초).")]
    [SerializeField] private float duration = 0.25f;
    [Tooltip("페이드 시작 전 대기(초). 팝업 안에서는 팝업 Fade Duration 만큼 주면 팝업이 뜬 뒤에 나타난다.")]
    [SerializeField] private float startDelay = 0.2f;
    [Tooltip("시작 알파. 0이면 완전히 투명한 상태에서 떠오른다.")]
    [Range(0f, 1f)]
    [SerializeField] private float startAlpha = 0f;
    [Tooltip("도착 알파. 보통 1이지만 반투명하게 두고 싶으면 낮춘다.")]
    [Range(0f, 1f)]
    [SerializeField] private float targetAlpha = 1f;
    [SerializeField] private Ease ease = Ease.OutQuad;
    [Tooltip("오브젝트가 켜질 때 자동 재생. 끄면 코드에서 Play() 를 불러 직접 재생한다.")]
    [SerializeField] private bool playOnEnable = true;

    [Header("Slide (선택)")]
    [Tooltip("어느 쪽에서 들어올지. None 이면 알파만 페이드한다(기존 동작).")]
    [SerializeField] private SlideFrom slideFrom = SlideFrom.None;
    [Tooltip("제자리에서 얼마나 떨어진 곳에서 출발할지(px). 40~80 정도가 '살짝 들어오는' 느낌이다.")]
    [SerializeField] private float slideDistance = 60f;

    private CanvasGroup _group;
    private RectTransform _rect;
    private Vector2 _restPosition;   // 인스펙터에 배치된 원래 위치(= 이동 연출의 도착점).
    private bool _captured;
    private Tween _tween;

    private CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>());

    // 이동 연출을 쓸 수 있는 조건. RectTransform 이 아닌 오브젝트(월드 캔버스 밖 등)나 거리 0 이면 알파만 재생한다.
    private bool SlideEnabled => slideFrom != SlideFrom.None && _rect != null && !Mathf.Approximately(slideDistance, 0f);

    private void Awake() {
        Capture();
    }

    // 원래 위치는 연출로 옮기기 전에 딱 한 번만 읽는다. 재생 도중에 다시 읽으면 이동한 위치가 도착점이 되어
    // 재생할 때마다 오브젝트가 한 방향으로 밀려난다.
    private void Capture() {
        if (_captured) return;
        _captured = true;
        _rect = transform as RectTransform;
        if (_rect != null) _restPosition = _rect.anchoredPosition;
    }

    private void OnEnable() {
        if (playOnEnable) Play();
    }

    private void OnDisable() {
        // 꺼진 오브젝트의 트윈이 살아남아 다음 재생과 겹치지 않도록 정리한다.
        Kill();
        // 다음에 켜질 때 시작 상태부터 다시 출발하도록 되돌린다(중간 알파/위치로 멈춘 상태가 남지 않게).
        if (playOnEnable) ApplyStart();
        else RestorePosition();
    }

    /// <summary>페이드인을 처음부터 재생한다. 이미 재생 중이면 중단하고 다시 시작한다.</summary>
    public void Play() {
        Kill();
        ApplyStart();

        float time = Mathf.Max(duration, 0f);
        var seq = DOTween.Sequence();
        seq.Join(Group.DOFade(targetAlpha, time).SetEase(ease));
        if (SlideEnabled) seq.Join(_rect.DOAnchorPos(_restPosition, time).SetEase(ease));

        // 시퀀스 자체는 Linear 로 둔다. 안쪽 트윈이 이미 Ease 를 가지고 있어 시퀀스에도 걸면 이중으로 적용된다.
        _tween = seq.SetEase(Ease.Linear)
            .SetDelay(Mathf.Max(startDelay, 0f))
            .SetUpdate(true)
            .SetLink(gameObject);   // 오브젝트가 파괴되면 트윈도 함께 정리된다.
    }

    /// <summary>연출을 건너뛰고 즉시 도착 상태로 만든다(연출이 방해되는 상황용).</summary>
    public void Complete() {
        Kill();
        Capture();
        Group.alpha = targetAlpha;
        RestorePosition();
    }

    // 재생 직전 상태(시작 알파 + 출발 위치). 시작 대기(startDelay) 동안 완성된 모습이 먼저 비치지 않게 한다.
    private void ApplyStart() {
        Capture();
        Group.alpha = startAlpha;
        if (SlideEnabled) _rect.anchoredPosition = _restPosition + StartOffset();
        else RestorePosition();
    }

    // 이동 연출을 쓰지 않거나 끝난 상태의 위치. 연출 도중 방향/거리를 바꿔도 원래 자리로 돌아온다.
    private void RestorePosition() {
        if (_rect != null) _rect.anchoredPosition = _restPosition;
    }

    private Vector2 StartOffset() {
        switch (slideFrom) {
            case SlideFrom.Left:  return new Vector2(-slideDistance, 0f);
            case SlideFrom.Right: return new Vector2(slideDistance, 0f);
            case SlideFrom.Up:    return new Vector2(0f, slideDistance);
            case SlideFrom.Down:  return new Vector2(0f, -slideDistance);
            default:              return Vector2.zero;
        }
    }

    private void Kill() {
        if (_tween == null) return;
        _tween.Kill();
        _tween = null;
    }
}
