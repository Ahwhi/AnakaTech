#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Messaging;
using UnityEngine;
using UnityEngine.Android;

/// <summary>
/// Android 원격 알림(Firebase Cloud Messaging).
///
/// Firebase 는 Cloud Messaging 하나만 쓴다. Authentication(구글 로그인은 Firebase Auth 가 아니다),
/// Analytics, Firestore, Crashlytics 등 다른 제품은 초기화하지도 참조하지도 않는다.
///
/// 자동 초기화는 매니페스트에서 꺼 두었다(firebase_messaging_auto_init_enabled=false).
/// 앱을 켜는 것만으로 토큰이 만들어지면 알림을 원하지 않는 사용자에 대해서도 기기 식별자가 생기기 때문이다.
/// 사용자가 알림을 켜겠다고 한 뒤(AcquireTokenAsync)에만 auto-init 을 켜고 토큰을 받는다.
/// </summary>
public sealed class AndroidFcmProvider : IPushProvider, IDisposable {
    private const string PostNotifications = "android.permission.POST_NOTIFICATIONS";

    /// <summary>런타임 권한이 도입된 Android 버전(13, API 33). 그 아래는 설치만으로 알림이 허용된다.</summary>
    private const int TiramisuSdk = 33;

    private bool _initialized;
    private bool _handlersBound;
    private string _token = "";

    public bool IsSupported => true;
    public int Platform => PushRules.PlatformAndroid;
    // FCM 은 개발/배포 환경 구분이 없다. 서버가 값을 섞어 쓰지 않도록 Production 으로 고정한다.
    public int ApnsEnvironment => PushRules.ApnsProduction;

    public event Action<string> TokenRefreshed;
    public event Action<string> NotificationOpened;

    public async UniTask<bool> HasPermissionAsync(CancellationToken ct = default) {
        await UniTask.SwitchToMainThread(ct);
        if (GetSdkInt() < TiramisuSdk) return true;
        return Permission.HasUserAuthorizedPermission(PostNotifications);
    }

    public async UniTask<bool> RequestPermissionAsync(CancellationToken ct = default) {
        await UniTask.SwitchToMainThread(ct);
        if (GetSdkInt() < TiramisuSdk) return true;
        if (Permission.HasUserAuthorizedPermission(PostNotifications)) return true;

        var completion = new UniTaskCompletionSource<bool>();
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += _ => completion.TrySetResult(true);
        callbacks.PermissionDenied += _ => completion.TrySetResult(false);
        // "다시 묻지 않음"도 거부와 같게 다룬다. 앱이 되돌릴 수 없는 상태이므로 안내만 할 수 있다.
        callbacks.PermissionDeniedAndDontAskAgain += _ => completion.TrySetResult(false);

        Permission.RequestUserPermission(PostNotifications, callbacks);

        try {
            return await completion.Task.AttachExternalCancellation(ct);
        } catch (OperationCanceledException) {
            return false;
        }
    }

    public async UniTask<string> AcquireTokenAsync(CancellationToken ct = default) {
        if (!await EnsureInitializedAsync(ct)) return "";

        try {
            // 여기서 처음으로 토큰 생성을 허용한다. 이 줄 전에는 토큰이 만들어지지 않는다.
            FirebaseMessaging.TokenRegistrationOnInitEnabled = true;

            string token = await FirebaseMessaging.GetTokenAsync().AsUniTask().AttachExternalCancellation(ct);
            _token = token ?? "";
            return _token;
        } catch (OperationCanceledException) {
            return "";
        } catch (Exception e) {
            // 토큰 원문은 남기지 않는다. 실패 종류만 남긴다.
            Debug.LogWarning($"[Push/FCM] 토큰을 받지 못했습니다: {e.GetType().Name}");
            return "";
        }
    }

    public async UniTask ReleaseTokenAsync(CancellationToken ct = default) {
        _token = "";
        if (!_initialized) return;

        try {
            // 새 토큰이 자동으로 다시 만들어지지 않게 먼저 끈 뒤 지운다. 순서가 반대면 지우자마자 다시 생긴다.
            FirebaseMessaging.TokenRegistrationOnInitEnabled = false;
            await FirebaseMessaging.DeleteTokenAsync().AsUniTask().AttachExternalCancellation(ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogWarning($"[Push/FCM] 토큰 삭제 실패: {e.GetType().Name}");
        }
    }

    // Firebase 초기화. 구글 플레이 서비스가 없거나 낡은 기기에서는 실패할 수 있으며, 그때는 알림만 못 받고
    // 게임은 그대로 돌아간다.
    private async UniTask<bool> EnsureInitializedAsync(CancellationToken ct) {
        if (_initialized) return true;

        try {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask().AttachExternalCancellation(ct);
            if (status != DependencyStatus.Available) {
                Debug.LogWarning($"[Push/FCM] Firebase 를 사용할 수 없습니다: {status}");
                return false;
            }
        } catch (OperationCanceledException) {
            return false;
        } catch (Exception e) {
            Debug.LogWarning($"[Push/FCM] Firebase 초기화 실패: {e.GetType().Name}");
            return false;
        }

        if (!_handlersBound) {
            FirebaseMessaging.TokenReceived += OnTokenReceived;
            FirebaseMessaging.MessageReceived += OnMessageReceived;
            _handlersBound = true;
        }

        _initialized = true;
        return true;
    }

    private void OnTokenReceived(object sender, TokenReceivedEventArgs e) {
        if (string.IsNullOrEmpty(e.Token) || e.Token == _token) return;
        _token = e.Token;
        TokenRefreshed?.Invoke(e.Token);
    }

    // 알림을 눌러 앱이 열렸을 때만 이동 대상을 올린다. 앱이 떠 있는 동안 도착한 알림은 화면을 바꾸지 않는다
    // (보고 있던 화면이 갑자기 튀는 것이 더 나쁘다).
    private void OnMessageReceived(object sender, MessageReceivedEventArgs e) {
        var message = e?.Message;
        if (message == null) return;

        // NotificationOpenedFromBackground 가 true 인 경우가 "알림을 눌러 열림"이다.
        if (!message.NotificationOpened) return;

        string action = message.Data != null && message.Data.TryGetValue("action", out string value) ? value : "";
        // 서버가 임의의 주소를 열게 하지 않는다. 아는 값만 통과시킨다.
        if (!PushRules.IsKnownAction(action)) return;

        NotificationOpened?.Invoke(action);
    }

    private static int GetSdkInt() {
        try {
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            return version.GetStatic<int>("SDK_INT");
        } catch (Exception) {
            // 알 수 없으면 권한이 필요한 쪽으로 가정한다(요청이 한 번 더 뜨는 것이 알림이 아예 안 오는 것보다 낫다).
            return TiramisuSdk;
        }
    }

    public void Dispose() {
        if (!_handlersBound) return;
        FirebaseMessaging.TokenReceived -= OnTokenReceived;
        FirebaseMessaging.MessageReceived -= OnMessageReceived;
        _handlersBound = false;
    }
}
#endif
