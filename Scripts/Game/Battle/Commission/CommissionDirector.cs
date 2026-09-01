using System;
using UnityEngine;
using VContainer;
using static Enums;

/// <summary>
/// 의뢰 씬 전투의 진행 규칙. 방치 던전(BattleDirector)과 달리 단판이며 부활이 없고, 성공/실패가 명확히 갈린다.
///
/// 종류별 규칙은 전략 객체(CommissionMode)가 갖는다. 이 디렉터는
///  - 파티 구성/배치/배경 등 전장 준비
///  - 모드에 매 프레임 진행을 위임하고 결과가 나오면 한 번만 알림
///  - 일시정지(계속/나가기)
/// 만 담당한다. 전투 자체는 BattleStage 공통부가 그대로 처리하므로 유닛/스킬 코드는 손대지 않는다.
/// </summary>
public sealed class CommissionDirector : BattleStage {
    private CommissionMode _mode;
    private CommissionData _data;
    private bool _finished;
    // 종료 시점의 게이지 상태. 전장을 정리한 뒤에도 화면이 마지막 모습을 유지하게 한다.
    private CommissionHudState _finalHudState;

    /// <summary>의뢰가 끝났을 때 발생(성공/실패). 씬 컨트롤러가 결과 팝업을 띄운다.</summary>
    public event Action<CommissionResult> Finished;

    /// <summary>현재 의뢰 데이터(HUD/결과 팝업이 참조).</summary>
    public CommissionData Current => _data;

    /// <summary>의뢰는 부활이 없다(음수 = 부활 안 함). 쓰러진 캐릭터는 그대로 남아 전멸 판정 대상이 된다.</summary>
    protected override float ReviveDelaySeconds => -1f;

    /// <summary>동시에 전장에 둘 수 있는 몬스터 수(의뢰 데이터가 지정하지 않으면 공용 상한).</summary>
    public int MaxConcurrent =>
        _data != null && _data.MaxConcurrentMonsters > 0 ? _data.MaxConcurrentMonsters : Settings.MaxConcurrentMonsters;

    /// <summary>스폰 주기(초). 의뢰 데이터가 지정하지 않으면 공용 주기.</summary>
    public float SpawnInterval =>
        _data != null && _data.SpawnIntervalSeconds > 0f ? _data.SpawnIntervalSeconds : Settings.SpawnIntervalSeconds;

    [Inject]
    public void Construct(BattleSettings settings, CharacterDatabase charDb, EquipmentDatabase equipDb,
                          PlayerStateStore store, ISoundService sound,
                          CharacterAppearanceService appearance, DungeonDatabase dungeonDb) {
        ConfigureStage(settings, charDb, equipDb, store, sound, appearance, dungeonDb);
    }

    /// <summary>의뢰 시작. CommissionSceneManager 가 서버 입장 처리(입장권 소모)를 마친 뒤 호출한다.</summary>
    public void Begin(CommissionData data) {
        if (data == null) {
            Debug.LogError("[CommissionDirector] 의뢰 데이터가 없습니다.");
            return;
        }

        InitializeSystems();

        _data = data;
        _finished = false;
        SetPaused(false);

        ClearMonsters();
        // 이전 단계의 의뢰인 등 파티 외 아군 유닛을 반드시 먼저 정리한다
        // (남겨 두면 다음 단계에서도 적의 표적으로 잡히고, 파괴된 참조가 아군 목록에 남는다).
        ClearExtraPlayerUnits();
        DamageMeter.Reset();
        SetBackground(data.BackgroundSprite);
        SetFormation(data.FormationOverride ?? Settings.Formation);
        RebuildParty();

        PlayBattleBgm();

        _mode = CreateMode(data.Type);
        _mode.Begin(this, data);

        // 데이터 누락 등으로 시작하자마자 결과가 정해진 경우도 한 번은 알려 준다(무한 대기 방지).
        if (_mode.Result != CommissionResult.InProgress) Finish(_mode.Result);
    }

    // 종류 -> 진행 규칙. 새 의뢰 종류를 추가할 때 손댈 유일한 분기다.
    private static CommissionMode CreateMode(CommissionType type) => type switch {
        CommissionType.Raid => new RaidCommissionMode(),
        CommissionType.Subjugation => new SubjugationCommissionMode(),
        CommissionType.Escort => new EscortCommissionMode(),
        _ => new RaidCommissionMode()
    };

    protected override void Update() {
        base.Update();   // 무대 공통 처리(투자 스쿼드 재생 등)를 먼저 돌린다.
        if (_finished || _mode == null || IsPaused) return;

        _mode.Tick(Time.deltaTime);
        if (_mode.Result != CommissionResult.InProgress) Finish(_mode.Result);
    }

    private void Finish(CommissionResult result) {
        if (_finished) return;

        // 전장을 정리하기 전에 마지막 게이지 상태를 붙잡아 둔다(아래 주석 참고: BuildHudState).
        _finalHudState = _mode != null ? _mode.BuildHudState() : default;
        _finished = true;

        // 결과가 난 뒤에도 남은 적이 계속 때리면 연출이 지저분해지므로 전장을 정리한다.
        ClearMonsters();
        Sound.PlaySfx(result == CommissionResult.Success ? SfxKey.DungeonClear : SfxKey.UnitDeath);
        Finished?.Invoke(result);
    }

    /// <summary>
    /// 현재 상단 게이지 상태(HUD 가 매 프레임 읽어간다). 모드가 없으면 빈 상태.
    /// 끝난 뒤에는 마지막 상태를 그대로 돌려준다. 종료 처리에서 전장을 정리하면 모드가 보던 대상(보스/의뢰인)이
    /// 사라져 게이지가 0 으로 주저앉는데, 시간 초과로 끝난 판까지 "다 깎은 것처럼" 보이기 때문이다.
    /// </summary>
    public CommissionHudState BuildHudState() {
        if (_finished) return _finalHudState;
        return _mode != null ? _mode.BuildHudState() : default;
    }

    // ── 모드가 쓰는 전장 조작(BattleStage 의 protected 기능을 규칙 계층에 공개) ──
    /// <summary>
    /// 몬스터 1기 스폰. 진행 규칙(모드)이 호출한다.
    /// showHealthBar 를 끄면 머리 위 체력바를 붙이지 않는다(상단 주 게이지가 그 체력을 이미 보여 주는 경우).
    /// </summary>
    public MonsterUnit SpawnCommissionMonster(MonsterData data, bool showHealthBar = true) =>
        SpawnMonster(data, showHealthBar);

    /// <summary>의뢰인(호위 대상)을 전장 중앙에 세우고 아군 팀에 등록한다.</summary>
    public EscortUnit SpawnEscort(EscortUnit prefab, float health, float defense, int maskSlot) {
        if (prefab == null) return null;

        var unit = Instantiate(prefab, CenterPos, Quaternion.identity, transform);
        unit.Init(health, defense, this, Settings, maskSlot);
        RegisterPlayerUnit(unit);
        return unit;
    }

    // ── BattleStage 훅 ──
    protected override BattleUnit ResolvePriorityTarget(BattleUnit unit) => _mode?.PriorityTarget;

    protected override void OnMonsterDied(BattleUnit unit) {
        if (_finished) return;
        _mode?.OnMonsterDied(unit);
    }

    protected override void OnPlayerUnitDied(BattleUnit unit) {
        if (_finished) return;
        _mode?.OnPlayerUnitDied(unit);
    }
}
