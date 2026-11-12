using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 투자 서버 통신 구현. 해금/최대레벨/골드 판정과 차감은 전부 서버가 하고 여기서는 값을 옮기기만 한다.
/// </summary>
public sealed class InvestmentService : IInvestmentService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public InvestmentService(INetworkService net) => _net = net;

    public async UniTask<InvestmentDto> RequestAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_Investment>(cts.Token);
        _net.Send(new C_Investment().Write());
        var res = await wait;

        var dto = new InvestmentDto { StarTotal = res.starTotal, NewGold = res.newGold };
        foreach (var s in res.slots) dto.Levels[s.slotIndex] = s.level;
        return dto;
    }

    public async UniTask<InvestmentUpgradeResultDto> UpgradeAsync(int slotIndex, int expectedLevel, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_UpgradeInvestmentResult>(cts.Token);
        _net.Send(new C_UpgradeInvestment { slotIndex = slotIndex, expectedLevel = expectedLevel }.Write());
        var res = await wait;

        return new InvestmentUpgradeResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            SlotIndex = res.slotIndex,
            NewLevel = res.newLevel,
            SpentGold = res.spentGold,
            NewGold = res.newGold,
            SpentDiamond = res.spentDiamond,
            NewDiamond = res.newDiamond,
            StarTotal = res.starTotal,
        };
    }
}
