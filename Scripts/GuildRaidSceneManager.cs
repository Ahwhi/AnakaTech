using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 길드 레이드 씬 진입/종료 오케스트레이션(CommissionSceneManager 와 같은 자리/구조).
///
/// 입장 처리(참여 횟수 소모/결제)는 길드 팝업이 이미 끝냈고, 이 씬은 그 결과(GuildRaidSession)로 전투만 진행한다.
/// 전투가 끝나면 깎은 피해량을 서버에 보고하고 결과를 보여 준 뒤 메인으로 돌아간다.
///
/// 피해 보고는 반드시 한 번은 나가야 한다. 보고하지 않으면 참여 횟수만 쓰고 기여가 사라지므로,
/// 나가기 경로가 무엇이든(결과 팝업/뒤로가기) 보고를 먼저 끝낸 뒤 씬을 뜬다.
/// </summary>
public sealed class GuildRaidSceneManager : MonoBehaviour {
    private IMainDataService _mainData;
    private IInventoryService _inventory;
    private IEngravingService _engraving;
    private ICostumeService _costume;
    private ITrainingService _training;
    private IHeadquartersService _headquarters;
    private IInvestmentService _investment;
    private IGuildService _guild;
    private PlayerStateStore _store;
    private IUIService _ui;
    private ISceneService _scene;
    private SceneCatalog _scenes;
    private UIPopupCatalog _popups;
    private GuildRaidDirector _director;
    private GuildRaidHudController _hud;
    // 보상 아이콘/이름 해석에 쓰는 HUD 공용 묶음(결과 팝업으로 넘긴다).
    private BattleHudContext _hudContext;

    private bool _leaving;
    private bool _reported;   // 피해 보고를 이미 보냈는지(중복 보고 방지)

    [Inject]
    public void Construct(IMainDataService mainData, IInventoryService inventory, IEngravingService engraving, ICostumeService costume,
                          ITrainingService training,
                          IHeadquartersService headquarters, IInvestmentService investment, IGuildService guild, PlayerStateStore store,
                          IUIService ui, ISceneService scene, SceneCatalog scenes, UIPopupCatalog popups,
                          GuildRaidDirector director, GuildRaidHudController hud, BattleHudContext hudContext) {
        _mainData = mainData;
        _inventory = inventory;
        _engraving = engraving;
        _costume = costume;
        _training = training;
        _headquarters = headquarters;
        _investment = investment;
        _guild = guild;
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
            Debug.LogError("[GuildRaidSceneManager] 의존성 주입이 되지 않았습니다. " +
                           "GuildRaidLifetimeScope 의 Configure 에 GuildRaidDirector / GuildRaidHudController / " +
                           "GuildRaidSceneManager 의 RegisterComponentInHierarchy 가 모두 있는지 확인하세요.");
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
        if (!GuildRaidSession.HasEntry) {
            // 입장 정보 없이 씬이 열렸다(에디터에서 직접 실행 등). 진행할 대상이 없으므로 메인으로 되돌린다.
            Debug.LogWarning("[GuildRaidSceneManager] 입장 정보가 없어 메인 씬으로 돌아갑니다.");
            ExitToMain();
            return;
        }

        // 씬이 올라온 뒤의 초기 데이터 로드도 유저에게는 같은 씬 전환이다. 팝업용 오버레이가 아니라
        // 씬 전환 화면을 이어서 붙잡아, 전환 도중 화면이 바뀌거나 두 로딩이 겹쳐 보이지 않게 한다.
        using (_ui.ShowSceneLoading()) {
            _store.BeginInitialLoad();
            try {
                _store.ApplyMainData(await _mainData.EnterMainAsync(ct));
                _store.ApplyInventory(await _inventory.RequestInventoryAsync(ct));
                // 각인/본부는 전투 스탯에 직접 반영되므로 전투 시작 전에 반드시 받아 둔다.
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
                _ui.ShowToast("레이드 정보를 불러오지 못했습니다.");
                ExitToMain();
                return;
            } finally {
                _store.EndInitialLoad();
            }
        }

        _director.Begin(GuildRaidSession.BossLevel, GuildRaidSession.BossHp, GuildRaidSession.BossMaxHp);
        _hud.RefreshForNewRaid();
    }

    // 제한 시간 종료 또는 보스 처치.
    private void OnFinished(GuildRaidResult result, long dealtDamage) {
        HandleFinishedAsync(result, dealtDamage, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid HandleFinishedAsync(GuildRaidResult result, long dealtDamage, CancellationToken ct) {
        if (_leaving) return;

        var report = await ReportDamageAsync(dealtDamage, ct);
        // 제한 시간 종료/보스 처치 = 끝까지 치른 판이다(도중 나가기는 ExitWithReportAsync 로 가서 여기를 지나지 않는다).
        // 서버는 입장 성공을 완주 허용의 근거로 쓰므로 피해량 보고 성공 여부와 무관하게 보낸다.
        _guild.ReportRaidFinished();

        try {
            var reference = _popups.GuildRaidResultPopup;
            if (reference != null && reference.RuntimeKeyIsValid()) {
                var popup = await _ui.ShowPopupAsync<GuildRaidResultPopup>(reference, ct);
                await popup.ShowAsync(result, dealtDamage, report, _hudContext);
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
        }

        ExitToMain();
    }

    /// <summary>
    /// 깎은 피해량을 서버에 보고한다. 실패해도 그 판의 기여가 사라지는 것이라 유저에게 알린다.
    /// 중복 보고를 막기 위해 한 번만 보낸다(결과 팝업 경로와 뒤로가기 경로가 같은 함수를 쓴다).
    ///
    /// 피해가 0 이어도 보낸다(V1.13). 한 번에 한 명만 도전할 수 있어서, 이 보고가 서버의 "도전 중" 자리를 비우는 신호다.
    /// 보내지 않으면 다른 길드원이 시간 초과까지 기다려야 한다. 서버는 피해 0 보고에 보상을 주지 않는다.
    /// </summary>
    private async UniTask<GuildRaidDamageDto> ReportDamageAsync(long dealtDamage, CancellationToken ct) {
        if (_reported) return null;
        _reported = true;

        try {
            var res = await _guild.ReportRaidDamageAsync(dealtDamage, ct);
            if (!res.IsSuccess) _ui.ShowToast("기여도 반영에 실패했습니다.");
            return res;
        } catch (OperationCanceledException) {
            return null;
        } catch (Exception e) {
            Debug.LogException(e);
            _ui.ShowToast("기여도 반영에 실패했습니다.");
            return null;
        }
    }

    // 전투 도중 나가기(일시정지 메뉴 등): 그때까지 깎은 피해는 살려 준다.
    private void OnExitRequested() {
        if (_leaving) return;
        ExitWithReportAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid ExitWithReportAsync(CancellationToken ct) {
        await ReportDamageAsync(_director != null ? _director.DealtDamage : 0, ct);
        ExitToMain();
    }

    // 메인 씬으로 복귀. 전환 중 이 오브젝트가 파괴되므로 await 하지 않고 던진다.
    private void ExitToMain() {
        if (_leaving) return;
        _leaving = true;

        GuildRaidSession.Clear();
        _ui.CloseAllPopups();

        if (_scenes == null || _scenes.MainScene == null || !_scenes.MainScene.RuntimeKeyIsValid()) {
            Debug.LogError("[GuildRaidSceneManager] SceneCatalog 에 메인 씬이 할당되지 않았습니다.");
            return;
        }
        _scene.LoadAddressableAsync(_scenes.MainScene.RuntimeKey).Forget();
    }
}
