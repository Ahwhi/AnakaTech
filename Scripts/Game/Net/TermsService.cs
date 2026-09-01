using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 약관 동의 서버 통신 구현. 목록/필수 여부/버전 판정은 전부 서버가 하고 여기서는 값을 옮기기만 한다.
/// </summary>
public sealed class TermsService : ITermsService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(10);

    private readonly INetworkService _net;

    public TermsService(INetworkService net) => _net = net;

    public async UniTask<TermsStatusDto> GetStatusAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_TermsInfo>(cts.Token);
        _net.Send(new C_TermsInfo().Write());
        var res = await wait;

        var dto = new TermsStatusDto { NeedAgreement = res.needAgreement };
        foreach (var t in res.terms)
            dto.Items.Add(new TermsItemDto {
                Key             = t.termsKey ?? "",
                Version         = t.version,
                IsRequired      = t.isRequired,
                Title           = t.title ?? "",
                PrerequisiteKey = t.prerequisiteKey ?? "",
                IsAgreed        = t.isAgreed,
                Url             = t.url ?? "",
            });

        foreach (var d in res.documents)
            dto.Documents.Add(new TermsDocumentDto {
                Key     = d.documentKey ?? "",
                Version = d.version,
                Title   = d.title ?? "",
                Url     = d.url ?? "",
            });
        return dto;
    }

    public async UniTask<TermsSubmitResultDto> SubmitAsync(IReadOnlyList<TermsConsent> consents, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var req = new C_AgreeTerms();
        foreach (var c in consents)
            req.agreements.Add(new C_AgreeTerms.Agreement {
                termsKey = c.Key ?? "",
                version  = c.Version,
                isAgreed = c.IsAgreed,
            });

        var wait = _net.WaitPacketAsync<S_AgreeTermsResult>(cts.Token);
        _net.Send(req.Write());
        var res = await wait;

        return new TermsSubmitResultDto {
            IsSuccess     = res.isSuccess,
            FailReason    = res.failReason,
            NeedAgreement = res.needAgreement,
        };
    }
}
