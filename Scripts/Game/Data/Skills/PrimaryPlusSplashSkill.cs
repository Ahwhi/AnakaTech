using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주 대상 + 확산. 가장 가까운 적에게 primary 배수를 주고, 그 대상 주변의 가까운 적 최대 maxSplashTargets 명에게 splash 배수를 준다.
/// 발사체를 쓰면 주 대상에게 "한 발"만 날아가고, 도달하는 순간 주 대상 데미지 + 주변 확산이 함께 일어난다(확산은 별도 발사체가 아님).
/// 예) 호크 스트라이크(매 1마리가 주 대상에 명중 -> 주 240% + 주변 2명 90%).
/// </summary>
[CreateAssetMenu(fileName = "PrimaryPlusSplashSkill", menuName = "Anaka/Skills/Primary + Splash")]
public sealed class PrimaryPlusSplashSkill : DamageSkill {
    [Header("Primary + Splash")]
    [Tooltip("주 대상 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float primaryMultiplier = 1f;
    [Tooltip("확산 대상 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float splashMultiplier = 0.6f;
    [Tooltip("확산 대상 최대 수(주 대상 제외).")]
    [SerializeField] private int maxSplashTargets = 2;
    [Tooltip("확산 반경(월드 units). 주 대상 도달 지점에서 이 반경 이내의 적만 확산 대상. 0 이하면 거리 제한 없이 가까운 순으로 채운다.")]
    [SerializeField] private float splashRadius = 3f;

    // 주 대상 1타 + 주변 확산 maxSplashTargets 타(서로 다른 적, 1회 발동으로 끝나므로 1세트).
    public override SkillPowerProfile PowerProfile {
        get {
            int splash = splashMultiplier > 0f ? Mathf.Max(0, maxSplashTargets) : 0;
            return new SkillPowerProfile(primaryMultiplier + splashMultiplier * splash, 1 + splash, 1);
        }
    }

    protected override void OnExecute(SkillContext ctx) {
        var enemies = AliveEnemies(ctx);
        var primary = Nearest(ctx.Caster.Position, enemies);
        if (primary == null) return;
        enemies.Remove(primary);

        // 주 대상에게 발사체 1발(즉발이면 즉시) -> 도달 시 주 데미지 적용 후, 그 지점 반경 내 주변으로 확산.
        HitFrom(ctx, ctx.CastOrigin, primary, primaryMultiplier, at => {
            var splash = new List<ICombatUnit>();
            TakeNearestWithin(at, enemies, splashRadius, maxSplashTargets, splash);
            for (int i = 0; i < splash.Count; i++) Strike(ctx, splash[i], splashMultiplier, splash[i].Position);
        });
    }
}
