using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 플랫폼별 원격 알림 창구. Android 는 FCM, iOS 는 APNs 에 직접 붙는다.
///
/// 이 인터페이스가 있어야 위쪽(PushCoordinator)이 "동의가 있고 권한이 있으면 토큰을 등록한다"는
/// 한 가지 규칙만 갖고, 플랫폼 차이는 전부 아래에서 끝난다. 에디터에서는 아무것도 하지 않는 구현이 들어온다.
/// </summary>
public interface IPushProvider {
    /// <summary>이 환경에서 원격 알림을 쓸 수 있는지. 에디터/PC 는 false 다.</summary>
    bool IsSupported { get; }

    /// <summary>서버에 알려 줄 플랫폼 값(PushRules.PlatformAndroid / PlatformIos).</summary>
    int Platform { get; }

    /// <summary>APNs 환경(PushRules.ApnsSandbox / ApnsProduction). Android 는 Production 으로 고정한다.</summary>
    int ApnsEnvironment { get; }

    /// <summary>
    /// OS 알림 권한을 요청한다. 이미 허용돼 있으면 바로 true 를 돌려준다.
    /// 사용자가 거부했고 다시 물을 수 없는 상태면 false 다(앱이 강제로 되돌릴 수 없다).
    /// </summary>
    UniTask<bool> RequestPermissionAsync(CancellationToken ct = default);

    /// <summary>지금 알림을 띄울 수 있는 상태인지(권한 허용 여부).</summary>
    UniTask<bool> HasPermissionAsync(CancellationToken ct = default);

    /// <summary>
    /// 발송 주소 토큰을 받는다. 아직 준비되지 않았거나 실패하면 빈 문자열이다.
    /// 이 호출이 있기 전에는 토큰이 만들어지지 않아야 한다(동의 전 자동 생성 방지).
    /// </summary>
    UniTask<string> AcquireTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// 로컬 토큰을 폐기하고 원격 알림 등록을 해제한다. 사용자가 알림을 끌 때 부른다.
    /// 플랫폼이 지원하지 않으면 할 수 있는 데까지만 한다.
    /// </summary>
    UniTask ReleaseTokenAsync(CancellationToken ct = default);

    /// <summary>토큰이 갱신됐을 때. 서버 등록을 새 값으로 바꿔야 한다.</summary>
    event Action<string> TokenRefreshed;

    /// <summary>알림을 눌러 앱이 열렸을 때의 이동 대상(PushRules 의 action 값).</summary>
    event Action<string> NotificationOpened;
}
