using UnityEngine;

/// <summary>
/// 장비 부위별 스탯 구성표(주스탯 1개 + 옵션 4개). 어떤 부위가 어떤 스탯을 갖는지는 이 표가 단일 진실 원천이며,
/// 서버는 "포인트"만 굴려 저장하고 실제 스탯 종류/스케일 해석은 클라(이 표)가 담당한다.
///
/// 포인트 규약: 서버가 저장/전달하는 값은 전부 정수 "포인트"다. 실제 스탯 값 = 포인트 * PerPoint.
/// 스탯마다 사는 스케일이 달라(체력은 크게, 치명타확률은 작게) PerPoint 로 흡수한다.
///   - 체력          : 1포인트 = 10   (다른 옵션이 1 오를 때 체력은 10씩 오른다)
///   - 치명타확률    : 1포인트 = 0.1  (%)
///   - 그 외         : 1포인트 = 1
///
/// 부위 정체성(옵션이 겹치지 않게 배치해 어느 부위도 버릴 수 없게 한다):
///   무기     = 딜 총량   / 방어구 = 생존
///   신발     = 템포      / 악세서리 = 폭발력
///
/// 확장: 새 부위를 추가하면 EquipmentSlot 에 값을 넣고 Table 에 같은 순서로 행을 추가하면 된다.
/// </summary>
public static class EquipmentStatLayout {
    /// <summary>부위당 옵션 슬롯 수(최대). 등급이 낮으면 이보다 적게 개방된다(OptionCount).</summary>
    public const int MaxOptionSlots = 4;

    /// <summary>스탯 슬롯 1칸(스탯 종류 + 포인트 1당 실제 증가량).</summary>
    public readonly struct StatSlot {
        public readonly StatType Stat;
        public readonly float PerPoint;

        public StatSlot(StatType stat, float perPoint) {
            Stat = stat;
            PerPoint = perPoint;
        }
    }

    // 체력/치명타확률의 포인트 스케일. 표에서 반복되므로 상수로 둔다.
    private const float HealthPerPoint = 10f;
    private const float CritRatePerPoint = 0.1f;

    // [부위][0]=주스탯, [1..4]=1~4옵션. EquipmentSlot 순서(Weapon/Armor/Shoes/Accessory)와 반드시 일치.
    private static readonly StatSlot[][] Table = {
        // 무기: 공격력 / 방어구관통력, 최소데미지비율, 최대데미지비율, 최종데미지
        new[] {
            new StatSlot(StatType.Attack,         1f),
            new StatSlot(StatType.ArmorPen,       1f),
            new StatSlot(StatType.MinDamageRatio, 1f),
            new StatSlot(StatType.MaxDamageRatio, 1f),
            new StatSlot(StatType.FinalDamage,    1f),
        },
        // 방어구: 체력 / 방어력, 상태이상저항, 치명타저항, 피격데미지감소
        new[] {
            new StatSlot(StatType.Health,               HealthPerPoint),
            new StatSlot(StatType.Defense,              1f),
            new StatSlot(StatType.StatusResist,         1f),
            new StatSlot(StatType.CritResist,           1f),
            new StatSlot(StatType.DamageTakenReduction, 1f),
        },
        // 신발: 이동속도 / 체력, 방어력, 스킬가속, 공격속도
        new[] {
            new StatSlot(StatType.MoveSpeed,   1f),
            new StatSlot(StatType.Health,      HealthPerPoint),
            new StatSlot(StatType.Defense,     1f),
            new StatSlot(StatType.SkillAccel,  1f),
            new StatSlot(StatType.AttackSpeed, 1f),
        },
        // 악세서리: 치명타확률 / 치명타데미지, 스킬가속, 피격데미지감소, 최종데미지
        new[] {
            new StatSlot(StatType.CritRate,             CritRatePerPoint),
            new StatSlot(StatType.CritDamage,           1f),
            new StatSlot(StatType.SkillAccel,           1f),
            new StatSlot(StatType.DamageTakenReduction, 1f),
            new StatSlot(StatType.FinalDamage,          1f),
        },
    };

    /// <summary>부위의 주스탯 슬롯.</summary>
    public static StatSlot Main(EquipmentSlot slot) => Row(slot)[0];

    /// <summary>부위의 옵션 슬롯(optionIndex 0-based = 1옵션). 범위를 벗어나면 주스탯을 돌려주지 않고 안전한 기본값을 준다.</summary>
    public static StatSlot Option(EquipmentSlot slot, int optionIndex) {
        if (optionIndex < 0 || optionIndex >= MaxOptionSlots) return new StatSlot(StatType.Attack, 0f);
        return Row(slot)[optionIndex + 1];
    }

    /// <summary>
    /// 등급별 보유 옵션 개수. N=0, R=1, SR=2, SSR=3, XR=4 로 Grade 열거형 순서와 그대로 일치한다.
    /// 등급이 곧 옵션 개수이므로 같은 등급끼리는 옵션 개수 격차가 없다(뽑기 좌절 방지).
    /// </summary>
    public static int OptionCount(Grade grade) => Mathf.Clamp((int)grade, 0, MaxOptionSlots);

    /// <summary>부위 표시명.</summary>
    public static string SlotName(EquipmentSlot slot) => slot switch {
        EquipmentSlot.Weapon => "무기",
        EquipmentSlot.Armor => "방어구",
        EquipmentSlot.Shoes => "신발",
        EquipmentSlot.Accessory => "악세서리",
        _ => "장비"
    };

    /// <summary>스탯 표시명(주스탯). StatFormat/GrowthRules 와 같은 라벨을 쓴다.</summary>
    public static string MainLabel(EquipmentSlot slot) => GrowthRules.StatName(Main(slot).Stat);

    /// <summary>스탯 표시명(옵션).</summary>
    public static string OptionLabel(EquipmentSlot slot, int optionIndex) =>
        GrowthRules.StatName(Option(slot, optionIndex).Stat);

    /// <summary>주스탯 포인트를 실제 스탯 값으로 환산.</summary>
    public static float MainValue(EquipmentSlot slot, int points) => points * Main(slot).PerPoint;

    /// <summary>옵션 포인트를 실제 스탯 값으로 환산.</summary>
    public static float OptionValue(EquipmentSlot slot, int optionIndex, int points) =>
        points * Option(slot, optionIndex).PerPoint;

    // 부위 행. 정의되지 않은 부위(열거형 확장 후 표 미갱신)는 무기 행으로 폴백해 널 참조를 막는다.
    private static StatSlot[] Row(EquipmentSlot slot) {
        int i = (int)slot;
        return i >= 0 && i < Table.Length ? Table[i] : Table[0];
    }
}
