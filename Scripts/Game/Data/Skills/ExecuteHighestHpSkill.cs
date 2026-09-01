using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 처형 일격. 현재 체력이 가장 높은 적에게 primary 배수를 주고, 그 일격으로 처치되면 주변 최대 maxSplashTargets 명에게 splash 배수를 준다.
/// 예) 팬텀 샷(가장 체력 높은 적 500%, 처치 시 주변 3명 120%). CurrentHp / IsAlive(처치 판정) 계약을 사용한다.
/// </summary>
[CreateAssetMenu(fileName = "ExecuteHighestHpSkill", menuName = "Anaka/Skills/Execute Highest HP")]
public sealed class ExecuteHighestHpSkill : DamageSkill {
    [Header("Execute (Highest HP)")]
    [Tooltip("주 대상 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float primaryMultiplier = 5f;
    [Tooltip("처치 시 주변 확산 배수(1.0 = 100%). 0 이면 확산 없음.")]
    [SerializeField] private float splashMultiplier = 1.2f;
    [Tooltip("처치 시 확산 최대 대상 수.")]
    [SerializeField] private int maxSplashTargets = 3;

    // 주 대상 1타 + 처치 시 확산. 확산은 처치 조건부라 항상 터지지는 않지만, 잡몹 처리가 주 용도인 스킬이므로
    // 설계 화력 그대로 신고한다(보스 단독전에서는 다소 후하게 평가된다).
    public override SkillPowerProfile PowerProfile {
        get {
            int splash = splashMultiplier > 0f ? Mathf.Max(0, maxSplashTargets) : 0;
            return new SkillPowerProfile(primaryMultiplier + splashMultiplier * splash, 1 + splash, 1);
        }
    }

    protected override void OnExecute(SkillContext ctx) {
        var enemies = AliveEnemies(ctx);
        if (enemies.Count == 0) return;

        // 현재 체력이 가장 높은 적 선정.
        ICombatUnit target = enemies[0];
        for (int i = 1; i < enemies.Count; i++)
            if (enemies[i].CurrentHp > target.CurrentHp) target = enemies[i];

        Hit(ctx, target, primaryMultiplier);

        // 처치 성공 시에만 확산.
        if (splashMultiplier > 0f && !target.IsAlive) {
            enemies.Remove(target);
            var splash = new List<ICombatUnit>();
            TakeNearest(target.Position, enemies, maxSplashTargets, splash);
            for (int i = 0; i < splash.Count; i++) Hit(ctx, splash[i], splashMultiplier);
        }
    }
}
