using UniRx;

/// <summary>
/// 표시 버전 제공. 앞자리는 빌드 버전(Application.version = PlayerSettings.bundleVersion),
/// 뒷자리는 어드레서블 콘텐츠 버전(RemoteAppConfig.contentVersion)이다.
///
/// Display 는 "v{Application.version}" 으로 시작하고, 부트에서 원격 설정 로드 후
/// "v{Application.version}.{contentVersion}" 으로 갱신된다. ReactiveProperty 이므로 표시 측은 구독만 하면 된다.
/// </summary>
public interface IVersionInfo {
    IReadOnlyReactiveProperty<string> Display { get; }
    void SetContentVersion(int contentVersion);
}
