using UniRx;
using UnityEngine;

/// <summary>
/// 순수 Unity 앱 수명주기 담당(컴포지션과 분리).
/// VContainer 가 다룰 수 없는 엔진 콜백(프레임/슬립 설정, 일시정지/종료 시 저장)만 가진다.
/// 컴포지션 루트(GameLifetimeScope)와 같은 부트 오브젝트에 부착한다.
/// </summary>
public sealed class AppLifecycle : MonoBehaviour {
    // DontDestroyOnLoad 는 GameLifetimeScope.Awake 에서 처리한다(컨테이너 빌드/씬 로드 순서 보장을 위해 같은 곳에서).
    private void Awake() {
        ConfigureApplication();

        // 절전 모드가 바뀌면 화면 설정을 다시 적용한다(프레임/수직동기 상한은 DisplaySettings 가 절전 여부를 보고 정한다).
        // 현재 값은 위에서 이미 적용했으므로 바뀔 때만 받는다.
        PowerSaveMode.Active.Skip(1).Subscribe(_ => DisplaySettings.Apply()).AddTo(this);
    }

    private void OnApplicationPause(bool pause) {
        if (pause) UserDataManager.Save();
    }

    private void OnApplicationQuit() {
        UserDataManager.Save();

        // 저장이 끝나면 엔진 정리를 기다리지 않고 프로세스를 끝낸다. iOS 엔진 버그 회피이며
        // 이유는 NativeProcessExit 주석 참고. 다른 플랫폼에서는 이 호출이 무시된다.
        //
        // 이 줄 아래로는 다른 컴포넌트의 OnApplicationQuit 도, OnDisable/OnDestroy 도 실행되지 않는다.
        // 종료 시 반드시 남겨야 하는 처리가 생기면 이 줄 위에 둘 것.
        NativeProcessExit.Now();
    }

    private static void ConfigureApplication() {
        Application.runInBackground = true;
        // 프레임/수직동기/절전/해상도는 DisplaySettings 가 저장값을 보고 정한다.
        // 이 시점은 세이브 로드 전이라 기본값이 적용되고, 로드 직후 AppInitializeStep 이 저장값으로 다시 적용한다.
        DisplaySettings.Apply();
    }
}
