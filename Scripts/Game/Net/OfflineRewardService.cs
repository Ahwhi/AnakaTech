using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 오프라인 보상 통신 구현.
///
/// 요청 패킷에는 필드가 하나도 없다. 오프라인 구간은 세션이 붙고 떨어질 때 서버가 확정해 두므로
/// 클라이언트가 알려 줄 것이 없고, 알려 줄 수 있게 두면 그대로 조작 경로가 된다.
/// </summary>
public sealed class OfflineRewardService : IOfflineRewardService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public OfflineRewardService(INetworkService net) {
        _net = net;
    }

    public async UniTask<OfflineRewardDto> RequestAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_OfflineReward>(cts.Token);
        _net.Send(new C_OfflineReward().Write());
        var res = await wait;

        var dto = new OfflineRewardDto {
            SettlementId = res.settlementId,
            OfflineSeconds = res.offlineSeconds,
            RewardMinutes = res.rewardMinutes,
            Capped = res.capped,
            Gold = res.gold,
            Stamina = res.stamina,
            Diamond = res.diamond,
            Exp = res.exp,
            InvestExpPermille = res.investExpPermille,
            AlreadySettled = res.alreadySettled,
            NewGold = res.newGold,
            NewStamina = res.newStamina,
            NewDiamond = res.newDiamond,
        };
        foreach (var e in res.charExps) {
            dto.CharExp.Add(new CharExpGain {
                CharDataId = e.charDataId,
                GainedExp = e.gainedExp,
                NewLevel = e.newLevel,
                NewExp = e.newExp,
                MaxExp = e.maxExp,
                GainedStatPoints = e.gainedStatPoints,
            });
        }
        return dto;
    }
}
