using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 직접 조작 이동 입력(IUnitMoveInput)의 기본 구현. 터치 조이스틱을 잡고 있으면 그 방향을, 아니면 키보드
/// 방향키/WASD 를 쓴다(PC/에디터). 키보드가 없는 기기에서는 조이스틱만 남는다.
/// 유닛은 이 클래스를 모르고 IUnitMoveInput 만 읽는다.
/// </summary>
public sealed class DirectionalMoveInput : IUnitMoveInput {
    private readonly VirtualJoystick _joystick;

    /// <param name="joystick">터치 조이스틱. 없으면(null) 키보드만 쓴다.</param>
    public DirectionalMoveInput(VirtualJoystick joystick) => _joystick = joystick;

    public Vector2 Direction {
        get {
            if (_joystick != null && _joystick.IsHeld) return _joystick.Direction;
            return KeyboardDirection();
        }
    }

    private static Vector2 KeyboardDirection() {
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;

        float x = 0f, y = 0f;
        if (k.leftArrowKey.isPressed || k.aKey.isPressed) x -= 1f;
        if (k.rightArrowKey.isPressed || k.dKey.isPressed) x += 1f;
        if (k.downArrowKey.isPressed || k.sKey.isPressed) y -= 1f;
        if (k.upArrowKey.isPressed || k.wKey.isPressed) y += 1f;

        // 대각선이 더 빠르지 않게 크기를 1 로 맞춘다.
        var v = new Vector2(x, y);
        return v.sqrMagnitude > 1f ? v.normalized : v;
    }
}
