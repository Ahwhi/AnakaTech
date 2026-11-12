using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ServerCore;
using UniRx;

/// <summary>
/// 친구 통신 구현. 패킷을 주고받아 DTO 로 변환한다(다른 서비스와 동일한 WaitPacketAsync + Send 패턴).
/// 판정은 전부 서버가 하므로 여기서는 값을 옮기기만 하고 규칙을 재현하지 않는다.
///
/// 받은 신청 수는 ReactiveProperty 로 노출해 배지가 구독한다. 접속 중 신청이 오거나 취소되는 것은
/// 서버 푸시(S_FriendNotify)를 상시 수신해 즉시 반영한다(우편 배지와 같은 방식).
/// </summary>
public sealed class FriendService : IFriendService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<int> _pendingRequests = new(0);
    private readonly ReactiveProperty<FriendStaminaState> _staminaState = new(default);

    public IReadOnlyReactiveProperty<int> PendingRequests => _pendingRequests;
    public IReadOnlyReactiveProperty<FriendStaminaState> StaminaState => _staminaState;

    public FriendService(INetworkService net) {
        _net = net;
        // NetworkManager 는 루트 스코프(장수명)이고 이 서비스는 씬 스코프라, 재진입 시 중복 구독/누수를
        // 막기 위해 Dispose 에서 반드시 해제한다(MailService 와 같은 이유).
        if (_net != null) _net.PacketReceived += OnPacketReceived;
    }

    // 친구 신청 도착/취소, 기력 선물 도착 푸시: 서버가 센 값을 그대로 반영한다(추가 통신 없이 배지 즉시 갱신).
    private void OnPacketReceived(IPacket packet) {
        if (packet is not S_FriendNotify notify) return;
        _pendingRequests.Value = notify.pendingRequests;
        _staminaState.Value = new FriendStaminaState(notify.staminaSendable, notify.staminaClaimable);
    }

    public void Dispose() {
        if (_net != null) _net.PacketReceived -= OnPacketReceived;
    }

    public async UniTask<FriendHomeDto> RequestHomeAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_FriendHome>(cts.Token);
        _net.Send(new C_FriendHome().Write());
        var res = await wait;

        var dto = new FriendHomeDto { MaxFriends = res.maxFriends };
        foreach (var f in res.friends) {
            dto.Friends.Add(new FriendUserDto {
                AccountId = f.accountId, Nickname = f.nickname,
                ProfileFrameId = f.profileFrameId, RepresentativeCharId = f.representativeCharId,
                RepresentativeCostumeId = f.representativeCostumeId,
                CombatPower = f.combatPower,
                IsOnline = f.online, HasPresence = true,
                StaminaSentToMe = f.staminaSentToMe,
            });
        }
        _staminaState.Value = new FriendStaminaState(res.staminaSendable, res.staminaClaimable);
        foreach (var r in res.requests) {
            dto.Requests.Add(new FriendUserDto {
                AccountId = r.accountId, Nickname = r.nickname,
                ProfileFrameId = r.profileFrameId, RepresentativeCharId = r.representativeCharId,
                RepresentativeCostumeId = r.representativeCostumeId,
                CombatPower = r.combatPower,
                CreatedUnix = r.createdUnix,
            });
        }
        foreach (var b in res.blockeds) {
            dto.Blocked.Add(new FriendUserDto {
                AccountId = b.accountId, Nickname = b.nickname,
                ProfileFrameId = b.profileFrameId, RepresentativeCharId = b.representativeCharId,
                RepresentativeCostumeId = b.representativeCostumeId,
                CombatPower = b.combatPower,
                CreatedUnix = b.createdUnix,
            });
        }

        _pendingRequests.Value = dto.Requests.Count;
        return dto;
    }

    public async UniTask<FriendActionResultDto> ActAsync(FriendActionType action, int targetAccountId,
                                                        CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_FriendActionResult>(cts.Token);
        _net.Send(new C_FriendAction { action = (int)action, targetAccountId = targetAccountId }.Write());
        var res = await wait;

        var dto = new FriendActionResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Action = (FriendActionType)res.action,
            TargetAccountId = res.targetAccountId,
            Relation = (FriendRelation)res.relation,
            Blocked = res.blocked,
            PendingRequests = res.pendingRequests,
        };

        // 수락/거절은 내 신청함을 줄이므로 서버가 센 값으로 배지를 그 자리에서 맞춘다.
        if (dto.IsSuccess) _pendingRequests.Value = dto.PendingRequests;
        return dto;
    }

    public async UniTask<FriendStaminaResultDto> SendStaminaAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_FriendStaminaSendResult>(cts.Token);
        _net.Send(new C_FriendStaminaSend().Write());
        var res = await wait;

        var dto = new FriendStaminaResultDto {
            IsSuccess = res.isSuccess, FailReason = res.failReason, Amount = res.sentCount,
            State = new FriendStaminaState(res.staminaSendable, res.staminaClaimable),
        };
        // 실패해도 서버가 지금 상태를 함께 주므로 그대로 맞춘다(버튼/배지가 옛 상태로 남지 않게).
        _staminaState.Value = dto.State;
        return dto;
    }

    public async UniTask<FriendStaminaResultDto> ClaimStaminaAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_FriendStaminaClaimResult>(cts.Token);
        _net.Send(new C_FriendStaminaClaim().Write());
        var res = await wait;

        var dto = new FriendStaminaResultDto {
            IsSuccess = res.isSuccess, FailReason = res.failReason, Amount = res.claimedStamina,
            NewStamina = res.newStamina,
            State = new FriendStaminaState(res.staminaSendable, res.staminaClaimable),
        };
        _staminaState.Value = dto.State;
        return dto;
    }

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try { await RequestHomeAsync(ct); }
        catch (OperationCanceledException) { }
    }

    public async UniTask<FriendSearchResultDto> SearchAsync(string nickname, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_SearchFriendResult>(cts.Token);
        _net.Send(new C_SearchFriend { nickname = nickname ?? "" }.Write());
        var res = await wait;

        var dto = new FriendSearchResultDto { FailReason = res.failReason };
        foreach (var u in res.users) {
            dto.Users.Add(new FriendSearchEntry {
                User = new FriendUserDto {
                    AccountId = u.accountId, Nickname = u.nickname,
                    ProfileFrameId = u.profileFrameId, RepresentativeCharId = u.representativeCharId,
                    RepresentativeCostumeId = u.representativeCostumeId,
                    CombatPower = u.combatPower,
                    IsOnline = u.online, HasPresence = true,
                },
                Relation = (FriendRelation)u.relation,
                Blocked = u.blocked,
            });
        }
        return dto;
    }
}
