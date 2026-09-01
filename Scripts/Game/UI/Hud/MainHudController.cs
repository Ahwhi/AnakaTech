using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using VContainer;
using static Enums;

/// <summary>
/// MainScene HUD 총괄. 주입받은 MainHudContext 로 배치된 각 패널을 초기화(널 세이프)하고,
/// 전투 클리어 보상 이벤트를 받아 획득 팝업을 띄운다(PLANNING 7). 패널들은 이 컨트롤러가 구동하는 뷰라
/// 개별 DI 등록이 필요 없다. 이 컴포넌트만 씬에 배치하면 된다(MainLifetimeScope 가 주입).
///
/// 메뉴 버튼(상점/도감/덱 등)은 더 이상 이 컨트롤러가 관리하지 않는다. 각 버튼이 HudMenuButton 으로
/// 자가 주입되어 스스로 팝업을 열기 때문(확장 시 이 스크립트를 건드릴 필요가 없다).
/// </summary>
public sealed class MainHudController : MonoBehaviour {
    [Header("Panels (배치한 것만 연결, 비워도 됨)")]
    [SerializeField] private MainInfoPanel infoPanel;
    [SerializeField] private CurrencyPanel currencyPanel;
    [Tooltip("두 손가락 핀치 줌. 줌 버튼 대신 화면을 직접 벌려 확대/축소한다.")]
    [SerializeField] private BattlePinchZoom pinchZoom;
    [SerializeField] private DungeonInfoPanel dungeonPanel;
    [SerializeField] private BattleCharacterPanel battleCharacterPanel;
    [SerializeField] private DamageMeterPanel damageMeterPanel;
    [Tooltip("메인 퀘스트 안내(다음에 할 일 + 완료 시 보상 수령).")]
    [SerializeField] private MainQuestPanel mainQuestPanel;

    private MainHudContext _ctx;
    private DungeonClearRewardPopup _currentReward;
    private CombatPowerAnnouncer _combatPower;
    private readonly CompositeDisposable _subs = new();
    // 전투력 보고를 묶기 위한 변경 통지(성장/편성 변경이 잠잠해지면 한 번만 보낸다).
    private Subject<Unit> _combatPowerDirty;
    // 메인 데이터 초기 로딩이 끝났는지. 복귀 팝업을 여는 시점을 이 값으로 미룬다.
    private bool _mainDataReady;

    [Inject]
    public void Construct(MainHudContext ctx) => _ctx = ctx;

    private void Start() {
        if (infoPanel != null) infoPanel.Initialize(_ctx);
        if (currencyPanel != null) currencyPanel.Initialize(_ctx);
        if (pinchZoom != null) pinchZoom.Initialize(_ctx);
        if (dungeonPanel != null) dungeonPanel.Initialize(_ctx);
        if (battleCharacterPanel != null) battleCharacterPanel.Initialize(_ctx);
        if (damageMeterPanel != null) damageMeterPanel.Initialize(_ctx);
        if (mainQuestPanel != null) mainQuestPanel.Initialize(_ctx);

        // 완료 알림은 메인 퀘스트 패널이 아니라 여기서 받는다. 알림의 목적이 "다른 팝업을 보고 있어도 알게 하는 것"이라
        // 패널이 가려져 있거나 꺼져 있어도 떠야 하기 때문이다(HUD 컨트롤러는 메인 씬 내내 살아 있다).
        if (_ctx.MainQuest != null)
            _ctx.MainQuest.Completed
                .Subscribe(_ => {
                    // 알림음도 토스트와 같은 자리에서 낸다. 어느 화면을 보고 있든 "완료됐다"를 알리는 것이 목적이다.
                    _ctx.Sound.PlaySfx(SfxKey.QuestComplete);
                    _ctx.Ui.ShowToast(MainQuestRules.CompletedToast);
                })
                .AddTo(_subs);

        // 총 전투력 상승 감시. 패널이 아니라 규칙이라 뷰를 갖지 않고, 표시는 전역 UI 서비스에 맡긴다.
        // 연속 상승을 묶어 띄우기 위해 대기가 필요하므로 씬 종료 토큰을 넘긴다.
        _combatPower = new CombatPowerAnnouncer(_ctx, this.GetCancellationTokenOnDestroy());

        _ctx.Battle.RewardReceived += OnRewardReceived;
        _ctx.Battle.StageFailed += OnStageFailed;

        // 새 스테이지를 깨면 이벤트 배지를 다시 받아온다(코토하의 동기 부여, 7일 미션의 스테이지 미션 등이
        // 그 순간 수령 가능해진다). 매 클리어가 아니라 "진도가 올라갔을 때"만 태워, 방치 중 반복 클리어로
        // 서버를 두드리지 않는다. Skip(1) 로 구독 시점의 현재값은 흘려보낸다(진입 시 갱신은 배지 뷰가 한다).
        if (_ctx.Badges != null)
            _ctx.Store.HighestClearedDungeonIndex
                .Skip(1)
                .Subscribe(_ => _ctx.Badges.RefreshEventClaimStatesAsync(this.GetCancellationTokenOnDestroy()).Forget())
                .AddTo(_subs);

        BindCombatPowerReport();

        // 메인 데이터가 다 들어온 뒤에 열어야 하므로 신호를 먼저 걸어 둔다(아래 ReopenReturnedPopupAsync 참고).
        _ctx.Store.InitialLoadCompleted += OnInitialLoadCompleted;
        ReopenReturnedPopupAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// 인앱결제 준비와 미완료 결제 복구를 시작한다.
    ///
    /// 메인 데이터가 다 들어온 뒤에 하는 이유는 상품 목록을 서버 카탈로그에서 받아야 하기 때문이다.
    /// 실기기가 아니면 아무것도 하지 않으므로 에디터에서는 왕복이 늘지 않는다.
    /// 실패해도 게임 진행을 막지 않는다(현금 상품을 누를 때 안내가 나가고, 복구는 다음 접속에 다시 시도된다).
    /// </summary>
    private async UniTaskVoid StartIapAsync(CancellationToken ct) {
        if (_ctx.Iap == null || !_ctx.Iap.IsSupported) return;

        try {
            // 초기 로딩 신호가 두 번 오면 조정자도 두 개가 되어 같은 구독이 겹친다. 앞의 것을 먼저 정리한다.
            _iapRecovery?.Dispose();
            _iapRecovery = new IapRecoveryCoordinator(_ctx, ct);
            var catalog = await _ctx.Shop.RequestShopAsync(ct);
            await _iapRecovery.InitializeAsync(catalog);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogWarning($"[IAP] 준비 실패: {e.Message}");
        }
    }

    private IapRecoveryCoordinator _iapRecovery;

    private void OnInitialLoadCompleted() {
        _mainDataReady = true;
        StartIapAsync(this.GetCancellationTokenOnDestroy()).Forget();
        // 진입 시점의 보고는 스토어가 비어 있어 걸러졌다. 값이 다 채워진 지금이 실제 전투력을 알릴 자리다
        // (변경 통지로도 결국 보내지지만 묶음 대기 시간만큼 늦는다).
        ReportCombatPower();
    }

    /// <summary>
    /// 전투 씬으로 넘어가기 전에 닫아 둔 팝업을 다시 연다(예약이 없으면 아무 일도 하지 않는다).
    /// 어떤 팝업인지는 예약한 쪽이 정하므로 여기서는 기능을 알 필요가 없다.
    ///
    /// 씬 진입 직후에는 MainSceneManager 가 메인 데이터를 순차로 받아오는 중이라 스토어가 비어 있다.
    /// 그때 팝업을 열면 전투력 같은 스토어 기반 표시가 0 으로 그려진 채 남으므로, 로딩이 끝나기를 기다린다.
    /// </summary>
    private async UniTaskVoid ReopenReturnedPopupAsync(CancellationToken ct) {
        if (!MainPopupReturn.Consume(out var reference, out int tabIndex)) return;

        try {
            await UniTask.WaitUntil(() => _mainDataReady, cancellationToken: ct);

            var view = await _ctx.Ui.ShowPopupAsync<UIView>(reference, ct);
            if (view is IHudPopup hudPopup) hudPopup.Initialize(_ctx);
            if (tabIndex >= 0 && view is IHudPopupTab tabbed) tabbed.SelectTab(tabIndex);
            await view.ShowAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// 덱 전투력을 서버에 알린다(길드원 목록/길드 전투력/대항전 매칭 + 유저 랭킹과 타인 프로필의 근거).
    /// 길드에 속하지 않아도 계정 기록으로 남으므로 보고는 소속과 무관하게 항상 한다.
    /// 전투력은 성장/장비/편성이 바뀔 때마다 조금씩 움직이므로, 변경 통지를 묶어(Throttle) 잠잠해진 뒤 한 번만 보낸다.
    /// 실패해도 다음 보고가 덮으므로 재시도하지 않는다.
    /// </summary>
    private void BindCombatPowerReport() {
        if (_ctx.Guild == null) return;

        _combatPowerDirty = new Subject<Unit>();
        _combatPowerDirty.Throttle(TimeSpan.FromSeconds(CombatPowerReportDelay))
                         .Subscribe(_ => ReportCombatPower())
                         .AddTo(_subs);

        _ctx.Store.DeckChanged += OnCombatPowerSourceChanged;
        _ctx.Store.CharactersChanged += OnCharactersChangedForCombatPower;

        // 진입 시 1회는 바로 보낸다(다른 길드원이 내 최신 전투력을 볼 수 있어야 한다).
        // 씬을 다시 밟아 스토어가 이미 채워져 있는 경우를 위한 것이고, 처음 진입이라 아직 비어 있으면
        // 아래 ReportCombatPower 가 스스로 걸러 내고 초기 로딩이 끝날 때 다시 보낸다.
        ReportCombatPower();
    }

    private void OnCombatPowerSourceChanged() => _combatPowerDirty?.OnNext(Unit.Default);
    private void OnCharactersChangedForCombatPower(CharacterChange _) => _combatPowerDirty?.OnNext(Unit.Default);

    /// <summary>
    /// 덱 전투력을 서버에 보낸다. 단, 0 은 보내지 않는다.
    ///
    /// 이 컴포넌트의 Start 는 메인 데이터가 도착하기 전에 돌기 때문에 그 시점의 계산 결과는 항상 0 이다.
    /// 그 0 을 보내면 서버의 계정 전투력이 0 으로 덮이고, 데이터가 다 도착해 실제 값이 다시 보내지기까지의
    /// 몇 초 사이에 랭킹이 집계되면 그 계정이 순위에서 통째로 빠진다(길드 전투력 합계도 그만큼 깎인 채 남는다).
    /// 그 사이에 앱이 꺼지면 0 이 그대로 굳는다.
    ///
    /// 편성된 덱의 전투력은 0 이 될 수 없으므로, 0 은 "전투력이 없다"가 아니라 "아직 모른다"는 뜻이다.
    /// 모르는 값을 서버에 알릴 이유가 없으므로 그냥 보내지 않는다(마지막으로 보고된 값이 그대로 유지된다).
    /// </summary>
    private void ReportCombatPower() {
        if (_ctx.Guild == null) return;

        long power = (long)AccountStats.TotalDeckCombatPower(_ctx);
        if (power <= 0) return;
        _ctx.Guild.ReportCombatPower(power);
    }

    /// <summary>전투력 보고를 묶는 시간(초). 성장 팝업에서 연타로 올릴 때 매번 보내지 않기 위한 간격이다.</summary>
    private const float CombatPowerReportDelay = 2f;

    private void OnDestroy() {
        _combatPower?.Dispose();
        // 결제 서비스는 씬보다 오래 사는 싱글턴이라, 이 씬이 걸어 둔 구독은 여기서 반드시 푼다.
        _iapRecovery?.Dispose();
        _subs.Dispose();

        if (_ctx != null && _ctx.Store != null) {
            _ctx.Store.DeckChanged -= OnCombatPowerSourceChanged;
            _ctx.Store.CharactersChanged -= OnCharactersChangedForCombatPower;
            _ctx.Store.InitialLoadCompleted -= OnInitialLoadCompleted;
        }

        if (_ctx != null && _ctx.Battle != null) {
            _ctx.Battle.RewardReceived -= OnRewardReceived;
            _ctx.Battle.StageFailed -= OnStageFailed;
        }
    }

    // 파티 전멸로 런이 실패했을 때의 안내. 내려간 던전 이름을 함께 알려 어디서 다시 도는지 바로 알 수 있게 한다.
    // 첫 던전에서 실패하면 내려갈 곳이 없어 같은 던전이 다시 오므로 문구를 나눈다.
    private void OnStageFailed(int failedIndex, int nextIndex) {
        var next = _ctx.DungeonDb != null ? _ctx.DungeonDb.Get(nextIndex) : null;
        string name = next != null ? next.DisplayName : $"#{nextIndex}";
        _ctx.Ui.ShowToast(nextIndex == failedIndex
            ? $"파티가 전멸했습니다. {name}에서 다시 시작합니다."
            : $"스테이지에 실패했습니다. {name}(으)로 돌아갑니다.");
    }

    // 던전 클리어 보상 -> 획득 표시. 다른 팝업을 가리지 않도록 Light 레이어(SortOrder 50)에 비차단으로 뜬다.
    private void OnRewardReceived(DungeonRewardDto dto) {
        ShowRewardAsync(dto, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid ShowRewardAsync(DungeonRewardDto dto, CancellationToken ct) {
        // 연속 클리어로 아직 이전 표시가 남아 있으면 즉시 닫고 새로 띄운다(PLANNING 3-6).
        if (_currentReward != null) {
            _currentReward.CloseAsync().Forget();
            _currentReward = null;
        }

        var popup = await _ctx.Ui.ShowPopupAsync<DungeonClearRewardPopup>(_ctx.Popups.DungeonClearRewardPopup, ct, UIPopupLayer.Light);
        _currentReward = popup;
        popup.Setup(_ctx, dto);

        // 페이드인 -> 유지 -> 페이드아웃 자동 닫힘. 닫히면(또는 교체되면) 현재 참조를 정리한다.
        await popup.PlayAsync(ct);
        if (_currentReward == popup) _currentReward = null;
    }
}
