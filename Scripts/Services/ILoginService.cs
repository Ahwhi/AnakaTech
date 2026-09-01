using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 로그인 오케스트레이션 추상화. UI 는 버튼 탭에서 LoginAsync 를 호출하고,
/// 부트 LoginStep 은 WaitUntilLoggedInAsync 로 성공까지 대기한다.
/// 결과/진행 상태는 이벤트로 발행되어 구현체와 소비자가 분리된다.
/// </summary>
public interface ILoginService {
    bool IsLoggedIn { get; }
    long UserId { get; }
    string PlayerGuid { get; }
    string SessionToken { get; }

    /// <summary>
    /// 로그인 시점의 계정 표시 이름(서버가 S_Login 으로 내려준 값). 아직 지갑이 없는 신규 계정은 빈 문자열.
    /// 메인 진입 후에는 PlayerStateStore.Nickname 이 권위 값이다(닉네임 변경이 그쪽으로만 반영된다).
    /// 여기 값은 메인 진입 전(타이틀)에만 쓴다.
    /// </summary>
    string Nickname { get; }

    bool IsNewUser { get; }
    LoginProvider Provider { get; }

    event Action<LoginProvider> LoginStarted;
    event Action<LoginResult> LoginCompleted;

    UniTask<LoginResult> LoginAsync(LoginProvider provider, CancellationToken ct = default);

    /// <summary>
    /// 저장된 세션 토큰으로 소셜 인증 없이 로그인을 시도한다.
    /// true = 성공(로그인 화면을 띄우지 않아도 된다), false = 토큰이 없거나 더 이상 유효하지 않다.
    /// 실패는 정상 흐름이므로 예외를 던지지 않는다.
    /// </summary>
    UniTask<bool> TryAutoLoginAsync(CancellationToken ct = default);

    UniTask WaitUntilLoggedInAsync(CancellationToken ct);
}
