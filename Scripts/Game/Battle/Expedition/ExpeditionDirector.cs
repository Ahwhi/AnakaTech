using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static Enums;

/// <summary>원정대 한 판의 결과.</summary>
public enum ExpeditionResult {
    InProgress,
    TimeOver,   // 제한 시간 종료(정상 종료다. 남은 체력은 다음 파티원이 이어 깎는다)
    Cleared,    // 장소 체력을 다 깎음(레벨 상승)
    Wiped       // 아군 전멸(부활이 없으므로 더 깎을 수 없다)
}

/// <summary>
/// 원정대 전투 오케스트레이터(길드 레이드 GuildRaidDirector 와 같은 자리).
///
/// 길드 레이드와 다른 점은 두 가지다.
///  1) 아군: 내 캐릭터 1명(이번 주에 묶인 캐릭터) + 다른 파티원 4명. 파티원은 서버 스냅샷을 쟁탈전 상대와 같은 조립기로 재현한다.
///  2) 적: 장소 레벨에 구성된 몬스터 묶음이다. 장소 체력(서버 값)을 묶음 몬스터들에게 나눠 각자의 체력으로 덮어쓴다.
///     몬스터가 1기면 그 몬스터가 곧 장소 체력을 가진 보스다. 묶음을 모두 잡으면 장소 체력을 다 깎은 것이므로 클리어다.
/// 처치 판정과 레벨업은 서버가 한다. 여기서는 제한 시간 동안 깎은 피해량만 결과로 낸다.
/// </summary>
public sealed class ExpeditionDirector : BattleStage, IBossGaugeSource {
    [Header("Battle")]
    [Tooltip("장소 데이터에 배경이 없을 때 쓸 전장 배경. 비우면 씬에 깔린 배경을 그대로 쓴다.")]
    [SerializeField] private Sprite defaultBackground;

    [Header("Formation Override (선택)")]
    [Tooltip("끄면 BattleSettings 의 공용 배치를 그대로 쓴다. 켜면 원정대만 아래 값으로 배치한다.")]
    [SerializeField] private bool overrideFormation;
    [SerializeField] private BattleFormation formation = BattleFormation.Default;

    // 파티원 유닛의 마스크 정렬 슬롯 대역. 내 파티(0~)와 몬스터(100~) 사이를 쓴다.
    private const int AllyMaskSlotBase = 16;

    private ExpeditionDatabase _db;
    private RemoteCharacterBuilder _remoteBuilder;

    private ExpeditionLocation _location;
    private int _level;
    private long _startHp;       // 입장 시점 장소 체력(묶음 몬스터 체력 합의 시작값)
    private long _maxHp;
    private float _remaining;
    private bool _finished;
    // Begin 은 서버 조회를 몇 번 거친 뒤에 불린다. 그 전까지는 판정 자체를 하지 않는다(빈 전장을 전멸로 보지 않게).
    private bool _started;
    private long _finalDealtDamage;
    private float _finalHpRatio;
    // 이번 판의 몬스터 묶음. 남은 체력 합이 곧 남은 장소 체력이다.
    private readonly List<MonsterUnit> _wave = new();

    // 내 파티는 묶인 캐릭터 1명이다. 파티 생성(RebuildParty)이 이 목록을 그대로 쓴다.
    private readonly List<int> _myParty = new(1);
    // 아군 전체(나 + 파티원)의 자리 번호. 0 번이 나, 그 뒤가 파티원 순서다.
    private int[] _places = Array.Empty<int>();

    // 파티원의 스쿼드 재생(각자 자기 투자 기준). 무대 공통 재생은 내 파티만 보므로 파티원 몫은 여기서 돌린다.
    private readonly List<(CharacterUnit unit, float ratio)> _allyRegen = new();
    private float _allyRegenTimer;

    /// <summary>판이 끝났을 때(시간 종료/클리어/전멸). 씬 매니저가 결과 보고/팝업을 맡는다.</summary>
    public event Action<ExpeditionResult, long> Finished;

    protected override float ReviveDelaySeconds => -1f;

    protected override IReadOnlyList<int> PartyCharIds => _myParty;

    public ExpeditionLocation Location => _location;

    // ── IBossGaugeSource ──────────────────────────────
    /// <summary>이번 판에서 깎은 피해량(입장 시점 장소 체력 - 묶음의 남은 체력 합). 초과 피해는 들어가지 않는다.</summary>
    public long DealtDamage {
        get {
            if (_finished) return _finalDealtDamage;
            return Math.Max(0L, _startHp - RemainingWaveHp());
        }
    }

    public float BossHpRatio => _finished ? _finalHpRatio
        : (_maxHp > 0 ? Mathf.Clamp01((float)(_startHp - DealtDamage) / _maxHp) : 0f);

    public string BossName => _db != null ? _db.NameOf(_location) : _location.ToString();
    public string BossLevelText => ExpeditionRules.LevelText(_level);

    public float TimeRatio => ExpeditionRules.BattleSeconds > 0
        ? Mathf.Clamp01(_remaining / ExpeditionRules.BattleSeconds) : 0f;

    public string TimeText => $"{Mathf.CeilToInt(Mathf.Max(0f, _remaining))}";

    [Inject]
    public void Construct(BattleSettings settings, CharacterDatabase charDb, EquipmentDatabase equipDb,
                          PlayerStateStore store, ISoundService sound,
                          EngravingDatabase engravingDb, HqNodeDatabase hqNodeDb,
                          CostumeDatabase costumeDb, SynergyDatabase synergyDb,
                          CharacterAppearanceService appearance, DungeonDatabase dungeonDb,
                          ExpeditionDatabase expeditionDb) {
        ConfigureStage(settings, charDb, equipDb, store, sound, appearance, dungeonDb);
        _remoteBuilder = new RemoteCharacterBuilder(settings, equipDb, engravingDb, hqNodeDb, costumeDb, synergyDb);
        _db = expeditionDb;
    }

    /// <summary>
    /// 전투 시작. 씬 매니저가 서버 입장 처리(오늘 횟수 소모 + 장소 점유)와 파티원 편성 조회를 마친 뒤 호출한다.
    /// teammates 는 파티원과 그 편성 스냅샷이며, 편성이 비어 있는 파티원은 서지 않는다.
    /// </summary>
    public void Begin(ExpeditionLocation location, int level, long hp, long maxHp, int myCharDataId,
                      IReadOnlyList<(ExpeditionMemberDto member, UserDeckDto deck)> teammates) {
        InitializeSystems();

        _location = location;
        _level = level;
        _startHp = Math.Max(0, hp);
        _maxHp = Math.Max(1, maxHp);
        _finished = false;
        SetPaused(false);
        _remaining = ExpeditionRules.BattleSeconds;

        _myParty.Clear();
        _myParty.Add(myCharDataId);
        _places = ShuffledPlaces(1 + (teammates?.Count ?? 0));

        ClearMonsters();
        _wave.Clear();
        ClearExtraPlayerUnits();
        _allyRegen.Clear();
        _allyRegenTimer = 0f;
        DamageMeter.Reset();

        var locationData = _db != null ? _db.Get(location) : null;
        SetBackground(locationData != null && locationData.background != null ? locationData.background : defaultBackground);
        SetFormation(overrideFormation ? formation : Settings.Formation);
        RebuildParty();
        SpawnTeammates(teammates);

        PlayBattleBgm();

        if (!SpawnWave()) {
            Debug.LogError($"[ExpeditionDirector] {location} Lv.{level} 몬스터 구성이 비어 있습니다. ExpeditionDatabase 를 확인하세요.");
            Finish(ExpeditionResult.TimeOver);
            return;
        }
        _started = true;
    }

    // 내 캐릭터는 0 번 자리에 선다. 아군 전체 인원 기준의 원형 배치를 써야 파티원과 겹치지 않는다.
    protected override Vector3 CharacterRingPos(int index, int total) =>
        base.CharacterRingPos(_places.Length > 0 ? _places[0] : index, Math.Max(total, _places.Length));

    private void SpawnTeammates(IReadOnlyList<(ExpeditionMemberDto member, UserDeckDto deck)> teammates) {
        if (teammates == null) return;

        for (int i = 0; i < teammates.Count; i++) {
            var (member, deck) = teammates[i];
            if (deck == null) continue;

            var snapshot = FindMember(deck, member.CharDataId);
            var data = snapshot != null ? CharDb.Get(snapshot.CharDataId) : null;
            if (data == null) continue;

            // 시너지는 그 유저의 편성 기준이다(전투력 표시와 같은 기준이라 화면 숫자와 실제 세기가 어긋나지 않는다).
            var deckIds = new List<int>(deck.Members.Count);
            foreach (var m in deck.Members) deckIds.Add(m.CharDataId);

            var engravings = _remoteBuilder.Engravings(snapshot, deck);
            var stats = _remoteBuilder.Stats(data, snapshot, deck, engravings, deckIds);
            var appearance = Appearance != null ? Appearance.Other(data, snapshot.CostumeId, snapshot.UnitSkinId)
                                                : CharacterAppearance.Default(data);

            Vector3 spawn = base.CharacterRingPos(_places[i + 1], _places.Length);
            var unit = SpawnAllyCharacter(appearance, stats, snapshot.LimitBroken, spawn, AllyMaskSlotBase + i);
            unit.SetEngravings(engravings);
            unit.PrimeSkillGauge(InvestmentRules.ToRatio(deck.InvestmentPermille(InvestmentEffect.SkillGaugeStart)));

            float regen = InvestmentRules.ToRatio(deck.InvestmentPermille(InvestmentEffect.SquadRegen));
            if (regen > 0f) _allyRegen.Add((unit, regen));
        }
    }

    private static UserDeckMemberDto FindMember(UserDeckDto deck, int charDataId) {
        foreach (var m in deck.Members) if (m.CharDataId == charDataId) return m;
        return null;
    }

    // 몬스터 묶음을 스폰하고 장소 체력을 나눠 준다. 스폰할 것이 없으면 false.
    // 최대 체력은 레벨 최대 체력을, 현재 체력은 남은 장소 체력을 마릿수로 나눈 값이다(나머지는 첫 몬스터 몫).
    private bool SpawnWave() {
        var monsters = _db != null ? _db.MonstersFor(_location, _level) : null;
        if (monsters == null || monsters.Count == 0) return false;

        // 묶음 체력은 HUD 의 장소 체력 게이지가 보여 주므로, 1기일 때는 머리 위 체력바를 띄우지 않는다.
        bool showBars = monsters.Count > 1;
        foreach (var m in monsters) {
            if (m == null) continue;
            var unit = SpawnMonster(m, showBars);
            if (unit != null) _wave.Add(unit);
        }
        if (_wave.Count == 0) return false;

        long count = _wave.Count;
        for (int i = 0; i < _wave.Count; i++) {
            long max = _maxHp / count + (i == 0 ? _maxHp % count : 0);
            long hp = _startHp / count + (i == 0 ? _startHp % count : 0);
            _wave[i].OverrideHealth(max, hp);
        }
        // float 체력으로 옮기며 생기는 반올림 차이가 피해량으로 잡히지 않도록 시작값을 실제 합으로 맞춘다.
        _startHp = RemainingWaveHp();
        return true;
    }

    private long RemainingWaveHp() {
        double sum = 0;
        foreach (var m in _wave)
            if (m != null && m.IsAlive) sum += m.CurrentHp;
        return (long)Math.Ceiling(sum);
    }

    protected override void Update() {
        base.Update();   // 무대 공통 처리(내 파티 스쿼드 재생 등)를 먼저 돌린다.
        if (!_started || _finished || IsPaused) return;

        TickAllyRegen();

        // 묶음을 모두 잡았으면(장소 체력을 다 깎았으면) 시간이 남아도 즉시 끝낸다.
        if (RemainingWaveHp() <= 0) { Finish(ExpeditionResult.Cleared); return; }

        // 부활이 없으므로 아군이 모두 쓰러지면 더 깎을 수 없다. 빈 전장을 지켜보게 두지 않는다.
        if (IsAllyTeamWiped) { Finish(ExpeditionResult.Wiped); return; }

        _remaining -= Time.deltaTime;
        if (_remaining <= 0f) Finish(ExpeditionResult.TimeOver);
    }

    // 파티원 스쿼드 재생. 주기/표시는 무대 공통 재생(BattleStage.TickSquadRegen)과 같고 비율만 각자 것이다.
    private void TickAllyRegen() {
        if (_allyRegen.Count == 0) return;

        _allyRegenTimer += Time.deltaTime;
        if (_allyRegenTimer < SquadRegenInterval) return;
        _allyRegenTimer -= SquadRegenInterval;

        foreach (var (unit, ratio) in _allyRegen) {
            if (unit == null || !unit.IsAlive) continue;
            float healed = unit.Heal(unit.MaxHp * ratio);
            if (healed > 0f) ((IBattleContext)this).ShowDamage(unit.HitPoint, healed, DamagePopupStyle.Heal);
        }
    }

    private void Finish(ExpeditionResult result) {
        if (_finished) return;

        // 전장을 정리하기 전에 마지막 값을 붙잡아 둔다. _finished 를 세운 뒤에 읽으면 캐시된 값(0)이 나오므로 순서가 중요하다.
        _finalDealtDamage = DealtDamage;
        _finalHpRatio = BossHpRatio;
        _finished = true;

        ClearMonsters();
        Sound.PlaySfx(result == ExpeditionResult.Cleared ? SfxKey.DungeonClear : SfxKey.UnitDeath);
        Finished?.Invoke(result, _finalDealtDamage);
    }
}
