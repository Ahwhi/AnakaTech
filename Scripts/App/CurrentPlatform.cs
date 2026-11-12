using static Enums;

/// <summary>
/// 지금 실행 중인 빌드의 플랫폼. 컴파일 심볼로 정해지므로 에디터에서는 현재 빌드 대상(Build Target)을 따른다.
/// 플랫폼 판정을 여러 곳에서 각자 하면 한쪽만 고쳐 서로 다르게 판정하는 일이 생기므로 이곳 하나로 모은다.
/// </summary>
public static class CurrentPlatform {
    public static PlatformType Value {
        get {
#if UNITY_ANDROID
            return PlatformType.Android;
#elif UNITY_IOS
            return PlatformType.IOS;
#else
            return PlatformType.Windows;
#endif
        }
    }
}
