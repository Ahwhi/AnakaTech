using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 약관 동의 표시 흐름 오케스트레이터(ITermsConsentFlow 구현). 루트 DI 에 등록해 부트/설정 어디서든 호출한다.
/// 뷰는 주입이 필요 없는 순수 표시 컴포넌트로 두고, 서비스 의존(통신/문서 조회/팝업)은 전부 여기가 들고 있다.
///
/// 흐름:
///   1) 서버에 현재 동의 상태를 묻는다. 필수 항목이 모두 최신 버전으로 동의돼 있으면 아무것도 띄우지 않는다.
///   2) 동의 팝업을 띄우고, 항목의 상세 보기는 전문 팝업(앱 내부 웹뷰)으로 공식 홈페이지의 문서를 연다.
///   3) 사용자가 확정하면 서버에 저장하고, 광고성 정보 수신 동의 결과를 알린다.
///   4) 거부하면 종료할지 다시 확인한다. 종료를 고르지 않으면 동의 화면으로 돌아온다.
/// </summary>
public sealed class TermsConsentFlow : ITermsConsentFlow {
    private readonly ITermsService _terms;
    private readonly IUIService _ui;
    private readonly UIPopupCatalog _popups;
    private readonly IPushCoordinator _push;

    public TermsConsentFlow(ITermsService terms, IUIService ui, UIPopupCatalog popups, IPushCoordinator push) {
        _terms = terms;
        _ui = ui;
        _popups = popups;
        _push = push;
    }

    public async UniTask<bool> EnsureAgreedAsync(CancellationToken ct = default) {
        var status = await _terms.GetStatusAsync(ct);
        if (!status.NeedAgreement) return true;

        while (true) {
            var popup = await _ui.ShowPopupAsync<TermsAgreementPopup>(_popups.TermsAgreementPopup, ct);
            popup.Initialize(
                (url, title) => OpenDocumentAsync(url, title).Forget(),
                consents => SubmitAsync(consents, ct));

            if (await popup.ShowAsync(status)) return true;

            // 거부: 필수 약관 없이는 이용할 수 없으므로 종료 의사를 확인한다.
            var confirm = await _ui.ShowPopupAsync<ConfirmCancelPopup>(_popups.ConfirmCancelPopup, ct);
            bool quit = await confirm.ShowAsync(TermsRules.PopupTitle,
                $"{TermsRules.CancelledMessage}\n게임을 종료하시겠습니까?");
            if (quit) return false;

            // 다시 동의 화면으로. 서버에서 받은 항목은 그대로 재사용한다(같은 부트 안에서 바뀌지 않는다).
        }
    }

    public UniTask<TermsStatusDto> GetStatusAsync(CancellationToken ct = default) => _terms.GetStatusAsync(ct);

    public UniTask ShowDocumentAsync(string url, string title, CancellationToken ct = default) =>
        // 상세 팝업은 동의 항목과 같은 것을 쓴다. 문서를 보여 주는 일에 동의 여부는 관계가 없기 때문이다.
        OpenDocumentAsync(url, title);

    /// <summary>
    /// 선택 동의 한 건만 바꿔 서버에 제출한다. 필수 항목은 보내지 않는다.
    /// 서버가 저장된 상태 위에 이번 변경을 덮어 판정하므로, 보내지 않은 항목은 그대로 유지된다.
    /// </summary>
    public async UniTask<bool> SetOptionalConsentAsync(string key, int version, bool agreed, CancellationToken ct = default) {
        var before = await _terms.GetStatusAsync(ct);
        bool nightPushWasOn = before.Find(TermsRules.NightPushKey)?.IsAgreed ?? false;

        var res = await _terms.SubmitAsync(new[] { new TermsConsent(key, version, agreed) }, ct);
        if (!res.IsSuccess) {
            _ui.ShowToast(TermsRules.ConsentSaveFailed, 3f);
            return false;
        }

        NotifyConsentChanged(key, agreed, nightPushWasOn, ct).Forget();
        ApplyPushConsentAsync(key, agreed, ct).Forget();
        return true;
    }

    /// <summary>
    /// 광고성 수신 동의를 켜고 끌 때 이 기기의 푸시 발송 주소도 함께 맞춘다.
    ///
    /// 이 게임의 원격 알림은 광고성 하나뿐이라, 동의를 끄면 보낼 것이 없어져 주소를 남길 이유가 없다.
    /// 켤 때는 OS 알림 권한을 그 자리에서 묻는다. 권한이 없으면 알림이 오지 않는데 사용자는 켰다고
    /// 생각하게 되므로, 거부된 경우에는 설정으로 안내한다.
    ///
    /// 야간 동의만 바뀐 경우에는 주소를 건드리지 않는다. 주간에는 여전히 보낼 수 있기 때문이다.
    /// 동의 자체는 이미 서버에 저장됐으므로, 여기서 실패해도 법적 동의 상태는 어긋나지 않는다.
    /// </summary>
    private async UniTaskVoid ApplyPushConsentAsync(string key, bool agreed, CancellationToken ct) {
        if (key != TermsRules.MarketingKey) return;

        try {
            if (!agreed) { await _push.DisableAsync(ct); return; }

            if (!await _push.EnableAsync(ct))
                _ui.ShowToast(PushRules.PermissionDeniedMessage, 4f);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogWarning($"[Push] 동의 변경 반영 실패: {e.Message}");
        }
    }

    // 수신 동의와 철회는 그 처리 결과를 알려야 한다(정보통신망법상 처리 결과 통지).
    // 철회 안내에는 딸린 항목이 함께 해제됐는지도 포함한다.
    private async UniTaskVoid NotifyConsentChanged(string key, bool agreed, bool nightPushWasOn, CancellationToken ct) {
        var now = DateTime.UtcNow.AddHours(9);   // 표기는 기기 타임존과 무관하게 한국 시간으로 고정한다.

        if (key == TermsRules.NightPushKey) {
            _ui.ShowToast(agreed
                ? TermsRules.MarketingNotice(true, true, now)
                : TermsRules.NightPushWithdrawNotice(now), 3f);
            return;
        }

        if (key != TermsRules.MarketingKey) return;

        if (agreed) {
            _ui.ShowToast(TermsRules.MarketingNotice(true, false, now), 3f);
            return;
        }

        // 서버가 야간 동의를 함께 해제했는지는 저장된 결과로 확인한다(클라가 추측하지 않는다).
        var after = await _terms.GetStatusAsync(ct);
        bool nightPushNowOff = !(after.Find(TermsRules.NightPushKey)?.IsAgreed ?? false);
        _ui.ShowToast(TermsRules.MarketingWithdrawNotice(nightPushWasOn && nightPushNowOff, now), 3f);
    }

    // 저장은 서버가 최종 판정한다. 성공했을 때만 수신 동의 처리 결과를 알린다.
    private async UniTask<TermsSubmitResultDto> SubmitAsync(IReadOnlyList<TermsConsent> consents, CancellationToken ct) {
        var res = await _terms.SubmitAsync(consents, ct);
        if (res.IsSuccess && !res.NeedAgreement) NotifyMarketingResult(consents);
        return res;
    }

    // 광고성 정보 수신에 동의한 사실과 시점을 사용자에게 알린다(수신 동의 처리 결과 통지).
    // 동의하지 않은 경우엔 알릴 내용이 없으므로 표시하지 않는다.
    private void NotifyMarketingResult(IReadOnlyList<TermsConsent> consents) {
        bool marketing = false, nightPush = false, hasMarketingItem = false;
        foreach (var c in consents) {
            if (c.Key == TermsRules.MarketingKey) { hasMarketingItem = true; marketing = c.IsAgreed; }
            else if (c.Key == TermsRules.NightPushKey) nightPush = c.IsAgreed;
        }

        if (!hasMarketingItem || !marketing) return;
        // 표시 시각은 기기 타임존과 무관하게 한국 시간으로 고정한다(공지/이벤트 표기와 동일 규칙).
        _ui.ShowToast(TermsRules.MarketingNotice(true, nightPush, DateTime.UtcNow.AddHours(9)), 3f);
    }

    // 상세 보기: 공식 홈페이지의 문서를 앱 내부 웹뷰 팝업으로 연다.
    // 제목은 호출부가 정한다. 동의 화면은 "(필수)/(선택)"을 붙이지만 설정 화면의 열람은 문서 이름만 쓴다.
    //
    // 웹뷰를 쓸 수 없는 환경(에디터, PC)에서는 빈 팝업을 띄우지 않고 시스템 브라우저로 연다.
    // 문서를 읽을 수 있게 하는 것이 목적이지, 앱 안에서 보여 주는 것 자체가 목적은 아니기 때문이다.
    private async UniTask OpenDocumentAsync(string url, string title) {
        if (!WebLinkPolicy.IsInAppAllowed(url)) {
            Debug.LogWarning($"[Terms] 열 수 없는 문서 주소입니다: {url}");
            return;
        }

        if (!InAppWebView.IsSupported) {
            Application.OpenURL(url);
            return;
        }

        TermsDetailPopup popup;
        try {
            popup = await _ui.ShowPopupAsync<TermsDetailPopup>(_popups.TermsDetailPopup);
        } catch (OperationCanceledException) {
            return;
        }

        await popup.ShowAsync(title, url);
    }
}
