using System;
using UnityEngine;

/// <summary>
/// 전투력 산출 튜닝값. BattleSettings 에 묶여 인스펙터에서 조정한다.
/// 전투 밸런스에는 전혀 영향을 주지 않으며, 오직 "표시되는 전투력 숫자"만 바꾼다.
/// </summary>
[Serializable]
public sealed class CombatPowerSettings {
    [Header("Scale")]
    [Tooltip("최종 배수(표시 자릿수 조정용). 성장 구간별 비율은 그대로 두고 숫자 크기만 바뀐다. "
           + "기본값 기준 신규 캐릭터가 약 90~100 이 되도록 잡혀 있다.")]
    public float Scale = 30f;
    [Tooltip("공격 비중(0~1). 전투력 = DPS^이값 x 실효체력^(1-이값). "
           + "0.5 면 기하평균이라 DPS 와 실효체력이 둘 다 2배일 때 전투력도 정확히 2배가 된다. "
           + "올리면 딜러형이, 내리면 탱커형이 높게 평가된다.")]
    [Range(0f, 1f)] public float OffenseWeight = 0.5f;

    [Header("Reference Target (기준 더미: 화력 측정용)")]
    [Tooltip("기준 더미의 방어력 = 평가 대상의 공격력 x 이 값. 방어관통의 가치를 재는 기준이다. "
           + "고정 방어력을 쓰지 않는 이유는 몬스터 방어력이 플레이어 성장과 함께 자라기 때문이다"
           + "(실제 데이터: 중반 DEF 10 / ATK 60 = 0.17, 최종 DEF 30 / ATK 330 = 0.09).")]
    public float ReferenceDefenseRatio = 0.15f;
    [Tooltip("광역 스킬 평가에 쓰는 기준 적 수. 적 전체 스킬은 이 수만큼 맞히는 것으로 계산한다. "
           + "올리면 광역 캐릭터의 전투력이 높아진다. 단일 대상 컨텐츠(레이드)까지 감안한 평균값으로 잡을 것.")]
    public float ReferenceEnemyCount = 3f;

    [Header("Reference Attacker (기준 공격자: 생존력 측정용)")]
    [Tooltip("기준 공격자의 공격력 = 평가 대상의 컨텐츠 티어 지표 x 이 값. '자기 티어의 적에게 맞는다'는 가정이다. "
           + "고정값을 쓰면 저레벨에서 방어력이 무의미해지고 고레벨에서 무한대 실효체력이 나온다.")]
    public float ReferenceAttackRatio = 1f;
    [Tooltip("기준 공격자 공격력의 하한. 무장비 저스펙 캐릭터에서 0 나눗셈과 방어력 과대평가를 막는다.")]
    public float MinReferenceAttack = 5f;
    [Tooltip("컨텐츠 티어 지표를 낼 때 체력을 공격력 스케일로 환산하는 나눗수. "
           + "장비 1포인트가 주는 체력/공격력 비율(EquipmentStatLayout.HealthPerPoint)과 맞춰 둔다. "
           + "티어 지표 = sqrt(공격력 x 체력/이값) 이며, 공격력만으로 잡으면 공격 스탯이 스스로를 깎는다"
           + "(공격력을 올리면 기준 공격자도 세져 실효체력이 내려감). 기하평균으로 공/방 양쪽을 대칭 처리한다.")]
    public float TierHealthPerAttack = 10f;
    [Tooltip("기준 공격자의 치명타 확률(%). 치명타저항 스탯의 가치를 재는 기준이다.")]
    public float ReferenceCritRate = 30f;
    [Tooltip("기준 공격자의 치명타 데미지(%p).")]
    public float ReferenceCritDamage = 100f;
    [Tooltip("받는 피해 비율의 하한(0~1). 실효체력 상한 = 체력 / 이 값. "
           + "방어력이 뺄셈이라 방어력이 기준 공격력에 근접하면 실효체력이 무한대로 발산하는데, 이 값이 그것을 막는다. "
           + "0.05 = 실효체력 최대 20배.")]
    [Range(0.01f, 1f)] public float MinDamageTakenRatio = 0.05f;

    [Header("Utility (DPS/실효체력에 들어가지 않는 스탯)")]
    [Tooltip("이동속도 가중치. 이동속도는 데미지도 생존력도 아니어서 전투력 = ... x (1 + 이속% x 이 값) 으로만 반영한다.")]
    public float MoveSpeedWeight = 0.1f;
    [Tooltip("상태이상저항 가중치. 상태이상이 미구현이라 실효체력에 축소 반영한다. "
           + "상태이상을 구현하면 이 근사를 실제 기여로 교체할 것.")]
    public float StatusResistWeight = 0.25f;
    [Tooltip("사거리 증가 가중치(월드 거리 1당). 전투력 = ... x (1 + 사거리증가분 x 이 값). "
           + "무기타입 고정 사거리는 캐릭터 정체성이라 넣지 않고, 수련 등으로 '키워서 얻은 증가분'만 반영한다. "
           + "사거리는 데미지도 생존력도 아니지만 먼저 때리고 덜 맞는 실질 이득이 있어 이동속도와 같은 방식으로 다룬다.")]
    public float RangeBonusWeight = 0.05f;
}

/// <summary>
/// 표시용 전투력 산출기. CombatMath 와 같은 위상의 순수 정적 계산기로, 상태를 갖지 않고 통신도 하지 않는다.
///
/// 설계 의도: 전투력을 임의의 가중치 표가 아니라 "게임의 실제 전투 공식"에서 유도한다.
///  1) 화력   = 기준 더미를 상대로 한 초당 기대 피해(기본공격 + 고유 스킬)
///  2) 생존력 = 기준 공격자를 상대로 한 실효 체력(체력 / 받는 피해 비율)
///  3) 전투력 = Scale x 화력^w x 생존력^(1-w) x 유틸 보정
///
/// 기하평균으로 묶는 이유: 한쪽만 극단적으로 올린 빌드가 과대평가되지 않고,
/// 둘 다 2배가 되면 전투력도 정확히 2배가 되어(선형) 수치 인플레가 생기지 않는다.
///
/// 데미지 계산은 CombatMath.ExpectedDamage(= ResolveDamage 의 기대값 버전)를 그대로 쓴다.
/// 따라서 데미지 공식이나 스탯이 바뀌면 전투력도 자동으로 따라오며, 이 파일을 고칠 필요가 없다.
/// 무기타입 고정 사거리는 캐릭터 정체성이라 전투력에 넣지 않고, 키워서 얻은 사거리 증가분만 유틸 보정으로 반영한다.
/// </summary>
public static class CombatPowerFormula {
    /// <summary>
    /// 최종 스탯 + 고유 스킬로 표시용 전투력을 계산한다.
    /// skill 이 SkillPowerProfile.None 이거나 쿨타임이 0 이하면 기본 공격만으로 화력을 잡는다.
    /// </summary>
    /// <param name="rangeBonus">무기타입 고정 사거리 위에 얹힌 사거리 증가분(CharacterRuntime.RangeBonus).</param>
    public static float Evaluate(in CombatStats stats, in SkillPowerProfile skill, float skillBaseCooldown,
                                 BattleSettings settings, float rangeBonus = 0f) {
        if (settings == null) return 0f;
        var cp = settings.CombatPower;
        if (cp == null) return 0f;

        float dps = DamagePerSecond(stats, skill, skillBaseCooldown, settings, cp);
        float ehp = EffectiveHealth(stats, settings, cp);
        if (dps <= 0f || ehp <= 0f) return 0f;

        float w = Mathf.Clamp01(cp.OffenseWeight);
        float power = Mathf.Pow(dps, w) * Mathf.Pow(ehp, 1f - w);
        power *= 1f + Mathf.Max(0f, stats.MoveSpeed) / 100f * Mathf.Max(0f, cp.MoveSpeedWeight);
        power *= 1f + Mathf.Max(0f, rangeBonus) * Mathf.Max(0f, cp.RangeBonusWeight);

        return Mathf.Max(0f, cp.Scale * power);
    }

    /// <summary>기준 더미를 상대로 한 초당 기대 피해(기본 공격 + 고유 스킬).</summary>
    private static float DamagePerSecond(in CombatStats stats, in SkillPowerProfile skill, float skillBaseCooldown,
                                         BattleSettings s, CombatPowerSettings cp) {
        var dummy = ReferenceTarget(stats, cp);

        // 기본 공격: 배수 1 로 공속만큼 반복. 공속 소프트캡 설정도 실제 전투와 동일하게 통과시킨다.
        float attacksPerSecond = Mathf.Max(0f, s.BaseAttacksPerSecond) * CombatMath.AttackSpeedMultiplier(
            stats.AttackSpeed, s.EnableAttackSpeedSoftCap, s.AttackSpeedSoftCapStart, s.AttackSpeedSoftK);
        // 2회 공격은 기본공격만 한 번 더 나가므로(스킬은 대상이 아니다) 기본공격 화력에만 기대 타수를 곱한다.
        float dps = Expected(stats, dummy, 1f, s) * attacksPerSecond * (1f + stats.DoubleAttackChance01);

        // 고유 스킬: 발동 주기당 (타격 수 x 대상 수 x 1타 기대피해).
        // 총배율을 타격 수로 나눠 넣는 이유는 방어 감산이 타격마다 걸리는 실제 동작과 맞추기 위해서다.
        if (!skill.IsEmpty && skillBaseCooldown > 0f) {
            float interval = CombatMath.SkillInterval(skillBaseCooldown, stats.SkillCooldownReduction,
                                                      s.SkillCooldownFloorSeconds, stats.SkillAccel, s.SkillAccelSoftK);
            if (interval > 0f) {
                float perHit = Expected(stats, dummy, skill.TotalMultiplier / skill.HitCount, s);
                dps += perHit * skill.HitCount * TargetCount(skill, cp) / interval;
            }
        }

        return dps;
    }

    /// <summary>기준 공격자를 상대로 한 실효 체력. 방어력/치명타저항/피격데미지감소가 여기로 모인다.</summary>
    private static float EffectiveHealth(in CombatStats stats, BattleSettings s, CombatPowerSettings cp) {
        float health = Mathf.Max(0f, stats.Health);
        if (health <= 0f) return 0f;

        var attacker = ReferenceAttacker(stats, cp);

        // 방어 스탯이 전혀 없는 상태(baseline) 대비 실제로 얼마나 덜 맞는지의 비율.
        // 비율이 1 이면 실효 체력 = 체력 그대로이며, 0.25 면 4배가 된다.
        var naked = new CombatStats();
        float baseline = Expected(attacker, naked, 1f, s);
        if (baseline <= 0f) return health;

        float taken = Expected(attacker, stats, 1f, s);
        float ratio = Mathf.Max(Mathf.Clamp01(cp.MinDamageTakenRatio), taken / baseline);

        float ehp = health / ratio;

        // 상태이상은 미구현이라 실제 피해로 환산할 수 없다. 죽은 스탯이 되지 않도록 축소 가중치로만 반영한다.
        ehp *= 1f + stats.StatusResist01(s.ResistSoftK) * Mathf.Max(0f, cp.StatusResistWeight);

        // 회피는 받는 피해의 기대값을 (1 - 확률) 배로 줄이므로 실효 체력이 그 역수만큼 늘어난다
        // (50% 회피 = 실효 체력 2배). 가중치를 두지 않는 이유는 이것이 근사가 아니라 정확한 환산이기 때문이다.
        ehp /= Mathf.Max(0.0001f, 1f - stats.EvasionChance01);

        return ehp;
    }

    // 화력 측정용 기준 더미. 방어력만 갖고(방어관통의 가치를 재기 위해) 나머지는 0 이다.
    private static CombatStats ReferenceTarget(in CombatStats stats, CombatPowerSettings cp) => new CombatStats {
        Defense = TierAttack(stats, cp) * Mathf.Max(0f, cp.ReferenceDefenseRatio)
    };

    // 생존력 측정용 기준 공격자. 치명타를 갖고 있어야 치명타저항 스탯이 값을 가진다.
    private static CombatStats ReferenceAttacker(in CombatStats stats, CombatPowerSettings cp) => new CombatStats {
        Attack = TierAttack(stats, cp) * Mathf.Max(0f, cp.ReferenceAttackRatio),
        CritRate = Mathf.Max(0f, cp.ReferenceCritRate),
        CritDamage = Mathf.Max(0f, cp.ReferenceCritDamage)
    };

    // 평가 대상이 속한 컨텐츠 티어의 기준 공격력(= 이 캐릭터가 상대할 적의 스케일).
    //
    // 공격력만으로 잡으면 안 된다. 기준 공격자가 자기 공격력을 따라 세지므로 공격 스탯을 올릴수록
    // 자기 실효체력이 내려가 공격 스탯이 스스로를 깎는다(측정치가 실제 강함보다 낮게 나온다).
    // 공격력과 체력의 기하평균을 쓰면 공/방 어느 쪽을 올려도 티어가 같은 비율로 움직여 대칭이 되고,
    // 두 스탯의 상대 평가가 실제 기여도와 일치한다.
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
