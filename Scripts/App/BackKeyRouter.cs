using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using VContainer.Unity;

/// <summary>
/// 안드로이드 뒤로가기 버튼의 단일 창구. 루트 컨테이너의 엔트리포인트(ITickable)로 등록되어 앱 내내 동작한다.
///
/// 안드로이드 유저는 뒤로가기가 아무 반응이 없으면 앱이 멈춘 것으로 받아들인다. 그래서 다음 한 가지 규칙만 지킨다.
///   1) 떠 있는 팝업이 있으면 가장 위의 것 하나에 넘긴다(보통 그 팝업이 닫힌다).
///   2) 넘길 팝업이 없으면 게임 종료를 한 번 확인받는다(IQuitService).
/// 뒤로가기로 곧바로 앱을 끝내지는 않는다. 확인 없이 꺼지면 오조작으로 진행 중인 플레이를 잃기 때문이다.
///
/// 종료 확인/종료 자체는 IQuitService 가 갖는다. 화면의 종료 버튼(QuitButton)과 같은 절차를 쓰므로
/// 종료 경로가 늘어도 문구나 방식이 갈라지지 않는다. 여기서는 "뒤로가기를 누구에게 줄 것인가"만 정한다.
///
/// 팝업이 무엇을 할지는 팝업이 정한다(UIView.OnBackKey). 그래서 팝업이 늘어나도 이 파일은 그대로다.
///
/// 새 입력 시스템에서 안드로이드 하드웨어 뒤로가기 버튼은 Keyboard.escapeKey 로 들어온다. 데스크톱/에디터에서는
/// 같은 키가 Esc 이므로, 별도 분기 없이 PC 에서도 같은 규칙으로 동작한다(테스트에도 그대로 쓸 수 있다).
/// </summary>
public sealed class BackKeyRouter : ITickable {
    private readonly IUIService _ui;
    private readonly IQuitService _quit;

    public BackKeyRouter(IUIService ui, IQuitService quit) {
        _ui = ui;
        _quit = quit;
    }

    void ITickable.Tick() {
        if (!WasBackKeyPressed()) return;

        // 로딩 오버레이(System 레이어)는 팝업보다 위에서 화면을 덮는다. 그 위에 종료 확인을 띄우면
        // 보이지도 눌리지도 않는 팝업이 뒤에 깔리므로, 덮여 있는 동안에는 뒤로가기를 받지 않는다.
        if (_ui.IsLoading) return;

        if (_ui.TryHandleBackKey()) return;

        _quit.RequestQuitAsync().Forget();
    }

    private static bool WasBackKeyPressed() {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
    }
}
