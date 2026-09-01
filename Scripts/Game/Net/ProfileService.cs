using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>프로필/프로필 테두리/닉네임 통신 구현. 패킷을 주고받아 DTO 로 변환한다.</summary>
public sealed class ProfileService : IProfileService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public ProfileService(INetworkService net) {
        _net = net;
    }

    public async UniTask<ProfileDto> RequestProfileAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_Profile>(cts.Token);
        _net.Send(new C_Profile().Write());
        var res = await wait;

        var dto = new ProfileDto {
            Nickname = res.nickname,
            EquippedProfileFrameId = res.equippedProfileFrameId,
            NicknameTicketQty = res.nicknameTicketQty,
            CreatedAt = res.createdUnix > 0 ? FromUnix(res.createdUnix) : default,
            AchievementScore = res.achievementScore,
            CombatPowerRank = res.combatPowerRank,
            AchievementRank = res.achievementRank,
        };
        foreach (var f in res.profileFrames) dto.ProfileFrameIds.Add(f.profileFrameId);
        return dto;
    }

    // 서버 Unix epoch 초 -> 로컬 시간(메일 등과 동일 규약).
    private static DateTime FromUnix(long unixSeconds) =>
        DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime().DateTime;

    public async UniTask<EquipProfileFrameResult> EquipProfileFrameAsync(int profileFrameId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EquipProfileFrameResult>(cts.Token);
        _net.Send(new C_EquipProfileFrame { profileFrameId = profileFrameId }.Write());
        var res = await wait;

        return new EquipProfileFrameResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            EquippedProfileFrameId = res.equippedProfileFrameId,
        };
    }

    public async UniTask<int> CheckNicknameAsync(string nickname, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_CheckNicknameResult>(cts.Token);
        _net.Send(new C_CheckNickname { nickname = nickname }.Write());
        var res = await wait;
        return res.failReason;
    }

    public async UniTask<ChangeNicknameResult> ChangeNicknameAsync(string nickname, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ChangeNicknameResult>(cts.Token);
        _net.Send(new C_ChangeNickname { nickname = nickname }.Write());
        var res = await wait;

        return new ChangeNicknameResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Nickname = res.nickname,
            NewDiamond = res.newDiamond,
            NewTicketQty = res.newTicketQty,
        };
    }
}
