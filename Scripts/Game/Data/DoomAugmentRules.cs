using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 멸망전 증강의 종류. 값은 저장/표시에 쓰이므로 순서를 바꾸지 말고 새 종류는 항상 아래에 추가한다.
/// </summary>
public enum DoomAugmentKind {
    Attack = 0,
    Defense = 1,
    Health = 2,
    AttackSpeed = 3,
    MoveSpeed = 4,
    SkillAccel = 5,
    CritRate = 6,
    CritDamage = 7,
    Range = 8,
    FinalDamage = 9,
    DamageTakenReduction = 10,
    MinDamageRatio = 11,
    MaxDamageRatio = 12,

    /// <summary>전원 체력 100% 회복(즉시 1회).</summary>
    HealAll = 13,
    /// <summary>전투불능 소녀 부활(즉시, 고른 수만큼).</summary>
    Revive = 14,

    /// <summary>방어구관통력 N배(V1.12).</summary>
    ArmorPen = 15,
}

/// <summary>
/// 증강이 스탯에 얹히는 방식.
/// </summary>
public enum DoomAugmentMode {
    /// <summary>
    /// 실효값을 N배로 만든다. 변신 버프(TransformBuffSkill)와 완전히 같은 규칙을 쓰므로,
    /// 공속/이속/스킬가속처럼 "1 + 스탯/100" 인 스탯도 실효값이 정확히 N배가 된다.
    /// </summary>
    Multiply,
    /// <summary>스탯 값에 그대로 더한다(치명타확률 +5%p 처럼 퍼센트 포인트로 읽히는 것들).</summary>
    Add,
    /// <summary>
    /// 스탯을 건드리지 않고 고른 그 순간 한 번만 일어나는 효과(회복/부활).
    /// 누적치는 "몇 번(몇 명) 받았는가"라는 기록일 뿐이고, 전투 계산에는 들어가지 않는다.
    /// </summary>
    Instant,
}

/// <summary>
/// 멸망전 증강 규칙의 단일 원천. 어떤 증강이 있고, 값이 어느 범위에서 몇 단위로 굴려지며,
/// 어떤 스탯에 어떤 방식으로 얹히는지가 전부 여기 있다.
///
/// 중첩은 전부 "더하기"다. 공격력 1.3배를 먹고 1.5배를 또 먹으면 1.8배가 된다(0.3 + 0.5).
/// 곱으로 쌓으면 같은 증강을 몇 번 먹었느냐에 따라 값이 폭주해 밸런스를 잡을 수 없다.
///
/// 배수(Multiply)를 쓰는 스탯은 변신 버프와 같은 변환을 거친다. 그래서 "공속 1.5배"가
/// 스탯 숫자가 아니라 실제 공격 속도를 1.5배로 만든다(CombatStats.EffectiveScaled 와 같은 식).
/// </summary>
public static class DoomAugmentRules {
    /// <summary>한 번에 보여 주는 증강 선택지 수.</summary>
    public const int ChoiceCount = 3;

    /// <summary>선택지 하나당 새로고침 가능 횟수.</summary>
    public const int RerollPerChoice = 1;

    /// <summary>증강 하나의 정의. 범위/단위와 얹히는 방식까지 한 줄에 모은다.</summary>
    public readonly struct Def {
        public readonly DoomAugmentKind Kind;
        public readonly StatType Stat;
        public readonly DoomAugmentMode Mode;
        public readonly float Min;
        public readonly float Max;
        public readonly float Step;
        public readonly string DisplayName;
        public readonly Color Color;

        /// <summary>
        /// 이 증강이 더 이상 의미가 없어지는 누적치(0 = 상한 없음).
        /// 상한에 닿은 증강은 선택지에 나오지 않는다. 나와 봐야 값을 하지 않아 꽝을 뽑는 셈이기 때문이다.
        /// </summary>
        public readonly float Cap;

        public Def(DoomAugmentKind kind, StatType stat, DoomAugmentMode mode,
                   float min, float max, float step, string displayName, Color color, float cap = 0f) {
            Kind = kind; Stat = stat; Mode = mode;
            Min = min; Max = max; Step = step;
            DisplayName = displayName; Color = color; Cap = cap;
        }
    }

    // 색은 효과 종류를 한눈에 가르기 위한 것이다(공격 계열 붉은색, 방어 계열 푸른색, 유틸 계열 녹색 등).
    private static readonly Color CAttack = new(0.95f, 0.45f, 0.40f);
    private static readonly Color CDefense = new(0.45f, 0.70f, 0.95f);
    private static readonly Color CUtility = new(0.55f, 0.90f, 0.60f);
    private static readonly Color CCrit = new(1.00f, 0.80f, 0.35f);
    private static readonly Color CSpecial = new(0.80f, 0.60f, 0.95f);
    // 회복/부활은 스탯이 아니라 "지금 살아나는" 효과라 한눈에 갈리도록 따로 둔다.
    private static readonly Color CHeal = new(0.40f, 0.95f, 0.80f);

    /// <summary>
    /// 증강 목록. 선택지는 여기서 중복 없이 뽑으므로 종류 수가 ChoiceCount 보다 넉넉해야 한다.
    ///
    /// 치명타확률에 상한을 둔 이유: 실제 판정이 Mathf.Clamp01(CritRate/100) 이라 100%를 넘기면
    /// 그 뒤로는 아무 값도 하지 않는다. 상한을 알려 주면 선택지에서 알아서 빠진다.
    ///
    /// 사거리 범위가 기획안(+1.0~5.0)보다 작은 이유: Range 는 퍼센트가 아니라 절대값이라
    /// 몇 개만 먹어도 전장 밖의 적까지 닿는다. 사거리가 의미를 잃지 않는 선으로 낮춰 두었다.
    /// </summary>
    public static readonly IReadOnlyList<Def> All = new List<Def> {
        new(DoomAugmentKind.Attack,     StatType.Attack,      DoomAugmentMode.Multiply, 1.1f, 2.0f, 0.1f, "공격력",        CAttack),
        new(DoomAugmentKind.Defense,    StatType.Defense,     DoomAugmentMode.Multiply, 1.1f, 2.0f, 0.1f, "방어력",        CDefense),
        new(DoomAugmentKind.Health,     StatType.Health,      DoomAugmentMode.Multiply, 1.1f, 2.0f, 0.1f, "체력",          CDefense),
        new(DoomAugmentKind.AttackSpeed, StatType.AttackSpeed, DoomAugmentMode.Multiply, 1.1f, 2.0f, 0.1f, "공격속도",     CAttack),
        new(DoomAugmentKind.MoveSpeed,  StatType.MoveSpeed,   DoomAugmentMode.Multiply, 1.1f, 2.0f, 0.1f, "이동속도",      CUtility),
        new(DoomAugmentKind.SkillAccel, StatType.SkillAccel,  DoomAugmentMode.Multiply, 1.1f, 2.0f, 0.1f, "스킬가속",      CUtility),
        // 방어구관통력은 방어력을 직접 깎는 절대 수치라 다른 배수보다 크게(2~5배, 1배 단위) 굴린다(V1.12).
        // 관통력이 0 인 소녀는 몇 배를 먹어도 0 이다(배수 증강이라 기준값이 있어야 오른다).
        new(DoomAugmentKind.ArmorPen,   StatType.ArmorPen,    DoomAugmentMode.Multiply, 2f,   5f,   1f,   "방어구관통력",  CAttack),

        new(DoomAugmentKind.CritRate,   StatType.CritRate,    DoomAugmentMode.Add,   5f,  10f,  1f,  "치명타확률",    CCrit, cap: 100f),
        new(DoomAugmentKind.CritDamage, StatType.CritDamage,  DoomAugmentMode.Add,  50f, 300f, 10f,  "치명타데미지",  CCrit),
        new(DoomAugmentKind.Range,      StatType.Range,       DoomAugmentMode.Add, 0.2f, 1.0f, 0.1f, "사거리",        CUtility),
        new(DoomAugmentKind.FinalDamage, StatType.FinalDamage, DoomAugmentMode.Add, 10f, 30f,  1f,  "최종데미지",    CSpecial),
        new(DoomAugmentKind.DamageTakenReduction, StatType.DamageTakenReduction, DoomAugmentMode.Add, 10f, 30f, 1f, "피격데미지감소", CDefense),
        new(DoomAugmentKind.MinDamageRatio, StatType.MinDamageRatio, DoomAugmentMode.Add, 50f, 200f, 10f, "최소데미지비율", CSpecial),
        new(DoomAugmentKind.MaxDamageRatio, StatType.MaxDamageRatio, DoomAugmentMode.Add, 50f, 200f, 10f, "최대데미지비율", CSpecial),

        // 즉시 효과. StatType 은 쓰이지 않으므로 아무 값이나 두되, Apply 가 Instant 를 통째로 건너뛴다.
        // 수치를 굴릴 것이 없는 회복은 범위를 1~1 로 고정한다(값이 곧 "1회"라는 뜻).
        new(DoomAugmentKind.HealAll, StatType.Health, DoomAugmentMode.Instant, 1f, 1f, 1f, "전원 체력 회복", CHeal),
        new(DoomAugmentKind.Revive,  StatType.Health, DoomAugmentMode.Instant, 1f, 20f, 1f, "전투불능 부활",  CHeal),
    };

    public static Def Get(DoomAugmentKind kind) {
        foreach (var d in All)
            if (d.Kind == kind) return d;
        return All[0];
    }

    /// <summary>범위 안에서 단위에 맞춰 값을 하나 굴린다(양끝 포함).</summary>
    public static float Roll(in Def def, System.Random random) {
        int steps = Mathf.Max(0, Mathf.RoundToInt((def.Max - def.Min) / def.Step));
        float value = def.Min + def.Step * random.Next(0, steps + 1);
        // 0.1 단위를 더해 나가면 부동소수 찌꺼기가 남아 1.7000002 같은 값이 표시된다.
        return Mathf.Round(value / def.Step) * def.Step;
    }

    /// <summary>
    /// 한 번의 선택지에 올릴 증강들을 뽑는다. 같은 창에 같은 종류가 두 번 나오지 않으며,
    /// exclude 에 든 종류(새로고침 전에 있던 것)도 피한다.
    /// 이미 상한에 닿은 종류는 애초에 후보에서 빠진다.
    /// </summary>
    public static List<DoomAugmentKind> Draw(int count, IReadOnlyCollection<DoomAugmentKind> exclude,
                                             DoomAugmentState state, System.Random random) {
        var pool = new List<DoomAugmentKind>();
        foreach (var d in All) {
            if (exclude != null && exclude.Contains(d.Kind)) continue;
            if (state != null && state.IsCapped(d.Kind)) continue;
            pool.Add(d.Kind);
        }

        var picked = new List<DoomAugmentKind>(count);
        for (int i = 0; i < count && pool.Count > 0; i++) {
            int index = random.Next(pool.Count);
            picked.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return picked;
    }

    /// <summary>
    /// 선택 카드의 수치 문구. 이름은 카드가 따로 보여 주므로 값만 쓴다.
    /// 예: "1.5배", "+7%", "+0.4", "+100%", "3명".
    ///
    /// 표기 규칙을 여기 한 곳에 모은다. 화면이 제 나름대로 "+{값}%" 를 붙이면 즉시 효과가
    /// "전원 체력 회복 +1%" 처럼 엉뚱하게 나온다.
    /// </summary>
    public static string DescribeValue(DoomAugmentKind kind, float value) {
        var def = Get(kind);
        switch (kind) {
            // 회복은 언제나 전부 채우므로 굴린 값(1)이 아니라 뜻을 그대로 쓴다.
            case DoomAugmentKind.HealAll: return "+100%";
            // 부활은 퍼센트가 아니라 사람 수다.
            case DoomAugmentKind.Revive:  return $"{value:0}명";
        }

        if (def.Mode == DoomAugmentMode.Multiply) return $"{value:0.#}배";
        return def.Stat == StatType.Range ? $"+{value:0.#}" : $"+{value:0.#}%";
    }

    /// <summary>
    /// 누적된 결과를 한 줄로 쓴 문구(증강 목록 표시용).
    /// 즉시 효과는 지금 걸려 있는 효과가 아니라 "이번 판에 몇 번(몇 명) 받았는가"라는 기록이다.
    /// </summary>
    public static string DescribeTotal(DoomAugmentKind kind, float total) {
        var def = Get(kind);
        switch (kind) {
            case DoomAugmentKind.HealAll: return $"전원 체력 회복 {total:0}회";
            case DoomAugmentKind.Revive:  return $"전투불능 부활 누적 {total:0}명";
        }

        if (def.Mode == DoomAugmentMode.Multiply) return $"{def.DisplayName} {1f + total:0.#}배";
        return def.Stat == StatType.Range
            ? $"{def.DisplayName} +{total:0.#}"
            : $"{def.DisplayName} +{total:0.#}%";
    }
}
