using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2연격 광역. 가까운 적 최대 maxTargets 명에게 1파(firstMultiplier) 후 2파(secondMultiplier)를 준다.
/// 설명상 "잠시 후 다시" 같은 시간차 연출은 현재 즉시 2연타로 처리한다(데미지 총합/대상 동일).
/// 예) 벚꽃일섬(최대 5명 120% + 다시 180%).
/// </summary>
[CreateAssetMenu(fileName = "TwoWaveAoeSkill", menuName = "Anaka/Skills/Two-Wave AoE")]
public sealed class TwoWaveAoeSkill : DamageSkill {
    [Header("Two-Wave AoE")]
    [Tooltip("1파 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float firstMultiplier = 1.2f;
    [Tooltip("2파 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float secondMultiplier = 1.8f;
    [Tooltip("최대 타격 대상 수.")]
    [SerializeField] private int maxTargets = 5;

    // 최대 maxTargets 명 각각에게 1파 + 2파(총 2타격).
    public override SkillPowerProfile PowerProfile =>
        new SkillPowerProfile(firstMultiplier + secondMultiplier, 2, maxTargets);

    protected override void OnExecute(SkillContext ctx) {
        var enemies = AliveEnemies(ctx);
        if (enemies.Count == 0) return;

        Vector2 center = Nearest(ctx.Caster.Position, enemies).Position;
        var targets = new List<ICombatUnit>();
        TakeNearest(center, enemies, maxTargets, targets);

        for (int i = 0; i < targets.Count; i++)
            HitSequence(ctx, targets[i], firstMultiplier, secondMultiplier);
    }
}
