using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 돌진 연속 타격. 전방(선택)의 가까운 적 최대 maxTargets 명을 하나씩 대시로 이동하며 순차 타격한다(각 damageMultiplier).
/// 시전자의 시각 표현만 대상 사이를 빠르게 오가며(논리 위치 불변), 도착 시점마다 데미지를 준 뒤 다음 대상으로 이어진다.
/// 예) C2002 셀도우 스텝: 전방 최대 4명 각 90% (총 360%).
/// 예) C3401 백열난타: 최대 6명 각 90% + 마지막 대상 피니셔 260% (총 800%). finisherMultiplier 사용.
/// </summary>
[CreateAssetMenu(fileName = "DashStrikeSkill", menuName = "Anaka/Skills/Dash Strike")]
public sealed class DashStrikeSkill : DamageSkill {
    [Header("Dash Strike")]
    [Tooltip("각 대상에게 주는 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float damageMultiplier = 0.9f;
    [Tooltip("최대 타격(대시) 대상 수.")]
    [SerializeField] private int maxTargets = 4;
    [Tooltip("각 대상으로 대시하는 데 걸리는 시간(초). 도착과 동시에 타격한다.")]
    [SerializeField] private float dashDuration = 0.08f;
    [Tooltip("대상 지점에서 이만큼 앞에 멈춘다(겹침 방지, 월드 units).")]
    [SerializeField] private float stopShort = 0.6f;
    [Tooltip("타격 후 다음 대상으로 대시하기 전 머무는 시간(초).")]
    [SerializeField] private float hitPause = 0.04f;
    [Tooltip("마지막 타격 후 원위치로 복귀하는 데 걸리는 시간(초).")]
    [SerializeField] private float returnDuration = 0.12f;
    [Tooltip("체크 시 시전자가 조준한 방향(주 타겟 쪽) 전방의 적만 대상으로 삼는다.")]
    [SerializeField] private bool frontalOnly = true;

    [Header("Caster Attack VFX (선택)")]
    [Tooltip("타격마다 시전자의 현재 위치(머즐)에서 재생하는 VFX. 대시로 이동한 지점에서 나온다. 비우면 재생 안 함(적 쪽 임팩트는 impactVfxPrefab 이 별도 담당).")]
    [SerializeField] private GameObject attackVfxPrefab;
    [Tooltip("시전자 타격 VFX 를 대상 방향으로 회전시켜 재생할지(castVfx 와 동일하게 적 쪽을 바라봄).")]
    [SerializeField] private bool orientAttackToTarget = true;

    [Header("Finisher (선택, 마지막 대상 마무리타)")]
    [Tooltip("마지막 대시 대상에게 추가로 넣는 피니셔 배수(1.0 = 100%). 0이면 피니셔 없음. 대시 도착 자리(복귀 전)에서 넣는다. 예: 백열난타(6명 90% + 마지막 260%).")]
    [SerializeField] private float finisherMultiplier = 0f;
    [Tooltip("마지막 대시 타격 후 피니셔까지 지연(초). 0이면 마지막 타격과 동시에 들어간다.")]
    [SerializeField] private float finisherDelay = 0.15f;
    [Tooltip("피니셔 순간 시전자 머즐에 재생하는 VFX. 비우면 attackVfxPrefab 을 재사용한다(그것도 비우면 재생 안 함).")]
    [SerializeField] private GameObject finisherVfxPrefab;

    [Header("Second Wave (선택, 검집 닫기류)")]
    [Tooltip("마무리 2파 배수(1.0 = 100%). 0이면 2파 없음. 대시로 훑은 대상 생존자 전체에게 잠시 후 한 번 더 준다(예: 벚꽃일섬 '검집 닫는 순간 다시').")]
    [SerializeField] private float secondWaveMultiplier = 0f;
    [Tooltip("마지막 1파 타격 후 2파까지 지연(초). 보통 원위치 복귀(returnDuration) 뒤 검집을 닫는 타이밍으로 잡는다.")]
    [SerializeField] private float secondWaveDelay = 0.3f;

    // 훑은 대상 각각에게 대시 1타 + (설정 시) 마무리 2파. 피니셔는 실제로는 마지막 1명에게만 들어가지만
    // 여기서는 세트에 포함해 신고한다(대상이 여럿일 때 소폭 후하게 평가된다).
    public override SkillPowerProfile PowerProfile {
        get {
            bool hasFinisher = finisherMultiplier > 0f;
            bool hasSecondWave = secondWaveMultiplier > 0f;
            return new SkillPowerProfile(
                damageMultiplier + finisherMultiplier + secondWaveMultiplier,
                1 + (hasFinisher ? 1 : 0) + (hasSecondWave ? 1 : 0),
                maxTargets);
        }
    }

    protected override void OnExecute(SkillContext ctx) {
        var enemies = AliveEnemies(ctx);
        if (frontalOnly) FilterFrontal(ctx, enemies);
        if (enemies.Count == 0) return;

        var targets = new List<ICombatUnit>();
        TakeNearest(ctx.Caster.Position, enemies, maxTargets, targets);
        if (targets.Count == 0) return;

        DashHit(ctx, targets, 0);
    }

    // targets[i] 로 대시(연출) -> dashDuration 후 타격 -> hitPause 후 다음 대상. 마지막이면 복귀 + (선택)2파.
    private void DashHit(SkillContext ctx, List<ICombatUnit> targets, int index) {
        // 이미 죽은/사라진 대상은 건너뛴다.
        while (index < targets.Count && !targets[index].IsTargetable()) index++;
        if (index >= targets.Count) { Finish(ctx, targets); return; }

        var target = targets[index];
        // 멈추는 거리에 대상의 몸통 반경을 더한다(덩치가 큰 적의 중심으로 뛰어들지 않게).
        float stop = stopShort + target.BodyRadius;
        ctx.Runtime.DashCaster(ctx.Caster, target.Position, dashDuration, stop); // 시각 이동(순수 연출)

        int current = index;
        ctx.Runtime.Schedule(dashDuration, () => {
            if (target.IsTargetable() && ctx.Caster.Exists()) {
                // 도착한 시전자 위치(머즐)에서 대상 방향으로 타격 VFX 재생(적 쪽 임팩트와 별개).
                Vector2 muzzle = ctx.Caster.MuzzlePoint;
                PlayDirectionalVfx(ctx, attackVfxPrefab, muzzle, orientAttackToTarget, (Vector2)target.Position - muzzle);
            }
            Hit(ctx, target, damageMultiplier); // 도착 시점 타격(즉발/발사체 자동 라우팅)
            int next = current + 1;
            if (next >= targets.Count) FinisherThenFinish(ctx, targets, target);
            else ctx.Runtime.Schedule(hitPause, () => DashHit(ctx, targets, next));
        });
    }

    // 마지막 대시 타격 직후: 피니셔가 설정돼 있으면 대시 도착 자리에서 마지막 대상에게 추가타를 넣고 복귀한다.
    private void FinisherThenFinish(SkillContext ctx, List<ICombatUnit> targets, ICombatUnit last) {
        if (finisherMultiplier <= 0f) { Finish(ctx, targets); return; }
        if (finisherDelay > 0f) ctx.Runtime.Schedule(finisherDelay, () => ApplyFinisher(ctx, targets, last));
        else ApplyFinisher(ctx, targets, last);
    }

    // 피니셔 적용: 시전자 머즐에서 피니셔 VFX(비면 attackVfx 재사용) + 마지막 대상 추가타. Hit 이 생존을 재확인한다.
    private void ApplyFinisher(SkillContext ctx, List<ICombatUnit> targets, ICombatUnit last) {
        if (last.IsTargetable() && ctx.Caster.Exists()) {
            Vector2 muzzle = ctx.Caster.MuzzlePoint;
            var vfx = finisherVfxPrefab != null ? finisherVfxPrefab : attackVfxPrefab;
            PlayDirectionalVfx(ctx, vfx, muzzle, orientAttackToTarget, (Vector2)last.Position - muzzle);
        }
        Hit(ctx, last, finisherMultiplier);
        Finish(ctx, targets);
    }

    // 대시 종료: 원위치로 복귀하고, 2파가 설정돼 있으면 잠시 후 마무리 타격을 넣는다.
    private void Finish(SkillContext ctx, List<ICombatUnit> targets) {
        ctx.Runtime.ReturnCaster(ctx.Caster, returnDuration);
        if (secondWaveMultiplier <= 0f) return;
        if (secondWaveDelay > 0f) ctx.Runtime.Schedule(secondWaveDelay, () => SecondWave(ctx, targets));
        else SecondWave(ctx, targets);
    }

    // 검집 닫기(마무리 2파): 대시로 훑은 대상 중 생존자 전체에게 그 자리에서 한 번 더. 시전자 타격 VFX 는 1회만 낸다.
    private void SecondWave(SkillContext ctx, List<ICombatUnit> targets) {
        ICombatUnit facing = null;
        for (int i = 0; i < targets.Count; i++) {
            if (targets[i].IsTargetable()) { facing = targets[i]; break; }
        }
        if (facing != null && ctx.Caster.Exists()) {
            Vector2 muzzle = ctx.Caster.MuzzlePoint;
            PlayDirectionalVfx(ctx, attackVfxPrefab, muzzle, orientAttackToTarget, (Vector2)facing.Position - muzzle);
        }
        for (int i = 0; i < targets.Count; i++) Hit(ctx, targets[i], secondWaveMultiplier); // Hit 이 생존 재확인
    }

    // 시전자 -> 주 타겟 방향을 전방으로 보고, 그 반대편(뒤쪽) 적을 목록에서 제거한다. 주 타겟이 없으면 필터하지 않는다.
    private static void FilterFrontal(SkillContext ctx, List<ICombatUnit> enemies) {
        var primary = ctx.PrimaryTarget;
        if (!primary.IsTargetable()) return;
        Vector2 forward = (Vector2)primary.Position - (Vector2)ctx.Caster.Position;
        if (forward.sqrMagnitude < 0.0001f) return;
        for (int k = enemies.Count - 1; k >= 0; k--) {
            Vector2 to = (Vector2)enemies[k].Position - (Vector2)ctx.Caster.Position;
            if (Vector2.Dot(forward, to) < 0f) enemies.RemoveAt(k);
        }
    }
}
