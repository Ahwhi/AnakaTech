using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;
using static Enums;

/// <summary>
/// 게임 플레이 중(부트 완료 후) 서버 연결이 끊기면 먼저 화면 그대로 조용히 다시 붙이고(ISessionRecovery),
/// 그게 안 될 때만 안내 팝업을 띄우고 타이틀 화면으로 돌려보내는 전역 가드.
/// 루트 컨테이너의 엔트리포인트(IStartable)로 등록되어 앱 내내 INetworkService.Disconnected 를 감시한다.
///
/// 대표적인 경우가 앱을 잠시 내려 두었다 돌아온 것이다. 그 사이 서버가 무응답 세션을 정리하므로 돌아오는 순간
/// 끊김이 감지되는데, 이것을 오류로 보여 주지 않고 재연결한다. 업데이트/점검/토큰 만료/약관 개정처럼
/// 화면이 필요한 경우는 복구가 실패로 돌려주고 기존처럼 타이틀이 처리한다.
///
/// 끊김 감지는 두 경로로 들어온다: (1) 소켓 단절(서버 종료/정상 끊김) (2) 하트비트 타임아웃(NetworkManager 의 핑-퐁 워치독).
/// 어느 쪽이든 Disconnected 이벤트로 수렴하므로 여기서는 한 곳만 본다.
///
/// 부트 진행 중(타이틀)에는 부트 흐름이 연결 오류를 직접 처리하므로 무시한다(State==Ready 일 때만 개입).
/// 타이틀로 복귀하면 타이틀이 부트를 재실행(IBootFlow.RunAsync)하므로 자연히 재연결을 다시 시도한다.
/// </summary>
public sealed class ConnectionGuard : IStartable, IDisposable {
    private const string TitleSceneName = "TitleScene";

    private readonly INetworkService _net;
    private readonly IBootStatus _boot;
    private readonly IUIService _ui;
    private readonly ISceneService _scene;
    private readonly ILogoutService _logout;
    private readonly UIPopupCatalog _popups;
    private readonly ISessionRecovery _recovery;

    private bool _handling;   // 중복 처리 가드(복구 중 재시도가 일으키는 끊김도 여기서 걸러진다)

    public ConnectionGuard(INetworkService net, IBootStatus boot, IUIService ui, ISceneService scene, ILogoutService logout, UIPopupCatalog popups,
                           ISessionRecovery recovery) {
        _net = net;
        _boot = boot;
        _ui = ui;
        _scene = scene;
        _logout = logout;
        _popups = popups;
        _recovery = recovery;
    }

    void IStartable.Start() => _net.Disconnected += OnDisconnected;

    public void Dispose() => _net.Disconnected -= OnDisconnected;

    private void OnDisconnected() {
        if (_logout.IsLoggingOut) return;                   // 의도적 로그아웃은 LogoutService 가 처리(에러 팝업 생략)
        if (_boot.State.Value != BootState.Ready) return;   // 부트 중 끊김은 부트 흐름이 처리
        if (_handling) return;
        _handling = true;
        HandleAsync().Forget();
    }

    private async UniTaskVoid HandleAsync() {
        try {
            // 재연결 중에는 입력을 막는다. 끊긴 연결로 요청을 보내면 응답 없이 시간 초과만 나기 때문이다.
            // 떠 있던 팝업은 그대로 둔다. 붙으면 하던 화면에서 그대로 이어 간다.
            bool recovered;
            using (_ui.ShowLoading("서버에 다시 연결하는 중")) {
                recovered = await _recovery.TryRecoverAsync();
            }
            if (recovered) return;

            // 끊김 시 떠 있던 이벤트/출석/공지 팝업들을 먼저 정리한다(팝업 레이어는 씬 전환에도 살아남기 때문).
            // 전투 씬 복귀용 팝업 예약도 함께 버린다(재접속 후 메인 진입에서 엉뚱하게 열리지 않도록).
            MainPopupReturn.Clear();
            _ui.CloseAllPopups();

            var popup = await _ui.ShowPopupAsync<ConfirmPopup>(_popups.ConfirmPopup);
            // ConfirmPopup 를 재사용한다. 결과(확인/취소) 무관하게 타이틀로 이동한다(끊긴 상태라 머무를 수 없음).
            await popup.ShowAsync("연결 끊김", "서버와의 연결이 끊어졌습니다.\n타이틀 화면으로 이동합니다.");
            await _scene.LoadAsync(TitleSceneName);
        } finally {
            _handling = false;
        }
    }
}
