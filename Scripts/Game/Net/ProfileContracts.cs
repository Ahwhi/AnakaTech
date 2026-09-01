using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 프로필/프로필 테두리/닉네임 통신 계약과 순수 DTO(생성 패킷 비의존). 프로필 UI 는 이 타입들만 참조한다.

public sealed class ProfileDto {
    public string Nickname = "";
    public int EquippedProfileFrameId;
    public int NicknameTicketQty;
    public System.DateTime CreatedAt;   // 계정 생성일(로컬 시간). 미상이면 default(DateTime).
    /// <summary>업적 총점. 업적 팝업의 총합과 같은 값이며 서버가 같은 계산식으로 만든다.</summary>
    public long AchievementScore;
    /// <summary>유저 랭킹(전투력) 순위. 0 이면 순위권 밖이거나 아직 집계 전이다.</summary>
    public int CombatPowerRank;
    /// <summary>업적 랭킹 순위. 0 이면 순위권 밖이거나 아직 집계 전이다.</summary>
    public int AchievementRank;
    public List<int> ProfileFrameIds = new();
}

public sealed class EquipProfileFrameResult {
    public bool IsSuccess;
    public int FailReason;          // 0성공,1미보유,5지갑없음
    public int EquippedProfileFrameId;
}

public sealed class ChangeNicknameResult {
    public bool IsSuccess;
    public int FailReason;          // 0성공,1형식오류,2중복,3재화부족,5지갑없음
    public string Nickname = "";
    public int NewDiamond;
    public int NewTicketQty;
}

/// <summary>프로필/프로필 테두리/닉네임 통신 추상화(WaitPacketAsync + Send 패턴).</summary>
public interface IProfileService {
    UniTask<ProfileDto> RequestProfileAsync(CancellationToken ct = default);
    UniTask<EquipProfileFrameResult> EquipProfileFrameAsync(int profileFrameId, CancellationToken ct = default);
    /// <summary>닉네임 중복확인. 0 이면 사용 가능, 그 외는 NicknameRules.FailMessage 로 옮길 실패 사유(중복/형식/금지어).</summary>
    UniTask<int> CheckNicknameAsync(string nickname, CancellationToken ct = default);
    UniTask<ChangeNicknameResult> ChangeNicknameAsync(string nickname, CancellationToken ct = default);
}
