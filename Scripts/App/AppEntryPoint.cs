using Cysharp.Threading.Tasks;
using VContainer.Unity;

/// <summary>
/// 앱 진입점. 루트 컨테이너 빌드 직후 1회 실행되어 첫 화면(TitleScene)을 로드한다.
/// 부트 오브젝트는 전용 Bootstrap 씬에 있고 DontDestroyOnLoad 라, TitleScene 을 Single 로드해도 살아남는다.
/// 첫 진입은 타이틀의 부트 스플래시가 로딩 UI 역할을 하므로 전역 로딩 오버레이는 띄우지 않는다(showLoading: false).
/// </summary>
public sealed class AppEntryPoint : IStartable {
    private const string TitleSceneName = "TitleScene";

    private readonly ISceneService _scene;

    public AppEntryPoint(ISceneService scene) => _scene = scene;

    void IStartable.Start() => _scene.LoadAsync(TitleSceneName, showLoading: false).Forget();
}
