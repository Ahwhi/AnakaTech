using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 네이티브 소셜 인증 결과. 서버로 전달할 토큰만 추려 담는다(→ C_Login.authToken).
/// </summary>
public readonly struct SocialAuthResult {
    public LoginProvider Provider { get; }
    public string Token { get; }        // 서버 검증용 idToken/accessToken
    public string DisplayName { get; }  // UX 표시용(선택)

    public SocialAuthResult(LoginProvider provider, string token, string displayName = null) {
        Provider = provider;
        Token = token;
        DisplayName = displayName;
    }
}

/// <summary>
/// 제공자별 네이티브 SDK 를 감싸는 seam. LoginManager 는 이 인터페이스에만 의존.
/// 사용자가 취소하면 OperationCanceledException 을 던지는 것을 규약으로 한다.
/// </summary>
public interface ISocialAuthProvider {
    LoginProvider Provider { get; }
    UniTask<SocialAuthResult> AuthenticateAsync(CancellationToken ct);
    UniTask SignOutAsync();
}

// ─────────────────────────────────────────────────────────────
// Google — Google Sign-In(또는 Play Games)으로 idToken(JWT) 획득.
//   서버는 공개키로 aud·exp·서명 검증 후 sub 를 유저키로 사용.
// ─────────────────────────────────────────────────────────────
public sealed class GoogleAuthProvider : ISocialAuthProvider {
    private const string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenUrl = "https://oauth2.googleapis.com/token";

    private readonly SocialAuthConfig _config;

    public LoginProvider Provider => LoginProvider.Google;

    public GoogleAuthProvider(SocialAuthConfig config) {
        _config = config;
    }

    public async UniTask<SocialAuthResult> AuthenticateAsync(CancellationToken ct) {
#if UNITY_EDITOR
        // 에디터에는 딥링크가 없어 브라우저 응답을 받을 수 없다. 목 토큰으로 파이프라인만 검증한다
        // (개발 PC 서버는 Debug 빌드라 이 토큰을 통과시킨다. 운영 서버는 거절한다).
        await UniTask.Delay(200, cancellationToken: ct);
        return new SocialAuthResult(Provider, "editor-mock-google-idtoken", "GoogleTester");
#else
        if (_config == null || string.IsNullOrEmpty(_config.GoogleClientId))
            throw new InvalidOperationException("SocialAuthConfig 에 구글 클라이언트 ID 가 없습니다.");

        var pkce = PkceCodes.Create();
        string redirect = _config.GoogleRedirectUri;

        // openid 는 ID 토큰을 받기 위한 필수 범위다. 서버는 이 ID 토큰만 검증하며 프로필 API 는 부르지 않는다.
        string url = $"{AuthorizeUrl}" +
                     $"?client_id={UnityWebRequestUtil.Escape(_config.GoogleClientId)}" +
                     $"&redirect_uri={UnityWebRequestUtil.Escape(redirect)}" +
                     "&response_type=code" +
                     "&scope=openid" +
                     $"&code_challenge={pkce.Challenge}" +
                     "&code_challenge_method=S256" +
                     $"&state={pkce.State}";

        string code = await OAuthBrowserFlow.RequestAuthorizationCodeAsync(
            url, _config.GoogleRedirectScheme, pkce.State, ct);

        // 코드를 ID 토큰으로 교환한다. 안드로이드/iOS 클라이언트는 공개 클라이언트라 client_secret 이 없다.
        var form = new WWWForm();
        form.AddField("client_id", _config.GoogleClientId);
        form.AddField("code", code);
        form.AddField("code_verifier", pkce.Verifier);
        form.AddField("redirect_uri", redirect);
        form.AddField("grant_type", "authorization_code");

        using var request = UnityWebRequest.Post(TokenUrl, form);
        await request.SendWebRequest().WithCancellation(ct);

        if (request.result != UnityWebRequest.Result.Success)
            throw new Exception($"구글 토큰 교환 실패({request.responseCode})");

        var token = JsonUtility.FromJson<GoogleTokenResponse>(request.downloadHandler.text);
        if (token == null || string.IsNullOrEmpty(token.id_token))
            throw new Exception("구글 응답에 id_token 이 없습니다.");

        return new SocialAuthResult(Provider, token.id_token);
#endif
    }

    public UniTask SignOutAsync() {
        // 브라우저 세션 기반이라 앱이 들고 있는 상태가 없다. 다음 로그인 때 계정 선택 화면이 다시 뜬다.
        return UniTask.CompletedTask;
    }

    // JsonUtility 로 읽기 위한 응답 모델(필요한 항목만).
    [Serializable]
    private sealed class GoogleTokenResponse {
        public string id_token;
    }
}

// ─────────────────────────────────────────────────────────────
// Apple — Sign in with Apple(AppleAuth.unity)로 identityToken(JWT) 획득. iOS 전용.
// ─────────────────────────────────────────────────────────────
public sealed class AppleAuthProvider : ISocialAuthProvider {
    public LoginProvider Provider => LoginProvider.Apple;

    public async UniTask<SocialAuthResult> AuthenticateAsync(CancellationToken ct) {
#if UNITY_EDITOR
        await UniTask.Delay(200, cancellationToken: ct);
        return new SocialAuthResult(Provider, "editor-mock-apple-idtoken", "AppleTester");
#elif UNITY_IOS
        // 애플은 브라우저를 거치지 않는다. 시스템이 띄우는 로그인 시트에서 바로 identityToken(JWT)을 받는다.
        // 서버는 이 토큰을 애플 공개키로 검증하고 sub 를 계정키로 쓴다.
        string identityToken = await AppleAuthNative.SignInAsync(ct);
        return new SocialAuthResult(Provider, identityToken);
#else
        await UniTask.CompletedTask;
        throw new NotSupportedException("Apple 로그인은 iOS에서만 지원됩니다.");
#endif
    }

    public UniTask SignOutAsync() => UniTask.CompletedTask;
}

// ─────────────────────────────────────────────────────────────
// Naver — Naver Login SDK 로 accessToken 획득.
//   서버는 openapi.naver.com/v1/nid/me 로 검증 후 response.id 를 유저키로 사용.
// ─────────────────────────────────────────────────────────────
public sealed class NaverAuthProvider : ISocialAuthProvider {
    private const string AuthorizeUrl = "https://nid.naver.com/oauth2.0/authorize";

    private readonly SocialAuthConfig _config;

    public LoginProvider Provider => LoginProvider.Naver;

    public NaverAuthProvider(SocialAuthConfig config) {
        _config = config;
    }

    public async UniTask<SocialAuthResult> AuthenticateAsync(CancellationToken ct) {
#if UNITY_EDITOR
        await UniTask.Delay(200, cancellationToken: ct);
        return new SocialAuthResult(Provider, "editor-mock-naver-accesstoken", "NaverTester");
#else
        if (_config == null || string.IsNullOrEmpty(_config.NaverClientId))
            throw new InvalidOperationException("SocialAuthConfig 에 네이버 클라이언트 ID 가 없습니다.");

        // 네이버는 PKCE 를 쓰지 않는다. 코드 교환에 client_secret 이 필요한데 그것을 앱에 넣으면 추출되므로,
        // 여기서는 인가 코드까지만 받고 교환은 서버가 한다. state 는 서버가 교환 요청에 그대로 실어야 해서 함께 보낸다.
        var pkce = PkceCodes.Create();

        string url = $"{AuthorizeUrl}" +
                     "?response_type=code" +
                     $"&client_id={UnityWebRequestUtil.Escape(_config.NaverClientId)}" +
                     $"&redirect_uri={UnityWebRequestUtil.Escape(_config.NaverRedirectUri)}" +
                     $"&state={pkce.State}";

        // 네이버에 보내는 주소(중계 서버)와 앱이 기다리는 스킴은 서로 다르다.
        // 중계 페이지가 인가 코드를 그대로 붙여 앱 스킴으로 넘겨준다.
        string code = await OAuthBrowserFlow.RequestAuthorizationCodeAsync(
            url, _config.NaverAppReturnScheme, pkce.State, ct);

        // 서버가 code 와 state 를 함께 받아야 하므로 한 문자열로 합친다(둘 다 '|' 를 포함하지 않는 값이다).
        return new SocialAuthResult(Provider, $"{code}|{pkce.State}");
#endif
    }

    public UniTask SignOutAsync() => UniTask.CompletedTask;
}

// ─────────────────────────────────────────────────────────────
// Guest — 개발용 임시 계정. 네이티브 SDK 없이 매번 새 토큰을 만들어 보낸다.
//   서버는 계정키를 "provider:providerUserId" 로 합성하므로, 토큰이 매번 다르면 매번 새 계정이 생긴다.
//   따라서 이 계정은 한 번 나가면 다시 접속할 수 없다(그게 목적이다 - 목 로그인 3개로는
//   길드처럼 인원이 필요한 컨텐츠를 시험할 수 없다).
//
//   목 토큰은 서버가 Debug 빌드에서만 통과시키므로 운영 서버에는 통하지 않는다.
// ─────────────────────────────────────────────────────────────
public sealed class GuestAuthProvider : ISocialAuthProvider {
    public LoginProvider Provider => LoginProvider.Guest;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>이 빌드에서 임시 계정을 쓸 수 있는지. 로그인 UI 가 버튼 노출 여부를 정할 때 본다.</summary>
    public static bool IsAvailable => true;
#else
    public static bool IsAvailable => false;
#endif

    public async UniTask<SocialAuthResult> AuthenticateAsync(CancellationToken ct) {
        if (!IsAvailable) {
            await UniTask.CompletedTask;
            throw new NotSupportedException("임시 계정은 개발 빌드에서만 사용할 수 있습니다.");
        }

        // 다른 provider 의 목 토큰과 같은 접두사를 써서 서버의 개발용 통과 경로를 그대로 탄다.
        // 뒤에 붙는 값이 매번 달라야 새 계정이 생기므로 GUID 를 쓴다.
        string token = $"editor-mock-guest-{Guid.NewGuid():N}";
        await UniTask.Delay(100, cancellationToken: ct);
        return new SocialAuthResult(Provider, token, "Guest");
    }

    // 임시 계정은 폐기할 세션이 없다(다시 접속할 수 없으므로 로그아웃 개념 자체가 없다).
    public UniTask SignOutAsync() => UniTask.CompletedTask;
}
