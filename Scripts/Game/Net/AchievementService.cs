using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>
/// 업적 통신 구현(다른 서비스와 동일한 WaitPacketAsync + Send 패턴).
/// 점수 계산(횟수 x 배점)과 보상 수령 판정은 서버가 하므로 여기서는 값을 옮기기만 한다.
///
/// 수령 가능한 보상이 있는지는 ReactiveProperty 로 노출해 배지가 구독한다. 업적 조회와 수령 응답이
/// 모두 이 값을 갱신하므로, 보상을 받는 순간 배지가 그 자리에서 꺼진다.
/// </summary>
public sealed class AchievementService : IAchievementService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<bool> _hasClaimableReward = new(false);

    public IReadOnlyReactiveProperty<bool> HasClaimableReward => _hasClaimableReward;

    public AchievementService(INetworkService net) {
        _net = net;
    }

    public async UniTask<AchievementHomeDto> RequestAchievementsAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_Achievements>(cts.Token);
        _net.Send(new C_Achievements().Write());
        var res = await wait;

        var dto = new AchievementHomeDto { TotalScore = res.totalScore };
        foreach (var e in res.entrys) {
            dto.Entries.Add(new AchievementEntryDto {
                Metric = (AchievementMetric)e.metric,
                Category = (AchievementCategory)e.category,
                Count = e.count,
                Points = e.points,
                Score = e.score,
            });
        }

        // 보상 구성은 tier 참조 평면 리스트로 오므로 tier 로 묶어 각 티어에 붙인다(중첩 리스트 미지원 대응).
        var byTier = new Dictionary<int, AchievementRewardTierDto>();
        foreach (var t in res.rewardTiers) {
            var tier = new AchievementRewardTierDto { Tier = t.tier, Threshold = t.threshold, Claimed = t.claimed };
            byTier[t.tier] = tier;
            dto.RewardTiers.Add(tier);
        }
        foreach (var g in res.rewardGrants) {
            if (byTier.TryGetValue(g.tier, out var tier))
                tier.Rewards.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        }

        _hasClaimableReward.Value = dto.HasClaimableReward;
        return dto;
    }

    public async UniTask<AchievementClaimResultDto> ClaimRewardAsync(int tier, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ClaimAchievementRewardResult>(cts.Token);
        _net.Send(new C_ClaimAchievementReward { tier = tier }.Write());
        var res = await wait;

        var dto = new AchievementClaimResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Tier = res.tier,
            NewDiamond = res.newDiamond,
        };
        foreach (var g in res.grantedRewards)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        return dto;
    }

    /// <summary>
    /// 남은 수령 가능 여부를 알린다. 팝업이 수령 후 남은 티어를 이미 알고 있으므로,
    /// 배지를 맞추려고 서버를 다시 두드리지 않는다.
    /// </summary>
    public void SetClaimable(bool claimable) => _hasClaimableReward.Value = claimable;

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try { await RequestAchievementsAsync(ct); }
        catch (OperationCanceledException) { }
    }
}
