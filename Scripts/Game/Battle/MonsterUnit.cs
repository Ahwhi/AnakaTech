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

    // 컨텐츠가 이동 방식을 바꿀 때의 입력(영웅전의 떠돌기 등). 비어 있으면(기본) 아래의 추적 행동 그대로다.
    private IUnitMoveInput _moveInput;

    /// <summary>
    /// 이동을 입력에 맡긴다(null 이면 기본 추적으로 돌아간다). 연결되면 대상을 쫓지 않고 입력대로 움직이며,
    /// 사거리 안에 대상이 들어올 때만 멈추지 않고 공격한다. 영웅전처럼 탄을 피하는 컨텐츠만 부른다.
    /// </summary>
    public void SetMoveInput(IUnitMoveInput input) => _moveInput = input;

    protected override void Tick(float dt) {
        if (_moveInput != null) { TickByInput(dt); return; }

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

    // 공격이 요구한 정지 시간(예고 중 등)이 남았는지. 입력 이동에서만 쓴다.
    private float _castLock;

    // 입력대로 움직이면서, 사거리 안의 대상에게는 걸음을 멈추지 않고 공격한다.
    // 다만 공격이 정지 시간을 요구하면(예고 후 발사) 그동안은 제자리에 선다. 발사 위치가 예고한 위치와 같아야 하기 때문이다.
    private void TickByInput(float dt) {
        if (_castLock > 0f) { _castLock -= dt; return; }

        Vector2 dir = _moveInput.Direction;
        bool moving = dir.sqrMagnitude > 0.0001f;
        if (moving) MoveAlong(dir, dt);

        var target = ResolveTarget();
        if (target != null && DistanceTo(target) <= _stats.Range) {
            FaceTowards(target.Position);
            if (CanAttack) {
                PerformAttackOrSkill(target);
                var attack = BasicAttackAsset;
                if (attack != null) _castLock = attack.CastLockSeconds;
            }
            return;
        }
        if (moving) FaceTowards((Vector2)transform.position + dir);
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

    // 컨텐츠가 공격 방식을 통째로 바꿀 때의 기본공격(영웅전의 논타겟 공격 등). 비어 있으면 데이터의 공격을 쓴다.
    private BasicAttack _attackOverride;
    private bool _skillSuppressed;

    /// <summary>
    /// 이 몬스터의 기본공격을 컨텐츠 규칙으로 바꾼다(스폰 직후 1회). 몬스터 데이터를 컨텐츠마다 복제하지 않고
    /// 같은 몬스터를 다른 공격 방식으로 세우기 위한 자리다. suppressSkill 이면 데이터의 스킬도 쓰지 않는다
    /// (스킬은 대상 지정형이라, 공격 방식 전체를 바꾸는 컨텐츠에서는 규칙이 섞인다).
    /// </summary>
    public void SetAttackOverride(BasicAttack attack, bool suppressSkill) {
        _attackOverride = attack;
        _skillSuppressed = suppressSkill;
        ResetSkillCooldown();   // 스킬 유무가 바뀌었으므로 게이지 주기를 다시 잡는다.
    }

    // ── 데이터/런타임 연결(BattleUnit 공통 공격/스킬 파이프라인이 참조) ──
    // 캐릭터와 동일한 구조다: 몬스터도 연출/데미지를 전부 SO 에 위임하고 여기서는 출처만 알려준다.
    protected override BasicAttack BasicAttackAsset =>
        _attackOverride != null ? _attackOverride : (Data != null ? Data.BasicAttack : null);
    protected override UnitSkill SkillAsset => !_skillSuppressed && Data != null ? Data.Skill : null;
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

    // 몬스터는 죽으면 곧바로 파괴된다(BattleStage.OnUnitDied). 막타가 건 깜빡임 트윈은 파괴보다 오래 이어지므로
    // 여기서 정리하지 않으면 파괴된 렌더러를 건드리다 DOTween 안전 모드의 예외 처리로 끝난다(몬스터 1마리당 최대 3회).
    protected override void Die() {
        base.Die();
        if (illust != null) illust.StopBlink();
    }
}
