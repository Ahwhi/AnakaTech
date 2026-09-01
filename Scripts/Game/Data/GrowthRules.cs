/// <summary>
/// 캐릭터 육성 규칙(별레벨 필요 조각/최대치). 서버 GameConstants 와 값이 일치해야 한다(치트 판정은 서버가 하며,
/// 여기는 UI 표시용 미러). 값이 어긋나면 표시만 다를 뿐 실제 승급 성패는 서버가 결정한다.
/// </summary>
public static class GrowthRules {
    public const int MaxStarLevel = 15;   // PLANNING 5-9 (별 5칸 x 3티어: 노랑->분홍->파랑)

    /// <summary>캐릭터 최대 레벨. 서버 GameConstants.MaxCharacterLevel 과 일치해야 한다(만렙 표시 판정).</summary>
    public const int MaxCharacterLevel = 9999;

    /// <summary>이 캐릭터가 만렙인지. 경험치 바를 꽉 찬 상태로 두고 더 이상 오르지 않음을 알리는 데 쓴다.</summary>
    public static bool IsMaxLevel(int level) => level >= MaxCharacterLevel;
    public const int StarShardStep = 10;  // PLANNING 5-11
    public const int StarDiaPerLevel = 100;   // 별레벨 승급당 다이아 = 현재 별레벨 * 100 (서버 GameDataRepository 와 일치)

    /// <summary>스탯 초기화 비용(다이아). 서버 GameDataRepository.StatResetDia 와 값이 일치해야 한다.</summary>
    public const int StatResetDia = 500;

    /// <summary>자동 투자 미설정 값. StatType 에 없는 값이라 실제 스탯과 섞이지 않는다(서버 AutoStatNone 과 동일).</summary>
    public const int AutoStatNone = -1;

    // 레벨업 필요 경험치(MaxExp)는 여기서 계산하지 않는다. 서버가 모든 경로(로그인 스냅샷/던전 보상/가챠)에서
    // 확정값을 내려주므로 클라에 공식 미러를 두면 값이 어긋날 뿐이다(실제로 어긋난 이력이 있어 제거).

    public static int RequiredShardsForStar(int currentStar) =>
        StarShardStep * (currentStar < 1 ? 1 : currentStar);

    /// <summary>현재 별레벨에서 다음 레벨로 승급 시 필요한 다이아(1->2=100, 2->3=200 ...).</summary>
    public static int RequiredDiaForStar(int currentStar) =>
        StarDiaPerLevel * (currentStar < 1 ? 1 : currentStar);

    /// <summary>
    /// 스탯 1종의 툴팁 설명(StatName 과 짝). 실제 전투 수식(CombatMath/CombatStats)이 하는 일을 그대로 풀어 쓴다.
    /// 수식이 바뀌면 이 문구도 함께 고칠 것. 표시 단위는 StatFormat.FormatValue 와 일치한다.
    /// </summary>
    public static string StatDescription(StatType stat) => stat switch {
        StatType.Attack =>
            "모든 피해의 기준값입니다.\n기본 피해 = 공격력 x 스킬 배수로 계산되며, 여기서 상대의 방어 감산이 빠집니다.",
        StatType.Defense =>
            "받는 피해를 깎습니다.\n상대의 방어구관통력이 적용된 뒤 남은 방어력만큼 피해가 줄어듭니다.",
        StatType.Health =>
            "최대 체력입니다.\n0이 되면 쓰러지며, 부활이 없어 그 전투가 끝날 때까지 다시 일어나지 않습니다.",
        StatType.Range =>
            "적을 공격할 수 있는 거리입니다.\n수련의 사거리 효과로 증가시킬 수 있습니다.",
        StatType.AttackSpeed =>
            "공격 간격을 줄입니다.\n100%면 기준 속도의 2배로 공격합니다. 아주 높게 쌓으면 상승폭이 완만해집니다.",
        StatType.MoveSpeed =>
            "이동 속도를 올립니다.\n100%면 기준 속도의 2배로 움직여 적에게 더 빨리 붙습니다.",
        StatType.CritRate =>
            "치명타가 터질 확률입니다.\n100%면 모든 공격이 치명타가 됩니다.",
        StatType.CritDamage =>
            "치명타로 터졌을 때 늘어나는 피해량입니다.\n50%면 치명타 피해가 1.5배가 됩니다.",
        StatType.MinDamageRatio =>
            "피해량 흔들림의 아래쪽을 끌어올립니다.\n낮게 뜨는 피해가 줄어들어 딜이 안정적으로 들어갑니다.",
        StatType.MaxDamageRatio =>
            "피해량 흔들림의 위쪽을 넓힙니다.\n높게 뜨는 피해의 상한이 올라가 한 방이 크게 터질 수 있습니다.",
        StatType.ArmorPen =>
            "상대의 방어력을 무시합니다.\n50%면 상대 방어력의 절반만 적용되어 방어가 두꺼운 적에게 특히 강합니다.",
        StatType.FinalDamage =>
            "모든 계산이 끝난 피해에 마지막으로 곱해집니다.\n10%면 최종 피해가 1.1배가 되며, 다른 공격 스탯과 곱해져 함께 커집니다.",
        StatType.SkillAccel =>
            "스킬 재사용 대기시간을 줄입니다.\n쌓을수록 효율이 완만해지며 최소 대기시간 아래로는 내려가지 않습니다.",
        StatType.SkillCooldownReduction =>
            "스킬 대기시간을 초 단위로 직접 줄입니다.\n스킬가속과 달리 고정 수치로 빠지며, 최소 대기시간까지만 줄어듭니다.",
        StatType.CritResist =>
            "상대 치명타로 늘어나는 추가 피해를 깎습니다.\n쌓을수록 효율이 완만해지고, 치명타가 일반 공격보다 약해지지는 않습니다.",
        StatType.StatusResist =>
            "받는 상태이상의 지속시간을 줄입니다.\n쌓을수록 효율이 완만해지며 완전 면역은 되지 않습니다.",
        StatType.DamageTakenReduction =>
            "받는 모든 피해를 줄입니다.\n방어력과 별개로 최종 피해에 곱해지며, 쌓을수록 효율이 완만해집니다.",
        StatType.DoubleAttackChance =>
            "기본 공격이 한 번 더 나갈 확률입니다.\n두 번째 타격은 잠시 뒤 같은 위력으로 들어갑니다(스킬에는 적용되지 않습니다).",
        StatType.EvasionChance =>
            "공격을 흘려보낼 확률입니다.\n회피하면 피해를 전혀 받지 않고 MISS 가 표시됩니다.",
        _ => ""
    };

    public static string StatName(StatType stat) => stat switch {
        StatType.Attack => "공격력",
        StatType.Defense => "방어력",
        StatType.Health => "체력",
        StatType.Range => "사거리",
        StatType.AttackSpeed => "공격속도",
        StatType.MoveSpeed => "이동속도",
        StatType.CritRate => "치명타확률",
        StatType.CritDamage => "치명타데미지",
        StatType.MinDamageRatio => "최소데미지비율",
        StatType.MaxDamageRatio => "최대데미지비율",
        StatType.ArmorPen => "방어구관통력",
        StatType.FinalDamage => "최종데미지",
        StatType.SkillAccel => "스킬가속",
        StatType.SkillCooldownReduction => "스킬쿨타임감소",
        StatType.CritResist => "치명타저항",
        StatType.StatusResist => "상태이상저항",
        StatType.DamageTakenReduction => "피격데미지감소",
        StatType.DoubleAttackChance => "2회공격확률",
        StatType.EvasionChance => "회피확률",
        _ => stat.ToString()
    };
}
