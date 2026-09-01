using UnityEngine;

/// <summary>
/// Cinemachine 없이 동작하는 순수 전투 카메라. 대표 캐릭터를 부드럽게 추적하고 오쏘 사이즈로 줌하며,
/// 대표 캐릭터 타격 시 짧은 흔들림을 준다. 추적 위치에 흔들림 오프셋을 더해 두 효과가 충돌하지 않게 한다.
/// (추후 Cinemachine 설치 시 IBattleCamera 를 구현한 다른 컴포넌트로 교체 가능.)
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class SimpleBattleCamera : MonoBehaviour, IBattleCamera {
    private Camera _cam;
    private BattleSettings _settings;
    private Transform _target;

    private float _targetZoom;
    private Vector3 _shakeOffset;
    private float _shakeRemaining;

    public void Configure(BattleSettings settings) {
        _cam = GetComponent<Camera>();
        _settings = settings;
        _cam.orthographic = true;
        _targetZoom = settings.CameraDefaultZoom;
        _cam.orthographicSize = _targetZoom;
    }

    public void SetTarget(Transform target) => _target = target;

    /// <summary>현재 목표 줌. 설정 시 허용 범위로 잘린다(저장값이 설정 변경으로 범위를 벗어나도 안전).</summary>
    public float Zoom {
        get => _targetZoom;
        set => SetZoom(value);
    }

    private void SetZoom(float value) {
        if (_settings == null) return;
        _targetZoom = Mathf.Clamp(value, _settings.CameraMinZoom, _settings.CameraMaxZoom);
    }

    public void Shake() {
        if (_settings == null) return;
        _shakeRemaining = _settings.CameraShakeDuration;
    }

    private void Update() {
        if (_settings == null) return;
        float dt = Time.deltaTime;

        if (_shakeRemaining > 0f) {
            _shakeRemaining -= dt;
            float t = Mathf.Clamp01(_shakeRemaining / _settings.CameraShakeDuration);
            Vector2 rnd = Random.insideUnitCircle * (_settings.CameraShakeAmplitude * t);
            _shakeOffset = new Vector3(rnd.x, rnd.y, 0f);
        } else {
            _shakeOffset = Vector3.zero;
        }
    }

    private void LateUpdate() {
        if (_settings == null) return;
        float dt = Time.deltaTime;

        // 흔들림 오프셋을 제외한 실제 카메라 기준 위치를 목표로 러프 추적.
        Vector3 basePos = transform.position - _shakeOffset;
        if (_target != null) {
            Vector3 goal = new(_target.position.x, _target.position.y, basePos.z);
            basePos = Vector3.Lerp(basePos, goal, _settings.CameraFollowLerp * dt);
        }
        transform.position = basePos + _shakeOffset;

        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, 10f * dt);
    }
}
