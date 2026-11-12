using UniRx;
using UnityEngine;

/// <summary>
/// 절전 모드(PowerSaveMode) 동안 이 오브젝트의 화면 그리기를 멈춘다. 같은 오브젝트의 Canvas 와 Camera 를 대상으로 한다.
///   - Canvas: 컴포넌트만 끈다. 오브젝트와 스크립트는 그대로 살아 있어 값 갱신/구독이 끊기지 않고, 풀리면 바로 최신 상태로 보인다.
///   - Camera: 컬링 마스크를 비워 아무것도 그리지 않게 한다. 카메라 자체는 켜 둬서 화면 지우기는 계속되므로
///     잠금 화면이 반투명이어도 뒤에 이전 프레임이 남지 않고, 카메라를 찾는 다른 코드도 영향을 받지 않는다.
///
/// 전투 시뮬레이션은 그리기와 무관하게 Update 로 돌기 때문에 사냥은 그대로 진행된다.
/// 절전 모드 화면(잠금 팝업)이 올라가는 캔버스에는 붙이지 않는다. 붙이면 잠금 화면까지 함께 사라진다.
/// </summary>
public sealed class PowerSaveHidden : MonoBehaviour {
    private Canvas _canvas;
    private Camera _camera;
    private int _savedCullingMask;
    private bool _hidden;

    private void Awake() {
        _canvas = GetComponent<Canvas>();
        _camera = GetComponent<Camera>();
        if (_canvas == null && _camera == null)
            Debug.LogWarning($"[PowerSaveHidden] Canvas 나 Camera 가 없는 오브젝트에 붙어 있어 아무 일도 하지 않습니다: {name}");

        PowerSaveMode.Active.Subscribe(SetHidden).AddTo(this);
    }

    // 파괴되는 순간 절전 중이었다면 카메라 설정을 원래대로 돌려 둔다(같은 카메라를 다시 쓰는 경우 대비).
    private void OnDestroy() => SetHidden(false);

    private void SetHidden(bool hidden) {
        if (_hidden == hidden) return;
        _hidden = hidden;

        if (_canvas != null) _canvas.enabled = !hidden;

        if (_camera == null) return;
        if (hidden) {
            _savedCullingMask = _camera.cullingMask;
            _camera.cullingMask = 0;
        } else {
            _camera.cullingMask = _savedCullingMask;
        }
    }
}
