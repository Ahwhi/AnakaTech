using UnityEngine;

/// <summary>
/// 소셜 로그인에 필요한 공개 식별자 모음(ScriptableObject). 인스펙터에서 값을 넣고 LoginManager 에 할당한다.
///
/// 여기 담기는 값은 모두 "공개되어도 되는" 식별자다. 클라이언트 ID 는 인증 요청에 그대로 실려 나가므로 비밀이 아니다.
/// 반대로 네이버 client_secret 은 절대 넣지 않는다. 앱에 넣으면 추출되므로 서버가 갖고 코드 교환도 서버가 한다.
///
/// 생성: 프로젝트 창에서 Create > Anaka > Social Auth Config.
/// </summary>
[CreateAssetMenu(fileName = "SocialAuthConfig", menuName = "Anaka/Social Auth Config")]
public sealed class SocialAuthConfig : ScriptableObject {
    [Header("Google")]
    [Tooltip("Google Cloud Console 의 안드로이드 OAuth 클라이언트 ID.")]
    [SerializeField] private string googleAndroidClientId;
    [Tooltip("Google Cloud Console 의 iOS OAuth 클라이언트 ID.")]
    [SerializeField] private string googleIosClientId;

    [Header("Naver")]
    [Tooltip("네이버 개발자센터 애플리케이션의 Client ID. Client Secret 은 서버만 갖는다(앱에 넣지 말 것).")]
    [SerializeField] private string naverClientId;
    [Tooltip("네이버 콘솔에 등록한 Callback URL 과 정확히 같은 값. 네이버 콘솔이 커스텀 스킴을 받지 않으므로 " +
             "보통 서버의 HTTPS 중계 주소를 넣는다. 예: https://game.ahwhistudio.com:8443/oauth/naver")]
    [SerializeField] private string naverRedirectUri = "https://game.ahwhistudio.com:8443/oauth/naver";

    [Tooltip("로그인을 마친 브라우저가 앱을 다시 열 때 쓰는 스킴. 위 중계 주소가 이 스킴으로 넘겨준다. " +
             "안드로이드 매니페스트와 iOS URL Types 에 등록한 값과 같아야 한다.")]
    [SerializeField] private string naverAppReturnScheme = "anaka";

    public string NaverClientId => naverClientId;

    /// <summary>네이버에 보내는 redirect_uri(콘솔 등록 값과 일치해야 한다).</summary>
    public string NaverRedirectUri => naverRedirectUri;

    /// <summary>앱이 기다릴 딥링크 스킴. 중계를 거치면 네이버에 보낸 주소와 서로 다르다.</summary>
    public string NaverAppReturnScheme => naverAppReturnScheme;

    /// <summary>실행 중인 플랫폼에 맞는 구글 클라이언트 ID. 플랫폼마다 다른 ID 가 발급된다.</summary>
    public string GoogleClientId {
#if UNITY_IOS && !UNITY_EDITOR
        get => googleIosClientId;
#else
        get => googleAndroidClientId;
#endif
    }

    /// <summary>
    /// 구글이 요구하는 리다이렉트 주소. 클라이언트 ID 를 뒤집은 것이 그대로 커스텀 스킴이 된다.
    ///   370921296019-abc.apps.googleusercontent.com
    ///   -> com.googleusercontent.apps.370921296019-abc:/oauth2redirect
    /// 이 스킴을 앱이 받도록 안드로이드 매니페스트/ iOS URL Types 에 등록해야 한다.
    /// </summary>
    public string GoogleRedirectUri {
        get {
            string id = GoogleClientId;
            if (string.IsNullOrEmpty(id)) return string.Empty;
            int dot = id.IndexOf('.');
            string identifier = dot < 0 ? id : id.Substring(0, dot);
            return $"com.googleusercontent.apps.{identifier}:/oauth2redirect";
        }
    }

    /// <summary>구글 리다이렉트의 스킴 부분만(딥링크 수신 판별용).</summary>
    public string GoogleRedirectScheme {
        get {
            string uri = GoogleRedirectUri;
            int sep = uri.IndexOf(':');
            return sep < 0 ? uri : uri.Substring(0, sep);
        }
    }
}
