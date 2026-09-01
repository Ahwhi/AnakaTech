using UnityEngine;
using static Enums;

/// <summary>
/// 근접 기본공격: 대상에게 즉시 데미지 + 시전자 연출(self) + 피격 임팩트(target) + 공격 사운드.
/// 현재 기본 전투의 동작을 그대로 재현하며, 연출/사운드는 이 SO 인스턴스에 데이터로 지정해 캐릭터별로 달리한다.
/// </summary>
[CreateAssetMenu(fileName = "MeleeAttack", menuName = "Anaka/Attacks/Melee")]
public sealed class MeleeAttack : BasicAttack {
    [Tooltip("공격력에 곱할 데미지 배수(기본공격은 보통 1).")]
    [SerializeField] private float damageMultiplier = 1f;
    [Tooltip("시전자 쪽 연출 VFX(휘두름/머즐 등). 시전자 머즐 지점에 재생. 선택.")]
    [SerializeField] private GameObject selfVfxPrefab;
    [Tooltip("피격 지점 임팩트 VFX. 대상 지점에 재생. 선택.")]
    [SerializeField] private GameObject targetVfxPrefab;
    [Tooltip("공격 사운드. 비우면 공통 기본 공격음(SfxKey.Attack)을 사용.")]
    [SerializeField] private AudioClip attackClip;
    [Tooltip("VFX 를 공격 방향(시전자->대상)으로 회전시켜 재생할지. 방향성 있는 휘두름/베기 연출이면 켠다. 사방 대칭/오라형이면 끈다.")]
    [SerializeField] private bool orientVfxToTarget = true;

    public override void Execute(AttackContext ctx) {
        ctx.DealDamage(damageMultiplier * ctx.Multiplier);
        // 타격(임팩트) VFX 는 시전자 상태(변신 등)의 오버라이드가 있으면 그걸 쓴다.
        var targetVfx = ctx.ResolveImpactVfx(targetVfxPrefab);
        if (orientVfxToTarget) {
            if (selfVfxPrefab != null) ctx.PlayVfxToward(selfVfxPrefab, ctx.CasterPoint);
            if (targetVfx != null) ctx.PlayVfxToward(targetVfx, ctx.TargetPoint);
        } else {
            if (selfVfxPrefab != null) ctx.PlayVfx(selfVfxPrefab, ctx.CasterPoint);
            if (targetVfx != null) ctx.PlayVfx(targetVfx, ctx.TargetPoint);
        }
        if (attackClip != null) ctx.PlaySfx(attackClip);
        else ctx.PlaySfx(SfxKey.Attack);
    }
}
