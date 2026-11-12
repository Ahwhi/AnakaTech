using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>
/// 멸망전 통신 구현(현황 조회 / 오늘 참여 현황 / 입장 / 종료 보고 / 보상 수령).
///
/// 수령 가능 여부와 무료 참여 가능 여부는 서버 응답이 올 때마다 곧바로 반영한다. 팝업을 열든, 입장하든,
/// 한 판을 끝내든, 보상을 받든 배지가 그 자리에서 켜지고 꺼진다.
/// </summary>
public sealed class DoomService : IDoomService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<bool> _hasClaimable = new(false);
    private readonly ReactiveProperty<bool> _hasFreeEntry = new(false);

    public DoomService(INetworkService net) {
        _net = net;
    }

    public IReadOnlyReactiveProperty<bool> HasClaimable => _hasClaimable;
    public IReadOnlyReactiveProperty<bool> HasFreeEntry => _hasFreeEntry;

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            // 응답을 받는 과정에서 HasClaimable / HasFreeEntry 가 갱신된다.
            await RequestInfoAsync(ct);
            await RequestEntryAsync(ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            // 배지 갱신 실패로 화면 전체가 멈추면 안 된다. 다음 갱신에서 다시 맞춰진다.
            UnityEngine.Debug.LogException(e);
        }
    }

    public void Dispose() {
        _hasClaimable.Dispose();
        _hasFreeEntry.Dispose();
    }

    public async UniTask<DoomEntryDto> RequestEntryAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DoomEntryInfo>(cts.Token);
        _net.Send(new C_DoomEntryInfo().Write());
        var res = await wait;

        return ApplyEntry(res.usedEntries, res.nextEntryCost);
    }

    public async UniTask<DoomEnterDto> EnterAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DoomEnterResult>(cts.Token);
        _net.Send(new C_DoomEnter().Write());
        var res = await wait;

        // 실패(다이아 부족)여도 서버가 현황을 함께 보내므로 배지는 그 값으로 맞춘다. 계정 없음은 현황이 비어 있다.
        bool hasEntry = res.failReason != DoomEnterFail.NoAccount;
        return new DoomEnterDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Entry = hasEntry ? ApplyEntry(res.usedEntries, res.nextEntryCost) : null,
            NewDiamond = res.newDiamond,
        };
    }

    // 서버가 확정한 현황을 DTO 로 옮기며 배지 근거(무료 참여 가능)를 갱신한다. 현황이 오는 모든 응답이 이 한 곳을 지난다.
    private DoomEntryDto ApplyEntry(int usedEntries, int nextEntryCost) {
        var entry = new DoomEntryDto { UsedEntries = usedEntries, NextEntryCost = nextEntryCost };
        _hasFreeEntry.Value = entry.IsFree;
        return entry;
    }

    public async UniTask<DoomInfoDto> RequestInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DoomInfo>(cts.Token);
        _net.Send(new C_DoomInfo().Write());
        var res = await wait;

        _hasClaimable.Value = res.canClaim;
        return new DoomInfoDto {
            BestScore = res.bestScore,
            ClaimedScore = res.claimedScore,
            CanClaim = res.canClaim,
        };
    }

    // 단방향 보고라 기다릴 응답이 없다(의뢰 도전 종료 보고와 같은 방식).
    public void ReportFinished() =>
        _net.Send(new C_BattleFinished { content = BattleFinishContent.Doom }.Write());

    public async UniTask<DoomFinishDto> FinishAsync(int score, int phase, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DoomFinishResult>(cts.Token);
        _net.Send(new C_DoomFinish { score = score, phase = phase }.Write());
        var res = await wait;

        // 한 판을 끝내 구간을 새로 넘었으면 그 자리에서 배지가 켜진다(메인으로 돌아가기 전에).
        if (res.isSuccess) _hasClaimable.Value = res.canClaim;
        var dto = new DoomFinishDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            BestScore = res.bestScore,
            IsBest = res.isBest,
            CanClaim = res.canClaim,
            GainedExp = res.gainedExp,
        };
        foreach (var e in res.charExps)
            dto.CharExp.Add(new CharExpGain {
                CharDataId = e.charDataId, GainedExp = e.gainedExp, NewLevel = e.newLevel,
                NewExp = e.newExp, MaxExp = e.maxExp, GainedStatPoints = e.gainedStatPoints,
            });
        return dto;
    }

    public async UniTask<DoomClaimResultDto> ClaimAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DoomClaimResult>(cts.Token);
        _net.Send(new C_DoomClaim().Write());
        var res = await wait;

        // 수령에 성공하면 도달한 구간을 전부 받은 것이므로 더 받을 것이 없다.
        if (res.isSuccess) _hasClaimable.Value = false;

        var dto = new DoomClaimResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            ClaimedScore = res.claimedScore,
            BestScore = res.bestScore,
            NewGold = res.newGold,
            NewDiamond = res.newDiamond,
        };
        foreach (var g in res.granteds)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        return dto;
    }
}
