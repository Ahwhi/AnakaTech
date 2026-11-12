using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 영웅전 결과. 클리어하면 받은 보상을 보여 주고, 이어서 칠지(클리어: 다음 단계 / 실패: 재도전) 나갈지를 await 로 돌려준다.
/// 이어서 치는 것도 도전 횟수를 1회 쓰므로, 남은 횟수가 없거나 끝까지 깼으면 나가기만 남는다.
/// </summary>
public sealed class HeroesResultPopup : UIView {
    public enum Choice { Exit, Continue }

    [Header("Result")]
    [Tooltip("도전한 단계(예: 3단계).")]
    [SerializeField] private TMP_Text stageText;
    [Tooltip("결과 문구 한 줄. 클리어면 성공색, 실패면 실패색.")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Color successColor = new(0.35f, 0.9f, 0.4f);
    [SerializeField] private Color failColor = new(0.9f, 0.25f, 0.25f);
    [Tooltip("선택. 오늘 남은 도전 횟수(n / 3). 라벨은 프리팹에서 따로 둔다.")]
    [SerializeField] private TMP_Text entryCountText;
    [Tooltip("선택. 마지막 단계까지 깼을 때만 켜지는 안내 문구(다음 업데이트를 기다려 달라는 내용).")]
    [SerializeField] private TMP_Text maxStageText;

    [Header("Rewards")]
    [Tooltip("받은 보상 칸이 생성될 부모.")]
    [SerializeField] private Transform rewardContent;
    [SerializeField] private RewardSlotView rewardSlotPrefab;
    [Tooltip("선택. 받은 보상이 없을 때만 켜지는 문구.")]
    [SerializeField] private TMP_Text noRewardText;

    [Header("Buttons")]
    [Tooltip("이어서 치기(클리어: 다음 단계 도전 / 실패: 재도전). 남은 횟수가 없으면 숨는다.")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonText;
    [SerializeField] private Button exitButton;

    private readonly List<RewardSlotView> _slots = new();
    private UniTaskCompletionSource<Choice> _result;

    /// <param name="stage">방금 친 단계.</param>
    /// <param name="nextStage">다음에 칠 단계(클리어면 stage + 1, 실패면 stage).</param>
    /// <param name="remainEntries">오늘 남은 도전 횟수.</param>
    /// <param name="granted">클리어로 받은 보상(실패면 비어 있다).</param>
    public async UniTask<Choice> ShowAsync(BattleHudContext ctx, HeroesResult result, int stage, int nextStage,
                                           int remainEntries, IReadOnlyList<RewardItem> granted) {
        bool cleared = result == HeroesResult.Cleared;
        bool maxReached = nextStage > HeroesRules.MaxStage;

        if (stageText != null) stageText.text = HeroesRules.StageText(stage);
        if (messageText != null) {
            messageText.text = HeroesRules.ResultMessage(result);
            messageText.color = cleared ? successColor : failColor;
        }
        if (entryCountText != null) entryCountText.text = HeroesRules.EntryCountText(remainEntries);
        if (maxStageText != null) {
            maxStageText.gameObject.SetActive(maxReached);
            maxStageText.text = HeroesRules.MaxStageMessage;
        }

        BindRewards(ctx, granted);

        bool canContinue = !maxReached && remainEntries > 0;
        if (continueButton != null) continueButton.gameObject.SetActive(canContinue);
        if (continueButtonText != null) continueButtonText.text = cleared ? "다음 단계 도전" : "재도전";

        _result = new UniTaskCompletionSource<Choice>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void BindRewards(BattleHudContext ctx, IReadOnlyList<RewardItem> granted) {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();

        bool has = granted != null && granted.Count > 0;
        if (noRewardText != null) noRewardText.gameObject.SetActive(!has);
        if (!has || rewardContent == null || rewardSlotPrefab == null) return;

        foreach (var item in granted) {
            var slot = Instantiate(rewardSlotPrefab, rewardContent);
            slot.gameObject.SetActive(true);
            slot.Bind(RewardVisualResolver.Resolve(ctx, item));
            _slots.Add(slot);
        }
    }

    protected override void Awake() {
        base.Awake();
        if (continueButton != null) continueButton.onClick.AddListener(() => Resolve(Choice.Continue));
        if (exitButton != null) exitButton.onClick.AddListener(() => Resolve(Choice.Exit));
    }

    private void Resolve(Choice value) {
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }

    // 버튼 외의 경로로 닫혀도 반드시 결과를 확정한다. 확정하지 않으면 await 중인 씬 매니저가 멈춘다.
    protected override void OnClosed() => _result?.TrySetResult(Choice.Exit);
}
