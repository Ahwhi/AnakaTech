using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 투척/낙하 폭발. 투척체 1발을 중심 지점으로 보내고(시전자에서 던지거나 상공에서 떨어뜨림), 도달하면 그 주변 적에게 광역 데미지를 준다.
/// 대상마다 발사체가 날아가는 AreaRain 과 달리, 발사체는 "하나"이고 도착 지점에서 폭발하는 것이 특징이다.
/// 예) 플라즈마 그레네이드(시전자 투척 -> 밀집 5명 90%) / 메테오(상공 낙하 -> 8명 180%) / 폴른 저지먼트(상공 낙하 -> 적 전체 320%).
///
/// 투척체가 겨냥한 적이 도착 전에 죽어도 그 자리에서 폭발한다. 이 스킬의 효과는 겨냥한 적 개인이 아니라
/// "도착 지점"에 걸리는 것이라, 그 적이 먼저 쓰러졌다고 폭발이 사라지면 주변에 적이 몰려 있어도
/// 스킬 한 번을 허공에 버리게 된다. 누구를 때릴지는 아래 도착 콜백이 그 시점에 다시 고른다.
/// </summary>
[CreateAssetMenu(fileName = "LobAoeSkill", menuName = "Anaka/Skills/Lob AoE")]
public sealed class LobAoeSkill : DamageSkill {
    public enum ImpactCenter { Caster, NearestEnemy, DensestCluster }
    public enum LobSource { FromCaster, FromSky }

    [Header("Lob AoE")]
    [Tooltip("폭발에 맞는 각 대상의 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float damageMultiplier = 0.9f;
    [Tooltip("폭발 대상 수(중심에 가까운 순). hitAllEnemies 가 켜져 있으면 무시된다.")]
    [SerializeField] private int maxTargets = 5;
    [Tooltip("폭발(도달 지점) 반경(월드 units). 이 반경 이내의 적만 맞는다. 0 이하면 거리 제한 없이 가까운 순으로 maxTargets 를 채운다. hitAllEnemies 면 무시.")]
    [SerializeField] private float blastRadius = 2.5f;
    [Tooltip("체크 시 폭발이 적 전체에 적용된다(maxTargets/blastRadius 무시).")]
    [SerializeField] private bool hitAllEnemies = false;
    [Tooltip("폭발 중심 선정: Caster(자기 주변) / NearestEnemy(가장 가까운 적) / DensestCluster(적이 가장 많은 위치). 실제 폭발 반경은 blastRadius 다.")]
    [SerializeField] private ImpactCenter center = ImpactCenter.DensestCluster;
    [Tooltip("DensestCluster '중심점'을 찾을 때만 쓰는 밀집 판정 반경(월드 units). 폭발 반경이 아니다(그건 blastRadius).")]
    [SerializeField] private float clusterRadius = 3f;

    [Header("Lob Projectile")]
    [Tooltip("투척체 출발 지점: FromCaster(시전자에서 던짐) / FromSky(중심 상공에서 떨어짐). 발사체 프리팹을 비우면 투척 없이 즉시 폭발한다.")]
    [SerializeField] private LobSource source = LobSource.FromCaster;
    [Tooltip("FromSky 일 때 중심 위로 얼마나 높은 곳에서 떨어질지(월드 units).")]
    [SerializeField] private float skyHeight = 10f;

    // 폭발 1회가 대상들에게 1타씩. 전체 타격 옵션이 켜져 있으면 MaxTargets 0(전체)으로 신고한다.
    public override SkillPowerProfile PowerProfile =>
        new SkillPowerProfile(damageMultiplier, 1, hitAllEnemies ? 0 : maxTargets);

    protected override void OnExecute(SkillContext ctx) {
        var enemies = AliveEnemies(ctx);
        if (enemies.Count == 0) return;

        Vector2 impact;
        switch (center) {
            case ImpactCenter.Caster: impact = ctx.Caster.Position; break;
            case ImpactCenter.DensestCluster: impact = DensestCenter(enemies, clusterRadius); break;
            default: impact = Nearest(ctx.Caster.Position, enemies).Position; break;
        }

        // 투척체가 유도될 대표 대상(중심에 가장 가까운 적). 도달 지점 = 이 근처에서 폭발.
        var anchor = Nearest(impact, enemies);
        if (anchor == null) return;

        Vector2 from = source == LobSource.FromSky ? impact + Vector2.up * skyHeight : (Vector2)ctx.CastOrigin;

        // 도달 시점(at)에 그 지점 기준 blastRadius 반경 내 적을 골라 폭발시킨다(착탄 위치/현재 위치 반영).
        // 겨냥한 적(anchor)이 그 사이 죽었을 수 있으므로 여기서 생존 목록을 다시 받는다.
        // at 은 그 적이 마지막으로 서 있던 자리이고, 남은 적이 없으면 아무도 맞지 않는다.
        LaunchArc(ctx, from, anchor, at => {
            var alive = AliveEnemies(ctx);
            var targets = new List<ICombatUnit>();
            if (hitAllEnemies) targets.AddRange(alive);
            else TakeNearestWithin(at, alive, blastRadius, maxTargets, targets);
            for (int i = 0; i < targets.Count; i++) Strike(ctx, targets[i], damageMultiplier, targets[i].Position);
        });
    }
}
