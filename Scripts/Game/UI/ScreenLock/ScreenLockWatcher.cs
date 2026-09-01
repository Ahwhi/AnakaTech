using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 자동 화면 잠금 감시. 일정 시간 입력이 없으면 잠금(잠수) 화면을 띄운다.
/// 대기 시간은 설정값(GameData.AutoLockSeconds)이며 0 이면 자동 잠금을 쓰지 않는다.
///
/// 수동 잠금은 메뉴의 잠금 버튼(HudMenuButton)이 같은 팝업을 직접 열기 때문에 이 컴포넌트를 거치지 않는다.
/// 두 경로가 겹치는 일은 "팝업이 떠 있으면 잠그지 않는다"는 조건 하나로 막힌다.
///
/// 메인 HUD 아무 오브젝트에나 하나 붙여 두면 된다(자가 주입이라 별도 등록/참조가 필요 없다).
/// </summary>
public sealed class ScreenLockWatcher : HudInjectedBehaviour {
    private float _idleSeconds;
    private bool _opening;   // 팝업이 뜨는 중(아직 HasOpenPopup 이 서기 전) 중복 요청 방지.

    private void Update() {
        if (Ctx == null) return;

        int limit = UserDataManager.Data != null ? UserDataManager.Data.AutoLockSeconds : 0;

        // 자동 잠금을 껐거나, 이미 무언가 떠 있거나, 방금 입력이 있었으면 무입력 시간을 처음으로 되돌린다.
        // (다른 팝업을 열어 둔 채 두는 것은 "보고 있는 중"이라 잠글 상황이 아니다.)
        if (limit <= 0 || _opening || Ctx.Ui.HasOpenPopup || HasInput()) {
            _idleSeconds = 0f;
            return;
        }

        _idleSeconds += Time.unscaledDeltaTime;
        if (_idleSeconds < limit) return;

        _idleSeconds = 0f;
        LockAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid LockAsync(CancellationToken ct) {
        var reference = Ctx.Popups.ScreenLockPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;

        _opening = true;
        try {
            var popup = await Ctx.Ui.ShowPopupAsync<ScreenLockPopup>(reference, ct);
            popup.Initialize(Ctx);
            await popup.ShowAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        } finally {
            // 이후 중복 방지는 HasOpenPopup 이 이어받는다(잠금이 풀릴 때까지 계속 서 있다).
            _opening = false;
        }
    }

    // 터치/포인터/키 중 무엇이든 만지고 있으면 "입력 있음"으로 본다.
    // EventSystem 을 거치지 않고 장치를 직접 읽으므로 UI 위든 게임 화면이든 똑같이 잡힌다.
    private static bool HasInput() {
        var touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.press.isPressed) return true;

        var pointer = Pointer.current;
        if (pointer != null && (pointer.press.isPressed || pointer.delta.ReadValue().sqrMagnitude > 0f)) return true;

        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.anyKey.isPressed;
    }
}
