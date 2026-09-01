using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 단일 대상(가장 가까운 적) 공격. hitCount 회 연속 타격 후, finisher 배수가 있으면 마지막 일격을 추가로 넣는다.
/// 예) 파워 슬래시(200%,1) / 더블 애로우(110%,2) / 버스트 샷(70%,3) / 러시 펀치(60%,4) / 비스트 콤보(100%,4 + 120%).
/// </summary>
[CreateAssetMenu(fileName = "SingleTargetSkill", menuName = "Anaka/Skills/Single Target")]
public sealed class SingleTargetSkill : DamageSkill {
    [Header("Single Target")]
    [Tooltip("각 타격의 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float damageMultiplier = 2f;
    [Tooltip("연속 타격 횟수.")]
    [SerializeField] private int hitCount = 1;
    [Tooltip("마지막에 추가로 넣는 피니셔 배수. 0 이면 없음.")]
    [SerializeField] private float finisherMultiplier = 0f;

    // 대상 1명에게 (배수 x 타격수) + 피니셔.
    public override SkillPowerProfile PowerProfile {
        get {
            int hits = Mathf.Max(1, hitCount);
            bool hasFinisher = finisherMultiplier > 0f;
            return new SkillPowerProfile(damageMultiplier * hits + finisherMultiplier,
                                         hits + (hasFinisher ? 1 : 0), 1);
        }
    }

    protected override void OnExecute(SkillContext ctx) {
        var target = Nearest(ctx.Caster.Position, AliveEnemies(ctx));
        if (target == null) return;

        HitRepeated(ctx, target, damageMultiplier, Mathf.Max(1, hitCount));
        if (finisherMultiplier > 0f && target.IsAlive) Hit(ctx, target, finisherMultiplier);
    }
}
