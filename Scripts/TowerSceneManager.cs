using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 무한 타워 씬 진입/종료 오케스트레이션(DoomSceneManager 와 같은 자리/구조).
///
/// 층/보너스/편성은 타워 팝업이 TowerSession 에 담아 넘긴다. 이 씬은 진입 시 계정 스냅샷을 받아 스토어를 채우고,
/// 입장 요청(C_TowerEnter)이 받아들여진 뒤에 전투를 시작한다. 실패는 횟수 제한이 없어 몇 번이든 다시 입장한다.
/// 버텨 내면 등반을 보고하고(보상 지급 + 다음 층), 결과를 보여 준 뒤 메인으로 돌아간다(팝업이 다시 열린다).
/// </summary>
public sealed class TowerSceneManager : MonoBehaviour {
    private IMainDataService _mainData;
    private IInventoryService _inventory;
    private IEngravingService _engraving;
    private ICostumeService _costume;
    private ITrainingService _training;
    private IHeadquartersService _headquarters;
    private IInvestmentService _investment;
    private ITowerService _tower;
    private PlayerStateStore _store;
    private IUIService _ui;
    private ISceneService _scene;
    private SceneCatalog _scenes;
    private TowerDirector _director;
    private TowerHudController _hud;
    private TowerHudContext _ctx;

    private bool _leaving;
    private bool _handlingResult;

    [Inject]
    public void Construct(IMainDataService mainData, IInventoryService inventory, IEngravingService engraving,
                          ICostumeService costume, ITrainingService training, IHeadquartersService headquarters,
                          IInvestmentService investment, ITowerService tower, PlayerStateStore store,
                          IUIService ui, ISceneService scene, SceneCatalog scenes,
                          TowerDirector director, TowerHudController hud, TowerHudContext ctx) {
        _mainData = mainData;
        _inventory = inventory;
        _engraving = engraving;
        _costume = costume;
        _training = training;
        _headquarters = headquarters;
        _investment = investment;
        _tower = tower;
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
            Debug.LogError("[TowerSceneManager] 의존성 주입이 되지 않았습니다. " +
                           "TowerLifetimeScope 의 Configure 에 TowerDirector / TowerHudController / " +
                           "TowerSceneManager 의 RegisterComponentInHierarchy 가 모두 있는지, " +
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
        if (!TowerSession.HasEntry) {
            Debug.LogWarning("[TowerSceneManager] 입장 정보가 없어 메인 씬으로 돌아갑니다.");
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
                _ui.ShowToast("무한 타워 정보를 불러오지 못했습니다.");
                ExitToMain();
                return;
            } finally {
                _store.EndInitialLoad();
            }
        }

        BeginRunAsync(ct).Forget();
    }

    // 한 판 입장. 서버가 받아들인 뒤에만 판을 연다(오늘 이미 올랐거나 층이 바뀌었으면 메인으로 돌아간다).
    // 첫 판과 재도전이 모두 이 경로를 지나므로 등반 보고의 시간 판정 기준이 판마다 새로 잡힌다.
    private async UniTaskVoid BeginRunAsync(CancellationToken ct) {
        try {
            var res = await _tower.EnterAsync(TowerSession.Floor, ct);
            if (!res.IsSuccess) {
                _ui.ShowToast(TowerRules.EnterFailMessage(res.FailReason));
                ExitToMain();
                return;
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _ui.ShowToast("무한 타워에 입장하지 못했습니다.");
            ExitToMain();
            return;
        }

        if (_leaving) return;
        _handlingResult = false;
        _director.Begin(TowerSession.Floor, TowerSession.Bonus, TowerSession.Members);
        _hud.RefreshForNewRun();
    }

    private void OnFinished(TowerResult result) {
        HandleFinishedAsync(result, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid HandleFinishedAsync(TowerResult result, CancellationToken ct) {
        if (_leaving || _handlingResult) return;
        _handlingResult = true;

        int floor = TowerSession.Floor;
        List<RewardItem> granted = null;

        // 성공이든 실패든 판이 끝난 것이다(일시정지 메뉴의 나가기/재도전은 이 경로를 지나지 않는다).
        // 이벤트 일일 미션(무한 타워 1회 도전)이 이 보고로만 인정된다.
        _tower.ReportFinished();

        if (result == TowerResult.Cleared) {
            try {
                var res = await _tower.ClearAsync(floor, ct);
                if (!res.IsSuccess) {
                    _ui.ShowToast(TowerRules.ClearFailMessage(res.FailReason));
                    ExitToMain();
                    return;
                }
                granted = res.Granted;
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
                _ui.ShowToast("등반 결과를 반영하지 못했습니다.");
                ExitToMain();
                return;
            }
        }

        try {
            var reference = _ctx.Popups != null ? _ctx.Popups.TowerResultPopup : null;
            if (reference == null || !reference.RuntimeKeyIsValid()) {
                Debug.LogError("[TowerSceneManager] UIPopupCatalog 에 무한 타워 결과 팝업이 할당되지 않았습니다.");
                ExitToMain();
                return;
            }

            var popup = await _ui.ShowPopupAsync<TowerResultPopup>(reference, ct);
            var choice = await popup.ShowAsync(_ctx, result, floor, granted);

            if (choice == TowerResultPopup.Choice.Retry && result != TowerResult.Cleared) {
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

    // 일시정지 메뉴의 나가기. 도중에 그만둔 판은 실패와 같아 보고할 것이 없다.
    private void OnExitRequested() {
        if (_leaving) return;
        ExitToMain();
    }

    // 메인 씬으로 복귀. 전환 중 이 오브젝트가 파괴되므로 await 하지 않고 던진다.
    private void ExitToMain() {
        if (_leaving) return;
        _leaving = true;

        TowerSession.ClearEntry();
        _ui.CloseAllPopups();

        if (_scenes == null || _scenes.MainScene == null || !_scenes.MainScene.RuntimeKeyIsValid()) {
            Debug.LogError("[TowerSceneManager] SceneCatalog 에 메인 씬이 할당되지 않았습니다.");
            return;
        }
        _scene.LoadAddressableAsync(_scenes.MainScene.RuntimeKey).Forget();
    }
}
