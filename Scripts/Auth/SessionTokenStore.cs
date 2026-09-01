using UnityEngine;

/// <summary>
/// 자동 로그인 토큰 보관소. 서버가 발급한 토큰과 마지막 로그인 제공자를 기기에 남긴다.
///
/// 세이브 파일(UserDataManager)이 아니라 PlayerPrefs 를 쓰는 이유는 시점 때문이다.
/// 세이브는 계정이 정해진 뒤에 열리는데, 자동 로그인은 계정을 정하기 전에 토큰을 읽어야 한다.
///
/// 여기에 담는 값은 소셜 토큰이 아니라 우리 서버에서만 통하는 토큰이다. 서버는 해시만 갖고 있고,
/// 한 번 쓰면 새 토큰으로 바뀌므로 예전 값이 유출돼도 재사용되지 않는다.
/// 로그아웃과 탈퇴는 이 값을 지운다.
/// </summary>
public static class SessionTokenStore {
    private const string TokenKey = "Anaka.Session.Token";
    private const string ProviderKey = "Anaka.Session.Provider";

    /// <summary>저장된 토큰(없으면 빈 문자열).</summary>
    public static string Token => PlayerPrefs.GetString(TokenKey, string.Empty);

    /// <summary>마지막으로 로그인한 제공자. 재로그인 화면에서 어떤 버튼을 강조할지 정할 때 쓴다.</summary>
    public static LoginProvider Provider =>
        (LoginProvider)PlayerPrefs.GetInt(ProviderKey, (int)LoginProvider.Guest);

    /// <summary>자동 로그인을 시도할 수 있는 상태인지.</summary>
    public static bool HasToken => !string.IsNullOrEmpty(Token);

    public static void Save(string token, LoginProvider provider) {
        if (string.IsNullOrEmpty(token)) {
            Clear();
            return;
        }

        PlayerPrefs.SetString(TokenKey, token);
        PlayerPrefs.SetInt(ProviderKey, (int)provider);
        PlayerPrefs.Save();
    }

    /// <summary>토큰을 지운다. 로그아웃, 탈퇴, 그리고 서버가 토큰을 거절했을 때 호출한다.</summary>
    public static void Clear() {
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.DeleteKey(ProviderKey);
        PlayerPrefs.Save();
    }
}
