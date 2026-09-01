/// <summary>
/// 업적 표시 규칙(클라 전용). 어떤 업적이 있고 몇 점인지는 서버가 정하고 응답으로 내려주므로,
/// 여기에는 화면에 쓰는 문구만 둔다(수치를 미러링하면 서버와 어긋날 여지만 생긴다).
///
/// 서버가 새 지표를 추가했는데 여기 문구가 없으면 지표 이름 대신 안내 문구가 나온다.
/// 목록 자체는 서버가 채우므로 항목이 사라지지는 않는다.
/// </summary>
public static class AchievementRules {
    public static string CategoryName(AchievementCategory category) => category switch {
        AchievementCategory.Game => "게임",
        AchievementCategory.Guild => "길드",
        AchievementCategory.Growth => "성장",
        AchievementCategory.Recruit => "모집",
        _ => "기타"
    };

    public static string Name(AchievementMetric metric) => metric switch {
        AchievementMetric.StaminaSpent => "기력 소모",
        AchievementMetric.CommissionRaidClear => "의뢰: 레이드 해결",
        AchievementMetric.CommissionSubjugationClear => "의뢰: 토벌 해결",
        AchievementMetric.CommissionEscortClear => "의뢰: 호위 해결",
        AchievementMetric.AttendanceClaim => "출석",
        AchievementMetric.ArenaEntry => "쟁탈전 참여",
        AchievementMetric.InvestmentUpgrade => "투자 강화",
        AchievementMetric.DispatchComplete => "파견 완료",

        AchievementMetric.GuildRaidEntry => "길드 레이드 참여",
        AchievementMetric.GuildWarWin => "길드 대항전 승리",

        AchievementMetric.HqNodeUpgrade => "본부 훈련 강화",
        AchievementMetric.EngravingUpgrade => "각인 강화",
        AchievementMetric.TrainingComplete => "수련 완료",
        AchievementMetric.CharacterStarUp => "캐릭터 별 승급",
        AchievementMetric.EquipStarUp => "장비 별 승급",
        AchievementMetric.CharacterLevelUp => "캐릭터 레벨업",
        AchievementMetric.EquipLevelUp => "장비 레벨업",

        AchievementMetric.NormalCharacterGacha => "일반 소녀 모집",
        AchievementMetric.SpecialCharacterGacha => "특수 소녀 모집",
        AchievementMetric.NormalEquipGacha => "일반 장비 모집",
        AchievementMetric.SpecialEquipGacha => "특수 장비 모집",

        _ => "새로운 업적"
    };

    /// <summary>
    /// 누적값의 단위. 기력만 소모"량"이고 나머지는 횟수라, 숫자 옆에 붙는 말이 다르다.
    /// </summary>
    public static string Unit(AchievementMetric metric) =>
        metric == AchievementMetric.StaminaSpent ? "" : "회";

    /// <summary>"1,234 회" 형태의 누적 표시.</summary>
    public static string CountText(AchievementMetric metric, long count) {
        string unit = Unit(metric);
        return string.IsNullOrEmpty(unit) ? count.ToString("N0") : $"{count:N0} {unit}";
    }

    /// <summary>"1회당 50점" 형태의 배점 안내.</summary>
    public static string PointsText(AchievementMetric metric, int points) =>
        metric == AchievementMetric.StaminaSpent ? $"1당 {points}점" : $"1회당 {points}점";

    public static string ScoreText(long score) => $"{score:N0}점";

    // ── 점수 보상(보상 탭) ───────────────────────────
    /// <summary>보상 칸의 달성 조건 문구(예: "10,000점 달성").</summary>
    public static string RewardRequirementText(long threshold) => $"{threshold:N0}점 달성";

    /// <summary>서버 AchievementRepository.ClaimFail 값 -> 안내 문구. 값이 서버와 일치해야 한다.</summary>
    public static string ClaimFailMessage(int failReason) => failReason switch {
        1 => "잘못된 보상 단계입니다.",
        2 => "아직 점수가 부족합니다.",
        3 => "이미 수령한 보상입니다.",
        5 => "로그인이 필요합니다.",
        _ => "보상을 수령하지 못했습니다."
    };
}
