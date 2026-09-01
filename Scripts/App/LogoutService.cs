using Cysharp.Threading.Tasks;

/// <summary>
/// 로그아웃 처리(ILogoutService 구현). 루트(GameLifetimeScope)에 영속 서비스로 등록해 어느 씬에서든 호출된다.
/// 흐름: 열린 팝업 정리 -> 소켓 끊기 -> 타이틀 씬 로드(타이틀이 부트를 재실행해 재로그인으로 이어짐).
///
/// 끊김은 ConnectionGuard 도 감시하지만, 로그아웃은 의도적이라 IsLoggingOut 플래그로 표시해
/// ConnectionGuard 가 "연결 끊김" 에러 팝업을 띄우지 않도록 한다(에러 처리와 로그아웃을 분리).
/// </summary>
public sealed class LogoutService : ILogoutService {
    // ConnectionGuard 의 복귀 씬과 동일해야 한다(빌드 세팅에 포함된 타이틀 씬).
    private const string TitleSceneName = "TitleScene";

    private readonly INetworkService _net;
    private readonly ISceneService _scene;
    private readonly IUIService _ui;
    private readonly IPushCoordinator _push;

    public bool IsLoggingOut { get; private set; }

    public LogoutService(INetworkService net, ISceneService scene, IUIService ui, IPushCoordinator push) {
        _net = net;
        _scene = scene;
        _ui = ui;
        _push = push;
    }

    public async UniTask LogoutAsync() {
        if (IsLoggingOut) return;
        IsLoggingOut = true;
        try {
            // 이 기기의 푸시 발송 주소를 먼저 지운다. 소켓을 끊은 뒤에는 서버에 알릴 방법이 없어,
            // 로그아웃한 계정으로 이 기기에 알림이 계속 가게 된다.
            // 다음 로그인에서 다시 등록되므로(TermsStep) 알림을 잃지 않는다.
            await _push.DisableAsync();

            // 전투 씬 복귀용 팝업 예약이 남아 있으면 다음 로그인의 메인 진입에서 엉뚱하게 열린다.
            MainPopupReturn.Clear();
            // 로그아웃은 "다음에 다시 로그인하겠다"는 의사표시다. 자동 로그인 토큰을 남겨 두면
            // 타이틀에서 부트가 다시 돌 때 곧바로 같은 계정으로 들어가 버려 로그아웃이 무의미해진다.
            SessionTokenStore.Clear();
            _ui.CloseAllPopups();
            _net.Disconnect();                    // Disconnected -> ConnectionGuard 는 IsLoggingOut 로 건너뜀
            await _scene.LoadAsync(TitleSceneName);  // 타이틀 진입 -> 부트 재실행 -> 재로그인
        } finally {
            IsLoggingOut = false;
        }
    }
}
