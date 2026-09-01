using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 다운로드 동의 게이트. 부트 스텝(다운로드 담당)이 "이만큼 받아야 한다"고 알리고 사용자 응답을 기다린다.
/// 실제 팝업 UI는 타이틀 씬(IDownloadConsentPresenter 구현측)이 담당하므로, 부트 로직은 UI를 모른 채
/// 계약(RequestAsync)에만 의존한다(디커플링). 어드레서블 다운로드 전에 떠야 하는 팝업이라 원격 자산에 의존하면
/// 안 되고, 타이틀 씬 하이어라키에 심은 인씬 패널이 이 게이트를 통해 응답을 돌려준다.
/// </summary>
public interface IDownloadConsent {
    /// <summary>다운로드 용량을 알리고 사용자 동의를 기다린다. true=동의, false=거절(앱 종료).</summary>
    UniTask<bool> RequestAsync(long sizeBytes, CancellationToken ct = default);
}

/// <summary>
/// 타이틀 UI 측 계약. 동의 요청(다운로드 용량)을 구독해 인씬 팝업을 띄우고, 사용자의 선택을 Respond 로 돌려준다.
/// </summary>
public interface IDownloadConsentPresenter {
    IObservable<long> Requested { get; }
    void Respond(bool agreed);
}
