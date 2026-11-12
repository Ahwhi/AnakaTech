using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 전선 씬 진입/진행/종료 오케스트레이션(TowerSceneManager 와 같은 자리/구조, V1.12).
///
/// 메인 던전처럼 판을 계속 이어서 친다.
///   클리어   서버에 보고(기력 소모 + 스쿼드 경험치) -> 다음 단계로 자동 진행(최고 단계는 그대로 반복)
///   실패     전멸 또는 제한 시간 초과. 서버에 보고(기력 미소모) -> 이전 단계로 자동 진행
///   멈춤     다음 판을 치기 전 기력이나 오늘 한도가 모자라면 전투를 멈추고 결과 팝업을 띄운 뒤,
///            카운트다운이 끝나면 메인으로 돌아간다(전선 팝업이 다시 열린다).
/// 판이 끝날 때마다 결과 팝업을 띄우고, 카운트다운이 끝나면 다음 판으로 이어 간다(나가기로 중간에 끊을 수 있다).
/// 스쿼드와 단계는 매 판 서버 현황(FrontlineService.Info)에서 읽으므로 화면과 서버가 어긋나지 않는다.
/// </summary>
public sealed class FrontlineSceneManager : MonoBehaviour {
    [Tooltip("클리어 보고 최소 시간(FrontlineRules.MinClearSeconds)에 더 얹는 여유(초). 서버와 시계 오차를 흡수한다.")]
    [SerializeField, Min(0f)] private float clearReportMargin = 0.3f;

    private IMainDataService _mainData;
    private IInventoryService _inventory;
    private IEngravingService _engraving;
    private ICostumeService _costume;
    private ITrainingService _training;
    private IHeadquartersService _headquarters;
    private IInvestmentService _investment;
    private IFrontlineService _frontline;
    private PlayerStateStore _store;
    private IUIService _ui;
    private ISceneService _scene;
    private SceneCatalog _scenes;
    private FrontlineDirector _director;
    private FrontlineHudController _hud;
    private FrontlineHudContext _ctx;

    private bool _leaving;
    private bool _handlingResult;

    // 이번 판의 입장 응답을 받은 시각(실시간). 서버는 응답 전에 입장 시각을 적으므로 여기서 재면 서버 경과 시간보다 짧거나 같다.
    private float _enteredAtRealtime;

    // 이번 입장의 누적(결과 팝업 표시용).
    private long _totalExp;

    private int FrontlineId => FrontlineSession.FrontlineId;

    [Inject]
    public void Construct(IMainDataService mainData, IInventoryService inventory, IEngravingService engraving,
                          ICostumeService costume, ITrainingService training, IHeadquartersService headquarters,
                          IInvestmentService investment, IFrontlineService frontline, PlayerStateStore store,
                          IUIService ui, ISceneService scene, SceneCatalog scenes,
                          FrontlineDirector director, FrontlineHudController hud, FrontlineHudContext ctx) {
        _mainData = mainData;
        _inventory = inventory;
        _engraving = engraving;
        _costume = costume;
        _training = training;
        _headquarters = headquarters;
        _investment = investment;
        _frontline = frontline;
        _store = store;
        _ui = ui;
        _scene = scene;
        _scenes = scenes;
        _director = director;
        _hud = hud;
        _ctx = ctx;
    }

    private void Start() {
        if (_director == null || _hud == null || _ctx == null) {
            Debug.LogError("[FrontlineSceneManager] 의존성 주입이 되지 않았습니다. " +
                           "FrontlineLifetimeScope 의 Configure 에 FrontlineDirector / FrontlineHudController / " +
                           "FrontlineSceneManager 의 RegisterComponentInHierarchy 가 모두 있는지, " +
                           "세 컴포넌트가 씬 로드 시점에 활성 상태인지 확인하세요.");
            return;
        }

        _director.Finished += OnFinished;
        _hud.ExitRequested += OnExitRequested;
        InitializeAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnDestroy() {
        if (_director != null) _director.Finished -= OnFinished;
        if (_hud != null) {
            _hud.ExitRequested -= OnExitRequested;
        }
    }

    private async UniTaskVoid InitializeAsync(CancellationToken ct) {
        if (!FrontlineSession.HasEntry) {
            Debug.LogWarning("[FrontlineSceneManager] 입장 정보가 없어 메인 씬으로 돌아갑니다.");
            ExitToMain();
            return;
        }

        using (_ui.ShowSceneLoading()) {
            _store.BeginInitialLoad();
            try {
                // 아래는 전부 캐릭터 최종 스탯에 직접 들어간다. 빠뜨리면 이 씬에서만 소녀가 그 보정 없이 싸운다.
                _store.ApplyMainData(await _mainData.EnterMainAsync(ct));
                _store.ApplyInventory(await _inventory.RequestInventoryAsync(ct));
                _store.ApplyEngravings(await _engraving.RequestInfoAsync(ct));
                _store.ApplyCostumes(await _costume.RequestInfoAsync(ct));
                _store.ApplyHeadquarters(await _headquarters.RequestInfoAsync(ct));
                _store.ApplyInvestment(await _investment.RequestAsync(ct));
                _store.ApplyTrainingInfo(await _training.RequestInfoAsync(ct));
                await _frontline.RequestInfoAsync(ct);
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
                _ui.ShowToast("전선 정보를 불러오지 못했습니다.");
                ExitToMain();
                return;
            } finally {
                _store.EndInitialLoad();
            }
        }

        BeginRunAsync(ct).Forget();
    }

    // 한 판 입장. 기력/오늘 한도가 모자라면 판을 열지 않고 결과를 보여 준 뒤 나간다.
    private async UniTaskVoid BeginRunAsync(CancellationToken ct) {
        var info = _frontline.Info.Value;
        var squad = info?.Squad(FrontlineId);
        if (squad == null || squad.Members.Count == 0) {
            _ui.ShowToast(FrontlineRules.EnterFailMessage(4));
            ExitToMain();
            return;
        }

        int cost = info.StaminaPerClear;
        if (info.DailyStaminaRemaining < cost) { StopWithResultAsync(6, ct).Forget(); return; }
        if (_store.Stamina.Value < cost) { StopWithResultAsync(5, ct).Forget(); return; }

        try {
            var res = await _frontline.EnterAsync(FrontlineId, squad.Stage, ct);
            if (res.NewStamina >= 0) _store.SetStamina(res.NewStamina);
            if (!res.IsSuccess) {
                if (res.FailReason == 5 || res.FailReason == 6) { StopWithResultAsync(res.FailReason, ct).Forget(); return; }
                _ui.ShowToast(FrontlineRules.EnterFailMessage(res.FailReason));
                ExitToMain();
                return;
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _ui.ShowToast(FrontlineRules.EnterFailMessage(0));
            ExitToMain();
            return;
        }

        if (_leaving) return;
        _handlingResult = false;
        _enteredAtRealtime = Time.realtimeSinceStartup;
        _director.Begin(FrontlineId, squad.Stage, squad.Members);
        _hud.RefreshForNewRun();
    }

    private void OnFinished(FrontlineResult result) {
        HandleFinishedAsync(result, this.GetCancellationTokenOnDestroy()).Forget();
    }

    // 판의 결말을 보고하고 다음 판으로 잇는다(클리어 = 다음 단계, 실패 = 이전 단계, 단계는 서버가 정한다).
    private async UniTaskVoid HandleFinishedAsync(FrontlineResult result, CancellationToken ct) {
        if (_leaving || _handlingResult) return;
        _handlingResult = true;

        bool cleared = result == FrontlineResult.Cleared;
        int stage = _director.Stage;
        FrontlineFinishDto res;

        try {
            // 너무 빨리 깬 판은 서버가 전투 기록이 없는 보고로 보고 거부한다. 최소 시간이 지날 때까지 보고를 미룬다.
            if (cleared) {
                float wait = FrontlineRules.MinClearSeconds + clearReportMargin - (Time.realtimeSinceStartup - _enteredAtRealtime);
                if (wait > 0f) await UniTask.Delay(TimeSpan.FromSeconds(wait), DelayType.Realtime, cancellationToken: ct);
                if (_leaving) return;
            }

            res = await _frontline.FinishAsync(FrontlineId, stage, cleared, ct);
            if (!res.IsSuccess) {
                // 판을 치는 사이 다른 경로로 기력을 썼다면 클리어가 거부된다. 보상 없이 멈춘다.
                if (res.FailReason == 5 || res.FailReason == 6) { StopWithResultAsync(res.FailReason, ct).Forget(); return; }
                _ui.ShowToast(FrontlineRules.FinishFailMessage(res.FailReason));
                ExitToMain();
                return;
            }

            _store.SetStamina(res.NewStamina);
            if (cleared) {
                _store.ApplyCharExp(res.CharExp);
                _totalExp += res.GainedExp;
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _ui.ShowToast(FrontlineRules.FinishFailMessage(0));
            ExitToMain();
            return;
        }

        if (_leaving) return;

        // 다음 판을 칠 수 없으면 판 결과 대신 바로 종료 결과를 보여 준다(팝업이 연달아 두 번 뜨지 않게).
        int shortage = NextRunShortage();
        if (shortage != 0) { StopWithResultAsync(shortage, ct).Forget(); return; }

        var rewards = new List<RewardItem>();
        if (cleared && res.GainedExp > 0) rewards.Add(RewardItem.Exp(res.GainedExp));
        FrontlineResultPopup.Choice choice;
        try {
            choice = await ShowResultPopupAsync(
                popup => popup.ShowRunAsync(_ctx, FrontlineName, stage, cleared, rewards), ct);
        } catch (OperationCanceledException) {
            return;
        }

        if (_leaving) return;
        if (choice == FrontlineResultPopup.Choice.Exit) { ExitToMain(); return; }
        BeginRunAsync(ct).Forget();
    }

    // 다음 판 입장에 모자란 것(0 = 없음, 5 = 기력, 6 = 오늘 한도). 서버 입장 실패 코드와 같다.
    private int NextRunShortage() {
        var info = _frontline.Info.Value;
        if (info == null) return 0;
        int cost = info.StaminaPerClear;
        if (info.DailyStaminaRemaining < cost) return 6;
        if (_store.Stamina.Value < cost) return 5;
        return 0;
    }

    private string FrontlineName =>
        _ctx.FrontlineDb != null ? _ctx.FrontlineDb.NameOf(FrontlineId) : FrontlineRules.DefaultName(FrontlineId);

    // 결과 팝업을 띄워 선택을 받는다. 팝업을 띄울 수 없으면 나가기로 본다.
    private async UniTask<FrontlineResultPopup.Choice> ShowResultPopupAsync(
            Func<FrontlineResultPopup, UniTask<FrontlineResultPopup.Choice>> show, CancellationToken ct) {
        var reference = _ctx.Popups != null ? _ctx.Popups.FrontlineResultPopup : null;
        if (reference == null || !reference.RuntimeKeyIsValid()) {
            Debug.LogError("[FrontlineSceneManager] UIPopupCatalog 에 전선 결과 팝업이 할당되지 않았습니다.");
            return FrontlineResultPopup.Choice.Exit;
        }
        try {
            var popup = await _ui.ShowPopupAsync<FrontlineResultPopup>(reference, ct);
            return await show(popup);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception e) {
            Debug.LogException(e);
            return FrontlineResultPopup.Choice.Exit;
        }
    }

    // 더 칠 수 없을 때(기력 부족 / 오늘 한도 소진) 전투를 멈추고 이번 입장의 결과를 보여 준 뒤 나간다.
    private async UniTaskVoid StopWithResultAsync(int reason, CancellationToken ct) {
        if (_leaving) return;
        _handlingResult = true;
        _director.Abort();

        var rewards = new List<RewardItem>();
        if (_totalExp > 0) rewards.Add(RewardItem.Exp((int)Math.Min(int.MaxValue, _totalExp)));
        string message = FrontlineRules.EnterFailMessage(reason);

        try {
            await ShowResultPopupAsync(popup => popup.ShowEndAsync(_ctx, FrontlineName, message, rewards), ct);
        } catch (OperationCanceledException) {
            return;
        }

        ExitToMain();
    }

    // 일시정지 메뉴의 나가기. 도중에 그만둔 판은 보고하지 않는다(단계도 기력도 그대로).
    private void OnExitRequested() {
        if (_leaving) return;
        ExitToMain();
    }

    // 메인 씬으로 복귀. 전환 중 이 오브젝트가 파괴되므로 await 하지 않고 던진다.
    private void ExitToMain() {
        if (_leaving) return;
        _leaving = true;

        FrontlineSession.ClearEntry();
        _ui.CloseAllPopups();

        if (_scenes == null || _scenes.MainScene == null || !_scenes.MainScene.RuntimeKeyIsValid()) {
            Debug.LogError("[FrontlineSceneManager] SceneCatalog 에 메인 씬이 할당되지 않았습니다.");
            return;
        }
        _scene.LoadAddressableAsync(_scenes.MainScene.RuntimeKey).Forget();
    }
}
