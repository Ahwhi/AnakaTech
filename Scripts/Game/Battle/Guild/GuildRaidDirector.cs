using System;
using UnityEngine;
using VContainer;
using static Enums;

/// <summary>길드 레이드 한 판의 결과.</summary>
public enum GuildRaidResult {
    InProgress,
    TimeOver,   // 제한 시간 종료(정상 종료다. 남은 체력은 다음 도전자가 이어 깎는다)
    Defeated,   // 최고 레벨 보스 처치(V1.13 부터 그 전 레벨은 잡아도 다음 레벨이 이어 나와 끝나지 않는다)
    Wiped       // 파티 전멸(부활이 없으므로 더 깎을 수 없다)
}

/// <summary>
/// 길드 레이드 전투 오케스트레이터(의뢰 씬의 CommissionDirector 와 같은 자리).
///
/// 의뢰 레이드와 다른 점은 보스 체력이 데이터가 아니라 "길드가 지금까지 깎아 둔 서버 상태"에서 온다는 것이다.
/// 그래서 스폰 직후 체력을 서버 값으로 덮고(OverrideHealth), 제한 시간이 끝나면 그 사이 깎은 만큼만 서버에 보고한다.
/// 보스를 잡으면 시간이 남아 있는 동안 다음 레벨 보스가 이어 나오며(V1.13), 결과는 여러 레벨에 걸친 "깎은 피해량" 합이다.
/// 처치 판정과 레벨업 기록은 서버가 같은 순서로 다시 계산한다.
/// </summary>
public sealed class GuildRaidDirector : BattleStage, IBossGaugeSource {
    [Header("Boss")]
    [Tooltip("길드 레이드 보스로 쓸 몬스터 데이터. 체력은 서버 값으로 덮어쓰므로 여기서는 외형/공격력만 의미가 있다.")]
    [SerializeField] private MonsterData bossData;

    [Header("Battle")]
    [Tooltip("전장 배경. 비우면 씬에 깔린 배경을 그대로 쓴다.")]
    [SerializeField] private Sprite backgroundSprite;

    [Header("Formation Override (선택)")]
    [Tooltip("끄면 BattleSettings 의 공용 배치를 그대로 쓴다. 켜면 레이드만 아래 값으로 배치한다(보스전답게 가깝게 등).")]
    [SerializeField] private bool overrideFormation;
    [SerializeField] private BattleFormation formation = BattleFormation.Default;

    private BattleUnit _boss;
    private float _startHp;      // 지금 보스가 나올 때의 체력(이 값과의 차이가 곧 지금 보스에게 깎은 피해량)
    private long _dealtBefore;   // 앞서 잡은 보스들에게 깎은 피해량 합(V1.13: 한 판에 여러 레벨을 이어 친다)
    private float _remaining;    // 남은 제한 시간
    private bool _finished;
    // Begin 은 서버 조회를 몇 번 거친 뒤에 불린다. 그 전까지 Update 가 돌면 아직 없는 보스를 죽은 것으로 보고
    // 첫 프레임에 "처치 + 피해량 0" 으로 끝나 버리므로, 시작 전에는 판정 자체를 하지 않는다.
    private bool _started;
    // 종료 시점 표시값. 전장을 정리한 뒤에도 화면이 마지막 모습을 유지하게 한다.
    private long _finalDealtDamage;
    private float _finalHpRatio;

    /// <summary>레이드가 끝났을 때(시간 종료 또는 처치). 씬 매니저가 결과 보고/팝업을 맡는다.</summary>
    public event Action<GuildRaidResult, long> Finished;

    /// <summary>길드 레이드는 부활이 없다. 그래서 파티가 전멸하면 시간이 남아도 더 깎을 수 없어 그 자리에서 끝난다.</summary>
    protected override float ReviveDelaySeconds => -1f;

    public int BossLevel { get; private set; } = 1;

    /// <summary>이번 도전에서 잡은 보스 수. 잡을 때마다 같은 자리에 다음 레벨 보스가 나온다.</summary>
    public int DefeatedCount { get; private set; }

    // HUD 가 매 프레임 읽으므로 레벨이 바뀔 때만 문자열을 만든다.
    private string _bossLevelText = "Lv.1";
    public string BossLevelText => _bossLevelText;
    /// <summary>
    /// 지금까지 이번 도전에서 깎은 피해량(HUD 표시 + 결과 보고값).
    /// 끝난 뒤에는 종료 시점 값을 그대로 돌려준다. 종료 처리에서 보스를 치우면 남은 체력이 0 으로 읽혀
    /// 피해량이 갑자기 "남아 있던 체력만큼" 뛰어 보이기 때문이다.
    /// </summary>
    public long DealtDamage {
        get {
            if (_finished) return _finalDealtDamage;
            float current = _boss != null && _boss.IsAlive ? _boss.CurrentHp : 0f;
            return _dealtBefore + (long)Mathf.Max(0f, _startHp - current);
        }
    }

    /// <summary>보스 남은 체력 비율(상단 게이지). 끝난 뒤에는 종료 시점 값을 유지한다(게이지가 0 으로 주저앉지 않게).</summary>
    public float BossHpRatio => _finished ? _finalHpRatio
        : (_boss != null && _boss.IsAlive ? Mathf.Clamp01(_boss.HpRatio) : 0f);

    /// <summary>남은 시간 비율(1 -> 0).</summary>
    public float TimeRatio => GuildRaidRules.BattleSeconds > 0
        ? Mathf.Clamp01(_remaining / GuildRaidRules.BattleSeconds) : 0f;

    public string TimeText => $"{Mathf.CeilToInt(Mathf.Max(0f, _remaining))}";

    public string BossName => bossData != null ? bossData.DisplayName : "";

    [Inject]
    public void Construct(BattleSettings settings, CharacterDatabase charDb, EquipmentDatabase equipDb,
                          PlayerStateStore store, ISoundService sound,
                          CharacterAppearanceService appearance, DungeonDatabase dungeonDb) {
        ConfigureStage(settings, charDb, equipDb, store, sound, appearance, dungeonDb);
    }

    /// <summary>
    /// 레이드 시작. 씬 매니저가 서버 입장 처리(참여 횟수 소모)를 마친 뒤 호출한다.
    /// bossHp/bossMaxHp 는 입장 시점의 서버 값이다.
    /// </summary>
    public void Begin(int bossLevel, long bossHp, long bossMaxHp) {
        if (bossData == null) {
            Debug.LogError("[GuildRaidDirector] 보스 몬스터 데이터가 없습니다.");
            Finish(GuildRaidResult.TimeOver);
            return;
        }

        InitializeSystems();

        SetBossLevel(bossLevel);
        DefeatedCount = 0;
        _dealtBefore = 0;
        _finished = false;
        SetPaused(false);
        _remaining = GuildRaidRules.BattleSeconds;

        ClearMonsters();
        ClearExtraPlayerUnits();
        DamageMeter.Reset();
        SetBackground(backgroundSprite);
        SetFormation(overrideFormation ? formation : Settings.Formation);
        RebuildParty();

        PlayBattleBgm();

        // 체력만 서버 상태로 덮는다(공격력 등 나머지는 몬스터 데이터 그대로).
        if (!SpawnBoss(bossMaxHp, bossHp)) { Finish(GuildRaidResult.TimeOver); return; }
        _started = true;
    }

    // 보스는 1기뿐이고 상단 주 게이지가 그 체력을 보여 주므로 머리 위 체력바는 붙이지 않는다.
    private bool SpawnBoss(long maxHp, long hp) {
        _boss = SpawnMonster(bossData, showHealthBar: false);
        if (_boss == null) return false;
        _boss.OverrideHealth(maxHp, hp);
        _startHp = _boss.CurrentHp;
        return true;
    }

    private void SetBossLevel(int level) {
        BossLevel = level;
        _bossLevelText = $"Lv.{level}";
    }

    /// <summary>
    /// 보스를 잡으면 시간이 남아 있는 동안 같은 자리에 다음 레벨 보스(최대 체력)를 내보낸다(V1.13).
    /// 한 번에 한 명만 도전하므로(서버 GuildRaidOccupancy) 여기서 이어 친 레벨이 곧 서버가 반영할 레벨이다.
    /// 서버도 같은 순서로 피해량을 레벨마다 나눠 깎는다(GuildRepository.ApplyRaidDamage).
    /// 최고 레벨 보스를 잡으면 더 나올 보스가 없으므로 끝난다.
    /// </summary>
    private void OnBossDefeated() {
        // 이미 체력이 0 인 최고 레벨 보스로 들어온 경우는 잡은 것이 아니다(서버도 더 깎지 않는다).
        if (_startHp <= 0f) { _boss = null; Finish(GuildRaidResult.Defeated); return; }

        _dealtBefore += (long)_startHp;
        DefeatedCount++;

        if (BossLevel >= GuildRaidRules.MaxBossLevel) {
            _boss = null;
            Finish(GuildRaidResult.Defeated);
            return;
        }

        SetBossLevel(BossLevel + 1);
        long maxHp = GuildRaidRules.BossMaxHp(BossLevel);
        if (!SpawnBoss(maxHp, maxHp)) { _boss = null; Finish(GuildRaidResult.TimeOver); }
    }

    protected override void Update() {
        base.Update();   // 무대 공통 처리(투자 스쿼드 재생 등)를 먼저 돌린다.
        if (!_started || _finished || IsPaused) return;

        // 보스를 잡았으면 다음 레벨 보스를 이어 내보낸다(최고 레벨이면 거기서 끝난다).
        if (_boss == null || !_boss.IsAlive) {
            OnBossDefeated();
            if (_finished) return;
        }

        // 부활이 없으므로 파티가 전멸하면 시간이 남아도 더 깎을 수 없다. 빈 전장을 지켜보게 두지 않는다.
        if (AllPartyDead()) { Finish(GuildRaidResult.Wiped); return; }

        _remaining -= Time.deltaTime;
        if (_remaining <= 0f) Finish(GuildRaidResult.TimeOver);
    }

    // 편성이 비어 있으면(스폰 전) 전멸로 보지 않는다. 시작 직후 한 프레임에 끝나 버리는 것을 막기 위함이다.
    private bool AllPartyDead() {
        var party = Party;
        if (party == null || party.Count == 0) return false;
        for (int i = 0; i < party.Count; i++)
            if (party[i] != null && party[i].IsAlive) return false;
        return true;
    }

    private void Finish(GuildRaidResult result) {
        if (_finished) return;

        // 전장을 정리하기 전에 마지막 값을 붙잡아 둔다. _finished 를 세운 뒤에 읽으면 캐시된 값(0)이 나오므로 순서가 중요하다.
        _finalDealtDamage = DealtDamage;
        _finalHpRatio = BossHpRatio;
        _finished = true;

        // 결과가 난 뒤에도 적이 계속 때리면 연출이 지저분해지므로 전장을 정리한다.
        ClearMonsters();
        Sound.PlaySfx(result == GuildRaidResult.Defeated ? SfxKey.DungeonClear : SfxKey.UnitDeath);
        Finished?.Invoke(result, _finalDealtDamage);
    }

}
