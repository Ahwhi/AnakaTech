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
///
/// PC(Google Play Games 등)에서는 마우스 휠로도 같은 줌을 조작한다. 휠은 터치와 다른 장치라 서로 간섭하지 않으며,
/// 터치 기기에는 휠 입력이 들어오지 않으므로 모바일 조작감은 그대로다.
/// </summary>
public sealed class BattlePinchZoom : MonoBehaviour {
    [Tooltip("손가락 간격이 두 배가 될 때 줌이 전체 범위의 몇 배만큼 움직일지. 클수록 조금만 벌려도 크게 변한다.")]
    [SerializeField] private float sensitivity = 1f;

    [Tooltip("이 픽셀만큼 간격이 변해야 조작으로 인정한다. 두 손가락을 그냥 올려 둔 미세한 흔들림을 걸러 낸다.")]
    [SerializeField] private float deadZonePixels = 4f;

    [Tooltip("마우스 휠 한 칸에 줌이 전체 범위의 몇 배만큼 움직일지(PC/Google Play Games 용). 0 이면 휠 줌을 끈다.")]
    [SerializeField] private float wheelStep = 0.1f;

    // 휠 값의 단위는 입력 설정에 따라 한 칸 = 약 120(플랫폼 원값) 또는 약 1(정규화)이다.
    // 이 값보다 크면 원값으로 보고 120 으로 나눠 "칸 수"로 맞춘다. 어느 설정이든 한 칸의 조작감이 같다.
    private const float RawWheelThreshold = 10f;
    private const float RawWheelPerNotch = 120f;
    // 한 프레임에 여러 칸이 몰려 들어와도(빠르게 굴림) 줌이 튀지 않도록 칸 수를 제한한다.
    private const float MaxWheelNotchesPerFrame = 3f;

    private BattleHudContext _ctx;

    // 이번 핀치의 기준. 손가락을 다시 짚을 때마다 새로 잡는다(이어 붙이면 값이 튄다).
    private float _startDistance;
    private float _startZoom;
    private bool _pinching;

    public void Initialize(BattleHudContext ctx) => _ctx = ctx;

    private void Update() {
        if (_ctx == null || _ctx.Stage == null) return;

        // 마우스 휠(PC). 터치 기기에는 휠 입력이 없어 이 분기는 항상 건너뛰며, 아래 핀치 경로는 그대로다.
        if (TryReadWheel(out float notches)) {
            // 휠을 위로(양수) 굴리면 확대 = 직교 크기가 작아진다(핀치로 벌리는 것과 같은 방향).
            _ctx.Stage.SetZoom(_ctx.Stage.Zoom - notches * wheelStep * _ctx.Stage.ZoomRange);
        }

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

    /// <summary>
    /// 이번 프레임 마우스 휠이 굴러간 칸 수(위로 양수). 마우스가 없거나 굴리지 않았거나 조작으로 볼 수 없으면 false.
    ///
    /// 핀치와 같은 조건으로 막는다. 팝업/로딩 중이거나 커서가 UI 위에 있으면 넘긴다(스크롤 목록 위에서 휠은 목록을 굴린다).
    /// </summary>
    private bool TryReadWheel(out float notches) {
        notches = 0f;
        if (wheelStep <= 0f) return false;

        var mouse = Mouse.current;
        if (mouse == null) return false;

        float raw = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(raw, 0f)) return false;
        if (_ctx.Ui != null && (_ctx.Ui.HasOpenPopup || _ctx.Ui.IsLoading)) return false;

        var events = EventSystem.current;
        if (events != null && events.IsPointerOverGameObject()) return false;

        notches = Mathf.Abs(raw) > RawWheelThreshold ? raw / RawWheelPerNotch : raw;
        notches = Mathf.Clamp(notches, -MaxWheelNotchesPerFrame, MaxWheelNotchesPerFrame);
        return true;
    }

    private static bool IsOverUI(TouchControl touch) {
        var events = EventSystem.current;
        if (events == null) return false;
        return events.IsPointerOverGameObject(touch.touchId.ReadValue());
    }
}
