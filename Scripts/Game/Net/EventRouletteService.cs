using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>이벤트 돌림판 통신 구현(정보 조회/돌리기). 다른 이벤트 서비스와 같이 대기자 등록 후 송신 + 짧은 타임아웃.</summary>
public sealed class EventRouletteService : IEventRouletteService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly Dictionary<string, ReactiveProperty<int>> _tokens = new();

    public EventRouletteService(INetworkService net) {
        _net = net;
    }

    public IReadOnlyReactiveProperty<int> TokensOf(string eventKey) => Tokens(eventKey);

    private ReactiveProperty<int> Tokens(string eventKey) {
        string key = eventKey ?? "";
        if (!_tokens.TryGetValue(key, out var prop)) {
            prop = new ReactiveProperty<int>(0);
            _tokens[key] = prop;
        }
        return prop;
    }

    public async UniTask<EventRouletteInfoDto> RequestInfoAsync(string eventKey, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EventRouletteInfo>(cts.Token);
        _net.Send(new C_EventRouletteInfo { eventKey = eventKey ?? "" }.Write());
        var res = await wait;

        var dto = new EventRouletteInfoDto { EventKey = res.eventKey, Tokens = res.tokens };
        foreach (var s in res.slots)
            dto.Slots.Add(RewardItem.FromGrant(s.kind, s.value, s.qty, s.currencyKey));

        Tokens(eventKey).Value = dto.Tokens;
        return dto;
    }

    public async UniTask RefreshAsync(string eventKey, CancellationToken ct = default) {
        try {
            await RequestInfoAsync(eventKey, ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            // 보유량 표시가 늦게 맞는 것뿐이라 화면을 멈추지 않는다. 다음 조회에서 다시 맞춰진다.
            UnityEngine.Debug.LogException(e);
        }
    }

    public async UniTask<EventRouletteSpinResultDto> SpinAsync(string eventKey, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EventRouletteSpinResult>(cts.Token);
        _net.Send(new C_EventRouletteSpin { eventKey = eventKey ?? "" }.Write());
        var res = await wait;

        var dto = new EventRouletteSpinResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            SlotIndex = res.slotIndex,
            Tokens = res.tokens,
            NewGold = res.newGold,
            NewDiamond = res.newDiamond,
            NewStamina = res.newStamina,
        };
        foreach (var g in res.granteds)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));

        // 실패해도 서버가 현재 보유량을 함께 보내므로 그대로 맞춘다(재화 부족이면 0 등).
        if (dto.IsSuccess || dto.FailReason == 2) Tokens(eventKey).Value = dto.Tokens;
        return dto;
    }

    public void Dispose() {
        foreach (var p in _tokens.Values) p.Dispose();
        _tokens.Clear();
    }
}
