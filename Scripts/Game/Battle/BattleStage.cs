using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Enums;
// 주의: UniRx 의 Unit 타입이 전투 Unit 과 이름 충돌하므로 using UniRx 를 생략하고 ReactiveProperty 를 정규화한다.

/// <summary>
/// 전투 "무대"(호스트) 공통부. 파티/몬스터 생성과 배치, 연출 풀, 카메라, 겹침 해소, 스킬 런타임 등
/// 어떤 전투 컨텐츠에서도 동일한 부분을 모두 여기서 담당하고 IBattleContext / IBattleRuntime 을 구현한다.
///
/// "무엇을 언제 스폰하고 언제 끝나는가"(진행 규칙)만 파생 클래스가 정한다:
///  - BattleDirector     : 메인 씬 무한 방치 루프(기력 소모 -> 전멸까지 -> 클리어 보상 -> 반복)
///  - CommissionDirector : 의뢰 씬 단판 규칙(제한 시간/처치 목표/호위 대상, 부활 없음)
///
/// 유닛(CharacterUnit/MonsterUnit/EscortUnit)과 스킬 SO 는 구체 디렉터를 전혀 모르고 위 두 인터페이스만 참조한다.
/// 따라서 새 전투 컨텐츠를 추가할 때 유닛/스킬/공격 코드는 한 줄도 고칠 필요가 없다.
/// </summary>
public abstract class BattleStage : MonoBehaviour, IBattleContext, IBattleRuntime {
    [Header("Scene Refs")]
    [SerializeField] private CharacterUnit characterUnitPrefab;
    [Tooltip("전투 중심점(캐릭터/몬스터 원형 배치 기준). 미할당 시 원점(0,0,0).")]
    [SerializeField] private Transform battleCenter;
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Header("Systems")]
    [SerializeField] private SimpleBattleCamera battleCamera;
    [SerializeField] private DamagePopupPool damagePopupPool;
    [Tooltip("스킬 사용 말풍선 풀(선택). 비우면 말풍선 미표시.")]
    [SerializeField] private SkillBubblePool skillBubblePool;
    [SerializeField] private BattleVfxPool vfxPool;
    [Tooltip("원거리 공격 발사체 풀(선택). 원거리 캐릭터가 없으면 비워도 된다.")]
    [SerializeField] private ProjectilePool projectilePool;
    [Tooltip("유닛 체력바 공유 오버레이(전투 씬의 캔버스에 배치).")]
    [SerializeField] private UnitBarOverlay barOverlay;

    // 주입 의존성(파생 클래스가 ConfigureStage 로 넘긴다).
    protected BattleSettings Settings { get; private set; }
    protected CharacterDatabase CharDb { get; private set; }
    protected EquipmentDatabase EquipDb { get; private set; }
    protected PlayerStateStore Store { get; private set; }
    protected ISoundService Sound { get; private set; }
    /// <summary>던전 레지스트리. 진행 중인 스테이지의 그룹(장)을 찾아 전투 BGM 을 고르는 데 쓴다.</summary>
    protected DungeonDatabase DungeonDb { get; private set; }
    /// <summary>캐릭터 외형(기본 일러스트/코스튬) 해석기. 파티 스폰이 유닛에게 넘길 외형을 여기서 받는다.</summary>
    protected CharacterAppearanceService Appearance { get; private set; }

    // 런타임 상태
    private readonly List<CharacterUnit> _party = new();
    private readonly List<BattleUnit> _playerUnits = new();   // 아군 팀 전체(파티 + 호위 대상 등). 적의 타게팅 대상.
    private readonly List<BattleUnit> _monsterUnits = new();
    private readonly List<ICombatUnit> _skillBuffer = new();
    private readonly List<BattleUnit> _sepBuffer = new();     // 겹침 해소 패스용 재사용 버퍼(프레임 할당 회피).

    // 마스크 일러스트를 쓰는 몬스터의 정렬순서 격리용. 캐릭터 슬롯(0..N, N<<Base)과 겹치지 않도록
    // 충분히 큰 베이스에서 시작해 몬스터끼리 유일한 대역을 갖는다(안 그러면 서로/캐릭터 마스크가 섞인다).
    // Span 으로 재사용 순환시켜 sortingOrder(short 범위)를 넘지 않게 한다. 몬스터를 캐릭터 뒤로 보내려면 Base 를 음수로.
    private const int MonsterMaskSlotBase = 100;
    private const int MonsterMaskSlotSpan = 500;
    private int _monsterSpawnSeq;

    // 예약된 스킬 콜백(스태거/딜레이 연출) 전용 취소원. 파티 재구성마다 갱신되어, 파괴될 이전 유닛을 참조하는 스테일 콜백을 끊는다.
    private CancellationTokenSource _scheduleCts;
    private bool _systemsConfigured;

    // 이번 전투에 적용 중인 원형 배치. 파생 클래스가 SetFormation 으로 바꾼다(던전/의뢰별 오버라이드).
    private BattleFormation _formation;

    /// <summary>현재 파티(대표 포함). 하단 전투 캐릭터 패널이 참조한다.</summary>
    public IReadOnlyList<CharacterUnit> Party => _party;

    /// <summary>현재 살아 있는(전장에 존재하는) 몬스터 목록. 진행 규칙이 남은 수/보스 체력을 읽는다.</summary>
    public IReadOnlyList<BattleUnit> ActiveMonsters => _monsterUnits;

    /// <summary>이번 전투의 캐릭터별 누적 딜량(딜량 그래프 패널이 순위를 읽어간다). 전투가 바뀔 때마다 초기화된다.</summary>
    public BattleDamageMeter DamageMeter { get; } = new();

    /// <summary>파티가 재구성될 때 발생(HUD 슬롯 재바인딩용).</summary>
    public event Action PartyChanged;

    /// <summary>파티 전원이 쓰러졌는지(부활 대기 중인 유닛도 사망으로 본다). 부활 없는 컨텐츠의 실패 판정에 쓴다.</summary>
    public bool IsPartyWiped {
        get {
            if (_party.Count == 0) return false;
            for (int i = 0; i < _party.Count; i++)
                if (_party[i] != null && _party[i].IsAlive) return false;
            return true;
        }
    }

    // ── 초기화 ────────────────────────────────────────
    /// <summary>파생 클래스의 [Inject] Construct 에서 호출한다. 베이스에 [Inject] 를 두면 파생과 중복 주입되므로 위임 방식으로 받는다.</summary>
    protected void ConfigureStage(BattleSettings settings, CharacterDatabase charDb, EquipmentDatabase equipDb,
                                  PlayerStateStore store, ISoundService sound,
                                  CharacterAppearanceService appearance, DungeonDatabase dungeonDb) {
        Settings = settings;
        CharDb = charDb;
        EquipDb = equipDb;
        Store = store;
        Sound = sound;
        Appearance = appearance;
        DungeonDb = dungeonDb;
        _formation = settings.Formation;   // 컨텐츠가 오버라이드하기 전에도 유효한 배치를 갖도록 공용 기본값으로 시작.
    }

    /// <summary>풀/카메라 등 씬 시스템을 1회 설정하고 성장 변경 구독을 건다. 파생 클래스의 진입점에서 호출한다.</summary>
    protected void InitializeSystems() {
        if (_systemsConfigured) return;
        _systemsConfigured = true;

        damagePopupPool.Configure(Settings);
        vfxPool.Configure(Settings);
        if (projectilePool != null) projectilePool.Configure();
        battleCamera.Configure(Settings);
        RestoreSavedZoom();
        // 배경 커버 스케일의 기준(최대 줌아웃)을 카메라 설정과 자동 동기화한다(있을 때만).
        if (backgroundRenderer != null)
            backgroundRenderer.GetComponent<CameraCoverBackground>()?.SetReferenceOrthographicSize(Settings.CameraMaxZoom);

        // 성장/장비/별 승급 등으로 능력치가 바뀌면 진행 중인 전투에도 즉시 반영한다(PLANNING 3-3).
        Store.CharactersChanged += OnCharactersChanged;
    }

    private void OnCharactersChanged(CharacterChange change) => RefreshPartyStats();

    // 전투를 끊지 않고 살아있는 파티 유닛의 스탯(과 외형)만 최신 값으로 교체한다.
    // 파티는 최대 5명이고 계산은 순수 산술이라 대상 판별 없이 전원 갱신해도 부담이 없다.
    protected void RefreshPartyStats() {
        for (int i = 0; i < _party.Count; i++) {
            var unit = _party[i];
            if (unit == null || unit.Data == null) continue;

            var progress = Store.GetProgress(unit.Data.Id);
            if (progress == null) continue;

            // 각인은 "항상 적용분"이 기준 스탯에, "조건부"가 유닛 상태로 들어가므로 둘 다 함께 갱신한다.
            unit.SetEngravings(Store.EngravingLoadoutFor(unit.Data.Id));
            unit.ApplyStats(BuildCharacterStats(unit.Data, progress));
            // 코스튬은 스탯과 외형을 함께 바꾸므로, 전투 중에 갈아입어도 그 자리에서 그림까지 바뀐다.
            if (Appearance != null) unit.ApplyAppearance(Appearance.Mine(unit.Data));
        }
    }

    /// <summary>
    /// 캐릭터 1명의 최종 전투 스탯. 성장 + 장비 + 각인이 합쳐지는 단일 관문이라 여기만 고치면
    /// 파티 생성/즉시 갱신/전투력 표시가 모두 같은 값을 쓴다.
    /// </summary>
    protected CombatStats BuildCharacterStats(CharacterData data, CharacterProgress progress) =>
        new CharacterRuntime(data, progress, Settings, Store.TotalBonusFor(data.Id, EquipDb)).FinalStats;

    /// <summary>이번 전투의 원형 배치를 바꾼다(파티 생성 전에 호출해야 그 배치로 스폰된다).</summary>
    protected void SetFormation(BattleFormation formation) => _formation = formation;

    protected BattleFormation Formation => _formation;

    // ── 파티 / 유닛 생성 ───────────────────────────────
    /// <summary>현재 덱으로 파티를 새로 만든다(기존 파티는 파괴). 대표 캐릭터를 카메라 타깃으로 잡는다.</summary>
    protected void RebuildParty() {
        // 유닛을 파괴하기 전에 이전 전투의 예약 스킬 콜백을 끊는다(파괴된 시전자 참조로 인한 MissingReferenceException 방지).
        ResetScheduleToken();

        // 스쿼드 재생 주기도 이전 전투의 잔여 시간을 물려받지 않는다. 파티를 새로 만드는 것이 곧 새 전투이므로
        // 재생 주기도 여기서 0 부터 다시 시작해야 "어떤 전투든 시작 후 10초에 첫 회복"이 성립한다.
        // (물려받으면 앞 판이 8초까지 채운 채 끝났을 때 다음 판이 2초 만에 회복해 버린다.)
        ResetSquadRegen();

        foreach (var u in _party) {
            if (u == null) continue;
            barOverlay?.Unregister(u);
            _playerUnits.Remove(u);
            Destroy(u.gameObject);
        }
        _party.Clear();

        var deck = Store.Deck;
        var ids = deck.SelectedCharIds;
        int repId = deck.RepresentativeCharId;

        for (int i = 0; i < ids.Count; i++) {
            int charId = ids[i];
            var data = CharDb.Get(charId);
            var progress = Store.GetProgress(charId);
            if (data == null || progress == null) continue;

            int slot = _party.Count;   // 살아있는 유닛 사이에서 유일한 슬롯(마스크 격리/배치용)
            Vector3 spawn = CharacterRingPos(i, ids.Count);
            var unit = Instantiate(characterUnitPrefab, spawn, Quaternion.identity, transform);

            bool isRep = charId == repId;
            // 외형은 내 장착 코스튬을 반영한다(코스튬이 없으면 기본 일러스트가 그대로 나온다).
            var appearance = Appearance != null ? Appearance.Mine(data) : CharacterAppearance.Default(data);
            unit.Init(appearance, BuildCharacterStats(data, progress), isRep, spawn, slot, this, this, Settings);
            unit.SetEngravings(Store.EngravingLoadoutFor(charId));
            // 투자(선제 스킬 게이지). 각인/스탯이 다 얹힌 뒤라야 스킬 주기가 확정되므로 마지막에 채운다.
            unit.PrimeSkillGauge(InvestmentRules.ToRatio(Store.InvestmentPermille(InvestmentEffect.SkillGaugeStart)));

            _party.Add(unit);
            _playerUnits.Add(unit);
            barOverlay?.Register(unit);
        }

        // 이전 판의 유닛은 파괴되었으므로 버리고, 저장된 선택(없으면 대표)으로 카메라 대상을 다시 잡는다.
        // 고른 캐릭터가 이번 덱에 없으면 여기서 대표로 초기화된다.
        _cameraFocus = null;
        UpdateCameraFocus();

        RaisePartyChanged();
    }

    /// <summary>
    /// 몬스터가 아닌 적 유닛(유저 대 유저 전투의 상대 캐릭터 등)을 적 진영에 등록한다.
    /// 적 목록은 종류를 가리지 않으므로, 등록만 하면 타게팅/전멸 판정/사망 처리가 몬스터와 동일하게 동작한다.
    /// </summary>
    protected void RegisterEnemyUnit(BattleUnit unit) {
        if (unit == null || _monsterUnits.Contains(unit)) return;
        _monsterUnits.Add(unit);
        barOverlay?.Register(unit);
    }

    /// <summary>파티 외의 아군 유닛(호위 대상 등)을 전장에 등록한다. 적의 타게팅 대상이 되고 체력바가 붙는다.</summary>
    protected void RegisterPlayerUnit(BattleUnit unit) {
        if (unit == null || _playerUnits.Contains(unit)) return;
        _playerUnits.Add(unit);
        barOverlay?.Register(unit);
    }

    /// <summary>등록 해제 + 파괴. 파티 유닛에는 쓰지 않는다(RebuildParty 가 관리).</summary>
    protected void DestroyPlayerUnit(BattleUnit unit) {
        if (unit == null) return;
        _playerUnits.Remove(unit);
        barOverlay?.Unregister(unit);
        Destroy(unit.gameObject);
    }

    /// <summary>
    /// 파티가 아닌 아군 유닛(호위 대상 등)을 전부 정리한다. 전투를 다시 시작할 때 반드시 호출해야
    /// 이전 판의 유닛이 아군 목록에 남아 적의 타게팅 대상이 되거나 파괴된 참조로 남지 않는다.
    /// </summary>
    protected void ClearExtraPlayerUnits() {
        for (int i = _playerUnits.Count - 1; i >= 0; i--) {
            var unit = _playerUnits[i];
            if (unit is CharacterUnit) continue;   // 파티 유닛은 RebuildParty 가 관리한다.

            _playerUnits.RemoveAt(i);
            if (unit == null) continue;            // 이미 파괴된 항목은 목록에서 빼는 것으로 충분.
            barOverlay?.Unregister(unit);
            Destroy(unit.gameObject);
        }
    }

    /// <summary>
    /// 몬스터 1기를 바깥 원 위에 스폰한다. 생성된 유닛을 반환한다(보스 참조 등에 사용, 실패 시 null).
    /// showHealthBar 를 끄면 공유 오버레이에 등록하지 않는다(체력을 다른 UI 가 이미 보여 주는 경우).
    /// </summary>
    protected MonsterUnit SpawnMonster(MonsterData data, bool showHealthBar = true) {
        if (data == null || data.BodyPrefab == null) return null;

        var prefabUnit = data.BodyPrefab.GetComponent<MonsterUnit>();
        if (prefabUnit == null) {
            // 데이터 누락을 조용히 삼키지 않는다(스폰이 안 되는 원인을 즉시 드러낸다).
            Debug.LogError($"[BattleStage] 몬스터 프리팹에 MonsterUnit 이 없습니다: {data.DisplayName}");
            return null;
        }

        var unit = Instantiate(prefabUnit, MonsterRingPos(), Quaternion.identity, transform);

        // 마스크 유닛끼리 겹치지 않을 유일한 정렬 슬롯을 부여한다(표현부가 없으면 MonsterUnit 이 무시).
        int slot = MonsterMaskSlotBase + (_monsterSpawnSeq++ % MonsterMaskSlotSpan);
        // 캐릭터와 동일하게 IBattleContext + IBattleRuntime 을 함께 넘긴다(기본공격/스킬 SO 가 런타임에 의존).
        unit.Init(data, this, this, Settings, slot);
        _monsterUnits.Add(unit);
        if (showHealthBar) barOverlay?.Register(unit);
        return unit;
    }

    protected void ClearMonsters() {
        foreach (var m in _monsterUnits) {
            if (m == null) continue;
            barOverlay?.Unregister(m);
            Destroy(m.gameObject);
        }
        _monsterUnits.Clear();
    }

    protected void SetBackground(Sprite sprite) {
        if (backgroundRenderer != null && sprite != null) backgroundRenderer.sprite = sprite;
    }

    protected void RaisePartyChanged() => PartyChanged?.Invoke();

    protected Vector3 CenterPos => battleCenter != null ? battleCenter.position : Vector3.zero;

    /// <summary>파티를 가로 일렬로 정렬한다(휴식/대기 상태 표시용).</summary>
    protected void ArrangePartyInLine() {
        Vector3 center = CenterPos;
        for (int i = 0; i < _party.Count; i++) {
            var u = _party[i];
            if (u == null) continue;
            float x = (i - (_party.Count - 1) * 0.5f) * Settings.PartyFormationSpacing;
            u.transform.position = new Vector3(center.x + x, center.y, 0f);
        }
    }

    // 캐릭터를 중심점 둘레의 작은 원 위에 균등 배치하고 지터를 준다(PLANNING 2). 1명이면 정중앙.
    // 사방에서 몬스터가 몰려오는 던전/의뢰용 배치라, 양 팀이 마주 서는 대전에는 맞지 않는다.
    // 그래서 배치 방식 자체를 파생 클래스가 갈아끼울 수 있게 열어 둔다(기본 동작은 그대로).
    protected virtual Vector3 CharacterRingPos(int index, int total) {
        if (total <= 1) return CenterPos;
        float step = 360f / total;
        float angle = step * index + UnityEngine.Random.Range(-step * 0.25f, step * 0.25f);
        float radius = _formation.CharacterRingRadius +
                       UnityEngine.Random.Range(-_formation.CharacterRingJitter, _formation.CharacterRingJitter);
        return CenterPos + PolarOffset(angle, radius);
    }

    // 몬스터를 중심점 둘레의 큰 원 위(캐릭터를 감싸는 바깥쪽)에 랜덤 배치한다(PLANNING 2).
    // 배치의 각도 범위는 BattleFormation 이 정한다(기본 전방위, 호위 의뢰처럼 좌우에서만 몰려오게 할 수도 있다).
    private Vector3 MonsterRingPos() {
        float angle = _formation.RandomSpawnAngle();
        float radius = _formation.MonsterRingRadius +
                       UnityEngine.Random.Range(-_formation.MonsterRingJitter, _formation.MonsterRingJitter);
        return CenterPos + PolarOffset(angle, radius);
    }

    protected static Vector3 PolarOffset(float angleDeg, float radius) {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
    }

    // ── 파생 클래스 훅(진행 규칙) ───────────────────────
    /// <summary>
    /// 캐릭터가 쓰러졌을 때 몇 초 뒤에 부활할지. 음수를 반환하면 부활하지 않는다(의뢰의 "부활 없음").
    /// 유닛이 스스로 정하지 않고 무대에 물어보므로, 같은 CharacterUnit 프리팹이 컨텐츠마다 다르게 동작한다.
    /// </summary>
    protected abstract float ReviveDelaySeconds { get; }

    /// <summary>
    /// 몬스터가 "가장 가까운 적" 대신 노려야 할 우선 대상(호위 의뢰의 돌격병이 의뢰인에게 직행하는 규칙).
    /// null 을 반환하면 몬스터는 기본 타게팅(가장 가까운 적)을 쓴다. 기본 구현은 우선 대상이 없다.
    /// </summary>
    protected virtual BattleUnit ResolvePriorityTarget(BattleUnit unit) => null;

    /// <summary>몬스터가 쓰러졌을 때(목록에서 제거된 뒤) 호출. 진행 규칙이 처치 수/클리어를 판정한다.</summary>
    protected virtual void OnMonsterDied(BattleUnit unit) { }

    /// <summary>아군(파티/호위 대상)이 쓰러졌을 때 호출. 진행 규칙이 전멸/호위 실패를 판정한다.</summary>
    protected virtual void OnPlayerUnitDied(BattleUnit unit) { }

    // ── IBattleContext ─────────────────────────────
    IReadOnlyList<BattleUnit> IBattleContext.EnemiesOf(BattleUnit unit) =>
        unit != null && unit.Team == Team.Player ? _monsterUnits : _playerUnits;

    IReadOnlyList<BattleUnit> IBattleContext.AlliesOf(BattleUnit unit) =>
        unit != null && unit.Team == Team.Player ? _playerUnits : _monsterUnits;

    void IBattleContext.ShowDamage(Vector2 worldPos, float amount, DamagePopupStyle style) {
        // 로컬 설정 OFF 면 표시 안 함(기본 ON). 세이브 로드 전이면 기본값(표시)으로 동작.
        if (UserDataManager.Data != null && !UserDataManager.Data.ShowDamageNumber) return;
        damagePopupPool.Spawn(worldPos, amount, style);
    }

    void IBattleContext.ShowDamageText(Vector2 worldPos, string text, DamagePopupStyle style) {
        if (string.IsNullOrEmpty(text)) return;
        // 숫자 표시와 같은 설정을 따른다(데미지 표시를 끈 유저에게 회피 문구만 남으면 어긋난다).
        if (UserDataManager.Data != null && !UserDataManager.Data.ShowDamageNumber) return;
        damagePopupPool.Spawn(worldPos, 0f, style, text);
    }

    void IBattleContext.ShowSkillBubble(Vector2 worldPos, string text, Color color) {
        if (skillBubblePool == null) return;
        // 로컬 설정 OFF 면 표시 안 함(기본 ON). 세이브 로드 전이면 기본값(표시)으로 동작.
        if (UserDataManager.Data != null && !UserDataManager.Data.ShowSkillBubble) return;
        skillBubblePool.Spawn(worldPos, text, color);
    }

    void IBattleContext.PlayVfx(GameObject prefab, Vector2 at) => vfxPool.Play(prefab, at);
    void IBattleContext.PlayVfx(GameObject prefab, Vector2 at, float angleDeg) => vfxPool.Play(prefab, at, angleDeg);

    // 캐릭터가 낸 피해만 딜량 그래프에 집계한다(몬스터 -> 캐릭터 피해와 호위 대상의 피해는 대상 밖).
    void IBattleContext.ReportDamage(ICombatUnit source, float amount) {
        if (source is CharacterUnit c && c.Data != null) DamageMeter.Add(c.Data.Id, amount);
    }

    void IBattleContext.OnUnitDied(BattleUnit unit) {
        if (unit == null) return;

        if (unit.Team == Team.Enemy) {
            _monsterUnits.Remove(unit);
            barOverlay?.Unregister(unit);
            OnMonsterDied(unit);
            Destroy(unit.gameObject, 0.05f);
            return;
        }

        // 아군: 부활은 유닛이 스스로 처리하므로(부활 지연은 ReviveDelayFor 로 무대가 정한다) 목록에서 빼지 않는다.
        // 부활하지 않는 컨텐츠에서도 목록에 남겨 두어야 "전멸했는지"를 판정할 수 있다.
        OnPlayerUnitDied(unit);
    }

    float IBattleContext.ReviveDelayFor(BattleUnit unit) => ReviveDelaySeconds;

    BattleUnit IBattleContext.PriorityTargetFor(BattleUnit unit) => ResolvePriorityTarget(unit);

    void IBattleContext.OnRepresentativeHitEnemy() {
        // 로컬 설정 OFF 면 흔들지 않는다(기본 ON). 세이브 로드 전이면 기본값(흔들림)으로 동작.
        if (UserDataManager.Data != null && !UserDataManager.Data.UseCameraShake) return;
        battleCamera.Shake();
    }

    void IBattleContext.PlaySfx(SfxKey key) => Sound.PlaySfx(key);

    // ── IBattleRuntime (스킬용) ────────────────────
    IReadOnlyList<ICombatUnit> IBattleRuntime.Enemies(ICombatUnit of) {
        var src = of is BattleUnit u && u.Team == Team.Player ? _monsterUnits : _playerUnits;
        _skillBuffer.Clear();
        for (int i = 0; i < src.Count; i++) _skillBuffer.Add(src[i]);
        return _skillBuffer;
    }

    IReadOnlyList<ICombatUnit> IBattleRuntime.Allies(ICombatUnit of) {
        var src = of is BattleUnit u && u.Team == Team.Player ? _playerUnits : _monsterUnits;
        _skillBuffer.Clear();
        for (int i = 0; i < src.Count; i++) _skillBuffer.Add(src[i]);
        return _skillBuffer;
    }

    void IBattleRuntime.PlayVfx(GameObject prefab, Vector2 at) => vfxPool.Play(prefab, at);
    void IBattleRuntime.PlayVfx(GameObject prefab, Vector2 at, float angleDeg) => vfxPool.Play(prefab, at, angleDeg);
    void IBattleRuntime.PlaySfx(SfxKey key) => Sound.PlaySfx(key);
    void IBattleRuntime.PlaySfx(AudioClip clip) => Sound.PlaySfx(clip);

    void IBattleRuntime.LaunchProjectile(ProjectileSpec spec, Vector2 from, ICombatUnit source, ICombatUnit target) {
        if (projectilePool != null) {
            projectilePool.Launch(spec, from, source, target, this);
            return;
        }
        // 풀 미할당 폴백: 발사체 없이 즉시 도달 처리(효과 유실 방지, RangedAttack 의 즉시 명중 폴백과 동일 철학).
        // 도착 자체가 효과인 발사체(ArriveEvenIfTargetLost)는 대상이 죽어 있어도 그 자리에서 효과를 낸다(Projectile 과 같은 규칙).
        if (!target.Exists()) return;
        Vector2 at = target.Position;
        if (spec.OnHit != null && source.Exists() && (target.IsAlive || spec.ArriveEvenIfTargetLost))
            spec.OnHit.Invoke(new ProjectileHit(source, target, at));
        if (spec.ImpactVfxPrefab != null) vfxPool.Play(spec.ImpactVfxPrefab, at);
    }

    void IBattleRuntime.Schedule(float delaySeconds, Action action) {
        if (action == null) return;
        if (delaySeconds <= 0f) { action(); return; }
        RunScheduledAsync(delaySeconds, action).Forget();
    }

    void IBattleRuntime.DashCaster(ICombatUnit caster, Vector2 worldTarget, float arriveSeconds, float stopShort) {
        if (caster is BattleUnit u && u != null) u.DashVisual(worldTarget, arriveSeconds, stopShort); // u != null: 파괴된 유닛 접근 방지
    }

    void IBattleRuntime.ReturnCaster(ICombatUnit caster, float seconds) {
        if (caster is BattleUnit u && u != null) u.ReturnVisual(seconds); // u != null: 파괴된 유닛 접근 방지
    }

    // ── 예약 실행 ───────────────────────────────────
    /// <summary>파티 재구성/씬 종료 시 예약된 스킬 콜백을 전부 끊고 새 토큰을 만든다.</summary>
    protected void ResetScheduleToken() {
        _scheduleCts?.Cancel();
        _scheduleCts?.Dispose();
        _scheduleCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
    }

    // 전투 재시작/씬 종료 시 자동 취소되는 지연 실행. 취소되면 action 을 호출하지 않아
    // 파티 재구성/몬스터 정리 후의 스테일 참조 접근을 막는다(다단 스태거/시간차 연출 공용).
    private async UniTaskVoid RunScheduledAsync(float delaySeconds, Action action) {
        var token = _scheduleCts != null ? _scheduleCts.Token : this.GetCancellationTokenOnDestroy();
        try {
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: token);
        } catch (OperationCanceledException) {
            return;
        }
        action();
    }

    // ── 겹침 해소(유닛 분리) ─────────────────────────
    // 물리(Rigidbody2D)를 쓰지 않는 transform 이동 구조라, 각 유닛의 이동(Update)이 끝난 뒤(LateUpdate)
    // 겹친 유닛 쌍을 최소 간격까지 밀어내는 위치 완화(relaxation) 패스로 겹침을 해소한다.
    // 유닛끼리 서로 참조하지 않도록 무대가 목록을 쥐고 중앙에서 처리한다(디커플링). 유닛 수가 적어(파티+동시 몬스터 상한)
    // O(n^2) 로 충분하며, 반복(iteration)과 프레임당 밀림 캡으로 다중 겹침도 순간이동 없이 안정적으로 벌어진다.
    protected virtual void LateUpdate() {
        ResolveUnitOverlaps();
    }

    private void ResolveUnitOverlaps() {
        if (Settings == null) return;
        // 공용 최소 간격. 실제 간격은 여기에 두 유닛의 몸통 반경을 반영해 쌍마다 다시 정한다.
        float baseMinDist = Settings.UnitSeparationDistance;
        float maxStep = Settings.UnitSeparationMaxStep;

        _sepBuffer.Clear();
        for (int i = 0; i < _playerUnits.Count; i++) {
            var u = _playerUnits[i];
            if (u != null && u.IsAlive && u.PushableByOverlap) _sepBuffer.Add(u);
        }
        for (int i = 0; i < _monsterUnits.Count; i++) {
            var u = _monsterUnits[i];
            if (u != null && u.IsAlive) _sepBuffer.Add(u);
        }

        int n = _sepBuffer.Count;
        if (n < 2) return;

        const int iterations = 2;
        for (int it = 0; it < iterations; it++) {
            for (int i = 0; i < n; i++) {
                var a = _sepBuffer[i];
                for (int j = i + 1; j < n; j++) {
                    var b = _sepBuffer[j];

                    // 두 몸통이 맞닿는 거리(반경 합)와 공용 최소 간격 중 큰 쪽까지 벌린다.
                    // 덕분에 덩치가 큰 유닛 안으로 다른 유닛이 들어가지 않는다.
                    float minDist = Mathf.Max(baseMinDist, a.BodyRadius + b.BodyRadius);
                    if (minDist <= 0f) continue;

                    Vector2 d = b.Position - a.Position;
                    float sqr = d.sqrMagnitude;
                    if (sqr >= minDist * minDist) continue;

                    Vector2 dir;
                    float overlap;
                    if (sqr > 0.0001f) {
                        float dist = Mathf.Sqrt(sqr);
                        dir = d / dist;
                        overlap = minDist - dist;
                    } else {
                        // 완전히 같은 위치: 인덱스로 결정적 방향을 만들어 대칭을 깬다(무작위 대신 재현 가능).
                        float ang = ((i * 97 + j * 41) % 360) * Mathf.Deg2Rad;
                        dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                        overlap = minDist;
                    }

                    // 밀림 분담: 몸통 반경을 질량처럼 써서 덩치가 큰 쪽이 덜 밀린다(반경이 같거나 둘 다 0 이면 반씩).
                    // 이렇게 하지 않으면 파티 전원이 큰 보스를 계속 밀어내 보스가 뒷걸음질친다.
                    float total = a.BodyRadius + b.BodyRadius;
                    float shareA = total > 0f ? b.BodyRadius / total : 0.5f;

                    float pushA = overlap * shareA;
                    float pushB = overlap - pushA;
                    if (maxStep > 0f) {
                        pushA = Mathf.Min(pushA, maxStep);
                        pushB = Mathf.Min(pushB, maxStep);
                    }
                    a.Nudge(-dir * pushA);
                    b.Nudge(dir * pushB);
                }
            }
        }
    }

    // ── 카메라 포커스 ────────────────────────────────
    // 카메라가 누구를 비출지는 유저가 고른다(하단 전투 캐릭터 패널의 슬롯 클릭 -> SetCameraFocus).
    // 고른 대상은 로컬 JSON 에 남아 씬을 옮기거나 앱을 다시 켜도 유지되고, 그 캐릭터가 덱에서 빠지면 대표로 초기화된다.
    // 저장값 0 은 "대표 추종"이라, 대표를 바꿔도 유저가 다시 고를 필요 없이 새 대표를 따라간다(기본 상태).
    // 대상이 바뀌는 시점은 파티 재구성과 유저 선택 둘뿐이라 매 프레임 확인하지 않고 그때만 갱신한다.
    private const int RepresentativeFocus = 0;

    private CharacterUnit _cameraFocus;

    /// <summary>지금 카메라가 비추는 캐릭터 id(대상이 없으면 -1). HUD 가 포커스 표시를 그릴 때 읽는다.</summary>
    public int CameraFocusCharId => _cameraFocus != null && _cameraFocus.Data != null ? _cameraFocus.Data.Id : -1;

    /// <summary>카메라 포커스 대상이 바뀔 때 발생(HUD 포커스 표시 갱신용).</summary>
    public event Action CameraFocusChanged;

    /// <summary>
    /// 카메라가 비출 캐릭터를 지정한다(현재 파티에 없는 id 는 무시). 대표를 고르면 "대표 추종"으로 되돌아가
    /// 이후 대표가 바뀌어도 자동으로 따라간다.
    /// </summary>
    public void SetCameraFocus(int charId) {
        var unit = FindPartyMember(charId);
        if (unit == null) return;

        SaveFocus(unit.IsRepresentative ? RepresentativeFocus : charId);
        UpdateCameraFocus();
    }

    private void UpdateCameraFocus() {
        var next = ResolveCameraFocus();
        if (next == null || next == _cameraFocus) return;

        _cameraFocus = next;
        // 카메라는 목표 위치로 보간 추적하므로 대상만 바꿔 주면 화면이 부드럽게 넘어간다(순간 이동 없음).
        battleCamera.SetTarget(next.transform);
        CameraFocusChanged?.Invoke();
    }

    // 지금 비춰야 할 파티원. 파티가 비어 있으면 null(마지막 대상을 그대로 비춘다).
    private CharacterUnit ResolveCameraFocus() {
        int saved = SavedFocusCharId;
        if (saved != RepresentativeFocus) {
            var chosen = FindPartyMember(saved);
            if (chosen != null) return chosen;
            SaveFocus(RepresentativeFocus);   // 고른 캐릭터가 덱 구성 변경으로 빠졌다 -> 대표로 초기화.
        }

        var rep = FindRepresentative();
        if (rep != null) return rep;

        // 대표가 지정되지 않은 덱에서도 화면이 빈 곳을 비추지 않도록 첫 파티원으로 폴백한다.
        return _party.Count > 0 ? _party[0] : null;
    }

    private CharacterUnit FindRepresentative() {
        for (int i = 0; i < _party.Count; i++) {
            var u = _party[i];
            if (u != null && u.IsRepresentative) return u;
        }
        return null;
    }

    private CharacterUnit FindPartyMember(int charId) {
        for (int i = 0; i < _party.Count; i++) {
            var u = _party[i];
            if (u != null && u.Data != null && u.Data.Id == charId) return u;
        }
        return null;
    }

    // 포커스 대상은 그 계정의 캐릭터라 계정별 칸(UserDataManager.Account)에 기록한다.
    // 같은 기기에서 다른 계정으로 들어와도 서로의 선택에 영향을 주지 않는다.
    private static int SavedFocusCharId {
        get {
            var account = UserDataManager.Account;
            return account != null ? account.battleFocusCharId : RepresentativeFocus;
        }
    }

    // 디스크 쓰기는 기존 SaveFlag/오토세이버 흐름에 맡긴다(줌 저장과 같은 방식).
    private static void SaveFocus(int charId) {
        var account = UserDataManager.Account;
        if (account == null || account.battleFocusCharId == charId) return;

        account.battleFocusCharId = charId;
        UserDataManager.RequestSave();
    }

    // ── 전투 BGM ────────────────────────────────────
    /// <summary>
    /// 이 전투의 BGM 을 튼다. 곡은 "지금 진행 중인 스테이지가 속한 던전 그룹(장)"이 정하며, 그룹에 곡이
    /// 배정돼 있지 않으면 공용 전투 BGM 으로 떨어진다. 의뢰/길드레이드/쟁탈전에는 자기 던전이 없으므로
    /// 플레이어의 현재 진도(Store.CurrentDungeonIndex)를 기준으로 삼는다 - 그래서 어느 전투에 들어가든
    /// 같은 장의 곡이 이어진다.
    /// </summary>
    /// <param name="dungeon">지금 실제로 도는 던전(있으면 진도 대신 이 던전의 그룹을 쓴다). 메인 전투 전용.</param>
    protected void PlayBattleBgm(DungeonData dungeon = null) {
        if (Sound == null) return;

        var group = dungeon != null ? dungeon.Group
                  : DungeonDb != null ? DungeonDb.GroupOf(Store != null ? Store.CurrentDungeonIndex.Value : 0)
                  : null;

        var bgm = group != null ? group.BattleBgm : null;
        if (bgm != null) Sound.PlayBgm(bgm);
        else Sound.PlayBgm(BgmKey.Battle);
    }

    // ── 투자: 스쿼드 재생 ───────────────────────────
    // 일정 간격마다 파티 전원이 최대 체력의 일부를 회복한다. 전투가 도는 모든 화면(메인/의뢰/길드 레이드/쟁탈전)이
    // 이 무대를 공유하므로 여기 한 곳에 두면 네 화면에 함께 적용된다.
    //
    // 파생 화면들이 각자 Update 를 갖고 있어 가상 메서드로 두었다. 파생에서 base.Update() 를 부르지 않으면
    // 그 화면만 재생이 멈추므로, 재정의할 때는 반드시 먼저 호출해야 한다.
    private float _regenTimer;

    /// <summary>스쿼드 재생이 도는 간격(초).</summary>
    internal const float SquadRegenInterval = 10f;

    /// <summary>
    /// 전투가 멈춰 있는지(일시정지). 무대가 이 값을 갖는 이유는 멈춰야 하는 것이 유닛만이 아니기 때문이다.
    /// 스쿼드 재생처럼 무대가 직접 돌리는 처리도 함께 멈춰야 "게임이 멈췄다"가 성립한다.
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// 일시정지 토글. Time.timeScale 을 건드리지 않고 이 무대의 진행만 멈춘다
    /// (timeScale 을 0 으로 만들면 유닛 이동/공격은 멈춰도 UI 트윈 규약이 깨지고 다른 시스템에 파급된다).
    ///
    /// 유닛은 Time.deltaTime 으로 스스로 움직이므로 컴포넌트 활성으로 멈추고(위치/상태는 그대로 보존된다),
    /// 무대가 직접 돌리는 처리는 IsPaused 를 보고 건너뛴다.
    /// </summary>
    public virtual void SetPaused(bool paused) {
        if (IsPaused == paused) return;
        IsPaused = paused;
        SetUnitsRunning(!paused);
    }

    protected void SetUnitsRunning(bool running) {
        for (int i = 0; i < Party.Count; i++)
            if (Party[i] != null) Party[i].enabled = running;

        var monsters = ActiveMonsters;
        for (int i = 0; i < monsters.Count; i++)
            if (monsters[i] != null) monsters[i].enabled = running;
    }

    protected virtual void Update() => TickSquadRegen();

    /// <summary>
    /// 스쿼드 재생 주기를 처음으로 되돌린다. 전투 1판이 시작될 때마다 불려야 하며, 호출 지점은
    /// RebuildParty 한 곳이다(모든 전투가 그 자리를 지나므로 화면별로 따로 챙길 필요가 없다).
    /// </summary>
    protected void ResetSquadRegen() => _regenTimer = 0f;

    private void TickSquadRegen() {
        // 멈춰 있는 동안은 시간이 흐르지 않아야 한다. 유닛과 달리 이 처리는 무대의 Update 에서 도는데,
        // 무대는 일시정지에도 계속 돌기 때문에 여기서 직접 걸러야 한다(그러지 않으면 멈춘 채로 회복된다).
        if (IsPaused) return;

        float ratio = InvestmentRules.ToRatio(Store != null ? Store.InvestmentPermille(InvestmentEffect.SquadRegen) : 0);
        if (ratio <= 0f) { _regenTimer = 0f; return; }   // 투자하지 않았으면 타이머도 돌리지 않는다.

        _regenTimer += Time.deltaTime;
        if (_regenTimer < SquadRegenInterval) return;
        _regenTimer -= SquadRegenInterval;

        foreach (var unit in _party) {
            if (unit == null || !unit.IsAlive) continue;

            float healed = unit.Heal(unit.MaxHp * ratio);
            if (healed <= 0f) continue;   // 이미 가득 찬 유닛은 표시하지 않는다(0 이 뜨면 고장으로 보인다).
            ((IBattleContext)this).ShowDamage(unit.HitPoint, healed, DamagePopupStyle.Heal);
        }
    }

    // ── 카메라 ──────────────────────────────────────
    // 줌은 사용자가 맞춰 둔 화면 취향이라 로컬 JSON 에 기억한다. 조작 진입점이 여기로 모이므로
    // 메인/의뢰 어느 씬에서 바꾸든 같은 값이 저장되고, 다음 진입 시 그대로 복원된다.

    /// <summary>지금 줌 값(직교 카메라 크기). 클수록 멀리 보인다.</summary>
    public float Zoom => battleCamera != null ? battleCamera.Zoom : 0f;

    /// <summary>줌 한 단계 범위(설정값). 핀치가 손가락 간격 변화를 줌 값으로 환산할 때 기준으로 쓴다.</summary>
    public float ZoomRange => Settings != null ? Settings.CameraMaxZoom - Settings.CameraMinZoom : 0f;

    /// <summary>
    /// 줌을 지정한 값으로 맞춘다(핀치처럼 연속으로 바뀌는 조작용). 허용 범위를 벗어난 값은 카메라가 잘라 준다.
    /// 저장은 버튼과 같은 경로를 쓰므로 조작 방식이 달라져도 기억되는 값은 하나다.
    /// </summary>
    public void SetZoom(float value) {
        if (battleCamera == null) return;
        battleCamera.Zoom = value;
        SaveZoom();
    }

    // 저장값이 없으면(0 이하) 카메라가 Configure 에서 잡아 둔 기본 줌을 그대로 쓴다.
    // 값이 있어도 카메라가 허용 범위로 잘라 주므로, 설정에서 줌 범위를 바꿔도 어긋나지 않는다.
    private void RestoreSavedZoom() {
        float saved = UserDataManager.Data != null ? UserDataManager.Data.BattleCameraZoom : 0f;
        if (saved > 0f) battleCamera.Zoom = saved;
    }

    // 디스크 쓰기는 기존 SaveFlag/오토세이버 흐름에 맡긴다(버튼을 누를 때마다 파일을 쓰지 않는다).
    private void SaveZoom() {
        if (UserDataManager.Data == null) return;
        UserDataManager.Data.BattleCameraZoom = battleCamera.Zoom;
        UserDataManager.RequestSave();
    }

    protected virtual void OnDestroy() {
        if (Store != null) Store.CharactersChanged -= OnCharactersChanged;
        _scheduleCts?.Cancel();
        _scheduleCts?.Dispose();
        _scheduleCts = null;
    }
}
