using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>
/// 전선 통신 구현(현황 조회 / 배치 저장 / 초기화 / 입장 / 종료 보고).
///
/// 쓰기 응답은 바뀐 부분만 돌려주므로, 마지막 현황(Info)에 그 부분을 덮어쓰고 다시 알린다.
/// 그래서 전선 팝업과 배치 팝업은 서로를 참조하지 않고 Info 만 구독해도 같은 상태를 본다.
/// </summary>
public sealed class FrontlineService : IFrontlineService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<FrontlineInfoDto> _info = new(null);

    public FrontlineService(INetworkService net) {
        _net = net;
    }

    public IReadOnlyReactiveProperty<FrontlineInfoDto> Info => _info;

    public void Dispose() => _info.Dispose();

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            await RequestInfoAsync(ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            // 배지 갱신 실패로 화면 전체가 멈추면 안 된다. 다음 갱신에서 다시 맞춰진다.
            UnityEngine.Debug.LogException(e);
        }
    }

    public async UniTask<FrontlineInfoDto> RequestInfoAsync(CancellationToken ct = default) {
        var res = await RoundTripAsync<S_FrontlineInfo>(new C_FrontlineInfo().Write(), ct);

        var dto = new FrontlineInfoDto {
            MaxStage = res.maxStage,
            StaminaPerClear = res.staminaPerClear,
            DailyStaminaLimit = res.dailyStaminaLimit,
            DailyStaminaUsed = res.dailyStaminaUsed,
        };
        foreach (var f in res.frontlines) {
            var squad = new FrontlineSquadDto { FrontlineId = f.frontlineId, Stage = f.stage, Locked = f.locked };
            IdCsv.Parse(f.membersCsv, squad.Members);
            dto.Squads.Add(squad);
        }
        _info.SetValueAndForceNotify(dto);
        return dto;
    }

    public async UniTask<FrontlineActionDto> SetMembersAsync(int frontlineId, IReadOnlyList<int> members,
                                                             CancellationToken ct = default) {
        var pkt = new C_FrontlineSetMembers { frontlineId = frontlineId, membersCsv = IdCsv.Format(members) };
        var res = await RoundTripAsync<S_FrontlineSetMembersResult>(pkt.Write(), ct);

        // 실패해도 서버가 가진 배치를 돌려주므로 그대로 덮어 화면을 서버와 맞춘다(계정 없음은 내용이 비어 있어 건너뛴다).
        if (res.failReason != 2)
            UpdateSquad(frontlineId, s => {
                IdCsv.Parse(res.membersCsv, s.Members);
                s.Locked = res.locked;
            });
        return new FrontlineActionDto { IsSuccess = res.isSuccess, FailReason = res.failReason };
    }

    public async UniTask<FrontlineActionDto> ResetAsync(int frontlineId, CancellationToken ct = default) {
        var res = await RoundTripAsync<S_FrontlineResetResult>(new C_FrontlineReset { frontlineId = frontlineId }.Write(), ct);

        if (res.isSuccess)
            UpdateSquad(frontlineId, s => {
                s.Stage = res.stage;
                s.Members.Clear();
                s.Locked = false;
            });
        return new FrontlineActionDto { IsSuccess = res.isSuccess, FailReason = res.failReason };
    }

    public async UniTask<FrontlineActionDto> EnterAsync(int frontlineId, int stage, CancellationToken ct = default) {
        var pkt = new C_FrontlineEnter { frontlineId = frontlineId, stage = stage };
        var res = await RoundTripAsync<S_FrontlineEnterResult>(pkt.Write(), ct);

        if (res.failReason != 2) UpdateDailyUsed(res.dailyStaminaUsed);
        return new FrontlineActionDto {
            IsSuccess = res.isSuccess, FailReason = res.failReason,
            NewStamina = res.failReason != 2 ? res.newStamina : -1,
        };
    }

    public async UniTask<FrontlineFinishDto> FinishAsync(int frontlineId, int stage, bool cleared,
                                                         CancellationToken ct = default) {
        var pkt = new C_FrontlineFinish { frontlineId = frontlineId, stage = stage, cleared = cleared };
        var res = await RoundTripAsync<S_FrontlineFinishResult>(pkt.Write(), ct);

        var dto = new FrontlineFinishDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Cleared = res.cleared,
            Stage = res.stage,
            GainedExp = res.gainedExp,
            NewStamina = res.newStamina,
        };
        foreach (var e in res.charExps)
            dto.CharExp.Add(new CharExpGain {
                CharDataId = e.charDataId, GainedExp = e.gainedExp, NewLevel = e.newLevel,
                NewExp = e.newExp, MaxExp = e.maxExp, GainedStatPoints = e.gainedStatPoints,
            });

        if (res.failReason != 2) {
            UpdateDailyUsed(res.dailyStaminaUsed);
            UpdateSquad(frontlineId, s => {
                s.Stage = res.stage;
                s.Locked = res.locked;
            });
        }
        return dto;
    }

    // 요청 1건을 보내고 그 응답을 기다린다(응답이 늦으면 시간 초과로 취소).
    private async UniTask<T> RoundTripAsync<T>(ArraySegment<byte> packet, CancellationToken ct) where T : IPacket {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<T>(cts.Token);
        _net.Send(packet);
        return await wait;
    }

    // 마지막 현황의 전선 하나를 고치고 구독자에게 다시 알린다. 현황을 아직 받기 전이면 고칠 대상이 없다.
    private void UpdateSquad(int frontlineId, Action<FrontlineSquadDto> apply) {
        var squad = _info.Value?.Squad(frontlineId);
        if (squad == null) return;
        apply(squad);
        _info.SetValueAndForceNotify(_info.Value);
    }

    private void UpdateDailyUsed(int used) {
        if (_info.Value == null) return;
        _info.Value.DailyStaminaUsed = used;
        _info.SetValueAndForceNotify(_info.Value);
    }
}
