using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

/// <summary>
/// 수련 서버 통신 구현. 슬롯 현황/결과/확률표를 조회 1회로 함께 받고, 쓰기 작업은 하나의 창구로 보낸다.
///
/// 배지(수령 가능한 수련 존재)는 이 서비스가 판단한다. 근거가 "완료 시각이 지났는가"라 시간이 흐르면 저절로
/// 참이 되므로, 마지막으로 받은 완료 시각들을 들고 있다가 주기적으로 다시 따진다(서버를 다시 두드리지 않는다).
/// 화면이 열려 있지 않아도 배지가 켜져야 하므로 이 판정을 팝업에 두지 않는다.
/// </summary>
public sealed class TrainingService : ITrainingService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    // 완료 시각 재판정 주기. 배지가 몇 초 늦게 켜지는 것은 문제가 되지 않으므로 넉넉히 둔다.
    private const float WatchIntervalSeconds = 5f;

    private readonly INetworkService _net;

    // 마지막으로 받은 슬롯별 완료 시각(unix). 진행 중인 슬롯만 담긴다.
    private readonly List<long> _endUnix = new();
    private readonly ReactiveProperty<bool> _hasClaimable = new(false);

    private CancellationTokenSource _watchCts;

    public TrainingService(INetworkService net) => _net = net;

    public IReadOnlyReactiveProperty<bool> HasClaimable => _hasClaimable;

    public async UniTask<TrainingInfoDto> RequestInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_TrainingInfo>(cts.Token);
        _net.Send(new C_TrainingInfo().Write());
        var res = await wait;

        var dto = new TrainingInfoDto {
            UnlockedSlots = res.unlockedSlots,
            MaxSlots = res.maxSlots,
            NextUnlockCost = res.nextUnlockCost,
            DurationSeconds = res.durationSeconds,
            TrainingTicket = res.trainingTicket,
            InstantTicketCost = res.instantTicketCost,
        };
        foreach (var s in res.slots)
            dto.Slots.Add(new TrainingSlotDto {
                SlotIndex = s.slotIndex, CharDataId = s.charDataId,
                StartUnix = s.startUnix, EndUnix = s.endUnix,
            });
        foreach (var r in res.results)
            dto.Results.Add(new TrainingResultDto {
                CharDataId = r.charDataId, Effect = r.effect, ValueTenth = r.valueTenth,
            });
        foreach (var r in res.rates)
            dto.Rates.Add(new TrainingRateDto {
                Effect = r.effect, Weight = r.weight, TotalWeight = r.totalWeight,
                MinTenth = r.minTenth, MaxTenth = r.maxTenth, StepTenth = r.stepTenth,
            });

        TrackEndTimes(dto.Slots);
        return dto;
    }

    public async UniTask<TrainingActionResultDto> ActAsync(TrainingActionType action, int slotIndex = 0,
                                                          int charDataId = 0, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_TrainingActionResult>(cts.Token);
        _net.Send(new C_TrainingAction {
            action = (int)action, slotIndex = slotIndex, charDataId = charDataId
        }.Write());
        var res = await wait;

        return new TrainingActionResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Action = (TrainingActionType)res.action,
            SlotIndex = res.slotIndex,
            CharDataId = res.charDataId,
            NewDiamond = res.newDiamond,
            NewTrainingTicket = res.newTrainingTicket,
            UnlockedSlots = res.unlockedSlots,
            StartUnix = res.startUnix,
            EndUnix = res.endUnix,
            Effect = res.effect,
            ValueTenth = res.valueTenth,
        };
    }

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            await RequestInfoAsync(ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            // 배지 최신화 실패는 화면을 막을 이유가 없다(다음 조회가 덮는다).
            Debug.LogWarning($"[TrainingService] 수련 현황을 갱신하지 못했습니다: {e.Message}");
        }
    }

    /// <summary>
    /// 슬롯 목록을 배지 판정 근거로 갱신한다. 팝업이 작업 응답으로 이미 아는 최신 상태를 넘겨,
    /// 배지를 맞추려고 서버를 다시 두드리지 않게 한다.
    /// </summary>
    public void TrackEndTimes(IReadOnlyList<TrainingSlotDto> slots) {
        _endUnix.Clear();
        if (slots != null)
            foreach (var s in slots)
                if (s != null && s.CharDataId != 0 && s.EndUnix > 0) _endUnix.Add(s.EndUnix);

        Recompute();
        EnsureWatching();
    }

    // 완료 시각이 하나라도 지났으면 배지를 켠다. 시간만 보는 순수 계산이라 통신이 없다.
    private void Recompute() {
        long now = KstTime.NowUnix;
        bool claimable = false;
        for (int i = 0; i < _endUnix.Count; i++)
            if (_endUnix[i] <= now) { claimable = true; break; }
        _hasClaimable.Value = claimable;
    }

    // 진행 중인 수련이 남아 있는 동안만 재판정 루프를 돌린다(다 비면 스스로 멎는다).
    private void EnsureWatching() {
        if (_disposed || _endUnix.Count == 0 || _watchCts != null) return;
        var cts = new CancellationTokenSource();
        _watchCts = cts;
        WatchAsync(cts).Forget();
    }

    private async UniTaskVoid WatchAsync(CancellationTokenSource cts) {
        try {
            while (!_disposed && _endUnix.Count > 0) {
                await UniTask.Delay(TimeSpan.FromSeconds(WatchIntervalSeconds), ignoreTimeScale: true,
                                    cancellationToken: cts.Token);
                Recompute();
            }
        } catch (OperationCanceledException) {
        } finally {
            // 자기 루프가 만든 토큰만 정리한다(그 사이에 새 루프가 시작됐으면 그쪽 것을 건드리지 않는다).
            if (_watchCts == cts) _watchCts = null;
            cts.Dispose();
        }
    }

    private bool _disposed;

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        _watchCts?.Cancel();
        _hasClaimable.Dispose();
    }
}
