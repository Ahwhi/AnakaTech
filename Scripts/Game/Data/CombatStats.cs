using System;
using UnityEngine;

/// <summary>
/// 전투 스탯 값 묶음. 캐릭터/몬스터/장비가 공용으로 사용한다.
/// 모든 값을 float 로 두어 성장 합산(레벨/별레벨/장비/투자)에서 소수 계수도 표현 가능하게 한다.
/// 표시할 때는 소비측에서 반올림한다.
///
/// 단위 규약(스탯마다 사는 스케일이 다르다):
///  - Attack/Defense/Health : 절대값(딜/HP 스케일).
///  - Range                 : 월드 거리. 캐릭터는 무기타입 고정값으로 덮어써 성장/투자 대상이 아니다(CharacterRuntime).
///  - AttackSpeed/MoveSpeed : 기준값 대비 가속 퍼센트 포인트(%p). 0 = 기준 그대로(100%), 50 = +50%(1.5배).
///  - CritRate              : 치명타 확률 퍼센트(15 = 15%).
///  - CritDamage            : 치명타 시 추가 데미지 %p(50 = +50%).
///  - MinDamageRatio        : 데미지 지터의 하한을 끌어올리는 %p(안정성). 0 = 기본 지터 하한, 100 = 하한이 기준값까지.
///  - MaxDamageRatio        : 데미지 지터의 상한을 끌어올리는 %p(고배율 로망). 0 = 기본 지터 상한.
///  - ArmorPen              : 상대 방어력을 깎는 절대 수치(V1.12 부터. 이전에는 %p). 유효방어 = max(0, 방어 - 관통).
///  - FinalDamage           : 최종 데미지 증가 %p(모든 계산 마지막에 곱).
///  - SkillAccel            : 스킬 발동 가속 %p(점근 감소식에 투입, CombatMath.SkillInterval).
///  - SkillCooldownReduction: 스킬 기본 쿨타임을 깎는 절대 초(별 성장 전용).
///  - CritResist            : 치명타 저항 %p. 받는 치명타의 "추가 배율"만 점근 감쇄한다(일반타 데미지는 불변).
///  - StatusResist          : 상태이상 저항 %p. 상태이상 지속시간을 점근 감쇄한다(상태이상 구현 시 소비).
///  - DamageTakenReduction  : 피격 데미지 감소 %p. 최종 데미지에 독립 곱연산으로 적용한다.
///  - DoubleAttackChance    : 기본공격이 한 번 더 나갈 확률 %(50 = 50%).
///  - EvasionChance         : 피격을 무효로 할 확률 %(50 = 50%).
///
/// 방어 스탯 3종(CritResist/StatusResist/DamageTakenReduction)은 모두 Resist01 의 점근식을 통과한다.
/// 공격 스탯에서 뺄셈하지 않으므로 서로를 무효화하지 않고, 100% 도달이 원천 불가해 장기 스탯 인플레에 안전하다.
///
/// 반면 발동형 스탯 2종(DoubleAttackChance/EvasionChance)은 값이 곧 확률이라 점근 감쇄를 쓰지 않는다.
/// 획득 경로가 수련(캐릭터당 1개)과 계정 투자 두 곳이고 둘 다 카탈로그 최대 수치가 정해져 있어 합산 상한이 고정된다.
/// </summary>
[Serializable]
public struct CombatStats {
    public float Attack;
    public float Defense;
    public float Health;
    public float Range;
    public float AttackSpeed;
    public float MoveSpeed;
    public float CritRate;
    public float CritDamage;

    // 신규 스탯(투자는 SkillAccel 만 가능, 나머지는 별 성장 전용). 순서를 바꾸지 말 것(직렬화/StatType 매핑).
    public float MinDamageRatio;
    public float MaxDamageRatio;
    public float ArmorPen;
    public float FinalDamage;
    public float SkillAccel;
    public float SkillCooldownReduction;

    // 방어 스탯(장비 옵션 전용, 인덱스 14~). 순서 고정.
    public float CritResist;
    public float StatusResist;
    public float DamageTakenReduction;

    // 발동형 스탯(수련 전용, 인덱스 17~). 순서 고정.
    public float DoubleAttackChance;
    public float EvasionChance;

    /// <summary>
    /// 모든 스탯이 0인지(= 보정이 사실상 없는지). 빈 보정을 걸러내는 판정에 쓴다.
    ///
    /// 전투력은 여기 있지 않다. 전투력은 스탯만으로 정해지지 않고 전역 튜닝값(방어계수/지터/기준 공속)과
    /// 캐릭터 고유 스킬 화력까지 필요하므로 CombatPowerFormula 가 담당한다. 스탯 묶음은 그것들을 알지 못한다.
    /// </summary>
    public bool IsZero =>
        Attack == 0f && Defense == 0f && Health == 0f && Range == 0f
        && AttackSpeed == 0f && MoveSpeed == 0f && CritRate == 0f && CritDamage == 0f
        && MinDamageRatio == 0f && MaxDamageRatio == 0f && ArmorPen == 0f && FinalDamage == 0f
        && SkillAccel == 0f && SkillCooldownReduction == 0f
        && CritResist == 0f && StatusResist == 0f && DamageTakenReduction == 0f
        && DoubleAttackChance == 0f && EvasionChance == 0f;

    public float Get(StatType stat) => stat switch {
        StatType.Attack                 => Attack,
        StatType.Defense                => Defense,
        StatType.Health                 => Health,
        StatType.Range                  => Range,
        StatType.AttackSpeed            => AttackSpeed,
        StatType.MoveSpeed              => MoveSpeed,
        StatType.CritRate               => CritRate,
        StatType.CritDamage             => CritDamage,
        StatType.MinDamageRatio         => MinDamageRatio,
        StatType.MaxDamageRatio         => MaxDamageRatio,
        StatType.ArmorPen               => ArmorPen,
        StatType.FinalDamage            => FinalDamage,
        StatType.SkillAccel             => SkillAccel,
        StatType.SkillCooldownReduction => SkillCooldownReduction,
        StatType.CritResist             => CritResist,
        StatType.StatusResist           => StatusResist,
        StatType.DamageTakenReduction   => DamageTakenReduction,
        StatType.DoubleAttackChance     => DoubleAttackChance,
        StatType.EvasionChance          => EvasionChance,
        _ => 0f
    };

    /// <summary>지정 스탯에 delta 를 더한 새 값을 반환(불변 스타일).</summary>
    public CombatStats WithAdded(StatType stat, float delta) {
        var s = this;
        switch (stat) {
            case StatType.Attack:                 s.Attack                 += delta; break;
            case StatType.Defense:                s.Defense                += delta; break;
            case StatType.Health:                 s.Health                 += delta; break;
            case StatType.Range:                  s.Range                  += delta; break;
            case StatType.AttackSpeed:            s.AttackSpeed            += delta; break;
            case StatType.MoveSpeed:              s.MoveSpeed              += delta; break;
            case StatType.CritRate:               s.CritRate               += delta; break;
            case StatType.CritDamage:             s.CritDamage             += delta; break;
            case StatType.MinDamageRatio:         s.MinDamageRatio         += delta; break;
            case StatType.MaxDamageRatio:         s.MaxDamageRatio         += delta; break;
            case StatType.ArmorPen:               s.ArmorPen               += delta; break;
            case StatType.FinalDamage:            s.FinalDamage            += delta; break;
            case StatType.SkillAccel:             s.SkillAccel             += delta; break;
            case StatType.SkillCooldownReduction: s.SkillCooldownReduction += delta; break;
            case StatType.CritResist:             s.CritResist             += delta; break;
            case StatType.StatusResist:           s.StatusResist           += delta; break;
            case StatType.DamageTakenReduction:   s.DamageTakenReduction   += delta; break;
            case StatType.DoubleAttackChance:     s.DoubleAttackChance     += delta; break;
            case StatType.EvasionChance:          s.EvasionChance          += delta; break;
        }
        return s;
    }

    /// <summary>
    /// 지정 스탯의 실효값을 multiple 배로 만든 새 값(변신 버프/멸망전 증강/무한 타워 보너스 공용 규칙).
    /// 공속/이속/스킬가속은 "1 + 스탯/100" 배율이라 스탯 값을 N배 해서는 실효값이 N배가 되지 않으므로 따로 변환한다.
    /// </summary>
    public CombatStats WithEffectiveScale(StatType stat, float multiple) {
        float baseValue = Get(stat);
        return WithAdded(stat, EffectiveScaled(stat, baseValue, multiple) - baseValue);
    }

    /// <summary>실효값이 multiple 배가 되는 스탯 값. 미성장(스탯 0) 가속 스탯도 정확히 N배가 된다.</summary>
    public static float EffectiveScaled(StatType stat, float baseValue, float multiple) {
        if (IsGearingStat(stat)) return baseValue * multiple + 100f * (multiple - 1f);
        return baseValue * multiple;
    }

    /// <summary>"1 + 스탯/100" 형태의 배율 스탯(가속류)인지.</summary>
    public static bool IsGearingStat(StatType stat) =>
        stat == StatType.AttackSpeed || stat == StatType.MoveSpeed || stat == StatType.SkillAccel;

    public static CombatStats operator +(CombatStats a, CombatStats b) => new() {
        Attack                 = a.Attack                 + b.Attack,
        Defense                = a.Defense                + b.Defense,
        Health                 = a.Health                 + b.Health,
        Range                  = a.Range                  + b.Range,
        AttackSpeed            = a.AttackSpeed            + b.AttackSpeed,
        MoveSpeed              = a.MoveSpeed              + b.MoveSpeed,
        CritRate               = a.CritRate               + b.CritRate,
        CritDamage             = a.CritDamage             + b.CritDamage,
        MinDamageRatio         = a.MinDamageRatio         + b.MinDamageRatio,
        MaxDamageRatio         = a.MaxDamageRatio         + b.MaxDamageRatio,
        ArmorPen               = a.ArmorPen               + b.ArmorPen,
        FinalDamage            = a.FinalDamage            + b.FinalDamage,
        SkillAccel             = a.SkillAccel             + b.SkillAccel,
        SkillCooldownReduction = a.SkillCooldownReduction + b.SkillCooldownReduction,
        CritResist             = a.CritResist             + b.CritResist,
        StatusResist           = a.StatusResist           + b.StatusResist,
        DamageTakenReduction   = a.DamageTakenReduction   + b.DamageTakenReduction,
        DoubleAttackChance     = a.DoubleAttackChance     + b.DoubleAttackChance,
        EvasionChance          = a.EvasionChance          + b.EvasionChance
    };

    public static CombatStats operator *(CombatStats a, float k) => new() {
        Attack                 = a.Attack                 * k,
        Defense                = a.Defense                * k,
        Health                 = a.Health                 * k,
        Range                  = a.Range                  * k,
        AttackSpeed            = a.AttackSpeed            * k,
        MoveSpeed              = a.MoveSpeed              * k,
        CritRate               = a.CritRate               * k,
        CritDamage             = a.CritDamage             * k,
        MinDamageRatio         = a.MinDamageRatio         * k,
        MaxDamageRatio         = a.MaxDamageRatio         * k,
        ArmorPen               = a.ArmorPen               * k,
        FinalDamage            = a.FinalDamage            * k,
        SkillAccel             = a.SkillAccel             * k,
        SkillCooldownReduction = a.SkillCooldownReduction * k,
        CritResist             = a.CritResist             * k,
        StatusResist           = a.StatusResist           * k,
        DamageTakenReduction   = a.DamageTakenReduction   * k,
        DoubleAttackChance     = a.DoubleAttackChance     * k,
        EvasionChance          = a.EvasionChance          * k
    };

    /// <summary>치명타 확률(0~1). CritRate 퍼센트 값을 확률로 변환하고 0~1 로 자른다.</summary>
    public float CritChance01 => Mathf.Clamp01(CritRate / 100f);

    /// <summary>치명타 데미지 배수(예: CritDamage 50 -> 1.5).</summary>
    public float CritMultiplier => 1f + Mathf.Max(0f, CritDamage) / 100f;

    /// <summary>최종 데미지 배수(예: FinalDamage 10 -> 1.1). 모든 데미지 계산 마지막에 곱한다.</summary>
    public float FinalDamageMultiplier => 1f + Mathf.Max(0f, FinalDamage) / 100f;

    /// <summary>기준 대비 공격속도 배수(예: AttackSpeed 50 -> 1.5). 감쇠는 소비측(CombatMath)에서 별도 적용.</summary>
    public float AttackSpeedMultiplier => 1f + AttackSpeed / 100f;

    /// <summary>기준 대비 이동속도 배수(예: MoveSpeed 20 -> 1.2).</summary>
    public float MoveSpeedMultiplier => 1f + MoveSpeed / 100f;

    /// <summary>
    /// 점근 저항 계수(0~1). value=0 -> 0, value=k -> 0.5, value 무한 -> 1(도달 불가).
    /// 방어 스탯 3종(치명타저항/상태이상저항/피격데미지감소)이 공유하는 단일 변환식이다.
    ///
    /// 하드캡(예: 75% 상한) 대신 점근식을 쓰는 이유:
    ///  - 캡에 닿은 뒤 추가 투자가 완전히 죽는(dead stat) 현상이 없다. 항상 조금이라도 값을 한다.
    ///  - 100%/0% 도달이 원천 불가하므로 무적/완전무효 밸런스 사고가 나지 않는다.
    ///  - 값이 커질수록 체감이 완만해져 장기 운영의 스탯 인플레를 자동으로 흡수한다.
    /// 공속/스킬가속(CombatMath)이 쓰는 K/(K+x) 감쇠와 같은 계열의 식이다.
    /// </summary>
    public static float Resist01(float value, float k) {
        if (value <= 0f) return 0f;
        return value / (value + Mathf.Max(0.0001f, k));
    }

    /// <summary>치명타 저항 계수(0~1). 받는 치명타의 추가 배율에 (1 - 이 값)을 곱한다.</summary>
    public float CritResist01(float k) => Resist01(CritResist, k);

    /// <summary>상태이상 지속시간 감쇄 계수(0~1). 실제 지속시간 = 기본 지속시간 * (1 - 이 값).</summary>
    public float StatusResist01(float k) => Resist01(StatusResist, k);

    /// <summary>피격 데미지 감소 계수(0~1). 최종 데미지에 (1 - 이 값)을 곱한다.</summary>
    public float DamageTakenReduction01(float k) => Resist01(DamageTakenReduction, k);

    /// <summary>2회 공격 발동 확률(0~1). 기본공격 직후 한 번 더 나갈지 판정한다.</summary>
    public float DoubleAttackChance01 => Mathf.Clamp01(DoubleAttackChance / 100f);

    /// <summary>회피 확률(0~1). 1 에 닿으면 무적이 되므로 상한을 살짝 남겨 둔다.</summary>
    public float EvasionChance01 => Mathf.Clamp(EvasionChance / 100f, 0f, MaxEvasionChance01);

    /// <summary>회피 확률의 상한(0~1). 획득 상한이 카탈로그로 묶여 있어도, 규칙이 바뀌어 100% 가 되는 사고를 막는 안전장치다.</summary>
    public const float MaxEvasionChance01 = 0.9f;
}
