using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 유저 대 유저 전투 씬 진입/종료 오케스트레이션.
///
/// 전투 자체는 컨텐츠와 무관하지만 결과를 누구에게 보고할지는 다르므로, 진입 정보(PvpBattleSession.Origin)로 갈라
/// 보고 경로만 분기한다. 새 PvP 컨텐츠가 생기면 여기 분기 한 줄과 Origin 값 하나만 추가하면 된다.
///
/// 보고는 반드시 한 번은 나가야 한다. 공격 횟수는 이미 소모됐으므로 보고 없이 나가면 그 판이 통째로 사라진다.
/// </summary>
public sealed class PvpBattleSceneManager : MonoBehaviour {
    private IMainDataService _mainData;
    private IInventoryService _inventory;
    private IEngravingService _engraving;
    private ICostumeService _costume;
    private ITrainingService _training;
    private IHeadquartersService _headquarters;
    private IInvestmentService _investment;
    private IGuildService _guild;
    private IArenaService _arena;
    private PlayerStateStore _store;
    private IUIService _ui;
    private ISceneService _scene;
    private SceneCatalog _scenes;
    private UIPopupCatalog _popups;
    private PvpBattleDirector _director;
    private PvpBattleHudController _hud;
    // 보상 아이콘/이름 해석에 쓰는 HUD 공용 묶음(결과 팝업으로 넘긴다).
    private BattleHudContext _hudContext;

    private bool _leaving;
    private bool _reported;

    [Inject]
    public void Construct(IMainDataService mainData, IInventoryService inventory, IEngravingService engraving, ICostumeService costume,
                          ITrainingService training,
                          IHeadquartersService headquarters, IInvestmentService investment, IGuildService guild, IArenaService arena,
                          PlayerStateStore store,
                          IUIService ui, ISceneService scene, SceneCatalog scenes, UIPopupCatalog popups,
                          PvpBattleDirector director, PvpBattleHudController hud, BattleHudContext hudContext) {
        _mainData = mainData;
        _inventory = inventory;
        _engraving = engraving;
        _costume = costume;
        _training = training;
        _headquarters = headquarters;
        _investment = investment;
        _guild = guild;
        _arena = arena;
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
            Debug.LogError("[PvpBattleSceneManager] 의존성 주입이 되지 않았습니다. " +
                           "PvpBattleLifetimeScope 의 Configure 에 PvpBattleDirector / PvpBattleHudController / " +
                           "PvpBattleSceneManager 의 RegisterComponentInHierarchy 가 모두 있는지 확인하세요.");
            return;
        }

        _director.Finished += OnFinished;
        InitializeAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnDestroy() {
        if (_director != null) _director.Finished -= OnFinished;
    }

    private async UniTaskVoid InitializeAsync(CancellationToken ct) {
        if (!PvpBattleSession.HasEntry) {
            Debug.LogWarning("[PvpBattleSceneManager] 대전 정보가 없어 메인 씬으로 돌아갑니다.");
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
                // 수련 효과(사거리/2회공격/회피 등)도 최종 스탯에 들어간다. 상대 파티는 서버가 준 수련 결과로
                // 살아나므로, 이것을 빠뜨리면 내 파티만 수련 없이 싸우는 불공정한 대전이 된다.
                _store.ApplyTrainingInfo(await _training.RequestInfoAsync(ct));
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
                _ui.ShowToast("전투 정보를 불러오지 못했습니다.");
                ExitToMain();
                return;
            } finally {
                _store.EndInitialLoad();
            }
        }

        // 쟁탈전이면 매칭 응답에 실려 온 양 팀 룬을 넘긴다(다른 컨텐츠는 null = 보정 없음).
        _director.Begin(PvpBattleSession.OpponentDeck, PvpBattleRules.TuningOf(PvpBattleSession.Origin),
                        PvpBattleSession.MyRunes, PvpBattleSession.OpponentRunes);
        _hud.RefreshForNewBattle();
    }

    private void OnFinished(PvpBattleResult result) {
        HandleFinishedAsync(result, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid HandleFinishedAsync(PvpBattleResult result, CancellationToken ct) {
        if (_leaving) return;

        bool win = result == PvpBattleResult.Win;
        // 쟁탈전 점수는 "얼마나 압도했는가"로 정해진다. 이긴 쪽은 자기 남은 체력이, 진 쪽은 상대 남은 체력이 기준이라
        // 승패가 갈린 이 시점에 한 번만 읽어 둔다(전장이 정리되면 값이 사라진다).
        float hpRatio = win ? _director.MyHpRatio : _director.EnemyHpRatio;
        var rewards = await ReportAsync(win, hpRatio, ct);

        try {
            var reference = _popups.PvpBattleResultPopup;
            if (reference != null && reference.RuntimeKeyIsValid()) {
                var popup = await _ui.ShowPopupAsync<PvpBattleResultPopup>(reference, ct);
                await popup.ShowAsync(win, _director.OpponentName, rewards, _hudContext);
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
        }

        ExitToMain();
    }

    /// <summary>
    /// 결과를 진입한 컨텐츠에 보고하고, 그 응답으로 받은 참여 보상을 돌려준다(결과 팝업이 그린다).
    /// 컨텐츠가 늘면 여기 분기만 추가한다.
    /// 한 번만 보내며(중복 보고 방지), 실패해도 유저에게 알리고 넘어간다.
    /// </summary>
    private async UniTask<List<RewardItem>> ReportAsync(bool win, float hpRatio, CancellationToken ct) {
        if (_reported) return null;
        _reported = true;

        try {
            switch (PvpBattleSession.Origin) {
                case PvpBattleSession.OriginType.GuildWar: {
                    var res = await _guild.ReportWarAttackAsync(PvpBattleSession.OpponentAccountId, win, ct);
                    if (!res.IsSuccess) { _ui.ShowToast("전투 결과 반영에 실패했습니다."); return null; }
                    return res.Granted;
                }

                case PvpBattleSession.OriginType.Arena: {
                    // 점수 산정(반올림/상하한)은 서버가 하므로 여기서는 체력 비율만 규약대로 옮겨 보낸다.
                    int hpTenth = ArenaRules.ToHpPercentTenth(hpRatio);
                    var res = await _arena.ReportAsync(PvpBattleSession.OpponentAccountId, win, hpTenth, ct);
                    if (!res.IsSuccess) { _ui.ShowToast(ArenaRules.FailMessage(res.FailReason)); return null; }

                    // 코인/점수는 서버 확정값이므로 그대로 반영한다(메인으로 돌아가면 팝업이 최신 상태로 열린다).
                    _store.SetCurrency(CurrencyType.ArenaCoin, res.NewCoin);
                    return res.Granted;
                }
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ui.ShowToast("전투 결과 반영에 실패했습니다.");
        }
        return null;
    }

    private void ExitToMain() {
        if (_leaving) return;
        _leaving = true;

        PvpBattleSession.Clear();
        _ui.CloseAllPopups();

        if (_scenes == null || _scenes.MainScene == null || !_scenes.MainScene.RuntimeKeyIsValid()) {
            Debug.LogError("[PvpBattleSceneManager] SceneCatalog 에 메인 씬이 할당되지 않았습니다.");
            return;
        }
        _scene.LoadAddressableAsync(_scenes.MainScene.RuntimeKey).Forget();
    }
}
