using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Enums;

/// <summary>
/// 4단계: 어드레서블 카탈로그 갱신 + 프리로드 다운로드.
/// 받을 용량이 있으면 상용 게임처럼 용량/예상시간 동의 팝업(IDownloadConsent)을 거친 뒤 다운로드한다.
/// 진행 정보(받은/전체 바이트 + 퍼센트)는 IProgress&lt;DownloadInfo&gt; 로 통지하며, 이 스텝은 "부트 상태"나
/// 실제 UI를 전혀 모른다(완전한 디커플링).
/// </summary>
public sealed class AddressableStep : IBootStep {
    private readonly IAssetService _addressable;
    private readonly IDownloadConsent _consent;
    private readonly IProgress<DownloadInfo> _progress;

    public BootState State => BootState.DownloadingAddressable;

    public AddressableStep(IAssetService addressable, IDownloadConsent consent, IProgress<DownloadInfo> progress) {
        _addressable = addressable;
        _consent = consent;
        _progress = progress;
    }

    public async UniTask ExecuteAsync(CancellationToken ct) {
        long size = await _addressable.GetPreloadDownloadSizeAsync(ct);

        // 받을 게 있으면 용량/예상시간을 알리고 동의를 받는다. 거절 시 앱 종료(부트 중단).
        if (size > 0) {
            bool agreed = await _consent.RequestAsync(size, ct);
            if (!agreed) {
                Application.Quit();
                throw new DownloadDeclinedException();   // 에디터 등 즉시 종료되지 않는 환경에서 부트를 멈춘다
            }
        }

        await _addressable.DownloadPreloadAsync(_progress.Report, ct);
    }
}
