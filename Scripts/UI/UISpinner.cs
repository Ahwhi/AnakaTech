using UnityEngine;

/// <summary>
/// 로딩 스피너 등 UI 요소를 일정 속도로 계속 회전시키는 범용 컴포넌트.
/// 회전시킬 오브젝트(보통 스피너 이미지)에 부착만 하면 되고, 오브젝트가 비활성화되면 자연히 멈춘다.
/// (loadingGroup 을 SetActive 로 끄면 스피너도 함께 꺼지므로 별도 정지 코드가 필요 없다.)
/// 기본값은 타임스케일 비의존(unscaled)이라 로딩/일시정지(timeScale=0) 중에도 회전한다.
/// </summary>
public sealed class UISpinner : MonoBehaviour {
    [Tooltip("초당 회전 각도(도).")]
    [SerializeField] private float degreesPerSecond = 220f;
    [Tooltip("체크 시 시계 방향으로 회전한다.")]
    [SerializeField] private bool clockwise = true;
    [Tooltip("체크 시 타임스케일의 영향을 받지 않는다(로딩/일시정지 중에도 회전).")]
    [SerializeField] private bool useUnscaledTime = true;

    private void Update() {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float delta = degreesPerSecond * dt * (clockwise ? -1f : 1f);
        transform.Rotate(0f, 0f, delta);
    }
}
