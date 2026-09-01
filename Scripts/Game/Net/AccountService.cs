using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 계정 관리 통신 구현(회원 탈퇴). 삭제 범위와 보관 규칙은 전부 서버가 정하고,
/// 여기서는 요청을 보내고 결과를 옮기기만 한다.
///
/// 탈퇴가 성공하면 서버가 연결을 끊는다. 응답을 받은 뒤 연결이 끊기는 것은 정상 흐름이다.
/// </summary>
public sealed class AccountService : IAccountService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(15);

    private readonly INetworkService _net;

    public AccountService(INetworkService net) => _net = net;

    public async UniTask<WithdrawResultDto> WithdrawAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_WithdrawAccountResult>(cts.Token);
        _net.Send(new C_WithdrawAccount().Write());
        var res = await wait;

        return new WithdrawResultDto {
            IsSuccess  = res.isSuccess,
            FailReason = res.failReason,
        };
    }
}
