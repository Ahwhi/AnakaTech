using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>
/// 영웅전 통신 구현(현황 조회 / 입장 / 클리어 보고). 오늘 도전할 수 있는지는 응답이 올 때마다 곧바로 반영해
/// 팝업을 열든 입장하든 배지가 그 자리에서 켜지고 꺼진다.
/// </summary>
public sealed class HeroesService : IHeroesService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<bool> _canEnterToday = new(false);

    // 마지막으로 안 단계. 입장 응답에는 단계가 없으므로 배지 판정(끝까지 깼는지)에 이 값을 쓴다.
    private int _stage = 1;

    public HeroesService(INetworkService net) {
        _net = net;
    }

    public IReadOnlyReactiveProperty<bool> CanEnterToday => _canEnterToday;

    public void Dispose() => _canEnterToday.Dispose();

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            await RequestInfoAsync(ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            // 배지 갱신 실패로 화면 전체가 멈추면 안 된다. 다음 갱신에서 다시 맞춰진다.
            UnityEngine.Debug.LogException(e);
        }
    }

    public async UniTask<HeroesInfoDto> RequestInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_HeroesInfo>(cts.Token);
        _net.Send(new C_HeroesInfo().Write());
        var res = await wait;

        var dto = new HeroesInfoDto {
            Stage = res.stage,
            MaxStage = res.maxStage,
            RemainEntries = res.remainEntries,
            DailyEntries = res.dailyEntries,
        };
        Apply(dto.Stage, dto.RemainEntries);
        return dto;
    }

    public async UniTask<HeroesEnterDto> EnterAsync(int stage, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_HeroesEnterResult>(cts.Token);
        _net.Send(new C_HeroesEnter { stage = stage }.Write());
        var res = await wait;

        // 계정 없음(2)이면 남은 횟수가 비어 있으므로 배지를 건드리지 않는다. 최대 단계 도달(4)은 더 칠 단계가 없다.
        if (res.failReason == 4) _canEnterToday.Value = false;
        else if (res.failReason != 2) Apply(_stage, res.remainEntries);
        return new HeroesEnterDto { IsSuccess = res.isSuccess, FailReason = res.failReason, RemainEntries = res.remainEntries };
    }

    public async UniTask<HeroesClearDto> ClearAsync(int stage, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_HeroesClearResult>(cts.Token);
        _net.Send(new C_HeroesClear { stage = stage }.Write());
        var res = await wait;

        var dto = new HeroesClearDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            ClearedStage = res.clearedStage,
            NewGold = res.newGold,
            NewDiamond = res.newDiamond,
            Next = new HeroesInfoDto {
                Stage = res.stage,
                MaxStage = HeroesRules.MaxStage,
                RemainEntries = res.remainEntries,
                DailyEntries = HeroesRules.DailyEntries,
            },
        };
        foreach (var g in res.granteds)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));

        // 계정 없음(2)이면 다음 단계 정보가 비어 있으므로 배지를 건드리지 않는다.
        if (res.failReason != 2) Apply(res.stage, res.remainEntries);
        return dto;
    }

    private void Apply(int stage, int remainEntries) {
        _stage = stage;
        _canEnterToday.Value = stage <= HeroesRules.MaxStage && remainEntries > 0;
    }
}
