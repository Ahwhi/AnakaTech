using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 쿠폰 서버 통신 구현. 유효성/기간/수량/중복 판정과 지급은 전부 서버가 하고 여기서는 값을 옮기기만 한다.
/// </summary>
public sealed class CouponService : ICouponService {
    // 쿠폰 사용은 우편 발송까지 함께 도는 처리라 다른 조회보다 넉넉히 기다린다.
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(12);

    private readonly INetworkService _net;

    public CouponService(INetworkService net) => _net = net;

    public async UniTask<CouponRedeemResultDto> RedeemAsync(string code, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_RedeemCouponResult>(cts.Token);
        _net.Send(new C_RedeemCoupon { code = code ?? "" }.Write());
        var res = await wait;

        var dto = new CouponRedeemResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Code = res.code ?? "",
        };
        foreach (var g in res.grantedRewards)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        return dto;
    }
}
