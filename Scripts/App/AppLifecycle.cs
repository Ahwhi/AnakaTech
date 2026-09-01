using UnityEngine;

/// <summary>
/// 순수 Unity 앱 수명주기 담당(컴포지션과 분리).
/// VContainer 가 다룰 수 없는 엔진 콜백(프레임/슬립 설정, 일시정지/종료 시 저장)만 가진다.
/// 컴포지션 루트(GameLifetimeScope)와 같은 부트 오브젝트에 부착한다.
/// </summary>
public sealed class AppLifecycle : MonoBehaviour {
    // DontDestroyOnLoad 는 GameLifetimeScope.Awake 에서 처리한다(컨테이너 빌드/씬 로드 순서 보장을 위해 같은 곳에서).
    private void Awake() => ConfigureApplication();

    private void OnApplicationPause(bool pause) {
        if (pause) UserDataManager.Save();
    }

    private void OnApplicationQuit() => UserDataManager.Save();

    private static void ConfigureApplication() {
        Application.runInBackground = true;
        // 프레임/수직동기/절전/해상도는 DisplaySettings 가 저장값을 보고 정한다.
        // 이 시점은 세이브 로드 전이라 기본값이 적용되고, 로드 직후 AppInitializeStep 이 저장값으로 다시 적용한다.
        DisplaySettings.Apply();
    }
}
