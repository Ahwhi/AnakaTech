using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static Enums;

/// <summary>전선 한 판의 결말.</summary>
public enum FrontlineResult {
    /// <summary>몬스터를 전부 잡았다.</summary>
    Cleared,
    /// <summary>스쿼드가 전멸했다.</summary>
    Defeat,
    /// <summary>제한 시간 안에 몬스터를 전부 잡지 못했다(실패로 처리한다).</summary>
    TimeOver,
}

/// <summary>
/// 전선 한 판의 전투. 그 전선에 배치된 스쿼드 5명이 메인 던전처럼 한꺼번에 나서고, 전선 구성의 몬스터를 전부 잡으면 클리어,
/// 스쿼드가 전멸하거나 제한 시간(FrontlineDatabase, 기본 30초)이 다 되면 실패다(부활 없음).
///
/// 시너지는 메인 덱이 아니라 이 스쿼드로 판정한다. 기준 스탯 조립(BuildCharacterStats)에서 스쿼드를 넘기므로
/// 전투 중 성장/장비가 바뀌어 스탯이 다시 조립되어도 같은 기준이 유지된다.
/// 몬스터는 단계가 오를수록 체력, 공격력/방어력이 각자의 배율로 강해진다(FrontlineDatabase).
///
/// 판과 판 사이의 흐름(입장/보고/다음 단계)은 FrontlineSceneManager 가 맡고, 이 클래스는 한 판만 치른다.
/// </summary>
public sealed class FrontlineDirector : BattleStage {
    [Header("Formation Override (선택)")]
    [Tooltip("끄면 BattleSettings 의 공용 배치를 그대로 쓴다. 켜면 전선만 아래 값으로 배치한다.")]
    [SerializeField] private bool overrideFormation;
    [SerializeField] private BattleFormation formation = BattleFormation.Default;

    /// <summary>한 판이 끝났을 때.</summary>
    public event Action<FrontlineResult> Finished;

    private FrontlineDatabase _db;
    private FrontlineDatabase.Frontline _data;
    private readonly List<int> _members = new();
    private readonly Queue<MonsterData> _queue = new();
    private float _healthMultiplier = 1f;
    private float _attackMultiplier = 1f;
    private float _spawnTimer;
    private float _timeLimit;
    private float _timeRemaining;
    private int _totalMonsters;
    private bool _running;

    public int FrontlineId { get; private set; } = FrontlineSession.None;
    public int Stage { get; private set; }

    /// <summary>이번 판의 몬스터 체력 배율. 1단계는 1 이다.</summary>
    public float HealthMultiplier => _healthMultiplier;

    /// <summary>이번 판의 몬스터 공격력/방어력 배율. 1단계는 1 이다.</summary>
    public float AttackMultiplier => _attackMultiplier;

    /// <summary>남은 몬스터 수(대기열 + 전장). HUD 가 매 프레임 읽는다.</summary>
    public int RemainingMonsters => _queue.Count + ActiveMonsters.Count;

    /// <summary>이번 판의 전체 몬스터 수.</summary>
    public int TotalMonsters => _totalMonsters;

    /// <summary>이번 판에 잡은 몬스터 수.</summary>
    public int KilledMonsters => Mathf.Max(0, _totalMonsters - RemainingMonsters);

    /// <summary>
    /// 남은 몬스터 비율(1~0). 가득 찬 상태로 시작해 마지막 한 기를 잡으면 0 이 된다(토벌 의뢰 게이지와 같은 방향).
    /// </summary>
    public float RemainingRatio => _totalMonsters > 0 ? Mathf.Clamp01((float)RemainingMonsters / _totalMonsters) : 0f;

    /// <summary>이번 판의 남은 시간(초).</summary>
    public float TimeRemaining => _timeRemaining;

    /// <summary>남은 시간 비율(1~0).</summary>
    public float TimeProgress => _timeLimit > 0f ? Mathf.Clamp01(_timeRemaining / _timeLimit) : 1f;

    /// <summary>판이 진행 중인지(입장 응답 대기/결과 처리 중에는 false).</summary>
    public bool IsRunning => _running;

    private int MaxConcurrent =>
        _data != null && _data.MaxConcurrentMonsters > 0 ? _data.MaxConcurrentMonsters : Settings.MaxConcurrentMonsters;

    private float SpawnInterval =>
        _data != null && _data.SpawnIntervalSeconds > 0f ? _data.SpawnIntervalSeconds : Settings.SpawnIntervalSeconds;

    /// <summary>부활 없음. 스쿼드가 전멸하면 그 판은 실패다.</summary>
    protected override float ReviveDelaySeconds => -1f;

    /// <summary>파티는 메인 덱이 아니라 이 전선의 스쿼드다.</summary>
    protected override IReadOnlyList<int> PartyCharIds => _members;

    [Inject]
    public void Construct(BattleSettings settings, CharacterDatabase charDb, EquipmentDatabase equipDb,
                          PlayerStateStore store, ISoundService sound,
                          CharacterAppearanceService appearance, DungeonDatabase dungeonDb, FrontlineDatabase frontlineDb) {
        ConfigureStage(settings, charDb, equipDb, store, sound, appearance, dungeonDb);
        _db = frontlineDb;
    }

    /// <summary>한 판 시작. 다음 단계/이전 단계/재도전 모두 같은 경로로 다시 부른다.</summary>
    /// <param name="members">스쿼드(순서 = 배치 순서). 보유하지 않은 소녀는 건너뛴다.</param>
    public void Begin(int frontlineId, int stage, IReadOnlyList<int> members) {
        InitializeSystems();

        FrontlineId = frontlineId;
        Stage = stage;
        _running = false;
        _spawnTimer = 0f;
        _queue.Clear();
        _totalMonsters = 0;
        _timeLimit = _db != null ? _db.TimeLimitOf(frontlineId) : 30f;
        _timeRemaining = _timeLimit;

        SetPaused(false);
        ClearMonsters();
        DamageMeter.Reset();

        _members.Clear();
        if (members != null)
            foreach (int id in members)
                if (id != 0 && !_members.Contains(id) && CharDb.Get(id) != null && Store.GetProgress(id) != null)
                    _members.Add(id);

        if (_members.Count == 0) {
            Debug.LogError("[FrontlineDirector] 나설 소녀가 없습니다. 전선 배치를 확인하세요.");
            Finish(FrontlineResult.Defeat);
            return;
        }

        _data = _db != null ? _db.Get(frontlineId) : null;
        _healthMultiplier = _db != null ? _db.HealthMultiplierFor(frontlineId, stage) : 1f;
        _attackMultiplier = _db != null ? _db.AttackMultiplierFor(frontlineId, stage) : 1f;
        if (_db != null) foreach (var m in _db.BuildShuffledSpawnQueue(frontlineId)) _queue.Enqueue(m);
        if (_queue.Count == 0) {
            Debug.LogError($"[FrontlineDirector] 전선 {frontlineId} 의 몬스터 구성이 비어 있습니다. FrontlineDatabase 를 확인하세요.");
            Finish(FrontlineResult.Defeat);
            return;
        }
        _totalMonsters = _queue.Count;

        if (_db != null) SetBackground(_db.BackgroundOf(frontlineId));
        SetFormation(overrideFormation ? formation : Settings.Formation);
        PlayBattleBgm();
        RebuildParty();

        _running = true;
        FillSpawns();
    }

    /// <summary>판을 멈춘다(결과를 보고하지 않고 나갈 때). 남은 몬스터를 치운다.</summary>
    public void Abort() {
        _running = false;
        _queue.Clear();
        ClearMonsters();
    }

    // 시너지를 메인 덱이 아니라 이 스쿼드로 판정한다.
    protected override CombatStats BuildCharacterStats(CharacterData data, CharacterProgress progress) =>
        new CharacterRuntime(data, progress, Settings, Store.TotalBonusFor(data.Id, EquipDb, _members)).FinalStats;

    protected override void Update() {
        base.Update();
        if (!_running || IsPaused) return;

        if (IsPartyWiped) { Finish(FrontlineResult.Defeat); return; }
        if (_queue.Count == 0 && ActiveMonsters.Count == 0) { Finish(FrontlineResult.Cleared); return; }

        // 일시정지 중에는 위에서 빠져나가므로 시간도 함께 멈춘다.
        _timeRemaining = Mathf.Max(0f, _timeRemaining - Time.deltaTime);
        if (_timeRemaining <= 0f) { Finish(FrontlineResult.TimeOver); return; }

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f) return;
        _spawnTimer = SpawnInterval;
        FillSpawns();
    }

    private void FillSpawns() {
        while (_queue.Count > 0 && ActiveMonsters.Count < MaxConcurrent) {
            // 스폰에 실패하는 데이터(프리팹 누락 등)는 건너뛴다. 남겨 두면 대기열이 줄지 않아 판이 끝나지 않는다.
            SpawnScaled(_queue.Dequeue());
        }
    }

    // 단계 배율을 얹어 스폰한다. 스폰 직후라 체력 비율이 가득 찬 상태로 유지된다(ApplyStats 가 비율을 지킨다).
    private void SpawnScaled(MonsterData data) {
        var unit = SpawnMonster(data);
        if (unit == null) return;
        if (Mathf.Approximately(_healthMultiplier, 1f) && Mathf.Approximately(_attackMultiplier, 1f)) return;

        var stats = data.BaseStats;
        stats.Attack *= _attackMultiplier;
        stats.Health *= _healthMultiplier;
        stats.Defense *= _attackMultiplier;
        unit.ApplyStats(stats);
    }

    private void Finish(FrontlineResult result) {
        _running = false;
        ClearMonsters();
        SetPaused(false);
        Sound.PlaySfx(result == FrontlineResult.Cleared ? SfxKey.DungeonClear : SfxKey.UnitDeath);
        Finished?.Invoke(result);
    }
}
