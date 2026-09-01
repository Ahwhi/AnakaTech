using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 의뢰 팝업(메인 HUD 햄버거 메뉴 -> 의뢰 버튼). 목록에서 의뢰를 고르면 아래 공용 상세 패널이 그 의뢰의
/// 정보/보상을 보여주고, 실행 버튼도 상세 패널의 것 한 벌만 쓴다(선택과 실행의 분리).
///
///  - 의뢰도전 : 무료다(의뢰해결권 소모 없음). 서버에 도전 가능 여부만 확인하고 의뢰 씬으로 전환한다.
///               보상은 그 단계를 처음 깼을 때만 나오므로 무료여도 반복 파밍이 되지 않는다.
///  - 즉시해결 : 의뢰해결권을 써서 "클리어한 최고 단계"의 보상을 장수만큼 받는다(씬 전환 없음). 반복 수급 담당.
///
/// 진행도/입장권은 팝업을 열 때 서버에서 받아 스토어에 반영하고, 갱신될 때마다 목록과 상세를 다시 그린다.
/// </summary>
public sealed class CommissionPopup : UIView, IHudPopup {
    [Header("List")]
    [Tooltip("의뢰 줄이 배치될 부모(Vertical Layout Group). 자식으로 미리 배치한 CommissionEntryView 들을 쓴다. " +
             "배치 순서가 곧 의뢰 종류 순서다(레이드/토벌/호위).")]
    [SerializeField] private CommissionEntryView[] entries;
    [Tooltip("목록을 채우는 동안 표시할 로딩 그룹(선택).")]
    [SerializeField] private GameObject loadingGroup;

    [Header("Detail")]
    [Tooltip("선택한 의뢰의 상세 + 공용 실행 버튼. 목록 줄에는 버튼을 두지 않는다.")]
    [SerializeField] private CommissionDetailPanel detailPanel;

    [Header("Currency")]
    [Tooltip("상단 재화 패널. 의뢰해결권 CurrencyView 를 자식으로 배치한다.")]
    [SerializeField] private CurrencyPanel currencyPanel;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private bool _busy;

    // 종류별 도전 단계 캐시. 목록/상세를 다시 그릴 때마다 재계산하지 않고 한 번만 정리해 둔다.
    private readonly Dictionary<CommissionType, int> _challengeStage = new();

    // 현재 선택. 팝업을 열면 첫 줄(레이드)이 선택된 상태로 시작하고, 갱신 후에도 선택이 유지된다.
    private CommissionType _selectedType;
    private bool _hasSelection;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (currencyPanel != null) currencyPanel.Initialize(ctx);
        if (closeButton != null)
            closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });

        if (detailPanel != null) {
            detailPanel.ChallengeRequested += OnChallenge;
            detailPanel.SweepRequested += OnSweep;
        }

        _ctx.Store.CommissionChanged -= Rebuild;
        _ctx.Store.CommissionChanged += Rebuild;

        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        if (loadingGroup != null) loadingGroup.SetActive(true);
        try {
            var info = await _ctx.Commission.RequestInfoAsync(_ct);
            _ctx.Store.ApplyCommissionInfo(info);   // ApplyCommissionInfo 가 CommissionChanged 를 내며 Rebuild 를 부른다.
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("의뢰 정보를 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
    }

    // 스토어의 최신 진행도/입장권으로 목록과 상세를 다시 그린다.
    private void Rebuild() {
        if (entries == null || _ctx == null) return;

        for (int i = 0; i < entries.Length; i++) {
            var entry = entries[i];
            if (entry == null) continue;

            // 배치 순서를 그대로 종류 인덱스로 쓴다(레이드/토벌/호위 순).
            var type = (CommissionType)i;
            int cleared = _ctx.Store.ClearedStageOf(type);
            int challenge = CommissionRewardRules.NextStage(cleared);
            _challengeStage[type] = challenge;

            // 도전할 단계 데이터가 있어야 도전 버튼이 열린다(마지막 단계까지 깬 경우 등).
            bool hasNextStage = _ctx.CommissionDb != null && _ctx.CommissionDb.Get(type, challenge) != null;
            entry.Bind(type, cleared, challenge, hasNextStage, OnEntrySelected);
        }

        // 최초 진입이면 첫 줄(레이드)을 자동 선택한다. 이미 고른 게 있으면 그 선택을 유지한다.
        if (!_hasSelection) {
            var first = FirstEntry();
            if (first != null) {
                _selectedType = first.Type;
                _hasSelection = true;
            }
        }
        if (_hasSelection) ApplySelection(_selectedType);
    }

    private CommissionEntryView FirstEntry() {
        for (int i = 0; i < entries.Length; i++)
            if (entries[i] != null) return entries[i];
        return null;
    }

    private void OnEntrySelected(CommissionEntryView entry) {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        _selectedType = entry.Type;
        _hasSelection = true;
        ApplySelection(_selectedType);
    }

    // 선택 표시(목록)와 상세 내용을 현재 선택 기준으로 맞춘다.
    private void ApplySelection(CommissionType type) {
        for (int i = 0; i < entries.Length; i++)
            if (entries[i] != null) entries[i].SetSelected(entries[i].Type == type);

        if (detailPanel == null) return;

        int cleared = _ctx.Store.ClearedStageOf(type);
        int challenge = _challengeStage.TryGetValue(type, out int s) ? s : CommissionRewardRules.NextStage(cleared);
        bool hasData = _ctx.CommissionDb != null && _ctx.CommissionDb.Get(type, challenge) != null;

        detailPanel.Bind(_ctx, type, cleared, challenge, hasData);
    }

    // ── 의뢰도전: 도전 가능 확인(무료) -> 의뢰 씬 전환 ──
    private void OnChallenge(CommissionType type) {
        if (_busy) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        ChallengeAsync(type).Forget();
    }

    private async UniTaskVoid ChallengeAsync(CommissionType type) {
        if (!_challengeStage.TryGetValue(type, out int stage)) return;

        _busy = true;
        try {
            var res = await _ctx.Commission.StartAsync(type, stage, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(CommissionRewardRules.FailMessage(res.FailReason));
                return;
            }
            // 도전은 입장권을 쓰지 않지만, 응답에 담겨 온 최신 보유량(지연 충전 반영)으로 표시를 맞춰 둔다.
            _ctx.Store.SetCommissionTickets(res.RemainingTickets);

            if (_ctx.Scenes == null || _ctx.Scenes.CommissionScene == null || !_ctx.Scenes.CommissionScene.RuntimeKeyIsValid()) {
                Debug.LogError("[CommissionPopup] SceneCatalog 에 의뢰 씬이 할당되지 않았습니다.");
                _ctx.Ui.ShowToast("의뢰 씬을 열 수 없습니다.");
                return;
            }

            // CloseAllPopups 는 이 팝업 자신도 파괴하므로, 그 뒤에 쓸 것들을 먼저 지역 변수로 확보한다.
            var ui = _ctx.Ui;
            var sceneService = _ctx.SceneService;
            var sceneKey = _ctx.Scenes.CommissionScene.RuntimeKey;

            // 씬 경계를 넘는 인자는 컨테이너로 전달할 수 없으므로 세션에 담아 넘긴다.
            CommissionSession.Request(type, stage);

            // UIRoot 는 부트 오브젝트 밑에 있어 씬 전환에도 살아남는다. 메인 씬에서 열려 있던 팝업을
            // 명시적으로 닫지 않으면 의뢰 씬 위에 그대로 떠 있게 되므로 여기서 전부 정리한다.
            ui.CloseAllPopups();

            // 전환 중 이 팝업은 이미 파괴된 상태이므로 await 하지 않고 던진다(매뉴얼 6-3).
            sceneService.LoadAddressableAsync(sceneKey).Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("의뢰에 진입하지 못했습니다.");
        } finally {
            _busy = false;
        }
    }

    // ── 즉시해결: 사용 장수 선택 -> 서버 지급 ──
    private void OnSweep(CommissionType type) {
        if (_busy) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        SweepAsync(type).Forget();
    }

    private async UniTaskVoid SweepAsync(CommissionType type) {
        int cleared = _ctx.Store.ClearedStageOf(type);
        if (cleared < 1) { _ctx.Ui.ShowToast("한 번도 클리어하지 않은 의뢰입니다."); return; }

        _busy = true;
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<CommissionSweepPopup>(_ctx.Popups.CommissionSweepPopup, _ct);
            int count = await popup.ShowAsync(_ctx, type, cleared, _ctx.Store.CommissionTicket.Value);
            if (count <= 0) return;

            var res = await _ctx.Commission.SweepAsync(type, count, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(CommissionRewardRules.FailMessage(res.FailReason));
                return;
            }

            // 재화/경험치 반영 + CommissionChanged 로 목록/상세가 갱신되므로, 늘어난 재화는 상단 재화 패널에서 바로 보인다.
            // 즉시해결은 전투가 없어 "결과"라 할 것이 없으므로 결과 팝업을 띄우지 않는다(전투 결과 전용).
            _ctx.Store.ApplyCommissionReward(res);
            _ctx.Sound.PlaySfx(SfxKey.Claim);
            _ctx.Ui.ShowToast($"{CommissionRewardRules.TypeName(type)} {cleared}단계를 {count}회 완료했습니다.");
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("즉시해결에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    protected override void OnClosed() => Unsubscribe();

    // OnClosed 를 거치지 않고 파괴되는 경우(연결 끊김 일괄 정리 등)의 이벤트 누수 방지.
    private void OnDestroy() => Unsubscribe();

    private void Unsubscribe() {
        if (_ctx != null) _ctx.Store.CommissionChanged -= Rebuild;
        if (detailPanel != null) {
            detailPanel.ChallengeRequested -= OnChallenge;
            detailPanel.SweepRequested -= OnSweep;
        }
    }
}
