using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터의 최종 런타임 스탯 조립기. 정적 데이터(CharacterData) + 서버 성장(CharacterProgress) + 튜닝(BattleSettings)
/// 을 합쳐 전투/표시에 쓰는 CombatStats 를 계산한다. 외부 요인(레벨/별/투자)이 바뀌면 다시 만들어 즉시 반영한다(PLANNING 3-3).
///
/// 합산 규칙:
///  - 기본: CharacterData.BaseStats
///  - 레벨: LevelGrowthPerLevel * (level - 1)
///  - 별레벨: StarGrowthTable 의 승급별 델타 누적(스탯별 성장, 균등 증가 아님)
///  - 투자: 각 투자 가능 스탯에 InvestedX * PointValue(stat)
///  - 사거리: 무기타입 고정값(BattleSettings.RangeFor)을 기준으로 삼고, 보정원이 준 사거리만 그 위에 더한다
/// (장비 보정은 equipmentBonus 로 더한다.)
/// </summary>
public sealed class CharacterRuntime {
    public readonly CharacterData Data;
    public readonly CharacterProgress Progress;
    public readonly CombatStats FinalStats;

    /// <summary>
    /// 무기타입 고정 사거리 위에 얹힌 사거리 증가분. 전투력 산출이 "정체성인 기본 사거리"는 빼고
    /// "키워서 얻은 사거리"만 반영하기 위해 따로 들고 있다(전투에는 FinalStats.Range 만 쓰인다).
    /// </summary>
    public readonly float RangeBonus;

    private readonly BattleSettings _settings;

    // 전투 중에만 켜지는 조건부 효과(각인). FinalStats 에는 넣지 않고(전투 유닛이 스스로 켜고 끈다)
    // 전투력 산출에서만 가동률로 환산해 반영한다.
    private readonly IReadOnlyList<EngravingLoadout.Conditional> _conditionals;

    /// <param name="conditionals">조건부 효과. 전투력 표시에만 쓰이며 FinalStats 에는 영향이 없다.</param>
    public CharacterRuntime(CharacterData data, CharacterProgress progress, BattleSettings settings, CombatStats equipmentBonus = default,
                            IReadOnlyList<EngravingLoadout.Conditional> conditionals = null) {
        Data = data;
        Progress = progress;
        _settings = settings;
        _conditionals = conditionals;
        var composed = Compose(data, progress, settings) + equipmentBonus;
        // 사거리의 기준은 성장/투자와 무관한 무기타입 고정값이다. 다만 보정원이 명시적으로 준 사거리
        // (수련의 사거리 효과)는 기준을 덮어써 없애지 않고 그 위에 더한다.
        RangeBonus = Mathf.Max(0f, equipmentBonus.Range);
        composed.Range = settings.RangeFor(data.WeaponType) + RangeBonus;
        FinalStats = composed;
    }

    private static CombatStats Compose(CharacterData data, CharacterProgress p, BattleSettings s) {
        var stats = data.BaseStats;

        // 레벨 성장
        int levelSteps = Mathf.Max(0, p.Level - 1);
        if (levelSteps > 0) stats += s.LevelGrowthPerLevel * levelSteps;

        // 별레벨 성장(스탯별 성장표 누적)
        int star = Mathf.Max(1, p.StarLevel);
        if (s.StarGrowthTable != null) stats += s.StarGrowthTable.CumulativeTo(star);

        // 스탯포인트 투자(투자 가능 스탯만, 스탯별 포인트 가치 적용)
        stats.Attack      += p.InvestedAttack      * s.PointValue(StatType.Attack);
        stats.Defense     += p.InvestedDefense     * s.PointValue(StatType.Defense);
        stats.Health      += p.InvestedHealth      * s.PointValue(StatType.Health);
        stats.AttackSpeed += p.InvestedAttackSpeed * s.PointValue(StatType.AttackSpeed);
        stats.MoveSpeed   += p.InvestedMoveSpeed   * s.PointValue(StatType.MoveSpeed);
        stats.SkillAccel  += p.InvestedSkillAccel  * s.PointValue(StatType.SkillAccel);

        // 한계돌파 조각 보너스(한계돌파 전이면 0). 성장치에서 나오는 값이라 여기서 함께 더한다.
        stats += LimitBreakRules.ShardBonus(p);

        // 사거리는 생성자에서 무기타입 고정값으로 최종 확정한다(여기서는 건드리지 않음).
        return stats;
    }

    /// <summary>
    /// 표시용 전투력. 최종 스탯과 고유 스킬 화력을 실제 전투 공식에 넣어 산출한다(CombatPowerFormula).
    /// 스킬이 없는 캐릭터는 기본 공격만으로 평가된다. 조건부 효과는 가동률만큼 환산해 더한다.
    /// 한계돌파한 캐릭터는 실제 전투와 같이 한계돌파 스킬로 평가한다.
    /// </summary>
    public float CombatPower {
        get {
            var skill = Data != null ? Data.SkillFor(Progress != null && Progress.LimitBroken) : null;
            return CombatPowerFormula.Evaluate(
                FinalStats,
                skill != null ? skill.PowerProfile : SkillPowerProfile.None,
                skill != null ? skill.BaseCooldown : 0f,
                _settings,
                RangeBonus,
                _conditionals);
        }
    }
}
