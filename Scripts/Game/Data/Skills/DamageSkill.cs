using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데미지형 스킬 공용 베이스. 타겟 수집/정렬/데미지 적용/임팩트 연출 등 공통 로직을 제공해
/// 파생 스킬은 "어떤 대상을 몇 % 로 때릴지"만 조립하면 된다(전투 코드 수정 불필요, UnitSkill 계약만 사용).
/// 모든 데미지는 대상의 TakeDamage(공격자 스탯 + 배수)로 전달한다(방어/관통/지터/치명타/최종데미지/사망은 대상이 처리).
///
/// 전달 방식은 여기 세 메서드에 집중된다: Hit(단발), HitRepeated(같은 대상 동일배수 다단), HitSequence(같은 대상 이종배수 다단).
/// projectilePrefab 을 할당하면 즉발 대신 발사체로 전달되고(도달 시 데미지/임팩트), 비우면 즉발이다. 파생 스킬은
/// 이 메서드들만 호출하므로 전달 방식(즉발/발사체/발사체 개수)을 신경 쓸 필요가 없다.
///
/// 발사체 다단 전달 방식은 projectileDelivery 로 스킬마다 고른다(서로 다른 대상은 항상 각각 발사됨):
///  - OnePerHit  : 타격 1회 = 발사체 1발 (예: 화살 2발이 각각 명중해 총 2회)
///  - OnePerTarget: 대상 1명 = 발사체 1발 (예: 오브 1발이 도달해 그 자리에서 2회 명중)
/// </summary>
public abstract class DamageSkill : UnitSkill {
    /// <summary>발사체로 다단 히트를 전달하는 방식(같은 대상 기준). 서로 다른 대상은 항상 각각 발사된다.</summary>
    public enum ProjectileDelivery {
        OnePerHit,      // 타격마다 발사체 1발(여러 발이 각각 명중)
        OnePerTarget    // 대상마다 발사체 1발(1발이 도달해 여러 번 명중)
    }

    [Header("Impact (선택)")]
    [Tooltip("각 타격 대상 위치에 재생할 임팩트 VFX. 비우면 재생 안 함(피격 연출은 대상 TakeDamage 가 별도로 처리). 발사체 사용 시 도달 지점에 재생된다.")]
    [SerializeField] private GameObject impactVfxPrefab;
    [Tooltip("임팩트 VFX 를 공격 방향(시전자->대상)으로 회전시켜 재생할지. 방향성 있는 베기/충격 연출이면 켠다. 사방 대칭/폭발형이면 끈다.")]
    [SerializeField] private bool orientImpactToTarget = true;

    [Header("Projectile (선택)")]
    [Tooltip("할당 시 이 스킬의 각 타격이 즉발 대신 발사체로 전달된다(발사 -> 도달 시 데미지/임팩트). 비우면 즉발. " +
             "다단 히트는 대상에게 여러 발이, 광역/다중 대상은 대상마다 한 발씩 날아간다. 프리팹에는 Projectile 컴포넌트가 필요.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("발사체 이동 속도(월드 units/초). 0 이하면 즉시 명중(hitscan).")]
    [SerializeField] private float projectileSpeed = 16f;
    [Tooltip("발사체를 진행 방향으로 회전시킬지(화살처럼). 총알/구체/오브형이면 끈다.")]
    [SerializeField] private bool projectileFaceDirection = true;
    [Tooltip("발사체로 같은 대상에 다단 히트를 전달하는 방식. OnePerHit=타격마다 발사체 1발(여러 발이 각각 명중). " +
             "OnePerTarget=대상마다 발사체 1발(1발이 도달해 여러 번 명중). 서로 다른 대상은 항상 각각 발사된다.")]
    [SerializeField] private ProjectileDelivery projectileDelivery = ProjectileDelivery.OnePerHit;
    [Tooltip("발사체 발사 순간 발사 지점에 재생하는 머즐 VFX(총구 화염/시위 등). 발사체 사용 시 매 발사마다 재생. 비우면 재생 안 함. " +
             "시전 순간 1회만 내는 연출은 castVfxPrefab 을 쓴다.")]
    [SerializeField] private GameObject muzzleVfxPrefab;
    [Tooltip("머즐 VFX 를 발사 방향(발사 지점->대상)으로 회전시켜 재생할지. 방향성 있는 화염/시위 연출이면 켠다. 사방 대칭이면 끈다.")]
    [SerializeField] private bool orientMuzzleToDirection = true;

    [Header("Timing (선택)")]
    [Tooltip("같은 대상에 대한 다단 히트 사이의 간격(초). 0이면 한 프레임에 동시 처리(뭉쳐 나감). " +
             "0보다 크면 순차 발사/적용되어 연사 느낌이 난다. OnePerHit=발사가, OnePerTarget=도달 후 타격이 이 간격으로 벌어진다. 서로 다른 대상 간에는 적용 안 됨.")]
    [SerializeField] private float hitInterval = 0f;

    /// <summary>다단 히트 간격(초). 대상 재선정형 다단(RandomMultiHit 등 HitRepeated 를 못 쓰는 스킬)이 직접 Schedule 로 스태거할 때 참조한다.</summary>
    protected float HitInterval => hitInterval;

    // 발사체로 다단을 한 발에 묶어 보낼지(OnePerTarget). 프리팹이 없으면(즉발) 의미 없다.
    private bool BundlePerTarget => projectilePrefab != null && projectileDelivery == ProjectileDelivery.OnePerTarget;

    /// <summary>대상 1명에게 배수 데미지 1회를 전달한다(치명타 판정 + 선택 임팩트 VFX).
    /// projectilePrefab 이 할당되어 있으면 발사체로(도달 시 적용), 아니면 즉발로 적용한다.</summary>
    protected void Hit(SkillContext ctx, ICombatUnit target, float multiplier) {
        if (!target.IsTargetable()) return;
        if (projectilePrefab != null)
            LaunchBundled(ctx, target, hit => ApplyHit(ctx, hit.Target, multiplier, hit.Position));
        else
            ApplyHit(ctx, target, multiplier, target.Position);
    }

    /// <summary>같은 대상에게 동일 배수로 hits 회 타격한다(예: SingleTargetSkill 의 hitCount).
    /// 발사체 사용 시 projectileDelivery 에 따라 타격마다 1발(OnePerHit) 또는 1발이 여러 번 명중(OnePerTarget)하며,
    /// hitInterval 이 0보다 크면 그 간격으로 순차 발사/적용된다(연사).</summary>
    protected void HitRepeated(SkillContext ctx, ICombatUnit target, float multiplier, int hits) {
        if (!target.IsTargetable() || hits <= 0) return;
        if (BundlePerTarget) {
            // 발사체 1발이 도달한 뒤 hits 회 적용(간격이 있으면 도달 후 순차 틱). 도달 시점 대상 재확인은 ApplyHit 이 담당.
            LaunchBundled(ctx, target, hit => {
                for (int i = 0; i < hits; i++) ScheduleApply(ctx, i * hitInterval, hit.Target, multiplier, hit.Position);
            });
        } else {
            // 타격마다 발사체 1발/즉발을 hitInterval 간격으로 순차 실행(발사 시점 대상 재확인은 Hit 이 담당).
            for (int i = 0; i < hits; i++) ScheduleHit(ctx, i * hitInterval, target, multiplier);
        }
    }

    /// <summary>같은 대상에게 서로 다른 배수로 연속 타격한다(예: TwoWaveAoeSkill 의 1파/2파).
    /// 발사체 사용 시 projectileDelivery 에 따라 배수마다 1발(OnePerHit) 또는 1발이 모든 배수를 순서대로 명중(OnePerTarget)하며,
    /// hitInterval 이 0보다 크면 그 간격으로 순차 발사/적용된다.</summary>
    protected void HitSequence(SkillContext ctx, ICombatUnit target, params float[] multipliers) {
        if (!target.IsTargetable() || multipliers == null || multipliers.Length == 0) return;
        if (BundlePerTarget) {
            LaunchBundled(ctx, target, hit => {
                for (int i = 0; i < multipliers.Length; i++) ScheduleApply(ctx, i * hitInterval, hit.Target, multipliers[i], hit.Position);
            });
        } else {
            for (int i = 0; i < multipliers.Length; i++) ScheduleHit(ctx, i * hitInterval, target, multipliers[i]);
        }
    }

    // hitInterval 스태거: delay 후(0이면 즉시) 발사체 1발/즉발 1타를 target 에게 전달한다. Hit 이 실행 시점의 대상 생존을 재확인한다.
    private void ScheduleHit(SkillContext ctx, float delay, ICombatUnit target, float multiplier) {
        if (delay <= 0f) { Hit(ctx, target, multiplier); return; }
        ctx.Runtime.Schedule(delay, () => Hit(ctx, target, multiplier));
    }

    // hitInterval 스태거: delay 후(0이면 즉시) 데미지/임팩트를 지점에 적용한다(발사체 도달 후 다단 틱). ApplyHit 이 대상 생존을 재확인한다.
    private void ScheduleApply(SkillContext ctx, float delay, ICombatUnit target, float multiplier, Vector2 at) {
        if (delay <= 0f) { ApplyHit(ctx, target, multiplier, at); return; }
        ctx.Runtime.Schedule(delay, () => ApplyHit(ctx, target, multiplier, at));
    }

    /// <summary>from 위치에서 target 에게 배수 데미지 1회를 전달하고, 데미지 적용 후 onArrive(도달 지점) 를 실행한다.
    /// projectilePrefab 이 있으면 발사체로(도달 시 적용), 없으면 즉발이다. Hit 과 달리 발사 origin 을 임의 지정할 수 있어
    /// "이전 대상 위치 -> 다음 대상"으로 이어 쏘는 연쇄(체인) 스킬의 기반이 된다. onArrive 로 다음 발사를 잇는다.</summary>
    protected void HitFrom(SkillContext ctx, Vector2 from, ICombatUnit target, float multiplier, Action<Vector2> onArrive = null) {
        if (!target.IsTargetable()) return;
        if (projectilePrefab != null) {
            var spec = new ProjectileSpec {
                Prefab = projectilePrefab,
                Speed = projectileSpeed,
                FaceDirection = projectileFaceDirection,
                OnHit = hit => { ApplyHit(ctx, hit.Target, multiplier, hit.Position); onArrive?.Invoke(hit.Position); }
            };
            PlayMuzzle(ctx, from, target.Position);
            ctx.Runtime.LaunchProjectile(spec, from, ctx.Caster, target);
        } else {
            ApplyHit(ctx, target, multiplier, target.Position);
            onArrive?.Invoke(target.Position);
        }
    }

    /// <summary>from 에서 anchor(대표 대상) 쪽으로 투척체 1발을 보내고, 도달 시 onArrive(도달 지점) 를 실행한다(데미지 없음, 순수 전달).
    /// Hit/HitFrom 과 달리 도착 자체는 데미지를 주지 않아 "한 발이 도착해 광역/설치 등 여러 효과를 내는" 스킬(투척 폭발/낙하)의 기반이 된다.
    /// 발사체는 anchor 유닛으로 유도되므로 중심에 가장 가까운 적을 anchor 로 주면 그 지점에서 터진다. 프리팹 미할당 시 즉시 onArrive(anchor 위치).
    ///
    /// anchor 가 비행 중에 죽어도 발사체는 마지막 위치까지 날아가 onArrive 를 실행한다(ArriveEvenIfTargetLost).
    /// 이 발사체의 효과는 anchor 개인이 아니라 "도착 지점"에 걸리는 것이라, anchor 가 먼저 쓰러졌다고 효과가
    /// 통째로 사라지면 주변에 적이 남아 있어도 스킬 한 번을 허공에 버리게 된다.
    /// 도착 지점에서 누구를 때릴지는 onArrive 가 그 시점에 다시 고른다(호출측 책임).</summary>
    protected void LaunchArc(SkillContext ctx, Vector2 from, ICombatUnit anchor, Action<Vector2> onArrive) {
        if (!anchor.Exists()) return;
        if (projectilePrefab != null) {
            var spec = new ProjectileSpec {
                Prefab = projectilePrefab,
                Speed = projectileSpeed,
                FaceDirection = projectileFaceDirection,
                ArriveEvenIfTargetLost = true,
                OnHit = hit => onArrive?.Invoke(hit.Position)
            };
            PlayMuzzle(ctx, from, anchor.Position);
            ctx.Runtime.LaunchProjectile(spec, from, ctx.Caster, anchor);
        } else {
            onArrive?.Invoke(anchor.Position);
        }
    }

    /// <summary>대상 1명에게 배수 데미지 1회를 즉시 적용한다(발사체 없이). 도달 지점 광역/폭발에서 개별 대상 타격에 쓴다(임팩트 VFX 포함).</summary>
    protected void Strike(SkillContext ctx, ICombatUnit target, float multiplier, Vector2 impactAt) {
        ApplyHit(ctx, target, multiplier, impactAt);
    }

    /// <summary>from 에서 발사체 "1발"만 발사해 orderedTargets(가까운 순 정렬)를 차례로 관통한다.
    /// 각 대상은 발사체가 자기 위치를 통과하는 예상 시점(거리/속도)에 onPass 로 전달되고, 마지막 대상은 실제 도달 시 전달된다.
    /// Hit/HitFrom 이 대상마다 발사체를 내는 것과 달리 한 발이 경로를 지나가는 관통(피어스) 스킬의 기반이다.
    /// 발사체 미할당 또는 속도 0 이하(hitscan)면 전원에게 즉시 onPass 를 실행한다. 데미지는 주지 않는 순수 전달이며,
    /// 실제 타격/확산은 호출측이 onPass 안에서 Strike 등으로 적용한다(대상 생존 재확인 포함).</summary>
    protected void LaunchPierce(SkillContext ctx, Vector2 from, List<ICombatUnit> orderedTargets, Action<ICombatUnit> onPass) {
        if (orderedTargets == null || orderedTargets.Count == 0 || onPass == null) return;
        if (projectilePrefab == null || projectileSpeed <= 0f) {
            for (int i = 0; i < orderedTargets.Count; i++) onPass(orderedTargets[i]);
            return;
        }

        // 중간 대상: 발사 시점 거리 기준 통과 예상 시각에 순차 적용한다(발사체는 계속 날아간다).
        for (int i = 0; i < orderedTargets.Count - 1; i++) {
            var t = orderedTargets[i];
            float delay = ((Vector2)t.Position - from).magnitude / projectileSpeed;
            ctx.Runtime.Schedule(delay, () => onPass(t));
        }

        // 마지막 대상: 발사체가 실제 도달할 때 적용한다(도달 지점 기준 연출과 일치).
        var last = orderedTargets[orderedTargets.Count - 1];
        var spec = new ProjectileSpec {
            Prefab = projectilePrefab,
            Speed = projectileSpeed,
            FaceDirection = projectileFaceDirection,
            OnHit = hit => onPass(hit.Target)
        };
        PlayMuzzle(ctx, from, last.Position);
        ctx.Runtime.LaunchProjectile(spec, from, ctx.Caster, last);
    }

    // 시전 기준점에서 대상에게 발사체 1발을 쏘고, 도달 시 onArrive 를 실행한다(발사체 전달 공용).
    private void LaunchBundled(SkillContext ctx, ICombatUnit target, Action<ProjectileHit> onArrive) {
        var spec = new ProjectileSpec {
            Prefab = projectilePrefab,
            Speed = projectileSpeed,
            FaceDirection = projectileFaceDirection,
            OnHit = onArrive
        };
        PlayMuzzle(ctx, ctx.CastOrigin, target.Position);
        ctx.Runtime.LaunchProjectile(spec, ctx.CastOrigin, ctx.Caster, target);
    }

    // 발사 순간 머즐 VFX 재생(할당 시). 매 발사마다 발사 지점(from)에서 대상(toward) 방향으로 재생한다.
    private void PlayMuzzle(SkillContext ctx, Vector2 from, Vector2 toward) {
        PlayDirectionalVfx(ctx, muzzleVfxPrefab, from, orientMuzzleToDirection, toward - from);
    }

    // 실제 데미지 + 임팩트 적용(즉발/발사체 도달 공용). 대상 생존 시에만 적용한다.
    private void ApplyHit(SkillContext ctx, ICombatUnit target, float multiplier, Vector2 impactAt) {
        // 예약 실행/발사체 도달로 시간차를 두고 불리므로, 그 사이 전장이 정리돼 대상이나 시전자가 파괴됐을 수 있다.
        if (!target.IsTargetable() || !ctx.Caster.Exists()) return;
        target.TakeDamage(ctx.Caster.Stats, multiplier, ctx.Caster, StatusEffects);
        PlayImpact(ctx, impactAt);
    }

    // 임팩트 VFX 재생(할당 시). orientImpactToTarget 이면 시전자->지점 방향으로 회전시켜 낸다.
    private void PlayImpact(SkillContext ctx, Vector2 at) {
        PlayDirectionalVfx(ctx, impactVfxPrefab, at, orientImpactToTarget, at - (Vector2)ctx.Caster.Position);
    }

    /// <summary>prefab 을 at 위치에 재생한다. orient 면 facing 방향(월드 Z)으로 회전시켜 낸다(비면 프리팹 기본 회전).
    /// 임팩트/시전자 타격 등 방향성 있는 VFX 연출의 공용 재생부. prefab 이 없으면 아무 것도 하지 않는다.</summary>
    protected void PlayDirectionalVfx(SkillContext ctx, GameObject prefab, Vector2 at, bool orient, Vector2 facing) {
        if (prefab == null) return;
        if (orient && facing.sqrMagnitude > 0.000001f) {
            float ang = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            ctx.Runtime.PlayVfx(prefab, at, ang);
        } else {
            ctx.Runtime.PlayVfx(prefab, at);
        }
    }

    /// <summary>생존한 적만 모아 반환한다(스킬 실행 시 스냅샷).</summary>
    protected static List<ICombatUnit> AliveEnemies(SkillContext ctx) {
        var src = ctx.Runtime.Enemies(ctx.Caster);
        var list = new List<ICombatUnit>(src.Count);
        for (int i = 0; i < src.Count; i++) {
            var e = src[i];
            if (e.IsTargetable()) list.Add(e);
        }
        return list;
    }

    /// <summary>from 에 가장 가까운 유닛(없으면 null).</summary>
    protected static ICombatUnit Nearest(Vector2 from, List<ICombatUnit> units) {
        ICombatUnit best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < units.Count; i++) {
            var u = units[i];
            if (!u.IsTargetable()) continue;
            float sqr = ((Vector2)u.Position - from).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = u; }
        }
        return best;
    }

    /// <summary>from 에 가장 가까운 유닛 중 exclude 에 없는 것(없으면 null). maxRadius>0 이면 그 반경 이내만 후보.
    /// 이전 대상을 제외하며 이어 찾는 연쇄(체인) 타게팅에 사용한다.</summary>
    protected static ICombatUnit NearestExcluding(Vector2 from, List<ICombatUnit> units, HashSet<ICombatUnit> exclude, float maxRadius = 0f) {
        ICombatUnit best = null;
        float bestSqr = float.MaxValue;
        float limitSqr = maxRadius > 0f ? maxRadius * maxRadius : float.MaxValue;
        for (int i = 0; i < units.Count; i++) {
            var u = units[i];
            if (!u.IsTargetable()) continue;
            if (exclude != null && exclude.Contains(u)) continue;
            float sqr = ((Vector2)u.Position - from).sqrMagnitude;
            if (sqr <= limitSqr && sqr < bestSqr) { bestSqr = sqr; best = u; }
        }
        return best;
    }

    /// <summary>from 반경 radius 이내의 유닛을 가까운 순으로 최대 n 명 outList 에 담는다. radius 가 0 이하면 거리 제한이 없다(TakeNearest 와 동일).
    /// 확산/폭발의 "반경 안에서만 최대 N명" 타게팅에 쓴다.</summary>
    protected static void TakeNearestWithin(Vector2 from, List<ICombatUnit> units, float radius, int n, List<ICombatUnit> outList) {
        if (radius <= 0f) { TakeNearest(from, units, n, outList); return; }
        float r2 = radius * radius;
        var inRange = new List<ICombatUnit>(units.Count);
        for (int i = 0; i < units.Count; i++) {
            var u = units[i];
            if (!u.IsTargetable()) continue;
            if (((Vector2)u.Position - from).sqrMagnitude <= r2) inRange.Add(u);
        }
        TakeNearest(from, inRange, n, outList);
    }

    /// <summary>from 에 가까운 순으로 최대 n 명을 outList 에 담는다(가까운 순 정렬).</summary>
    protected static void TakeNearest(Vector2 from, List<ICombatUnit> units, int n, List<ICombatUnit> outList) {
        units.Sort((a, b) => ((Vector2)a.Position - from).sqrMagnitude
            .CompareTo(((Vector2)b.Position - from).sqrMagnitude));
        int count = Mathf.Min(Mathf.Max(0, n), units.Count);
        for (int i = 0; i < count; i++) outList.Add(units[i]);
    }

    /// <summary>반경 내 적이 가장 많은 지점(밀집 중심). 각 적을 후보 중심으로 반경 내 적 수를 세어 최다 지점을 고른다.</summary>
    protected static Vector2 DensestCenter(List<ICombatUnit> units, float radius) {
        if (units.Count == 0) return Vector2.zero;
        float r2 = radius * radius;
        Vector2 best = units[0].Position;
        int bestCount = -1;
        for (int i = 0; i < units.Count; i++) {
            Vector2 c = units[i].Position;
            int count = 0;
            for (int j = 0; j < units.Count; j++)
                if (((Vector2)units[j].Position - c).sqrMagnitude <= r2) count++;
            if (count > bestCount) { bestCount = count; best = c; }
        }
        return best;
    }
}
