using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

// 업적 통신 계약과 순수 DTO(생성 패킷 비의존). 업적 UI 는 이 타입들만 참조한다.

/// <summary>
/// 업적 지표. 값은 서버 AchievementMetric 과 일치해야 한다(패킷에 int 로 실려 온다).
/// 서버가 정의를 내려주므로 클라는 표시 이름을 붙이는 데만 쓴다(AchievementRules).
/// </summary>
public enum AchievementMetric {
    // 게임
    StaminaSpent = 0,
    CommissionRaidClear = 1,
    CommissionSubjugationClear = 2,
    CommissionEscortClear = 3,
    AttendanceClaim = 4,
    ArenaEntry = 8,
    InvestmentUpgrade = 9,
    DispatchComplete = 12,

    // 길드
    GuildRaidEntry = 10,
    GuildWarWin = 11,

    // 성장(5~7 은 처음에 게임으로 분류했다가 옮긴 값이라 번호가 20번대와 떨어져 있다)
    HqNodeUpgrade = 5,
    EngravingUpgrade = 6,
    TrainingComplete = 7,
    CharacterStarUp = 20,
    EquipStarUp = 21,
    CharacterLevelUp = 22,
    EquipLevelUp = 23,

    // 모집
    NormalCharacterGacha = 30,
    SpecialCharacterGacha = 31,
    NormalEquipGacha = 32,
    SpecialEquipGacha = 33,

    // 게임(V1.11)
    ExpeditionEntry = 34,
    DoomEntry = 35,
    TowerClear = 36,

    // 성장(V1.11)
    CharacterLimitBreak = 37,
    EquipLimitBreak = 38,

    // 게임(V1.12)
    FrontlineClear = 39,
    HeroesClear = 40,
    FriendStaminaSend = 41
}

/// <summary>업적 분류(팝업 탭). 값은 서버 AchievementCategory 와 일치. 요약 탭은 이들의 합이라 여기 없다.</summary>
public enum AchievementCategory {
    Game = 0,
    Guild = 1,
    Growth = 2,
    Recruit = 3
}

/// <summary>업적 1건의 현황. 점수는 누적 횟수 x 배점이며 서버가 이미 곱해 보낸다.</summary>
public sealed class AchievementEntryDto {
    public AchievementMetric Metric;
    public AchievementCategory Category;
    public long Count;
    /// <summary>1회(또는 1 단위)당 점수.</summary>
    public int Points;
    public long Score;
}

/// <summary>업적 점수 보상 1티어(보상 탭).</summary>
public sealed class AchievementRewardTierDto {
    public int Tier;
    public long Threshold;
    public bool Claimed;
    public List<RewardItem> Rewards = new();

    /// <summary>지금 점수로 수령할 수 있는지.</summary>
    public bool IsClaimable(long totalScore) => !Claimed && totalScore >= Threshold;
}

/// <summary>업적 팝업 진입 응답.</summary>
public sealed class AchievementHomeDto {
    public long TotalScore;
    /// <summary>서버 카탈로그 순서 그대로. 클라는 분류로 거르기만 하고 정렬하지 않는다.</summary>
    public List<AchievementEntryDto> Entries = new();
    /// <summary>점수 보상 티어(임계치 오름차순).</summary>
    public List<AchievementRewardTierDto> RewardTiers = new();

    /// <summary>분류 하나의 점수 합(요약 탭). 항목을 이미 받아 왔으므로 서버에 다시 묻지 않는다.</summary>
    public long ScoreOf(AchievementCategory category) {
        long sum = 0;
        foreach (var e in Entries) if (e.Category == category) sum += e.Score;
        return sum;
    }

    /// <summary>수령할 수 있는 보상이 하나라도 있는지(배지 근거).</summary>
    public bool HasClaimableReward {
        get {
            foreach (var t in RewardTiers) if (t.IsClaimable(TotalScore)) return true;
            return false;
        }
    }
}

/// <summary>업적 보상 수령 결과.</summary>
public sealed class AchievementClaimResultDto {
    public bool IsSuccess;
    public int FailReason;
    public int Tier;
    public int NewDiamond;
    public List<RewardItem> Granted = new();
}

/// <summary>업적 통신 추상화. UI 는 이 계약에만 의존한다.</summary>
public interface IAchievementService {
    /// <summary>
    /// 수령 가능한 업적 보상이 있는지. 배지가 구독한다.
    /// 업적 조회/수령 응답이 이 값을 갱신하므로 배지가 따로 서버를 두드리지 않는다.
    /// </summary>
    IReadOnlyReactiveProperty<bool> HasClaimableReward { get; }

    UniTask<AchievementHomeDto> RequestAchievementsAsync(CancellationToken ct = default);
    UniTask<AchievementClaimResultDto> ClaimRewardAsync(int tier, CancellationToken ct = default);

    /// <summary>
    /// 남은 수령 가능 여부를 갱신한다. 보상을 받은 직후처럼 팝업이 이미 아는 값을 그대로 넘겨,
    /// 배지를 맞추려고 서버를 다시 두드리지 않게 한다.
    /// </summary>
    void SetClaimable(bool claimable);

    /// <summary>보상 수령 가능 여부만 최신화한다(배지 초기 로드용). 실패해도 조용히 넘어간다.</summary>
    UniTask RefreshAsync(CancellationToken ct = default);
}
