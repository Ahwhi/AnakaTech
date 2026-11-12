using System.Text;
using UnityEngine;

/// <summary>능력치 표시 문자열 공용 포맷(덱/도감/상세 등에서 재사용). 6대 스탯 + 치명타 2종을 한 블록으로 만든다.</summary>
public static class StatFormat {
    // 표시 순서/라벨 고정. 한 곳에서 관리해 FullStats 와 개별 Value 가 동일 순서를 공유한다.
    private static readonly (StatType stat, string label)[] Order = {
        (StatType.Attack,      "공격력"),
        (StatType.Defense,     "방어력"),
        (StatType.Health,      "체력"),
        (StatType.Range,       "사거리"),
        (StatType.AttackSpeed, "공격속도"),
        (StatType.MoveSpeed,   "이동속도"),
        (StatType.CritRate,    "치명타확률"),
        (StatType.CritDamage,  "치명타데미지"),
        (StatType.MinDamageRatio,         "최소데미지비율"),
        (StatType.MaxDamageRatio,         "최대데미지비율"),
        (StatType.ArmorPen,               "방어구관통력"),
        (StatType.FinalDamage,            "최종데미지"),
        (StatType.SkillAccel,             "스킬가속"),
        (StatType.SkillCooldownReduction, "스킬쿨타임감소"),
        (StatType.CritResist,             "치명타저항"),
        (StatType.StatusResist,           "상태이상저항"),
        (StatType.DamageTakenReduction,   "피격데미지감소"),
        (StatType.DoubleAttackChance,     "2회공격확률"),
        (StatType.EvasionChance,          "회피확률"),
    };

    /// <summary>
    /// 능력치 블록 문자열. skipZero 가 true 면 값이 0인 스탯 줄은 생략한다(장비 옵션처럼 일부 스탯만 있는 경우 표시용).
    /// </summary>
    public static string FullStats(CombatStats s, bool skipZero = false) {
        var sb = new StringBuilder();
        foreach (var (stat, label) in Order) {
            if (skipZero && Raw(s, stat) == 0f) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(label).Append(' ').Append(Value(s, stat));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 강화 후 예상 능력치 범위 블록. 스탯별로 low~high 를 표시하되, low==high(변동 없음, perLevel 0)면 단일 값으로 표시.
    /// skipZero 면 low/high 가 모두 0인 스탯 줄은 생략한다(장비 옵션 표시용).
    /// </summary>
    public static string RangeStats(CombatStats low, CombatStats high, bool skipZero = false) {
        var sb = new StringBuilder();
        foreach (var (stat, label) in Order) {
            float lo = Raw(low, stat);
            float hi = Raw(high, stat);
            if (skipZero && lo == 0f && hi == 0f) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(label).Append(' ');
            if (Mathf.Approximately(lo, hi)) sb.Append(FormatValue(stat, lo));
            else sb.Append(FormatValue(stat, lo)).Append('~').Append(FormatValue(stat, hi));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 스탯 델타(증가분) 요약 블록. 0이 아닌 스탯만 "이름 +값"으로 줄바꿈 나열한다(별 승급 미리보기 등).
    /// SkillCooldownReduction 은 "스킬쿨타임 -X초"로 표시한다(감소값이라 부호를 뒤집어 직관적으로).
    /// </summary>
    public static string DeltaSummary(CombatStats d) {
        var sb = new StringBuilder();
        foreach (var (stat, label) in Order) {
            float v = Raw(d, stat);
            if (Mathf.Approximately(v, 0f)) continue;
            if (sb.Length > 0) sb.Append('\n');
            if (stat == StatType.SkillCooldownReduction) {
                sb.Append("스킬쿨타임 -").Append(v.ToString("0.##")).Append('초');
            } else {
                if (v > 0f) sb.Append(label).Append(" +").Append(FormatValue(stat, v));
                else sb.Append(label).Append(' ').Append(FormatValue(stat, v));
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// 스탯 1종의 값 문자열(라벨 없이 값만). 능력치를 개별 텍스트 UI로 나눠 표시할 때 사용한다.
    /// 정수 스탯은 반올림, 속도/사거리는 소수 2자리, 치명타는 소수 1자리 + % 를 붙인다.
    /// </summary>
    public static string Value(CombatStats s, StatType stat) => FormatValue(stat, Raw(s, stat));

    // 스탯 종류별 포맷 규칙(단일 float 값 -> 표시 문자열).
    public static string FormatValue(StatType stat, float v) => stat switch {
        StatType.Attack      => Mathf.RoundToInt(v).ToString(),
        StatType.Defense     => Mathf.RoundToInt(v).ToString(),
        StatType.Health      => Mathf.RoundToInt(v).ToString(),
        StatType.Range       => v.ToString("0.##"),
        // 공속/이속은 기준 대비 %가속이다(실효 배수 = 1 + 값/100). 거리 단위인 사거리와 달리 % 를 붙인다.
        StatType.AttackSpeed => v.ToString("0.##") + "%",
        StatType.MoveSpeed   => v.ToString("0.##") + "%",
        StatType.CritRate    => v.ToString("0.#") + "%",
        StatType.CritDamage  => v.ToString("0.#") + "%",
        StatType.MinDamageRatio         => v.ToString("0.#") + "%",
        StatType.MaxDamageRatio         => v.ToString("0.#") + "%",
        // 관통은 V1.12 부터 방어력을 직접 깎는 절대 수치라 % 를 붙이지 않는다.
        StatType.ArmorPen               => v.ToString("0.#"),
        StatType.FinalDamage            => v.ToString("0.#") + "%",
        StatType.SkillAccel             => v.ToString("0.#") + "%",
        StatType.SkillCooldownReduction => v.ToString("0.##") + "초",
        StatType.CritResist             => v.ToString("0.#") + "%",
        StatType.StatusResist           => v.ToString("0.#") + "%",
        StatType.DamageTakenReduction   => v.ToString("0.#") + "%",
        StatType.DoubleAttackChance     => v.ToString("0.#") + "%",
        StatType.EvasionChance          => v.ToString("0.#") + "%",
        _ => ""
    };

    // 0 판정용 원시 값(포맷 전). 스탯이 없는 장비 옵션은 정확히 0f 이므로 이 값으로 생략 여부를 정한다.
    private static float Raw(CombatStats s, StatType stat) => stat switch {
        StatType.Attack      => s.Attack,
        StatType.Defense     => s.Defense,
        StatType.Health      => s.Health,
        StatType.Range       => s.Range,
        StatType.AttackSpeed => s.AttackSpeed,
        StatType.MoveSpeed   => s.MoveSpeed,
        StatType.CritRate    => s.CritRate,
        StatType.CritDamage  => s.CritDamage,
        StatType.MinDamageRatio         => s.MinDamageRatio,
        StatType.MaxDamageRatio         => s.MaxDamageRatio,
        StatType.ArmorPen               => s.ArmorPen,
        StatType.FinalDamage            => s.FinalDamage,
        StatType.SkillAccel             => s.SkillAccel,
        StatType.SkillCooldownReduction => s.SkillCooldownReduction,
        StatType.CritResist             => s.CritResist,
        StatType.StatusResist           => s.StatusResist,
        StatType.DamageTakenReduction   => s.DamageTakenReduction,
        StatType.DoubleAttackChance     => s.DoubleAttackChance,
        StatType.EvasionChance          => s.EvasionChance,
        _ => 0f
    };
}
