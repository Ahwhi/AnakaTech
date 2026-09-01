using UnityEngine;
using static Enums;

/// <summary>
/// 원거리 기본공격(활/총): 발사 순간 머즐 연출 + 발사음을 내고, 발사체를 쏜다.
/// 데미지/임팩트는 발사체가 대상에 "도달할 때" 적용되어 원거리 타격이 자연스럽다.
/// 궤적은 발사체 프리팹의 TrailRenderer 로 표현한다. 연출/사운드/발사체는 이 SO 인스턴스에 데이터로 지정한다.
/// (발사체 프리팹 미할당 시 즉시 명중으로 폴백해 오설정에도 데미지가 0 이 되지 않는다.)
/// </summary>
[CreateAssetMenu(fileName = "RangedAttack", menuName = "Anaka/Attacks/Ranged")]
public sealed class RangedAttack : BasicAttack {
    [Header("Damage")]
    [Tooltip("도달 시 데미지 배수(기본공격은 보통 1).")]
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Projectile")]
    [Tooltip("발사체 프리팹(Projectile 컴포넌트 + TrailRenderer 등 궤적).")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("발사체 이동 속도(월드 units/초). 0 이하면 즉시 명중.")]
    [SerializeField] private float projectileSpeed = 14f;
    [Tooltip("발사체를 진행 방향으로 회전시킬지(화살처럼). 총알/구체면 끈다.")]
    [SerializeField] private bool faceDirection = true;

    [Header("Fire (발사 순간)")]
    [Tooltip("발사 순간 머즐 연출 VFX(총구 화염/시위 등). 시전자 머즐 지점에 재생. 선택.")]
    [SerializeField] private GameObject muzzleVfxPrefab;
    [Tooltip("머즐 VFX 를 공격 방향(시전자->대상)으로 회전시켜 재생할지. 방향성 있는 화염/시위 연출이면 켠다. 사방 대칭이면 끈다.")]
    [SerializeField] private bool orientMuzzleToTarget = true;
    [Tooltip("발사음. 비우면 공통 기본 공격음(SfxKey.Attack).")]
    [SerializeField] private AudioClip fireClip;

    [Header("Impact (도달 순간)")]
    [Tooltip("도달 지점 임팩트 VFX. 선택.")]
    [SerializeField] private GameObject impactVfxPrefab;
    [Tooltip("도달(타격) 사운드. 선택.")]
    [SerializeField] private AudioClip impactClip;

    public override void Execute(AttackContext ctx) {
        // 발사 순간: 머즐 연출 + 발사음. 방향 옵션이 켜져 있으면 공격 방향으로 회전시켜 재생한다.
        if (muzzleVfxPrefab != null) {
            if (orientMuzzleToTarget) ctx.PlayVfxToward(muzzleVfxPrefab, ctx.CasterPoint);
            else ctx.PlayVfx(muzzleVfxPrefab, ctx.CasterPoint);
        }
        if (fireClip != null) ctx.PlaySfx(fireClip);
        else ctx.PlaySfx(SfxKey.Attack);

        float mult = damageMultiplier * ctx.Multiplier;

        // 타격(임팩트) VFX 는 시전자 상태(변신 등)의 오버라이드가 있으면 그걸 쓴다.
        var impactVfx = ctx.ResolveImpactVfx(impactVfxPrefab);

        // 발사체 미할당: 즉시 명중으로 폴백(데미지 + 임팩트).
        if (projectilePrefab == null) {
            ctx.DealDamage(mult);
            if (impactVfx != null) ctx.PlayVfx(impactVfx, ctx.TargetPoint);
            if (impactClip != null) ctx.PlaySfx(impactClip);
            return;
        }

        var spec = new ProjectileSpec {
            Prefab = projectilePrefab,
            Speed = projectileSpeed,
            FaceDirection = faceDirection,
            ImpactVfxPrefab = impactVfx,
            ImpactClip = impactClip,
            // 도달 시 기본공격 데미지 적용(방어/지터/치명타/최종데미지 포함). 발사체는 이 규칙을 모르고 전달만 한다.
            OnHit = hit => hit.Target.TakeDamage(hit.Source.Stats, mult, hit.Source)
        };
        ctx.LaunchProjectile(spec, ctx.CasterPoint, ctx.Target);
    }
}
