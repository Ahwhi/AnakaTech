using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using static Enums;

/// <summary>
/// 플레이어 캐릭터 전투 유닛. 스폰포인트 부근에 자리 잡고 사거리 안의 가장 가까운 적을 기본 공격한다.
/// 스킬게이지가 가득 차면 다음 타격은 고유 스킬로 대체한다(PLANNING 1-18).
/// 사망 후 부활 여부/대기 시간은 전장(IBattleContext.ReviveDelayFor)이 정한다: 방치 던전은 설정된 초 뒤 부활,
/// 의뢰는 부활 없이 쓰러진 채 남아 전멸 판정 대상이 된다.
/// </summary>
public sealed class CharacterUnit : BattleUnit, ITransformableUnit {
    [Header("Character Refs")]
    [Tooltip("마스크 일러스트 표현부(일러스트/프레임/그림자/마스크/피격 깜빡임). 캐릭터/몬스터 공용 컴포넌트.")]
    [SerializeField] private BattleUnitIllustration illust;

    [Header("Team Marker (선택)")]
    [Tooltip("아군 표시 그룹(링/깃발 등). 아군/적이 함께 서는 전투(쟁탈전/대항전)에서 아군일 때만 켜진다.")]
    [SerializeField] private GameObject allyMarker;
    [Tooltip("적 표시 그룹. 아군/적이 함께 서는 전투에서 적일 때만 켜진다.")]
    [SerializeField] private GameObject enemyMarker;

    private Team _team = Team.Player;
    private bool _showTeamMarker;

    public override Team Team => _team;

    /// <summary>
    /// 이 캐릭터가 설 팀. 기본은 아군이며, 유저 대 유저 전투에서 상대 파티를 적 팀으로 세울 때만 바꾼다.
    /// 아웃핏마다 프리팹이 달라 "적 전용 프리팹"을 둘 수 없으므로, 적/아군 구분은 유닛 안의 표시 그룹이 맡는다.
    /// </summary>
    public void SetTeam(Team team) {
        _team = team;
        ApplyTeamMarker();
    }

    /// <summary>
    /// 팀 표시 그룹을 켤지. 양 팀이 모두 캐릭터인 전투만 켠다(몬스터와 싸우는 전투는 구분할 필요가 없어 끈다).
    /// </summary>
    public void ShowTeamMarker(bool show) {
        _showTeamMarker = show;
        ApplyTeamMarker();
    }

    private void ApplyTeamMarker() {
        if (allyMarker != null) allyMarker.SetActive(_showTeamMarker && _team == Team.Player);
        if (enemyMarker != null) enemyMarker.SetActive(_showTeamMarker && _team != Team.Player);
    }

    public CharacterData Data { get; private set; }
    /// <summary>이 유닛의 외형(기본 일러스트 또는 장착 코스튬). 스폰할 때 전장이 정해 준다.</summary>
    public CharacterAppearance Appearance { get; private set; }
    public bool IsRepresentative { get; private set; }

    // 부활 상태(하단 HUD 오버레이가 구독해 "부활중 N초"를 표시, PLANNING 1-12).
    public bool IsReviving { get; private set; }
    public float ReviveRemaining { get; private set; }
    public event Action ReviveStateChanged;

    // 하단 전투 캐릭터 패널 슬롯 / 공유 체력바 오버레이가 폴링하는 실시간 비율(PLANNING 2-4).
    // HpRatio/HasGauge/GaugeRatio 는 BattleUnit(IUnitBar)에서 상속한다(스킬 쿨타임 진행도).

    private IBattleRuntime _skillRuntime;
    private Vector3 _spawnPoint;
    private Coroutine _reviveRoutine;

    // 각인(조건부 효과). 항상 적용분은 이미 기준 스탯에 합쳐져 들어오므로 여기서는 조건부만 다룬다.
    // 조건이 켜지고 꺼질 때만 스탯을 다시 조립하므로(RecomposeStats) 매 프레임 비용은 조건 검사뿐이다.
    private const int ConditionMaskCapacity = 32;   // 마스크가 int 라 조건 수 상한(각인 슬롯 수 x 각인당 조건부 효과 수).

    private EngravingLoadout _engravings = EngravingLoadout.Empty;
    private float _battleElapsed;        // 스폰/부활 이후 경과 시간(전투 시작 조건 판정용)
    private int _activeConditionMask;    // 현재 켜져 있는 조건부 효과 비트마스크(Conditionals 인덱스 기준)

    // 변신 상태(ITransformableUnit). 베이스 스탯 스냅샷을 기준으로 강화를 재계산해 재시전 시 중첩되지 않는다.
    private bool _transformed;
    private float _transformRemaining;
    private CombatStats _transformBaseStats;
    private System.Collections.Generic.IReadOnlyList<TransformStatBuff> _transformBuffs;
    private GameObject _transformImpactVfxOverride;
    // 변신 중 기본공격이 거는 상태이상(원래 공격 설정 + 변신이 얹은 추가분의 합본).
    // 변신을 시작할 때 한 번 만들어 두고 공격마다 그대로 넘긴다(타격할 때마다 목록을 새로 만들지 않는다).
    private StatusEffectSpec[] _transformAttackStatuses;
    private Sprite _transformSprite;
    private Vector2 _transformFocus;

    /// <summary>현재 표시용 초상 스프라이트(변신 중이면 변신 일러스트, 아니면 외형이 정한 그림). 유닛이 외형의 단일 진실 원천이며,
    /// UI(전투 캐릭터 슬롯 등)는 이 값을 폴링해 변화를 감지한다(부활 상태 폴링과 동일한 패턴).
    /// 코스튬은 Appearance 안에서 이미 해석되므로 여기서 따로 갈라 볼 것이 없다.</summary>
    public Sprite PortraitSprite =>
        _transformed && _transformSprite != null ? _transformSprite : Appearance.Illustration;

    /// <summary>현재 표시용 초상 focus(PortraitSprite 와 짝. 변신 중이면 변신 focus, 아니면 외형의 UiFocus).</summary>
    public Vector2 PortraitFocus =>
        _transformed && _transformSprite != null ? _transformFocus : Appearance.UiFocus;

    /// <param name="appearance">표시할 외형(기본 일러스트 또는 장착 코스튬). 원본 캐릭터도 여기서 꺼내 쓴다.</param>
    /// <param name="limitBroken">한계돌파 여부. 쓸 스킬(기본/한계돌파)이 이 값으로 정해지며, 스킬 주기 계산(BaseInit)보다 먼저 정해져야 한다.</param>
    public void Init(CharacterAppearance appearance, CombatStats stats, bool limitBroken, bool isRepresentative,
                     Vector3 spawnPoint, int slot, IBattleContext ctx, IBattleRuntime skillRuntime, BattleSettings settings) {
        Appearance = appearance;
        Data = appearance.Character;
        _limitBroken = limitBroken;
        ApplyTeamMarker();   // 프리팹에 켜진 채 저장돼 있어도 시작 상태(꺼짐 또는 팀에 맞게)로 맞춘다.
        IsRepresentative = isRepresentative;
        _skillRuntime = skillRuntime;
        _spawnPoint = spawnPoint;

        transform.position = spawnPoint;
        if (body != null) body.localPosition = Vector3.zero;

        if (illust != null) {
            illust.Apply(appearance.BattleSprite, appearance.UiFocus);
            illust.IsolateMask(slot, transform);
        }

        BaseInit(stats, ctx, settings);   // 스킬 쿨타임 초기화까지 포함한다.
        IsReviving = false;
        ReviveRemaining = 0f;
        _transformed = false;
        _transformRemaining = 0f;
        _transformBuffs = null;
        _transformImpactVfxOverride = null;
        _transformAttackStatuses = null;
        _transformSprite = null;
        ResetEngravingConditions();
    }

    /// <summary>
    /// UI 미리보기 전용 초기화. 전투에 참가하지 않고 모습(초상 + 프리팹 꾸밈)만 세운다.
    /// 전투 로직(Update)을 끄므로 공격/이동/체력 처리가 돌지 않고, 전장 등록도 하지 않는다.
    /// </summary>
    public void InitPreview(CharacterAppearance appearance, int maskSlot) {
        Appearance = appearance;
        Data = appearance.Character;
        enabled = false;
        ApplyTeamMarker();
        if (body != null) body.localPosition = Vector3.zero;
        if (illust != null) {
            illust.Apply(appearance.BattleSprite, appearance.UiFocus);
            illust.IsolateMask(maskSlot, transform);
        }
    }

    /// <summary>
    /// 표시 외형을 교체한다(코스튬 변경 등). 변신 중이면 변신이 끝난 뒤 이 외형으로 돌아온다.
    /// </summary>
    public void ApplyAppearance(CharacterAppearance appearance) {
        if (appearance.Character == null) return;
        // 실제로 달라졌을 때만 갈아끼운다. 스탯 갱신은 전투 중 자주 일어나는데(레벨업 등) 그때마다
        // 스프라이트를 다시 적용하면 피격 깜빡임 트윈이 끊긴다.
        if (appearance.Character == Appearance.Character && appearance.CostumeId == Appearance.CostumeId) return;
        Appearance = appearance;
        // 변신 중에는 변신 일러스트가 우선이므로 지금 갈아끼우지 않는다(EndTransform 이 새 외형으로 복귀시킨다).
        if (!_transformed && illust != null) illust.Apply(appearance.BattleSprite, appearance.UiFocus);
    }

    /// <summary>
    /// 이 캐릭터의 각인 로드아웃을 설정한다(조건부 효과만 사용). 전장이 파티를 만들 때/각인이 바뀔 때 호출한다.
    /// 항상 적용 효과는 이미 기준 스탯(BuildCharacterStats)에 포함되어 들어오므로 여기서 다시 더하지 않는다.
    /// </summary>
    public void SetEngravings(EngravingLoadout loadout) {
        _engravings = loadout ?? EngravingLoadout.Empty;
        ResetEngravingConditions();
        RecomposeStats();
    }

    // 조건 상태를 처음부터 다시 잡는다(스폰/부활/각인 변경 시). 전투 시작 조건이 다시 켜진다.
    private void ResetEngravingConditions() {
        _battleElapsed = 0f;
        _activeConditionMask = 0;

        var list = _engravings.Conditionals;
        int count = Mathf.Min(list.Count, ConditionMaskCapacity);
        for (int i = 0; i < count; i++)
            if (IsConditionActive(list[i])) _activeConditionMask |= 1 << i;
    }

    // 행동이 막혀 있어도(기절) 흘러야 하는 시간. 여기 두지 않으면 기절당한 동안 변신이 풀리지 않고
    // 전투 시작 조건 각인도 멈춰, 상태이상에 걸린 쪽이 오히려 이득을 본다.
    protected override void TickTimers(float dt) {
        // 변신 지속시간 소진 -> 원래 모습/스탯으로 복귀.
        if (_transformed) {
            _transformRemaining -= dt;
            if (_transformRemaining <= 0f) EndTransform();
        }

        if (_engravings.HasConditionals) UpdateEngravingConditions(dt);
    }

    // 직접 조작 입력. 비어 있으면(기본) 아래의 자동 전투 행동을 그대로 쓴다.
    private IUnitMoveInput _moveInput;

    /// <summary>
    /// 직접 조작 입력을 연결한다(null 이면 자동 전투로 돌아간다). 연결되면 이동은 입력이 정하고,
    /// 공격은 사거리 안의 가장 가까운 적에게만 자동으로 한다(적을 쫓아가거나 스폰 위치로 돌아가지 않는다).
    /// 영웅전처럼 유저가 소녀를 움직이는 컨텐츠만 부르며, 다른 전투는 이 경로를 타지 않는다.
    /// </summary>
    public void SetMoveInput(IUnitMoveInput input) => _moveInput = input;

    /// <summary>현재 사거리(월드 units, 몸통 표면 기준). 사거리 표시처럼 화면이 범위를 그릴 때 읽는다.</summary>
    public float AttackRange => _stats.Range;

    protected override void Tick(float dt) {
        if (_moveInput != null) { TickManual(dt); return; }

        var target = FindNearestEnemy();

        // 탐지: 탐지 범위(0 이하면 무제한) 안에 가장 가까운 적이 있으면 교전 대상으로 삼는다.
        // 거리는 몸통 표면 기준이라 덩치 큰 적의 중심까지 파고들지 않는다.
        float dist = DistanceTo(target);
        bool detected = target != null && (Settings.CharacterDetectRange <= 0f || dist <= Settings.CharacterDetectRange);

        if (!detected) {
            // 탐지된 적이 없으면 스폰(대형) 위치로 복귀한다.
            if (Settings.CharacterReturnToSpawn) ReturnToSpawn(dt);
            return;
        }

        FaceTowards(target.Position);

        // 사거리 안: 공격(스킬 쿨타임이 차 있으면 스킬로 대체).
        if (dist <= _stats.Range) {
            if (CanAttack) PerformAttackOrSkill(target);
            return;
        }

        // 사거리 밖: 리쉬(스폰에서 최대 전진 거리) 안이면 접근, 리쉬 끝이면 그 자리에서 대기(적이 다가오길 기다림).
        float fromSpawn = Vector2.Distance((Vector2)transform.position, (Vector2)_spawnPoint);
        bool withinLeash = Settings.CharacterMaxAdvance <= 0f || fromSpawn < Settings.CharacterMaxAdvance;
        if (withinLeash) ApproachUnit(target, dt);
    }

    // 직접 조작: 입력 방향으로 걷고, 사거리 안에 적이 있으면 걸으면서 자동으로 공격한다.
    // 바라보는 방향은 공격 대상이 있으면 대상 쪽, 없으면 걷는 쪽이다.
    private void TickManual(float dt) {
        Vector2 dir = _moveInput.Direction;
        bool moving = dir.sqrMagnitude > 0.0001f;
        if (moving) MoveAlong(dir, dt);

        var target = FindNearestEnemy();
        if (target != null && DistanceTo(target) <= _stats.Range) {
            FaceTowards(target.Position);
            if (CanAttack) PerformAttackOrSkill(target);
            return;
        }
        if (moving) FaceTowards((Vector2)transform.position + dir);
    }

    // 탐지된 적이 없을 때 스폰(대형) 위치로 걸어서 복귀한다(도착하면 멈춘다).
    private void ReturnToSpawn(float dt) {
        if (Vector2.Distance((Vector2)transform.position, (Vector2)_spawnPoint) <= 0.05f) return;
        FaceTowards(_spawnPoint);
        MoveTowards(_spawnPoint, dt);
    }

    // ── 데이터/런타임 연결(BattleUnit 공통 공격/스킬 파이프라인이 참조) ──
    // 기본공격/스킬은 전부 SO 에 위임하고, 여기서는 "어느 데이터에서 가져오는지"만 알려준다.
    protected override BasicAttack BasicAttackAsset => Data != null ? Data.BasicAttack : null;
    protected override UnitSkill SkillAsset => Data != null ? Data.SkillFor(_limitBroken) : null;

    // 한계돌파 여부. 쓸 스킬을 고르는 기준이며, 전투 중 돌파하면 SetLimitBroken 으로 바뀐다.
    private bool _limitBroken;

    /// <summary>
    /// 전투 중 한계돌파 상태가 바뀌었을 때 쓸 스킬을 갈아끼운다. 스킬 주기는 이어서 부르는 ApplyStats 가
    /// 새 스킬 기준으로 다시 계산한다(차오른 게이지 비율은 유지된다). 그래서 반드시 ApplyStats 보다 먼저 부른다.
    /// </summary>
    public void SetLimitBroken(bool limitBroken) => _limitBroken = limitBroken;
    protected override IBattleRuntime Runtime => _skillRuntime;
    protected override Color SkillBubbleColor => Data != null ? Data.PersonalColor : Color.white;

    // 변신 중이면 기본공격 임팩트 VFX 를 변신 전용으로 교체(BattleUnit 이 AttackContext 로 전달).
    protected override GameObject AttackImpactVfxOverride => _transformImpactVfxOverride;

    // ── 변신 (ITransformableUnit) ────────────────────
    /// <summary>변신 적용: 일러스트 교체 + 베이스 대비 % 스탯 강화 + 기본공격 임팩트 오버라이드.
    /// 이미 변신 중이면 지속시간만 spec.Duration 으로 원복한다(스탯은 항상 베이스 스냅샷 기준 재계산이라 중첩 없음).</summary>
    public void ApplyTransform(in UnitTransformSpec spec) {
        if (!_alive) return;

        // 첫 변신 시점의 스탯을 베이스로 스냅샷(재시전/다른 변신에도 이 기준으로 재계산).
        if (!_transformed) {
            _transformBaseStats = _stats;
            _transformed = true;
        }

        // 버프 목록을 들고 있어야 변신 중 성장/장비 변경이 들어와도 새 기준으로 다시 얹을 수 있다(ResolveStats).
        _transformBuffs = spec.Buffs;
        _stats = ComposeTransformed(_transformBaseStats);

        _transformRemaining = Mathf.Max(0.01f, spec.Duration);
        _transformImpactVfxOverride = spec.AttackImpactVfxOverride;
        _transformAttackStatuses = ComposeAttackStatuses(spec.AttackStatusEffects);
        _transformSprite = spec.Sprite;
        _transformFocus = spec.SpriteFocus;

        if (illust != null && spec.Sprite != null) illust.Apply(spec.Sprite, spec.SpriteFocus);
    }

    /// <summary>변신 중이면 합본을, 아니면 null 을 돌려준다(공격 SO 의 설정을 그대로 쓴다).</summary>
    protected override IReadOnlyList<StatusEffectSpec> AttackStatusOverride => _transformAttackStatuses;

    // 변신 중 기본공격이 걸 상태이상 합본. 원래 공격이 걸던 것은 그대로 두고 변신 추가분을 얹는다
    // (변신했다고 원래 붙어 있던 효과가 사라지면 변신이 손해가 되는 경우가 생긴다).
    // 추가분이 없으면 null 을 돌려주어 공격 SO 의 설정이 그대로 쓰이게 한다.
    private StatusEffectSpec[] ComposeAttackStatuses(IReadOnlyList<StatusEffectSpec> extra) {
        if (extra == null || extra.Count == 0) return null;

        var own = BasicAttackAsset != null ? BasicAttackAsset.StatusEffects : null;
        int ownCount = own != null ? own.Count : 0;

        var merged = new StatusEffectSpec[ownCount + extra.Count];
        for (int i = 0; i < ownCount; i++) merged[i] = own[i];
        for (int i = 0; i < extra.Count; i++) merged[ownCount + i] = extra[i];
        return merged;
    }

    // 변신 버프를 기준 스탯 위에 얹는다. 항상 베이스에서 다시 계산하므로 재시전/스탯 변경에도 중첩되지 않는다.
    // 각 스탯을 "실효 N배"로 만든다(bonusMultiplier = 최종 배수, 3 = 3배).
    // 공속/이속/스킬가속은 "1 + 스탯/100" 배율이라 실효값이 N배가 되도록 변환하고(미성장 0 스탯도 정확히 N배),
    // 그 외(공격력 등)는 값 자체를 N배로 한다.
    private CombatStats ComposeTransformed(CombatStats baseStats) {
        var s = baseStats;
        if (_transformBuffs == null) return s;

        for (int i = 0; i < _transformBuffs.Count; i++) {
            var b = _transformBuffs[i];
            float baseVal = baseStats.Get(b.stat);
            float target = CombatStats.EffectiveScaled(b.stat, baseVal, b.bonusMultiplier);
            s = s.WithAdded(b.stat, target - baseVal);
        }
        return s;
    }

    // 기준 스탯 위에 유닛의 현재 상태 보정을 얹는다: 조건부 각인 -> 변신 순서.
    // 변신 중에 성장/장비로 기준 스탯이 바뀌면 베이스 스냅샷도 새 값으로 교체한다.
    // 스냅샷을 갱신하지 않으면 변신이 끝나는 순간 변경 전 스탯으로 되돌아간다(즉시 반영이 사라지는 버그).
    protected override CombatStats ResolveStats(CombatStats baseStats) {
        var stats = ComposeEngraved(baseStats);
        if (!_transformed) return stats;
        _transformBaseStats = stats;
        return ComposeTransformed(stats);
    }

    // 현재 켜져 있는(마스크) 조건부 각인 효과만 더한다. 항상 베이스에서 다시 계산하므로 중첩되지 않는다.
    private CombatStats ComposeEngraved(CombatStats baseStats) {
        if (_activeConditionMask == 0) return baseStats;

        var stats = baseStats;
        var list = _engravings.Conditionals;
        int count = Mathf.Min(list.Count, ConditionMaskCapacity);
        for (int i = 0; i < count; i++) {
            if ((_activeConditionMask & (1 << i)) == 0) continue;
            var c = list[i];
            stats = stats.WithAdded(c.Stat, c.Value);
        }
        return stats;
    }

    // 조건 1개가 지금 켜져 있는지. 새 조건을 추가할 때는 이 switch 와 EngravingTrigger 만 손보면 된다.
    private bool IsConditionActive(in EngravingLoadout.Conditional c) => c.Trigger switch {
        EngravingTrigger.BattleStart => _battleElapsed < c.Param,
        EngravingTrigger.SelfHpBelow => HpRatio * 100f <= c.Param,
        _ => false
    };

    // 조건들의 켜짐 상태를 비트마스크로 계산해, 마스크가 실제로 바뀐 프레임에만 스탯을 다시 조립한다.
    // 마스크로 비교하는 이유: 임계값이 서로 다른 조건(예: 체력 50% 이하 / 30% 이하)이 섞여 있어도
    // 각 조건의 전환 시점을 개별적으로 잡아낸다("하나라도 켜졌는가"로 판정하면 낮은 임계값이 영영 안 켜진다).
    private void UpdateEngravingConditions(float dt) {
        _battleElapsed += dt;

        int mask = 0;
        var list = _engravings.Conditionals;
        int count = Mathf.Min(list.Count, ConditionMaskCapacity);
        for (int i = 0; i < count; i++)
            if (IsConditionActive(list[i])) mask |= 1 << i;

        if (mask == _activeConditionMask) return;
        _activeConditionMask = mask;
        RecomposeStats();
    }

    // 변신 종료: 스탯/일러스트/임팩트 오버라이드를 원래대로 되돌린다(지속시간 소진/사망/강제 리셋 공통).
    private void EndTransform() {
        if (!_transformed) return;
        _transformed = false;
        _transformRemaining = 0f;
        _stats = _transformBaseStats;
        _transformBuffs = null;
        _transformImpactVfxOverride = null;
        _transformAttackStatuses = null;
        _transformSprite = null;
        if (illust != null && Data != null) illust.Apply(Appearance.BattleSprite, Appearance.UiFocus);
    }


    // 기본공격 시 유닛 몸체 반응: 반동(총잡이 + 반동을 켠 캐릭터) + 대표 카메라 흔들림.
    protected override void OnAttackReactions(BattleUnit target) {
        // 반동(PLANNING 1-15). 대상 판정은 CharacterData.HasAttackRecoil 한 곳에서 한다.
        if (Data.HasAttackRecoil && body != null) {
            body.DOKill();
            body.localPosition = Vector3.zero;
            body.DOShakePosition(0.12f, new Vector3(0.12f, 0.03f, 0f), 20, 90f, false, true);
        }

        // 대표 캐릭터가 적을 타격하면 카메라 흔들림(PLANNING 1-10).
        if (IsRepresentative) Ctx.OnRepresentativeHitEnemy();
    }

    // 스킬 사용 시에도 대표 캐릭터면 카메라를 흔든다(기본공격과 동일 규칙, 반동은 기본공격 전용).
    protected override void OnSkillReactions(BattleUnit target) {
        if (IsRepresentative) Ctx.OnRepresentativeHitEnemy();
    }

    protected override void OnDamaged(float amount, ICombatUnit source) {
        // 넉백에 더해 스프라이트 알파를 깜빡이는 피격 연출(PLANNING 1-13).
        if (illust != null)
            illust.PlayHitBlink(Settings.HitBlinkAlphaFactor, Settings.HitBlinkDuration, Settings.HitBlinkLoops);
    }

    protected override void Die() {
        EndTransform();   // 죽으면 변신 해제(부활은 원래 모습/스탯으로).
        base.Die();
        if (illust != null) illust.StopBlink();
        // 몬스터와 달리 파괴하지 않고 숨긴다(PLANNING 1-12).
        // 체력바는 공유 오버레이가 BarActive(=_alive)로 자동 숨긴다.
        if (body != null) body.gameObject.SetActive(false);

        // 부활 여부/지연은 전장이 정한다(방치 던전=설정값, 의뢰=부활 없음).
        // 음수면 부활 예약을 하지 않고 그대로 쓰러진 상태로 남아 전멸 판정 대상이 된다.
        float delay = Ctx.ReviveDelayFor(this);
        if (delay < 0f) {
            IsReviving = false;
            ReviveRemaining = 0f;
            ReviveStateChanged?.Invoke();
            return;
        }

        if (_reviveRoutine != null) StopCoroutine(_reviveRoutine);
        _reviveRoutine = StartCoroutine(ReviveRoutine(delay));
    }

    private IEnumerator ReviveRoutine(float delaySeconds) {
        IsReviving = true;
        ReviveRemaining = delaySeconds;
        ReviveStateChanged?.Invoke();

        while (ReviveRemaining > 0f) {
            yield return null;
            ReviveRemaining -= Time.deltaTime;
            ReviveStateChanged?.Invoke();
        }

        Revive();
    }

    private void Revive() {
        IsReviving = false;
        ReviveRemaining = 0f;
        _reviveRoutine = null;

        // 살아 있는 채로 불리는 경로(ForceReset)도 있으므로 이전 전투의 무적을 여기서도 정리한다.
        EndInvulnerability();
        CancelDash();
        transform.position = _spawnPoint;
        if (body != null) {
            body.localPosition = Vector3.zero;
            body.gameObject.SetActive(true);
        }
        if (illust != null) illust.StopBlink();

        _hp = _maxHp;
        _alive = true;
        ResetSkillCooldown();
        // 부활은 다시 전투에 참가하는 것이므로 "전투 시작" 계열 각인 조건도 처음부터 다시 켠다.
        ResetEngravingConditions();
        RecomposeStats();

        Ctx.PlaySfx(SfxKey.Revive);
        ReviveStateChanged?.Invoke();
    }

    /// <summary>
    /// 쓰러진 소녀를 그 자리에서 즉시 되살린다(부활 대기 없이). 스폰포인트로 돌아가고 체력이 가득 찬다.
    /// 살아 있으면 아무 일도 하지 않는다.
    ///
    /// 부활이 없는 전장(ReviveDelayFor 가 음수)에서도 동작한다. 그 규칙은 "시간이 지나면 저절로 일어나는가"를
    /// 정하는 것이지, 외부 효과가 되살리는 것까지 막는 것이 아니다(멸망전의 부활 증강 등).
    /// </summary>
    public void ReviveNow() {
        if (IsAlive) return;
        if (_reviveRoutine != null) { StopCoroutine(_reviveRoutine); _reviveRoutine = null; }
        Revive();
    }

    /// <summary>디렉터가 던전(런) 재시작 시 강제로 스폰포인트에 되살릴 때 사용.</summary>
    public void ForceReset(Vector3 spawnPoint) {
        EndTransform();   // 재시작 시 변신 상태를 정리(스탯/일러스트 원복).
        _spawnPoint = spawnPoint;
        if (_reviveRoutine != null) { StopCoroutine(_reviveRoutine); _reviveRoutine = null; }
        Revive();
    }
}
