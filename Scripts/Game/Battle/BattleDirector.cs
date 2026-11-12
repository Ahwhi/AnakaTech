using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;
using static Enums;
// 주의: UniRx 의 Unit 타입이 전투 Unit 과 이름 충돌하므로 using UniRx 를 생략하고 ReactiveProperty 를 정규화한다.

/// <summary>
/// MainScene 방치 전투의 진행 규칙(PLANNING 1,3). 던전 런을 무한 반복하며, 기력을 소모해 던전을 시작하고
/// 몬스터를 비율대로 스폰한 뒤 전멸하면 클리어 보상을 받아 다음 런으로 이어간다.
///
/// 클리어 뒤의 행선지는 자동 전투 모드가 정한다(AutoBattleMode):
///  - 사냥(기본): 같은 던전을 계속 반복한다.
///  - 도전: 클리어할 때마다 다음 던전으로 올라간다. 더 올라갈 곳이 없으면 사냥으로 돌아온다.
///
/// 던전에도 부활이 없다(의뢰와 동일). 파티가 전멸하면 그 런은 실패이며, 한 단계 아래 던전으로 내려가고
/// 모드는 사냥으로 돌아간다. 실패는 손수 고른 던전에서도 똑같이 적용된다.
///
/// 파티/몬스터 생성, 연출 풀, 카메라, 겹침 해소, 스킬 런타임 등 "전투 무대" 공통부는 전부 BattleStage 가 담당한다.
/// 이 클래스에는 방치형 고유의 규칙(기력/보상/휴식/던전-덱 변경 예약)만 남는다.
/// </summary>
public sealed class BattleDirector : BattleStage {
    [Header("Rest State (PLANNING 3-4)")]
    [SerializeField] private TMP_Text restingLabel;

    private IMainDataService _mainData;

    private CancellationTokenSource _loopCts;
    private bool _started;

    // HUD 바인딩용. 재화/기력은 PlayerStateStore, 아래 값들은 전투 진행 상태.
    public UniRx.ReactiveProperty<int> RemainingMonsters { get; } = new(0);
    public UniRx.ReactiveProperty<bool> IsResting { get; } = new(false);

    /// <summary>자동 전투 모드(사냥/도전). HUD 버튼이 구독해 표시/연출을 바꾸고, 눌러서 토글한다.</summary>
    public UniRx.ReactiveProperty<AutoBattleMode> Mode { get; } = new(AutoBattleMode.Hunt);

    private readonly UniRx.ReactiveProperty<bool> _boostMode = new(false);

    /// <summary>
    /// 기력 부스트 모드. 켜져 있으면 클리어 1회를 BoostRules.ClearMultiplier 회분으로 정산한다
    /// (기력을 그만큼 한 번에 쓰고 부스트권 1장을 소모하며 보상도 그만큼 받는다).
    /// 자원이 모자라면 루프가 스스로 꺼서(BoostRules.CanBoost) 일반 클리어로 계속 돈다.
    ///
    /// 읽기 전용으로 내보낸다. 값을 바꾸는 창구는 SetBoostMode 하나뿐이며, 그래야 유저의 선택이
    /// 한 곳에서 저장된다(HUD 버튼도 이 창구를 쓴다).
    /// </summary>
    public UniRx.IReadOnlyReactiveProperty<bool> BoostMode => _boostMode;

    /// <summary>
    /// 기력 부스트 모드를 바꾼다(유일한 창구). 선택은 계정 로컬 세이브에 남아, 씬을 다녀오거나
    /// 재접속해도 그대로 이어진다(자원이 떨어져 저절로 꺼지거나 직접 끄면 그 상태가 저장된다).
    /// </summary>
    public void SetBoostMode(bool on) {
        if (_boostMode.Value == on) return;
        _boostMode.Value = on;

        var account = UserDataManager.Account;
        if (account == null) return;
        account.boostModeOn = on;
        UserDataManager.RequestSave();
    }

    // 저장된 부스트 모드를 되살린다(메인 씬 진입 1회). 자원이 모자란 상태로 켜면 루프가 곧바로 도로 내리고
    // 안내만 뜨므로, 지금 쓸 수 있을 때만 켠다. 저장값 자체는 건드리지 않아 자원이 다시 갖춰지면 그대로 돌아온다.
    private void RestoreBoostMode() {
        var account = UserDataManager.Account;
        if (account == null || !account.boostModeOn) return;
        _boostMode.Value = BoostRules.CanBoost(Store.Stamina.Value, Store.StaminaBoostTicket.Value);
    }

    /// <summary>부스트 자원이 떨어져 부스트 모드가 자동으로 꺼졌을 때 발생. 인자는 어느 자원이 바닥났는지.
    /// HUD 가 이 값으로 안내 문구를 고른다(기력이 모자란 것과 부스트권이 모자란 것은 할 일이 다르다).</summary>
    public event Action<BoostStopReason> BoostExhausted;

    /// <summary>던전 클리어 보상 수령 시 발생(획득 팝업 표시용, PLANNING 7).</summary>
    public event Action<DungeonRewardDto> RewardReceived;

    /// <summary>파티 전멸로 런이 실패했을 때 발생. 인자는 (실패한 던전, 내려간 던전). HUD 가 안내를 띄운다.</summary>
    public event Action<int, int> StageFailed;

    // 덱 즉시 변경 시 현재 런을 끊기 위한 신호.
    private bool _restartRequested;

    // 던전 변경 예약(PLANNING 1-3). 값이 있으면 현재 던전 클리어 보상까지 받은 뒤 이 던전으로 자동 전환한다.
    private int? _reservedDungeonIndex;

    /// <summary>던전에도 부활은 없다(음수 = 부활 안 함). 쓰러진 캐릭터는 그대로 남아 전멸 판정 대상이 된다.</summary>
    protected override float ReviveDelaySeconds => -1f;

    [Inject]
    public void Construct(BattleSettings settings, CharacterDatabase charDb, DungeonDatabase dungeonDb,
                          EquipmentDatabase equipDb, PlayerStateStore store,
                          IMainDataService mainData, ISoundService sound,
                          CharacterAppearanceService appearance) {
        ConfigureStage(settings, charDb, equipDb, store, sound, appearance, dungeonDb);
        _mainData = mainData;
    }

    /// <summary>MainSceneManager 가 EnterMain 반영 후 호출. 시스템을 1회 설정하고 전투 루프를 시작한다.</summary>
    public void Begin() {
        if (!_started) {
            _started = true;
            InitializeSystems();
            RestoreBoostMode();   // 스토어가 채워진 뒤에 부른다(자원을 보고 되살릴지 정한다).
            if (restingLabel != null) restingLabel.gameObject.SetActive(false);
        }
        StartLoop();
    }

    // 루프를 (재)시작한다. 진행 중이던 런/몬스터를 정리하고 처음부터 다시 돈다.
    private void StartLoop() {
        _loopCts?.Cancel();
        _loopCts?.Dispose();
        ClearMonsters();
        _restartRequested = false;
        _reservedDungeonIndex = null;   // (재)시작 시 남은 예약은 무의미하므로 비운다.
        _loopCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        RunLoopAsync(_loopCts.Token).Forget();
    }

    /// <summary>던전 즉시 전환(보상 없음). 현재 런을 중단하고 새 던전으로 재시작하며, 휴식 상태에서도 재개한다.</summary>
    public void SwitchDungeon(int dungeonIndex) {
        Store.CurrentDungeonIndex.Value = dungeonIndex;
        StartLoop();
    }

    /// <summary>던전 변경 예약(PLANNING 1-3). 현재 런을 유지한 채, 클리어 보상을 받은 뒤 다음 런부터 이 던전으로 전환한다.</summary>
    public void ReserveDungeonChange(int dungeonIndex) {
        _reservedDungeonIndex = dungeonIndex;
    }

    // 자원 부족으로 부스트를 더 쓸 수 없을 때 모드를 내리고 한 번만 알린다.
    // (이미 꺼져 있으면 알림을 내지 않아, 휴식 중 재시도 등에서 안내가 반복되지 않는다.)
    private void DisableBoost(BoostStopReason reason) {
        if (!_boostMode.Value) return;
        SetBoostMode(false);
        BoostExhausted?.Invoke(reason);
    }

    // 지금 부스트를 막고 있는 자원. 둘 다 모자라면 부스트권을 먼저 알린다(기력은 저절로 차지만 부스트권은 아니라,
    // 유저가 실제로 조치해야 하는 쪽을 먼저 보여 준다).
    private BoostStopReason CurrentBoostShortage() =>
        Store.StaminaBoostTicket.Value < BoostRules.TicketPerBoostClear
            ? BoostStopReason.TicketDepleted
            : BoostStopReason.StaminaDepleted;

    /// <summary>
    /// 방치 전투 루프. 한 번의 통신 실패로 전투가 영영 멈추지 않도록, 예외와 실패 응답을 모두 "잠깐 쉬고 다시"로 다룬다.
    ///
    /// 예전에는 실패하면 그대로 루프를 끝냈다. 그러면 응답이 한 번 늦어지는 것만으로도 필드가 죽은 채 남아
    /// 재접속 말고는 되살릴 방법이 없었다(몬스터 0 + 배경 없음 상태). 취소(씬 종료/전환)만 진짜 종료로 본다.
    /// </summary>
    private async UniTaskVoid RunLoopAsync(CancellationToken ct) {
        while (!ct.IsCancellationRequested) {
            try {
                if (!await RunOnceAsync(ct)) return;
            } catch (OperationCanceledException) {
                if (ct.IsCancellationRequested) return;   // 씬 종료/전환. 여기서 끝내는 것이 맞다.
                // 응답 대기 시간 초과. 서버가 잠깐 늦은 것뿐이라 조금 쉬었다 같은 자리에서 다시 시도한다.
                if (!await RetryDelayAsync(ct)) return;
            } catch (Exception e) {
                Debug.LogException(e);
                if (!await RetryDelayAsync(ct)) return;
            }
        }
    }

    /// <summary>재시도 전 대기. 취소되면 false(루프를 끝내야 한다).</summary>
    private async UniTask<bool> RetryDelayAsync(CancellationToken ct) {
        try {
            await UniTask.Delay(TimeSpan.FromSeconds(RetrySeconds), cancellationToken: ct);
            return true;
        } catch (OperationCanceledException) {
            return false;
        }
    }

    /// <summary>실패한 뒤 다시 시도하기까지의 간격(초). 서버를 몰아치지 않으면서 유저가 오래 기다리지도 않는 값.</summary>
    private const float RetrySeconds = 2f;

    /// <summary>런 한 번. 계속 돌아도 되면 true, 루프를 끝내야 하면 false.</summary>
    private async UniTask<bool> RunOnceAsync(CancellationToken ct) {
        {
            int dungeonIndex = Store.CurrentDungeonIndex.Value;

            // 던전 시작 신고(서버 권위). 기력은 여기서 걸리지 않는다. 모자란 채로 돌면 클리어 보상이 깎일 뿐이다.
            var start = await _mainData.StartDungeonAsync(dungeonIndex, ct);
            if (!start.IsSuccess) {
                // 잠긴 던전(3)이면 진행도에 맞는 던전으로 스스로 되돌린다. 그러지 않으면 같은 거절을 영원히 반복한다.
                if (start.FailReason == 3)
                    Store.CurrentDungeonIndex.Value = DungeonUnlockRule.UnlockedThrough(Store.HighestClearedDungeonIndex.Value);

                // 그 외(일시적 서버 사정)는 잠깐 쉬었다 다시 시도한다. 화면은 휴식으로 두어 멈춘 이유를 알린다.
                EnterRestState();
                return await RetryDelayAsync(ct);
            }
            Store.SetStamina(start.RemainingStamina);

            var dungeon = DungeonDb.Get(dungeonIndex);
            if (dungeon == null) {
                Debug.LogError($"[BattleDirector] 던전 데이터 없음: index={dungeonIndex}");
                // 데이터가 없는 것은 다시 시도해도 같으므로 여기서만 루프를 끝낸다.
                EnterRestState();
                return false;
            }

            // BGM 은 이 던전이 속한 장의 곡이다. 런마다 부르는 이유는 장이 넘어가는 순간(예: 1-9 -> 2-1)
            // 곡도 함께 바뀌어야 하기 때문이다. 같은 곡이 이미 흐르고 있으면 재생 요청은 무시된다.
            PlayBattleBgm(dungeon);

            // 이번 런의 배치 결정: 던전이 오버라이드를 켰으면 그 값, 아니면 공용 기본값.
            // 파티 배치(RebuildParty)보다 먼저 정해져야 캐릭터가 이 배치로 스폰된다.
            SetFormation(dungeon.FormationOverride ?? Settings.Formation);

            RebuildParty();                      // 최신 덱/성장 반영(PLANNING 3-3)
            bool cleared = await RunDungeonAsync(dungeon, ct);   // 스폰 -> 전멸(클리어) 또는 파티 전멸(실패)
            if (ct.IsCancellationRequested) return false;

            if (_restartRequested) {            // 덱 즉시변경 등으로 런 취소된 경우 보상 없이 재시작
                _restartRequested = false;
                return true;
            }

            if (!cleared) {                     // 파티 전멸 = 스테이지 실패. 보상 없이 한 단계 내려간다.
                HandleStageFailed(dungeonIndex);
                return true;
            }

            // 클리어 보상(서버 확정, PLANNING 1-16). 기력은 시작이 아니라 여기서 소모된다.
            // 부스트는 자원이 갖춰졌을 때만 요청한다(모자라면 서버가 거부하므로 미리 걸러 일반 클리어로 정산한다).
            bool boost = BoostMode.Value && BoostRules.CanBoost(Store.Stamina.Value, Store.StaminaBoostTicket.Value);
            var reward = await _mainData.ClearDungeonAsync(dungeonIndex, boost, ct);
            if (ct.IsCancellationRequested) return false;

            if (!reward.IsSuccess) {
                // 기력 부족은 더 이상 실패가 아니다(보상이 깎인 채로 클리어된다).
                // 부스트 자원이 모자란 것이라면 부스트만 내리고 그대로 계속 돈다.
                if (reward.FailReason == 3 || reward.FailReason == 4) {
                    DisableBoost(reward.FailReason == 4 ? BoostStopReason.TicketDepleted : CurrentBoostShortage());
                    return true;
                }

                // 잠긴 던전(2)이면 진행도에 맞는 던전으로 되돌린다. 그러지 않으면 같은 거절을 영원히 반복한다.
                if (reward.FailReason == 2)
                    Store.CurrentDungeonIndex.Value = DungeonUnlockRule.UnlockedThrough(Store.HighestClearedDungeonIndex.Value);

                // 남은 실패는 일시적일 수 있으므로 잠깐 쉬었다 다시 돈다(여기서 끝내면 재접속 말고는 되살릴 방법이 없다).
                return await RetryDelayAsync(ct);
            }

            Store.ApplyReward(reward);
            Sound.PlaySfx(SfxKey.DungeonClear);
            RewardReceived?.Invoke(reward);   // 획득 팝업 표시(PLANNING 7)

            // 이번 클리어로 자원이 바닥나 다음 런에서 부스트가 불가능해졌으면 모드를 내리고 알린다.
            // ApplyReward 로 스토어가 이미 서버 확정값이라 어느 자원이 모자란지 여기서 바로 가려낼 수 있다.
            if (BoostMode.Value && !BoostRules.CanBoost(Store.Stamina.Value, Store.StaminaBoostTicket.Value))
                DisableBoost(CurrentBoostShortage());

            // 변경 예약이 있으면 이번 런 보상까지 받은 뒤 다음 런부터 예약 던전으로 전환한다(PLANNING 1-3).
            // 손수 고른 예약이 도전 모드의 자동 진행보다 우선한다(유저가 방금 지정한 의도를 덮지 않는다).
            if (_reservedDungeonIndex.HasValue) {
                Store.CurrentDungeonIndex.Value = _reservedDungeonIndex.Value;
                _reservedDungeonIndex = null;
            } else if (Mode.Value == AutoBattleMode.Challenge) {
                AdvanceToNextStage(dungeonIndex);
            }

            return true;
        }
    }

    // 파티 전멸: 한 단계 아래 던전으로 내려가고 사냥 모드로 돌아간다.
    // 첫 던전에서는 내려갈 곳이 없으므로 같은 던전을 다시 돈다(인덱스가 0/음수로 새지 않게 한다).
    private void HandleStageFailed(int failedIndex) {
        int next = Mathf.Max(DungeonUnlockRule.FirstDungeonIndex, failedIndex - 1);

        Mode.Value = AutoBattleMode.Hunt;   // 실패한 진도를 자동으로 다시 올라가지 않는다.
        Store.CurrentDungeonIndex.Value = next;

        ClearMonsters();
        StageFailed?.Invoke(failedIndex, next);
    }

    // 도전 모드 클리어: 다음 던전으로 올라간다. 다음 던전이 없거나(마지막 단계) 아직 잠겨 있으면
    // 그대로 두고 사냥 모드로 되돌린다(계속 도는 것보다 상태를 명확히 하는 편이 낫다).
    private void AdvanceToNextStage(int clearedIndex) {
        int next = clearedIndex + 1;
        bool exists = DungeonDb != null && DungeonDb.Get(next) != null;
        bool unlocked = DungeonUnlockRule.IsUnlocked(next, Store.HighestClearedDungeonIndex.Value);

        if (exists && unlocked) Store.CurrentDungeonIndex.Value = next;
        else Mode.Value = AutoBattleMode.Hunt;
    }

    /// <summary>한 런을 끝까지 돌린다. 몬스터를 전부 잡으면 true(클리어), 파티가 전멸하면 false(실패).
    /// 취소/재시작 요청으로 빠져나온 경우도 true 를 돌려주지만 호출측이 그 경로를 먼저 걸러낸다.</summary>
    private async UniTask<bool> RunDungeonAsync(DungeonData dungeon, CancellationToken ct) {
        SetResting(false);
        SetBackground(dungeon.BackgroundSprite);
        // 딜량 그래프는 런 단위 기록이다. 다음 런이 실제로 시작되는 이 시점에 비워, 직전 런의 최종
        // 순위가 클리어 보상 표시 동안에도 남아 있게 한다(클리어 즉시 지우면 결과를 볼 틈이 없다).
        DamageMeter.Reset();

        var queue = new Queue<MonsterData>(dungeon.BuildShuffledSpawnQueue());
        UpdateRemaining(queue.Count);
        if (queue.Count == 0) return true;

        while (!ct.IsCancellationRequested && !_restartRequested) {
            // 파티 전멸이면 즉시 실패로 끊는다. 부활이 없으므로 더 기다려도 상황이 나아지지 않는다.
            if (IsPartyWiped) return false;

            // 여유가 있으면 스폰(비율 랜덤 순서, PLANNING 3-2).
            while (queue.Count > 0 && ActiveMonsters.Count < Settings.MaxConcurrentMonsters)
                SpawnMonster(queue.Dequeue());

            UpdateRemaining(queue.Count);

            if (queue.Count == 0 && ActiveMonsters.Count == 0) break;  // 전멸 -> 클리어

            if (!await WaitSpawnIntervalAsync(ct)) return false;
        }
        return true;
    }

    // 다음 스폰까지 기다리되, 기다리는 동안 파티가 전멸하면 즉시 false 로 빠져나온다.
    // 통짜 Delay 로 기다리면 전멸하고도 스폰 주기만큼 화면이 멈춘 것처럼 보인다.
    private async UniTask<bool> WaitSpawnIntervalAsync(CancellationToken ct) {
        float remaining = Settings.SpawnIntervalSeconds;
        while (remaining > 0f) {
            if (IsPartyWiped) return false;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
            if (ct.IsCancellationRequested || _restartRequested) return true;   // 실패가 아니라 중단이므로 호출측이 거른다.
            remaining -= Time.deltaTime;
        }
        return true;
    }

    private void UpdateRemaining(int queued) => RemainingMonsters.Value = queued + ActiveMonsters.Count;

    // 몬스터가 죽으면 남은 수 표시를 즉시 줄인다(다음 스폰 주기를 기다리지 않고 반응하도록).
    protected override void OnMonsterDied(BattleUnit unit) {
        RemainingMonsters.Value = Mathf.Max(0, RemainingMonsters.Value - 1);
    }

    // ── 휴식 상태 ───────────────────────────────────
    private void EnterRestState() {
        SetResting(true);
        ClearMonsters();
        SetBackground(Settings.DefaultBackground);
        ArrangePartyInLine();
    }

    private void SetResting(bool resting) {
        IsResting.Value = resting;
        if (restingLabel != null) {
            restingLabel.gameObject.SetActive(resting);
            if (resting) restingLabel.text = Settings.RestingText;
        }
    }

    protected override void OnDestroy() {
        _loopCts?.Cancel();
        _loopCts?.Dispose();
        _loopCts = null;
        base.OnDestroy();
    }
}
