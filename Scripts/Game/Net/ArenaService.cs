using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

/// <summary>
/// 쟁탈전 서버 통신 구현. 매칭/보고는 서버가 판정하고 여기서는 패킷 <-> DTO 변환만 한다.
///
/// 무료 매칭 잔여 여부(배지 근거)는 응답이 올 때마다 갱신한다. 하루 한 번 자정에만 바뀌는 값이라
/// 수련처럼 시간을 재는 감시가 필요 없고, 조회/매칭 응답이 곧 최신 상태다.
/// </summary>
public sealed class ArenaService : IArenaService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<bool> _hasFreeEntry = new(false);

    public ArenaService(INetworkService net) => _net = net;

    public IReadOnlyReactiveProperty<bool> HasFreeEntry => _hasFreeEntry;

    public async UniTask<ArenaHomeDto> RequestHomeAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ArenaHome>(cts.Token);
        _net.Send(new C_ArenaHome().Write());
        var res = await wait;

        var dto = new ArenaHomeDto {
            Coin = res.coin,
            Score = res.score,
            Wins = res.wins,
            Losses = res.losses,
            Rank = res.rank,
            UsedEntries = res.usedEntries,
            FreeEntries = res.freeEntries,
            MaxEntries = res.maxEntries,
            NextEntryCost = res.nextEntryCost,
            SeasonEndUnix = res.seasonEndUnix,
            LastDefenseLogId = res.lastDefenseLogId,
        };
        _hasFreeEntry.Value = ArenaRules.HasFreeEntry(dto.UsedEntries);
        return dto;
    }

    public async UniTask<ArenaMatchResultDto> MatchAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ArenaMatchResult>(cts.Token);
        _net.Send(new C_ArenaMatch().Write());
        var res = await wait;

        var dto = new ArenaMatchResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            OpponentAccountId = res.opponentAccountId,
            OpponentNickname = res.opponentNickname ?? "",
            OpponentCombatPower = res.opponentCombatPower,
            OpponentScore = res.opponentScore,
            OpponentProfileFrameId = res.opponentProfileFrameId,
            OpponentRepresentativeCharId = res.opponentRepresentativeCharId,
            OpponentRepresentativeCostumeId = res.opponentRepresentativeCostumeId,
            NewDiamond = res.newDiamond,
            UsedEntries = res.usedEntries,
            NextEntryCost = res.nextEntryCost,
        };
        // 매칭에 성공했으면 횟수가 하나 줄었으므로 배지 근거도 함께 갱신한다.
        if (dto.IsSuccess) _hasFreeEntry.Value = ArenaRules.HasFreeEntry(dto.UsedEntries);
        return dto;
    }

    public async UniTask<ArenaReportResultDto> ReportAsync(int targetAccountId, bool win, int hpPercentTenth,
                                                          CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ArenaReportResult>(cts.Token);
        _net.Send(new C_ArenaReport {
            targetAccountId = targetAccountId, win = win, hpPercentTenth = hpPercentTenth
        }.Write());
        var res = await wait;

        var dto = new ArenaReportResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Win = res.win,
            ScoreDelta = res.scoreDelta,
            NewScore = res.newScore,
            NewCoin = res.newCoin,
            Wins = res.wins,
            Losses = res.losses,
        };
        foreach (var g in res.grantedRewards)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        return dto;
    }

    public async UniTask<ArenaLogDto> RequestLogAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ArenaLog>(cts.Token);
        _net.Send(new C_ArenaLog().Write());
        var res = await wait;

        var dto = new ArenaLogDto { MyAccountId = res.myAccountId };
        foreach (var e in res.entrys)
            dto.Entries.Add(new ArenaLogEntryDto {
                AttackerAccountId = e.attackerAccountId,
                AttackerNickname = e.attackerNickname ?? "",
                DefenderAccountId = e.defenderAccountId,
                DefenderNickname = e.defenderNickname ?? "",
                AttackerWon = e.attackerWon,
                ScoreDelta = e.scoreDelta,
                CreatedUnix = e.createdUnix,
            });
        return dto;
    }

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            await RequestHomeAsync(ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            // 배지 최신화 실패는 화면을 막을 이유가 없다(다음 조회가 덮는다).
            Debug.LogWarning($"[ArenaService] 쟁탈전 현황을 갱신하지 못했습니다: {e.Message}");
        }
    }
}
