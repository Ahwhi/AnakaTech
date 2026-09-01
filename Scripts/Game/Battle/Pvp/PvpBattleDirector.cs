using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static Enums;

/// <summary>PvP 한 판의 결과.</summary>
public enum PvpBattleResult {
    InProgress,
    Win,    // 상대 파티 전멸
    Lose    // 내 파티 전멸 또는 제한 시간 종료(공격자가 못 이겼으면 방어 성공)
}

/// <summary>
/// 유저 대 유저 전투 오케스트레이터. 길드 대항전에서 처음 쓰지만 컨텐츠에 묶이지 않게 설계했다
/// (아레나 등 다른 유저간 배틀도 이 무대를 그대로 쓰고, 결과를 누구에게 보고할지만 씬 매니저가 정한다).
///
/// 방치 던전/의뢰와 다른 점은 "적이 몬스터가 아니라 상대 유저의 캐릭터"라는 것뿐이다.
/// 그래서 적도 CharacterUnit 으로 세우고, 상대 팀(Team.Enemy)으로 등록해 서로를 적으로 인식하게 한다.
///
/// 상대 능력치는 서버가 준 원본 상태(UserDeckDto: 성장 + 장비 + 각인 + 본부 + 수련)를 클라의 스탯 조립 규칙에
/// 그대로 통과시켜 만든다. 서버가 최종 스탯을 계산해 보내면 조립 규칙이 두 곳에 생기므로 그렇게 하지 않는다.
/// </summary>
public sealed class PvpBattleDirector : BattleStage {
    [Header("Enemy")]
    [Tooltip("상대 캐릭터로 쓸 유닛 프리팹. 내 파티와 같은 프리팹을 쓰되 팀만 다르게 세운다.")]
    [SerializeField] private CharacterUnit enemyUnitPrefab;

    [Header("Battle")]
    [Tooltip("전장 배경. 비우면 씬에 깔린 배경을 그대로 쓴다.")]
    [SerializeField] private Sprite backgroundSprite;

    // 대전은 양 팀이 마주 서는 구도라 던전의 원형 배치(BattleFormation)를 쓰지 않는다.
    // 그 배치는 사방에서 몬스터가 몰려오는 것을 전제로 해, 여기 적용하면 두 팀이 같은 원 위에서 겹친다.
    [Header("Formation (좌우 대치)")]
    [Tooltip("중앙에서 각 팀까지의 가로 거리. 내 파티는 왼쪽, 상대는 오른쪽에 선다.")]
    [SerializeField, Min(0f)] private float sideDistance = 6f;
    [Tooltip("같은 팀 유닛 사이의 세로 간격.")]
    [SerializeField, Min(0f)] private float lineSpacing = 1.6f;
    [Tooltip("바깥쪽(위아래 끝) 유닛일수록 뒤로 물러나는 정도. 0 이면 완전히 일직선으로 선다.")]
    [SerializeField, Min(0f)] private float lineStagger = 0.6f;
    [Tooltip("배치 흔들림. 0 이면 정확히 정렬된다.")]
    [SerializeField, Min(0f)] private float positionJitter = 0.2f;

    private readonly List<CharacterUnit> _enemyParty = new();
    // 스폰 시점의 상대 파티 최대 체력 합. 쓰러진 유닛은 전장에서 사라지므로(적 유닛 공통 처리)
    // 게이지 분모를 매번 다시 세면 값이 튄다. 그래서 시작할 때 한 번 재 둔다.
    private float _enemyMaxHpTotal;
    private float _remaining;
    private bool _finished;
    // Begin 은 서버 조회를 몇 번 거친 뒤에 불린다. 그 전까지 Update 가 돌면 아직 비어 있는 양쪽 파티를
    // 전멸로 보고 첫 프레임에 승패가 나 버리므로, 시작 전에는 판정 자체를 하지 않는다.
    private bool _started;

    /// <summary>전투가 끝났을 때. 씬 매니저가 결과 보고/팝업을 맡는다.</summary>
    public event Action<PvpBattleResult> Finished;

    /// <summary>PvP 는 부활이 없다. 쓰러진 쪽이 그대로 전멸 판정 대상이 된다.</summary>
    protected override float ReviveDelaySeconds => -1f;

    public string OpponentName { get; private set; } = "";

    /// <summary>상대 대표 캐릭터. HUD 가 상대 초상을 그릴 때 쓴다.</summary>
    public int OpponentRepresentativeCharId { get; private set; }
    /// <summary>상대 대표 캐릭터가 장착한 코스튬(0=기본 일러스트). 전투 HUD 의 상대 초상이 쓴다.</summary>
    public int OpponentRepresentativeCostumeId { get; private set; }

    /// <summary>상대 프로필 테두리. HUD 가 상대 초상 테두리를 그릴 때 쓴다.</summary>
    public int OpponentProfileFrameId { get; private set; }

    /// <summary>내 파티 전체 체력 비율(상단 좌측 게이지). 내 유닛은 쓰러져도 전장에 남으므로 분모를 그때그때 센다.</summary>
    public float MyHpRatio {
        get {
            float current = 0f, max = 0f;
            for (int i = 0; i < Party.Count; i++) {
                var u = Party[i];
                if (u == null) continue;
                max += Mathf.Max(1f, u.Stats.Health);
                if (u.IsAlive) current += u.CurrentHp;
            }
            return max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }
    }

    /// <summary>상대 파티 전체 체력 비율(상단 우측 게이지). 분모는 스폰 시점 합계로 고정한다.</summary>
    public float EnemyHpRatio {
        get {
            if (_enemyMaxHpTotal <= 0f) return 0f;
            float current = 0f;
            for (int i = 0; i < _enemyParty.Count; i++) {
                var u = _enemyParty[i];
                if (u != null && u.IsAlive) current += u.CurrentHp;
            }
            return Mathf.Clamp01(current / _enemyMaxHpTotal);
        }
    }

    public float TimeRatio => PvpBattleRules.BattleSeconds > 0
        ? Mathf.Clamp01(_remaining / PvpBattleRules.BattleSeconds) : 0f;

    public string TimeText => $"{Mathf.CeilToInt(Mathf.Max(0f, _remaining))}";

    // 상대 파티도 내 파티와 같은 보정(장비/각인/본부)을 받아야 하므로, 그 표들을 여기서도 들고 있는다.
    private EngravingDatabase _engravingDb;
    private HqNodeDatabase _hqNodeDb;
    // 상대가 끼운 코스튬의 스탯 보정을 내 파티와 같은 규칙으로 해석하기 위한 표.
    private CostumeDatabase _costumeDb;
    // 상대 편성의 시너지도 같은 규칙으로 살려야 하므로 시너지 표도 함께 들고 있는다.
    private SynergyDatabase _synergyDb;
    // 상대 편성원 id 목록(시너지 판정 기준). 상대 덱을 받을 때 1회 만들어 재사용한다.
    private readonly List<int> _opponentDeckIds = new();

    [Inject]
    public void Construct(BattleSettings settings, CharacterDatabase charDb, EquipmentDatabase equipDb,
                          PlayerStateStore store, ISoundService sound,
                          EngravingDatabase engravingDb, HqNodeDatabase hqNodeDb,
                          CostumeDatabase costumeDb, SynergyDatabase synergyDb,
                          CharacterAppearanceService appearance, DungeonDatabase dungeonDb) {
        ConfigureStage(settings, charDb, equipDb, store, sound, appearance, dungeonDb);
        _engravingDb = engravingDb;
        _hqNodeDb = hqNodeDb;
        _costumeDb = costumeDb;
        _synergyDb = synergyDb;
    }

    /// <summary>전투 시작. 씬 매니저가 서버 입장 처리(공격 횟수 소모)를 마친 뒤 호출한다.</summary>
    public void Begin(UserDeckDto opponent) {
        if (opponent == null || opponent.Members.Count == 0) {
            Debug.LogError("[PvpBattleDirector] 상대 편성이 비어 있습니다.");
            Finish(PvpBattleResult.Win);   // 싸울 상대가 없으면 공격자 승리로 본다.
            return;
        }

        InitializeSystems();

        OpponentName = opponent.Nickname;
        OpponentRepresentativeCharId = opponent.RepresentativeCharId;
        OpponentRepresentativeCostumeId = opponent.RepresentativeCostumeId;
        OpponentProfileFrameId = opponent.ProfileFrameId;
        _finished = false;
        SetPaused(false);
        _remaining = PvpBattleRules.BattleSeconds;

        ClearEnemyParty();   // 목록만 비운다(실제 파괴는 아래 ClearMonsters 가 한다).
        ClearMonsters();
        ClearExtraPlayerUnits();
        DamageMeter.Reset();
        SetBackground(backgroundSprite);
        RebuildParty();

        PlayBattleBgm();
        SpawnEnemyParty(opponent);
    }

    // 상대 파티를 적 팀으로 세운다. 내 파티와 마주 보도록 원형 배치를 좌우 반전해 배치한다.
    private void SpawnEnemyParty(UserDeckDto opponent) {
        if (enemyUnitPrefab == null) {
            Debug.LogError("[PvpBattleDirector] 상대 캐릭터 프리팹이 없습니다.");
            Finish(PvpBattleResult.Win);
            return;
        }

        _enemyMaxHpTotal = 0f;
        int count = opponent.Members.Count;

        // 상대의 재생 비율은 덱을 받은 이 시점에 한 번만 구한다(매 프레임 다시 셀 이유가 없다).
        _opponentRegenRatio = InvestmentRules.ToRatio(opponent.InvestmentPermille(InvestmentEffect.SquadRegen));
        _opponentRegenTimer = 0f;

        // 시너지는 편성 전체를 봐야 판정되므로 상대 편성원 id 를 먼저 모아 둔다.
        // 서버가 상대 편성을 그대로 보내 주기 때문에 시너지만 따로 내려받을 필요가 없다.
        _opponentDeckIds.Clear();
        for (int i = 0; i < count; i++) _opponentDeckIds.Add(opponent.Members[i].CharDataId);

        for (int i = 0; i < count; i++) {
            var member = opponent.Members[i];
            var data = CharDb.Get(member.CharDataId);
            if (data == null) continue;

            // 마스크 슬롯은 내 파티와 겹치지 않는 대역을 쓴다(스프라이트가 서로 섞이지 않게).
            int slot = EnemyMaskSlotBase + i;
            Vector3 spawn = SidePos(i, count, mine: false);
            // 각인은 항상 적용분(스탯)과 조건부(전투 중 발동)로 나뉘므로 한 번만 만들어 양쪽에 쓴다.
            var engravings = OpponentEngravings(member, opponent);
            var stats = BuildOpponentStats(data, member, opponent, engravings);

            // 상대의 외형도 서버가 준 코스튬 id 로 해석한다. 상대가 a 코스튬을 끼고 있으면 내 화면에서도
            // 기본 일러스트가 아니라 a 코스튬 일러스트로 서 있어야 한다.
            var appearance = Appearance != null ? Appearance.Other(data, member.CostumeId)
                                                : CharacterAppearance.Default(data);

            var unit = Instantiate(enemyUnitPrefab, spawn, Quaternion.identity, transform);
            // 팀은 Init 전에 정한다. 초기화 과정에서 팀에 따라 갈리는 처리가 있어도 어긋나지 않게 하기 위함이다.
            unit.SetTeam(Team.Enemy);
            unit.Init(appearance, stats, isRepresentative: false, spawn, slot, this, this, Settings);
            // 조건부 각인(전투 시작 N초간 등)은 유닛이 직접 켜고 끈다. 내 파티와 같은 통로로 넘긴다.
            unit.SetEngravings(engravings);
            // 상대의 투자(선제 스킬 게이지)도 내 파티와 같은 규칙으로 적용한다.
            // 빠뜨리면 상대만 빈 게이지로 시작해 대전이 기울어진다.
            unit.PrimeSkillGauge(InvestmentRules.ToRatio(opponent.InvestmentPermille(InvestmentEffect.SkillGaugeStart)));

            _enemyParty.Add(unit);
            _enemyMaxHpTotal += Mathf.Max(1f, stats.Health);
            RegisterEnemyUnit(unit);
        }

        if (_enemyParty.Count == 0) { Finish(PvpBattleResult.Win); return; }
        _started = true;
    }

    /// <summary>
    /// 상대 캐릭터의 최종 스탯. 서버가 준 원본 상태(성장 + 장비 + 각인 + 본부 + 수련 + 코스튬 + 편성 시너지)를
    /// 내 파티와 완전히 같은 조립 경로(CharacterBuild -> CharacterRuntime)에 태운다.
    ///
    /// 서버가 최종 스탯을 계산해 보내지 않는 이유는 그러면 조립 규칙이 서버/클라 두 곳에 생기기 때문이고,
    /// 그렇다고 보정을 빼고 성장분만 쓰면 "내 덱 100% 대 약해진 상대"가 되어 대전이 성립하지 않는다.
    /// </summary>
    private CombatStats BuildOpponentStats(CharacterData data, UserDeckMemberDto member, UserDeckDto opponent,
                                           EngravingLoadout engravings) {
        var bonus = CharacterBuild.EquipmentBonus(EquippedItemsOf(opponent, member.CharDataId), member.CharDataId, EquipDb)
                  + engravings.Always
                  + (_hqNodeDb != null ? _hqNodeDb.TotalBonus(opponent.HqNodeLevels) : new CombatStats())
                  + OpponentTraining(opponent, member.CharDataId)
                  + CharacterBuild.CostumeBonus(member.CostumeId, member.CharDataId, _costumeDb)
                  + CharacterBuild.SynergyBonus(member.CharDataId, _opponentDeckIds, _synergyDb);

        return new CharacterRuntime(data, ToProgress(member), Settings, bonus).FinalStats;
    }

    // 상대의 수련 효과. 내 파티와 같은 해석 규칙(TrainingRules)을 그대로 통과시키므로
    // 상대가 키운 사거리/2회공격/회피가 내 화면에서도 똑같이 동작한다.
    private static CombatStats OpponentTraining(UserDeckDto opponent, int charDataId) =>
        opponent.Trainings.TryGetValue(charDataId, out var t)
            ? TrainingRules.BonusOf(t.Effect, t.ValueTenth)
            : new CombatStats();

    private static CharacterProgress ToProgress(UserDeckMemberDto member) => new() {
        CharDataId = member.CharDataId,
        Level = member.Level,
        StarLevel = member.StarLevel,
        InvestedAttack = member.InvAttack,
        InvestedDefense = member.InvDefense,
        InvestedHealth = member.InvHealth,
        InvestedAttackSpeed = member.InvAttackSpeed,
        InvestedMoveSpeed = member.InvMoveSpeed,
        InvestedSkillAccel = member.InvSkillAccel,
    };

    private EngravingLoadout OpponentEngravings(UserDeckMemberDto member, UserDeckDto opponent) {
        opponent.EngravingSlots.TryGetValue(member.CharDataId, out var slots);
        return CharacterBuild.Engravings(slots, member.StarLevel, opponent.EngravingLevels, _engravingDb);
    }

    // 상대 파티의 스쿼드 재생. 주기/비율 규칙은 내 파티와 같고(BattleStage.TickSquadRegen) 대상만 다르다.
    // 회복 표시도 같은 팝업을 쓰므로 화면에서 양쪽이 같은 방식으로 읽힌다.
    private void TickOpponentRegen() {
        if (_opponentRegenRatio <= 0f) return;

        _opponentRegenTimer += Time.deltaTime;
        if (_opponentRegenTimer < BattleStage.SquadRegenInterval) return;
        _opponentRegenTimer -= BattleStage.SquadRegenInterval;

        foreach (var unit in _enemyParty) {
            if (unit == null || !unit.IsAlive) continue;

            float healed = unit.Heal(unit.MaxHp * _opponentRegenRatio);
            if (healed <= 0f) continue;
            ((IBattleContext)this).ShowDamage(unit.HitPoint, healed, DamagePopupStyle.Heal);
        }
    }

    // 상대의 재생 비율. 덱을 받은 시점에 한 번만 구해 둔다(매 프레임 다시 셀 이유가 없다).
    private float _opponentRegenRatio;
    private float _opponentRegenTimer;

    private static IEnumerable<EquipmentInstanceDto> EquippedItemsOf(UserDeckDto opponent, int charDataId) {
        foreach (var e in opponent.Equips)
            if (e != null && e.EquippedCharDataId == charDataId) yield return e;
    }

    protected override void Update() {
        base.Update();   // 무대 공통 처리(투자 스쿼드 재생 등)를 먼저 돌린다.
        if (!_started || _finished || IsPaused) return;

        // 상대에게도 같은 재생을 돌린다. 무대의 공통 처리는 내 파티만 보므로 상대 쪽은 여기서 챙긴다.
        TickOpponentRegen();

        // 전멸 판정이 먼저다(시간이 남아도 한쪽이 무너지면 그 자리에서 끝난다).
        if (AllDead(_enemyParty)) { Finish(PvpBattleResult.Win); return; }
        if (AllDead(Party)) { Finish(PvpBattleResult.Lose); return; }

        _remaining -= Time.deltaTime;
        // 시간 종료는 공격자가 이기지 못한 것이므로 방어 성공(공격자 패배)으로 본다.
        if (_remaining <= 0f) Finish(PvpBattleResult.Lose);
    }

    private void Finish(PvpBattleResult result) {
        if (_finished) return;
        _finished = true;
        Sound.PlaySfx(result == PvpBattleResult.Win ? SfxKey.DungeonClear : SfxKey.UnitDeath);
        Finished?.Invoke(result);
    }

    // 상대 파티는 이 화면에만 있는 유닛이라 무대의 공통 처리(내 파티 + 몬스터)가 알지 못한다.
    // 한쪽만 멈추면 멈춘 채로 상대에게 맞으므로 여기서 함께 끊는다.
    public override void SetPaused(bool paused) {
        if (IsPaused == paused) return;
        base.SetPaused(paused);
        SetRunning(_enemyParty, !paused);
    }

    // 적 유닛의 전장 등록 해제/파괴는 몬스터와 같은 경로(ClearMonsters)가 처리하므로 목록만 비운다.
    private void ClearEnemyParty() {
        _enemyParty.Clear();
        _enemyMaxHpTotal = 0f;
    }

    private static bool AllDead(IReadOnlyList<CharacterUnit> units) {
        if (units == null || units.Count == 0) return true;
        for (int i = 0; i < units.Count; i++)
            if (units[i] != null && units[i].IsAlive) return false;
        return true;
    }

    private static void SetRunning(IReadOnlyList<CharacterUnit> units, bool running) {
        if (units == null) return;
        for (int i = 0; i < units.Count; i++)
            if (units[i] != null) units[i].enabled = running;
    }

    /// <summary>내 파티 배치를 좌우 반전해 마주 보게 만든다.</summary>
    /// <summary>
    /// 내 파티 배치를 좌우 대치로 갈아끼운다(기본 구현인 원형 배치는 던전용이라 대전에 맞지 않는다).
    /// 상대 파티는 SpawnEnemyParty 가 같은 함수로 반대편에 세운다.
    /// </summary>
    protected override Vector3 CharacterRingPos(int index, int total) => SidePos(index, total, mine: true);

    // 한 팀을 중앙 기준 세로로 고르게 펴고, 바깥쪽 유닛일수록 뒤로 물려 앞뒤가 겹쳐 보이지 않게 한다.
    private Vector3 SidePos(int index, int total, bool mine) {
        float center = (total - 1) * 0.5f;
        float slot = index - center;                       // 가운데가 0, 위아래로 갈수록 커진다
        float depth = Mathf.Abs(slot) * lineStagger;       // 바깥쪽일수록 뒤로
        float x = (mine ? -1f : 1f) * (sideDistance + depth);

        return CenterPos + new Vector3(x + Jitter(), slot * lineSpacing + Jitter(), 0f);
    }

    private float Jitter() =>
        positionJitter <= 0f ? 0f : UnityEngine.Random.Range(-positionJitter, positionJitter);

    /// <summary>상대 파티의 마스크 정렬 대역(내 파티/몬스터와 겹치지 않는 구간).</summary>
    private const int EnemyMaskSlotBase = 32;
}
