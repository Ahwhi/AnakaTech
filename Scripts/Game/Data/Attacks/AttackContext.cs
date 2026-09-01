using System.Collections.Generic;
using UnityEngine;
using static Enums;

/// <summary>
/// 기본공격(BasicAttack) 실행에 필요한 문맥. 스킬의 SkillContext 와 같은 역할이되,
/// 발사/피격 기준점과 데미지/연출 헬퍼를 더 제공한다. 전투 유닛이 만들어 공격 SO 에 넘긴다.
/// 공격 SO 는 이 문맥에만 의존하므로 구체 유닛/디렉터 타입과 디커플링된다(스킬과 동일한 방향).
/// </summary>
public sealed class AttackContext {
    public readonly ICombatUnit Caster;
    public readonly ICombatUnit Target;
    public readonly float Multiplier;      // 기본 데미지 배수(기본공격은 보통 1).
    public readonly Vector2 CasterPoint;   // 시전자 연출 기준(머즐/손). 없으면 시전자 머리.
    public readonly Vector2 TargetPoint;   // 피격 연출 기준(대상 머리).

    /// <summary>시전자 상태(변신 등)에 의한 타격(임팩트) VFX 교체. null 이 아니면 공격 SO 는 자신의 임팩트 VFX 대신 이걸 쓴다.</summary>
    public readonly GameObject ImpactVfxOverride;

    /// <summary>이 공격이 적중 시 거는 상태이상(공격 SO 의 설정). 데미지를 넣을 때 함께 전달된다.</summary>
    public readonly IReadOnlyList<StatusEffectSpec> StatusEffects;

    private readonly IBattleRuntime _runtime;

    public AttackContext(ICombatUnit caster, ICombatUnit target, float multiplier,
                         Vector2 casterPoint, Vector2 targetPoint, IBattleRuntime runtime,
                         GameObject impactVfxOverride = null,
                         IReadOnlyList<StatusEffectSpec> statusEffects = null) {
        Caster = caster;
        Target = target;
        Multiplier = multiplier;
        CasterPoint = casterPoint;
        TargetPoint = targetPoint;
        _runtime = runtime;
        ImpactVfxOverride = impactVfxOverride;
        StatusEffects = statusEffects;
    }

    /// <summary>공격 SO 가 타격(임팩트) VFX 를 고를 때 사용: 오버라이드가 있으면 그것, 없으면 자신의 기본 프리팹.</summary>
    public GameObject ResolveImpactVfx(GameObject defaultPrefab) =>
        ImpactVfxOverride != null ? ImpactVfxOverride : defaultPrefab;

    /// <summary>대상에게 기본 데미지(공격력 x mult, 방어/관통/지터/치명타/최종데미지 포함)를 즉시 적용한다.</summary>
    public void DealDamage(float multiplier) {
        if (!Target.IsTargetable()) return;
        Target.TakeDamage(Caster.Stats, multiplier, Caster, StatusEffects);
    }

    public void PlayVfx(GameObject prefab, Vector2 at) {
        if (prefab != null) _runtime.PlayVfx(prefab, at);
    }

    /// <summary>연출 VFX 를 at 위치에 재생하되, 공격 방향(시전자 -> 대상)을 바라보도록 회전시킨다.
    /// 대상이 없으면 방향 없이 재생한다. 근접/즉시 공격의 휘두름/임팩트 연출이 대상 쪽으로 향하게 한다.</summary>
    public void PlayVfxToward(GameObject prefab, Vector2 at) {
        if (prefab == null) return;
        if (!Target.Exists()) { _runtime.PlayVfx(prefab, at); return; }
        Vector2 d = (Vector2)Target.Position - CasterPoint;
        float ang = d.sqrMagnitude > 0.000001f ? Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg : 0f;
        _runtime.PlayVfx(prefab, at, ang);
    }

    public void PlaySfx(SfxKey key) => _runtime.PlaySfx(key);
    public void PlaySfx(AudioClip clip) { if (clip != null) _runtime.PlaySfx(clip); }

    /// <summary>from 지점에서 Target 을 향해 발사체를 발사한다(도달 시 데미지 + 임팩트). 원거리 공격용.</summary>
    public void LaunchProjectile(ProjectileSpec spec, Vector2 from, ICombatUnit target) {
        _runtime.LaunchProjectile(spec, from, Caster, target);
    }

    /// <summary>대상/아군 질의가 필요한 특수 공격(광역 등)용. 단일 대상 공격은 Target 만 쓰면 된다.</summary>
    public IBattleRuntime Runtime => _runtime;
}
