using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 다이아 보유 내역 통신 구현.
///
/// 요청 패킷에는 필드가 없다. 어느 계정인지는 세션이 알고 있고, 보유량은 서버가 권위이기 때문이다.
/// </summary>
public sealed class DiamondDetailService : IDiamondDetailService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public DiamondDetailService(INetworkService net) {
        _net = net;
    }

    public async UniTask<DiamondDetailDto> RequestAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DiamondDetail>(cts.Token);
        _net.Send(new C_DiamondDetail().Write());
        var res = await wait;

        return new DiamondDetailDto { Paid = res.paid, Free = res.free };
    }
}
