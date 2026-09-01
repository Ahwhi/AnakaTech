using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ServerCore;
using UniRx;

/// <summary>우편함 통신 구현(목록 요청/수령). 미수령 수는 ReactiveProperty 로 노출해 배지가 구독한다.
/// 접속 중 우편 도착은 서버 푸시(S_MailNotify)를 INetworkService.PacketReceived 로 상시 수신해 즉시 반영한다.</summary>
public sealed class MailService : IMailService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<int> _unread = new(0);

    public IReadOnlyReactiveProperty<int> UnreadCount => _unread;

    public MailService(INetworkService net) {
        _net = net;
        // 서버 푸시로 즉시 배지 갱신. NetworkManager 는 루트 스코프(장수명)이고 이 서비스는 씬 스코프라
        // 재진입 시 중복 구독/누수를 막기 위해 Dispose 에서 반드시 해제한다.
        if (_net != null) _net.PacketReceived += OnPacketReceived;
    }

    // 우편 도착 푸시: 서버가 계산한 미수령 수를 그대로 반영한다(추가 통신 없이 배지 즉시 갱신).
    private void OnPacketReceived(IPacket packet) {
        if (packet is S_MailNotify notify) _unread.Value = notify.unreadCount;
    }

    public void Dispose() {
        if (_net != null) _net.PacketReceived -= OnPacketReceived;
    }

    public async UniTask<List<MailDto>> RequestMailAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_MailList>(cts.Token);
        _net.Send(new C_RequestMail().Write());
        var res = await wait;

        // 보상은 mailId 참조 평면 리스트로 내려오므로 mailId 로 묶어 각 우편에 붙인다(중첩 리스트 미지원 대응).
        var byId = new Dictionary<long, MailDto>();
        var list = new List<MailDto>();
        foreach (var m in res.mails) {
            var dto = new MailDto {
                MailId = m.mailId,
                Title = m.title,
                Body = m.body,
                CreatedAt = FromUnix(m.createdUnix),
                ExpiresAt = FromUnix(m.expiresUnix),
            };
            byId[m.mailId] = dto;
            list.Add(dto);
        }
        foreach (var r in res.rewards) {
            if (byId.TryGetValue(r.mailId, out var dto))
                dto.Rewards.Add(new MailRewardDto { Kind = (MailRewardKind)r.kind, Value = r.value, Qty = r.qty, CurrencyKey = r.currencyKey });
        }

        _unread.Value = list.Count;   // 서버는 미수령 우편만 내려주므로 목록 수 = 미수령 수
        return list;
    }

    public UniTask<MailClaimResultDto> ClaimMailAsync(long mailId, CancellationToken ct = default)
        => ClaimInternalAsync(mailId, false, ct);

    public UniTask<MailClaimResultDto> ClaimAllAsync(CancellationToken ct = default)
        => ClaimInternalAsync(0, true, ct);

    private async UniTask<MailClaimResultDto> ClaimInternalAsync(long mailId, bool all, CancellationToken ct) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ClaimMailResult>(cts.Token);
        _net.Send(new C_ClaimMail { mailId = mailId, claimAll = all }.Write());
        var res = await wait;

        var dto = new MailClaimResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            NewGold = res.newGold,
            NewDiamond = res.newDiamond,
            NewStamina = res.newStamina,
        };
        foreach (var g in res.grantedRewards)
            dto.Granted.Add(new MailRewardDto { Kind = (MailRewardKind)g.kind, Value = g.value, Qty = g.qty, CurrencyKey = g.currencyKey });
        foreach (var id in res.claimedMailIds)
            dto.ClaimedMailIds.Add(id.mailId);

        if (dto.IsSuccess)
            _unread.Value = Math.Max(0, _unread.Value - dto.ClaimedMailIds.Count);
        return dto;
    }

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try { await RequestMailAsync(ct); }
        catch (OperationCanceledException) { }
    }

    private static DateTime FromUnix(long unixSeconds) =>
        DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime().DateTime;
}
