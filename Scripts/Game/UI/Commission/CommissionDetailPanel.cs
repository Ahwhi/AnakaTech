using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 목록에서 고른 의뢰 하나의 상세 정보 + 공용 실행 버튼. 팝업에 하나만 두고 선택이 바뀔 때마다 다시 채운다.
///
/// 버튼을 목록 줄이 아니라 여기 한 벌만 두는 이유: 선택과 실행을 분리해야 "무엇을 고른 상태인지"가 항상 명확하고,
/// 의뢰 종류가 늘어나도 화면 구조가 그대로 유지된다.
///
/// 보상 표시는 버튼과 1:1로 나눈다. 두 경로는 서로 다른 행동이고 보상표도 완전히 독립이기 때문이다:
///  - 의뢰도전   : 도전 단계(= 클리어한 단계 + 1)를 처음 깼을 때 1회 받는 보상
///  - 즉시해결   : 클리어한 최고 단계 기준으로 의뢰해결권 1장당 받는 보상
/// 대상 단계도 한 칸 다르고 품목도 다르므로 반드시 따로 그린다.
/// 통신은 하지 않고 클릭만 이벤트로 올려 보낸다(실제 처리는 CommissionPopup).
/// </summary>
public sealed class CommissionDetailPanel : MonoBehaviour {
    [Header("Header")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("한 줄 소개(무엇을 검증하는 컨텐츠인지).")]
    [SerializeField] private TMP_Text summaryText;
    [Tooltip("진행 상태 한 줄(예: 3단계 도전). 목록 줄과 같은 규칙으로 표시된다.")]
    [SerializeField] private TMP_Text stageText;

    [Header("Challenge Rewards (도전 단계 최초 클리어 1회)")]
    [Tooltip("도전 보상 전체를 감싸는 루트(선택). 더 도전할 단계가 없으면 통째로 숨긴다.")]
    [SerializeField] private GameObject challengeRewardGroup;
    [Tooltip("도전 클리어 보상 슬롯 부모.")]
    [SerializeField] private Transform challengeRewardParent;

    [Header("Sweep Rewards (즉시해결 1장당, 클리어한 최고 단계 기준)")]
    [Tooltip("즉시해결 보상 전체를 감싸는 루트(선택). 클리어 기록이 없으면 통째로 숨긴다.")]
    [SerializeField] private GameObject sweepRewardGroup;
    [Tooltip("즉시해결 1장당 보상 슬롯 부모.")]
    [SerializeField] private Transform sweepRewardParent;

    [Header("Common")]
    [Tooltip("보상 슬롯 프리팹(두 목록이 공유한다).")]
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    [Header("Buttons")]
    [Tooltip("즉시해결. 클리어 기록과 의뢰해결권이 모두 있어야 활성화된다.")]
    [SerializeField] private Button sweepButton;
    [Tooltip("즉시해결 버튼 안의 라벨(선택). 반복할 단계가 도전 단계와 한 칸 차이라, " +
             "버튼 자체에 '즉시해결: 2단계' 로 박아 무엇을 받는지 헷갈리지 않게 한다.")]
    [SerializeField] private TMP_Text sweepButtonLabel;
    [Tooltip("의뢰도전. 무료이며 다음 단계 데이터만 있으면 활성화된다.")]
    [SerializeField] private Button challengeButton;
    [Tooltip("의뢰도전 버튼 안의 라벨(선택). 즉시해결 버튼과 대상 단계가 한 칸 다르므로, " +
             "양쪽 버튼 모두에 대상 단계를 박아 무엇을 하는 버튼인지 바로 보이게 한다.")]
    [SerializeField] private TMP_Text challengeButtonLabel;
    [Tooltip("도전이 불가능할 때의 사유 안내(선택).")]
    [SerializeField] private TMP_Text challengeBlockedText;
    [Tooltip("즉시해결이 불가능할 때의 사유 안내(선택).")]
    [SerializeField] private TMP_Text sweepBlockedText;

    private readonly List<RewardSlotView> _challengeSlots = new();
    private readonly List<RewardSlotView> _sweepSlots = new();

    private bool _wired;
    private bool _validated;

    /// <summary>[의뢰도전] 클릭. 현재 선택된 종류를 함께 넘긴다.</summary>
    public event Action<CommissionType> ChallengeRequested;
    /// <summary>[즉시해결] 클릭. 현재 선택된 종류를 함께 넘긴다.</summary>
    public event Action<CommissionType> SweepRequested;

    /// <summary>현재 표시 중인 의뢰 종류.</summary>
    public CommissionType Type { get; private set; }

    /// <param name="clearedStage">클리어한 최고 단계(0=없음). 즉시해결이 반복할 대상이다.</param>
    /// <param name="challengeStage">이번에 도전할 단계.</param>
    /// <param name="hasNextStage">도전할 단계 데이터가 있는지(도전은 무료라 이것만 보면 된다).</param>
    public void Bind(BattleHudContext ctx, CommissionType type, int clearedStage, int challengeStage, bool hasNextStage) {
        Type = type;
        WireOnce();
        ValidateParentsOnce();

        if (nameText != null) nameText.text = CommissionRewardRules.TypeName(type);
        if (summaryText != null) summaryText.text = CommissionRewardRules.TypeSummary(type);
        if (stageText != null)
            stageText.text = CommissionEntryView.StageText(clearedStage, challengeStage, hasNextStage);

        BindChallengeRewards(ctx, type, challengeStage, hasNextStage);
        BindSweepRewards(ctx, type, clearedStage);
        RefreshButtons(ctx, clearedStage, challengeStage, hasNextStage);
    }

    // 도전 보상: 도전 단계를 "처음" 깼을 때 1회 받는 것.
    // 더 도전할 단계가 없으면 보여줄 것이 없으므로 통째로 숨긴다(최대 단계 보상이 남아 오해를 주지 않도록).
    private void BindChallengeRewards(BattleHudContext ctx, CommissionType type, int challengeStage, bool hasNextStage) {
        if (challengeRewardGroup != null) challengeRewardGroup.SetActive(hasNextStage);
        if (!hasNextStage) return;

        // 각인 도면도 보상 목록에 들어 있으므로 별도 안내 없이 슬롯으로 함께 그려진다.
        BuildSlots(ctx, _challengeSlots, challengeRewardParent, CommissionRewardRules.ClearRewards(type, challengeStage));
    }

    // 즉시해결 보상: 의뢰해결권 1장당 받는 몫. 도전 단계가 아니라 "클리어한 최고 단계"를 쓴다.
    // 클리어 기록이 없으면 반복할 대상 자체가 없으므로 숨긴다(버튼도 같은 조건으로 잠긴다).
    private void BindSweepRewards(BattleHudContext ctx, CommissionType type, int clearedStage) {
        bool hasTarget = clearedStage > 0;
        if (sweepRewardGroup != null) sweepRewardGroup.SetActive(hasTarget);
        if (!hasTarget) return;

        BuildSlots(ctx, _sweepSlots, sweepRewardParent, CommissionRewardRules.SweepRewards(type, clearedStage));
    }

    // 버튼 리스너는 1회만 건다(선택이 바뀔 때마다 다시 걸면 중복 호출된다).
    // 클릭 시점의 Type 을 읽으므로 캡처한 값이 낡을 걱정도 없다.
    private void WireOnce() {
        if (_wired) return;
        _wired = true;
        if (challengeButton != null) challengeButton.onClick.AddListener(() => ChallengeRequested?.Invoke(Type));
        if (sweepButton != null) sweepButton.onClick.AddListener(() => SweepRequested?.Invoke(Type));
    }

    private void RefreshButtons(BattleHudContext ctx, int clearedStage, int challengeStage, bool hasNextStage) {
        int tickets = ctx.Store.CommissionTicket.Value;
        bool canSweep = clearedStage > 0 && tickets > 0;

        if (challengeButton != null) challengeButton.interactable = hasNextStage;
        if (sweepButton != null) sweepButton.interactable = canSweep;

        // 두 버튼은 대상 단계가 한 칸 다르다(도전 = 클리어한 단계 + 1, 즉시해결 = 클리어한 단계).
        // 양쪽 라벨에 대상 단계를 박아 눌렀을 때 무엇이 오는지 헷갈리지 않게 한다.
        // 대상 단계가 없는 경우(버튼이 잠긴 상태)에는 숫자 없이 이름만 남긴다.
        if (challengeButtonLabel != null)
            challengeButtonLabel.text = hasNextStage ? $"도전: {challengeStage}단계" : "도전";
        if (sweepButtonLabel != null)
            sweepButtonLabel.text = clearedStage > 0 ? $"즉시해결: {clearedStage}단계" : "즉시해결";

        SetHint(challengeBlockedText, !hasNextStage, "더 도전할 단계가 없습니다");
        SetHint(sweepBlockedText, !canSweep,
                clearedStage <= 0 ? "먼저 1단계를 클리어해야 합니다" : "의뢰해결권이 부족합니다");
    }

    // 두 보상 목록은 각자 다른 부모에 슬롯을 스폰한다. 인스펙터에서 두 칸에 같은 오브젝트를 꽂으면
    // 서로의 슬롯이 한자리에 섞여 도전 보상과 즉시해결 보상이 함께 찍힌다.
    // 값이 틀린 게 아니라 "자리"가 틀린 것이라 눈으로 잡기 어려우므로 최초 1회 검사해 알려 준다.
    private void ValidateParentsOnce() {
        if (_validated) return;
        _validated = true;

        if (challengeRewardParent == null || challengeRewardParent != sweepRewardParent) return;
        Debug.LogError("[CommissionDetailPanel] challengeRewardParent 와 sweepRewardParent 에 같은 오브젝트가 " +
                       "할당되어 있습니다. 도전 보상과 즉시해결 보상이 한자리에 섞여 표시됩니다. 각각 다른 부모를 지정하세요.");
    }

    private static void SetHint(TMP_Text text, bool show, string message) {
        if (text == null) return;
        text.gameObject.SetActive(show);
        if (show) text.text = message;
    }

    // 슬롯을 필요한 만큼만 만들어 재사용한다(선택이 바뀔 때마다 갱신되므로 재생성하지 않는다).
    private void BuildSlots(BattleHudContext ctx, List<RewardSlotView> slots, Transform parent, IReadOnlyList<RewardItem> rewards) {
        if (rewardSlotPrefab == null || parent == null) return;

        while (slots.Count < rewards.Count) slots.Add(Instantiate(rewardSlotPrefab, parent));

        for (int i = 0; i < slots.Count; i++) {
            bool used = i < rewards.Count;
            slots[i].gameObject.SetActive(used);
            if (used) slots[i].Bind(RewardVisualResolver.Resolve(ctx, rewards[i]));
        }
    }
}
