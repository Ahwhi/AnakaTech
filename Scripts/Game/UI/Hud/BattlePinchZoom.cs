using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// 두 손가락으로 전투 화면을 확대/축소한다(핀치 줌). 줌 버튼 패널(ZoomPanel)을 대신하는 조작이다.
///
/// 화면을 가리는 버튼을 두지 않는 대신, 손가락 간격이 벌어진 만큼 그대로 줌이 바뀐다. 조작이 끝나면
/// 그 값이 저장되어 다음 진입에 복원된다(저장 경로는 버튼 시절과 같은 BattleStage 다).
///
/// 카메라 포커스(어느 소녀를 따라갈지)는 건드리지 않는다. 줌만 이관한 것이라 포커스는 하던 대로 동작한다.
///
/// 입력을 가로채지 않는 것이 중요하다. 팝업이 떠 있거나 손가락이 UI 위에 있으면 아무 일도 하지 않으므로,
/// 목록을 두 손가락으로 문질러도 화면이 확대되지 않는다.
/// 줌은 전투 무대 공통 기능이라 메인 씬과 의뢰 씬이 이 컴포넌트를 그대로 공유한다(ZoomPanel 과 같은 자리).
/// </summary>
public sealed class BattlePinchZoom : MonoBehaviour {
    [Tooltip("손가락 간격이 두 배가 될 때 줌이 전체 범위의 몇 배만큼 움직일지. 클수록 조금만 벌려도 크게 변한다.")]
    [SerializeField] private float sensitivity = 1f;

    [Tooltip("이 픽셀만큼 간격이 변해야 조작으로 인정한다. 두 손가락을 그냥 올려 둔 미세한 흔들림을 걸러 낸다.")]
    [SerializeField] private float deadZonePixels = 4f;

    private BattleHudContext _ctx;

    // 이번 핀치의 기준. 손가락을 다시 짚을 때마다 새로 잡는다(이어 붙이면 값이 튄다).
    private float _startDistance;
    private float _startZoom;
    private bool _pinching;

    public void Initialize(BattleHudContext ctx) => _ctx = ctx;

    private void Update() {
        if (_ctx == null || _ctx.Stage == null) return;

        if (!TryReadPinch(out float distance)) {
            _pinching = false;
            return;
        }

        if (!_pinching) {
            _pinching = true;
            _startDistance = distance;
            _startZoom = _ctx.Stage.Zoom;
            return;   // 기준을 잡은 프레임에는 움직이지 않는다.
        }

        if (Mathf.Abs(distance - _startDistance) < deadZonePixels) return;
        if (_startDistance <= 0f) return;

        // 간격이 벌어지면(비율 > 1) 확대 = 직교 크기가 작아진다. 그래서 비율을 뺀다.
        // 화면 해상도가 달라도 조작감이 같도록 픽셀 차가 아니라 "처음 간격 대비 비율"로 환산한다.
        float ratio = distance / _startDistance;
        float delta = (ratio - 1f) * sensitivity * _ctx.Stage.ZoomRange;
        _ctx.Stage.SetZoom(_startZoom - delta);
    }

    /// <summary>
    /// 지금 두 손가락이 화면에 닿아 있으면 그 간격(픽셀)을 돌려준다. 조작으로 볼 수 없는 상황이면 false.
    ///
    /// 손가락이 UI 위에 있는지는 두 개 모두를 본다. 하나라도 UI 위에 있으면 그 제스처는 화면이 아니라
    /// UI 를 향한 것으로 보고 넘긴다(스크롤 목록 위에서 두 손가락을 쓰는 경우).
    /// </summary>
    private bool TryReadPinch(out float distance) {
        distance = 0f;

        var screen = Touchscreen.current;
        if (screen == null) return false;
        if (_ctx.Ui != null && (_ctx.Ui.HasOpenPopup || _ctx.Ui.IsLoading)) return false;

        var touches = screen.touches;
        TouchControl a = null, b = null;
        for (int i = 0; i < touches.Count; i++) {
            if (!touches[i].press.isPressed) continue;
            if (a == null) a = touches[i];
            else if (b == null) b = touches[i];
            else return false;   // 세 손가락 이상은 줌 조작으로 보지 않는다.
        }
        if (a == null || b == null) return false;

        if (IsOverUI(a) || IsOverUI(b)) return false;

        distance = Vector2.Distance(a.position.ReadValue(), b.position.ReadValue());
        return true;
    }

    private static bool IsOverUI(TouchControl touch) {
        var events = EventSystem.current;
        if (events == null) return false;
        return events.IsPointerOverGameObject(touch.touchId.ReadValue());
    }
}
