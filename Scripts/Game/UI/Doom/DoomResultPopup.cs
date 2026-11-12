using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 멸망전 결과. 그 판의 점수와 역대 최고 점수를 보여 주고, 다시 도전할지 나갈지를 await 로 돌려준다.
///
/// 점수 보상은 여기서 주지 않는다. 멸망전의 점수 보상은 "한 판의 결과"가 아니라 "역대 최고 점수"에 걸려 있어
/// 입구(DoomPopup)의 보상 트랙에서 한 번에 받는다. 한 판마다 바로 들어오는 것은 보유 소녀 전원의 경험치뿐이라
/// 그것만 보상 칸 하나로 보여 준다.
/// </summary>
public sealed class DoomResultPopup : UIView {
    /// <summary>결과 화면의 선택.</summary>
    public enum Choice { Exit, Retry }

    [Header("Score")]
    [Tooltip("이번 판의 점수(처치한 누적 마리 수).")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("도달한 페이즈.")]
    [SerializeField] private TMP_Text phaseText;
    [Tooltip("역대 최고 점수.")]
    [SerializeField] private TMP_Text bestScoreText;
    [Tooltip("최고 기록을 갈아치웠을 때만 켜는 오브젝트(NEW RECORD 등). 선택.")]
    [SerializeField] private GameObject newRecordGroup;

    [Header("Message")]
    [Tooltip("상단 문구. 전멸/시간초과와 전 페이즈 클리어를 구분해 알린다.")]
    [SerializeField] private TMP_Text messageText;
    [Tooltip("받을 수 있는 보상이 생겼을 때만 켜는 안내. 선택.")]
    [SerializeField] private GameObject claimableHint;

    [Header("Exp")]
    [Tooltip("보유 소녀 1명이 얻은 경험치 칸. 얻은 경험치가 없으면 숨긴다.")]
    [SerializeField] private RewardSlotView expSlot;
    [Tooltip("선택. 경험치 칸을 감싸는 묶음(라벨 포함). 얻은 경험치가 없으면 함께 숨긴다.")]
    [SerializeField] private GameObject expGroup;

    [Header("Buttons")]
    [Tooltip("재도전. 유료 참여면 확인을 받은 뒤에 결과를 돌려준다.")]
    [SerializeField] private Button retryButton;
    [Tooltip("선택. 재도전 버튼의 비용 표시(무료 라벨 / 다이아 아이콘 + 비용 묶음).")]
    [SerializeField] private EntryCostView retryCost;
    [SerializeField] private Button exitButton;

    private UniTaskCompletionSource<Choice> _result;
    private BattleHudContext _ctx;
    private DoomEntryDto _entry;
    private bool _confirming;

    /// <param name="result">판의 결말(전 페이즈 클리어인지 패배인지).</param>
    /// <param name="score">이번 판의 점수.</param>
    /// <param name="phase">도달한 페이즈 번호.</param>
    /// <param name="bestScore">서버가 확정한 역대 최고 점수.</param>
    /// <param name="isBest">이번 판이 최고 기록을 갈아치웠는지.</param>
    /// <param name="canClaim">지금 받을 수 있는 보상이 있는지.</param>
    /// <param name="gainedExp">보유 소녀 1명이 얻은 경험치(0 이면 칸을 숨긴다).</param>
    /// <param name="entry">오늘 참여 현황(다음 참여 비용). 모르면 null 이며 그때는 재도전을 잠근다.</param>
    public async UniTask<Choice> ShowAsync(BattleHudContext ctx, DoomResult result, int score, int phase,
                                           int bestScore, bool isBest, bool canClaim,
                                           int gainedExp, DoomEntryDto entry) {
        _ctx = ctx;
        _entry = entry;
        _confirming = false;

        if (scoreText != null) scoreText.text = score.ToString("N0");
        if (phaseText != null) phaseText.text = $"{phase} 페이즈";
        if (bestScoreText != null) bestScoreText.text = bestScore.ToString("N0");
        if (newRecordGroup != null) newRecordGroup.SetActive(isBest);
        if (claimableHint != null) claimableHint.SetActive(canClaim);

        if (messageText != null)
            messageText.text = result == DoomResult.AllCleared
                ? "모든 페이즈를 돌파했습니다.\n다음 업데이트를 기다려주세요."
                : "소녀들이 모두 쓰러졌습니다.";

        bool hasExp = gainedExp > 0;
        if (expGroup != null) expGroup.SetActive(hasExp);
        if (expSlot != null) {
            expSlot.gameObject.SetActive(hasExp);
            if (hasExp) expSlot.Bind(RewardVisualResolver.Resolve(ctx, RewardItem.Exp(gainedExp)));
        }
        // 더 칠 페이즈가 없거나 참여 현황을 모르면 재도전을 잠근다. 하루 제한은 없으므로 횟수로는 잠그지 않는다.
        bool canRetry = result != DoomResult.AllCleared && entry != null;
        if (retryButton != null) retryButton.interactable = canRetry;
        if (retryCost != null) {
            if (canRetry) retryCost.Bind(ctx.Currencies, CurrencyType.Diamond, entry.NextEntryCost, CanAfford());
            else retryCost.Clear();
        }

        _result = new UniTaskCompletionSource<Choice>();
        await base.ShowAsync();
        return await _result.Task;
    }

    protected override void Awake() {
        base.Awake();
        if (retryButton != null) retryButton.onClick.AddListener(() => RetryAsync().Forget());
        if (exitButton != null) exitButton.onClick.AddListener(() => Resolve(Choice.Exit));
    }

    private bool CanAfford() =>
        _entry != null && (_entry.IsFree || (_ctx != null && _ctx.Store.Diamond.Value >= _entry.NextEntryCost));

    // 무료면 바로 재도전, 유료면 잔액과 동의를 확인한 뒤에 재도전을 돌려준다(서버도 잔액을 다시 검사한다).
    private async UniTaskVoid RetryAsync() {
        if (_confirming || _entry == null) return;
        if (_entry.IsFree) { Resolve(Choice.Retry); return; }

        if (!CanAfford()) { _ctx.Ui.ShowToast(DoomEntryRules.NotEnoughDiamondMessage); return; }

        _confirming = true;
        try {
            var confirm = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(_ctx.Popups.ConfirmCancelPopup,
                                                                           this.GetCancellationTokenOnDestroy());
            if (await confirm.ShowAsync(DoomEntryRules.PaidEntryTitle,
                                        DoomEntryRules.PaidEntryConfirmMessage(_entry.NextEntryCost)))
                Resolve(Choice.Retry);
        } catch (System.OperationCanceledException) {
        } finally {
            _confirming = false;
        }
    }

    private void Resolve(Choice value) {
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }

    // 버튼 외의 경로로 닫혀도 반드시 결과를 확정한다. 확정하지 않으면 await 중인 씬 매니저가 멈춘다.
    protected override void OnClosed() => _result?.TrySetResult(Choice.Exit);
}
