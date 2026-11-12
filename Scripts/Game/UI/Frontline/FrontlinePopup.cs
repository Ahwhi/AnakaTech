using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 전선 입구(슬라이드 메뉴의 전선 버튼, V1.12). 전선 6곳을 카드(FrontlineSlotView)로 보여 주고,
/// 카드마다 배치(배치 팝업) / 참여(전선 씬) / 초기화를 고른다.
///
/// 현황은 FrontlineService.Info 를 구독해 그린다. 배치 팝업이 배치를 저장하거나 전선 씬에서 단계가 바뀌면
/// 같은 값이 갱신되므로, 이 팝업은 배치 팝업을 몰라도 함께 맞춰진다.
/// </summary>
public sealed class FrontlinePopup : UIView, IHudPopup {
    [Header("Frontlines")]
    [Tooltip("전선 카드 6장(가로 3 x 세로 2). 배열 순서가 곧 전선 id 다(0 = 감시 초소 ... 5 = 해안선).")]
    [SerializeField] private FrontlineSlotView[] slots;

    [Header("Stamina")]
    [Tooltip("오늘 전선에 더 쓸 수 있는 기력. 숫자만 쓰므로 라벨은 따로 둔다(예: 700 / 1,000).")]
    [SerializeField] private TMP_Text dailyRemainText;
    [Tooltip("선택. 상단 재화 패널(기력).")]
    [SerializeField] private CurrencyPanel currencyPanel;
    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private readonly CompositeDisposable _subscriptions = new();
    private bool _busy;
    private bool _wired;

    private FrontlineInfoDto Info => _ctx?.Frontline.Info.Value;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (currencyPanel != null) currencyPanel.Initialize(ctx);

        if (!_wired) {
            _wired = true;
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            if (slots != null)
                for (int i = 0; i < slots.Length; i++) {
                    if (slots[i] == null) continue;
                    slots[i].Initialize(i);
                    slots[i].DeployRequested += OnDeployRequested;
                    slots[i].JoinRequested += OnJoinRequested;
                    slots[i].ResetRequested += OnResetRequested;
                }
        }

        _subscriptions.Clear();
        _ctx.Frontline.Info.Subscribe(_ => Refresh()).AddTo(_subscriptions);
        // 기력이 차거나 줄면 참여 버튼의 빨간 글씨/활성 상태가 따라 바뀌어야 한다.
        _ctx.Store.Stamina.Subscribe(_ => Refresh()).AddTo(_subscriptions);
        _ctx.Store.CharactersChanged -= OnCharactersChanged;
        _ctx.Store.CharactersChanged += OnCharactersChanged;

        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        _busy = true;
        Refresh();
        try {
            await _ctx.Frontline.RequestInfoAsync(_ct);
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("전선 정보를 불러오지 못했습니다.");
        } finally {
            _busy = false;
        }
        Refresh();
    }

    // 성장/장비가 바뀌면 스쿼드 전투력과 소녀 레벨 표시가 달라진다.
    private void OnCharactersChanged(CharacterChange change) => Refresh();

    private void Refresh() {
        if (_ctx == null) return;
        var info = Info;

        if (dailyRemainText != null)
            dailyRemainText.text = info != null
                ? FrontlineRules.DailyRemainText(info.DailyStaminaRemaining, info.DailyStaminaLimit)
                : "";

        if (slots == null) return;
        foreach (var slot in slots)
            if (slot != null) slot.Bind(_ctx, _ctx.FrontlineDb, info, _busy);
    }

    // ── 배치 ──────────────────────────────────────────
    private void OnDeployRequested(int frontlineId) {
        if (_busy) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenDeployAsync(frontlineId).Forget();
    }

    private async UniTaskVoid OpenDeployAsync(int frontlineId) {
        var reference = _ctx.Popups.FrontlineDeployPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) {
            Debug.LogError("[FrontlinePopup] UIPopupCatalog 에 전선 배치 팝업이 할당되지 않았습니다.");
            return;
        }
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<FrontlineDeployPopup>(reference, _ct);
            popup.SelectTab(frontlineId);   // Initialize 전에 지정해야 그 전선으로 열린다.
            popup.Initialize(_ctx);
            await popup.ShowAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // ── 참여 ──────────────────────────────────────────
    private void OnJoinRequested(int frontlineId) {
        if (_busy) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);

        var info = Info;
        var squad = info?.Squad(frontlineId);
        if (squad == null || squad.Members.Count == 0) { _ctx.Ui.ShowToast("배치된 소녀가 없습니다."); return; }
        if (info.DailyStaminaRemaining < info.StaminaPerClear) { _ctx.Ui.ShowToast(FrontlineRules.EnterFailMessage(6)); return; }
        if (_ctx.Store.Stamina.Value < info.StaminaPerClear) { _ctx.Ui.ShowToast(FrontlineRules.EnterFailMessage(5)); return; }

        if (_ctx.Scenes == null || _ctx.Scenes.FrontlineScene == null || !_ctx.Scenes.FrontlineScene.RuntimeKeyIsValid()) {
            Debug.LogError("[FrontlinePopup] SceneCatalog 에 전선 씬이 할당되지 않았습니다.");
            _ctx.Ui.ShowToast("전선 씬을 열 수 없습니다.");
            return;
        }

        FrontlineSession.SetEntry(frontlineId);

        // CloseAllPopups 는 이 팝업 자신도 파괴하므로, 그 뒤에 쓸 것들을 먼저 지역 변수로 확보한다.
        var ui = _ctx.Ui;
        var sceneService = _ctx.SceneService;
        var sceneKey = _ctx.Scenes.FrontlineScene.RuntimeKey;

        // 돌아왔을 때 이 팝업이 다시 열리도록 예약한 뒤 닫는다.
        MainPopupReturn.Request(_ctx.Popups.FrontlinePopup);
        ui.CloseAllPopups();
        sceneService.LoadAddressableAsync(sceneKey).Forget();
    }

    // ── 초기화 ────────────────────────────────────────
    private void OnResetRequested(int frontlineId) {
        if (_busy) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        ResetAsync(frontlineId).Forget();
    }

    private async UniTaskVoid ResetAsync(int frontlineId) {
        try {
            var confirm = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(_ctx.Popups.ConfirmCancelPopup, _ct);
            string name = _ctx.FrontlineDb != null ? _ctx.FrontlineDb.NameOf(frontlineId) : FrontlineRules.DefaultName(frontlineId);
            bool ok = await confirm.ShowAsync("전선 초기화",
                $"{name} 전선을 초기화하시겠습니까?\n레벨이 1로 돌아가고, 배치된 소녀가 모두 해제됩니다.");
            if (!ok) return;

            _busy = true;
            Refresh();
            var res = await _ctx.Frontline.ResetAsync(frontlineId, _ct);
            if (!res.IsSuccess) _ctx.Ui.ShowToast("전선을 초기화하지 못했습니다.");
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("전선을 초기화하지 못했습니다.");
        } finally {
            _busy = false;
        }
        Refresh();
    }

    protected override void OnClosed() {
        _subscriptions.Clear();
        if (_ctx != null) _ctx.Store.CharactersChanged -= OnCharactersChanged;
    }

    private void OnDestroy() {
        _subscriptions.Dispose();
        if (_ctx != null) _ctx.Store.CharactersChanged -= OnCharactersChanged;
    }
}
