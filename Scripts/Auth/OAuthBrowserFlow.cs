using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 인증 서버로 보낼 1회용 난수 묶음(PKCE).
///
/// 브라우저로 받은 인가 코드는 앱으로 돌아오는 길에 다른 앱이 가로챌 수 있다. PKCE 는 그 코드를 실제로 교환할 때
/// "처음 요청을 보낸 그 앱만 아는 값(verifier)"을 함께 요구해, 코드만 훔친 쪽은 토큰을 받지 못하게 한다.
/// state 는 돌아온 응답이 내가 보낸 요청에 대한 것인지 확인하는 값이다.
/// </summary>
public readonly struct PkceCodes {
    public string Verifier { get; }
    public string Challenge { get; }
    public string State { get; }

    private PkceCodes(string verifier, string challenge, string state) {
        Verifier = verifier;
        Challenge = challenge;
        State = state;
    }

    public static PkceCodes Create() {
        string verifier = RandomUrlSafe(32);
        string challenge;
        using (var sha = SHA256.Create())
            challenge = Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier)));
        return new PkceCodes(verifier, challenge, RandomUrlSafe(16));
    }

    private static string RandomUrlSafe(int byteCount) {
        var bytes = new byte[byteCount];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(bytes);
        return Base64Url(bytes);
    }

    // OAuth 규격이 요구하는 base64url(패딩 없음).
    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>
/// 시스템 브라우저로 로그인시키고, 앱으로 돌아오는 딥링크에서 인가 코드를 받아 오는 흐름.
///
/// 앱 안에 웹뷰를 띄우지 않고 시스템 브라우저를 쓰는 이유는, 이미 로그인된 세션을 그대로 쓸 수 있고
/// 사용자가 주소창으로 진짜 제공자 페이지임을 확인할 수 있기 때문이다(제공자들이 요구하는 방식이기도 하다).
///
/// 한 번에 하나의 로그인만 진행된다(LoginManager 가 중복 탭을 막는다). 그래서 대기 중인 요청도 하나만 둔다.
/// </summary>
public static class OAuthBrowserFlow {
    // 딥링크가 오지 않은 채 사용자가 앱으로 돌아온 뒤, 이 시간만큼 더 기다려 보고 취소로 판정한다.
    // 딥링크는 포커스 복귀와 거의 동시에 오지만 순서가 뒤바뀔 수 있어 약간의 유예를 둔다.
    private static readonly TimeSpan ReturnGrace = TimeSpan.FromSeconds(1.5);

    private static UniTaskCompletionSource<string> _pending;   // 결과: 돌아온 전체 URL
    private static string _pendingScheme;
    private static bool _subscribed;
    private static bool _leftApp;   // 브라우저가 떠서 앱이 포커스를 잃은 적이 있는지

    /// <summary>
    /// 브라우저를 열고 지정한 스킴으로 돌아올 때까지 기다린 뒤, 인가 코드를 돌려준다.
    /// 사용자가 취소하거나 응답이 어긋나면 OperationCanceledException 또는 InvalidOperationException 을 던진다.
    /// </summary>
    public static async UniTask<string> RequestAuthorizationCodeAsync(
        string authorizeUrl, string redirectScheme, string expectedState, CancellationToken ct) {

        EnsureSubscribed();

        if (_pending != null) throw new InvalidOperationException("이미 진행 중인 로그인이 있습니다.");

        _pending = new UniTaskCompletionSource<string>();
        _pendingScheme = redirectScheme;
        _leftApp = false;

        try {
            Application.OpenURL(authorizeUrl);

            string returnedUrl;
            using (ct.Register(() => _pending?.TrySetCanceled()))
                returnedUrl = await _pending.Task;

            var query = ParseQuery(returnedUrl);

            // 사용자가 동의를 거부하면 error=access_denied 로 돌아온다. 실패가 아니라 취소로 다룬다.
            if (query.TryGetValue("error", out var error)) {
                if (error == "access_denied") throw new OperationCanceledException();
                throw new InvalidOperationException($"인증 서버가 오류를 반환했습니다: {error}");
            }

            // 내가 보낸 요청에 대한 응답인지 확인한다. 다르면 다른 요청의 응답이거나 위조다.
            if (!query.TryGetValue("state", out var state) || state != expectedState)
                throw new InvalidOperationException("인증 응답의 state 가 일치하지 않습니다.");

            if (!query.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
                throw new InvalidOperationException("인증 응답에 코드가 없습니다.");

            return code;
        } finally {
            _pending = null;
            _pendingScheme = null;
        }
    }

    // 앱이 이미 떠 있는 상태로 돌아오는 경우와, 딥링크로 새로 실행되는 경우를 모두 받는다.
    private static void EnsureSubscribed() {
        if (_subscribed) return;
        Application.deepLinkActivated += OnDeepLink;
        Application.focusChanged += OnFocusChanged;
        _subscribed = true;

        // 콜드 스타트: 앱이 딥링크로 방금 실행됐다면 첫 URL 이 여기에 들어 있다.
        if (!string.IsNullOrEmpty(Application.absoluteURL)) OnDeepLink(Application.absoluteURL);
    }

    /// <summary>
    /// 브라우저에서 로그인을 마치지 않고 앱으로 돌아온 경우(뒤로가기, 홈, 로그인 실패 후 이탈)를 취소로 처리한다.
    /// 이 처리가 없으면 오지 않을 딥링크를 영원히 기다려 로딩 표시가 멈춘 채로 남고, 다시 로그인도 누를 수 없다.
    /// </summary>
    private static void OnFocusChanged(bool focused) {
        if (_pending == null) return;

        if (!focused) {
            _leftApp = true;   // 브라우저가 떴다.
            return;
        }

        if (!_leftApp) return;   // 아직 브라우저로 나간 적이 없다면 판단할 근거가 없다.
        CancelIfNoReturnAsync(_pending).Forget();
    }

    private static async UniTaskVoid CancelIfNoReturnAsync(UniTaskCompletionSource<string> pending) {
        // 딥링크로 복귀하는 경우에도 포커스 이벤트가 먼저 올 수 있어 잠깐 기다린다.
        await UniTask.Delay(ReturnGrace, DelayType.UnscaledDeltaTime);

        // 그 사이 결과가 들어왔거나 다른 로그인이 시작됐으면 건드리지 않는다.
        if (_pending != pending) return;
        pending.TrySetCanceled();
    }

    private static void OnDeepLink(string url) {
        if (_pending == null || string.IsNullOrEmpty(url)) return;
        if (!string.IsNullOrEmpty(_pendingScheme) && !url.StartsWith(_pendingScheme, StringComparison.OrdinalIgnoreCase))
            return;   // 우리가 기다리는 로그인 응답이 아니다(다른 딥링크).

        _pending.TrySetResult(url);
    }

    /// <summary>돌아온 URL 의 질의 문자열을 사전으로. 스킴이 커스텀이라 Uri 파서에 의존하지 않는다.</summary>
    private static Dictionary<string, string> ParseQuery(string url) {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        int start = url.IndexOf('?');
        if (start < 0) return result;

        foreach (var pair in url.Substring(start + 1).Split('&')) {
            if (pair.Length == 0) continue;
            int eq = pair.IndexOf('=');
            if (eq <= 0) continue;
            string key = UnityWebRequestUtil.Unescape(pair.Substring(0, eq));
            string value = UnityWebRequestUtil.Unescape(pair.Substring(eq + 1));
            result[key] = value;
        }
        return result;
    }
}

/// <summary>URL 인코딩 유틸. UnityWebRequest 의 정적 메서드를 한 곳에서만 부르도록 모아 둔다.</summary>
public static class UnityWebRequestUtil {
    public static string Escape(string value) => UnityEngine.Networking.UnityWebRequest.EscapeURL(value);
    public static string Unescape(string value) => UnityEngine.Networking.UnityWebRequest.UnEscapeURL(value);
}
