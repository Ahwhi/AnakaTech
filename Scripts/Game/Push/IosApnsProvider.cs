#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// iOS 원격 알림(APNs 직접 연동).
///
/// Firebase Messaging 을 쓰지 않는다. iOS 빌드에는 Firebase 네이티브 SDK 자체가 들어가지 않으며,
/// 기기 토큰은 APNs 가 앱에 직접 발급한 device token 이다. 서버도 그 토큰으로 APNs 에 바로 보낸다.
///
/// 네이티브 쪽은 Plugins/iOS/AnakaPushNotifications.mm 이다. 기존 Apple 로그인 브리지와 웹뷰가 쓰는
/// UnityAppController 를 건드리지 않고, UNUserNotificationCenter 의 대리자만 감싸서 이어 붙인다.
/// </summary>
public sealed class IosApnsProvider : IPushProvider {
    [DllImport("__Internal")] private static extern void anakaPushRequestAuthorization();
    [DllImport("__Internal")] private static extern int anakaPushGetAuthorizationStatus();
    [DllImport("__Internal")] private static extern void anakaPushRegister();
    [DllImport("__Internal")] private static extern void anakaPushUnregister();
    [DllImport("__Internal")] private static extern IntPtr anakaPushGetToken();
    [DllImport("__Internal")] private static extern IntPtr anakaPushTakePendingAction();
    [DllImport("__Internal")] private static extern int anakaPushIsSandbox();

    /// <summary>anakaPushGetAuthorizationStatus 의 반환값. 네이티브 쪽 상수와 일치해야 한다.</summary>
    private const int StatusUnknown = -1;   // 아직 물어보지 않았거나 조회 중
    private const int StatusDenied = 0;
    private const int StatusAuthorized = 1;
    private const int StatusNotDetermined = 2;

    /// <summary>권한 응답과 토큰 발급을 기다리는 한계. 넘으면 실패로 보고 다음 기회에 다시 시도한다.</summary>
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(20);
    private const int PollMilliseconds = 100;

    private string _token = "";

    public bool IsSupported => true;
    public int Platform => PushRules.PlatformIos;

    /// <summary>
    /// 이 빌드의 APNs 환경. 네이티브가 embedded.mobileprovision 의 aps-environment 값을 읽어 판정한다.
    /// 코드가 추측하지 않고 실제 서명 결과를 따르므로, 개발 빌드와 TestFlight/App Store 빌드가 서로 다른
    /// APNs 서버를 쓰는 것이 자동으로 맞춰진다.
    /// </summary>
    public int ApnsEnvironment {
    get {
        int environment = anakaPushIsSandbox();

        if (environment == 0)
            return PushRules.ApnsProduction;

        if (environment == 1)
            return PushRules.ApnsSandbox;

        throw new InvalidOperationException(
            "[Push/APNs] aps-environment entitlement를 읽을 수 없습니다. " +
            "Xcode의 Unity-iPhone 타깃에 Push Notifications capability가 있고, " +
            "프로비저닝 프로파일에 Push Notifications가 포함됐는지 확인하세요.");
    }
}

    public event Action<string> TokenRefreshed;
    public event Action<string> NotificationOpened;

    public async UniTask<bool> HasPermissionAsync(CancellationToken ct = default) {
    await UniTask.SwitchToMainThread(ct);

    // iOS의 getNotificationSettings는 비동기이므로
    // 첫 호출의 Unknown 값을 곧바로 권한 거부로 판단하면 안 된다.
    DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);

    while (DateTime.UtcNow < deadline) {
        int status = anakaPushGetAuthorizationStatus();

        if (status == StatusAuthorized)
            return true;

        if (status == StatusDenied ||
            status == StatusNotDetermined)
            return false;

        await UniTask.Delay(
            PollMilliseconds,
            ignoreTimeScale: true,
            cancellationToken: ct);
    }

    Debug.LogWarning("[Push/APNs] iOS 알림 권한 상태 조회 시간이 초과됐습니다.");
    return false;
}

    public async UniTask<bool> RequestPermissionAsync(CancellationToken ct = default) {
        await UniTask.SwitchToMainThread(ct);

        int status = anakaPushGetAuthorizationStatus();
        if (status == StatusAuthorized) return true;
        // 한 번 거부하면 iOS 는 다시 묻지 않는다. 설정으로 안내하는 것 외에 앱이 할 수 있는 것이 없다.
        if (status == StatusDenied) return false;

        anakaPushRequestAuthorization();

        var deadline = DateTime.UtcNow + WaitLimit;
        while (DateTime.UtcNow < deadline) {
            await UniTask.Delay(PollMilliseconds, ignoreTimeScale: true, cancellationToken: ct);
            status = anakaPushGetAuthorizationStatus();
            if (status == StatusAuthorized) return true;
            if (status == StatusDenied) return false;
        }
        return false;
    }

    public async UniTask<string> AcquireTokenAsync(CancellationToken ct = default) {
        await UniTask.SwitchToMainThread(ct);

        // 등록을 요청한 뒤 APNs 가 토큰을 돌려줄 때까지 기다린다. 네트워크가 없으면 오지 않으므로 한계를 둔다.
        anakaPushRegister();

        var deadline = DateTime.UtcNow + WaitLimit;
        while (DateTime.UtcNow < deadline) {
            string token = ReadString(anakaPushGetToken());
            if (!string.IsNullOrEmpty(token)) {
                bool changed = token != _token;
                _token = token;
                if (changed) TokenRefreshed?.Invoke(token);
                return token;
            }
            await UniTask.Delay(PollMilliseconds, ignoreTimeScale: true, cancellationToken: ct);
        }

        Debug.LogWarning("[Push/APNs] 기기 토큰을 받지 못했습니다(등록 응답 없음).");
        return "";
    }

    public async UniTask ReleaseTokenAsync(CancellationToken ct = default) {
        await UniTask.SwitchToMainThread(ct);
        _token = "";
        anakaPushUnregister();
    }

    /// <summary>
    /// 알림을 눌러 열린 이동 대상을 가져온다. 앱이 꺼져 있다가 알림으로 켜진 경우를 위해 네이티브가 한 건 보관하며,
    /// 여기서 한 번 가져가면 지워진다. 주기적으로 호출하는 쪽(PushCoordinator)이 이 값을 흘려 준다.
    /// </summary>
    public void PumpPendingAction() {
        string action = ReadString(anakaPushTakePendingAction());
        if (string.IsNullOrEmpty(action)) return;
        // 서버가 임의의 주소를 열게 하지 않는다. 아는 값만 통과시킨다.
        if (!PushRules.IsKnownAction(action)) return;
        NotificationOpened?.Invoke(action);
    }

    // 네이티브가 strdup 으로 넘긴 문자열을 읽고 해제한다. 해제하지 않으면 호출마다 조금씩 샌다.
    private static string ReadString(IntPtr pointer) {
        if (pointer == IntPtr.Zero) return "";
        try {
            return Marshal.PtrToStringAnsi(pointer) ?? "";
        } finally {
            Marshal.FreeHGlobal(pointer);
        }
    }
}
#endif
