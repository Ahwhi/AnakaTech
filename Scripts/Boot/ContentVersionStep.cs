using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using static Enums;

/// <summary>
/// 어드레서블 다운로드 직후: 원격 앱 설정(RemoteAppConfig)을 로드해 콘텐츠 버전을 표시 버전에 반영한다.
/// 콘텐츠 버전은 콘텐츠(어드레서블)에 실려 오므로 서버/CDN 별도 관리가 필요 없고, 콘텐츠와 절대 어긋나지 않는다.
/// 참조 미할당/로드 실패 시 표시는 v{Application.version}(콘텐츠 자리 없음)으로 유지된다(치명적 아님).
/// </summary>
public sealed class ContentVersionStep : IBootStep {
    private readonly IAssetService _asset;
    private readonly IVersionInfo _version;
    private readonly AssetReference _configRef;

    // 다운로드 단계에 이어 붙는 후처리라 같은 상태를 유지한다(로딩 UI 문구 유지).
    public BootState State => BootState.DownloadingAddressable;

    public ContentVersionStep(IAssetService asset, IVersionInfo version, AssetReference configRef) {
        _asset = asset;
        _version = version;
        _configRef = configRef;
    }

    public async UniTask ExecuteAsync(CancellationToken ct) {
        if (_configRef == null || !_configRef.RuntimeKeyIsValid()) return;
        try {
            var cfg = await _asset.LoadAssetAsync<RemoteAppConfig>(_configRef, ct);
            if (cfg != null) _version.SetContentVersion(cfg.ContentVersion);
        } catch (OperationCanceledException) {
            // 부트 취소는 무시한다.
        }
    }
}
