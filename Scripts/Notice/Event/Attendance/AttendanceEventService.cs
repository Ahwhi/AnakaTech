using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 출석형 이벤트 통신 구현. C_AttendanceEventInfo/C_ClaimAttendanceEvent 를 보내고 대응 응답을 DTO 로 바꾼다.
/// AttendanceService 와 같은 규약(대기자 먼저 등록 후 송신 + 짧은 타임아웃)이다.
/// </summary>
public sealed class AttendanceEventService : IAttendanceEventService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(5);

    private readonly INetworkService _net;

    public AttendanceEventService(INetworkService net) {
        _net = net;
    }

    public async UniTask<AttendanceInfo> GetInfoAsync(string eventKey, CancellationToken ct = default) {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_AttendanceEventInfo>(timeoutCts.Token);
        _net.Send(new C_AttendanceEventInfo { eventKey = eventKey ?? "" }.Write());
        var res = await wait;

        var rewards = new List<AttendanceReward>(res.dayRewards.Count);
        foreach (var r in res.dayRewards)
            rewards.Add(new AttendanceReward(r.day, r.kind, r.value, r.qty, r.currencyKey));

        return new AttendanceInfo(res.claimedDays, res.canClaimToday, res.maxDays, rewards, res.locked);
    }

    public async UniTask<AttendanceClaimResult> ClaimAsync(string eventKey, CancellationToken ct = default) {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ClaimAttendanceEventResult>(timeoutCts.Token);
        _net.Send(new C_ClaimAttendanceEvent { eventKey = eventKey ?? "" }.Write());
        var res = await wait;

        var granted = new List<AttendanceReward>(res.grantedRewards.Count);
        foreach (var g in res.grantedRewards)
            granted.Add(new AttendanceReward(0, g.kind, g.value, g.qty, g.currencyKey));

        return new AttendanceClaimResult(res.isSuccess, res.failReason, res.claimedDays, res.canClaimToday,
                                         res.newGold, res.newDiamond, res.newStamina, granted);
    }
}
