using System;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// Sign in with Apple 네이티브 호출 래퍼(iOS 전용).
///
/// Plugins/iOS/AnakaAppleAuth.mm 이 애플 로그인 시트를 띄우고, 결과를 함수 포인터로 돌려준다.
/// 결과가 오는 시점은 사용자가 시트를 다루는 동안이므로, 대기 토큰(UniTaskCompletionSource)으로 이어 준다.
///
/// 한 번에 하나의 요청만 진행한다(로그인 버튼은 LoginManager 가 중복 탭을 막는다).
/// </summary>
public static class AppleAuthNative {
    /// <summary>애플이 "사용자가 취소함"으로 돌려주는 오류 코드(ASAuthorizationErrorCanceled).</summary>
    private const string CanceledCode = "1001";

#if UNITY_IOS && !UNITY_EDITOR
    private delegate void ResultCallback(int success, string payload);

    [DllImport("__Internal")]
    private static extern void AnakaAppleAuthLogin(ResultCallback callback);

    // 콜백은 네이티브에서 호출되므로 정적이어야 하고, 대기 중인 요청도 정적으로 들고 있어야 한다.
    private static UniTaskCompletionSource<string> _pending;

    [AOT.MonoPInvokeCallback(typeof(ResultCallback))]
    private static void OnResult(int success, string payload) {
        var pending = _pending;
        if (pending == null) return;
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

        pending.TrySetException(new Exception($"애플 로그인에 실패했습니다(코드 {payload})."));
    }
#endif

    /// <summary>
    /// 애플 로그인 시트를 띄우고 identityToken(JWT)을 받는다.
    /// 사용자가 취소하면 OperationCanceledException 을 던진다(ISocialAuthProvider 규약).
    /// </summary>
    public static async UniTask<string> SignInAsync(CancellationToken ct) {
#if UNITY_IOS && !UNITY_EDITOR
        if (_pending != null) throw new InvalidOperationException("이미 진행 중인 애플 로그인이 있습니다.");

        _pending = new UniTaskCompletionSource<string>();
        try {
            AnakaAppleAuthLogin(OnResult);
            using (ct.Register(() => _pending?.TrySetCanceled()))
                return await _pending.Task;
        } finally {
            _pending = null;
        }
#else
        await UniTask.CompletedTask;
        throw new NotSupportedException("애플 로그인은 iOS 기기에서만 사용할 수 있습니다.");
#endif
    }
}
