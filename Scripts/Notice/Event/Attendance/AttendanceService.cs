using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 출석체크 통신 구현. C_AttendanceInfo/C_ClaimAttendance 를 보내고 대응 응답을 받아 DTO 로 변환한다.
/// EventStatusSync 와 동일하게 대기자를 먼저 등록한 뒤 송신하고, 짧은 타임아웃으로 무한 대기를 막는다.
/// </summary>
public sealed class AttendanceService : IAttendanceService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(5);

    private readonly INetworkService _net;

    public AttendanceService(INetworkService net) {
        _net = net;
    }

    public async UniTask<AttendanceInfo> GetInfoAsync(CancellationToken ct = default) {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_AttendanceInfo>(timeoutCts.Token);
        _net.Send(new C_AttendanceInfo().Write());
        var res = await wait;

        var rewards = new List<AttendanceReward>(res.dayRewards.Count);
        foreach (var r in res.dayRewards)
            rewards.Add(new AttendanceReward(r.day, r.kind, r.value, r.qty, r.currencyKey));

        return new AttendanceInfo(res.claimedDays, res.canClaimToday, res.maxDays, rewards);
    }

    public async UniTask<AttendanceClaimResult> ClaimAsync(CancellationToken ct = default) {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ClaimAttendanceResult>(timeoutCts.Token);
        _net.Send(new C_ClaimAttendance().Write());
        var res = await wait;

        var granted = new List<AttendanceReward>(res.grantedRewards.Count);
        foreach (var g in res.grantedRewards)
            granted.Add(new AttendanceReward(0, g.kind, g.value, g.qty, g.currencyKey));

        return new AttendanceClaimResult(res.isSuccess, res.failReason, res.claimedDays, res.canClaimToday,
                                         res.newGold, res.newDiamond, res.newStamina, granted);
    }
}
