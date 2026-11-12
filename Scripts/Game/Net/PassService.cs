using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ServerCore;
using UniRx;

/// <summary>
/// 패스 통신 구현(현황 조회 / 보상 수령 + 현황 푸시 수신).
///
/// 패스 레벨은 던전을 깨며 기력을 쓸 때 오른다. 서버는 새 레벨에 닿는 순간 S_PassInfo 를 밀어 주므로,
/// 이 서비스가 그 푸시를 상시 받아 현황과 배지를 그 자리에서 바꾼다(패스 화면을 열거나 재접속할 필요가 없다).
/// 조회 응답도 같은 패킷이라 반영 경로는 OnPacketReceived 하나뿐이다.
/// </summary>
public sealed class PassService : IPassService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<PassInfoDto> _info = new(null);
    private readonly ReactiveProperty<bool> _hasClaimable = new(false);

    public PassService(INetworkService net) {
        _net = net;
        // NetworkManager 는 루트 스코프(장수명)이고 이 서비스는 씬 스코프라 Dispose 에서 반드시 해제한다.
        if (_net != null) _net.PacketReceived += OnPacketReceived;
    }

    public IReadOnlyReactiveProperty<PassInfoDto> Info => _info;
    public IReadOnlyReactiveProperty<bool> HasClaimable => _hasClaimable;

    // 조회 응답이자 서버 푸시. 어느 쪽이든 같은 값이라 여기 한 곳에서 반영한다.
    private void OnPacketReceived(IPacket packet) {
        if (packet is S_PassInfo res) Apply(ToDto(res));
    }

    private static PassInfoDto ToDto(S_PassInfo res) => new() {
        StaminaSpent = res.staminaSpent,
        Level = res.level,
        FreeClaimedCount = res.freeClaimedCount,
        PassClaimedCount = res.passClaimedCount,
        HasPass = res.hasPass,
        CanClaim = res.canClaim,
    };

    private void Apply(PassInfoDto info) {
        _info.Value = info;
        _hasClaimable.Value = info.CanClaim;
    }

    public async UniTask<PassInfoDto> RequestInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_PassInfo>(cts.Token);
        _net.Send(new C_PassInfo().Write());
        // 푸시 처리(OnPacketReceived)도 같은 패킷을 반영하지만, 대기 해제가 그보다 먼저 일어날 수 있어 여기서도 맞춘다
        // (같은 값이라 두 번 반영돼도 결과가 같다).
        var res = await wait;
        Apply(ToDto(res));
        return _info.Value;
    }

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            await RequestInfoAsync(ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            // 배지 갱신 실패로 화면 전체가 멈추면 안 된다. 다음 갱신이나 푸시에서 다시 맞춰진다.
            UnityEngine.Debug.LogException(e);
        }
    }

    public async UniTask<PassClaimResultDto> ClaimAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_PassClaimResult>(cts.Token);
        _net.Send(new C_PassClaim().Write());
        var res = await wait;

        var dto = new PassClaimResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Info = new PassInfoDto {
                StaminaSpent = res.staminaSpent,
                Level = res.level,
                FreeClaimedCount = res.freeClaimedCount,
                PassClaimedCount = res.passClaimedCount,
                HasPass = res.hasPass,
                CanClaim = res.canClaim,
            },
            NewGold = res.newGold,
            NewDiamond = res.newDiamond,
            NewStamina = res.newStamina,
        };
        foreach (var g in res.granteds)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        foreach (var er in res.equipRolls) {
            var roll = new EquipRollDto {
                InstanceId = er.instanceId,
                EquipDataId = er.equipDataId,
                StarLevel = er.starLevel,
                Level = er.level,
                MainPoints = er.mainPoints
            };
            roll.OptionPoints[0] = er.opt1Points;
            roll.OptionPoints[1] = er.opt2Points;
            roll.OptionPoints[2] = er.opt3Points;
            roll.OptionPoints[3] = er.opt4Points;
            dto.EquipRolls.Add(roll);
        }

        // 실패해도 서버가 현재 현황을 함께 보내므로 그대로 맞춘다(화면과 서버가 어긋난 채 남지 않게).
        Apply(dto.Info);
        return dto;
    }

    public void Dispose() {
        if (_net != null) _net.PacketReceived -= OnPacketReceived;
        _info.Dispose();
        _hasClaimable.Dispose();
    }
}
