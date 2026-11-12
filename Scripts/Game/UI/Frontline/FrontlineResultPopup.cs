using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전선 결과 팝업. 두 가지 경우에 같은 팝업을 쓴다(의뢰 결과 팝업처럼 카운트다운이 끝나면 저절로 진행된다).
///
///   판 결과   한 판이 끝날 때마다 뜬다(클리어/실패). 카운트다운이 끝나면 다음 판으로 이어 가고,
///             나가기 버튼으로 중간에 끊을 수 있다. 계속 버튼은 카운트다운을 건너뛴다.
///   종료      기력이나 오늘 한도가 모자라 더 칠 수 없을 때 이번 입장의 누적 결과를 보여 주고,
///             카운트다운이 끝나면 나간다.
/// 버튼을 누르면 카운트다운은 즉시 멈추고 그 선택이 반영된다. 버튼 외의 경로로 닫히면 나가기로 본다.
/// </summary>
public sealed class FrontlineResultPopup : UIView {
    /// <summary>팝업 선택. 자동 진행도 같은 값으로 확정된다.</summary>
    public enum Choice { Continue, Exit }

    [Header("Header")]
    [Tooltip("전선 이름 + 결과(예: 감시 초소 결과).")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("한 줄 결과(예: Lv.3 클리어 / Lv.3 실패 / 기력이 부족합니다.). 클리어는 초록, 그 밖은 빨강으로 칠한다.")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Color successColor = new(0.30f, 0.85f, 0.40f);
    [SerializeField] private Color failColor = new(0.95f, 0.25f, 0.25f);

    [Header("Rewards")]
    [Tooltip("획득 경험치(소녀 1명 기준) 칸이 생성될 부모.")]
    [SerializeField] private Transform rewardParent;
    [SerializeField] private RewardSlotView rewardSlotPrefab;
    [Tooltip("선택. 받은 보상이 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyRewardHint;

    [Header("Auto Progress")]
    [Tooltip("판 결과에서 다음 판으로 저절로 넘어가기까지의 대기 시간(초).")]
    [SerializeField, Min(0f)] private float autoContinueSeconds = 3f;
    [Tooltip("종료에서 저절로 나가기까지의 대기 시간(초).")]
    [SerializeField, Min(0f)] private float autoExitSeconds = 5f;
    [Tooltip("남은 시간 안내(예: 3초 후 다음 레벨 진행 / 3초 후 이전 레벨 진행 / 5초 후 나가기).")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Buttons")]
    [Tooltip("다음 판으로 바로 넘어가기(판 결과 전용).")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;

    private UniTaskCompletionSource<Choice> _result;
    private readonly List<RewardSlotView> _slots = new();
    private Choice _autoChoice;
    private string _continueTarget;
    private float _remaining;
    private bool _counting;
    private int _shownSeconds = -1;

    protected override void Awake() {
        base.Awake();
        if (continueButton != null) continueButton.onClick.AddListener(() => Resolve(Choice.Continue));
        if (exitButton != null) exitButton.onClick.AddListener(() => Resolve(Choice.Exit));
    }

    /// <summary>
    /// 한 판의 결과. 카운트다운이 끝나면 서버가 정한 다음 단계로 이어 간다.
    ///   클리어   다음 레벨(최고 단계면 같은 레벨)
    ///   실패     이전 레벨. 1레벨에서 실패하면 더 내려갈 곳이 없으므로 나간다.
    /// </summary>
    /// <param name="cleared">클리어 여부. 실패면 보상이 비어 있다.</param>
    public UniTask<Choice> ShowRunAsync(BattleHudContext ctx, string frontlineName, int stage, bool cleared,
                                        IReadOnlyList<RewardItem> rewards) {
        string message = $"{FrontlineRules.StageText(stage)} {(cleared ? "클리어" : "실패")}";
        if (!cleared && stage <= FrontlineRules.StartStage)
            return ShowCoreAsync(ctx, frontlineName, message, false, rewards, Choice.Exit, autoExitSeconds, null);

        string next = !cleared ? "이전 레벨" : stage >= FrontlineRules.MaxStage ? "같은 레벨" : "다음 레벨";
        return ShowCoreAsync(ctx, frontlineName, message, cleared, rewards, Choice.Continue, autoContinueSeconds, next);
    }

    /// <summary>더 칠 수 없어 끝날 때. 멈춘 이유와 이번 입장의 누적 보상을 보여 주고 카운트다운이 끝나면 나간다.</summary>
    public UniTask<Choice> ShowEndAsync(BattleHudContext ctx, string frontlineName, string reason,
                                        IReadOnlyList<RewardItem> rewards) =>
        ShowCoreAsync(ctx, frontlineName, reason, false, rewards, Choice.Exit, autoExitSeconds, null);

    /// <param name="continueTarget">이어 갈 곳(예: 다음 레벨). 나가기로 끝나는 경우는 null.</param>
    private async UniTask<Choice> ShowCoreAsync(BattleHudContext ctx, string frontlineName, string message, bool success,
                                                IReadOnlyList<RewardItem> rewards, Choice autoChoice, float autoSeconds,
                                                string continueTarget) {
        _continueTarget = continueTarget;
        if (titleText != null) titleText.text = $"{frontlineName} 결과";
        if (messageText != null) {
            messageText.text = message;
            messageText.color = success ? successColor : failColor;
        }
        if (continueButton != null) continueButton.gameObject.SetActive(autoChoice == Choice.Continue);
        BuildRewards(ctx, rewards);

        _autoChoice = autoChoice;
        _counting = false;
        _result = new UniTaskCompletionSource<Choice>();
        await base.ShowAsync();

        // 등장 연출이 끝난 뒤부터 센다(연출 중에 시간이 깎이면 실제로 보는 시간이 줄어든다).
        if (autoSeconds > 0f) {
            _remaining = autoSeconds;
            _shownSeconds = -1;
            _counting = true;
            RefreshCountdown();
        }
        return await _result.Task;
    }

    // 타임스케일에 영향받지 않도록 unscaled 로 센다.
    private void Update() {
        if (!_counting) return;
        _remaining -= Time.unscaledDeltaTime;
        RefreshCountdown();
        if (_remaining > 0f) return;
        Resolve(_autoChoice);
    }

    private void RefreshCountdown() {
        if (countdownText == null) return;
        int seconds = Mathf.Max(0, Mathf.CeilToInt(_remaining));
        if (seconds == _shownSeconds) return;
        _shownSeconds = seconds;
        countdownText.text = _autoChoice == Choice.Continue ? $"{seconds}초 후 {_continueTarget} 진행" : $"{seconds}초 후 나가기";
    }

    // 보상 칸을 필요한 만큼만 만들어 재사용한다(판마다 같은 팝업이 다시 열린다).
    private void BuildRewards(BattleHudContext ctx, IReadOnlyList<RewardItem> rewards) {
        int count = rewards != null ? rewards.Count : 0;
        if (rewardParent != null) rewardParent.gameObject.SetActive(count > 0);
        if (emptyRewardHint != null) emptyRewardHint.SetActive(count == 0);
        if (rewardSlotPrefab == null || rewardParent == null) return;

        while (_slots.Count < count) _slots.Add(Instantiate(rewardSlotPrefab, rewardParent));
        for (int i = 0; i < _slots.Count; i++) {
            bool used = i < count;
            _slots[i].gameObject.SetActive(used);
            if (used) _slots[i].Bind(RewardVisualResolver.Resolve(ctx, rewards[i]));
        }
    }

    private void Resolve(Choice value) {
        _counting = false;   // 버튼 입력이 카운트다운보다 우선한다.
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }

    // 버튼 외의 경로(배경 탭/뒤로가기)로 닫혀도 반드시 결과를 확정한다(나가기로 본다).
    protected override void OnClosed() {
        _counting = false;
        _result?.TrySetResult(Choice.Exit);
    }
}
