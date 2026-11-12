using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static Enums;

/// <summary>영웅전 한 판의 결말.</summary>
public enum HeroesResult {
    InProgress,
    /// <summary>제한 시간 안에 처치 목표를 채웠다.</summary>
    Cleared,
    /// <summary>조작하던 소녀가 쓰러졌다.</summary>
    Defeat,
    /// <summary>처치 목표를 채우기 전에 제한 시간이 다 됐다.</summary>
    TimeOver,
}

/// <summary>
/// 영웅전 전투(V1.12). 이 게임에서 유일하게 유저가 소녀를 직접 움직이는 전투다.
///
///   소녀 1명이 나서며, 이동은 입력(IUnitMoveInput: 터치 조이스틱 / PC 방향키)이 정한다.
///   공격은 사거리 안의 가장 가까운 적에게 자동으로 한다. 발밑의 원(RangeIndicator)이 그 사거리다.
///   몬스터는 끝없이 나오며 전원 논타겟 직선 공격(LineShotAttack)을 한다. 예고된 길을 피하면 맞지 않는다.
///   전장에 회복 아이템이 무작위로 놓이고, 다친 상태로 집으면 체력이 가득 찬다.
///   BattleSeconds 안에 구간에 정한 몬스터를 전부(KillTarget 마리) 잡으면 승리. 소녀가 쓰러지거나(부활 없음) 시간이 다 되면 실패다.
///
/// 다른 전투 로직은 건드리지 않는다. 소녀의 직접 조작은 CharacterUnit.SetMoveInput 으로 이 전장만 켜고,
/// 몬스터의 공격 방식은 MonsterUnit.SetAttackOverride 로 스폰할 때만 바꾼다. 둘 다 부르지 않으면 기존 동작 그대로다.
/// </summary>
public sealed class HeroesDirector : BattleStage {
    [Header("Arena")]
    [Tooltip("움직일 수 있는 전장 크기의 절반(월드 units, 전장 중심 기준). 소녀/몬스터 스폰/회복 아이템이 이 안에 머문다.")]
    [SerializeField] private Vector2 arenaHalfSize = new(12f, 8f);
    [Tooltip("소녀 발밑의 사거리 원. 씬에 LineRenderer + RangeIndicator 오브젝트를 두고 연결한다. 비우면 표시하지 않는다.")]
    [SerializeField] private RangeIndicator rangeIndicator;

    /// <summary>한 판이 끝났을 때.</summary>
    public event Action<HeroesResult> Finished;

    private HeroesDatabase _db;
    private IUnitMoveInput _moveInput;
    private readonly List<int> _current = new(1);
    private readonly List<HealPickup> _pickups = new();

    private float _remaining;
    private float _spawnTimer;
    private float _healTimer;
    private bool _running;
    private bool _finished;

    // 피격 시간 감소를 걸어 둔 소녀. 판이 바뀌거나 끝날 때 반드시 풀어야 이전 판의 유닛이 이 판의 시간을 깎지 않는다.
    private BattleUnit _hitWatched;
    private static readonly Color HitPenaltyColor = new(1f, 0.35f, 0.35f);

    // 이번 판의 구성과, 항목별로 아직 나오지 않은 마릿수(구간 Spawns 와 같은 순서).
    private HeroesDatabase.StageBand _band;
    private int[] _unspawned = Array.Empty<int>();

    // 항목별 공격(라인샷 덮어쓰기를 얹은 사본 포함). 사본은 판이 바뀌어도 재사용하고 씬을 떠날 때 지운다.
    private readonly Dictionary<HeroesDatabase.SpawnEntry, BasicAttack> _attacks = new();
    private readonly List<LineShotAttack> _variants = new();

    public int Stage { get; private set; }

    /// <summary>이번 판에 잡은 몬스터 수.</summary>
    public int Kills { get; private set; }

    /// <summary>이번 판의 처치 목표(구간 Spawns 의 마릿수 합계).</summary>
    public int KillTarget { get; private set; }

    // 이번 판에 나온 보스. 쓰러지면 파괴되어 null 로 읽히므로 등장 여부는 따로 기억한다.
    private BattleUnit _boss;

    /// <summary>이번 판에 보스가 나왔는지. 나온 뒤로는 판이 끝날 때까지 상단 게이지가 보스 체력을 보여 준다.</summary>
    public bool BossAppeared { get; private set; }

    /// <summary>살아 있는 보스. 없거나 쓰러졌으면 null(화면 밖 위치 표시 등).</summary>
    public BattleUnit BossUnit => _boss != null && _boss.IsAlive ? _boss : null;

    /// <summary>보스 체력 비율(0~1). 보스가 쓰러졌거나 없으면 0.</summary>
    public float BossHpRatio => _boss != null && _boss.IsAlive ? _boss.HpRatio : 0f;

    /// <summary>처치 목표까지 남은 비율(1 에서 0 으로 줄어든다). 상단 게이지용.</summary>
    public float KillRemainProgress => KillTarget > 0 ? 1f - Mathf.Clamp01((float)Kills / KillTarget) : 1f;

    /// <summary>판의 남은 제한 시간(초).</summary>
    public float RemainingTime => Mathf.Max(0f, _remaining);
    // 시간 게이지의 기준(가득 찬 시간). 보스 추가 시간으로 남은 시간이 기본 제한 시간을 넘으면 그만큼 늘어난다.
    private float _timeScale = HeroesRules.BattleSeconds;

    public float TimeProgress => _timeScale > 0f ? Mathf.Clamp01(RemainingTime / _timeScale) : 0f;

    /// <summary>조작 중인 소녀(판이 시작되기 전이거나 비어 있으면 null).</summary>
    public CharacterUnit Hero => Party.Count > 0 ? Party[0] : null;

    /// <summary>쓰러진 소녀는 일어나지 않는다. 쓰러지면 판이 끝난다.</summary>
    protected override float ReviveDelaySeconds => -1f;

    /// <summary>파티는 고른 소녀 한 명이다.</summary>
    protected override IReadOnlyList<int> PartyCharIds => _current;

    [Inject]
    public void Construct(BattleSettings settings, CharacterDatabase charDb, EquipmentDatabase equipDb,
                          PlayerStateStore store, ISoundService sound,
                          CharacterAppearanceService appearance, DungeonDatabase dungeonDb, HeroesDatabase heroesDb) {
        ConfigureStage(settings, charDb, equipDb, store, sound, appearance, dungeonDb);
        _db = heroesDb;
    }

    /// <summary>
    /// 이동 입력을 연결한다(HUD 가 조이스틱으로 만들어 넘긴다). 판이 이미 시작됐으면 지금 소녀에게도 곧바로 건다.
    /// </summary>
    public void SetMoveInput(IUnitMoveInput input) {
        _moveInput = input;
        var hero = Hero;
        if (hero != null) hero.SetMoveInput(input);
    }

    /// <summary>한 판 시작. 다음 단계/재도전도 같은 경로로 다시 부른다.</summary>
    public void Begin(int stage, int charId) {
        InitializeSystems();

        Stage = stage;
        Kills = 0;
        _boss = null;
        BossAppeared = false;
        KillTarget = 0;
        _band = null;
        _finished = false;
        _running = false;
        _remaining = HeroesRules.BattleSeconds;
        _timeScale = HeroesRules.BattleSeconds;
        _spawnTimer = 0f;
        _healTimer = _db != null ? _db.HealSpawnIntervalSeconds : 0f;

        UnwatchHits();
        SetPaused(false);
        ClearMonsters();
        ClearPickups();
        DamageMeter.Reset();

        _current.Clear();
        if (charId != 0 && CharDb.Get(charId) != null && Store.GetProgress(charId) != null) _current.Add(charId);
        if (_current.Count == 0) {
            Debug.LogError("[HeroesDirector] 나설 소녀가 없습니다. 영웅전 팝업에서 소녀를 골랐는지 확인하세요.");
            Finish(HeroesResult.Defeat);
            return;
        }

        var band = _db != null ? _db.BandFor(stage) : null;
        if (band == null || band.TotalCount == 0) {
            Debug.LogError($"[HeroesDirector] {stage}단계 몬스터 구성(Spawns)이 비어 있습니다. HeroesDatabase 를 확인하세요.");
            Finish(HeroesResult.Defeat);
            return;
        }
        _band = band;
        _unspawned = new int[band.Spawns.Count];
        for (int i = 0; i < band.Spawns.Count; i++) {
            var e = band.Spawns[i];
            _unspawned[i] = e != null && e.Monster != null ? Mathf.Max(1, e.Count) : 0;
        }
        KillTarget = band.TotalCount;

        RebuildParty();
        var hero = Hero;
        if (hero != null) {
            // 대형 배치 대신 전장 한가운데에서 시작한다(움직이는 것은 유저다).
            Vector3 p = hero.transform.position;
            hero.transform.position = new Vector3(CenterPos.x, CenterPos.y, p.z);
            hero.SetMoveInput(_moveInput);
            if (rangeIndicator != null) rangeIndicator.Follow(hero.transform, RangeRadius(hero));
            WatchHits(hero);
        }

        PlayBattleBgm();
        _running = true;
    }

    // 사거리 판정은 몸통 표면 사이 거리라, 소녀 중심에서 그린 원은 소녀의 몸통 반경만큼 넓어야 실제 판정과 맞는다.
    private static float RangeRadius(CharacterUnit hero) => hero.AttackRange + hero.BodyRadius;

    protected override void Update() {
        base.Update();   // 무대 공통 처리(투자 스쿼드 재생 등). IsPaused 면 그쪽도 건너뛴다.
        if (!_running || _finished || IsPaused) return;

        // 쓰러졌으면 먼저 처리한다(같은 프레임에 목표를 채웠어도 쓰러진 판은 이긴 판이 아니다).
        if (IsPartyWiped) { Finish(HeroesResult.Defeat); return; }

        // 목표 달성은 처치 콜백이 아니라 여기서 판정한다. 콜백은 공격 판정 루프 도중에 불리므로
        // 거기서 전장을 정리(ClearMonsters)하면 순회 중인 목록이 바뀐다.
        if (Kills >= KillTarget) { Finish(HeroesResult.Cleared); return; }

        float dt = Time.deltaTime;
        _remaining -= dt;
        if (_remaining <= 0f) { Finish(HeroesResult.TimeOver); return; }

        TickSpawns(dt);
        TickPickups(dt);
    }

    protected override void LateUpdate() {
        base.LateUpdate();   // 겹침 해소가 소녀/몬스터를 밀어낸 뒤에 전장 안으로 되돌린다.

        var monsters = ActiveMonsters;
        for (int i = 0; i < monsters.Count; i++) {
            var m = monsters[i];
            if (m != null) m.transform.position = ClampToArena(m.transform.position);
        }

        var hero = Hero;
        if (hero == null || !hero.IsAlive) return;

        hero.transform.position = ClampToArena(hero.transform.position);
        // 변신/버프로 사거리가 바뀌면 원도 따라 바뀐다(같은 값이면 다시 그리지 않는다).
        if (rangeIndicator != null) rangeIndicator.SetRadius(RangeRadius(hero));
    }

    // ── 몬스터 ──────────────────────────────────────
    private void TickSpawns(float dt) {
        _spawnTimer -= dt;
        if (_spawnTimer > 0f) return;

        _spawnTimer = _band.SpawnIntervalSeconds;
        if (ActiveMonsters.Count >= _band.MaxConcurrentMonsters) return;

        int index = PickEntry();
        if (index < 0) return;
        _unspawned[index]--;
        SpawnOne(_band.Spawns[index]);
    }

    // 지금 나올 수 있는 항목 중 하나를 남은 마릿수에 비례해 고른다(종류가 고르게 섞인다). 없으면 -1.
    // 등장 시점(Appear After Kills)이 아직이어도, 다른 항목이 다 나왔고 전장이 비었으면 나오게 한다(판이 막히지 않게).
    private int PickEntry() {
        bool othersDone = ActiveMonsters.Count == 0;
        if (othersDone) {
            for (int i = 0; i < _unspawned.Length; i++)
                if (_unspawned[i] > 0 && Kills >= _band.Spawns[i].AppearAfterKills) { othersDone = false; break; }
        }

        int total = 0;
        for (int i = 0; i < _unspawned.Length; i++)
            if (CanAppear(i, othersDone)) total += _unspawned[i];
        if (total <= 0) return -1;

        int roll = UnityEngine.Random.Range(0, total);
        for (int i = 0; i < _unspawned.Length; i++) {
            if (!CanAppear(i, othersDone)) continue;
            if (roll < _unspawned[i]) return i;
            roll -= _unspawned[i];
        }
        return -1;
    }

    private bool CanAppear(int i, bool force) =>
        _unspawned[i] > 0 && (force || Kills >= _band.Spawns[i].AppearAfterKills);

    // 소녀 주변 화면 밖 거리에서 한 마리를 세운다. 공격 방식은 영웅전 규칙(논타겟)으로 덮고, 단계/항목 배율과 사거리를 얹는다.
    private void SpawnOne(HeroesDatabase.SpawnEntry entry) {
        var data = entry.Monster;
        var unit = SpawnMonster(data);
        if (unit == null) return;
        if (entry.IsBoss) {
            _boss = unit;
            BossAppeared = true;
            // 보스를 잡을 시간을 더 준다. 게이지가 넘치지 않도록 기준도 함께 늘린다.
            _remaining += _db.BossTimeBonusSeconds;
            _timeScale = Mathf.Max(_timeScale, _remaining);
        }

        var hero = Hero;
        Vector2 center = hero != null ? hero.Position : (Vector2)CenterPos;
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector2 at = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _db.SpawnDistance;
        unit.transform.position = ClampToArena(new Vector3(at.x, at.y, unit.transform.position.z));

        var attack = AttackFor(entry);
        if (attack != null) unit.SetAttackOverride(attack, suppressSkill: true);
        // 소녀를 쫓아 몰려다니지 않고 전장 안을 각자 떠돈다(탄을 피하는 판).
        unit.SetMoveInput(new WanderMoveInput(unit.transform, CenterPos, arenaHalfSize, _db.MonsterWander));

        // 단계별 스탯 규칙은 데이터(HeroesDatabase)가 안다. 스폰 직후라 체력 비율이 가득 찬 상태로 유지된다.
        unit.ApplyStats(_db.StatsFor(entry, _band, Stage, Settings.BaseAttacksPerSecond));
    }

    // 항목이 쓸 공격. 항목 공격 > DB 기본 공격 순이며, 라인샷 덮어쓰기가 있으면 그 값을 얹은 사본을 한 번 만들어 재사용한다.
    private BasicAttack AttackFor(HeroesDatabase.SpawnEntry entry) {
        if (_attacks.TryGetValue(entry, out var cached)) return cached;

        var attack = entry.Attack != null ? entry.Attack : _db.MonsterAttack;
        if (attack is LineShotAttack line && entry.LineShot != null && !entry.LineShot.IsEmpty) {
            var variant = line.CreateVariant(entry.LineShot);
            _variants.Add(variant);
            attack = variant;
        }
        _attacks[entry] = attack;
        return attack;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        UnwatchHits();
        foreach (var v in _variants) if (v != null) Destroy(v);
        _variants.Clear();
        _attacks.Clear();
    }

    // 판 도중에 쓰러진 몬스터만 센다(판 종료 정리로 치운 몬스터는 이 경로로 오지 않는다).
    protected override void OnMonsterDied(BattleUnit unit) {
        if (_running && !_finished) Kills++;
    }

    // ── 피격 시간 감소 ──────────────────────────────
    private void WatchHits(BattleUnit hero) {
        UnwatchHits();
        _hitWatched = hero;
        _hitWatched.Hit += OnHeroHit;
    }

    private void UnwatchHits() {
        if (_hitWatched != null) _hitWatched.Hit -= OnHeroHit;
        _hitWatched = null;
    }

    // 직접 맞을 때마다 남은 시간을 줄인다. 시간이 0 이하가 되면 다음 Update 가 시간 초과로 끝낸다
    // (쓰러짐과 목표 달성 판정이 시간보다 먼저라, 같은 프레임에 겹쳐도 결말이 뒤바뀌지 않는다).
    // 쓰러지는 타격은 패배로 끝나므로 깎지 않는다. 일시정지/판 종료 뒤에 도착한 탄도 세지 않는다.
    private void OnHeroHit(BattleUnit unit) {
        if (!_running || _finished || IsPaused) return;
        if (unit == null || !unit.IsAlive) return;

        _remaining -= HeroesRules.HitTimePenaltySeconds;
        ((IBattleContext)this).ShowSkillBubble(unit.HitPoint, $"-{HeroesRules.HitTimePenaltySeconds:0}초", HitPenaltyColor);
    }

    // ── 회복 아이템 ──────────────────────────────────
    private void TickPickups(float dt) {
        var hero = Hero;
        if (hero == null) return;

        // 다친 상태에서만 집는다. 가득 찬 채로 지나가도 아이템이 사라지지 않게 한다.
        for (int i = _pickups.Count - 1; i >= 0; i--) {
            var p = _pickups[i];
            if (p == null) { _pickups.RemoveAt(i); continue; }
            if (hero.HpRatio >= 1f || !p.IsTouching(hero)) continue;

            float healed = hero.Heal(hero.MaxHp);
            ((IBattleContext)this).ShowDamage(hero.HitPoint, healed, DamagePopupStyle.Heal);
            Sound.PlaySfx(SfxKey.HealPickup);
            if (_db.HealVfxPrefab != null) ((IBattleContext)this).PlayVfx(_db.HealVfxPrefab, hero.HitPoint);
            Destroy(p.gameObject);
            _pickups.RemoveAt(i);
        }

        if (_db.HealPickupPrefab == null || _pickups.Count >= _db.MaxHealPickups) return;
        _healTimer -= dt;
        if (_healTimer > 0f) return;
        _healTimer = _db.HealSpawnIntervalSeconds;
        SpawnPickup(hero.Position);
    }

    // 전장 안 무작위 자리에 하나 놓는다. 소녀 발밑에 바로 생기지 않도록 최소 거리를 지키는 자리를 몇 번 찾아본다.
    private void SpawnPickup(Vector2 heroPos) {
        Vector2 at = CenterPos;
        for (int attempt = 0; attempt < 10; attempt++) {
            at = (Vector2)CenterPos + new Vector2(UnityEngine.Random.Range(-arenaHalfSize.x, arenaHalfSize.x),
                                                  UnityEngine.Random.Range(-arenaHalfSize.y, arenaHalfSize.y));
            if (Vector2.Distance(at, heroPos) >= _db.HealMinDistance) break;
        }
        var pickup = Instantiate(_db.HealPickupPrefab, new Vector3(at.x, at.y, 0f), Quaternion.identity, transform);
        _pickups.Add(pickup);
    }

    private void ClearPickups() {
        foreach (var p in _pickups) if (p != null) Destroy(p.gameObject);
        _pickups.Clear();
    }

    // ── 공용 ────────────────────────────────────────
    private Vector3 ClampToArena(Vector3 p) {
        Vector3 c = CenterPos;
        return new Vector3(Mathf.Clamp(p.x, c.x - arenaHalfSize.x, c.x + arenaHalfSize.x),
                           Mathf.Clamp(p.y, c.y - arenaHalfSize.y, c.y + arenaHalfSize.y), p.z);
    }

    private void Finish(HeroesResult result) {
        if (_finished) return;
        _finished = true;
        _running = false;

        UnwatchHits();
        // 몬스터를 치우면 날아가던 논타겟 발사체와 경로 예고도 시전자를 잃고 스스로 사라진다.
        ClearMonsters();
        ClearPickups();
        if (rangeIndicator != null) rangeIndicator.Hide();
        SetPaused(false);
        Sound.PlaySfx(result == HeroesResult.Cleared ? SfxKey.DungeonClear : SfxKey.UnitDeath);
        Finished?.Invoke(result);
    }

#if UNITY_EDITOR
    // 전장 경계를 씬 뷰에서 맞추기 위한 표시(에디터 전용).
    private void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(CenterPos, new Vector3(arenaHalfSize.x * 2f, arenaHalfSize.y * 2f, 0f));
    }
#endif
}
