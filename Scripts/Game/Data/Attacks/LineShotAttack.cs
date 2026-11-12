using UnityEngine;
using static Enums;

/// <summary>
/// 논타겟 직선 기본공격. 대상 쪽으로 공격 경로(직선)를 잠시 예고한 뒤, 그 길을 따라 발사체를 곧게 쏜다.
/// 발사체는 대상을 쫓지 않으므로 예고를 보고 비켜서면 맞지 않는다(직접 조작 컨텐츠용, 영웅전의 몬스터 공격).
///
/// 경로는 공격을 시작한 순간의 시전자 위치와 대상 위치로 정해지고 그 뒤로 바뀌지 않는다.
/// 예고 중에 시전자가 쓰러지면 공격은 나가지 않으며, 날아가던 발사체도 시전자와 함께 사라진다.
/// 데미지 규칙은 다른 기본공격과 같다(방어/지터/치명타/상태이상). 판정은 발 위치(유닛 Position)와 몸통 반경 기준이다.
/// </summary>
[CreateAssetMenu(fileName = "LineShotAttack", menuName = "Anaka/Attacks/Line Shot")]
public sealed class LineShotAttack : BasicAttack {
    /// <summary>
    /// 같은 라인샷을 몬스터마다 조금씩 다르게 쓰기 위한 덮어쓰기 값(컨텐츠 데이터에 둔다). 0/음수 항목은 원본 값을 쓴다.
    /// SO 를 몬스터 수만큼 복제하지 않고 컨텐츠 DB 한 곳에서 발 수/속도/예고를 조정하기 위한 자리다.
    /// </summary>
    [System.Serializable]
    public sealed class Variant {
        [Tooltip("한 번에 쏘는 발 수. 0 이면 원본 값.")]
        [Min(0)] public int ShotCount;
        [Tooltip("부채꼴 전체 각도(도). Shot Count 를 덮어쓸 때만 쓴다.")]
        [Range(0f, 360f)] public float SpreadAngle = 30f;
        [Tooltip("발사체 속도. 0 이면 원본 값.")]
        [Min(0f)] public float ProjectileSpeed;
        [Tooltip("예고 시간(초). 음수면 원본 값, 0 이면 예고 없이 바로 쏜다.")]
        public float TelegraphSeconds = -1f;
        [Tooltip("경로 길이(날아가는 거리). 0 이면 원본 값.")]
        [Min(0f)] public float Length;

        [Tooltip("켜면 아래 상태이상으로 바꾼다(비워 두고 켜면 상태이상 없음). 끄면 원본 공격의 상태이상을 쓴다.")]
        public bool OverrideStatusEffects;
        [Tooltip("이 몬스터의 공격이 적중했을 때 거는 상태이상(Override Status Effects 를 켰을 때만).")]
        public StatusEffectSpec[] StatusEffects = System.Array.Empty<StatusEffectSpec>();

        public bool IsEmpty => ShotCount <= 0 && ProjectileSpeed <= 0f && TelegraphSeconds < 0f && Length <= 0f &&
                               !OverrideStatusEffects;
    }

    /// <summary>
    /// 덮어쓰기 값을 얹은 실행 중 전용 사본을 만든다(에셋으로 저장되지 않는다). 다 쓰면 만든 쪽이 Destroy 한다.
    /// </summary>
    public LineShotAttack CreateVariant(Variant v) {
        var copy = Instantiate(this);
        copy.name = name + " (Variant)";
        copy.hideFlags = HideFlags.DontSave;
        if (v == null) return copy;
        if (v.ShotCount > 0) { copy.shotCount = v.ShotCount; copy.spreadAngle = v.SpreadAngle; }
        if (v.ProjectileSpeed > 0f) copy.projectileSpeed = v.ProjectileSpeed;
        if (v.TelegraphSeconds >= 0f) copy.telegraphSeconds = v.TelegraphSeconds;
        if (v.Length > 0f) copy.length = v.Length;
        if (v.OverrideStatusEffects) copy.ReplaceStatusEffects(v.StatusEffects);
        return copy;
    }

    [Header("Damage")]
    [Tooltip("적중 시 데미지 배수(기본공격은 보통 1).")]
    [SerializeField] private float damageMultiplier = 1f;
    [Tooltip("켜면 경로 위의 적을 모두 맞히고, 끄면 처음 맞은 적에서 멈춘다.")]
    [SerializeField] private bool pierce;

    [Header("Path")]
    [Tooltip("경로 길이(월드 units). 시전자 위치에서 대상 쪽으로 이만큼 뻗는다.")]
    [SerializeField, Min(0.1f)] private float length = 8f;
    [Tooltip("경로 폭(월드 units). 예고 표시의 폭이자 발사체 판정 지름이다.")]
    [SerializeField, Min(0.05f)] private float width = 0.6f;
    [Tooltip("예고 시간(초). 경로를 보여 준 뒤 이 시간이 지나면 발사한다.")]
    [SerializeField, Min(0f)] private float telegraphSeconds = 0.6f;

    [Header("Volley")]
    [Tooltip("한 번에 쏘는 발 수. 2 이상이면 대상 방향을 가운데로 부채꼴로 퍼진다.")]
    [SerializeField, Min(1)] private int shotCount = 1;
    [Tooltip("부채꼴 전체 각도(도). 발 수가 1 이면 쓰지 않는다.")]
    [SerializeField, Range(0f, 360f)] private float spreadAngle = 30f;

    [Header("Prefabs")]
    [Tooltip("경로 예고 프리팹(LineTelegraph). 비우면 예고 없이 시간만 기다렸다 쏜다.")]
    [SerializeField] private LineTelegraph telegraphPrefab;
    [Tooltip("발사체 프리팹(LineProjectile). 루트의 +X 가 진행 방향이다.")]
    [SerializeField] private LineProjectile projectilePrefab;
    [Tooltip("발사체 속도(월드 units/초).")]
    [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;

    [Header("Fire / Impact")]
    [Tooltip("발사음. 비우면 공통 기본 공격음(SfxKey.Attack).")]
    [SerializeField] private AudioClip fireClip;
    [Tooltip("적중 지점 VFX. 선택.")]
    [SerializeField] private GameObject impactVfxPrefab;
    [Tooltip("적중 사운드. 선택.")]
    [SerializeField] private AudioClip impactClip;

    // 예고하는 동안 시전자가 움직이면 발사체가 시전자가 떠난 자리에서 나온다. 예고가 끝날 때까지 세워 둔다.
    public override float CastLockSeconds => telegraphSeconds;

    public override void Execute(AttackContext ctx) {
        if (!ctx.Target.IsTargetable() || !ctx.Caster.IsTargetable()) return;
        if (projectilePrefab == null) {
            Debug.LogError($"[LineShotAttack] 발사체 프리팹이 없습니다: {name}");
            return;
        }

        Vector2 origin = ctx.Caster.Position;
        Vector2 dir = ctx.Target.Position - origin;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        var caster = ctx.Caster;
        var runtime = ctx.Runtime;

        // 발마다의 방향. 1 발이면 대상 방향 그대로, 여러 발이면 대상 방향을 가운데로 고르게 퍼진다.
        int count = Mathf.Max(1, shotCount);
        var dirs = new Vector2[count];
        for (int i = 0; i < count; i++) {
            float offset = count == 1 ? 0f : spreadAngle * ((float)i / (count - 1) - 0.5f);
            dirs[i] = Rotate(dir, offset);
        }

        if (telegraphPrefab != null)
            foreach (var d in dirs)
                Instantiate(telegraphPrefab).Show(origin, d, length, width, telegraphSeconds, caster);

        float mult = damageMultiplier * ctx.Multiplier;
        var statuses = ctx.StatusEffects;
        var impactVfx = ctx.ResolveImpactVfx(impactVfxPrefab);

        // 예고가 끝나면 발사한다. 그 사이에 시전자가 쓰러졌으면 공격은 없던 일이다.
        runtime.Schedule(telegraphSeconds, () => {
            if (!caster.IsTargetable()) return;

            if (fireClip != null) runtime.PlaySfx(fireClip);
            else runtime.PlaySfx(SfxKey.Attack);

            foreach (var d in dirs)
                Instantiate(projectilePrefab).Launch(origin, d, length, projectileSpeed, width * 0.5f, pierce, caster, runtime,
                    target => {
                        target.TakeDamage(caster.Stats, mult, caster, statuses);
                        if (impactVfx != null) runtime.PlayVfx(impactVfx, target.Position);
                        if (impactClip != null) runtime.PlaySfx(impactClip);
                    });
        });
    }

    private static Vector2 Rotate(Vector2 v, float degrees) {
        float r = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
