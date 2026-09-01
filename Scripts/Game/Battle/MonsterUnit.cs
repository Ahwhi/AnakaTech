using UnityEngine;

/// <summary>
/// 몬스터 전투 유닛. 가장 가까운 생존 캐릭터로 이동하다가 사거리에 들면 공속 쿨타임마다 공격한다(PLANNING 1-21).
/// 체력이 0이 되면 즉시 처치(부활 없음, PLANNING 1-12). 디렉터가 사망 통지를 받아 제거/클리어 판정한다.
///
/// 공격/스킬은 캐릭터와 동일하게 데이터의 SO(MonsterData.BasicAttack / MonsterData.Skill)에 위임한다.
/// 스킬이 비어 있으면 기본공격만 하므로, 스킬을 주고 싶은 몬스터에만 SO 를 할당하면 된다.
/// </summary>
public sealed class MonsterUnit : BattleUnit {
    [Header("Monster Refs")]
    [Tooltip("마스크 일러스트 표현부(캐릭터와 공용 컴포넌트). 할당하면 MonsterData 의 Illustration 을 " +
             "BattleFocus 프레이밍으로 마스크 안에 보여준다. 미할당이면 BodyPrefab 자체 스프라이트만 사용.")]
    [SerializeField] private BattleUnitIllustration illust;

    public override Team Team => Team.Enemy;

    /// <summary>피격음 등 종류별 표현에서 몬스터로 취급된다(캐릭터와 다른 소리).</summary>
    public override bool IsMonster => true;

    public MonsterData Data { get; private set; }

    private IBattleRuntime _skillRuntime;

    public void Init(MonsterData data, IBattleContext ctx, IBattleRuntime skillRuntime, BattleSettings settings, int slot) {
        Data = data;
        _skillRuntime = skillRuntime;
        BaseInit(data.BaseStats, ctx, settings);   // 스킬 쿨타임 초기화까지 포함한다(Data 가 먼저 세팅되어야 한다).
        if (body != null) body.localPosition = Vector3.zero;

        // 표현부가 있으면 캐릭터와 동일한 마스크 일러스트로 표시한다. 프레이밍은 프리팹(창 규격)과
        // MonsterData.BattleFocus(개체별 관심점/크기)의 조합으로 결정되므로, 여러 몬스터가 같은 본체 프리팹을 공유해도 된다.
        // IsolateMask 로 이 몬스터만의 정렬 대역을 부여해, 인접 유닛(다른 몬스터/캐릭터) 스프라이트가 마스크에 섞이지 않게 한다.
        if (illust != null) {
            illust.Apply(data.Illustration, data.BattleFocus);
            illust.IsolateMask(slot);
        }
    }

    /// <summary>개체별 몸통 반경. 같은 BodyPrefab 을 크기만 다르게 쓰는 몬스터가 있으므로 데이터 값이 우선한다.</summary>
    public override float BodyRadius =>
        Data != null && Data.BodyRadius > 0f ? Data.BodyRadius : base.BodyRadius;

    protected override void Tick(float dt) {
        var target = ResolveTarget();
        if (target == null) return;

        FaceTowards(target.Position);

        // 거리는 몸통 표면 기준이다(덩치 큰 몬스터가 상대 중심까지 파고들지 않는다).
        if (DistanceTo(target) <= _stats.Range) {
            if (CanAttack) PerformAttackOrSkill(target);
        } else {
            ApproachUnit(target, dt);   // 사거리 밖이면 계속 쫓아간다(공용 추적 이동).
        }
    }

    // 이번 프레임의 공격 대상. 데이터가 우선 타게팅을 요구하면 전장에 물어보고, 없으면 가장 가까운 적을 쓴다.
    // "누가 우선 대상인가"는 컨텐츠(전장)가 알고 몬스터는 "우선 대상을 노리는 성향인가"만 안다.
    private BattleUnit ResolveTarget() {
        if (Data != null && Data.Targeting == MonsterTargeting.PriorityTarget) {
            var priority = Ctx.PriorityTargetFor(this);
            if (priority != null && priority.IsAlive) return priority;
        }
        return FindNearestEnemy();   // 적 = 캐릭터
    }

    // ── 데이터/런타임 연결(BattleUnit 공통 공격/스킬 파이프라인이 참조) ──
    // 캐릭터와 동일한 구조다: 몬스터도 연출/데미지를 전부 SO 에 위임하고 여기서는 출처만 알려준다.
    protected override BasicAttack BasicAttackAsset => Data != null ? Data.BasicAttack : null;
    protected override UnitSkill SkillAsset => Data != null ? Data.Skill : null;
    protected override IBattleRuntime Runtime => _skillRuntime;
    protected override Color SkillBubbleColor => Data != null ? Data.SkillBubbleColor : Color.white;

    protected override void OnDamaged(float amount, ICombatUnit source) {
        // 적 공통 피격 VFX(PLANNING 1-9).
        if (Settings.CommonEnemyHitVfxPrefab != null)
            Ctx.PlayVfx(Settings.CommonEnemyHitVfxPrefab, HitPoint);
        // 마스크 일러스트를 쓰는 몬스터는 캐릭터와 동일한 피격 깜빡임을 재생한다.
        if (illust != null)
            illust.PlayHitBlink(Settings.HitBlinkAlphaFactor, Settings.HitBlinkDuration, Settings.HitBlinkLoops);
    }
}
