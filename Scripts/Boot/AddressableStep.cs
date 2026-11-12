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

    // 실패 후 다시 시도하기 전 대기(초). 길이가 곧 재시도 횟수다(첫 시도 + 이 개수만큼).
    // 원격 그룹은 요청 단위 재시도가 없어서(RetryCount 0) 번들 하나의 일시적 실패가 곧 부트 실패가 된다.
    // 이미 받은 번들은 캐시에 남으므로 다시 시도하면 남은 것만 받는다.
    private static readonly float[] RetryDelaySeconds = { 1f, 3f };

    public async UniTask ExecuteAsync(CancellationToken ct) {
        bool consented = false;

        for (int attempt = 0; ; attempt++) {
            try {
                long size = await _addressable.GetPreloadDownloadSizeAsync(ct);

                // 받을 게 있으면 용량/예상시간을 알리고 동의를 받는다. 거절 시 앱 종료(부트 중단).
                // 재시도 때는 다시 묻지 않는다(이미 동의한 다운로드를 이어 받는 것이다).
                if (size > 0 && !consented) {
                    bool agreed = await _consent.RequestAsync(size, ct);
                    if (!agreed) {
                        Application.Quit();
                        throw new DownloadDeclinedException();   // 에디터 등 즉시 종료되지 않는 환경에서 부트를 멈춘다
                    }
                    consented = true;
                }

                await _addressable.DownloadPreloadAsync(_progress.Report, ct);
                return;
            } catch (DownloadDeclinedException) {
                throw;
            } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                throw;
            } catch (Exception e) when (attempt < RetryDelaySeconds.Length) {
                // 마지막 시도의 실패는 그대로 올려 부트가 실패 사유와 함께 멈추게 한다.
                Debug.LogWarning($"[Addressable] 다운로드 실패, {RetryDelaySeconds[attempt]}초 후 다시 시도합니다({attempt + 1}/{RetryDelaySeconds.Length}): {e.Message}");
                await UniTask.Delay(TimeSpan.FromSeconds(RetryDelaySeconds[attempt]), ignoreTimeScale: true, cancellationToken: ct);
            }
        }
    }
}
