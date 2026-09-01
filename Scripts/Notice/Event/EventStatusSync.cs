using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 이벤트 보상 상태 동기화 구현. C_EventStatus 를 보내고 S_EventStatus 응답을 받아 EventStatusProvider 에 반영한다.
/// 응답을 기다리는 동안 무한 대기하지 않도록 짧은 타임아웃을 둔다(서버 미구현/지연 시 상태 없이 진행).
///
/// 주의: 응답 대기(WaitPacketAsync)는 Send 전에 등록해야 응답을 놓치지 않는다(대기자 우선 등록).
/// 서버가 보내는 S_EventStatus.statusess 의 state 값은 0=None, 1=Claimable, 2=Claimed 로 약속한다.
/// </summary>
public sealed class EventStatusSync : IEventStatusSync {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(3);

    private readonly INetworkService _net;
    private readonly IEventStatusProvider _provider;

    public EventStatusSync(INetworkService net, IEventStatusProvider provider) {
        _net = net;
        _provider = provider;
    }

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        if (!_net.IsConnected) return;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(ResponseTimeout);

        try {
            // 대기자를 먼저 등록한 뒤 요청을 보낸다(응답이 먼저 도착해도 놓치지 않도록).
            var waitTask = _net.WaitPacketAsync<S_EventStatus>(timeoutCts.Token);
            _net.Send(new C_EventStatus().Write());
            var res = await waitTask;

            _provider.Clear();
            if (res.statusess != null) {
                foreach (var s in res.statusess)
                    _provider.Set(s.rewardKey, ToState(s.state));
            }
        } catch (OperationCanceledException) {
            // 서버 미구현 또는 응답 지연. 상태 없이 진행한다(Claimable=빨간 동그라미 배지는 뜨지 않음).
        }
    }

    private static EventClaimState ToState(int value) {
        switch (value) {
            case 1: return EventClaimState.Claimable;
            case 2: return EventClaimState.Claimed;
            default: return EventClaimState.None;
        }
    }
}
