using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 원격 알림을 쓸 수 없는 환경(에디터, PC)용 구현. 아무것도 하지 않고 실패를 사실대로 돌려준다.
///
/// 조용히 성공한 척하면 에디터에서 서버에 가짜 발송 주소가 등록되고, 그 주소로 보낸 알림은 영원히 실패한다.
/// </summary>
public sealed class NullPushProvider : IPushProvider {
    public bool IsSupported => false;
    public int Platform => PushRules.PlatformAndroid;
    public int ApnsEnvironment => PushRules.ApnsProduction;

    public event Action<string> TokenRefreshed { add { } remove { } }
    public event Action<string> NotificationOpened { add { } remove { } }

    public UniTask<bool> RequestPermissionAsync(CancellationToken ct = default) => UniTask.FromResult(false);
    public UniTask<bool> HasPermissionAsync(CancellationToken ct = default) => UniTask.FromResult(false);
    public UniTask<string> AcquireTokenAsync(CancellationToken ct = default) => UniTask.FromResult(string.Empty);
    public UniTask ReleaseTokenAsync(CancellationToken ct = default) => UniTask.CompletedTask;
}

/// <summary>
/// 실행 중인 플랫폼에 맞는 구현을 고른다. 조건부 컴파일을 여기 한 곳에만 두어
/// 나머지 코드가 플랫폼을 몰라도 되게 한다.
/// </summary>
public static class PushProviderFactory {
    public static IPushProvider Create() {
#if UNITY_ANDROID && !UNITY_EDITOR
        return new AndroidFcmProvider();
#elif UNITY_IOS && !UNITY_EDITOR
        return new IosApnsProvider();
#else
        return new NullPushProvider();
#endif
    }
}
