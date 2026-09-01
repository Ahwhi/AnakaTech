using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>발송 주소 등록/해제 통신 창구.</summary>
public interface IPushTokenService {
    /// <summary>이 설치의 발송 주소를 서버에 등록하거나 갱신한다.</summary>
    UniTask<bool> RegisterAsync(string installationId, int platform, string token, int apnsEnvironment,
                                CancellationToken ct = default);

    /// <summary>이 설치의 발송 주소를 서버에서 지운다.</summary>
    UniTask<bool> UnregisterAsync(string installationId, CancellationToken ct = default);
}

/// <summary>
/// 발송 주소 등록/해제 통신 구현.
///
/// 이 패킷은 "어디로 보낼 수 있는가"만 전달한다. 광고성 정보 수신 동의는 약관 패킷이 유일한 권위이며,
/// 여기서 동의를 만들거나 바꾸지 않는다.
///
/// 토큰은 로그에 남기지 않는다. 실패해도 토큰 값을 출력하지 않고 결과 코드만 남긴다.
/// </summary>
public sealed class PushTokenService : IPushTokenService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public PushTokenService(INetworkService net) {
        _net = net;
    }

    public async UniTask<bool> RegisterAsync(string installationId, int platform, string token, int apnsEnvironment,
                                             CancellationToken ct = default) {
        if (string.IsNullOrEmpty(installationId) || string.IsNullOrEmpty(token)) return false;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_PushTokenResult>(cts.Token);
        _net.Send(new C_RegisterPushToken {
            installationId = installationId,
            platform = platform,
            token = token,
            apnsEnvironment = apnsEnvironment,
        }.Write());

        var res = await wait;
        return res.isSuccess;
    }

    public async UniTask<bool> UnregisterAsync(string installationId, CancellationToken ct = default) {
        if (string.IsNullOrEmpty(installationId)) return false;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_PushTokenResult>(cts.Token);
        _net.Send(new C_UnregisterPushToken { installationId = installationId }.Write());

        var res = await wait;
        return res.isSuccess;
    }
}
