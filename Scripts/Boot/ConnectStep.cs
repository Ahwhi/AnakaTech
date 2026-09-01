using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using static Enums;

/// <summary>
/// 2단계: 서버 연결. 지정 시간 내 연결되지 않으면 취소되어 부트 실패로 이어진다.
/// </summary>
public sealed class ConnectStep : IBootStep {
    private const float TimeoutSeconds = 10f;

    private readonly INetworkService _network;

    public BootState State => BootState.ConnectingServer;

    public ConnectStep(INetworkService network) {
        _network = network;
    }

    public async UniTask ExecuteAsync(CancellationToken ct) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

        await _network.ConnectAsync(cts.Token);
    }
}
