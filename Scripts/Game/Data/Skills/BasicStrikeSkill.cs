using UnityEngine;

/// <summary>
/// 기본 제공 스킬 예시: 가장 가까운 적에게 강타(공격력 배수) + 치명타 판정.
/// 유닛별 고유 스킬 제작 시 이 파일을 참고해 UnitSkill 을 상속하면 된다.
/// </summary>
[CreateAssetMenu(fileName = "BasicStrikeSkill", menuName = "Anaka/Skills/Basic Strike")]
public sealed class BasicStrikeSkill : UnitSkill {
    [Tooltip("공격력에 곱해지는 스킬 데미지 배수.")]
    [SerializeField] private float damageMultiplier = 2.5f;

    // 가장 가까운 적 1명에게 1타.
    public override SkillPowerProfile PowerProfile => new SkillPowerProfile(damageMultiplier, 1, 1);

    protected override void OnExecute(SkillContext ctx) {
        var caster = ctx.Caster;
        var enemies = ctx.Runtime.Enemies(caster);

        ICombatUnit target = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < enemies.Count; i++) {
            var e = enemies[i];
            if (!e.IsTargetable()) continue;
            float sqr = ((Vector2)e.Position - caster.Position).sqrMagnitude;
            if (sqr < bestSqr) {
                bestSqr = sqr;
                target = e;
            }
        }

        if (target == null) return;

        target.TakeDamage(caster.Stats, damageMultiplier, caster, StatusEffects);
    }
}
