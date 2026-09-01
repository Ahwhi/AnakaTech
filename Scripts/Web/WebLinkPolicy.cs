using System;

/// <summary>
/// 앱 내부 웹뷰가 열어도 되는 주소의 판단 기준. 어떤 화면이든 웹 문서를 열기 전에 여기를 거친다.
///
/// 앱 안에서 여는 웹뷰는 사용자가 주소창을 볼 수 없다. 그래서 "지금 보고 있는 것이 우리 문서"라는 보장을
/// 앱이 대신 해야 한다. 기준은 두 가지뿐이다.
///   1) https 로만 연다(평문 http, file, content, javascript 같은 스킴은 전부 거절).
///   2) 호스트가 우리 공식 홈페이지일 때만 앱 안에서 연다. 그 외 주소는 앱 밖(시스템 브라우저)으로 보낸다.
///
/// 네이티브 웹뷰는 페이지 안의 링크를 눌렀을 때도 스스로 이동하므로, 같은 기준을 정규식으로도 만들어
/// 플러그인(WebViewObject.SetURLPattern)에 넘긴다. C# 검사만으로는 최초 주소만 막을 수 있기 때문이다.
///
/// 호스트 값은 서버 TermsCatalog.SiteBaseUrl 과 같아야 한다. 서버가 문서 주소를 내려주고 앱은 그 주소가
/// 우리 것인지만 확인하는 구조라, 문서 경로가 바뀌어도 앱은 고치지 않지만 도메인이 바뀌면 함께 고쳐야 한다.
/// </summary>
public static class WebLinkPolicy {
    /// <summary>공식 홈페이지 호스트. www 유무는 같은 곳으로 본다.</summary>
    public const string SiteHost = "ahwhistudio.com";

    private const string HostPattern = @"(www\.)?ahwhistudio\.com";

    /// <summary>앱 안에서 이동해도 되는 주소(우리 홈페이지). 나머지는 아래 두 패턴이 처리한다.</summary>
    public static readonly string AllowPattern = $@"^https://{HostPattern}/";

    /// <summary>
    /// 즉시 차단할 주소. https 가 아닌 모든 스킴이 대상이다.
    /// about:blank / about:srcdoc 은 웹뷰가 내부적으로 쓰는 값이라 예외로 둔다(막으면 빈 프레임이 깨진다).
    /// 허용 패턴이 먼저 평가되므로 우리 홈페이지는 여기에 걸리지 않는다.
    /// </summary>
    public const string DenyPattern = @"^(?!https://|about:blank|about:srcdoc)";

    /// <summary>앱 밖(시스템 브라우저)에서 열어야 하는 주소. 우리 홈페이지가 아닌 https 링크가 대상이다.</summary>
    public static readonly string HookPattern = $@"^https://(?!{HostPattern}/)";

    /// <summary>앱 내부 웹뷰로 열어도 되는 주소인지. 서버가 내려준 문서 주소를 열기 전에 확인한다.</summary>
    public static bool IsInAppAllowed(string url) {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;

        string host = uri.Host;
        return host.Equals(SiteHost, StringComparison.OrdinalIgnoreCase)
            || host.Equals("www." + SiteHost, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>시스템 브라우저로 넘겨도 되는 주소인지(외부 링크). 스킴이 https 인 것만 넘긴다.</summary>
    public static bool IsExternalBrowserAllowed(string url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;
}
