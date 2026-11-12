using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using static Enums;

/// <summary>소속 팀. 적대 판정에 사용.</summary>
public enum Team { Player, Enemy }

/// <summary>
/// 전투 유닛 공통 베이스(캐릭터/몬스터). HP/공격쿨타임/스킬쿨타임/넉백/사망을 공통 처리하고,
/// 타게팅/이동 등 팀별 행동은 파생 클래스의 Tick 에서 구현한다. ICombatUnit 을 구현해 스킬과 디커플링된다.
/// (타입명이 UniRx.Unit 과 충돌하지 않도록 BattleUnit 으로 명명.)
///
/// 공격/스킬은 전부 데이터(SO)에 위임한다: 기본공격은 BasicAttack, 스킬은 UnitSkill 이며 파생 클래스는
/// 자기 데이터에서 그 SO 를 어디서 가져오는지만(BasicAttackAsset/SkillAsset) 알려준다.
/// 따라서 캐릭터든 몬스터든 데이터에 SO 를 할당하는 것만으로 동일한 공격/스킬 파이프라인을 탄다.
/// </summary>
[DisallowMultipleComponent]
public abstract class BattleUnit : MonoBehaviour, ICombatUnit, IUnitBar, IInvulnerableUnit {
    [Header("Unit Refs")]
    [Tooltip("스프라이트 루트. 넉백/반동/좌우 반전 대상(루트 이동과 분리).")]
    [SerializeField] protected Transform body;
    [Tooltip("데미지 팝업/피격 VFX 및 체력바가 뜰 기준점(비우면 머리 위 추정).")]
    [SerializeField] protected Transform hitAnchor;
    [Tooltip("발사/자기 연출(머즐/손) 기준점. 비우면 HitPoint(머리)를 사용. 원거리 무기의 발사 지점.")]
    [SerializeField] protected Transform muzzleAnchor;
    [Tooltip("몸통 반경(월드 units). 사거리/접근/겹침 판정에서 이 유닛이 차지하는 크기다. " +
             "0 이면 크기 없는 점으로 보고 중심 기준으로 판정한다(기존 동작). 덩치가 큰 유닛에 값을 주면 " +
             "적이 중심까지 파고들지 않고 몸통 앞에서 멈춰 공격한다. 씬 뷰에서 선택하면 원으로 표시된다.")]
    [SerializeField] private float bodyRadius;

    protected IBattleContext Ctx;
    protected BattleSettings Settings;

    protected CombatStats _stats;
    protected float _hp;
    protected float _maxHp;
    protected bool _alive;

    private float _attackCooldown;
    private float _knockbackCooldown;
    private float _skillCooldownTimer;   // 남은 스킬 쿨타임(초). 0 이하면 발동 준비 완료.
    private float _skillIntervalCache;   // 마지막으로 계산한 발동 주기(게이지바 진행 표시 기준).
    private bool _warnedMissingAttack;   // 기본공격 SO 미할당 경고를 유닛당 1회로 제한.

    /// <summary>사망 순간 통지(디렉터가 목록/부활/클리어 판정에 사용).</summary>
    public event Action<BattleUnit> Died;

    /// <summary>
    /// 공격에 직접 맞아 실제로 피해를 입은 순간 통지. 회피/무적으로 막은 피격과 지속 피해(출혈 등)의 틱은 오지 않는다.
    /// 전장 규칙이 "맞았을 때"에 반응할 때 쓴다(예: 영웅전 피격 시 남은 시간 감소).
    /// </summary>
    public event Action<BattleUnit> Hit;

    public abstract Team Team { get; }

    /// <summary>
    /// 몬스터인지. 피격음처럼 "무엇이 맞았는가"로 갈리는 표현에 쓴다(팀이 아니라 종류가 기준이라,
    /// 쟁탈전에서 적 팀으로 서는 상대 캐릭터도 캐릭터로 친다). 몬스터만 true 로 재정의한다.
    /// </summary>
    public virtual bool IsMonster => false;

    // ── ICombatUnit ─────────────────────────────
    public Vector2 Position => transform.position;
    public CombatStats Stats => _stats;
    public bool IsAlive => _alive;
    public float CurrentHp => _hp;
    public bool IsEnemyOf(ICombatUnit other) => other is BattleUnit u && u.Team != Team;

    /// <summary>
    /// 몸통 반경(월드 units). 유닛은 점이 아니라 이 반경의 원으로 취급되며, 사거리/접근/겹침 판정이
    /// 전부 이 반경만큼 부푼 몸통 표면 기준으로 이뤄진다. 0 이면 중심점 기준(크기 없는 유닛).
    /// 파생 클래스는 데이터에 개체별 값이 있으면 그것으로 덮어쓴다(같은 프리팹을 크기만 다르게 쓰는 몬스터 등).
    /// </summary>
    public virtual float BodyRadius => Mathf.Max(0f, bodyRadius);

    /// <summary>데미지 팝업/피격 VFX 기준 위치(머리 위). 다른 유닛도 대상 위치로 참조한다.</summary>
    public Vector2 HitPoint => hitAnchor != null ? (Vector2)hitAnchor.position
                                                 : (Vector2)transform.position + Vector2.up * 0.5f;

    /// <summary>발사/자기 연출 기준 위치(머즐/손). 지정 앵커가 없으면 HitPoint 로 폴백.</summary>
    public Vector2 MuzzlePoint => muzzleAnchor != null ? (Vector2)muzzleAnchor.position : HitPoint;

    // ── IUnitBar (공유 체력바 오버레이가 폴링) ─────────
    public Transform BarAnchor => hitAnchor != null ? hitAnchor : transform;
    public float HpRatio => _maxHp > 0f ? _hp / _maxHp : 0f;
    /// <summary>스킬이 할당된 유닛만 게이지 바를 노출한다(캐릭터/몬스터 공통).</summary>
    public bool HasGauge => SkillAsset != null;
    /// <summary>게이지 바 = 스킬 쿨타임 진행도(0=방금 사용, 1=발동 준비 완료). 시간 기반으로 채워진다.</summary>
    public float GaugeRatio =>
        _skillIntervalCache > 0f ? Mathf.Clamp01(1f - _skillCooldownTimer / _skillIntervalCache) : 0f;
    public bool BarActive => _alive;

    /// <summary>
    /// 체력을 회복한다(최대치를 넘지 않는다). 실제로 회복된 양을 돌려주므로 호출측이 그만큼만 표시할 수 있다.
    /// 죽은 유닛은 되살리지 않는다(부활은 이 게임에 없다).
    /// </summary>
    public float Heal(float amount) {
        if (!_alive || amount <= 0f) return 0f;

        float before = _hp;
        _hp = Mathf.Min(_maxHp, _hp + amount);
        return _hp - before;
    }

    /// <summary>최대 체력(회복량을 비율로 계산하는 쪽이 쓴다).</summary>
    public float MaxHp => _maxHp;

    /// <summary>
    /// 스킬 게이지를 지정 비율만큼 채운 상태로 시작한다(0=빈 게이지, 1=발동 준비 완료).
    /// 쿨타임 감소/스킬가속과는 별개로 "시작 지점"만 옮기는 것이라, 주기 자체는 그대로다.
    /// 스킬이 없는 유닛은 아무 일도 하지 않는다.
    /// </summary>
    public void PrimeSkillGauge(float ratio) {
        if (_skillIntervalCache <= 0f || ratio <= 0f) return;
        _skillCooldownTimer = _skillIntervalCache * Mathf.Clamp01(1f - ratio);
    }

    /// <summary>
    /// 스킬 게이지를 처음 상태(빈 게이지)로 되돌린다. 주기 자체는 건드리지 않는다.
    /// 멸망전이 페이즈를 넘길 때 쓴다. 체력과 전투불능은 그대로 이어지고 게이지만 다시 차오르기 시작한다.
    /// </summary>
    public void ResetSkillGauge() {
        if (_skillIntervalCache <= 0f) return;
        _skillCooldownTimer = _skillIntervalCache;
    }

    /// <summary>
    /// 외부에서 들어온 "기준 스탯"(성장 + 장비 + 항상 적용 각인 + 본부). 유닛 상태(변신/조건부 각인)에 의한 보정을
    /// 얹기 전 값이라, 상태가 바뀔 때마다 이 값에서 다시 조립하면 보정이 중첩되지 않는다(RecomposeStats).
    /// </summary>
    protected CombatStats BaseStats { get; private set; }

    // ── 상태이상 ─────────────────────────────────
    // 걸려 있는 상태이상 전부를 이 객체가 관리한다(지속시간/틱/VFX). 유닛은 "지금 기절인가" 같은 질의만 한다.
    private UnitStatusEffects _status;

    /// <summary>이 유닛에게 걸려 있는 상태이상.</summary>
    public UnitStatusEffects Status => _status ??= new UnitStatusEffects(this);

    // 이 유닛이 "거는" 상태이상의 지속시간 배수(종류별). 컨텐츠 보정(무한 타워 층 보너스 등)이 정하며 기본은 전부 1 이다.
    private Dictionary<StatusEffectType, float> _inflictDurationScales;

    /// <summary>이 유닛이 거는 그 종류 상태이상의 지속시간 배수. 정한 적이 없으면 1.</summary>
    public float InflictDurationScale(StatusEffectType type) =>
        _inflictDurationScales != null && _inflictDurationScales.TryGetValue(type, out var scale) ? scale : 1f;

    /// <summary>이 유닛이 거는 그 종류 상태이상의 지속시간 배수를 정한다(1 이면 보정 없음).</summary>
    public void SetInflictDurationScale(StatusEffectType type, float scale) {
        _inflictDurationScales ??= new Dictionary<StatusEffectType, float>();
        _inflictDurationScales[type] = Mathf.Max(0f, scale);
    }

    /// <summary>상태이상 VFX 가 붙을 자리. 몸체가 있으면 몸체(넉백/반동을 함께 타도록), 없으면 유닛 자신.</summary>
    public Transform StatusVfxAnchor => body != null ? body : transform;

    /// <summary>상태이상 종류별 지속 표시 VFX(전투 설정이 배선한다). 미배선이면 null.</summary>
    public GameObject StatusVfxPrefab(StatusEffectType type) =>
        Settings != null ? Settings.StatusVfxPrefab(type) : null;

    /// <summary>유닛 상태가 바뀌었을 때 기준 스탯에서 최종 스탯을 다시 조립한다(체력 비율/스킬 진행률 보존).</summary>
    protected void RecomposeStats() => ApplyStats(BaseStats);

    /// <summary>공통 초기화. 파생 클래스 Init 에서 스탯 조립 후 호출한다(데이터는 그 전에 세팅해야 스킬 주기가 잡힌다).</summary>
    protected void BaseInit(CombatStats stats, IBattleContext ctx, BattleSettings settings) {
        BaseStats = stats;
        _stats = stats;
        Ctx = ctx;
        Settings = settings;
        _maxHp = Mathf.Max(1f, stats.Health);
        _hp = _maxHp;
        _alive = true;
        _attackCooldown = 0f;
        _knockbackCooldown = 0f;
        EndInvulnerability();
        ResetSkillCooldown();   // 스탯/설정이 준비된 뒤에 계산해야 하므로 마지막에 호출한다.
    }

    /// <summary>
    /// 최대/현재 체력을 직접 지정한다. 체력이 유닛 데이터가 아니라 바깥 상태에서 오는 경우에 쓴다
    /// (길드 레이드 보스처럼 서버가 들고 있는 체력을 이어받아 때리는 컨텐츠).
    /// 비율을 유지하는 ApplyStats 와 달리 값을 그대로 덮으므로, 스폰 직후에만 부르는 것을 전제로 한다.
    /// </summary>
    public void OverrideHealth(float maxHp, float currentHp) {
        _maxHp = Mathf.Max(1f, maxHp);
        _hp = Mathf.Clamp(currentHp, 0f, _maxHp);
        _alive = _hp > 0f;
    }

    /// <summary>런 진행 중 성장/장비 변경으로 최종 스탯이 바뀌었을 때 살아있는 유닛에 즉시 반영한다(PLANNING 3-3).
    /// 최대 체력이 바뀌면 현재 체력은 비율을 유지하고(장비를 갈아끼웠다고 갑자기 위태로워지지 않게),
    /// 스킬 주기가 바뀌면 차오른 진행률을 유지한다(게이지가 튀지 않게).
    /// 유닛 상태(변신 등)에 의한 보정은 ResolveStats 가 담당한다.</summary>
    public void ApplyStats(CombatStats stats) {
        if (Settings == null) return;   // 아직 초기화 전(BaseInit 이 스탯을 넣는다).

        float hpRatio = _maxHp > 0f ? Mathf.Clamp01(_hp / _maxHp) : 1f;
        float skillProgress = _skillIntervalCache > 0f ? Mathf.Clamp01(_skillCooldownTimer / _skillIntervalCache) : 0f;

        BaseStats = stats;
        _stats = ResolveStats(stats);

        _maxHp = Mathf.Max(1f, _stats.Health);
        // 죽어 있는(부활 대기) 유닛은 체력을 건드리지 않는다. 부활 시 새 최대치로 가득 채워진다.
        if (_alive) _hp = Mathf.Clamp(_maxHp * hpRatio, 1f, _maxHp);

        _skillIntervalCache = ComputeSkillInterval();
        _skillCooldownTimer = _skillIntervalCache * skillProgress;
    }

    /// <summary>외부에서 들어온 기준 스탯을 유닛의 현재 상태에 맞는 최종 스탯으로 만든다. 기본은 그대로 사용.</summary>
    protected virtual CombatStats ResolveStats(CombatStats baseStats) => baseStats;

    protected virtual void Update() {
        if (!_alive) return;
        float dt = Time.deltaTime;

        // 상태이상 시간은 기절 중에도 흘러야 한다(기절이 스스로 풀리지 않으면 영구 기절이 된다).
        if (_status != null) _status.Tick(dt);
        TickInvulnerability(dt);

        if (_attackCooldown > 0f) _attackCooldown -= dt;
        if (_knockbackCooldown > 0f) _knockbackCooldown -= dt;
        // 스킬 쿨타임은 교전 여부와 무관하게 시간으로 흐른다(시간 기반 발동).
        // 냉각 상태에서는 이 흐름이 멈춘다 - 그만큼 다음 스킬이 늦어진다.
        if (_skillCooldownTimer > 0f && !IsChilled) _skillCooldownTimer -= dt;

        // 행동과 무관하게 흘러야 하는 시간(변신 지속, 조건부 각인 등)은 기절 중에도 진행시킨다.
        // 여기 두지 않으면 기절당한 동안 변신이 풀리지 않아 기절이 오히려 이득이 된다.
        TickTimers(dt);

        // 기절 중에는 행동(이동/타게팅/공격)을 하지 않는다. 쿨타임은 위에서 이미 흘렀다.
        if (IsStunned) return;
        Tick(dt);
    }

    /// <summary>
    /// 행동 여부와 무관하게 흘러야 하는 시간. 기절 중에도 호출된다.
    /// 지속시간이 걸린 자기 상태(변신/조건부 효과 등)를 여기서 갱신한다.
    /// </summary>
    protected virtual void TickTimers(float dt) { }

    /// <summary>기절해 행동할 수 없는 상태인지. 파생 유닛도 자기 행동을 막을 때 참고할 수 있다.</summary>
    protected bool IsStunned => _status != null && _status.IsStunned;

    /// <summary>냉각으로 스킬 쿨타임이 멈춰 있는지.</summary>
    protected bool IsChilled => _status != null && _status.IsChilled;

    /// <summary>팀별 행동(타게팅/이동/공격 트리거).</summary>
    protected abstract void Tick(float dt);

    // ── 공격 ─────────────────────────────────────
    // 공격 간격 = 기준 간격(1/기준초당공격) / (기준 대비 공속 배수). 공속 스탯은 %가속(감쇠는 설정 토글).
    protected float AttackInterval {
        get {
            float baseInterval = 1f / Mathf.Max(0.01f, Settings.BaseAttacksPerSecond);
            float mult = CombatMath.AttackSpeedMultiplier(
                _stats.AttackSpeed,
                Settings.EnableAttackSpeedSoftCap, Settings.AttackSpeedSoftCapStart, Settings.AttackSpeedSoftK);
            return baseInterval / Mathf.Max(0.01f, mult);
        }
    }
    protected bool CanAttack => _attackCooldown <= 0f;

    /// <summary>기본 공격 경로를 타지 않는 행동(스킬 등)이 공격 쿨타임을 소모할 때 사용.</summary>
    protected void ConsumeAttackCooldown() => _attackCooldown = AttackInterval;

    /// <summary>기본 공격을 대상에게 가한다(multiplier 1). 공격 쿨타임을 소모한다.
    /// 데미지/VFX/사운드는 전부 기본공격 SO 에 위임하므로(유닛 고유 연출) 전투 코드에는 예외 경로가 없다.
    /// 유닛 몸체 반응(반동/카메라 흔들림)만 여기서 공통 처리한다.</summary>
    protected void PerformBasicAttack(BattleUnit target, float multiplier = 1f) {
        if (target == null || !target.IsAlive) return;
        _attackCooldown = AttackInterval;

        var attack = BasicAttackAsset;
        var runtime = Runtime;
        if (attack == null || runtime == null) {
            // 데이터 누락을 조용히 삼키지 않는다(공격이 아예 안 나가는 원인을 즉시 드러낸다).
            if (!_warnedMissingAttack) {
                _warnedMissingAttack = true;
                Debug.LogError($"[BattleUnit] 기본공격 SO 가 없어 공격할 수 없습니다: {name}. 데이터에 BasicAttack 을 할당하세요.");
            }
            return;
        }

        ExecuteBasicAttack(attack, runtime, target, multiplier);

        // 2회 공격(수련 효과): 확률이 맞으면 같은 기본공격을 짧은 간격 뒤 한 번 더 실행한다.
        // 공격 자체를 다시 실행하므로 데미지/VFX/사운드가 첫 타격과 완전히 같다(연출을 따로 복제하지 않는다).
        if (_stats.DoubleAttackChance01 > 0f && UnityEngine.Random.value < _stats.DoubleAttackChance01)
            ScheduleDoubleAttack(attack, runtime, target, multiplier);
    }

    // 기본공격 1회 실행(문맥 조립 + 유닛 반응). 첫 타격과 2회 공격이 같은 경로를 타게 모아 둔다.
    private void ExecuteBasicAttack(BasicAttack attack, IBattleRuntime runtime, BattleUnit target, float multiplier) {
        // 이 공격이 거는 상태이상을 문맥에 실어 보낸다. 데미지를 어디서 넣든(즉시/발사체 도달)
        // 같은 문맥을 쓰므로, 공격 종류가 늘어도 상태이상이 빠지는 경로가 생기지 않는다.
        attack.Execute(new AttackContext(this, target, multiplier, MuzzlePoint, target.HitPoint, runtime,
                                         AttackImpactVfxOverride, AttackStatusOverride ?? attack.StatusEffects));
        OnAttackReactions(target);
    }

    // 두 번째 타격을 예약한다. 예약 실행은 전투 재시작/씬 종료 시 취소되지만(BattleStage.Schedule),
    // 그 사이에 시전자나 대상이 죽거나 파괴될 수 있으므로 실행 직전에 유효성을 다시 확인한다.
    private void ScheduleDoubleAttack(BasicAttack attack, IBattleRuntime runtime, BattleUnit target, float multiplier) {
        float delay = Mathf.Max(0.01f, Settings.DoubleAttackDelaySeconds);
        runtime.Schedule(delay, () => {
            if (this == null || !_alive) return;
            if (target == null || !target.IsAlive) return;
            ExecuteBasicAttack(attack, runtime, target, multiplier);
        });
    }

    /// <summary>이 유닛의 기본공격 SO. 파생 클래스가 자기 데이터에서 제공한다.</summary>
    protected virtual BasicAttack BasicAttackAsset => null;

    /// <summary>공격/스킬 SO 가 의존하는 런타임 파사드. 파생 클래스가 주입받아 제공한다.</summary>
    protected virtual IBattleRuntime Runtime => null;

    /// <summary>유닛 상태(변신 등)에 의한 기본공격 타격(임팩트) VFX 교체. null 이면 공격 SO 자신의 VFX 사용.</summary>
    protected virtual GameObject AttackImpactVfxOverride => null;

    /// <summary>
    /// 기본공격이 거는 상태이상을 유닛 상태(변신 등)로 갈아끼운다. null 이면 공격 SO 의 설정을 그대로 쓴다.
    /// 임팩트 VFX 교체(AttackImpactVfxOverride)와 같은 방식이라, 유닛 상태로 공격이 달라지는 경로가 한 종류로 유지된다.
    /// </summary>
    protected virtual IReadOnlyList<StatusEffectSpec> AttackStatusOverride => null;

    /// <summary>공격 시 유닛 몸체 반응(총 반동/대표 카메라 흔들림 등). 기본 동작 없음.</summary>
    protected virtual void OnAttackReactions(BattleUnit target) { }

    // ── 스킬 ─────────────────────────────────────
    // 스킬은 기본공격과 형제 개념이다(쿨타임마다 발동 vs 주기 발동). 유닛은 "어떤 SO 인지"만 알려주고
    // 발동 판단/쿨타임/공통 연출은 이 자리에서 처리하므로 캐릭터/몬스터가 같은 경로를 공유한다.

    /// <summary>이 유닛의 고유 스킬 SO(없으면 null = 기본공격만 한다). 파생 클래스가 자기 데이터에서 제공한다.</summary>
    protected virtual UnitSkill SkillAsset => null;

    /// <summary>스킬 말풍선 텍스트 색. 파생 클래스가 데이터의 고유색을 제공한다(기본 흰색).</summary>
    protected virtual Color SkillBubbleColor => Color.white;

    /// <summary>스킬 발동 준비 완료 여부(스킬 SO 와 런타임이 있고 쿨타임이 다 찼는지).</summary>
    protected bool CanUseSkill => SkillAsset != null && Runtime != null && _skillCooldownTimer <= 0f;

    /// <summary>사거리 안에서의 공격 판단: 스킬 쿨타임이 찼으면 스킬로, 아니면 기본 공격으로 때린다.
    /// 캐릭터/몬스터 공통 경로이므로 어느 유닛이든 데이터에 스킬을 할당하면 그대로 발동한다.</summary>
    protected void PerformAttackOrSkill(BattleUnit target) {
        if (CanUseSkill) UseSkill(target);
        else PerformBasicAttack(target);
    }

    /// <summary>스킬 쿨타임을 가득 채운다(스폰/부활/사용 직후). 게이지바는 이 값에서 0까지 채워지며 진행을 표시한다.</summary>
    protected void ResetSkillCooldown() {
        _skillIntervalCache = ComputeSkillInterval();
        _skillCooldownTimer = _skillIntervalCache;
    }

    // 스킬 발동 주기(스킬쿨타임감소 + 스킬가속 점근 반영). 스킬이 없으면 0(게이지 미노출).
    private float ComputeSkillInterval() {
        var skill = SkillAsset;
        if (skill == null) return 0f;
        return CombatMath.SkillInterval(skill.BaseCooldown, _stats.SkillCooldownReduction,
                                        Settings.SkillCooldownFloorSeconds, _stats.SkillAccel, Settings.SkillAccelSoftK);
    }

    // 스킬 발동: 쿨타임 리셋 -> 공통 연출(사운드/말풍선) -> 스킬 SO 실행 -> 공격 쿨타임 소모.
    private void UseSkill(BattleUnit target) {
        var skill = SkillAsset;
        if (skill == null) return;

        ResetSkillCooldown();
        Ctx.PlaySfx(SfxKey.Skill);

        // 머리 위 말풍선(스킬명 + !). 표시 여부는 디렉터가 설정(ShowSkillBubble)으로 판단.
        Ctx.ShowSkillBubble(HitPoint, skill.SkillName + " !", SkillBubbleColor);

        skill.Execute(new SkillContext(this, target, HitPoint, Runtime));

        // 스킬도 공격 쿨타임을 소모해 난타를 막는다(PerformBasicAttack 경로를 타지 않으므로 직접 소모).
        ConsumeAttackCooldown();
        OnSkillReactions(target);
    }

    /// <summary>스킬 사용 시 유닛 고유 반응(대표 캐릭터 카메라 흔들림 등). 기본 동작 없음.</summary>
    protected virtual void OnSkillReactions(BattleUnit target) { }

    // ── 피격 ─────────────────────────────────────
    // 공격자 스탯 + 배수를 받아 전체 데미지 파이프라인(방어/관통 -> 지터 -> 치명타 -> 최종데미지)을 이 자리에서 계산한다.
    // 방어 스탯(방어력/치명타저항/피격데미지감소)은 대상(this)의 것을, 공격 스탯은 attacker 의 것을 쓴다.
    // 튜닝값은 Settings 에서 주입.
    public void TakeDamage(CombatStats attacker, float multiplier, ICombatUnit source,
                           IReadOnlyList<StatusEffectSpec> statuses = null) {
        if (!ApplyDamage(attacker, multiplier, source, canEvade: true, knockback: true)) return;
        Hit?.Invoke(this);

        // 상태이상은 실제로 맞은 경우에만 건다(회피했거나 이미 죽어 있으면 걸 대상이 아니다).
        if (_alive) ApplyStatuses(statuses, attacker, source);
    }

    /// <summary>
    /// 지속 피해(출혈 등) 전용 진입점. 데미지 계산은 일반 피격과 같은 파이프라인을 그대로 쓰되
    /// 회피와 넉백은 타지 않는다 - 틱마다 밀려나거나 빗나가면 상태이상이 아니라 또 다른 공격처럼 보인다.
    /// </summary>
    internal void TakeDamageOverTime(in CombatStats attacker, float multiplier, ICombatUnit source) =>
        ApplyDamage(attacker, multiplier, source, canEvade: false, knockback: false);

    // 데미지 파이프라인 본체. 실제로 피해가 들어갔으면 true(회피/사망 상태면 false).
    private bool ApplyDamage(in CombatStats attacker, float multiplier, ICombatUnit source,
                             bool canEvade, bool knockback) {
        if (!_alive) return false;

        // 무적: 확률 없이 모든 피격을 회피한다. 지속 피해(canEvade=false)는 MISS 없이 조용히 무시한다
        // (틱마다 MISS 가 뜨면 공격을 받는 것처럼 보인다).
        if (_invulnerable) {
            if (canEvade) ShowMiss();
            return false;
        }

        // 회피(수련 효과): 피격 자체가 없던 일이 된다. 데미지/넉백/딜량 집계 어디에도 남지 않고 MISS 만 뜬다.
        if (canEvade && TryEvade()) return false;

        float hpBefore = _hp;
        float dmg = CombatMath.ResolveDamage(attacker, _stats, multiplier, Settings.DefenseFactor,
                                             Settings.DamageJitterPercent, Settings.ResistSoftK, out bool isCritical);

        // 취약: 모든 스탯 계산이 끝난 최종 피해에 배수를 곱한다(2배면 88 -> 176).
        // 마지막에 곱하므로 방어/저항으로 깎인 뒤의 값이 정확히 그 배수만큼 늘어난다.
        if (_status != null) dmg *= _status.DamageTakenScale;

        _hp -= dmg;

        // 피해를 입는 대상이 내 캐릭터면 피격 전용 스타일(치명타면 피격 치명타 전용), 아니면 치명타/일반.
        var style = Team == Team.Player
                  ? (isCritical ? DamagePopupStyle.PlayerHitCritical : DamagePopupStyle.PlayerHit)
                  : (isCritical ? DamagePopupStyle.Critical : DamagePopupStyle.Normal);
        Ctx.ShowDamage(HitPoint, dmg, style);   // 팝업은 굴린 데미지 그대로 보여준다(타격감).
        // 맞은 쪽이 몬스터인지 캐릭터인지에 따라 피격음이 갈린다(쟁탈전 상대 캐릭터도 캐릭터음).
        Ctx.PlaySfx(IsMonster ? SfxKey.HitMonster : SfxKey.HitCharacter);

        // 딜량 그래프에는 실제로 깎은 체력만 넘긴다. 남은 체력 10 인 적을 90 으로 때리면 80 은 허공에 나간
        // 초과분(오버킬)이라, 그대로 집계하면 마무리 일격을 넣은 캐릭터의 기여도가 부풀려진다.
        Ctx.ReportDamage(source, Mathf.Min(dmg, hpBefore));

        if (knockback && source != null) ApplyKnockback(source.Position);
        OnDamaged(dmg, source);

        if (_hp <= 0f) {
            _hp = 0f;
            Die();
        }
        return true;
    }

    // 이번 적중으로 걸리는 상태이상들을 건다. 지속시간 감쇄(상태이상저항)와 확률 판정은 보유자가 처리한다.
    private void ApplyStatuses(IReadOnlyList<StatusEffectSpec> statuses, in CombatStats attacker, ICombatUnit source) {
        if (statuses == null || statuses.Count == 0) return;
        for (int i = 0; i < statuses.Count; i++)
            Status.Apply(statuses[i], attacker, source, Settings.ResistSoftK);
    }

    /// <summary>피격 직후 훅(스킬게이지 충전/피격 VFX 등).</summary>
    protected virtual void OnDamaged(float amount, ICombatUnit source) { }

    // 회피 판정. 성공하면 대상 위에 MISS 를 띄우고 true 를 돌려준다(호출측이 피격 처리를 건너뛴다).
    // 데미지 종류(기본공격/스킬)를 가리지 않는다 - "피격 시 회피"라는 규칙이 곧 이 관문 하나다.
    //
    // 팝업 스타일은 아래 데미지 표시와 똑같이 "맞은 쪽(this)이 누구인가"로 고른다.
    // 내 캐릭터가 피했으면 피격 팝업, 적이 피했으면 내가 준 피해 팝업이다. 대전(아레나/대항전)처럼
    // 양쪽 다 캐릭터인 전투에서도 이 축 하나로 정확히 갈린다(상대 팀은 Team.Enemy 로 세워진다).
    private bool TryEvade() {
        float chance = _stats.EvasionChance01;
        if (chance <= 0f || UnityEngine.Random.value >= chance) return false;

        ShowMiss();
        return true;
    }

    // 회피 표시(MISS 팝업 + 사운드). 확률 회피와 무적이 같은 연출을 쓴다.
    private void ShowMiss() {
        var style = Team == Team.Player ? DamagePopupStyle.PlayerHit : DamagePopupStyle.Normal;
        Ctx.ShowDamageText(HitPoint, Settings.MissText, style);
        Ctx.PlaySfx(SfxKey.Miss);
    }

    // ── 무적 (IInvulnerableUnit) ─────────────────────
    // 무적은 피격 관문(ApplyDamage) 하나만 막는다. 생존 판정은 건드리지 않으므로 적의 타게팅은 그대로다.
    // 해제는 거는 쪽(스킬)이 직접 부르며, 남은 시간은 그 호출이 오지 않을 때를 위한 안전 상한이다.
    private bool _invulnerable;
    private float _invulnerableRemaining;
    private GameObject _invulnerableVfx;
    private GameObject _invulnerableVfxPrefab;

    public bool IsInvulnerable => _invulnerable;

    public void BeginInvulnerability(float maxSeconds, GameObject vfxOverride = null) {
        if (!_alive || maxSeconds <= 0f) return;

        _invulnerableRemaining = _invulnerable ? Mathf.Max(_invulnerableRemaining, maxSeconds) : maxSeconds;
        _invulnerable = true;

        // VFX 는 하나만 유지한다. 같은 프리팹으로 다시 걸리면 그대로 두고, 다른 프리팹이면 교체한다.
        var prefab = vfxOverride != null ? vfxOverride : (Settings != null ? Settings.InvulnerableVfxPrefab : null);
        if (_invulnerableVfx != null && prefab == _invulnerableVfxPrefab) return;
        DestroyInvulnerableVfx();
        _invulnerableVfxPrefab = prefab;
        // 상태이상 VFX 와 같은 자리에 붙여 유닛(대시/넉백 포함)을 따라다니게 한다.
        // worldPositionStays: false 로 프리팹이 들고 있는 위치/회전/크기를 그대로 쓴다.
        if (prefab != null) _invulnerableVfx = Instantiate(prefab, StatusVfxAnchor, false);
    }

    public void EndInvulnerability() {
        _invulnerable = false;
        _invulnerableRemaining = 0f;
        DestroyInvulnerableVfx();
    }

    // 안전 상한 소진 시 자동 해제. 기절 중에도 흘러야 하므로 Update 의 행동 판정보다 앞에서 부른다.
    private void TickInvulnerability(float dt) {
        if (!_invulnerable) return;
        _invulnerableRemaining -= dt;
        if (_invulnerableRemaining <= 0f) EndInvulnerability();
    }

    private void DestroyInvulnerableVfx() {
        if (_invulnerableVfx != null) Destroy(_invulnerableVfx);
        _invulnerableVfx = null;
        _invulnerableVfxPrefab = null;
    }

    private void ApplyKnockback(Vector2 sourcePos) {
        if (_knockbackCooldown > 0f || body == null) return;
        _knockbackCooldown = Settings.KnockbackCooldownSeconds;

        Vector2 dir = ((Vector2)transform.position - sourcePos);
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        // 루트 이동(몬스터 추적)과 충돌하지 않도록 시각 루트(body)만 튕겨냈다가 복귀한다.
        body.DOKill();
        body.localPosition = Vector3.zero;
        body.DOPunchPosition(dir * Settings.KnockbackDistance, Settings.KnockbackDurationSeconds, 1, 0.5f);
    }

    protected virtual void Die() {
        _alive = false;
        // 걸려 있던 상태이상은 죽는 순간 전부 풀린다(VFX 도 함께 사라진다).
        // 남겨 두면 부활한 유닛이 죽기 전 기절을 그대로 이어받는다.
        _status?.Clear();
        EndInvulnerability();
        CancelDash();
        if (body != null) body.DOKill();
        Ctx.PlaySfx(SfxKey.UnitDeath);
        Died?.Invoke(this);
        Ctx.OnUnitDied(this);
    }

    /// <summary>대상 쪽을 바라보도록 시각 루트를 좌우 반전.</summary>
    protected void FaceTowards(Vector2 targetPos) {
        if (body == null) return;
        float dx = targetPos.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;
        var s = body.localScale;
        s.x = Mathf.Abs(s.x) * (dx >= 0f ? 1f : -1f);
        body.localScale = s;
    }

    /// <summary>
    /// 겹침 해소 패스가 이 유닛을 밀어낼 수 있는지. 기본은 true 이며, 전장에 고정되어야 하는 유닛
    /// (호위 대상처럼 위치가 컨텐츠 규칙인 유닛)이 false 로 덮어쓴다. false 인 유닛은 다른 유닛을 밀어내지도 않는다.
    /// </summary>
    public virtual bool PushableByOverlap => true;

    /// <summary>겹침 해소 등 외부 요인에 의한 미세 위치 보정(루트 이동). 살아있을 때만 적용하고 z(정렬용)는 보존한다.
    /// 이동 로직(Tick)과 독립적으로 디렉터가 호출한다.</summary>
    public void Nudge(Vector2 delta) {
        if (!_alive) return;
        Vector3 p = transform.position;
        transform.position = new Vector3(p.x + delta.x, p.y + delta.y, p.z);
    }

    // 대시 중 복귀할 원위치와 진행 여부. 대시는 루트 전체를 옮기므로(마스크/스프라이트/체력바가 함께 이동)
    // 시작 지점을 스냅샷해 두었다가 종료 시 그 자리로 되돌린다. 사망/부활/재시작 시에는 CancelDash 로 정리한다.
    private Vector3 _dashHome;
    private bool _dashing;

    /// <summary>유닛 전체(루트)를 worldTarget 근처로 duration 동안 빠르게 이동(대시)시킨다. 마스크 일러스트/체력바가 함께 움직여
    /// 초상이 잘리지 않는다. stopShort 만큼 대상 앞에서 멈춰 겹침을 피한다. 첫 호출 시 원위치를 스냅샷하고, ReturnVisual 로 복귀한다.
    /// 도착 타이밍의 데미지는 호출부(스킬)가 Schedule 로 별도 관리하므로 여기서는 이동 연출만 담당한다.</summary>
    public void DashVisual(Vector2 worldTarget, float duration, float stopShort) {
        if (!_alive) return;
        if (!_dashing) { _dashHome = transform.position; _dashing = true; }
        Vector2 cur = transform.position;
        Vector2 dir = worldTarget - cur;
        float dist = dir.magnitude;
        Vector2 dest = dist > stopShort ? worldTarget - dir / dist * stopShort : cur;
        FaceTowards(worldTarget);
        transform.DOKill();
        Vector3 to = new Vector3(dest.x, dest.y, transform.position.z);   // z(정렬용)는 보존
        if (duration <= 0f) { transform.position = to; return; }
        transform.DOMove(to, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>대시로 이동한 유닛을 시작 위치로 duration 동안 복귀시키고 대시 상태를 종료한다.</summary>
    public void ReturnVisual(float duration) {
        if (!_dashing) return;
        Vector3 home = _dashHome;
        _dashing = false;
        transform.DOKill();
        if (duration <= 0f) { transform.position = home; return; }
        transform.DOMove(home, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>진행 중인 대시를 즉시 중단하고 이동 트윈을 정리한다(사망/부활/전투 재시작 시 스테일 이동 방지).
    /// 위치 복원은 호출부(Die 는 부활이, Revive 는 스폰 배치가) 별도로 처리한다.</summary>
    protected void CancelDash() {
        if (!_dashing) return;
        _dashing = false;
        transform.DOKill();
    }

    /// <summary>대상 위치를 향해 MoveSpeed 로 한 프레임 직선 이동한다(공용 추적 이동, 캐릭터/몬스터 공통).
    /// keepDistance 만큼 목표 앞에서 멈춘다(0 이면 목표 지점까지). 한 스텝이 남은 거리보다 크면 멈출 지점에
    /// 스냅해 지나치지 않는다(복귀/근접 시 진동 방지).</summary>
    protected void MoveTowards(Vector2 targetPos, float dt, float keepDistance = 0f) {
        Vector3 cur = transform.position;
        Vector2 delta = targetPos - (Vector2)cur;
        float dist = delta.magnitude;
        float advance = dist - keepDistance;   // 이번 이동으로 좁힐 수 있는 최대 거리
        if (advance <= 0.0001f) return;        // 이미 멈출 지점(또는 더 안쪽)에 있다.

        float step = Settings.BaseMoveSpeed * _stats.MoveSpeedMultiplier * dt;
        if (step > advance) step = advance;
        Vector2 next = (Vector2)cur + delta * (step / dist);
        transform.position = new Vector3(next.x, next.y, cur.z);   // z(정렬용 등)는 보존
    }

    /// <summary>
    /// 정해진 방향으로 MoveSpeed 만큼 한 프레임 이동한다(직접 조작 이동). direction 의 크기가 곧 속도 비율이며
    /// 1 을 넘으면 1 로 자른다(조이스틱을 덜 밀면 천천히 걷는다). 목표 지점이 없는 이동이라 멈출 곳을 계산하지 않는다.
    /// </summary>
    protected void MoveAlong(Vector2 direction, float dt) {
        float amount = Mathf.Min(1f, direction.magnitude);
        if (amount <= 0.0001f) return;

        Vector3 cur = transform.position;
        float step = Settings.BaseMoveSpeed * _stats.MoveSpeedMultiplier * dt * amount;
        Vector2 next = (Vector2)cur + direction.normalized * step;
        transform.position = new Vector3(next.x, next.y, cur.z);   // z(정렬용 등)는 보존
    }

    /// <summary>대상 유닛에게 다가간다. 서로의 몸통이 맞닿는 지점에서 멈추므로 덩치가 큰 유닛의 중심까지 파고들지 않는다.</summary>
    protected void ApproachUnit(BattleUnit target, float dt) {
        if (target == null) return;
        MoveTowards(target.Position, dt, BodyRadius + target.BodyRadius);
    }

    /// <summary>대상 몸통 표면까지의 거리(중심 거리에서 양쪽 몸통 반경을 뺀 값, 최소 0).
    /// 사거리/탐지 판정은 전부 이 값을 쓴다. 덩치와 무관하게 "몸이 닿을 만큼 가까운가"를 일관되게 판정하기 위함이다.</summary>
    protected float DistanceTo(BattleUnit other) {
        if (other == null) return float.MaxValue;
        return Mathf.Max(0f, Vector2.Distance(other.Position, Position) - BodyRadius - other.BodyRadius);
    }

    /// <summary>가장 가까운 생존 적을 찾는다(공용 타게팅). 몸통 표면 기준이라 덩치가 큰 적이 실제로 더 가까우면 그쪽을 고른다.</summary>
    protected BattleUnit FindNearestEnemy() {
        var enemies = Ctx.EnemiesOf(this);
        BattleUnit best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < enemies.Count; i++) {
            var e = enemies[i];
            if (e == null || !e.IsAlive) continue;
            // 자기 반경은 모든 후보에 똑같이 빠지므로 순위에 영향이 없어 대상 반경만 뺀다.
            float dist = Vector2.Distance(e.Position, Position) - e.BodyRadius;
            if (dist < bestDist) {
                bestDist = dist;
                best = e;
            }
        }
        return best;
    }

#if UNITY_EDITOR
    // 몸통 반경을 씬 뷰에서 눈으로 맞추기 위한 표시(에디터 전용, 런타임 동작과 무관).
    private void OnDrawGizmosSelected() {
        float r = BodyRadius;
        if (r <= 0f) return;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, r);
    }
#endif
}
