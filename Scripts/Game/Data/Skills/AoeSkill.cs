using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 광역 공격. 중심점 기준 가장 가까운 적 최대 maxTargets 명에게 각각 배수 데미지를 hitCount 회 준다.
/// 중심점: Caster(자기 주변) / NearestEnemy(가장 가까운 적 주변) / DensestCluster(적이 가장 많은 위치).
/// 예) 회전 베기(자기 주변 2명) / 확산 사격(가까운 적 주변 3명) / 아이스 노바(5명) / 메테오(8명) / 플라즈마(밀집 5명).
/// </summary>
[CreateAssetMenu(fileName = "AoeSkill", menuName = "Anaka/Skills/AoE")]
public sealed class AoeSkill : DamageSkill {
    public enum AoeOrigin { Caster, NearestEnemy, DensestCluster }

    [Header("AoE")]
    [Tooltip("각 대상에게 주는 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float damageMultiplier = 1f;
    [Tooltip("최대 타격 대상 수.")]
    [SerializeField] private int maxTargets = 3;
    [Tooltip("각 대상 타격 횟수.")]
    [SerializeField] private int hitCount = 1;
    [Tooltip("타격 대상 선정 중심.")]
    [SerializeField] private AoeOrigin origin = AoeOrigin.NearestEnemy;
    [Tooltip("DensestCluster 중심 계산에 쓰는 반경(월드 units).")]
    [SerializeField] private float clusterRadius = 3f;
    [Tooltip("대상마다 타격을 벌리는 간격(초). 0이면 전부 동시에 터진다. 0보다 크면 대상을 하나씩 순차로 때린다(연속 폭발 연출).")]
    [SerializeField] private float perTargetInterval = 0f;

    // 최대 maxTargets 명 각각에게 (배수 x 타격수).
    public override SkillPowerProfile PowerProfile {
        get {
            int hits = Mathf.Max(1, hitCount);
            return new SkillPowerProfile(damageMultiplier * hits, hits, maxTargets);
        }
    }

    protected override void OnExecute(SkillContext ctx) {
        var enemies = AliveEnemies(ctx);
        if (enemies.Count == 0) return;

        Vector2 center;
        switch (origin) {
            case AoeOrigin.Caster: center = ctx.Caster.Position; break;
            case AoeOrigin.DensestCluster: center = DensestCenter(enemies, clusterRadius); break;
            default: center = Nearest(ctx.Caster.Position, enemies).Position; break;
        }

        var targets = new List<ICombatUnit>();
        TakeNearest(center, enemies, maxTargets, targets);

        int hits = Mathf.Max(1, hitCount);
        for (int i = 0; i < targets.Count; i++) {
            var target = targets[i];
            float delay = i * perTargetInterval;
            if (delay <= 0f) HitRepeated(ctx, target, damageMultiplier, hits);
            else ctx.Runtime.Schedule(delay, () => HitRepeated(ctx, target, damageMultiplier, hits)); // 순차(전투 재시작 시 자동 취소)
        }
    }
}
