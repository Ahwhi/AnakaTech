using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

/// <summary>
/// 어드레서블 초기화/카탈로그 갱신, 프리로드 라벨 다운로드(진행률 콜백),
/// 자산 로드/인스턴스화와 자동 해제(메모리 관리)를 담당하는 IAssetService 구현체.
/// 소비자는 IAssetService 를 주입받아 접근한다.
/// </summary>
public class AddressableManager : MonoBehaviour, IAssetService {
    [Header("Boot Preload")]
    [Tooltip("부트 시 미리 다운로드할 라벨 목록. 라벨을 추가하기만 하면 자동으로 부트 단계에서 사전 다운로드된다.")]
    [SerializeField] private List<AssetLabelReference> preloadLabels = new();

    private bool _initialized;

    // 로드/인스턴스 핸들을 보관해 종료 시 일괄 해제(메모리 누수 방지)한다.
    private readonly Dictionary<object, AsyncOperationHandle> _assetHandles = new();
    private readonly HashSet<GameObject> _instances = new();
    // 인스턴스 -> 그 인스턴스를 만든 핸들. 해제할 때 "이 오브젝트를 만든 바로 그 핸들"을 써야
    // 같은 프리팹이 여러 개 떠 있어도 엉뚱한 것이 해제되거나 해제가 실패하지 않는다.
    private readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> _instanceHandles = new();

    private void OnDestroy() => ReleaseAll();

    // 초기화 + 카탈로그 갱신 (최초 1회) ----------------------------------------
    public async UniTask InitializeAsync(CancellationToken ct = default) {
        if (_initialized) return;

        await Addressables.InitializeAsync().ToUniTask(cancellationToken: ct);

        var checkHandle = Addressables.CheckForCatalogUpdates(autoReleaseHandle: false);
        try {
            var catalogs = await checkHandle.ToUniTask(cancellationToken: ct);
            if (catalogs != null && catalogs.Count > 0) {
                await Addressables.UpdateCatalogs(catalogs, autoReleaseHandle: true)
                    .ToUniTask(cancellationToken: ct);
            }
        } finally {
            if (checkHandle.IsValid()) Addressables.Release(checkHandle);
        }

        _initialized = true;
    }

    // 부트 프리로드: 총 다운로드 용량(바이트). 카탈로그 갱신을 보장한 뒤 preload_boot 키들의 합산 용량을 구한다.
    // 0 이면 받을 것 없음(동의 팝업 없이 통과).
    public async UniTask<long> GetPreloadDownloadSizeAsync(CancellationToken ct = default) {
        await InitializeAsync(ct);

        var keys = GetPreloadKeys();
        if (keys.Count == 0) return 0;

        return await GetDownloadSizeAsync(keys, ct);
    }

    // 부트 프리로드 다운로드. 진행 정보(받은/전체 바이트 + 퍼센트)를 콜백으로 통지한다.
    public async UniTask DownloadPreloadAsync(Action<DownloadInfo> onProgress = null, CancellationToken ct = default) {
        await InitializeAsync(ct);

        var keys = GetPreloadKeys();
        if (keys.Count == 0) {
            onProgress?.Invoke(new DownloadInfo(0, 0, 1f));   // 받을 라벨 없음 - 즉시 완료
            return;
        }

        long size = await GetDownloadSizeAsync(keys, ct);
        if (size <= 0) {
            Debug.Log("[Addressable] 받을 내용 없음 (이미 캐시됨).");
            onProgress?.Invoke(new DownloadInfo(0, 0, 1f));   // 이미 캐시됨
            return;
        }

        Debug.Log($"[Addressable] 다운로드 시작: 총 {DownloadInfo.FormatBytes(size)}");

        var handle = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union, false);
        try {
            while (!handle.IsDone) {
                ct.ThrowIfCancellationRequested();
                var status = handle.GetDownloadStatus();
                onProgress?.Invoke(new DownloadInfo(status.DownloadedBytes, status.TotalBytes, status.Percent));
                await UniTask.Yield(ct);
            }

            if (handle.Status == AsyncOperationStatus.Failed)
                throw handle.OperationException ?? new Exception("Addressable download failed.");

            onProgress?.Invoke(new DownloadInfo(size, size, 1f));
            Debug.Log($"[Addressable] 다운로드 완료: {DownloadInfo.FormatBytes(size)}");
        } finally {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    // 다운로드 용량(여러 키 합산) - 내부용.
    private async UniTask<long> GetDownloadSizeAsync(IEnumerable<object> keys, CancellationToken ct) {
        var handle = Addressables.GetDownloadSizeAsync(keys);
        try {
            return await handle.ToUniTask(cancellationToken: ct);
        } finally {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    // AssetReference 인스턴스화 (인스펙터 할당용) ------------------------------
    /// <summary>
    /// 프리팹을 인스턴스화한다. 핸들은 이 매니저가 인스턴스별로 들고 있다가 ReleaseInstance 에서 그대로 해제한다.
    ///
    /// AssetReference.InstantiateAsync 를 쓰지 않는 이유: 그 API 는 생성 핸들을 AssetReference 객체 자신에
    /// 저장한다. 그런데 여기서 넘어오는 AssetReference 는 UIPopupCatalog(ScriptableObject)의 필드라
    /// 앱 전체가 같은 객체를 공유한다. 같은 참조로 두 번째 인스턴스를 만드는 순간(팝업을 닫는 중에 다시 열거나,
    /// 확인 팝업처럼 동시에 두 개가 뜨는 경우) 먼저 만든 핸들이 덮어써지고, 그 뒤 ReleaseInstance 는
    /// 대상을 찾지 못해 조용히 실패한다. 파괴되지 않은 팝업이 씬 전환에도 살아남아
    /// "내용이 비어 있고 버튼도 먹지 않는 유령 팝업"으로 남는 원인이 이것이다.
    /// </summary>
    public async UniTask<GameObject> InstantiateAsync(AssetReference reference, Transform parent = null, CancellationToken ct = default) {
        var handle = Addressables.InstantiateAsync(reference.RuntimeKey, parent);
        try {
            var go = await handle.ToUniTask(cancellationToken: ct);
            _instances.Add(go);
            _instanceHandles[go] = handle;
            return go;
        } catch {
            if (handle.IsValid()) Addressables.Release(handle);
            throw;
        }
    }

    public async UniTask<T> LoadAssetAsync<T>(AssetReference reference, CancellationToken ct = default) where T : Object {
        if (_assetHandles.TryGetValue(reference.RuntimeKey, out var cached) && cached.IsValid())
            return (T)cached.Result;

        var handle = reference.LoadAssetAsync<T>();
        try {
            var result = await handle.ToUniTask(cancellationToken: ct);
            _assetHandles[reference.RuntimeKey] = handle;
            return result;
        } catch {
            if (handle.IsValid()) Addressables.Release(handle);
            throw;
        }
    }

    // 해제 --------------------------------------------------------------------
    /// <summary>
    /// 인스턴스를 해제한다(오브젝트 파괴 포함). 생성 때 받아 둔 핸들로 해제하므로 같은 프리팹이 여러 개 떠 있어도
    /// 서로를 간섭하지 않는다.
    ///
    /// 핸들을 잃은 경우(앱 종료 순서 등)에도 오브젝트는 반드시 사라져야 한다. 해제에 실패했다고 그냥 두면
    /// 화면에 남아 입력을 먹는 유령 오브젝트가 되므로, 마지막에는 직접 파괴한다.
    /// </summary>
    public void ReleaseInstance(GameObject instance) {
        if (instance == null) return;
        _instances.Remove(instance);

        if (_instanceHandles.TryGetValue(instance, out var handle)) {
            _instanceHandles.Remove(instance);
            if (handle.IsValid() && Addressables.ReleaseInstance(handle)) return;
        }

        if (instance != null) Destroy(instance);
    }

    public void Release(object key) {
        if (!_assetHandles.TryGetValue(key, out var handle)) return;
        _assetHandles.Remove(key);
        if (handle.IsValid()) Addressables.Release(handle);
    }

    public void ReleaseAll() {
        // 생성 때 받아 둔 핸들로 해제한다(ReleaseInstance 와 같은 이유).
        foreach (var kv in _instanceHandles)
            if (kv.Value.IsValid()) Addressables.ReleaseInstance(kv.Value);
        _instanceHandles.Clear();
        _instances.Clear();

        foreach (var handle in _assetHandles.Values)
            if (handle.IsValid()) Addressables.Release(handle);
        _assetHandles.Clear();
    }

    // 내부 --------------------------------------------------------------------
    private List<object> GetPreloadKeys() {
        var keys = new List<object>(preloadLabels.Count);
        foreach (var label in preloadLabels) {
            if (label != null && !string.IsNullOrEmpty(label.labelString))
                keys.Add(label.labelString);
        }
        return keys;
    }
}
