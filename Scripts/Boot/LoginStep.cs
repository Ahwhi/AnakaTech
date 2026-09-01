using System.Threading;
using Cysharp.Threading.Tasks;
using static Enums;

/// <summary>
/// 5단계: 로그인(구글/애플/네이버).
/// 로그인은 사용자 상호작용이 필요하므로 이 스텝은 직접 패킷을 보내지 않는다.
/// 로딩 UI 가 BootState.LoggingIn 을 감지해 로그인 버튼을 띄우고,
/// 버튼 탭 시 LoginManager.LoginAsync(provider) 를 호출 → 성공하면 이 await 가 풀린다.
/// </summary>
public sealed class LoginStep : IBootStep {
    private readonly ILoginService _login;

    public BootState State => BootState.LoggingIn;

    public LoginStep(ILoginService login) {
        _login = login;
    }

    public async UniTask ExecuteAsync(CancellationToken ct) {
        // 한 번 로그인한 기기는 다시 로그인할 때까지 자동으로 들어간다.
        // 토큰이 없거나 만료됐으면 조용히 실패하고 아래 대화형 로그인으로 넘어간다.
        if (await _login.TryAutoLoginAsync(ct)) return;

        await _login.WaitUntilLoggedInAsync(ct);
    }
}
