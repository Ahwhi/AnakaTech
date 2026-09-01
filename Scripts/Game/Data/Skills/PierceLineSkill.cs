using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 관통/직선. 시전자에서 가장 가까운 적 방향으로 직선을 긋고, 그 경로(폭 lineWidth) 위의 적 최대 maxTargets 명을 관통 타격한다.
/// splashMultiplier > 0 이면 관통으로 맞은 적마다 주변에 작은 폭발(확산)을 추가한다(확산은 즉발).
/// 발사체 할당 시 대상마다 쏘지 않고 "1발"만 발사해 경로를 지나가며 통과 시점에 타격한다(LaunchPierce).
/// 예) 관통 사격(180%) / 허리케인 러시(경로 5명 80%) / 다크 오브(170% 관통 + 적중마다 주변 50%).
/// </summary>
[CreateAssetMenu(fileName = "PierceLineSkill", menuName = "Anaka/Skills/Pierce Line")]
public sealed class PierceLineSkill : DamageSkill {
    [Header("Pierce Line")]
    [Tooltip("경로상 각 대상의 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float damageMultiplier = 1.8f;
    [Tooltip("최대 관통 대상 수.")]
    [SerializeField] private int maxTargets = 5;
    [Tooltip("직선 판정 폭(월드 units). 경로 좌우 이 폭의 절반 이내 적을 맞힌다.")]
    [SerializeField] private float lineWidth = 1.2f;

    [Header("Per-Hit Splash (선택)")]
    [Tooltip("관통 적중마다 주변 확산 배수. 0 이면 확산 없음.")]
    [SerializeField] private float splashMultiplier = 0f;
    [Tooltip("확산 반경(월드 units).")]
    [SerializeField] private float splashRadius = 1.5f;
    [Tooltip("적중 1회당 확산 최대 대상 수(관통 대상 제외).")]
    [SerializeField] private int maxSplashPerHit = 2;

    // 관통 대상 각각에게 본체 1타 + (설정 시) 확산 maxSplashPerHit 타.
    public override SkillPowerProfile PowerProfile {
        get {
            bool hasSplash = splashMultiplier > 0f && maxSplashPerHit > 0;
            int splashHits = hasSplash ? maxSplashPerHit : 0;
            return new SkillPowerProfile(damageMultiplier + splashMultiplier * splashHits,
                                         1 + splashHits, maxTargets);
        }
    }

    protected override void OnExecute(SkillContext ctx) {
        var enemies = AliveEnemies(ctx);
        var aim = Nearest(ctx.Caster.Position, enemies);
        if (aim == null) return;

        Vector2 origin = ctx.Caster.Position;
        Vector2 dir = ((Vector2)aim.Position - origin);
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        float halfWidth = Mathf.Max(0.01f, lineWidth * 0.5f);

        // 경로(전방) + 폭 이내 적을 진행거리 순으로 모은다.
        var onLine = new List<ICombatUnit>();
        for (int i = 0; i < enemies.Count; i++) {
            Vector2 v = (Vector2)enemies[i].Position - origin;
            float proj = Vector2.Dot(v, dir);
            if (proj <= 0f) continue;                       // 뒤쪽 제외(전방만 관통)
            float perp = (v - dir * proj).magnitude;
            if (perp <= halfWidth) onLine.Add(enemies[i]);
        }
        onLine.Sort((a, b) => Vector2.Dot((Vector2)a.Position - origin, dir)
            .CompareTo(Vector2.Dot((Vector2)b.Position - origin, dir)));

        int count = Mathf.Min(Mathf.Max(0, maxTargets), onLine.Count);
        if (count <= 0) return;
        var pierced = onLine.GetRange(0, count);

        // 발사체 1발이 경로를 지나가며 각 대상을 통과 시점에 타격한다(전달은 LaunchPierce 가 담당).
        // 통과 시점에 대상이 이미 죽었으면 그 대상과 확산 모두 생략한다.
        float r2 = splashRadius * splashRadius;
        LaunchPierce(ctx, ctx.CastOrigin, pierced, t => {
            if (!t.IsTargetable()) return;
            Vector2 at = t.Position;
            Strike(ctx, t, damageMultiplier, at);
            if (splashMultiplier <= 0f) return;

            // 적중 지점 주변 확산(관통 대상 자신 제외, 가까운 순 제한). 즉발 폭발이라 발사체 없이 적용한다.
            var near = new List<ICombatUnit>();
            for (int j = 0; j < enemies.Count; j++) {
                var e = enemies[j];
                if (e == t || !e.IsTargetable()) continue;
                if (((Vector2)e.Position - at).sqrMagnitude <= r2) near.Add(e);
            }
            var splash = new List<ICombatUnit>();
            TakeNearest(at, near, maxSplashPerHit, splash);
            for (int j = 0; j < splash.Count; j++) Strike(ctx, splash[j], splashMultiplier, splash[j].Position);
        });
    }
}
