using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 연쇄(체인) 타격. 주 대상 1명을 때린 뒤, 그 대상 위치에서 가장 가까운 미타격 적으로 발사체가 튀어 이어진다(감전/번개 사슬).
/// 확산형(PrimaryPlusSplashSkill)과 달리 발사가 "시전자 -> 주 대상 -> 다음 대상 -> ..." 순으로 이전 대상 위치에서 이어진다.
/// 예) C1302 번개 사슬: 주 100% + 이후 최대 2회 각 60% (총 220%).
/// </summary>
[CreateAssetMenu(fileName = "ChainBounceSkill", menuName = "Anaka/Skills/Chain Bounce")]
public sealed class ChainBounceSkill : DamageSkill {
    [Header("Chain Bounce")]
    [Tooltip("주 대상 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float primaryMultiplier = 1f;
    [Tooltip("연쇄 대상 각각의 공격력 배수(1.0 = 100%). 모든 연쇄에 동일 적용.")]
    [SerializeField] private float chainMultiplier = 0.6f;
    [Tooltip("주 대상 이후 이어지는 연쇄 횟수(추가 대상 수).")]
    [SerializeField] private int maxChains = 2;
    [Tooltip("이전 대상에서 다음 대상까지 탐색 반경. 0 이하면 무제한(범위 내 아무 적에게나 튐).")]
    [SerializeField] private float chainRadius = 0f;
    [Tooltip("연쇄가 다음 대상으로 튀기까지 지연(초). 0이면 도달 즉시 다음으로 이어진다. 0보다 크면 사슬이 순차로 번지는 연출.")]
    [SerializeField] private float chainDelay = 0f;

    // 주 대상 1타 + 연쇄 maxChains 타. 연쇄는 서로 다른 적으로 튀지만 1회 발동으로 끝나므로 1세트로 신고한다.
    public override SkillPowerProfile PowerProfile {
        get {
            int chains = Mathf.Max(0, maxChains);
            return new SkillPowerProfile(primaryMultiplier + chainMultiplier * chains, 1 + chains, 1);
        }
    }

    protected override void OnExecute(SkillContext ctx) {
        var enemies = AliveEnemies(ctx);
        var primary = Nearest(ctx.Caster.Position, enemies);
        if (primary == null) return;

        var hit = new HashSet<ICombatUnit> { primary };
        HitFrom(ctx, ctx.CastOrigin, primary, primaryMultiplier, at => Bounce(ctx, at, hit, 0));
    }

    // fromPos(직전 대상 도달 지점)에서 가장 가까운 미타격 적으로 이어 발사한다. depth 가 maxChains 에 도달하면 종료.
    private void Bounce(SkillContext ctx, Vector2 fromPos, HashSet<ICombatUnit> hit, int depth) {
        if (depth >= maxChains) return;
        var next = NearestExcluding(fromPos, AliveEnemies(ctx), hit, chainRadius);
        if (next == null) return;
        hit.Add(next);

        void fire() => HitFrom(ctx, fromPos, next, chainMultiplier, at => Bounce(ctx, at, hit, depth + 1));
        if (chainDelay > 0f) ctx.Runtime.Schedule(chainDelay, fire);
        else fire();
    }
}
