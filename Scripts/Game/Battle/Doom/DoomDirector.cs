using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static Enums;

/// <summary>멸망전 한 판의 결말.</summary>
public enum DoomResult {
    InProgress,
    /// <summary>만들어 둔 페이즈를 전부 깼다(다음 업데이트 안내).</summary>
    AllCleared,
    /// <summary>전멸했거나 제한 시간 안에 페이즈를 끝내지 못했다.</summary>
    Defeat,
}

/// <summary>
/// 멸망전 전투. 페이즈 단위로 몬스터를 내보내고, 그 페이즈의 몬스터를 전부 잡으면 다음으로 넘어간다.
///
/// 의뢰의 토벌과 규칙이 같지만 두 가지가 다르다.
///   1) 참전이 덱이 아니라 보유 소녀 전원이다.
///   2) 페이즈가 넘어가도 파티를 다시 만들지 않는다. 체력과 전투불능이 그대로 이어져야 하기 때문이다.
///      (RebuildParty 를 부르면 전원이 만피로 되살아나 컨텐츠가 성립하지 않는다.)
///      대신 스킬 쿨타임 게이지만 초기화한다.
///
/// 점수는 그 판에서 처치한 누적 마리 수이고, 패배해도 그때까지의 점수가 남는다.
/// </summary>
public sealed class DoomDirector : BattleStage {
    /// <summary>페이즈가 바뀌었을 때(증강 선택을 띄울 자리). 인자는 시작할 페이즈 번호.</summary>
    public event Action<int> PhaseStarting;

    /// <summary>한 판이 끝났을 때.</summary>
    public event Action<DoomResult> Finished;

    private DoomDatabase _db;
    private DoomPhaseData _phase;
    private Queue<MonsterData> _queue;
    private readonly List<int> _partyIds = new();

    private int _phaseNumber;
    private int _phaseTarget;
    private int _phaseKilled;
    private float _remainingTime;
    private float _spawnTimer;
    private bool _running;
    private bool _finished;

    /// <summary>그 판에서 처치한 누적 마리 수(= 점수).</summary>
    public int Score { get; private set; }

    /// <summary>지금 진행 중인 페이즈 번호(1부터).</summary>
    public int PhaseNumber => _phaseNumber;

    public DoomPhaseData CurrentPhase => _phase;

    /// <summary>이번 판에서 고른 증강의 누적. 판이 끝나면 버려진다.</summary>
    public DoomAugmentState Augments { get; } = new();

    public int MaxConcurrent =>
        _phase != null && _phase.MaxConcurrentMonsters > 0 ? _phase.MaxConcurrentMonsters : Settings.MaxConcurrentMonsters;

    public float SpawnInterval =>
        _phase != null && _phase.SpawnIntervalSeconds > 0f ? _phase.SpawnIntervalSeconds : Settings.SpawnIntervalSeconds;

    /// <summary>
    /// 쓰러진 소녀는 그 판이 끝날 때까지 일어나지 않는다. 체력과 전투불능이 페이즈를 넘어 이어지는 것이
    /// 이 컨텐츠의 전제이므로, 부활이 있으면 페이즈를 나눈 의미가 사라진다.
    /// </summary>
    protected override float ReviveDelaySeconds => -1f;

    /// <summary>이 페이즈의 남은 시간(초).</summary>
    public float RemainingTime => Mathf.Max(0f, _remainingTime);

    /// <summary>이 페이즈의 제한 시간(초). 시간 게이지가 남은 시간을 이 값으로 나눠 그린다.</summary>
    public float PhaseTimeLimit => _phase != null ? _phase.TimeLimitSeconds : 0f;

    /// <summary>이 페이즈의 남은 시간 비율(0~1). 다 쓰면 0 이 된다.</summary>
    public float TimeProgress => PhaseTimeLimit > 0f ? Mathf.Clamp01(RemainingTime / PhaseTimeLimit) : 0f;

    /// <summary>아직 쓰러지지 않은 소녀 수. 멸망전은 부활이 없어 이 값은 판이 갈수록 줄기만 한다.</summary>
    public int AliveCount {
        get {
            int alive = 0;
            for (int i = 0; i < Party.Count; i++)
                if (Party[i] != null && Party[i].IsAlive) alive++;
            return alive;
        }
    }

    /// <summary>이번 판에 나선 소녀 수(보유 전원).</summary>
    public int PartyCount => Party.Count;

    /// <summary>
    /// 이 페이즈에 남은 몬스터 비율(1 = 손도 안 댐, 0 = 전부 처치).
    /// 토벌 의뢰와 같이 가득 찬 게이지가 줄어드는 형태라 "얼마나 남았는가"를 돌려준다.
    /// </summary>
    public float PhaseRemainingRatio =>
        _phaseTarget > 0 ? Mathf.Clamp01((float)(_phaseTarget - _phaseKilled) / _phaseTarget) : 0f;

    public string PhaseProgressText => $"{_phaseKilled} / {_phaseTarget}";

    /// <summary>쓰러진 소녀가 하나라도 있는지(부활 증강을 내보낼지 판단).</summary>
    public bool AnyDowned {
        get {
            for (int i = 0; i < Party.Count; i++)
                if (Party[i] != null && !Party[i].IsAlive) return true;
            return false;
        }
    }

    /// <summary>체력이 깎인 소녀가 하나라도 있는지(회복 증강을 내보낼지 판단).</summary>
    public bool AnyInjured {
        get {
            for (int i = 0; i < Party.Count; i++) {
                var unit = Party[i];
                if (unit != null && unit.IsAlive && unit.HpRatio < 0.999f) return true;
            }
            return false;
        }
    }

    [Inject]
    public void Construct(BattleSettings settings, CharacterDatabase charDb, EquipmentDatabase equipDb,
                          PlayerStateStore store, ISoundService sound,
                          CharacterAppearanceService appearance, DungeonDatabase dungeonDb, DoomDatabase doomDb) {
        ConfigureStage(settings, charDb, equipDb, store, sound, appearance, dungeonDb);
        _db = doomDb;
    }

    /// <summary>
    /// 멸망전은 보유한 소녀가 전부 나선다. 덱 편성과 무관하며, 많이 모을수록 유리한 것이 이 컨텐츠의 전제다.
    /// 목록은 판을 시작할 때 한 번만 확정한다(도중에 바뀌지 않는다).
    /// </summary>
    protected override IReadOnlyList<int> PartyCharIds => _partyIds;

    /// <summary>한 판 시작. 파티를 만들고 1페이즈를 준비하되, 실제 진행은 증강을 고른 뒤에 시작한다.</summary>
    public void Begin() {
        InitializeSystems();

        _finished = false;
        _running = false;
        Score = 0;
        _phaseNumber = 0;
        Augments.Clear();

        SetPaused(false);
        ClearMonsters();
        ClearExtraPlayerUnits();

        CollectParty();
        SetFormation(Settings.Formation);
        RebuildParty();
        PlayBattleBgm();

        // 1페이즈도 증강을 고르고 시작한다(게임 시작 = 첫 증강).
        PreparePhase(1);
    }

    // 보유한 소녀 전원. 데이터가 없는 id(구버전 데이터 등)는 조용히 건너뛴다.
    private void CollectParty() {
        _partyIds.Clear();
        foreach (var kv in Store.Characters) {
            if (CharDb == null || CharDb.Get(kv.Key) == null) continue;
            _partyIds.Add(kv.Key);
        }
        _partyIds.Sort(CompareByGrade);
    }

    /// <summary>
    /// 강한 순(등급 -> 별레벨 -> 레벨 -> id)으로 세운다. 이 순서가 그대로 하단 캐릭터 패널의 슬롯 순서가 된다.
    ///
    /// 덱 컨텐츠는 편성 순서가 곧 의미라 무대가 순서를 건드리지 않지만, 멸망전은 편성이 없고 보유 전원이
    /// 나서므로 무대가 정해 줘야 한다. 정렬을 여기 두면 BattleCharacterPanel 을 비롯한 공용 화면은
    /// 손대지 않아도 되고, 다른 씬의 순서에도 영향이 없다.
    ///
    /// id 를 마지막 기준으로 두는 이유는 같은 등급/별/레벨일 때 순서가 매 판 흔들리지 않게 하기 위함이다.
    /// </summary>
    private int CompareByGrade(int leftId, int rightId) {
        var left = CharDb.Get(leftId);
        var right = CharDb.Get(rightId);

        int byGrade = ((int)right.Grade).CompareTo((int)left.Grade);
        if (byGrade != 0) return byGrade;

        var leftProgress = Store.GetProgress(leftId);
        var rightProgress = Store.GetProgress(rightId);
        if (leftProgress == null || rightProgress == null) return leftId.CompareTo(rightId);

        int byStar = rightProgress.StarLevel.CompareTo(leftProgress.StarLevel);
        if (byStar != 0) return byStar;

        int byLevel = rightProgress.Level.CompareTo(leftProgress.Level);
        return byLevel != 0 ? byLevel : leftId.CompareTo(rightId);
    }

    /// <summary>
    /// 다음 페이즈를 준비한다. 여기서는 멈춘 채로 증강 선택만 알리고, 실제 전투는 StartPhase 가 시작한다.
    /// 만들어 둔 데이터가 끝났으면 그 자리에서 판을 끝낸다(모든 페이즈 완료).
    /// </summary>
    private void PreparePhase(int phase) {
        var data = _db != null ? _db.Get(phase) : null;
        if (data == null) { Finish(DoomResult.AllCleared); return; }

        _phase = data;
        _phaseNumber = phase;
        _phaseTarget = data.TotalMonsters;
        _phaseKilled = 0;
        _remainingTime = data.TimeLimitSeconds;
        _spawnTimer = 0f;
        _running = false;

        if (data.BackgroundSprite != null) SetBackground(data.BackgroundSprite);

        // 증강을 고르는 동안은 완전히 멈춘다(무대가 직접 돌리는 투자 스쿼드 재생도 함께 멈춘다).
        SetPaused(true);
        PhaseStarting?.Invoke(phase);
    }

    /// <summary>
    /// 고른 증강을 받아들인다. 누적 상태에 쌓고, 즉시 효과(회복/부활)라면 그 자리에서 실행한다.
    ///
    /// 실행을 무대가 맡는 이유는 회복/부활이 전장의 유닛을 직접 건드리는 일이기 때문이다.
    /// 화면이나 씬 매니저가 유닛을 만지기 시작하면 전투 상태의 주인이 둘이 된다.
    /// </summary>
    /// <returns>부활 증강일 때 실제로 일어난 소녀 수(회복/스탯 증강은 0).</returns>
    public int AddAugment(DoomAugmentKind kind, float value) {
        Augments.Add(kind, value);

        switch (kind) {
            case DoomAugmentKind.HealAll:
                HealPartyFull();
                return 0;
            case DoomAugmentKind.Revive:
                return RevivePartyMembers(Mathf.RoundToInt(value));
            default:
                return 0;
        }
    }

    // 살아 있는 소녀를 전부 만피로 되돌린다. 쓰러진 소녀는 건드리지 않는다(그쪽은 부활 증강의 몫이다).
    private void HealPartyFull() {
        for (int i = 0; i < Party.Count; i++) {
            var unit = Party[i];
            if (unit == null || !unit.IsAlive) continue;
            unit.Heal(unit.MaxHp);
        }
    }

    // 쓰러진 순서대로 되살린다. 쓰러진 수가 고른 수보다 적으면 있는 만큼만 일어난다.
    private int RevivePartyMembers(int count) {
        int revived = 0;
        for (int i = 0; i < Party.Count && revived < count; i++) {
            var unit = Party[i];
            if (unit == null || unit.IsAlive) continue;
            unit.ReviveNow();
            revived++;
        }
        return revived;
    }

    /// <summary>
    /// 증강 선택이 끝난 뒤 이 페이즈를 실제로 시작한다.
    ///
    /// 파티를 다시 만들지 않는 것이 핵심이다. 체력과 전투불능은 그대로 이어지고 스킬 게이지만 초기화된다.
    /// 그래서 뒤로 갈수록 누적된 피해가 그대로 부담이 된다.
    /// </summary>
    public void StartPhase() {
        if (_finished || _phase == null) return;

        if (_phaseTarget <= 0) {
            Debug.LogError($"[DoomDirector] 등장 몬스터가 없는 페이즈입니다: {_phase.name}");
            Finish(DoomResult.AllCleared);
            return;
        }

        ApplyAugmentsToParty();
        ResetPartySkillGauges();

        _queue = BuildQueue(_phase);
        _running = true;
        SetPaused(false);
        FillSpawns();
    }

    // 페이즈 구성을 한 줄로 펼친 뒤 섞는다. 같은 종류가 몰려 나오지 않게 하려는 것이다.
    private static Queue<MonsterData> BuildQueue(DoomPhaseData phase) {
        var list = new List<MonsterData>(phase.TotalMonsters);
        foreach (var s in phase.Spawns) {
            if (s.Monster == null) continue;
            for (int i = 0; i < Mathf.Max(1, s.Count); i++) list.Add(s.Monster);
        }
        for (int i = list.Count - 1; i > 0; i--) {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return new Queue<MonsterData>(list);
    }

    /// <summary>
    /// 고른 증강을 파티 전원에게 다시 얹는다.
    ///
    /// 유닛이 들고 있는 값이 아니라 성장/장비에서 기준 스탯을 매번 새로 조립한 뒤 증강을 얹는다.
    /// 유닛의 현재 스탯 위에 또 얹으면 페이즈마다 증강이 중첩돼 값이 폭주한다.
    /// ApplyStats 가 체력 비율과 스킬 진행률을 지켜 주므로 중간에 불러도 상태가 튀지 않는다.
    /// </summary>
    private void ApplyAugmentsToParty() {
        for (int i = 0; i < Party.Count; i++) {
            if (Party[i] is not CharacterUnit unit || unit.Data == null) continue;

            var progress = Store.GetProgress(unit.Data.Id);
            if (progress == null) continue;

            unit.ApplyStats(Augments.Apply(BuildCharacterStats(unit.Data, progress)));
        }
    }

    // 페이즈가 넘어갈 때 초기화되는 것은 스킬 쿨타임 게이지뿐이다(체력/전투불능은 그대로 간다).
    private void ResetPartySkillGauges() {
        for (int i = 0; i < Party.Count; i++) {
            var unit = Party[i];
            if (unit == null || !unit.IsAlive) continue;
            unit.ResetSkillGauge();
        }
    }

    protected override void Update() {
        base.Update();   // 무대 공통 처리(투자 스쿼드 재생 등). IsPaused 면 그쪽도 건너뛴다.
        if (_finished || !_running || IsPaused || _phase == null) return;

        _remainingTime -= Time.deltaTime;
        if (_remainingTime <= 0f) { Finish(DoomResult.Defeat); return; }

        // 파티가 전멸하면 그 자리에서 끝난다.
        if (!AnyPartyAlive()) { Finish(DoomResult.Defeat); return; }

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f) return;
        _spawnTimer = SpawnInterval;
        FillSpawns();
    }

    private bool AnyPartyAlive() {
        for (int i = 0; i < Party.Count; i++)
            if (Party[i] != null && Party[i].IsAlive) return true;
        return false;
    }

    private void FillSpawns() {
        while (_queue != null && _queue.Count > 0 && ActiveMonsters.Count < MaxConcurrent)
            SpawnScaled(_queue.Dequeue());
    }

    // 이어 붙인 페이즈(만들어 둔 마지막 페이즈 이후)는 같은 구성에 공격력/방어력/체력 배율을 얹어 스폰한다.
    // 스폰 직후라 체력이 가득 찬 상태로 유지된다(ApplyStats 가 체력 비율을 지킨다). 무한 타워와 같은 방식이다.
    private void SpawnScaled(MonsterData data) {
        var unit = SpawnMonster(data);
        float m = _db != null ? _db.StatMultiplier(_phaseNumber) : 1f;
        if (unit == null || Mathf.Approximately(m, 1f)) return;

        var stats = data.BaseStats;
        stats.Attack *= m;
        stats.Defense *= m;
        stats.Health *= m;
        unit.ApplyStats(stats);
    }

    protected override void OnMonsterDied(BattleUnit unit) {
        base.OnMonsterDied(unit);
        if (_finished || !_running) return;

        Score++;
        _phaseKilled++;
        if (_phaseKilled < _phaseTarget) return;

        // 이 페이즈를 끝냈다. 다음 페이즈를 준비하며 증강 선택을 띄운다.
        _running = false;
        ClearMonsters();
        PreparePhase(_phaseNumber + 1);
    }

    private void Finish(DoomResult result) {
        if (_finished) return;
        _finished = true;
        _running = false;

        ClearMonsters();
        SetPaused(false);
        Sound.PlaySfx(result == DoomResult.AllCleared ? SfxKey.DungeonClear : SfxKey.UnitDeath);
        Finished?.Invoke(result);
    }
}
