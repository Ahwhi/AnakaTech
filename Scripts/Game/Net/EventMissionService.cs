using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>미션형 이벤트 통신 구현(상태 조회/미션 수령). AttendanceService 와 동일하게 대기자 등록 후 송신 + 짧은 타임아웃.</summary>
public sealed class EventMissionService : IEventMissionService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public EventMissionService(INetworkService net) {
        _net = net;
    }

    public async UniTask<EventMissionStatus> GetStatusAsync(string eventKey, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EventMissions>(cts.Token);
        _net.Send(new C_EventMissions { eventKey = eventKey ?? "" }.Write());
        var res = await wait;

        var missions = new List<EventMission>(res.missions.Count);
        foreach (var m in res.missions)
            missions.Add(new EventMission(
                m.id, m.daily, m.metric, m.param, m.threshold, m.progress, m.claimed,
                m.locked, m.group, m.unlockDay,
                new EventMissionReward(m.rewardKind, m.rewardValue, m.rewardQty, m.rewardCurrencyKey)));

        return new EventMissionStatus(res.endUnix, missions, res.totalMissions, res.achievedMissions, res.unlockedDay);
    }

    public async UniTask<EventMissionClaimResult> ClaimAsync(int missionId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ClaimEventMissionResult>(cts.Token);
        _net.Send(new C_ClaimEventMission { missionId = missionId }.Write());
        var res = await wait;

        var granted = new List<EventMissionReward>(res.grantedRewards.Count);
        foreach (var g in res.grantedRewards)
            granted.Add(new EventMissionReward(g.kind, g.value, g.qty, g.currencyKey));

        return new EventMissionClaimResult(res.isSuccess, res.failReason, res.missionId,
                                        res.newGold, res.newDiamond, res.newStamina, granted);
    }
}
