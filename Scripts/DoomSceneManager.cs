using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 멸망전 씬 진입/종료 오케스트레이션. 의뢰 씬과 마찬가지로 서버에서 계정 스냅샷을 받아 스토어를 채운 뒤
/// 판을 시작하고, 페이즈마다 증강 선택을 띄우며, 판이 끝나면 점수를 보고하고 결과를 보여 준다.
///
/// 씬이 바뀌면 DI 스코프도 새로 만들어지므로(스토어가 비어 있음) 진입 시 데이터를 다시 받는다.
/// 멸망전은 보유 소녀 전원이 나서기 때문에, 장비/각인/코스튬/본부/투자/수련이 빠지면 전원이 맨몸으로 싸우게 된다.
/// </summary>
public sealed class DoomSceneManager : MonoBehaviour {
    private IMainDataService _mainData;
    private IInventoryService _inventory;
    private IEngravingService _engraving;
    private ICostumeService _costume;
    private ITrainingService _training;
    private IHeadquartersService _headquarters;
    private IInvestmentService _investment;
    private IDoomService _doom;
    private PlayerStateStore _store;
    private IUIService _ui;
    private ISceneService _scene;
    private SceneCatalog _scenes;
    private DoomDirector _director;
    private DoomHudController _hud;
    private DoomHudContext _ctx;

    // 증강 추첨기. 한 판 동안 같은 것을 쓰며, 판마다 새로 만든다.
    private System.Random _random;

    // 역대 최고 점수. HUD 가 "이번 판이 기록을 넘었는가"를 판단하는 기준이라 판을 시작하기 전에 받아 둔다.
    private int _bestScore;

    // 오늘 참여 현황(다음 참여 비용). 입장 응답마다 서버 값으로 갱신하며, 결과 화면의 재도전 비용 표시에 쓴다.
    private DoomEntryDto _entry;

    private bool _leaving;
    private bool _handlingPhase;

    [Inject]
    public void Construct(IMainDataService mainData, IInventoryService inventory, IEngravingService engraving,
                          ICostumeService costume, ITrainingService training, IHeadquartersService headquarters,
                          IInvestmentService investment, IDoomService doom, PlayerStateStore store,
                          IUIService ui, ISceneService scene, SceneCatalog scenes,
                          DoomDirector director, DoomHudController hud, DoomHudContext ctx) {
        _mainData = mainData;
        _inventory = inventory;
        _engraving = engraving;
        _costume = costume;
        _training = training;
        _headquarters = headquarters;
        _investment = investment;
        _doom = doom;
        _store = store;
        _ui = ui;
        _scene = scene;
        _scenes = scenes;
        _director = director;
        _hud = hud;
        _ctx = ctx;
    }

    private void Start() {
        // 주입 누락은 여기서 끊는다(그대로 두면 전투/HUD 어딘가에서 NullReference 로 흩어져 원인 추적이 어렵다).
        if (_director == null || _hud == null || _ctx == null) {
            Debug.LogError("[DoomSceneManager] 의존성 주입이 되지 않았습니다. " +
                           "DoomLifetimeScope 의 Configure 에 DoomDirector / DoomHudController / " +
                           "DoomSceneManager 의 RegisterComponentInHierarchy 가 모두 있는지, " +
                           "세 컴포넌트가 씬 로드 시점에 활성 상태인지 확인하세요.");
            return;
        }

        _director.PhaseStarting += OnPhaseStarting;
        _director.Finished += OnFinished;
        _hud.ExitRequested += OnExitRequested;
        InitializeAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnDestroy() {
        if (_director != null) {
            _director.PhaseStarting -= OnPhaseStarting;
            _director.Finished -= OnFinished;
        }
        if (_hud != null) {
            _hud.ExitRequested -= OnExitRequested;
        }
    }

    private async UniTaskVoid InitializeAsync(CancellationToken ct) {
        // 씬이 올라온 뒤의 초기 데이터 로드도 유저에게는 같은 씬 전환이다. 팝업용 오버레이가 아니라
        // 씬 전환 화면을 이어서 붙잡아, 전환 도중 화면이 바뀌거나 두 로딩이 겹쳐 보이지 않게 한다.
        using (_ui.ShowSceneLoading()) {
            // 값이 계단식으로 채워지는 구간임을 알린다(변화에 반응하는 표시가 오작동하지 않도록).
            _store.BeginInitialLoad();
            try {
                _store.ApplyMainData(await _mainData.EnterMainAsync(ct));
                _store.ApplyInventory(await _inventory.RequestInventoryAsync(ct));

                // 아래 다섯은 전부 캐릭터 최종 스탯에 직접 들어간다. 전투를 시작하기 전에 반드시 받아 둔다
                // (빠뜨리면 이 씬에서만 내 소녀들이 그 보정 없이 싸운다).
                _store.ApplyEngravings(await _engraving.RequestInfoAsync(ct));
                _store.ApplyCostumes(await _costume.RequestInfoAsync(ct));
                _store.ApplyHeadquarters(await _headquarters.RequestInfoAsync(ct));
                _store.ApplyInvestment(await _investment.RequestAsync(ct));
                _store.ApplyTrainingInfo(await _training.RequestInfoAsync(ct));

                // 역대 최고 점수는 전투 값이 아니라 계정 값이다. HUD 가 기록 갱신을 알리려면 판이 시작되기
                // 전에 있어야 하므로 여기서 함께 받아 둔다.
                var info = await _doom.RequestInfoAsync(ct);
                _bestScore = info.BestScore;
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
                _ui.ShowToast("멸망전 정보를 불러오지 못했습니다.");
                ExitToMain();
                return;
            } finally {
                _store.EndInitialLoad();
            }
        }

        BeginRunAsync(ct).Forget();
    }

    // 한 판 입장. 서버가 무료분 또는 다이아를 쓴 뒤에만 판을 연다(다이아가 모자라거나 실패하면 메인으로 돌아간다).
    // 씬 진입과 재도전이 모두 이 경로를 지나므로 판마다 정확히 한 번씩 비용이 나간다.
    // 유료 참여의 동의는 이 요청을 보내기 전에 입구 팝업/결과 팝업이 받는다.
    private async UniTaskVoid BeginRunAsync(CancellationToken ct) {
        try {
            var res = await _doom.EnterAsync(ct);
            if (res.Entry != null) _entry = res.Entry;
            if (!res.IsSuccess) {
                _ui.ShowToast(DoomEntryRules.EnterFailMessage(res.FailReason));
                ExitToMain();
                return;
            }
            // 서버가 확정한 잔액(유료 참여면 비용이 빠진 값)을 그대로 쓴다.
            _store.Diamond.Value = res.NewDiamond;
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _ui.ShowToast("멸망전에 입장하지 못했습니다.");
            ExitToMain();
            return;
        }

        if (_leaving) return;
        BeginRun();
    }

    // 한 판 시작. 무대가 1페이즈를 "멈춘 채로" 준비하고 PhaseStarting 을 내므로, 증강 선택이 이어서 열린다.
    // 재도전으로 다시 들어올 때도 같은 경로를 타므로, 갱신된 기록이 HUD 에 반영된 채로 시작한다.
    private void BeginRun() {
        _random = new System.Random();
        // 앞 판의 증강 선택이 어떤 이유로든 정리되지 않았더라도 새 판의 1페이즈가 막히지 않게 한다
        // (이 잠금이 켜진 채로 남으면 증강 창이 안 뜨고 전투가 멈춘 채 굳는다).
        _handlingPhase = false;
        _hud.SetBestScore(_bestScore);
        _director.Begin();
    }

    // ── 페이즈 시작: 증강을 고를 때까지 전투는 멈춰 있다 ──
    private void OnPhaseStarting(int phase) {
        // 무대는 이미 SetPaused(true) 로 멈춰 있다. 고르고 나면 StartPhase 가 다시 풀어 준다.
        SelectAugmentAsync(phase, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid SelectAugmentAsync(int phase, CancellationToken ct) {
        if (_leaving || _handlingPhase) return;
        _handlingPhase = true;

        try {
            var reference = _ctx.Popups != null ? _ctx.Popups.DoomAugmentSelectPopup : null;
            if (reference == null || !reference.RuntimeKeyIsValid()) {
                // 증강을 못 고르면 게임이 멈춘 채로 남는다. 고르지 않은 것으로 보고 그냥 진행시킨다.
                Debug.LogError("[DoomSceneManager] UIPopupCatalog 에 멸망전 증강 선택 팝업이 할당되지 않았습니다.");
                _director.StartPhase();
                return;
            }

            var popup = await _ui.ShowPopupAsync<DoomAugmentSelectPopup>(reference, ct);
            var pick = await popup.ShowAsync(_ctx, phase, _director.Augments, _random, BlockedAugments());

            // 회복/부활 같은 즉시 효과는 무대가 실행한다. 여기서 유닛을 직접 만지면 전투 상태의 주인이 둘이 된다.
            _director.AddAugment(pick.Kind, pick.Value);
            _director.StartPhase();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            // 증강 없이라도 진행시킨다. 멈춘 채로 두면 나가기 외에는 아무것도 할 수 없다.
            _director.StartPhase();
        } finally {
            _handlingPhase = false;
        }
    }

    /// <summary>
    /// 지금 골라 봐야 아무 일도 일어나지 않는 증강. 선택지에서 빼 두지 않으면 꽝 카드가 되어,
    /// 세 장 중 한 장이 사실상 없는 셈이 된다(1페이즈는 전원 멀쩡하므로 특히 자주 걸린다).
    /// </summary>
    private List<DoomAugmentKind> BlockedAugments() {
        var blocked = new List<DoomAugmentKind>(2);
        if (!_director.AnyDowned) blocked.Add(DoomAugmentKind.Revive);
        if (!_director.AnyInjured) blocked.Add(DoomAugmentKind.HealAll);
        return blocked;
    }

    // ── 판 종료: 점수 보고 -> 결과 화면 -> 재도전/나가기 ──
    private void OnFinished(DoomResult result) {
        HandleFinishedAsync(result, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid HandleFinishedAsync(DoomResult result, CancellationToken ct) {
        if (_leaving) return;

        int score = _director.Score;
        int phase = _director.PhaseNumber;

        int bestScore = Mathf.Max(_bestScore, score);
        bool isBest = false;
        bool canClaim = false;
        int gainedExp = 0;

        DoomFinishDto res;
        try {
            res = await FinishRunAsync(ct);
        } catch (OperationCanceledException) {
            return;
        }
        if (res != null) {
            // 전멸로 끝난 판 = 끝까지 치른 판이다(나가기/재도전 경로는 여기를 지나지 않는다).
            // 종료 보고가 받아들여진 뒤에 보내야 서버가 완주로 인정한다.
            _doom.ReportFinished();
            bestScore = res.BestScore;
            isBest = res.IsBest;
            canClaim = res.CanClaim;
            gainedExp = res.GainedExp;
        }

        try {
            var reference = _ctx.Popups != null ? _ctx.Popups.DoomResultPopup : null;
            if (reference == null || !reference.RuntimeKeyIsValid()) {
                Debug.LogError("[DoomSceneManager] UIPopupCatalog 에 멸망전 결과 팝업이 할당되지 않았습니다.");
                ExitToMain();
                return;
            }

            var popup = await _ui.ShowPopupAsync<DoomResultPopup>(reference, ct);
            var choice = await popup.ShowAsync(_ctx, result, score, phase, bestScore, isBest, canClaim,
                                               gainedExp, _entry);

            if (choice == DoomResultPopup.Choice.Retry) {
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

    // 일시정지 메뉴의 나가기. 도중에 그만두어도 거기까지의 점수와 경험치는 받는다
    // (전멸할 때까지 버티는 것 말고는 판을 끝낼 방법이 없으므로, 나가기로 기록이 사라지면 부당하다).
    private void OnExitRequested() {
        if (_leaving) return;
        ReportAndExitAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid ReportAndExitAsync(CancellationToken ct) {
        try {
            await FinishRunAsync(ct);
        } catch (OperationCanceledException) {
            return;
        }
        ExitToMain();
    }

    /// <summary>
    /// 지금 판의 종료 보고(처치 수 + 끝난 페이즈). 받은 경험치는 스토어에 바로 반영해,
    /// 같은 씬에서 이어지는 재도전이 오른 레벨로 시작하게 한다. 실패하면 안내 후 null.
    /// </summary>
    private async UniTask<DoomFinishDto> FinishRunAsync(CancellationToken ct) {
        try {
            var res = await _doom.FinishAsync(_director.Score, Mathf.Max(1, _director.PhaseNumber), ct);
            if (!res.IsSuccess) {
                _ui.ShowToast("점수를 기록하지 못했습니다.");
                return null;
            }
            // 재도전이 같은 씬에서 이어지므로, 갱신된 기록을 들고 있어야 다음 판 HUD 가 맞는다.
            _bestScore = res.BestScore;
            _store.ApplyCharExp(res.CharExp);
            return res;
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception e) {
            Debug.LogException(e);
            _ui.ShowToast("점수를 기록하지 못했습니다.");
            return null;
        }
    }

    // 메인 씬으로 복귀. 전환 중 이 오브젝트가 파괴되므로 await 하지 않고 던진다(매뉴얼 6-3).
    private void ExitToMain() {
        if (_leaving) return;
        _leaving = true;

        // 결과/일시정지 팝업은 닫힘 연출이 남아 있을 수 있고, UIRoot 는 씬 전환에도 살아남는다.
        // 정리하지 않으면 메인 씬으로 돌아간 뒤에도 잠깐 떠 있으므로 여기서 확실히 닫는다.
        _ui.CloseAllPopups();

        if (_scenes == null || _scenes.MainScene == null || !_scenes.MainScene.RuntimeKeyIsValid()) {
            Debug.LogError("[DoomSceneManager] SceneCatalog 에 메인 씬이 할당되지 않았습니다.");
            return;
        }
        _scene.LoadAddressableAsync(_scenes.MainScene.RuntimeKey).Forget();
    }
}
