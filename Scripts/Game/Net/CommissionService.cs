using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 의뢰 컨텐츠 서버 통신 구현. 다른 서비스와 동일하게 대기자를 먼저 등록하고 요청을 보낸 뒤 응답을 DTO 로 옮긴다
/// (짧은 타임아웃으로 무한 대기 방지). 생성 패킷(C_/S_)에 의존하는 유일한 계층이다.
/// </summary>
public sealed class CommissionService : ICommissionService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public CommissionService(INetworkService net) => _net = net;

    public async UniTask<CommissionInfoDto> RequestInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_CommissionInfo>(cts.Token);
        _net.Send(new C_CommissionInfo().Write());
        var res = await wait;

        var dto = new CommissionInfoDto {
            Tickets = res.tickets,
            TicketMax = res.ticketMax,
            Renown = res.renown,
            EngravingStone = res.engravingStone
        };
        foreach (var s in res.stages)
            dto.Stages.Add(new CommissionStageDto { CommissionType = s.commissionType, ClearedStage = s.clearedStage });
        return dto;
    }

    public async UniTask<StartCommissionResult> StartAsync(CommissionType type, int stage, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_StartCommissionResult>(cts.Token);
        _net.Send(new C_StartCommission { commissionType = (int)type, stage = stage }.Write());
        var res = await wait;

        return new StartCommissionResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            RemainingTickets = res.newTickets
        };
    }

    public async UniTask<CommissionRewardDto> ClearAsync(CommissionType type, int stage, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ClearCommissionResult>(cts.Token);
        _net.Send(new C_ClearCommission { commissionType = (int)type, stage = stage }.Write());
        var res = await wait;

        // 클리어 보고는 입장권을 소모하지 않으므로 잔량을 건드리지 않는다는 뜻으로 -1 을 유지한다.
        var dto = new CommissionRewardDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            CommissionType = res.commissionType,
            ClearedStage = res.clearedStage,
            NewGold = res.newGold,
            NewDiamond = res.newDiamond,
            NewRenown = res.newRenown,
            NewEngravingStone = res.newEngravingStone
        };
        foreach (var g in res.grantedRewards)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        foreach (var e in res.charExps)
            dto.CharExp.Add(ToCharExp(e.charDataId, e.gainedExp, e.newLevel, e.newExp, e.maxExp, e.gainedStatPoints));
        return dto;
    }

    public async UniTask<CommissionRewardDto> SweepAsync(CommissionType type, int count, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_SweepCommissionResult>(cts.Token);
        _net.Send(new C_SweepCommission { commissionType = (int)type, count = count }.Write());
        var res = await wait;

        var dto = new CommissionRewardDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            CommissionType = res.commissionType,
            NewTickets = res.newTickets,
            NewGold = res.newGold,
            NewDiamond = res.newDiamond,
            NewRenown = res.newRenown,
            NewEngravingStone = res.newEngravingStone
        };
        foreach (var g in res.grantedRewards)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        foreach (var e in res.charExps)
            dto.CharExp.Add(ToCharExp(e.charDataId, e.gainedExp, e.newLevel, e.newExp, e.maxExp, e.gainedStatPoints));
        return dto;
    }

    // 응답이 없는 단방향 보고다. 결과를 기다릴 것이 없고(진도는 서버가 알아서 반영한다) 실패해도
    // 되돌릴 것이 없으므로 그냥 보내고 끝낸다.
    public void ReportAttempt(CommissionType type) =>
        _net.Send(new C_CommissionAttempt { commissionType = (int)type }.Write());

    // 두 응답 패킷의 경험치 항목 타입이 서로 달라(생성 패킷은 중첩 클래스를 각각 만든다) 값만 받아 DTO 로 옮긴다.
    private static CharExpGain ToCharExp(int charDataId, int gainedExp, int newLevel, int newExp, int maxExp, int gainedStatPoints) =>
        new CharExpGain {
            CharDataId = charDataId,
            GainedExp = gainedExp,
            NewLevel = newLevel,
            NewExp = newExp,
            MaxExp = maxExp,
            GainedStatPoints = gainedStatPoints
        };
}
