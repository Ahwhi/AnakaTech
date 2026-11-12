/// <summary>층 보너스의 종류. 값은 서버 Server.Data.TowerBonusKind 와 패킷(bonusKind)에 그대로 쓰이므로 바꾸지 않는다.</summary>
public enum TowerBonusKind {
    None = 0,
    /// <summary>그 무기 타입 소녀의 공격력 N배.</summary>
    WeaponAttack = 1,
    /// <summary>그 무기 타입 소녀의 공격속도 N배.</summary>
    WeaponAttackSpeed = 2,
    /// <summary>그 상태이상을 거는 소녀가 거는 지속시간 N배.</summary>
    StatusDuration = 3,
}

/// <summary>
/// 무한 타워 층 보너스 1건. 서버가 층에 도달할 때 계정마다 굴려 주며, 클라는 받은 값으로 대상 판정/전투 적용/표시만 한다.
/// Target 의 뜻은 종류마다 다르다(무기 보너스 = WeaponType, 상태이상 보너스 = StatusEffectType).
///
/// 대상 판정(AppliesTo)이 한 곳에 있으므로 편성 화면의 보너스 표식과 전투의 실제 적용이 어긋나지 않는다.
/// </summary>
public readonly struct TowerBonus {
    public readonly TowerBonusKind Kind;
    public readonly int Target;
    public readonly int Value;

    public TowerBonus(TowerBonusKind kind, int target, int value) {
        Kind = kind; Target = target; Value = value;
    }

    public bool IsValid => Kind != TowerBonusKind.None && Value > 0;

    public WeaponType Weapon => (WeaponType)Target;
    public StatusEffectType Status => (StatusEffectType)Target;

    private bool IsWeaponBonus => Kind == TowerBonusKind.WeaponAttack || Kind == TowerBonusKind.WeaponAttackSpeed;

    /// <summary>이 보너스를 받는 소녀인지. 상태이상 보너스는 지금 쓰는 스킬 기준이다(한계돌파 여부에 따라 달라진다).</summary>
    public bool AppliesTo(CharacterData data, bool limitBroken) {
        if (!IsValid || data == null) return false;
        if (IsWeaponBonus) return data.WeaponType == Weapon;
        return Kind == TowerBonusKind.StatusDuration && data.Inflicts(Status, limitBroken);
    }

    /// <summary>
    /// 기준 스탯에 보너스를 얹은 값. 대상이 아니거나 스탯 보너스가 아니면 그대로 돌려준다.
    /// 항상 기준 스탯 위에서 다시 계산하므로 여러 번 불러도 중첩되지 않는다.
    /// </summary>
    public CombatStats Apply(CombatStats stats, CharacterData data) {
        if (!IsValid || !IsWeaponBonus || data == null || data.WeaponType != Weapon) return stats;
        var stat = Kind == TowerBonusKind.WeaponAttack ? StatType.Attack : StatType.AttackSpeed;
        return stats.WithEffectiveScale(stat, Value);
    }

    /// <summary>상태이상 지속시간 보너스면 그 대상 종류, 아니면 null.</summary>
    public StatusEffectType? DurationTarget =>
        IsValid && Kind == TowerBonusKind.StatusDuration ? Status : (StatusEffectType?)null;

    // 표시 색. 무기/스탯/배수를 서로 다른 색으로 칠해 한 줄에서도 무엇이 무엇인지 바로 읽히게 한다.
    private const string ValueColor = "#FFE066";
    private const string AttackColor = "#FF8A65";
    private const string AttackSpeedColor = "#4DD0E1";
    private const string DurationColor = "#B39DDB";

    private static string WeaponColor(WeaponType type) => type switch {
        WeaponType.Sword => "#FF6E6E",
        WeaponType.Bow => "#8BD46E",
        WeaponType.Gun => "#F0B35A",
        WeaponType.Knuckle => "#E58AD6",
        WeaponType.Staff => "#7EB8FF",
        _ => "#FFFFFF"
    };

    /// <summary>
    /// 보너스 문구(TMP 리치 텍스트). 라벨 없이 내용만 쓴다.
    /// 예: "총 타입 공격력 5배", "출혈 지속시간 2배". 보너스가 없으면 빈 문자열.
    /// </summary>
    public string Describe() {
        if (!IsValid) return "";

        string value = Colored(ValueColor, $"{Value}배");
        if (Kind == TowerBonusKind.StatusDuration) {
            string status = Colored(StatusEffectRules.ColorHex(Status), StatusEffectRules.DisplayName(Status));
            return $"{status} {Colored(DurationColor, "지속시간")} {value}";
        }

        string weapon = Colored(WeaponColor(Weapon), GameEnumRules.WeaponDisplayName(Weapon));
        string stat = Kind == TowerBonusKind.WeaponAttack
            ? Colored(AttackColor, "공격력")
            : Colored(AttackSpeedColor, "공격속도");
        return $"{weapon} 타입 {stat} {value}";
    }

    private static string Colored(string hex, string text) =>
        string.IsNullOrEmpty(hex) ? text : $"<color={hex}>{text}</color>";
}
