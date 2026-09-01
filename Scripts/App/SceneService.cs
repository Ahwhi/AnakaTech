using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using VContainer;

/// <summary>
/// 씬 전환 서비스(ISceneService 구현). 컴포지션 루트(GameLifetimeScope)에 영속 싱글톤으로 등록한다.
/// UIRoot 의 로딩 오버레이는 DontDestroyOnLoad 라 전환 중에도 유지되어, 이전 씬이 파괴되고
/// 새 씬이 올라오는 빈틈을 가려준다.
///
/// 빌트인 씬과 Addressable 씬의 차이는 "내부 로드 호출" 뿐이며(아래 두 private 메서드),
/// 로딩 오버레이 표시/해제 흐름(RunWithLoadingAsync)은 공유한다.
/// </summary>
public sealed class SceneService : ISceneService {
    private readonly IUIService _ui;

    // 현재 Addressable 로 로드돼 있는 씬 핸들(있다면). Single 전환 시 이전 핸들을 정리해 누수를 막는다.
    private AsyncOperationHandle<SceneInstance>? _currentAddressableScene;

    [Inject]
    public SceneService(IUIService ui) => _ui = ui;

    public UniTask LoadAsync(string sceneName, bool showLoading = true, CancellationToken ct = default)
        => RunWithLoadingAsync(p => LoadBuiltInAsync(sceneName, p, ct), showLoading, $"씬 {sceneName}");

    public UniTask LoadAddressableAsync(object key, bool showLoading = true, CancellationToken ct = default)
        => RunWithLoadingAsync(p => LoadAddressableInternalAsync(key, p, ct), showLoading, $"씬(어드레서블) {key}");

    // 공통 흐름: 로딩 화면을 켜고 -> 진행도를 흘려 보내며 로드를 기다리고 -> 무슨 일이 있어도 화면을 닫는다.
    //
    // 팝업들이 쓰는 ShowLoading 이 아니라 씬 전용 화면(ShowSceneLoading)을 쓴다. 씬 전환은 몇 초를
    // 붙잡아 두므로 진행도와 읽을거리(팁)가 필요한데, 그걸 공용 오버레이에 넣으면 잠깐 뜨는
    // 팝업 로딩에까지 진행도 막대가 딸려 다닌다.
    //
    // 로드 전후를 계측해 "어느 씬이 얼마나 메모리를 먹는지"를 구간으로 남긴다.
    private async UniTask RunWithLoadingAsync(Func<IProgress<float>, UniTask> loadOperation, bool showLoading, string label) {
        ISceneLoadingHandle loading = showLoading ? _ui.ShowSceneLoading() : null;
        // 로딩 화면을 띄우지 않는 전환은 진행도를 받을 곳이 없다(null 을 넘기면 로더가 알아서 건너뛴다).
        var progress = loading != null ? Progress.Create<float>(loading.Report) : null;

        try {
            MemoryProbe.Mark($"{label} 로드 전");
            await loadOperation(progress);
            MemoryProbe.Mark($"{label} 로드 후");
        } finally {
            loading?.Dispose();
        }
    }

    // 빌트인 씬: SceneManager 사용. Single 로드는 모든 씬을 언로드하므로 추적 중인 어드레서블 씬도 정리한다.
    private async UniTask LoadBuiltInAsync(string sceneName, IProgress<float> progress, CancellationToken ct) {
        await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single)
            .ToUniTask(progress, cancellationToken: ct);
        ReleaseAddressableScene(_currentAddressableScene);
        _currentAddressableScene = null;
    }

    // Addressable 씬: Addressables 사용. 새 씬을 받은 뒤 이전 어드레서블 씬 핸들을 해제한다.
    private async UniTask LoadAddressableInternalAsync(object key, IProgress<float> progress, CancellationToken ct) {
        var previous = _currentAddressableScene;

        var handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Single);
        await handle.ToUniTask(progress, cancellationToken: ct);
        _currentAddressableScene = handle;

        ReleaseAddressableScene(previous);
    }

    // Single 로드로 이미 언로드됐다면 IsValid()==false 라 건너뛴다(엔진 버전별 자동 해제 차이에 안전).
    private static void ReleaseAddressableScene(AsyncOperationHandle<SceneInstance>? handle) {
        if (handle.HasValue && handle.Value.IsValid())
            Addressables.Release(handle.Value);
    }
}
