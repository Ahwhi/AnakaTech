using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>도감 보상 통신 구현. 진행 상태 조회/수령을 패킷으로 주고받아 DTO 로 변환한다.</summary>
public sealed class EncyclopediaService : IEncyclopediaService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public EncyclopediaService(INetworkService net) {
        _net = net;
    }

    public async UniTask<EncyclopediaRewardStatusDto> RequestRewardsAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EncyclopediaRewards>(cts.Token);
        _net.Send(new C_EncyclopediaRewards().Write());
        var res = await wait;

        var dto = new EncyclopediaRewardStatusDto {
            CollectionCount = res.collectionCount,
            TotalStar = res.totalStar,
        };
        foreach (var c in res.claimedCollections) dto.ClaimedCollectionSteps.Add(c.step);
        foreach (var s in res.claimedStars) dto.ClaimedStarSteps.Add(s.step);
        return dto;
    }

    public async UniTask<ClaimEncyclopediaResult> ClaimAsync(int rewardType, int step, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ClaimEncyclopediaRewardResult>(cts.Token);
        _net.Send(new C_ClaimEncyclopediaReward { rewardType = rewardType, step = step }.Write());
        var res = await wait;

        return new ClaimEncyclopediaResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            RewardType = res.rewardType,
            Step = res.step,
            NewDiamond = res.newDiamond,
            GrantedGrade = res.grantedGrade,
        };
    }
}
