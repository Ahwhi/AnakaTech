using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 낙하 범위 공격(화살비/포화 등). 중심점 기준 가까운 적 최대 maxTargets 명을 골라, 각 대상의 상공(spawnOffset + 무작위 흩뿌림)에서
/// 발사체가 대상으로 떨어져 각각 배수 데미지를 준다. 발사체가 시전자가 아니라 대상 부근에서 생성되는 것이 SingleTarget/Aoe 와의 차이다.
/// 예) C2101 스타폴 애로우: 상공에서 화살비 -> 범위 내 최대 5명 각 80% (총 400%).
/// </summary>
[CreateAssetMenu(fileName = "AreaRainSkill", menuName = "Anaka/Skills/Area Rain")]
public sealed class AreaRainSkill : DamageSkill {
    public enum RainOrigin { Caster, NearestEnemy, DensestCluster }

    [Header("Area Rain")]
    [Tooltip("각 대상에게 주는 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float damageMultiplier = 0.8f;
    [Tooltip("최대 타격 대상 수.")]
    [SerializeField] private int maxTargets = 5;
    [Tooltip("타격 대상 선정 중심(가까운 순으로 maxTargets 명을 고른다).")]
    [SerializeField] private RainOrigin origin = RainOrigin.DensestCluster;
    [Tooltip("DensestCluster 중심 계산에 쓰는 반경(월드 units).")]
    [SerializeField] private float clusterRadius = 3f;

    [Header("Rain Drop")]
    [Tooltip("각 대상 기준 발사체 생성 오프셋(대상 위치 + 이 값에서 대상으로 떨어진다). 보통 (0, 양수) = 상공.")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 8f);
    [Tooltip("발사체 생성 지점에 줄 무작위 가로 흔들림(하늘에서 흩뿌리는 느낌). 0이면 정확히 오프셋 위치에서 떨어진다.")]
    [SerializeField] private float spawnJitterX = 1.5f;
    [Tooltip("대상마다 낙하 시작을 벌리는 간격(초). 0이면 동시에 쏟아진다. 서로 다른 대상 간 스태거(같은 대상 다단 아님).")]
    [SerializeField] private float perTargetInterval = 0.06f;

    // 최대 maxTargets 명 각각에게 1발씩.
    public override SkillPowerProfile PowerProfile => new SkillPowerProfile(damageMultiplier, 1, maxTargets);

    protected override void OnExecute(SkillContext ctx) {
        var enemies = AliveEnemies(ctx);
        if (enemies.Count == 0) return;

        Vector2 center;
        switch (origin) {
            case RainOrigin.Caster: center = ctx.Caster.Position; break;
            case RainOrigin.DensestCluster: center = DensestCenter(enemies, clusterRadius); break;
            default: center = Nearest(ctx.Caster.Position, enemies).Position; break;
        }

        var targets = new List<ICombatUnit>();
        TakeNearest(center, enemies, maxTargets, targets);

        for (int i = 0; i < targets.Count; i++) {
            var target = targets[i];
            float delay = i * perTargetInterval;
            if (delay <= 0f) RainOn(ctx, target);
            else ctx.Runtime.Schedule(delay, () => RainOn(ctx, target)); // 스태거(전투 재시작 시 자동 취소)
        }
    }

    // 대상 상공(+가로 지터)에서 대상으로 발사체를 떨어뜨려 도달 시 데미지/임팩트를 준다. 발사체 미할당이면 즉발(대상 위치 적용).
    private void RainOn(SkillContext ctx, ICombatUnit target) {
        if (!target.IsTargetable()) return;
        Vector2 jitter = spawnJitterX > 0f ? new Vector2(Random.Range(-spawnJitterX, spawnJitterX), 0f) : Vector2.zero;
        Vector2 from = (Vector2)target.Position + spawnOffset + jitter;
        HitFrom(ctx, from, target, damageMultiplier);
    }
}
