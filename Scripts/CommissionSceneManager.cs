using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 의뢰 씬 진입/종료 오케스트레이션. 메인 씬과 마찬가지로 서버에서 계정 스냅샷을 받아 스토어를 채운 뒤
/// 요청된 의뢰(CommissionSession)를 시작하고, 결과가 나오면 보상 수령 -> 결과 팝업 -> 다음 단계/나가기를 처리한다.
///
/// 씬이 바뀌면 DI 스코프도 새로 만들어지므로(스토어가 비어 있음) 진입 시 데이터를 다시 받는다.
/// 의뢰도전은 무료라 입장권을 쓰지 않으며, 클리어 보상도 그 단계를 처음 깼을 때만 나온다(서버 판정).
/// </summary>
public sealed class CommissionSceneManager : MonoBehaviour {
    private IMainDataService _mainData;
    private IInventoryService _inventory;
    private IEngravingService _engraving;
    private ICostumeService _costume;
    private ITrainingService _training;
    private IHeadquartersService _headquarters;
    private IInvestmentService _investment;
    private ICommissionService _commission;
    private PlayerStateStore _store;
    private IUIService _ui;
    private ISceneService _scene;
    private SceneCatalog _scenes;
    private CommissionDatabase _commissionDb;
    private CommissionDirector _director;
    private CommissionHudController _hud;
    private CommissionHudContext _ctx;

    private CommissionType _type;
    private int _stage;
    private bool _leaving;

    [Inject]
    public void Construct(IMainDataService mainData, IInventoryService inventory, IEngravingService engraving, ICostumeService costume,
                          ITrainingService training,
                          IHeadquartersService headquarters, IInvestmentService investment, ICommissionService commission, PlayerStateStore store,
                          IUIService ui, ISceneService scene, SceneCatalog scenes, CommissionDatabase commissionDb,
                          CommissionDirector director, CommissionHudController hud, CommissionHudContext ctx) {
        _mainData = mainData;
        _inventory = inventory;
        _engraving = engraving;
        _costume = costume;
        _training = training;
        _headquarters = headquarters;
        _investment = investment;
        _commission = commission;
        _store = store;
        _ui = ui;
        _scene = scene;
        _scenes = scenes;
        _commissionDb = commissionDb;
        _director = director;
        _hud = hud;
        _ctx = ctx;
    }

    private void Start() {
        // 주입 누락은 여기서 끊는다(그대로 두면 전투/HUD 어딘가에서 NullReference 로 흩어져 원인 추적이 어렵다).
        if (_director == null || _hud == null || _ctx == null) {
            Debug.LogError("[CommissionSceneManager] 의존성 주입이 되지 않았습니다. " +
                           "CommissionLifetimeScope 의 Configure 에 CommissionDirector / CommissionHudController / " +
                           "CommissionSceneManager 의 RegisterComponentInHierarchy 가 모두 있는지, " +
                           "세 컴포넌트가 씬 로드 시점에 활성 상태인지 확인하세요.");
            return;
        }

        _director.Finished += OnFinished;
        _hud.ExitRequested += ExitToMain;
        _hud.RetryRequested += OnRetryRequested;
        InitializeAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnDestroy() {
        if (_director != null) _director.Finished -= OnFinished;
        if (_hud != null) {
            _hud.ExitRequested -= ExitToMain;
            _hud.RetryRequested -= OnRetryRequested;
        }
    }

    // 일시정지 메뉴의 재도전: 진행 중이던 판을 버리고 같은 단계를 처음부터 다시 시작한다.
    // 결과 팝업의 재도전과 같은 경로(StartStageAsync)를 써서 해금 판정이 한 곳으로 모인다.
    private void OnRetryRequested() {
        if (_leaving) return;
        StartStageAsync(_stage, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid InitializeAsync(CancellationToken ct) {
        if (!CommissionSession.Consume(out _type, out _stage)) {
            // 의뢰 요청 없이 씬이 열렸다(직접 실행 등). 진행할 대상이 없으므로 메인으로 되돌린다.
            Debug.LogWarning("[CommissionSceneManager] 시작할 의뢰 요청이 없어 메인 씬으로 돌아갑니다.");
            ExitToMain();
            return;
        }

        // 씬이 올라온 뒤의 초기 데이터 로드도 유저에게는 같은 씬 전환이다. 팝업용 오버레이가 아니라
        // 씬 전환 화면을 이어서 붙잡아, 전환 도중 화면이 바뀌거나 두 로딩이 겹쳐 보이지 않게 한다.
        using (_ui.ShowSceneLoading()) {
            // 메인 씬과 동일하게, 값이 계단식으로 채워지는 구간임을 알린다(변화에 반응하는 표시가 오작동하지 않도록).
            _store.BeginInitialLoad();
            try {
                var data = await _mainData.EnterMainAsync(ct);
                _store.ApplyMainData(data);

                var inventory = await _inventory.RequestInventoryAsync(ct);
                _store.ApplyInventory(inventory);

                // 각인/본부는 전투 스탯에 직접 반영되므로 전투를 시작하기 전에 반드시 받아 둔다.
                _store.ApplyEngravings(await _engraving.RequestInfoAsync(ct));
                // 코스튬도 외형뿐 아니라 스탯 보정을 주므로 전투를 시작하기 전에 받아 둔다.
                _store.ApplyCostumes(await _costume.RequestInfoAsync(ct));
                _store.ApplyHeadquarters(await _headquarters.RequestInfoAsync(ct));
                // 투자도 전투 효과(선제 스킬 게이지/스쿼드 재생)를 주므로 전투를 시작하기 전에 받아 둔다.
                _store.ApplyInvestment(await _investment.RequestAsync(ct));
                // 수련 효과도 최종 스탯에 들어가므로 전투를 시작하기 전에 받아 둔다
                // (빠뜨리면 이 씬에서만 내 파티가 수련 보정 없이 싸운다).
                _store.ApplyTrainingInfo(await _training.RequestInfoAsync(ct));
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
                _ui.ShowToast("의뢰 정보를 불러오지 못했습니다.");
                ExitToMain();
                return;
            } finally {
                _store.EndInitialLoad();
            }
        }

        StartStage(_stage);
    }

    // 지정 단계의 의뢰 데이터를 찾아 전투를 시작한다. 데이터가 없으면 메인으로 되돌린다.
    private void StartStage(int stage) {
        var data = _commissionDb.Get(_type, stage);
        if (data == null) {
            Debug.LogError($"[CommissionSceneManager] 의뢰 데이터 없음: type={_type} stage={stage}");
            _ui.ShowToast("의뢰 데이터를 찾을 수 없습니다.");
            ExitToMain();
            return;
        }

        _stage = stage;
        _director.Begin(data);
        _hud.RefreshForNewCommission();
    }

    private void OnFinished(CommissionResult result) {
        HandleFinishedAsync(result, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid HandleFinishedAsync(CommissionResult result, CancellationToken ct) {
        if (_leaving) return;

        var rewards = new System.Collections.Generic.List<RewardItem>();
        bool success = result == CommissionResult.Success;

        // 승패와 무관하게 "끝까지 싸웠다"를 먼저 알린다(메인 퀘스트의 의뢰 도전은 실패도 인정한다).
        // 도중에 나가면 이 자리에 오지 않으므로 자연히 인정되지 않는다.
        _commission.ReportAttempt(_type);

        // 성공했을 때만 서버에 클리어를 보고한다(보상/진행도는 서버가 확정).
        if (success) {
            try {
                var dto = await _commission.ClearAsync(_type, _stage, ct);
                if (dto.IsSuccess) {
                    _store.ApplyCommissionReward(dto);
                    rewards.AddRange(dto.Granted);
                    // 캐릭터 경험치는 지갑 재화가 아니라 지급 목록(Granted)에 실려 오지 않는다.
                    // 편성원 전원이 같은 양을 받으므로 대표값 1개를 보상 슬롯으로 덧붙여 보이게 한다.
                    int gainedExp = dto.CharExp.Count > 0 ? dto.CharExp[0].GainedExp : 0;
                    if (gainedExp > 0) rewards.Add(RewardItem.Exp(gainedExp));
                } else {
                    _ui.ShowToast(CommissionRewardRules.FailMessage(dto.FailReason));
                }
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
                _ui.ShowToast("보상 수령에 실패했습니다. 잠시 후 다시 시도해 주세요.");
            }
        }

        // 도전은 무료이므로 다음 단계 데이터만 있으면 바로 이어갈 수 있다(의뢰해결권과 무관).
        int nextStage = _stage + 1;
        bool hasNextData = _commissionDb.Get(_type, nextStage) != null;
        bool canContinue = success && hasNextData;
        string blockedReason = hasNextData ? null : "마지막 단계";

        string title = _director.Current != null ? _director.Current.DisplayName : "";

        try {
            var popup = await _ui.ShowPopupAsync<CommissionResultPopup>(_ctx.Popups.CommissionResultPopup, ct);
            var choice = await popup.ShowAsync(_ctx, success, title, rewards, canContinue, blockedReason);

            switch (choice) {
                case CommissionResultPopup.Choice.NextStage when canContinue:
                    await StartStageAsync(nextStage, ct);
                    return;
                // 재도전은 방금 실패한 단계를 그대로 다시 친다(도전은 무료라 재화 소모가 없다).
                case CommissionResultPopup.Choice.Retry:
                    await StartStageAsync(_stage, ct);
                    return;
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
        }

        ExitToMain();
    }

    // 지정 단계 도전(무료). 서버가 해금 여부를 확정해 준 뒤에만 실제로 시작한다.
    // 다음 단계 진행과 재도전이 같은 경로를 쓴다(둘 다 "그 단계를 지금 칠 수 있는가" 판정이 전부다).
    private async UniTask StartStageAsync(int stage, CancellationToken ct) {
        try {
            var start = await _commission.StartAsync(_type, stage, ct);
            if (!start.IsSuccess) {
                _ui.ShowToast(CommissionRewardRules.FailMessage(start.FailReason));
                ExitToMain();
                return;
            }
            _store.SetCommissionTickets(start.RemainingTickets);
            StartStage(stage);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ui.ShowToast("의뢰를 시작하지 못했습니다.");
            ExitToMain();
        }
    }

    // 메인 씬으로 복귀. 전환 중 이 오브젝트가 파괴되므로 await 하지 않고 던진다(매뉴얼 6-3).
    private void ExitToMain() {
        if (_leaving) return;
        _leaving = true;

        // 결과/일시정지 팝업은 닫힘 연출(페이드)이 남아 있을 수 있고, UIRoot 는 씬 전환에도 살아남는다.
        // 정리하지 않으면 메인 씬으로 돌아간 뒤에도 잠깐 떠 있으므로 여기서 확실히 닫는다.
        _ui.CloseAllPopups();

        if (_scenes == null || _scenes.MainScene == null || !_scenes.MainScene.RuntimeKeyIsValid()) {
            Debug.LogError("[CommissionSceneManager] SceneCatalog 에 메인 씬이 할당되지 않았습니다.");
            return;
        }
        _scene.LoadAddressableAsync(_scenes.MainScene.RuntimeKey).Forget();
    }
}
