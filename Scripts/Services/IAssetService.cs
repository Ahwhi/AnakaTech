using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

/// <summary>
/// 어드레서블 자산 로드/인스턴스화/다운로드 추상화. 로드한 핸들의 수명 관리(메모리 해제)는
/// 구현체가 담당하며, 소비자는 이 계약을 통해서만 자산에 접근한다.
/// </summary>
public interface IAssetService {
    UniTask InitializeAsync(CancellationToken ct = default);

    // 부트 프리로드(preload_boot 라벨) 총 다운로드 용량(바이트). 내부에서 카탈로그 갱신을 보장한다. 0 이면 받을 것 없음.
    UniTask<long> GetPreloadDownloadSizeAsync(CancellationToken ct = default);

    // 부트 프리로드 다운로드. 진행 정보(받은/전체 바이트 + 퍼센트)를 콜백으로 통지한다.
    UniTask DownloadPreloadAsync(Action<DownloadInfo> onProgress = null, CancellationToken ct = default);

    UniTask<T> LoadAssetAsync<T>(AssetReference reference, CancellationToken ct = default) where T : Object;

    UniTask<GameObject> InstantiateAsync(AssetReference reference, Transform parent = null, CancellationToken ct = default);

    void ReleaseInstance(GameObject instance);
    void Release(object key);
    void ReleaseAll();
}
