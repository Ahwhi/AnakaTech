using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 영웅전 씬 진입/종료 오케스트레이션(TowerSceneManager 와 같은 자리/구조).
///
/// 단계/소녀는 영웅전 팝업이 HeroesSession 에 담아 넘긴다. 이 씬은 진입 시 계정 스냅샷을 받아 스토어를 채우고,
/// 입장 요청(C_HeroesEnter, 도전 횟수 1회 사용)이 받아들여진 뒤에 전투를 시작한다.
/// 버텨 내면 클리어를 보고하고(보상 지급 + 다음 단계), 결과 팝업에서 이어서 칠지 나갈지 고른다.
/// 이어서 치기(다음 단계/재도전)도 입장을 다시 하므로 도전 횟수를 쓴다.
/// </summary>
public sealed class HeroesSceneManager : MonoBehaviour {
    private IMainDataService _mainData;
    private IInventoryService _inventory;
    private IEngravingService _engraving;
    private ICostumeService _costume;
    private ITrainingService _training;
    private IHeadquartersService _headquarters;
    private IInvestmentService _investment;
    private IHeroesService _heroes;
    private PlayerStateStore _store;
    private IUIService _ui;
    private ISceneService _scene;
    private SceneCatalog _scenes;
    private HeroesDirector _director;
    private HeroesHudController _hud;
    private HeroesHudContext _ctx;

    private bool _leaving;
    private bool _handlingResult;
    private int _remainEntries;

    [Inject]
    public void Construct(IMainDataService mainData, IInventoryService inventory, IEngravingService engraving,
                          ICostumeService costume, ITrainingService training, IHeadquartersService headquarters,
                          IInvestmentService investment, IHeroesService heroes, PlayerStateStore store,
                          IUIService ui, ISceneService scene, SceneCatalog scenes,
                          HeroesDirector director, HeroesHudController hud, HeroesHudContext ctx) {
        _mainData = mainData;
        _inventory = inventory;
        _engraving = engraving;
        _costume = costume;
        _training = training;
        _headquarters = headquarters;
        _investment = investment;
        _heroes = heroes;
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
            Debug.LogError("[HeroesSceneManager] 의존성 주입이 되지 않았습니다. " +
                           "HeroesLifetimeScope 의 Configure 에 HeroesDirector / HeroesHudController / " +
                           "HeroesSceneManager 의 RegisterComponentInHierarchy 가 모두 있는지, " +
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
        if (!HeroesSession.HasEntry) {
            Debug.LogWarning("[HeroesSceneManager] 입장 정보가 없어 메인 씬으로 돌아갑니다.");
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
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
                _ui.ShowToast("영웅전 정보를 불러오지 못했습니다.");
                ExitToMain();
                return;
            } finally {
                _store.EndInitialLoad();
            }
        }

        BeginRunAsync(ct).Forget();
    }

    // 한 판 입장. 서버가 받아들인 뒤에만 판을 연다(횟수를 다 썼거나 단계가 바뀌었으면 메인으로 돌아간다).
    // 첫 판과 이어서 치기가 모두 이 경로를 지나므로 클리어 보고의 시간 판정 기준이 판마다 새로 잡힌다.
    private async UniTaskVoid BeginRunAsync(CancellationToken ct) {
        try {
            var res = await _heroes.EnterAsync(HeroesSession.Stage, ct);
            if (!res.IsSuccess) {
                _ui.ShowToast(HeroesRules.EnterFailMessage(res.FailReason));
                ExitToMain();
                return;
            }
            _remainEntries = res.RemainEntries;
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _ui.ShowToast("영웅전에 입장하지 못했습니다.");
            ExitToMain();
            return;
        }

        if (_leaving) return;
        _handlingResult = false;
        _director.Begin(HeroesSession.Stage, HeroesSession.CharId);
        _hud.RefreshForNewRun();
    }

    private void OnFinished(HeroesResult result) {
        HandleFinishedAsync(result, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid HandleFinishedAsync(HeroesResult result, CancellationToken ct) {
        if (_leaving || _handlingResult) return;
        _handlingResult = true;

        int stage = HeroesSession.Stage;
        int nextStage = stage;
        List<RewardItem> granted = null;

        if (result == HeroesResult.Cleared) {
            try {
                var res = await _heroes.ClearAsync(stage, ct);
                if (!res.IsSuccess) {
                    _ui.ShowToast(HeroesRules.ClearFailMessage(res.FailReason));
                    ExitToMain();
                    return;
                }
                granted = res.Granted;
                nextStage = res.Next.Stage;
                _remainEntries = res.Next.RemainEntries;
                _store.Gold.Value = res.NewGold;
                _store.Diamond.Value = res.NewDiamond;
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
                _ui.ShowToast("클리어 결과를 반영하지 못했습니다.");
                ExitToMain();
                return;
            }
        }

        try {
            var reference = _ctx.Popups != null ? _ctx.Popups.HeroesResultPopup : null;
            if (reference == null || !reference.RuntimeKeyIsValid()) {
                Debug.LogError("[HeroesSceneManager] UIPopupCatalog 에 영웅전 결과 팝업이 할당되지 않았습니다.");
                ExitToMain();
                return;
            }

            var popup = await _ui.ShowPopupAsync<HeroesResultPopup>(reference, ct);
            var choice = await popup.ShowAsync(_ctx, result, stage, nextStage, _remainEntries, granted);

            if (choice == HeroesResultPopup.Choice.Continue) {
                HeroesSession.AdvanceTo(nextStage);
                BeginRunAsync(ct).Forget();
                return;
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
        }

        ExitToMain();
    }

    // 일시정지 메뉴의 나가기. 도중에 그만둔 판은 실패와 같아 보고할 것이 없다(도전 횟수는 입장 때 이미 썼다).
    private void OnExitRequested() {
        if (_leaving) return;
        ExitToMain();
    }

    // 메인 씬으로 복귀. 전환 중 이 오브젝트가 파괴되므로 await 하지 않고 던진다.
    private void ExitToMain() {
        if (_leaving) return;
        _leaving = true;

        HeroesSession.ClearEntry();
        _ui.CloseAllPopups();

        if (_scenes == null || _scenes.MainScene == null || !_scenes.MainScene.RuntimeKeyIsValid()) {
            Debug.LogError("[HeroesSceneManager] SceneCatalog 에 메인 씬이 할당되지 않았습니다.");
            return;
        }
        _scene.LoadAddressableAsync(_scenes.MainScene.RuntimeKey).Forget();
    }
}
