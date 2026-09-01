using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Enums;

/// <summary>
/// 6단계: 약관 동의(가입 게이트). 로그인으로 계정이 확정된 직후에 실행된다.
/// 이미 최신 버전 약관에 동의한 계정은 아무것도 뜨지 않고 그대로 통과하며,
/// 신규 가입이거나 약관이 개정된 경우에만 동의 화면이 열린다.
///
/// 이 스텝은 화면을 모른다. 게이트(ITermsConsentFlow)에 "동의를 보장해 달라"고 요청할 뿐이다.
/// 사용자가 끝내 거부하면 다운로드 거절과 같은 방식으로 앱을 종료한다.
/// </summary>
public sealed class TermsStep : IBootStep {
    private readonly ITermsConsentFlow _terms;
    private readonly IPushCoordinator _push;

    public BootState State => BootState.AgreeingTerms;

    public TermsStep(ITermsConsentFlow terms, IPushCoordinator push) {
        _terms = terms;
        _push = push;
    }

    public async UniTask ExecuteAsync(CancellationToken ct) {
        if (!await _terms.EnsureAgreedAsync(ct)) {
            Application.Quit();
            throw new TermsDeclinedException();   // 에디터 등 즉시 종료되지 않는 환경에서 부트를 멈춘다
        }

        // 계정과 동의가 확정된 직후에 푸시 발송 주소를 맞춘다. 이 시점 이전에는 어느 계정의 기기인지
        // 알 수 없어 등록할 수 없고, 등록하면 이전 계정에 남는다.
        //
        // 부트를 막지 않는다. 토큰 발급과 서버 등록은 몇 초가 걸릴 수 있는데, 그동안 로딩을 붙잡아 둘 만큼
        // 급한 일이 아니다. 실패해도 다음 접속에서 다시 맞춰진다.
        _push.SyncAsync(ct).Forget();
    }
}
