using System.Threading;
using Cysharp.Threading.Tasks;
using static Enums;

/// <summary>
/// 1단계: 유저 데이터 로드 + 저장된 화면/사운드 설정 적용.
/// (AppLifecycle.Awake 는 세이브 로드 전이라 기본값으로 화면을 잡아 두고, 여기서 저장값으로 다시 적용한다.)
/// </summary>
public sealed class AppInitializeStep : IBootStep {
    private readonly ISoundService _sound;

    public BootState State => BootState.Initializing;

    public AppInitializeStep(ISoundService sound) {
        _sound = sound;
    }

    public UniTask ExecuteAsync(CancellationToken ct) {
        bool isNewUser = UserDataManager.Load();
        if (isNewUser) SetupNewUser();

        DisplaySettings.Apply();
        _sound?.Initialize();
        return UniTask.CompletedTask;
    }

    private void SetupNewUser() {
        // TODO: 신규 유저 기본 데이터 세팅(스타터 아이템 등).
        // Load() 내부에서 신규 유저는 이미 기본값으로 1회 저장하므로 여기서 Save 중복 호출하지 않는다.
    }
}
