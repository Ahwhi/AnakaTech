using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>
/// 무한 타워 통신 구현(현황 조회 / 입장 / 등반 보고). 오늘 오를 수 있는지는 응답이 올 때마다 곧바로 반영해
/// 팝업을 열든 등반하든 배지가 그 자리에서 켜지고 꺼진다.
/// </summary>
public sealed class TowerService : ITowerService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<bool> _canClimbToday = new(false);

    public TowerService(INetworkService net) {
        _net = net;
    }

    public IReadOnlyReactiveProperty<bool> CanClimbToday => _canClimbToday;

    public void Dispose() => _canClimbToday.Dispose();

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            await RequestInfoAsync(ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            // 배지 갱신 실패로 화면 전체가 멈추면 안 된다. 다음 갱신에서 다시 맞춰진다.
            UnityEngine.Debug.LogException(e);
        }
    }

    public async UniTask<TowerInfoDto> RequestInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_TowerInfo>(cts.Token);
        _net.Send(new C_TowerInfo().Write());
        var res = await wait;

        var dto = new TowerInfoDto {
            Floor = res.floor,
            MaxFloor = res.maxFloor,
            CanClimb = res.canClimb,
            Bonus = new TowerBonus((TowerBonusKind)res.bonusKind, res.bonusTarget, res.bonusValue),
        };
        _canClimbToday.Value = dto.CanClimb;
        return dto;
    }

    public async UniTask<TowerEnterDto> EnterAsync(int floor, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_TowerEnterResult>(cts.Token);
        _net.Send(new C_TowerEnter { floor = floor }.Write());
        var res = await wait;

        // 오늘 이미 올랐거나 끝까지 오른 계정이면 오를 수 없다는 뜻이므로 배지도 끈다.
        if (res.failReason == 1 || res.failReason == 4) _canClimbToday.Value = false;
        return new TowerEnterDto { IsSuccess = res.isSuccess, FailReason = res.failReason };
    }

    // 단방향 보고라 기다릴 응답이 없다(멸망전 완주 보고와 같은 방식).
    public void ReportFinished() =>
        _net.Send(new C_BattleFinished { content = BattleFinishContent.Tower }.Write());

    public async UniTask<TowerClearDto> ClearAsync(int floor, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_TowerClearResult>(cts.Token);
        _net.Send(new C_TowerClear { floor = floor }.Write());
        var res = await wait;

        var dto = new TowerClearDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            ClearedFloor = res.clearedFloor,
            NewDiamond = res.newDiamond,
            Next = new TowerInfoDto {
                Floor = res.floor,
                MaxFloor = TowerRules.MaxFloor,
                CanClimb = res.canClimb,
                Bonus = new TowerBonus((TowerBonusKind)res.bonusKind, res.bonusTarget, res.bonusValue),
            },
        };
        foreach (var g in res.granteds)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));

        // 실패 응답이 2(계정 없음)면 다음 층 정보가 비어 있으므로 배지를 건드리지 않는다.
        if (res.failReason != 2) _canClimbToday.Value = res.canClimb;
        return dto;
    }
}
