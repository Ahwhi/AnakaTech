using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 게임 종료 처리(IQuitService 구현). 루트(GameLifetimeScope)에 영속 서비스로 등록해 어느 씬에서든 호출된다.
///
/// 확인 없이 끄지 않는다. 오조작으로 진행 중인 플레이를 잃기 때문이다. 뒤로가기든 종료 버튼이든
/// 같은 확인 팝업(ConfirmCancelPopup)을 거치므로 경로가 늘어도 사용자가 겪는 절차는 하나로 유지된다.
/// </summary>
public sealed class QuitService : IQuitService {
    private const string QuitTitle = "게임 종료";
    private const string QuitMessage = "게임을 종료하시겠습니까?";

    private readonly IUIService _ui;
    private readonly UIPopupCatalog _popups;

    private bool _asking;   // 확인 팝업이 뜨는 중(프리팹 로드 포함). 연타로 여러 개가 열리지 않게 한다.

    public QuitService(IUIService ui, UIPopupCatalog popups) {
        _ui = ui;
        _popups = popups;
    }

    public async UniTask RequestQuitAsync() {
        if (_asking) return;
        _asking = true;
        try {
            var reference = _popups.ConfirmCancelPopup;
            if (reference == null || !reference.RuntimeKeyIsValid()) return;

            var popup = await _ui.ShowPopupAsync<ConfirmCancelPopup>(reference);
            if (await popup.ShowAsync(QuitTitle, QuitMessage)) Quit();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        } finally {
            _asking = false;
        }
    }

    // 에디터에서는 Application.Quit 이 아무 일도 하지 않아 종료 흐름을 확인할 수 없으므로 플레이 모드를 멈춘다.
    private static void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
