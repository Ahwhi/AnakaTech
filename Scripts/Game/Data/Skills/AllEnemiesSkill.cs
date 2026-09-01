using UnityEngine;

/// <summary>
/// 적 전체 공격. 생존한 모든 적에게 각각 배수 데미지를 hitCount 회 준다(SSR/XR 궁극기류).
/// 예) 폴른 저지먼트(전체 320%,1) / 여우불 난무(전체 60%,6) / 엔젤 콘서트(전체 700%,1) / 스타라이트 쇼(전체 80%,10).
/// </summary>
[CreateAssetMenu(fileName = "AllEnemiesSkill", menuName = "Anaka/Skills/All Enemies")]
public sealed class AllEnemiesSkill : DamageSkill {
    [Header("All Enemies")]
    [Tooltip("각 대상에게 주는 공격력 배수(1.0 = 100%).")]
    [SerializeField] private float damageMultiplier = 3f;
    [Tooltip("전체 타격을 반복하는 횟수.")]
    [SerializeField] private int hitCount = 1;

    // 적 전체가 대상이므로 MaxTargets 는 0(전체)으로 신고한다.
    public override SkillPowerProfile PowerProfile {
        get {
            int hits = Mathf.Max(1, hitCount);
            return new SkillPowerProfile(damageMultiplier * hits, hits, 0);
        }
    }

    protected override void OnExecute(SkillContext ctx) {
        var enemies = AliveEnemies(ctx);
        if (enemies.Count == 0) return;
        // 생존 적 각각에게 hitCount 회 타격(도달/즉발 시점의 생존 여부는 개별 타격에서 재확인).
        for (int i = 0; i < enemies.Count; i++)
            HitRepeated(ctx, enemies[i], damageMultiplier, Mathf.Max(1, hitCount));
    }
}
