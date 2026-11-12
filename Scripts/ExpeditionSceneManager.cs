using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 원정대 씬 진입/종료 오케스트레이션(GuildRaidSceneManager 와 같은 자리/구조).
///
/// 입장 처리(오늘 횟수 소모 + 장소 점유)는 원정대 팝업이 이미 끝냈고, 이 씬은 그 결과(ExpeditionSession)로 전투만 진행한다.
/// 진입 시 내 데이터와 함께 파티원 편성 스냅샷을 받아 아군을 세우고, 끝나면 깎은 피해량을 보고한 뒤 메인으로 돌아간다.
///
/// 결과 보고는 피해량이 0 이어도 반드시 한 번 나가야 한다. 보고가 곧 장소 점유 해제이자 참여 보상 지급이라,
/// 빠뜨리면 다른 파티원이 점유가 풀릴 때까지 그 장소를 칠 수 없고 나는 보상을 못 받는다.
/// </summary>
public sealed class ExpeditionSceneManager : MonoBehaviour {
    private IMainDataService _mainData;
    private IInventoryService _inventory;
    private IEngravingService _engraving;
    private ICostumeService _costume;
    private ITrainingService _training;
    private IHeadquartersService _headquarters;
    private IInvestmentService _investment;
    private IExpeditionService _expedition;
    private PlayerStateStore _store;
    private IUIService _ui;
    private ISceneService _scene;
    private SceneCatalog _scenes;
    private UIPopupCatalog _popups;
    private ExpeditionDirector _director;
    private GuildRaidHudController _hud;
    private BattleHudContext _hudContext;

    private bool _leaving;
    private bool _reported;

    [Inject]
    public void Construct(IMainDataService mainData, IInventoryService inventory, IEngravingService engraving,
                          ICostumeService costume, ITrainingService training,
                          IHeadquartersService headquarters, IInvestmentService investment,
                          IExpeditionService expedition, PlayerStateStore store,
                          IUIService ui, ISceneService scene, SceneCatalog scenes, UIPopupCatalog popups,
                          ExpeditionDirector director, GuildRaidHudController hud, BattleHudContext hudContext) {
        _mainData = mainData;
        _inventory = inventory;
        _engraving = engraving;
        _costume = costume;
        _training = training;
        _headquarters = headquarters;
        _investment = investment;
        _expedition = expedition;
        _store = store;
        _ui = ui;
        _scene = scene;
        _scenes = scenes;
        _popups = popups;
        _director = director;
        _hud = hud;
        _hudContext = hudContext;
    }

    private void Start() {
        if (_director == null || _hud == null) {
            Debug.LogError("[ExpeditionSceneManager] 의존성 주입이 되지 않았습니다. " +
                           "ExpeditionLifetimeScope 의 Configure 에 ExpeditionDirector / GuildRaidHudController / " +
                           "ExpeditionSceneManager 의 RegisterComponentInHierarchy 가 모두 있는지 확인하세요.");
            return;
        }

        _director.Finished += OnFinished;
        _hud.ExitRequested += OnExitRequested;
        InitializeAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnDestroy() {
        if (_director != null) _director.Finished -= OnFinished;
        if (_hud != null) _hud.ExitRequested -= OnExitRequested;
    }

    private async UniTaskVoid InitializeAsync(CancellationToken ct) {
        if (!ExpeditionSession.HasEntry) {
            Debug.LogWarning("[ExpeditionSceneManager] 입장 정보가 없어 메인 씬으로 돌아갑니다.");
            ExitToMain();
            return;
        }

        var teammates = new List<(ExpeditionMemberDto member, UserDeckDto deck)>();
        using (_ui.ShowSceneLoading()) {
            _store.BeginInitialLoad();
            try {
                // 내 캐릭터는 내 최신 상태로 싸우므로 레이드 씬과 같은 데이터를 전부 받아 둔다.
                _store.ApplyMainData(await _mainData.EnterMainAsync(ct));
                _store.ApplyInventory(await _inventory.RequestInventoryAsync(ct));
                _store.ApplyEngravings(await _engraving.RequestInfoAsync(ct));
                _store.ApplyCostumes(await _costume.RequestInfoAsync(ct));
                _store.ApplyHeadquarters(await _headquarters.RequestInfoAsync(ct));
                _store.ApplyInvestment(await _investment.RequestAsync(ct));
                _store.ApplyTrainingInfo(await _training.RequestInfoAsync(ct));

                // 파티원은 서버 스냅샷으로 재현한다(주인이 탈퇴했어도 스냅샷은 남아 있다).
                // 응답 형태가 같은 패킷(S_UserDeck)이라 겹치지 않도록 한 명씩 차례로 받는다.
                foreach (var m in ExpeditionSession.Teammates)
                    teammates.Add((m, await _expedition.RequestMemberDeckAsync(m.Slot, ct)));
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
                _ui.ShowToast("원정대 정보를 불러오지 못했습니다.");
                // 이미 입장 처리가 끝났으므로 보고는 보내 둔다(점유 해제 + 참여 보상).
                await ReportDamageAsync(0, ct);
                ExitToMain();
                return;
            } finally {
                _store.EndInitialLoad();
            }
        }

        _director.Begin(ExpeditionSession.Location, ExpeditionSession.Level, ExpeditionSession.Hp,
                        ExpeditionSession.MaxHp, ExpeditionSession.MyCharDataId, teammates);
        _hud.RefreshForNewRaid();
    }

    private void OnFinished(ExpeditionResult result, long dealtDamage) {
        HandleFinishedAsync(result, dealtDamage, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid HandleFinishedAsync(ExpeditionResult result, long dealtDamage, CancellationToken ct) {
        if (_leaving) return;

        var report = await ReportDamageAsync(dealtDamage, ct);

        try {
            var reference = _popups.ExpeditionResultPopup;
            if (reference != null && reference.RuntimeKeyIsValid()) {
                var popup = await _ui.ShowPopupAsync<ExpeditionResultPopup>(reference, ct);
                await popup.ShowAsync(result, _director.BossName, dealtDamage, report, _hudContext);
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
        }

        ExitToMain();
    }

    /// <summary>깎은 피해량 보고(한 번만). 결과 팝업 경로와 나가기 경로가 같은 함수를 쓴다.</summary>
    private async UniTask<ExpeditionDamageDto> ReportDamageAsync(long dealtDamage, CancellationToken ct) {
        if (_reported) return null;
        _reported = true;

        try {
            var res = await _expedition.ReportDamageAsync(ExpeditionSession.Location, Math.Max(0, dealtDamage), ct);
            if (!res.IsSuccess) _ui.ShowToast(ExpeditionRules.FailMessage(res.FailReason));
            return res;
        } catch (OperationCanceledException) {
            return null;
        } catch (Exception e) {
            Debug.LogException(e);
            _ui.ShowToast("원정대 결과 반영에 실패했습니다.");
            return null;
        }
    }

    // 전투 도중 나가기: 그때까지 깎은 피해는 살려 준다.
    private void OnExitRequested() {
        if (_leaving) return;
        ExitWithReportAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid ExitWithReportAsync(CancellationToken ct) {
        await ReportDamageAsync(_director != null ? _director.DealtDamage : 0, ct);
        ExitToMain();
    }

    private void ExitToMain() {
        if (_leaving) return;
        _leaving = true;

        ExpeditionSession.Clear();
        _ui.CloseAllPopups();

        if (_scenes == null || _scenes.MainScene == null || !_scenes.MainScene.RuntimeKeyIsValid()) {
            Debug.LogError("[ExpeditionSceneManager] SceneCatalog 에 메인 씬이 할당되지 않았습니다.");
            return;
        }
        _scene.LoadAddressableAsync(_scenes.MainScene.RuntimeKey).Forget();
    }
}
