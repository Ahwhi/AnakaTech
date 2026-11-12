/// <summary>
/// 한계돌파 규칙의 클라 미러. 판정은 서버 권위(GameDataRepository.LimitBreak)이며, 여기 값은 버튼/배지/조건 표시용이다.
/// 서버 GameConstants.LimitBreak* 와 값이 일치해야 한다(어긋나면 버튼은 켜지는데 서버가 거부한다).
///
/// 한계돌파는 캐릭터마다 한 번이며 되돌리지 않는다. 돌파한 캐릭터는 CharacterData 의 한계돌파 스킬을 쓴다
/// (한계돌파 스킬을 할당하지 않은 캐릭터는 기존 스킬을 그대로 쓴다).
/// </summary>
public static class LimitBreakRules {
    public const int RequiredLevel = 10000;
    public const int RequiredStar = GrowthRules.MaxStarLevel;
    public const int TicketCost = 1;

    public static bool IsLevelMet(CharacterProgress p) => p != null && p.Level >= RequiredLevel;
    public static bool IsStarMet(CharacterProgress p) => p != null && p.StarLevel >= RequiredStar;
    public static bool IsTicketMet(int ownedTickets) => ownedTickets >= TicketCost;

    /// <summary>지금 한계돌파할 수 있는지(미돌파 + 레벨 + 별레벨 + 한계돌파권). 버튼 활성과 배지가 같은 판정을 쓴다.</summary>
    public static bool CanLimitBreak(CharacterProgress p, int ownedTickets) =>
        p != null && !p.LimitBroken && IsLevelMet(p) && IsStarMet(p) && IsTicketMet(ownedTickets);

    // 한계돌파 후 조각 보너스. 한계돌파한 캐릭터는 보유 조각 1개당 아래 스탯이 이만큼(% 포인트) 오른다.
    // 별레벨이 최대라 더 쓸 곳이 없는 조각을 성장으로 이어 주기 위한 규칙이며, 조각이 늘면 즉시 따라 오른다.
    public const float ShardBonusPerShard = 0.01f;
    private static readonly StatType[] ShardBonusStats = { StatType.FinalDamage, StatType.DamageTakenReduction };

    /// <summary>
    /// 조각 보너스 스탯. 한계돌파 전이면 0 이다. 스탯 조립(CharacterRuntime)이 이 값을 더하므로
    /// 필드/의뢰/PvP 상대/전투력이 모두 같은 값을 쓴다.
    /// </summary>
    public static CombatStats ShardBonus(CharacterProgress p) {
        var bonus = new CombatStats();
        if (p == null || !p.LimitBroken || p.Shards <= 0) return bonus;

        float value = p.Shards * ShardBonusPerShard;
        foreach (var stat in ShardBonusStats) bonus = bonus.WithAdded(stat, value);
        return bonus;
    }

    /// <summary>조각 보너스 표시 문구("최종데미지 +1.97%\n피격데미지감소 +1.97%"). 조각이 1개당 0.01% 라 소수 둘째 자리까지 적는다.</summary>
    public static string ShardBonusSummary(CharacterProgress p) {
        int shards = p != null ? System.Math.Max(0, p.Shards) : 0;
        float value = shards * ShardBonusPerShard;
        var sb = new System.Text.StringBuilder();
        foreach (var stat in ShardBonusStats) {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(GrowthRules.StatName(stat)).Append(" +").Append(value.ToString("0.##")).Append('%');
        }
        return sb.ToString();
    }

    /// <summary>서버 GameDataRepository.LimitBreakFail 값 -> 안내 문구.</summary>
    public static string FailMessage(int failReason) => failReason switch {
        2 => "이미 한계돌파한 캐릭터입니다.",
        3 => "레벨이 부족합니다.",
        4 => "별레벨이 부족합니다.",
        5 => "한계돌파권이 부족합니다.",
        _ => "한계돌파할 수 없습니다."
    };
}
