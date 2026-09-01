using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>
/// IDownloadConsent 구현. 부트 스텝의 RequestAsync 를 타이틀 UI의 응답과 이어준다.
///   - 부트 스텝: RequestAsync(size) -> Requested 로 용량 방출 후 Respond 가 올 때까지 대기.
///   - 타이틀 UI: Requested 구독 -> 인씬 팝업 표시 -> 버튼 클릭 시 Respond(true/false).
/// VContainer 에 Singleton 으로 등록하고 두 인터페이스로 노출한다(역할 분리).
/// </summary>
public sealed class DownloadConsent : IDownloadConsent, IDownloadConsentPresenter {
    private readonly Subject<long> _requested = new();
    private UniTaskCompletionSource<bool> _pending;

    public IObservable<long> Requested => _requested;

    public async UniTask<bool> RequestAsync(long sizeBytes, CancellationToken ct = default) {
        _pending = new UniTaskCompletionSource<bool>();
        _requested.OnNext(sizeBytes);   // 타이틀 UI가 인씬 팝업을 띄운다.

        using (ct.Register(() => _pending.TrySetResult(false)))
            return await _pending.Task;
    }

    public void Respond(bool agreed) => _pending?.TrySetResult(agreed);
}
