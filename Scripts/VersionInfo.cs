using UniRx;
using UnityEngine;

/// <summary>
/// IVersionInfo 구현. 앞자리는 Application.version(빌드에 구워진 PlayerSettings.bundleVersion, 예: "1.0"),
/// 뒷자리는 부트에서 어드레서블로 로드한 콘텐츠 버전이다. VContainer Singleton 으로 등록해
/// ContentVersionStep(설정)과 TitleSceneManager(표시)가 공유한다.
///
/// 주의: PlayerSettings 의 Version(bundleVersion)은 "MAJOR.MINOR"(예: 1.0)로 둔다.
/// 콘텐츠 패치 시 뒷자리(contentVersion)가 붙어 v1.0.3 처럼 표시되고, 스토어 새 빌드 때 앞자리를 올린다.
/// </summary>
public sealed class VersionInfo : IVersionInfo {
    private readonly ReactiveProperty<string> _display;

    public VersionInfo() {
        _display = new ReactiveProperty<string>($"v{Application.version}");
    }

    public IReadOnlyReactiveProperty<string> Display => _display;

    public void SetContentVersion(int contentVersion) {
        _display.Value = $"v{Application.version}.{contentVersion}";
    }
}
