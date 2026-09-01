using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer.Unity;
using static Enums;

/// <summary>
/// UI 클릭음의 단일 창구. 루트 컨테이너의 엔트리포인트(ITickable)로 등록되어 앱 내내 동작한다.
///
/// 클릭음을 버튼마다 코드로 붙이면(화면 수십 개, 목록 셀까지) 새 버튼이 생길 때마다 빠뜨리게 되고,
/// 실제로 소리가 안 나는 버튼이 화면마다 남는다. 그래서 "버튼을 눌렀다"는 사실 자체를 여기서 한 번만 듣고
/// 클릭음을 낸다. 새 버튼은 아무것도 하지 않아도 소리가 난다.
///
/// 판정은 유니티의 클릭 규칙을 그대로 따른다: 누른 대상과 뗀 대상이 같은 버튼/토글일 때만 클릭으로 본다
/// (목록을 스크롤하려고 끌었다가 다른 곳에서 떼면 소리가 나지 않는다). 눌린 그래픽이 아이콘이나 글자여도
/// 부모로 올라가며 버튼을 찾으므로 카드/셀처럼 자식이 여럿인 버튼도 똑같이 잡힌다.
///
/// 슬라이더/스크롤바/입력창은 대상이 아니다(끄는 조작이라 클릭음이 어울리지 않는다).
/// 화면을 덮는 로딩 오버레이나 팝업 딤은 버튼이 아니므로 자연히 걸러진다.
///
/// 화면이 자기 사정으로 직접 클릭음을 내는 경우(기존 코드)와 겹칠 수 있는데, 두 요청은 같은 프레임에 들어오므로
/// SoundManager 의 중복 억제(같은 효과음 연속 요청은 1회)로 한 번만 울린다.
/// </summary>
public sealed class UIClickSfxRouter : ITickable {
    private readonly ISoundService _sound;
    private readonly List<RaycastResult> _results = new();

    private PointerEventData _pointer;
    private GameObject _pressed;   // 누르기 시작한 버튼(뗀 자리와 비교해 클릭인지 판단한다)

    public UIClickSfxRouter(ISoundService sound) {
        _sound = sound;
    }

    void ITickable.Tick() {
        var eventSystem = EventSystem.current;
        if (eventSystem == null) return;

        if (!TryReadPointer(out var position, out bool pressed, out bool released)) return;

        if (pressed) {
            _pressed = FindClickTarget(eventSystem, position);
            return;
        }
        if (!released) return;

        var target = FindClickTarget(eventSystem, position);
        if (target != null && target == _pressed) _sound.PlaySfx(SfxKey.ButtonClick);
        _pressed = null;
    }

    // 이번 프레임의 포인터 입력. 터치(모바일)와 마우스(에디터/PC)를 같은 규칙으로 다룬다.
    private static bool TryReadPointer(out Vector2 position, out bool pressed, out bool released) {
        var touch = Touchscreen.current;
        if (touch != null) {
            var primary = touch.primaryTouch;
            pressed = primary.press.wasPressedThisFrame;
            released = primary.press.wasReleasedThisFrame;
            position = primary.position.ReadValue();
            if (pressed || released) return true;
        }

        var mouse = Mouse.current;
        if (mouse != null) {
            pressed = mouse.leftButton.wasPressedThisFrame;
            released = mouse.leftButton.wasReleasedThisFrame;
            position = mouse.position.ReadValue();
            return pressed || released;
        }

        position = default;
        pressed = false;
        released = false;
        return false;
    }

    // 지정 좌표에서 클릭음 대상이 되는 버튼/토글을 찾는다(없으면 null).
    private GameObject FindClickTarget(EventSystem eventSystem, Vector2 position) {
        _pointer ??= new PointerEventData(eventSystem);
        _pointer.position = position;

        _results.Clear();
        eventSystem.RaycastAll(_pointer, _results);
        if (_results.Count == 0) return null;

        // 가장 위에 잡힌 그래픽에서 부모로 올라가며 첫 선택 가능 요소를 찾는다. 그것이 버튼/토글이 아니면
        // (슬라이더 손잡이 등) 클릭음 대상이 아니다. 더 위의 부모까지 계속 올라가면 슬라이더를 감싼 패널의
        // 버튼이 잘못 잡힐 수 있어 첫 요소에서 판단을 끝낸다.
        for (var t = _results[0].gameObject.transform; t != null; t = t.parent) {
            var selectable = t.GetComponent<Selectable>();
            if (selectable == null) continue;
            if (!(selectable is Button || selectable is Toggle)) return null;
            return selectable.IsInteractable() ? selectable.gameObject : null;
        }
        return null;
    }
}
