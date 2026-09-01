using DG.Tweening;
using UnityEngine;

/// <summary>
/// 오브젝트를 한 축의 두 각도 사이로 부드럽게 왕복 회전시키는 범용 연출 컴포넌트(DOTween).
/// 회전시킬 오브젝트(아이콘 등)에 부착만 하면 되고, 비활성화되면 트윈이 정리되고 시작 각도로 복원된다.
/// 조각 아이콘 흔들림처럼 어느 팝업/오브젝트에서나 재사용한다(UISpinner 와 동일한 부착형 컴포넌트).
/// </summary>
public sealed class UIRotateWobble : MonoBehaviour {
    public enum Axis { X, Y, Z }

    [Tooltip("왕복 회전시킬 축.")]
    [SerializeField] private Axis axis = Axis.Z;
    [Tooltip("왕복의 시작 각도(도).")]
    [SerializeField] private float fromAngle = -15f;
    [Tooltip("왕복의 끝 각도(도).")]
    [SerializeField] private float toAngle = -18f;
    [Tooltip("한 방향(시작 -> 끝)에 걸리는 시간(초).")]
    [SerializeField] private float duration = 0.7f;
    [Tooltip("가감속 곡선.")]
    [SerializeField] private Ease ease = Ease.InOutSine;
    [Tooltip("체크 시 타임스케일의 영향을 받지 않는다(일시정지 중에도 회전).")]
    [SerializeField] private bool useUnscaledTime = false;

    private Tween _tween;
    private Vector3 _baseEuler;
    private bool _baseCaptured;

    private void OnEnable() => Play();
    private void OnDisable() => Stop();

    /// <summary>왕복 회전을 시작한다(이미 재생 중이면 재시작).</summary>
    public void Play() {
        CaptureBase();
        _tween?.Kill();

        transform.localRotation = Quaternion.Euler(WithAxis(fromAngle));
        _tween = transform
            .DOLocalRotate(WithAxis(toAngle), duration)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject);
    }

    /// <summary>왕복 회전을 멈추고 시작(원래) 각도로 되돌린다.</summary>
    public void Stop() {
        _tween?.Kill();
        _tween = null;
        if (_baseCaptured) transform.localRotation = Quaternion.Euler(_baseEuler);
    }

    // 최초 1회, 부착 시점의 회전을 원본으로 캐시한다(정지 시 복원 기준).
    private void CaptureBase() {
        if (_baseCaptured) return;
        _baseEuler = transform.localEulerAngles;
        _baseCaptured = true;
    }

    // 지정한 축만 angle 로 바꾸고 나머지 축은 원본 각도를 유지한 euler 를 만든다.
    private Vector3 WithAxis(float angle) {
        var e = _baseEuler;
        switch (axis) {
            case Axis.X: e.x = angle; break;
            case Axis.Y: e.y = angle; break;
            default: e.z = angle; break;
        }
        return e;
    }
}
