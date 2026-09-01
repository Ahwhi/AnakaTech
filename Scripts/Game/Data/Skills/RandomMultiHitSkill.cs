using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 다단 히트. hitCount 회 각각 대상 1명을 골라 배수 데미지를 준다. 매 타격마다 대상을 다시 고르므로 동일 대상 중복 가능.
/// preferNearest=false 면 무작위 대상, true 면 매번 가장 가까운 적(정령이 근처 적을 노리는 연출 등).
/// 예) 블레이징 레인(무작위 70%,6) / 오버드라이브(무작위 80%,8) / 스피릿 플라이트(가까운 적 100%,5) / 캣츠 카니발(100%,8).
/// </summary>
[CreateAssetMenu(fileName = "RandomMultiHitSkill", menuName = "Anaka/Skills/Random Multi-Hit")]
public sealed class RandomMultiHitSkill : DamageSkill {
    [Header("Random Multi-Hit")]
    [Tooltip("각 타격의 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float damageMultiplier = 0.7f;
    [Tooltip("총 타격 횟수.")]
    [SerializeField] private int hitCount = 6;
    [Tooltip("체크 시 매번 가장 가까운 적을 타격(무작위 대신).")]
    [SerializeField] private bool preferNearest = false;

    // 타격이 여러 대상에 흩어지지만 총 화력은 같으므로 1세트(대상 1)로 신고한다.
    public override SkillPowerProfile PowerProfile {
        get {
            int hits = Mathf.Max(1, hitCount);
            return new SkillPowerProfile(damageMultiplier * hits, hits, 1);
        }
    }

    protected override void OnExecute(SkillContext ctx) {
        int hits = Mathf.Max(1, hitCount);
        for (int i = 0; i < hits; i++) {
            float delay = i * HitInterval;
            // hitInterval 0이면 한 프레임에 전부, 0보다 크면 그 간격으로 순차 발사(연사). 매 타격은 발사 시점에 대상을 다시 고른다.
            if (delay <= 0f) FireOne(ctx);
            else ctx.Runtime.Schedule(delay, () => FireOne(ctx)); // 전투 재시작 시 자동 취소
        }
    }

    // 1발: 발사 시점의 생존 적 중에서 무작위(또는 최근접) 대상을 골라 타격한다.
    private void FireOne(SkillContext ctx) {
        var enemies = AliveEnemies(ctx);
        if (enemies.Count == 0) return;

        var target = preferNearest
            ? Nearest(ctx.Caster.Position, enemies)
            : enemies[Random.Range(0, enemies.Count)];
        Hit(ctx, target, damageMultiplier);
    }
}
