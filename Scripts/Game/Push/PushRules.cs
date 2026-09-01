/// <summary>
/// 원격 푸시의 클라이언트 상수와 문구. 값은 서버와 반드시 일치해야 한다.
///
/// 이 게임의 원격 알림은 오프라인 보상 최대치 안내 하나뿐이고 그것은 광고성 정보다.
/// 그래서 "앱 알림 설정"을 따로 두지 않는다. 광고성 정보 수신 동의(약관)가 곧 원격 알림 사용 여부이며,
/// 동의를 끄면 보낼 것이 하나도 남지 않는다. 설정을 하나 더 만들면 두 값이 어긋날 때
/// 어느 쪽이 진짜인지 알 수 없게 된다.
/// </summary>
public static class PushRules {
    /// <summary>서버 DataModel.PushPlatform 과 값이 일치해야 한다.</summary>
    public const int PlatformAndroid = 0;
    public const int PlatformIos = 1;

    /// <summary>서버 DataModel.ApnsEnvironment 와 값이 일치해야 한다.</summary>
    public const int ApnsSandbox = 0;
    public const int ApnsProduction = 1;

    /// <summary>서버 PushEndpointRepository.Fail 과 값이 일치해야 한다.</summary>
    public const int FailNone = 0;
    public const int FailNotAuthenticated = 1;
    public const int FailInvalidValue = 2;
    public const int FailError = 3;

    /// <summary>
    /// 알림을 눌렀을 때 앱이 갈 수 있는 곳. 서버 Push.PushAction 과 값이 일치해야 하며,
    /// 여기 없는 값이 오면 무시한다(서버가 임의의 주소를 열게 하지 않는다).
    /// </summary>
    public const string ActionOfflineReward = "offline_reward";
    public const string ActionMain = "main";

    public static bool IsKnownAction(string action) =>
        action == ActionOfflineReward || action == ActionMain;

    /// <summary>OS 알림 권한이 거부돼 있을 때의 안내. 앱이 직접 권한을 되돌릴 수 없으므로 설정으로 안내한다.</summary>
    public const string PermissionDeniedMessage =
        "기기 설정에서 아나카의 알림이 꺼져 있어 푸시를 받을 수 없습니다.\n" +
        "설정 > 앱 > 아나카 > 알림에서 허용해 주세요.";
}
