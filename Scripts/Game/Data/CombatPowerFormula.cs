using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투력 산출 튜닝값. BattleSettings 에 묶여 인스펙터에서 조정한다.
/// 전투 밸런스에는 전혀 영향을 주지 않으며, 오직 "표시되는 전투력 숫자"만 바꾼다.
/// </summary>
[Serializable]
public sealed class CombatPowerSettings {
    [Header("Scale")]
    [Tooltip("최종 배수(표시 자릿수 조정용). 성장 구간별 비율은 그대로 두고 숫자 크기만 바뀐다. "
           + "기본값은 V1.9 공식과 유저 중앙값이 같아지도록 맞춘 값이다.")]
    public float PowerScale = 22.1f;
    [Tooltip("공격 비중. 전투력 = DPS^이값 x 실효체력^(1-이값). "
           + "1/3 미만으로 내리면 공격력을 올렸을 때 전투력이 떨어질 수 있으므로 하한을 둔다(CombatPowerFormula 주석 참고).")]
    [Range(0.34f, 1f)] public float OffenseWeight = 0.5f;

    [Header("Reference Target (기준 더미: 화력 측정용)")]
    [Tooltip("기준 더미의 방어력 = 평가 대상의 공격력 x 이 값. 방어관통의 가치를 재는 기준이다. "
           + "공격력에만 연동하므로 체력/방어 성장은 화력 측정에 영향을 주지 않는다.")]
    public float ReferenceDefenseRatio = 0.15f;
    [Tooltip("광역 스킬 평가에 쓰는 기준 적 수. 적 전체 스킬은 이 수만큼 맞히는 것으로 계산한다. "
           + "올리면 광역 캐릭터의 전투력이 높아진다. 단일 대상 컨텐츠(레이드)까지 감안한 평균값으로 잡을 것.")]
    public float ReferenceEnemyCount = 3f;

    [Header("Reference Attacker (기준 공격자: 생존력 측정용)")]
    [Tooltip("기준 공격자의 공격력 = 평가 대상의 컨텐츠 티어 지표 x 이 값. '자기 티어의 적에게 맞는다'는 가정이다.")]
    public float ReferenceAttackRatio = 1f;
    [Tooltip("기준 공격자 공격력의 하한. 무장비 저스펙 캐릭터에서 0 나눗셈을 막는다.")]
    public float MinReferenceAttack = 5f;
    [Tooltip("컨텐츠 티어 지표를 낼 때 체력을 공격력 스케일로 환산하는 나눗수. 티어 지표 = sqrt(공격력 x 체력/이값). "
           + "장비 1포인트가 주는 체력/공격력 비율(EquipmentStatLayout.HealthPerPoint)과 맞춰 둔다.")]
    public float TierHealthPerAttack = 10f;
    [Tooltip("기준 공격자의 치명타 확률(%). 치명타저항 스탯의 가치를 재는 기준이다.")]
    public float ReferenceCritRate = 30f;
    [Tooltip("기준 공격자의 치명타 데미지(%p).")]
    public float ReferenceCritDamage = 100f;
    [Tooltip("방어력 가치 계수. 받는 피해 비율 = 기본 비율 / (1 + 이 값 x 방어력 x 방어계수 / 기준 공격력). "
           + "올리면 방어력의 전투력 기여가 커진다. 어떤 값이어도 스탯 증가 시 전투력이 줄지 않는다.")]
    [Min(0f)] public float DefenseWeight = 6f;

    [Header("Utility (DPS/실효체력에 들어가지 않는 스탯)")]
    [Tooltip("이동속도 가중치. 이동속도는 데미지도 생존력도 아니어서 전투력 = ... x (1 + 이속% x 이 값) 으로만 반영한다.")]
    public float MoveSpeedWeight = 0.1f;
    [Tooltip("이동속도 감쇠 시작점(%). 이 값까지는 이동속도를 그대로 반영하고, 넘는 부분은 점점 덜 반영한다. " +
             "실제 전투의 이동속도에는 영향이 없고 전투력 표시만 바뀐다.")]
    [Min(0f)] public float MoveSpeedSoftCapStart = 2000f;
    [Tooltip("감쇠 구간에서 더해질 수 있는 최대치(%). 반영치는 '시작점 + 이 값'에 점근하며, 이동속도가 오르면 " +
             "반영치도 항상 조금씩은 오른다(이속 성장 시 전투력이 멈추지 않는다). 0 이면 시작점에서 더 오르지 않으므로 0 보다 크게 둔다.")]
    [Min(1f)] public float MoveSpeedSoftCapExtra = 2000f;
    [Tooltip("상태이상저항 가중치. 상태이상이 미구현이라 실효체력에 축소 반영한다. "
           + "상태이상을 구현하면 이 근사를 실제 기여로 교체할 것.")]
    public float StatusResistWeight = 0.25f;
    [Tooltip("사거리 증가 가중치(월드 거리 1당). 전투력 = ... x (1 + 사거리증가분 x 이 값). "
           + "무기타입 고정 사거리는 캐릭터 정체성이라 넣지 않고, 수련 등으로 '키워서 얻은 증가분'만 반영한다.")]
    public float RangeBonusWeight = 0.05f;
    [Tooltip("전투 상한을 넘긴 스탯의 초과분 가중치. 치명타확률 100%, 2회공격 100%, 회피 상한, "
           + "스킬 쿨타임 하한에 닿은 뒤의 성장은 실제 전투 효과가 없지만, 성장이 전투력에 보이도록 "
           + "전투력 = ... x (1 + 초과분 합계% / 100 x 이 값) 으로 반영한다(전투에는 영향 없음). 0 이면 초과분을 무시한다.")]
    [Min(0f)] public float CapOverflowWeight = 0.05f;
    [Tooltip("전투력 계산에만 쓰는 치명타 확률 하한(%). 치명타 확률이 0 이면 치명타 데미지가 전투력에 반영되지 않으므로, "
           + "환산 확률 = 이 값 + (1 - 이 값/100) x 실제 확률 로 잡아 치명타 데미지 성장도 항상 보이게 한다(전투에는 영향 없음).")]
    [Range(0f, 10f)] public float CritChanceFloor = 1f;

    [Header("Conditional (조건부 효과 환산)")]
    [Tooltip("전투 시작 N초간 효과(BattleStart)의 가동률 기준이 되는 전투 1회 길이(초). "
           + "가동률 = 지속시간 / 이 값(최대 1). 예: 30 이면 10초 효과는 1/3 만 반영된다. "
           + "내리면 초반 집중형 각인(선봉 등)의 전투력이 높아진다.")]
    [Min(1f)] public float ReferenceBattleSeconds = 30f;
    [Tooltip("체력 N% 이하 효과(SelfHpBelow)의 가동률 배수. 가동률 = N% x 이 값(최대 1). "
           + "1 이면 체력이 일정하게 깎인다고 보고 체력 구간 비율을 그대로 가동률로 쓴다(50% 이하 = 절반). "
           + "내리면 위기 대응형 각인(수호 등)의 전투력이 낮아진다.")]
    [Min(0f)] public float LowHealthUptimeScale = 1f;
}

/// <summary>
/// 표시용 전투력 산출기. CombatMath 와 같은 위상의 순수 정적 계산기로, 상태를 갖지 않고 통신도 하지 않는다.
///
/// 설계 의도: 전투력을 임의의 가중치 표가 아니라 "게임의 실제 전투 공식"에서 유도한다.
///  1) 화력   = 기준 더미를 상대로 한 초당 기대 피해(기본공격 + 고유 스킬)
///  2) 생존력 = 기준 공격자를 상대로 한 실효 체력(체력 / 받는 피해 비율)
///  3) 전투력 = PowerScale x 화력^w x 생존력^(1-w) x 유틸 보정
///
/// 단조성 규칙(가장 중요): 어떤 스탯이 오르더라도 전투력은 절대 내려가지 않아야 한다.
/// 기준 상대(더미/공격자)가 평가 대상의 스탯을 따라 자라므로, 그 연동이 성장을 상쇄하지 않도록 다음을 지킨다.
///  - 기준 더미 방어력은 공격력에만 연동한다. 그러면 화력은 공격력에 정비례하고 체력과는 무관하다.
///  - 방어력은 뺄셈이 아니라 1 / (1 + k x 방어 / 기준공격) 형태로 평가한다. 기준 공격자가 sqrt(공격 x 체력) 로
///    자라도 실효체력의 탄력성이 체력에 대해 1/2 이상, 공격력에 대해 -1/2 이상으로 묶인다.
///  - 따라서 공격 비중 w 가 1/3 이상이면 공격력/체력 1% 성장마다 전투력이 최소 (w - (1-w)/2)% 오른다.
///    그 밖의 스탯은 전부 화력 또는 실효체력 또는 유틸 보정을 늘리기만 한다.
///  - 실제 전투 상한(치명타 확률 100% 등)에 닿은 뒤의 성장은 초과분 보정(CapOverflowWeight)으로, 치명타 확률이 0 인
///    캐릭터의 치명타 데미지 성장은 환산 확률 하한(CritChanceFloor)으로 반영한다. 둘 다 전투에는 영향이 없다.
///  - 직접 피해가 없는 버프형 스킬은 UnitSkill.PowerProfile 로 버프 가치를 환산해 신고한다.
/// (V1.9 까지는 뺄셈 방어 + 체력 연동 더미라 체력/공격 성장 시 전투력이 떨어지는 구간이 있었다.)
///
/// 데미지 계산은 CombatMath.ExpectedDamage(= ResolveDamage 의 기대값 버전)를 그대로 쓴다.
/// 조건부 효과(각인의 전투 시작 N초간/체력 N% 이하 등)는 가동률만큼 곱해 상시 스탯으로 환산한 뒤 더한다.
/// </summary>
public static class CombatPowerFormula {
    /// <summary>
    /// 최종 스탯 + 고유 스킬로 표시용 전투력을 계산한다.
    /// skill 이 SkillPowerProfile.None 이거나 쿨타임이 0 이하면 기본 공격만으로 화력을 잡는다.
    /// </summary>
    /// <param name="rangeBonus">무기타입 고정 사거리 위에 얹힌 사거리 증가분(CharacterRuntime.RangeBonus).</param>
    /// <param name="conditionals">전투 중에만 켜지는 조건부 효과(각인). 가동률로 환산해 반영한다.</param>
    public static float Evaluate(in CombatStats stats, in SkillPowerProfile skill, float skillBaseCooldown,
                                 BattleSettings settings, float rangeBonus = 0f,
                                 IReadOnlyList<EngravingLoadout.Conditional> conditionals = null) {
        if (settings == null) return 0f;
        var cp = settings.CombatPower;
        if (cp == null) return 0f;

        var evaluated = stats;
        if (conditionals != null && conditionals.Count > 0) {
            var equivalent = ConditionalEquivalent(conditionals, cp);
            evaluated += equivalent;
            rangeBonus += Mathf.Max(0f, equivalent.Range);
        }

        float dps = DamagePerSecond(evaluated, skill, skillBaseCooldown, settings, cp);
        float ehp = EffectiveHealth(evaluated, settings, cp);
        if (dps <= 0f || ehp <= 0f) return 0f;

        float w = Mathf.Clamp01(cp.OffenseWeight);
        float power = Mathf.Pow(dps, w) * Mathf.Pow(ehp, 1f - w);
        power *= 1f + EffectiveMoveSpeed(evaluated.MoveSpeed, cp) / 100f * Mathf.Max(0f, cp.MoveSpeedWeight);
        power *= 1f + Mathf.Max(0f, rangeBonus) * Mathf.Max(0f, cp.RangeBonusWeight);
        power *= 1f + CapOverflowPercent(evaluated, skill, skillBaseCooldown, settings) / 100f * Mathf.Max(0f, cp.CapOverflowWeight);

        return Mathf.Max(0f, cp.PowerScale * power);
    }

    /// <summary>
    /// 실제 전투에서 상한에 걸려 더는 효과가 없는 성장분의 합계(%). 상한은 CombatStats/CombatMath 의 실제 규칙과 같다.
    /// 스킬 쿨타임 감소는 하한에 닿은 뒤의 초과 초를 기본 쿨타임 대비 비율(%)로 환산한다.
    /// </summary>
    private static float CapOverflowPercent(in CombatStats stats, in SkillPowerProfile skill, float skillBaseCooldown,
                                            BattleSettings s) {
        // 방어구관통은 V1.12 부터 방어 수치를 직접 깎아 고정 상한이 없으므로 여기 들어가지 않는다.
        // (기준 더미 방어력이 공격력에 연동되므로, 관통 초과분을 넣으면 공격력이 오를 때 초과분이 줄어 단조성이 깨질 수 있다.)
        float overflow = Mathf.Max(0f, stats.CritRate - 100f)
                       + Mathf.Max(0f, stats.DoubleAttackChance - 100f)
                       + Mathf.Max(0f, stats.EvasionChance - CombatStats.MaxEvasionChance01 * 100f);

        if (!skill.IsEmpty && skillBaseCooldown > 0f) {
            float reducible = Mathf.Max(0f, skillBaseCooldown - Mathf.Max(0.05f, s.SkillCooldownFloorSeconds));
            float overSeconds = Mathf.Max(0f, stats.SkillCooldownReduction - reducible);
            overflow += overSeconds / skillBaseCooldown * 100f;
            // 쿨타임이 하한에 닿으면 스킬 가속도 발동 주기를 더 줄이지 못한다(CombatMath.SkillInterval). 그때는 가속 전체가 초과분이다.
            if (stats.SkillCooldownReduction >= reducible) overflow += Mathf.Max(0f, stats.SkillAccel);
        }
        return overflow;
    }

    /// <summary>
    /// 전투력에 반영할 이동속도(%). 실제 전투에서 이동속도는 적에게 붙는 시간만 줄여 주므로 높아질수록 가치가 줄어든다.
    /// 시작점까지는 그대로, 넘는 부분은 extra x over / (extra + over) 로 더한다(V1.13).
    /// 이 함수의 기울기는 시작점 이후에도 extra^2 / (extra + over)^2 > 0 이라 이동속도가 오르면 전투력도 반드시 오른다.
    /// </summary>
    private static float EffectiveMoveSpeed(float moveSpeed, CombatPowerSettings cp) {
        float m = Mathf.Max(0f, moveSpeed);
        float start = Mathf.Max(0f, cp.MoveSpeedSoftCapStart);
        if (m <= start) return m;

        float extra = Mathf.Max(1f, cp.MoveSpeedSoftCapExtra);
        float over = m - start;
        return start + extra * over / (extra + over);
    }

    // 화력 측정용 공격자 스탯. 치명타 확률을 하한 쪽으로 살짝 끌어올려 확률 0 인 캐릭터도 치명타 데미지 성장이 보이게 한다.
    // 확률이 오를수록 환산 확률도 오르므로(기울기 1 - 하한/100) 치명타 확률 성장의 단조성은 그대로다.
    private static CombatStats PowerAttacker(in CombatStats stats, CombatPowerSettings cp) {
        var attacker = stats;
        float floor = Mathf.Clamp(cp.CritChanceFloor, 0f, 100f);
        attacker.CritRate = floor + (1f - floor / 100f) * Mathf.Max(0f, stats.CritRate);
        return attacker;
    }

    /// <summary>조건부 효과들을 가동률만큼 곱해 상시 스탯으로 환산한 합계.</summary>
    private static CombatStats ConditionalEquivalent(IReadOnlyList<EngravingLoadout.Conditional> conditionals,
                                                     CombatPowerSettings cp) {
        var sum = new CombatStats();
        for (int i = 0; i < conditionals.Count; i++) {
            var c = conditionals[i];
            float uptime = Uptime(c.Trigger, c.Param, cp);
            if (uptime > 0f) sum = sum.WithAdded(c.Stat, c.Value * uptime);
        }
        return sum;
    }

    /// <summary>
    /// 조건이 전투 중 평균적으로 켜져 있는 비율(0~1). 새 조건을 추가하면 이 switch 에도 가동률 규칙을 넣는다
    /// (넣지 않으면 0 으로 취급되어 전투력에 반영되지 않는다).
    /// </summary>
    private static float Uptime(EngravingTrigger trigger, float param, CombatPowerSettings cp) => trigger switch {
        EngravingTrigger.Always      => 1f,
        EngravingTrigger.BattleStart => Mathf.Clamp01(param / Mathf.Max(1f, cp.ReferenceBattleSeconds)),
        EngravingTrigger.SelfHpBelow => Mathf.Clamp01(param / 100f * Mathf.Max(0f, cp.LowHealthUptimeScale)),
        _ => 0f
    };

    /// <summary>기준 더미를 상대로 한 초당 기대 피해(기본 공격 + 고유 스킬).</summary>
    private static float DamagePerSecond(in CombatStats stats, in SkillPowerProfile skill, float skillBaseCooldown,
                                         BattleSettings s, CombatPowerSettings cp) {
        var dummy = ReferenceTarget(stats, cp);
        var attacker = PowerAttacker(stats, cp);

        // 기본 공격: 배수 1 로 공속만큼 반복. 공속 소프트캡 설정도 실제 전투와 동일하게 통과시킨다.
        float attacksPerSecond = Mathf.Max(0f, s.BaseAttacksPerSecond) * CombatMath.AttackSpeedMultiplier(
            stats.AttackSpeed, s.EnableAttackSpeedSoftCap, s.AttackSpeedSoftCapStart, s.AttackSpeedSoftK);
        // 2회 공격은 기본공격만 한 번 더 나가므로(스킬은 대상이 아니다) 기본공격 화력에만 기대 타수를 곱한다.
        float dps = Expected(attacker, dummy, 1f, s) * attacksPerSecond * (1f + stats.DoubleAttackChance01);

        // 고유 스킬: 발동 주기당 (타격 수 x 대상 수 x 1타 기대피해).
        // 총배율을 타격 수로 나눠 넣는 이유는 방어 감산이 타격마다 걸리는 실제 동작과 맞추기 위해서다.
        if (!skill.IsEmpty && skillBaseCooldown > 0f) {
            float interval = CombatMath.SkillInterval(skillBaseCooldown, stats.SkillCooldownReduction,
                                                      s.SkillCooldownFloorSeconds, stats.SkillAccel, s.SkillAccelSoftK);
            if (interval > 0f) {
                float perHit = Expected(attacker, dummy, skill.TotalMultiplier / skill.HitCount, s);
                dps += perHit * skill.HitCount * TargetCount(skill, cp) / interval;
            }
        }

        return dps;
    }

    /// <summary>기준 공격자를 상대로 한 실효 체력. 방어력/치명타저항/피격데미지감소/회피가 여기로 모인다.</summary>
    private static float EffectiveHealth(in CombatStats stats, BattleSettings s, CombatPowerSettings cp) {
        float health = Mathf.Max(0f, stats.Health);
        if (health <= 0f) return 0f;

        var attacker = ReferenceAttacker(stats, cp);
        var naked = new CombatStats();
        float baseline = Expected(attacker, naked, 1f, s);
        if (baseline <= 0f) return health;

        // 방어력을 뺀 상태의 받는 피해 비율(치명타저항/피격데미지감소만 반영). 기준 공격자의 공격력과 무관하다.
        var withoutDefense = stats;
        withoutDefense.Defense = 0f;
        float ratio = Expected(attacker, withoutDefense, 1f, s) / baseline;

        // 방어력은 기준 공격력 대비 비율로 점근 평가한다(뺄셈처럼 특정 지점에서 가치가 급변하지 않는다).
        float defenseShare = Mathf.Max(0f, stats.Defense) * Mathf.Max(0f, s.DefenseFactor) / attacker.Attack;
        ratio /= 1f + Mathf.Max(0f, cp.DefenseWeight) * defenseShare;
        if (ratio <= 0f) return health;

        float ehp = health / ratio;

        // 상태이상은 미구현이라 실제 피해로 환산할 수 없다. 죽은 스탯이 되지 않도록 축소 가중치로만 반영한다.
        ehp *= 1f + stats.StatusResist01(s.ResistSoftK) * Mathf.Max(0f, cp.StatusResistWeight);

        // 회피는 받는 피해의 기대값을 (1 - 확률) 배로 줄이므로 실효 체력이 그 역수만큼 늘어난다.
        ehp /= Mathf.Max(0.0001f, 1f - stats.EvasionChance01);

        return ehp;
    }

    // 화력 측정용 기준 더미. 방어력만 갖고(방어관통의 가치를 재기 위해) 나머지는 0 이다.
    // 공격력에만 연동해야 체력/방어 성장이 화력 측정값을 깎지 않는다.
    private static CombatStats ReferenceTarget(in CombatStats stats, CombatPowerSettings cp) => new CombatStats {
        Defense = Mathf.Max(Mathf.Max(0.0001f, cp.MinReferenceAttack), stats.Attack) * Mathf.Max(0f, cp.ReferenceDefenseRatio)
    };

    // 생존력 측정용 기준 공격자. 치명타를 갖고 있어야 치명타저항 스탯이 값을 가진다.
    private static CombatStats ReferenceAttacker(in CombatStats stats, CombatPowerSettings cp) => new CombatStats {
        Attack = Mathf.Max(0.0001f, TierAttack(stats, cp) * Mathf.Max(0f, cp.ReferenceAttackRatio)),
        CritRate = Mathf.Max(0f, cp.ReferenceCritRate),
        CritDamage = Mathf.Max(0f, cp.ReferenceCritDamage)
    };

    // 평가 대상이 속한 컨텐츠 티어의 기준 공격력(= 이 캐릭터가 상대할 적의 스케일).
    // 공격력과 체력의 기하평균이라 어느 쪽이 1% 올라도 티어는 0.5% 만 오른다(단조성 규칙의 근거).
    private static float TierAttack(in CombatStats stats, CombatPowerSettings cp) {
        float floor = Mathf.Max(0.0001f, cp.MinReferenceAttack);
        float perAttack = Mathf.Max(0.0001f, cp.TierHealthPerAttack);
        float offense = Mathf.Max(floor, stats.Attack);
        float defense = Mathf.Max(floor, stats.Health / perAttack);
        return Mathf.Sqrt(offense * defense);
    }

    // 광역 스킬이 실제로 맞히는 대상 수(기준 적 수로 상한). MaxTargets 0 이하는 "적 전체".
    private static float TargetCount(in SkillPowerProfile skill, CombatPowerSettings cp) {
        float reference = Mathf.Max(1f, cp.ReferenceEnemyCount);
        return skill.MaxTargets <= 0 ? reference : Mathf.Min(skill.MaxTargets, reference);
    }

    // 전역 튜닝값을 매번 풀어 넘기지 않도록 감싼 것. 실제 전투와 완전히 같은 파이프라인을 탄다.
    private static float Expected(in CombatStats attacker, in CombatStats defender, float multiplier, BattleSettings s) =>
        CombatMath.ExpectedDamage(attacker, defender, multiplier,
                                  s.DefenseFactor, s.DamageJitterPercent, s.ResistSoftK);
}
