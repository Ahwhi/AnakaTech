using System;
using UnityEngine;

/// <summary>
/// 이 앱 설치를 가리키는 무작위 식별자.
///
/// 광고 ID, 기기 일련번호, SystemInfo.deviceUniqueIdentifier 같은 값을 쓰지 않는다. 그 값들은 앱을 지워도
/// 남아 사람을 계속 따라다니고, 알림을 보낼 주소를 구분하는 데 필요한 정보보다 훨씬 많은 것을 알려 준다.
/// 필요한 것은 "같은 설치인가"뿐이므로, 설치할 때 한 번 만든 무작위 UUID 로 충분하다.
///
/// 앱을 지우면 함께 사라지고, 다시 설치하면 다른 값이 된다. 그래야 서버의 기기 등록도 함께 끊긴다.
/// </summary>
public static class PushInstallation {
    private const string Key = "anaka.push.installation_id";

    private static string _cached;

    /// <summary>이 설치의 식별자. 없으면 만들어 저장한다.</summary>
    public static string Id {
        get {
            if (!string.IsNullOrEmpty(_cached)) return _cached;

            _cached = PlayerPrefs.GetString(Key, string.Empty);
            if (!string.IsNullOrEmpty(_cached)) return _cached;

            _cached = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(Key, _cached);
            PlayerPrefs.Save();
            return _cached;
        }
    }

    /// <summary>
    /// 식별자를 버린다. 다음 조회에서 새 값이 만들어진다.
    /// 서버 등록을 지운 뒤에만 부른다(먼저 버리면 서버에 남은 등록을 가리킬 방법이 사라진다).
    /// </summary>
    public static void Reset() {
        _cached = null;
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
