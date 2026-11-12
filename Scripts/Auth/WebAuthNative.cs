using System;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 앱 안에서 뜨는 인증 시트(ASWebAuthenticationSession) 호출 래퍼(iOS 전용).
///
/// Plugins/iOS/AnakaWebAuth.mm 이 시트를 띄우고, 로그인을 마치고 돌아온 콜백 URL 전체를 함수 포인터로 돌려준다.
/// 결과가 오는 시점은 사용자가 시트를 다루는 동안이므로, 대기 토큰(UniTaskCompletionSource)으로 이어 준다.
///
/// 이 클래스는 "브라우저를 어떻게 띄우는가"만 안다. 돌아온 주소에서 인가 코드를 꺼내고 state 를 확인하는 일은
/// 제공자와 무관한 OAuth 규칙이라 OAuthBrowserFlow 가 맡는다.
///
/// 한 번에 하나의 요청만 진행한다(로그인 버튼은 LoginManager 가 중복 탭을 막는다).
/// </summary>
public static class WebAuthNative {
    /// <summary>사용자가 시트를 닫았을 때의 오류 코드(ASWebAuthenticationSessionErrorCodeCanceledLogin).</summary>
    private const string CanceledCode = "1";

    /// <summary>
    /// 이 플랫폼에서 앱 안 인증 시트를 쓸 수 있는지. false 면 호출부가 시스템 브라우저로 대신 연다.
    /// 안드로이드에는 대응하는 표준 API 가 없고, 에디터에는 네이티브 자체가 없다.
    /// </summary>
    public static bool IsSupported {
#if UNITY_IOS && !UNITY_EDITOR
        get => true;
#else
        get => false;
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    private delegate void ResultCallback(int success, string payload);

    [DllImport("__Internal")]
    private static extern void AnakaWebAuthStart(string authorizeUrl, string callbackScheme, ResultCallback callback);

    [DllImport("__Internal")]
    private static extern void AnakaWebAuthCancel();

    // 콜백은 네이티브에서 호출되므로 정적이어야 하고, 대기 중인 요청도 정적으로 들고 있어야 한다.
    private static UniTaskCompletionSource<string> _pending;

    [AOT.MonoPInvokeCallback(typeof(ResultCallback))]
    private static void OnResult(int success, string payload) {
        var pending = _pending;
        if (pending == null) return;   // 이미 취소로 정리된 요청이다.
        _pending = null;

        if (success == 1) {
            pending.TrySetResult(payload);
            return;
        }

        // 사용자가 시트를 닫은 경우는 실패가 아니라 취소로 다룬다(로그인 화면이 그대로 유지된다).
        if (payload == CanceledCode) {
            pending.TrySetCanceled();
            return;
        }

        pending.TrySetException(new Exception($"로그인 창을 여는 데 실패했습니다(코드 {payload})."));
    }
#endif

    /// <summary>
    /// 인증 페이지를 앱 안 시트로 열고, 돌아온 콜백 URL 전체를 돌려준다.
    /// 사용자가 취소하면 OperationCanceledException 을 던진다(ISocialAuthProvider 규약).
    /// </summary>
    /// <param name="authorizeUrl">제공자의 인증 페이지 주소.</param>
    /// <param name="callbackScheme">돌아올 때 쓰는 스킴 이름만(예: anaka). 콜론이나 슬래시를 붙이지 않는다.</param>
    public static async UniTask<string> AuthenticateAsync(
        string authorizeUrl, string callbackScheme, CancellationToken ct) {
#if UNITY_IOS && !UNITY_EDITOR
        if (_pending != null) throw new InvalidOperationException("이미 진행 중인 로그인이 있습니다.");

        _pending = new UniTaskCompletionSource<string>();
        try {
            AnakaWebAuthStart(authorizeUrl, callbackScheme, OnResult);

            // 취소 요청이 오면 대기를 끊고, 화면에 떠 있는 시트도 함께 닫는다.
            // 시트를 남겨 두면 사용자가 직접 닫기 전까지 게임 화면으로 돌아갈 수 없다.
            using (ct.Register(Cancel))
                return await _pending.Task;
        } finally {
            _pending = null;
        }
#else
        await UniTask.CompletedTask;
        throw new NotSupportedException("앱 안 인증 시트는 iOS 기기에서만 사용할 수 있습니다.");
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    private static void Cancel() {
        _pending?.TrySetCanceled();
        AnakaWebAuthCancel();
    }
#endif
}
