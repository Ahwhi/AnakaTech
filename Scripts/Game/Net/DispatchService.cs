using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ServerCore;
using UniRx;
using UnityEngine;

/// <summary>
/// 파견 서버 통신 구현. 슬롯 현황/조건표/보상 구성을 조회 1회로 함께 받고, 쓰기 작업은 하나의 창구로 보낸다.
///
/// 배지(수령 가능한 파견 존재)는 이 서비스가 판단한다. 근거가 "완료 시각이 지났는가"라 시간이 흐르면 저절로
/// 참이 되므로, 마지막으로 받은 완료 시각들을 들고 있다가 주기적으로 다시 따진다(서버를 다시 두드리지 않는다).
/// 화면이 열려 있지 않아도 배지가 켜져야 하므로 이 판정을 팝업에 두지 않는다(수련과 같은 구조).
/// </summary>
public sealed class DispatchService : IDispatchService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    // 완료 시각 재판정 주기. 배지가 몇 초 늦게 켜지는 것은 문제가 되지 않으므로 넉넉히 둔다.
    private const float WatchIntervalSeconds = 5f;

    private readonly INetworkService _net;

    // 마지막으로 받은 슬롯별 완료 시각(unix). 진행 중인 슬롯만 담긴다.
    private readonly List<long> _endUnix = new();
    private readonly ReactiveProperty<bool> _hasClaimable = new(false);

    private readonly Subject<Unit> _changedByServer = new();

    private CancellationTokenSource _watchCts;
    private bool _disposed;

    public DispatchService(INetworkService net) {
        _net = net;
        // 서버가 파견 상태를 바꿔 알려 오는 푸시(S_DispatchNotify)를 상시 수신한다.
        // NetworkManager 는 루트 스코프(장수명)이고 이 서비스는 씬 스코프라 Dispose 에서 반드시 해제한다.
        if (_net != null) _net.PacketReceived += OnPacketReceived;
    }

    public IReadOnlyReactiveProperty<bool> HasClaimable => _hasClaimable;

    public IObservable<Unit> ChangedByServer => _changedByServer;

    // 파견 상태 푸시: 서버가 알려 준 완료 시각을 그대로 배지 판정 근거로 삼는다.
    // 이게 없으면 서버에서만 시각이 바뀐 경우(운영 도구/개발 명령) 클라가 다시 조회할 때까지 알 수 없다.
    private void OnPacketReceived(IPacket packet) {
        if (packet is not S_DispatchNotify notify) return;

        _endUnix.Clear();
        foreach (var s in notify.slots)
            if (s.endUnix > 0) _endUnix.Add(s.endUnix);

        Recompute();
        EnsureWatching();
        _changedByServer.OnNext(Unit.Default);
    }

    public async UniTask<DispatchInfoDto> RequestInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DispatchInfo>(cts.Token);
        _net.Send(new C_DispatchInfo().Write());
        var res = await wait;

        var dto = new DispatchInfoDto {
            RefreshCharges = res.refreshCharges,
            MaxRefreshCharges = res.maxRefreshCharges,
            MembersPerSlot = res.membersPerSlot,
        };
        foreach (var s in res.slots) {
            var slot = new DispatchSlotDto {
                SlotIndex = s.slotIndex, ConditionId = s.conditionId,
                StartUnix = s.startUnix, EndUnix = s.endUnix,
            };
            IdCsv.Parse(s.membersCsv, slot.Members);
            dto.Slots.Add(slot);
        }
        foreach (var c in res.conditions)
            dto.Conditions.Add(new DispatchConditionDto {
                ConditionId = c.conditionId, Kind = (DispatchConditionKind)c.kind, Param = c.param,
                Threshold = c.threshold, Tier = c.tier, DurationSeconds = c.durationSeconds,
            });
        foreach (var g in res.tierGrants) {
            if (!dto.TierGrants.TryGetValue(g.tier, out var list)) {
                list = new List<RewardItem>();
                dto.TierGrants[g.tier] = list;
            }
            list.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        }

        TrackEndTimes(dto.Slots);
        return dto;
    }

    public async UniTask<DispatchActionResultDto> ActAsync(DispatchActionType action, int slotIndex = 0,
                                                          IReadOnlyList<int> members = null,
                                                          CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DispatchActionResult>(cts.Token);
        _net.Send(new C_DispatchAction {
            action = (int)action, slotIndex = slotIndex, membersCsv = IdCsv.Format(members)
        }.Write());
        var res = await wait;

        var dto = new DispatchActionResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Action = (DispatchActionType)res.action,
            SlotIndex = res.slotIndex,
            ConditionId = res.conditionId,
            StartUnix = res.startUnix,
            EndUnix = res.endUnix,
            RefreshCharges = res.refreshCharges,
            NewGold = res.newGold,
        };
        IdCsv.Parse(res.membersCsv, dto.Members);
        foreach (var g in res.grantedRewards)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        return dto;
    }

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            await RequestInfoAsync(ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            // 배지 최신화 실패는 화면을 막을 이유가 없다(다음 조회가 덮는다).
            Debug.LogWarning($"[DispatchService] 파견 현황을 갱신하지 못했습니다: {e.Message}");
        }
    }

    public void TrackEndTimes(IReadOnlyList<DispatchSlotDto> slots) {
        _endUnix.Clear();
        if (slots != null)
            foreach (var s in slots)
                if (s != null && s.IsRunning && s.EndUnix > 0) _endUnix.Add(s.EndUnix);

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

    // 진행 중인 파견이 남아 있는 동안만 재판정 루프를 돌린다(다 비면 스스로 멎는다).
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

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        if (_net != null) _net.PacketReceived -= OnPacketReceived;
        _watchCts?.Cancel();
        _changedByServer.Dispose();
        _hasClaimable.Dispose();
    }
}
