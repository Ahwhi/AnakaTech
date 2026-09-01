using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 의뢰 전투 결과 팝업(직접 도전했을 때만 뜬다. 즉시해결은 전투가 없으므로 이 팝업을 쓰지 않는다).
///
/// 방치형이라 결과를 확인한 뒤 손을 대지 않아도 흐름이 이어지도록 자동 진행 카운트다운을 둔다:
///  - 성공: 카운트가 끝나면 다음 단계로 자동 도전(다음 단계가 없으면 나가기)
///  - 실패: 카운트가 끝나면 메인으로 나가기. 그전에 재도전 버튼으로 같은 단계를 다시 칠 수 있다.
/// 버튼을 누르면 카운트다운은 즉시 멈추고 그 선택이 반영된다.
/// </summary>
public sealed class CommissionResultPopup : UIView {
    /// <summary>결과 팝업 선택. 자동 진행도 같은 값으로 확정된다.</summary>
    public enum Choice { NextStage, Retry, Exit }

    [Header("Header")]
    [Tooltip("결과 제목(예: 의뢰 완료 / 의뢰 실패).")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("의뢰 이름 + 단계.")]
    [SerializeField] private TMP_Text subtitleText;
    [Tooltip("성공일 때만 켜지는 연출 루트(선택).")]
    [SerializeField] private GameObject successGroup;
    [Tooltip("실패일 때만 켜지는 연출 루트(선택).")]
    [SerializeField] private GameObject failGroup;

    [Header("Rewards")]
    [Tooltip("획득 보상 슬롯이 생성될 부모. 실패 시에는 비활성된다.")]
    [SerializeField] private Transform rewardParent;
    [SerializeField] private RewardSlotView rewardSlotPrefab;
    [Tooltip("보상이 하나도 없을 때 켜는 안내 오브젝트(선택).")]
    [SerializeField] private GameObject emptyRewardHint;

    [Header("Auto Progress")]
    [Tooltip("자동 진행까지의 대기 시간(초). 성공이면 다음 단계 자동 도전, 실패면 자동 나가기.")]
    [SerializeField, Min(0f)] private float autoDelaySeconds = 5f;
    [Tooltip("남은 시간 안내 텍스트(예: 5초 후 다음 단계 자동 도전).")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Buttons")]
    [Tooltip("다음 단계로 즉시 넘어가기(성공 전용).")]
    [SerializeField] private Button nextStageButton;
    [Tooltip("다음 단계 버튼 라벨(예: 다음 단계). 잠긴 사유를 표시할 때 바꿔 쓴다.")]
    [SerializeField] private TMP_Text nextStageLabel;
    [Tooltip("같은 단계 재도전(실패 전용). 도전은 무료라 의뢰해결권을 쓰지 않는다.")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button exitButton;

    private UniTaskCompletionSource<Choice> _result;
    private readonly List<RewardSlotView> _slots = new();

    // 카운트다운이 끝났을 때 자동으로 확정할 선택. 성공/실패와 다음 단계 유무로 결정된다.
    private Choice _autoChoice;
    private float _remaining;
    private bool _counting;

    protected override void Awake() {
        base.Awake();
        if (nextStageButton != null) nextStageButton.onClick.AddListener(() => Resolve(Choice.NextStage));
        if (retryButton != null) retryButton.onClick.AddListener(() => Resolve(Choice.Retry));
        if (exitButton != null) exitButton.onClick.AddListener(() => Resolve(Choice.Exit));
    }

    /// <param name="success">성공 여부.</param>
    /// <param name="commissionName">표시할 의뢰 이름(예: 레이드 의뢰 3단계).</param>
    /// <param name="rewards">획득 보상(실패면 비어 있다).</param>
    /// <param name="nextStageAvailable">다음 단계로 이어서 도전할 수 있는지(데이터 존재 여부).</param>
    /// <param name="nextStageBlockedReason">이어가기가 막힌 사유(버튼 라벨에 표시). 비우면 기본 라벨 유지.</param>
    public async UniTask<Choice> ShowAsync(BattleHudContext ctx, bool success, string commissionName,
                                           IReadOnlyList<RewardItem> rewards,
                                           bool nextStageAvailable, string nextStageBlockedReason = null) {
        if (titleText != null) titleText.text = success ? "의뢰 완료" : "의뢰 실패";
        if (subtitleText != null) subtitleText.text = commissionName;
        if (successGroup != null) successGroup.SetActive(success);
        if (failGroup != null) failGroup.SetActive(!success);

        BuildRewards(ctx, rewards);

        // 성공은 진도를 이어가는 게 기본, 실패는 나가는 게 기본이다.
        // 성공했는데 다음 단계가 없으면(마지막 단계 클리어) 이어갈 곳이 없으므로 나가기로 떨어진다.
        _autoChoice = success && nextStageAvailable ? Choice.NextStage : Choice.Exit;

        if (nextStageButton != null) {
            nextStageButton.gameObject.SetActive(success);
            nextStageButton.interactable = success && nextStageAvailable;
        }
        if (nextStageLabel != null && !string.IsNullOrEmpty(nextStageBlockedReason) && !nextStageAvailable)
            nextStageLabel.text = nextStageBlockedReason;

        // 재도전은 실패했을 때만 의미가 있다(성공한 단계를 다시 깨도 보상이 없다).
        if (retryButton != null) retryButton.gameObject.SetActive(!success);

        _result = new UniTaskCompletionSource<Choice>();
        await base.ShowAsync();

        // 등장 연출이 끝난 뒤부터 센다(연출 중에 시간이 깎여 나가면 실제로 보는 시간이 줄어든다).
        StartCountdown();
        return await _result.Task;
    }

    private void StartCountdown() {
        if (autoDelaySeconds <= 0f) return;   // 0 이하면 자동 진행 없이 버튼 입력만 기다린다.
        _remaining = autoDelaySeconds;
        _counting = true;
        RefreshCountdownText();
    }

    // 타임스케일에 영향받지 않도록 unscaled 로 센다(UIView 연출도 SetUpdate(true) 규약을 따른다).
    private void Update() {
        if (!_counting) return;

        _remaining -= Time.unscaledDeltaTime;
        RefreshCountdownText();

        if (_remaining > 0f) return;
        _counting = false;
        Resolve(_autoChoice);
    }

    private void RefreshCountdownText() {
        if (countdownText == null) return;

        int seconds = Mathf.Max(0, Mathf.CeilToInt(_remaining));
        countdownText.text = _autoChoice == Choice.NextStage
            ? $"{seconds}초 후 다음 단계 자동 도전"
            : $"{seconds}초 후 나가기";
    }

    // 보상 슬롯을 필요한 만큼만 만들어 재사용한다(다음 단계로 이어가면 같은 팝업이 다시 열리므로).
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

    // 버튼 외의 경로로 닫혀도 반드시 결과를 확정한다(닫히면 나가기로 간주).
    protected override void OnClosed() {
        _counting = false;
        _result?.TrySetResult(Choice.Exit);
    }
}
