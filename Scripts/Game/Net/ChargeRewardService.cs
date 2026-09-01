using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>누적 충전 보상 통신 구현.</summary>
public sealed class ChargeRewardService : IChargeRewardService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public ChargeRewardService(INetworkService net) {
        _net = net;
    }

    public async UniTask<ChargeRewardStatusDto> RequestStatusAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ChargeRewards>(cts.Token);
        _net.Send(new C_ChargeRewards().Write());
        var res = await wait;

        var dto = new ChargeRewardStatusDto { CumulativeWon = res.cumulativeWon };
        foreach (var t in res.claimedTiers) dto.ClaimedTiers.Add(t.tier);
        return dto;
    }

    public async UniTask<ClaimChargeResult> ClaimAsync(int tier, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ClaimChargeRewardResult>(cts.Token);
        _net.Send(new C_ClaimChargeReward { tier = tier }.Write());
        var res = await wait;

        return new ClaimChargeResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Tier = res.tier,
            NewDiamond = res.newDiamond,
        };
    }
}
