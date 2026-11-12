using UnityEngine;

/// <summary>
/// 앱을 내려받을 스토어 주소. 강제 업데이트 안내에서 "스토어로 이동"이 이 주소를 연다.
///
/// 플랫폼별로 주소가 다른데, 안드로이드 쪽은 패키지 이름이 곧 주소의 일부라 Application.identifier 에서
/// 그대로 가져온다(빌드 설정과 어긋날 일이 없다). 앱 스토어의 숫자 id 는 번들 id 로 유추할 수 없어
/// 값으로 적어 둔다.
///
/// market:// 나 itms-apps:// 같은 전용 스킴을 쓰지 않는 이유: 스토어 앱이 없는 기기(직접 설치한 APK 등)에서
/// 아무 일도 일어나지 않고 실패한 사실도 알 수 없다. 강제 업데이트는 이 버튼이 죽으면 게임을 아예 할 수 없는
/// 자리라, 한 번 더 눌러야 할지언정 반드시 열리는 https 주소를 쓴다(두 스토어 모두 https 주소를 자기 앱으로 연다).
///
/// 어느 스토어도 해당하지 않는 플랫폼(에디터/윈도우)은 홈페이지의 내려받기 페이지로 보낸다.
/// 그 페이지가 기기를 보고 알맞은 스토어로 다시 보내 준다.
/// </summary>
public static class StoreLink {
    /// <summary>앱 스토어의 숫자 id. 홈페이지 STORE_LINKS.appStore 와 같은 값이어야 한다.</summary>
    private const string AppleAppId = "6801899846";

    /// <summary>
    /// 홈페이지의 내려받기 페이지. 기기를 보고 알맞은 스토어로 다시 보내 준다.
    /// 스토어를 특정할 수 없는 플랫폼이 쓰고, 스토어 주소가 바뀌어도 앱을 새로 내지 않아도 되는 경로이기도 하다.
    /// </summary>
    public static string DownloadPageUrl => $"https://{WebLinkPolicy.SiteHost}/download/";

    /// <summary>이 기기에서 열어야 할 스토어 주소.</summary>
    public static string Url {
        get {
#if UNITY_ANDROID
            return $"https://play.google.com/store/apps/details?id={Application.identifier}";
#elif UNITY_IOS
            return $"https://apps.apple.com/kr/app/id{AppleAppId}";
#else
            return DownloadPageUrl;
#endif
        }
    }

    /// <summary>
    /// 스토어를 연다. 열지 못하면 false 를 돌려주므로 부르는 쪽이 안내를 이어갈 수 있다.
    /// 주소 검사는 다른 외부 링크와 같은 기준(WebLinkPolicy)을 쓴다.
    /// </summary>
    public static bool TryOpen() {
        string url = Url;
        if (!WebLinkPolicy.IsExternalBrowserAllowed(url)) {
            // 패키지 이름이 비었거나 주소 규약이 깨진 경우. 조용히 넘기면 버튼이 죽은 이유를 알 수 없다.
            Debug.LogWarning($"[StoreLink] 열 수 없는 스토어 주소입니다: {url}");
            return false;
        }

        Application.OpenURL(url);
        return true;
    }
}
