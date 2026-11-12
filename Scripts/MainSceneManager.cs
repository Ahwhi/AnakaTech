using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using VContainer;

/// <summary>
/// 메인 씬 진입 오케스트레이션. 서버에서 계정 스냅샷(EnterMain)을 받아 PlayerStateStore 에 반영한 뒤
/// 자동전투(BattleDirector)를 시작한다. 실제 전투/루프는 BattleDirector 가 담당하고 여기서는 초기화만 한다.
/// 의존성은 MainLifetimeScope 가 주입한다.
/// </summary>
public class MainSceneManager : MonoBehaviour {
    private IMainDataService _mainData;
    private IInventoryService _inventory;
    private IEngravingService _engraving;
    private ICostumeService _costume;
    private IItemService _item;
    private IHeadquartersService _headquarters;
    private IInvestmentService _investment;

    // 없어도 게임이 도는 정보를 받아 온다. 실패하면 그 정보만 비운 채 넘어가고, 무엇이 빠졌는지는 로그로 남긴다.
    // 진입 순서에 요청을 하나 더할 때마다 "그 하나가 늦으면 메인이 통째로 안 열리는" 위험이 커지므로 이 통로로 감싼다.
    private static async UniTask TryLoadAsync(Func<UniTask> load, string what, CancellationToken ct) {
        try {
            await load();
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            throw;   // 씬을 떠나는 중이면 바깥이 정리하도록 그대로 올린다.
        } catch (OperationCanceledException) {
            Debug.LogWarning($"[MainSceneManager] {what} 정보를 받지 못했습니다(응답 지연). 이번 진입에는 빠진 채로 진행합니다.");
        } catch (Exception e) {
            Debug.LogWarning($"[MainSceneManager] {what} 정보를 받지 못했습니다: {e.Message}");
        }
    }
    private ICommissionService _commission;
    private IMainQuestService _mainQuest;
    private ITrainingService _training;
    private PlayerStateStore _store;
    private IUIService _ui;
    private BattleDirector _battle;
    private IHeartbeatService _heartbeat;
    private IOfflineRewardPresenter _offlineReward;
    private IFlashSaleService _flashSale;
    private IChatService _chat;
    private MainHudContext _hud;
    private UIPopupCatalog _popups;
    private ISessionRecovery _recovery;

    [Inject]
    public void Construct(IMainDataService mainData, IInventoryService inventory, IEngravingService engraving, ICostumeService costume,
                          IItemService item, IHeadquartersService headquarters, IInvestmentService investment, ICommissionService commission,
                          ITrainingService training, IMainQuestService mainQuest,
                          PlayerStateStore store, IUIService ui, BattleDirector battle, IHeartbeatService heartbeat,
                          IOfflineRewardPresenter offlineReward,
                          IFlashSaleService flashSale, IChatService chat,
                          MainHudContext hud, UIPopupCatalog popups, ISessionRecovery recovery) {
        _mainData = mainData;
        _inventory = inventory;
        _engraving = engraving;
        _costume = costume;
        _item = item;
        _headquarters = headquarters;
        _investment = investment;
        _commission = commission;
        _mainQuest = mainQuest;
        _training = training;
        _store = store;
        _ui = ui;
        _battle = battle;
        _heartbeat = heartbeat;
        _offlineReward = offlineReward;
        _flashSale = flashSale;
        _chat = chat;
        _hud = hud;
        _popups = popups;
        _recovery = recovery;
    }

    private void Start() {
        InitializeAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid InitializeAsync(CancellationToken ct) {
        // 씬이 올라온 뒤의 초기 데이터 로드도 유저에게는 같은 씬 전환이다. 팝업용 오버레이가 아니라
        // 씬 전환 화면을 이어서 붙잡아, 전환 도중 화면이 바뀌거나 두 로딩이 겹쳐 보이지 않게 한다.
        using (_ui.ShowSceneLoading()) {
            // 아래 요청들이 순차로 도착하며 캐릭터 스탯이 계단식으로 채워진다. 그 구간을 "변화"로 보는 표시가
            // 오작동하지 않도록(전투력 상승 연출 등) 로딩 중임을 상태로 알린다. 실패/취소로 빠져나가도 반드시 닫는다.
            _store.BeginInitialLoad();
            try {
                var data = await _mainData.EnterMainAsync(ct);
                _store.ApplyMainData(data);

                var inventory = await _inventory.RequestInventoryAsync(ct);
                _store.ApplyInventory(inventory);

                // 각인/본부는 캐릭터 최종 스탯에 직접 반영되므로 전투를 시작하기 전에 받아 둔다
                // (나중에 받으면 첫 런이 보정 없는 스탯으로 시작한다).
                _store.ApplyEngravings(await _engraving.RequestInfoAsync(ct));
                // 코스튬도 외형뿐 아니라 스탯 보정을 주므로 전투를 시작하기 전에 받아 둔다.
                _store.ApplyCostumes(await _costume.RequestInfoAsync(ct));
                _store.ApplyHeadquarters(await _headquarters.RequestInfoAsync(ct));
                // 투자도 전투 효과(선제 스킬 게이지/스쿼드 재생)를 주므로 전투를 시작하기 전에 받아 둔다.
                // 다만 이것 하나가 늦는다고 전투까지 못 열면 안 되므로 실패해도 넘어간다(효과만 이번 진입에 빠진다).
                await TryLoadAsync(async () => _store.ApplyInvestment(await _investment.RequestAsync(ct)),
                                   "투자", ct);
                // 수련 효과도 캐릭터 최종 스탯에 직접 반영되므로 전투 시작 전에 받는다.
                // 겸사겸사 완료 대기 중인 수련이 있으면 접속 직후부터 수련 버튼 배지가 켜진다.
                _store.ApplyTrainingInfo(await _training.RequestInfoAsync(ct));

                // 소모품은 전투에 영향을 주지 않지만, 덱 화면이 열리기 전에 받아 두면 목록이 빈 채로 뜨지 않는다.
                _store.ApplyItems(await _item.RequestInfoAsync(ct));

                // 범용 재화(파견 증명서/마일리지/모집권 등) 잔량. 메인 데이터에는 일부만 실려 오므로, 나머지는
                // 상점 카탈로그의 지갑으로 한 번 맞춰 둔다. 빠뜨리면 그 재화를 스스로 받아 오지 않는 팝업
                // (예: 파견)의 재화 패널이 상점을 한 번 다녀올 때까지 0 으로 보인다. 없어도 게임은 돌므로 실패해도 넘어간다.
                await TryLoadAsync(async () => _store.ApplyShopCurrencies((await _hud.Shop.RequestShopAsync(ct)).Currencies),
                                   "재화", ct);

                // 의뢰 정보는 팝업을 열 때도 다시 받지만, 여기서 미리 받아야 입장권 알림 배지가 접속 직후부터 켜진다.
                _store.ApplyCommissionInfo(await _commission.RequestInfoAsync(ct));

                // 메인 퀘스트는 "다음에 무엇을 할지"를 알려 주는 자리라 화면이 그려지는 순간 채워져 있어야 한다.
                // 이후 갱신은 서버 푸시가 맡으므로 여기서 받는 것은 이 한 번뿐이다.
                await _mainQuest.RefreshAsync(ct);
            } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                return;   // 씬을 떠나는 중이다. 이어서 할 일이 없다.
            } catch (OperationCanceledException) {
                // 씬은 살아 있는데 응답만 늦은 경우(요청별 타임아웃). 예전에는 여기서 조용히 돌아가
                // 아래 _battle.Begin() 까지 건너뛰는 바람에 "전투 필드만 죽은 채 나머지는 멀쩡한" 상태가 됐다.
                // 로그도 남지 않아 원인을 찾을 단서조차 없었다. 이제는 남기고, 전투는 그대로 연다.
                Debug.LogWarning("[MainSceneManager] 메인 데이터 응답이 늦어 일부가 비어 있습니다. 전투는 그대로 시작합니다.");
                _ui.ShowToast("일부 정보를 불러오지 못했습니다.");
            } catch (Exception e) {
                Debug.LogException(e);
                _ui.ShowToast("메인 데이터를 불러오지 못했습니다.");
            } finally {
                _store.EndInitialLoad();
            }
        }

        // 전투는 어떤 경우에도 시작한다. 여기서 못 열면 화면에 아무것도 돌지 않고, 유저가 할 수 있는 일이
        // 재접속뿐이 된다(카메라 설정도 이 안에서 하므로 배경/캐릭터 크기까지 어긋난다).
        _battle.Begin();

        // 기력 부스트권 자정 충전을 화면까지 흘린다. 충전은 서버가 접근 시점에 따라잡는 방식이라
        // 앱을 켜 둔 채 자정을 넘기면 다음 접근까지 어제 값이 남는데, 주기 신호인 하트비트가 그 접근을 겸한다.
        // 하트비트는 씬보다 오래 사는 서비스라 저장소를 직접 건드리지 않으므로, 여기서 받아 넣는다.
        //
        // 하트비트가 흘리는 것은 마지막 값이 아니라 "방금 서버가 알려줬다"는 알림이다. 마지막 값을 품고
        // 있었다면 구독하는 이 순간 이전 계정의 값이 재생되어, 바로 위에서 스냅샷으로 채운 값을 덮어썼다.
        _heartbeat.StaminaBoostTicket
                  .Subscribe(v => _store.StaminaBoostTicket.Value = v)
                  .AddTo(this);

        // 접속 유지 시간 적립 시작(접속 보상 이벤트). 스냅샷을 받은 뒤에 켜야 계정이 확정된 상태로 신호가 나간다.
        _heartbeat.Start();

        // 오프라인 보상은 결과 통보일 뿐이라 게임 진행을 막을 이유가 없다. 그래서 기다리지 않고(Forget)
        // 전투/HUD 가 이미 돌기 시작한 뒤에 띄운다. 기다리면 확인을 누르기 전까지 배경도 전투도 시작되지 않아
        // 로딩이 덜 끝난 화면 위에 팝업만 떠 있는 것처럼 보인다.
        // 지급은 서버가 이미 끝냈으므로 여기서 실패하거나 팝업을 닫아도 보상이 사라지지 않는다.
        _offlineReward.PresentAsync(ct).Forget();

        BeginFlashSaleWatch(ct);

        // 최근 대화를 한 번 받아 둔다. 메인 화면의 채팅 한 줄이 접속 직후부터 채워져 있게 하는 것이 목적이고,
        // 이후 대화는 서버 푸시로 계속 쌓인다(채팅창을 열지 않아도 흐름이 이어진다).
        _chat.RefreshAsync(ct).Forget();

        // 앱을 내려 두었다 돌아와 화면 그대로 다시 붙은 경우(ConnectionGuard), 메인 진입이 다시 일어나지 않는다.
        // 끊겨 있던 동안 놓친 서버 푸시(우편/이벤트/대화/한정 판매)와 새 세션에서 받을 수 있는 오프라인 보상을 여기서 맞춘다.
        _recovery.Recovered
                 .Subscribe(_ => ResyncAfterRecovery(ct))
                 .AddTo(this);
    }

    private void ResyncAfterRecovery(CancellationToken ct) {
        _chat.RefreshAsync(ct).Forget();
        _flashSale.RefreshAsync(ct).Forget();
        if (_hud.Badges != null) _hud.Badges.RefreshAsync(ct).Forget();

        // 오프라인 보상은 서버 기준(최소 오프라인 시간)을 넘겼을 때만 지급되므로, 잠깐 다녀온 경우에는 아무것도 뜨지 않는다.
        _offlineReward.PresentAsync(ct).Forget();
    }

    /// <summary>
    /// 한정 판매를 화면까지 흘린다. 지금 열려 있는 것을 한 번 받아 메인 버튼 배지를 맞추고,
    /// 이후 새로 열리는 것(서버 알림)은 안내 팝업으로 띄운다.
    ///
    /// 발동시킨 컨텐츠가 무엇이든 알림은 한 종류로 오므로, 나중에 스테이지 말고 다른 조건이 추가되어도
    /// 이 자리는 그대로다(그 컨텐츠가 서버에서 발동만 시키면 된다).
    /// </summary>
    private void BeginFlashSaleWatch(CancellationToken ct) {
        // 배지는 목록을 받아야 맞으므로 진입 직후 한 번 받아 둔다(실패해도 다음 알림에서 맞춰진다).
        _flashSale.RefreshAsync(ct).Forget();

        _flashSale.Opened
                  .Subscribe(_ => ShowFlashSaleAsync(ct).Forget())
                  .AddTo(this);
    }

    private async UniTaskVoid ShowFlashSaleAsync(CancellationToken ct) {
        var reference = _popups != null ? _popups.FlashSalePopup : null;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;

        try {
            // 절전 모드(잠금 화면) 중에 열린 판매는 바로 띄우지 않고, 유저가 잠금을 풀어 화면으로 돌아왔을 때 띄운다.
            // 절전이 아니면 기다리지 않고 지나간다.
            await UniTask.WaitUntil(() => !PowerSaveMode.Active.Value, cancellationToken: ct);

            var popup = await _ui.ShowPopupAsync<FlashSalePopup>(reference, ct);
            popup.Initialize(_hud);
            await popup.ShowAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }
}
