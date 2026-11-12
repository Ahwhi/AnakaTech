using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static Enums;

/// <summary>무한 타워 한 판의 결말.</summary>
public enum TowerResult {
    InProgress,
    /// <summary>전투 시간을 버텼다(등반 성공).</summary>
    Cleared,
    /// <summary>마지막 소녀까지 쓰러졌다.</summary>
    Defeat,
}

/// <summary>
/// 무한 타워 전투(V1.12 부터 누적 합류식). 편성원이 한꺼번에 나서지 않고 편성 순서대로 한 명씩 합류한다.
///
///   1번 소녀가 혼자 전투를 시작하고, JoinSeconds 마다 다음 순번 소녀가 그 전장에 그대로 합류한다.
///   먼저 나선 소녀는 빠지지 않고 계속 싸운다(교대가 아니라 합류다).
///   합류한 소녀가 전부 쓰러졌는데 아직 합류하지 않은 소녀가 남아 있으면, 시간을 기다리지 않고 다음 소녀가 바로 합류한다
///   (전장이 빈 채로 시간만 흐르면 몬스터만 쌓여 다음 소녀가 서자마자 쓰러진다).
///   쓰러진 소녀는 이번 판에 일어나지 않는다. BattleSeconds 를 버티면 승리, 모두 합류한 뒤 전원이 쓰러지면 패배다.
///
/// 합류는 파티를 다시 세우지 않고 한 명을 더 세우는 것이다(BattleStage.AddPartyMember). 그래서 먼저 선 소녀의 체력/스킬/위치가
/// 그대로 이어진다. 하단 캐릭터 패널은 로스터(RosterCharIds)를 받아 아직 합류하지 않은 소녀까지 순번대로 나열한다.
///
/// 층 보너스는 기준 스탯 조립(BuildCharacterStats)에 얹으므로, 전투 중 성장/장비가 바뀌어 스탯이 다시 조립되어도 빠지지 않는다.
/// 몬스터는 층 템플릿을 반복해 내보내며, 템플릿을 한 바퀴 돌 때마다 공격력/체력/방어력이 배율만큼 강해진다(TowerDatabase).
/// </summary>
public sealed class TowerDirector : BattleStage {
    [Header("Formation Override (선택)")]
    [Tooltip("끄면 BattleSettings 의 공용 배치를 그대로 쓴다. 켜면 무한 타워만 아래 값으로 배치한다.")]
    [SerializeField] private bool overrideFormation;
    [SerializeField] private BattleFormation formation = BattleFormation.Default;

    /// <summary>한 판이 끝났을 때.</summary>
    public event Action<TowerResult> Finished;

    /// <summary>다음 소녀가 합류했을 때(판의 첫 소녀는 합류 안내 없이 시작한다). 인자는 새로 합류한 소녀.</summary>
    public event Action<CharacterData> MemberJoined;

    private TowerDatabase _db;
    private TowerDatabase.FloorTemplate _template;
    private float _monsterMultiplier = 1f;
    private readonly Queue<MonsterData> _queue = new();
    private float _spawnTimer;

    private readonly List<int> _order = new();
    // 지금 전장에 선(합류한) 소녀들. 파티를 다시 세워야 하는 경우(RebuildParty)에도 이 목록이 기준이 된다.
    private readonly List<int> _current = new();
    // 원형 배치의 자리. 판을 시작할 때 전체 인원으로 한 번 섞어 두고 합류하는 소녀가 자기 자리에 선다.
    private int[] _places = Array.Empty<int>();
    private int _joined;   // 합류한 소녀 수(= 다음에 합류할 순번 인덱스)

    private float _remaining;
    private float _joinRemaining;
    private float _joinDuration;
    private bool _running;
    private bool _finished;

    public int Floor { get; private set; }
    public TowerBonus Bonus { get; private set; }

    /// <summary>합류한 소녀 수(1부터). 시작 전이면 0.</summary>
    public int RunnerNumber => _joined;
    public int RunnerCount => _order.Count;
    /// <summary>합류할 소녀가 남지 않았는지(전원이 이미 전장에 섰다).</summary>
    public bool IsLastRunner => _joined > 0 && !HasWaiting;

    private bool HasWaiting => _joined < _order.Count;

    /// <summary>판 전체의 남은 시간(초).</summary>
    public float RemainingTime => Mathf.Max(0f, _remaining);
    public float TimeProgress => TowerRules.BattleSeconds > 0f ? Mathf.Clamp01(RemainingTime / TowerRules.BattleSeconds) : 0f;

    /// <summary>다음 소녀가 합류하기까지 남은 시간(초). 전원이 합류했으면 판의 남은 시간과 같다.</summary>
    public float RelayRemaining => HasWaiting ? Mathf.Max(0f, _joinRemaining) : RemainingTime;
    public float RelayProgress => _joinDuration > 0f ? Mathf.Clamp01(RelayRemaining / _joinDuration) : 0f;

    /// <summary>이번 판의 몬스터 배율(공격력/체력/방어력). 1~10층은 1 이다.</summary>
    public float MonsterMultiplier => _monsterMultiplier;

    private int MaxConcurrent =>
        _template != null && _template.MaxConcurrentMonsters > 0 ? _template.MaxConcurrentMonsters : Settings.MaxConcurrentMonsters;

    private float SpawnInterval =>
        _template != null && _template.SpawnIntervalSeconds > 0f ? _template.SpawnIntervalSeconds : Settings.SpawnIntervalSeconds;

    /// <summary>쓰러진 소녀는 일어나지 않는다. 대신 아직 합류하지 않은 소녀가 이어서 나선다.</summary>
    protected override float ReviveDelaySeconds => -1f;

    /// <summary>파티는 지금까지 합류한 소녀들이다(합류할 때마다 한 명씩 늘어난다).</summary>
    protected override IReadOnlyList<int> PartyCharIds => _current;

    /// <summary>하단 패널은 전장에 선 소녀만이 아니라 순번 전체를 보여 준다.</summary>
    public override IReadOnlyList<int> RosterCharIds => _order;

    // 전장에 선 소녀는 패널이 유닛으로 직접 그리므로, 여기로 오는 것은 아직 합류하지 않은(대기) 소녀뿐이다.
    public override BattleRosterState RosterStateOf(int charId) => BattleRosterState.Waiting;

    public override float RosterHpRatioOf(int charId) => 1f;

    [Inject]
    public void Construct(BattleSettings settings, CharacterDatabase charDb, EquipmentDatabase equipDb,
                          PlayerStateStore store, ISoundService sound,
                          CharacterAppearanceService appearance, DungeonDatabase dungeonDb, TowerDatabase towerDb) {
        ConfigureStage(settings, charDb, equipDb, store, sound, appearance, dungeonDb);
        _db = towerDb;
    }

    /// <summary>한 판 시작. 재도전도 같은 인자로 다시 부른다.</summary>
    /// <param name="members">합류 순번(앞에서부터 나선다). 보유하지 않은 소녀/중복은 건너뛴다.</param>
    public void Begin(int floor, TowerBonus bonus, IReadOnlyList<int> members) {
        InitializeSystems();

        Floor = floor;
        Bonus = bonus;
        _finished = false;
        _running = false;
        _remaining = TowerRules.BattleSeconds;
        _joined = 0;
        _joinRemaining = 0f;
        _joinDuration = 0f;
        _spawnTimer = 0f;
        _queue.Clear();   // 재도전이 앞 판의 남은 대기열을 이어받지 않게 한다.

        SetPaused(false);
        ClearMonsters();
        ClearExtraPlayerUnits();
        DamageMeter.Reset();

        _order.Clear();
        if (members != null)
            foreach (int id in members)
                if (id != 0 && !_order.Contains(id) && CharDb.Get(id) != null && Store.GetProgress(id) != null)
                    _order.Add(id);

        // 앞 판의 파티를 비운다(재도전). 빈 목록으로 다시 세우면 기존 유닛이 정리되고 예약 스킬/스쿼드 재생도 초기화된다.
        _current.Clear();
        RebuildParty();

        if (_order.Count == 0) {
            Debug.LogError("[TowerDirector] 나설 소녀가 없습니다. 편성을 확인하세요.");
            Finish(TowerResult.Defeat);
            return;
        }

        _template = _db != null ? _db.TemplateFor(floor) : null;
        _monsterMultiplier = _db != null ? _db.StatMultiplierFor(floor) : 1f;
        if (_template == null || _db.MonsterCountFor(floor) == 0) {
            Debug.LogError($"[TowerDirector] {floor}층 몬스터 구성이 비어 있습니다. TowerDatabase 를 확인하세요.");
            Finish(TowerResult.Defeat);
            return;
        }

        if (_db != null) SetBackground(_db.BackgroundFor(floor));
        SetFormation(overrideFormation ? formation : Settings.Formation);
        PlayBattleBgm();

        // 자리는 전체 인원 기준으로 한 번만 섞는다. 합류할 때마다 다시 섞으면 이미 선 소녀의 자리 기준이 흔들린다.
        _places = ShuffledPlaces(_order.Count);

        if (!JoinNext()) {
            Debug.LogError("[TowerDirector] 첫 소녀를 세우지 못했습니다.");
            Finish(TowerResult.Defeat);
            return;
        }
        RefillQueue();
        _running = true;
        FillSpawns();
    }

    // 층 보너스는 기준 스탯 조립 단계에 얹는다. 성장/장비 변경으로 스탯을 다시 조립해도(RefreshPartyStats) 보너스가 남는다.
    protected override CombatStats BuildCharacterStats(CharacterData data, CharacterProgress progress) =>
        Bonus.Apply(base.BuildCharacterStats(data, progress), data);

    /// <summary>
    /// 다음 순번 소녀를 전장에 합류시킨다. 세우지 못한 소녀(데이터가 사라짐 등)는 건너뛰고 그 다음 소녀를 시도한다.
    /// 누군가 합류했으면 true, 더 합류할 소녀가 없으면 false.
    /// </summary>
    private bool JoinNext() {
        while (HasWaiting) {
            int index = _joined++;
            int charId = _order[index];
            int place = index < _places.Length ? _places[index] : index;

            var unit = AddPartyMember(charId, place, _order.Count);
            if (unit == null) {
                Debug.LogWarning($"[TowerDirector] {charId} 소녀를 합류시키지 못해 건너뜁니다.");
                continue;
            }

            _current.Add(charId);
            ApplyDurationBonus(unit);

            // 다음 합류까지의 시간. 전원이 합류했으면 판의 남은 시간이 곧 버텨야 할 몫이다.
            _joinRemaining = TowerRules.JoinSeconds;
            _joinDuration = HasWaiting ? TowerRules.JoinSeconds : _remaining;

            // 판의 첫 소녀는 안내하지 않는다(시작과 동시에 서는 것이라 합류가 아니다).
            if (index > 0) MemberJoined?.Invoke(unit.Data);
            return true;
        }
        return false;
    }

    // 상태이상 지속시간 보너스는 스탯이 아니라 유닛이 "거는" 값이라 합류한 유닛에 직접 건다.
    private void ApplyDurationBonus(CharacterUnit unit) {
        var target = Bonus.DurationTarget;
        if (target == null || unit == null || unit.Data == null) return;

        var progress = Store.GetProgress(unit.Data.Id);
        if (Bonus.AppliesTo(unit.Data, progress != null && progress.LimitBroken))
            unit.SetInflictDurationScale(target.Value, Bonus.Value);
    }

    protected override void Update() {
        base.Update();   // 무대 공통 처리(투자 스쿼드 재생 등). IsPaused 면 그쪽도 건너뛴다.
        if (!_running || _finished || IsPaused) return;

        float dt = Time.deltaTime;

        // 전장에 선 소녀가 전부 쓰러졌으면 시간보다 먼저 처리한다(같은 프레임에 시간이 다 돼도 쓰러진 판은 이긴 판이 아니다).
        // 아직 합류하지 않은 소녀가 있으면 기다리지 않고 바로 세운다. 없으면 패배다.
        if (IsPartyWiped || Party.Count == 0) {
            if (!JoinNext()) { Finish(TowerResult.Defeat); return; }
        }

        _remaining -= dt;
        if (_remaining <= 0f) { Finish(TowerResult.Cleared); return; }

        if (HasWaiting) {
            _joinRemaining -= dt;
            // 한 프레임이 길어도(일시정지 해제 직후 등) 한 번에 한 명만 합류한다. 남은 몫은 다음 합류 시간에서 다시 센다.
            if (_joinRemaining <= 0f) JoinNext();
        }

        _spawnTimer -= dt;
        if (_spawnTimer > 0f) return;
        _spawnTimer = SpawnInterval;
        FillSpawns();
    }

    // 층 구성을 한 줄로 펼쳐 섞는다(같은 종류가 몰려 나오지 않게).
    private void RefillQueue() {
        var list = new List<MonsterData>();
        foreach (var s in _template.Spawns) {
            if (s.Monster == null) continue;
            for (int i = 0; i < Mathf.Max(1, s.Count); i++) list.Add(s.Monster);
        }
        for (int i = list.Count - 1; i > 0; i--) {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        foreach (var m in list) _queue.Enqueue(m);
    }

    private void FillSpawns() {
        while (ActiveMonsters.Count < MaxConcurrent) {
            if (_queue.Count == 0) {
                if (_db == null || !_db.LoopSpawns) return;
                RefillQueue();
                if (_queue.Count == 0) return;
            }
            // 스폰에 실패하는 데이터(프리팹 누락 등)가 있으면 자리가 채워지지 않아 이 반복이 끝나지 않는다. 그 자리에서 멈춘다.
            if (SpawnScaled(_queue.Dequeue()) == null) return;
        }
    }

    // 층 배율을 얹어 스폰한다. 스폰 직후라 체력 비율이 가득 찬 상태로 유지된다(ApplyStats 가 비율을 지킨다).
    private MonsterUnit SpawnScaled(MonsterData data) {
        var unit = SpawnMonster(data);
        if (unit == null || Mathf.Approximately(_monsterMultiplier, 1f)) return unit;

        var stats = data.BaseStats;
        stats.Attack *= _monsterMultiplier;
        stats.Health *= _monsterMultiplier;
        stats.Defense *= _monsterMultiplier;
        unit.ApplyStats(stats);
        return unit;
    }

    private void Finish(TowerResult result) {
        if (_finished) return;
        _finished = true;
        _running = false;

        ClearMonsters();
        SetPaused(false);
        Sound.PlaySfx(result == TowerResult.Cleared ? SfxKey.DungeonClear : SfxKey.UnitDeath);
        Finished?.Invoke(result);
    }
}
