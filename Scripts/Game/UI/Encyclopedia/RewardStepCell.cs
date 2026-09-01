using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보상 스크롤뷰의 한 칸(단계/티어). 요구 조건 + 보상 슬롯(아이콘/조각프리팹 + 수량) + 수령 상태(수령완료/수령가능/잠금)를 표시한다.
/// 보상은 개수만큼 슬롯을 생성하므로(우편함과 동일 패턴) 복합 보상(예: 티켓 + 프로필 테두리)도 그대로 나열된다.
/// 도감 보상/충전 보상 등이 공용으로 사용한다(품목 해석은 RewardVisualResolver, 스폰은 RewardSlotView 담당).
/// </summary>
public sealed class RewardStepCell : MonoBehaviour {
    public enum CellState { Locked, Claimable, Claimed }

    [SerializeField] private TMP_Text stepText;
    [Tooltip("요구 조건(예: '5명 수집', '1,200원').")]
    [SerializeField] private TMP_Text requirementText;

    [Header("Rewards")]
    [Tooltip("보상 슬롯(아이콘/조각 + 수량)이 생성될 부모.")]
    [SerializeField] private Transform rewardSlotParent;
    [Tooltip("보상 1개를 표시하는 슬롯 프리팹.")]
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    [SerializeField] private Button claimButton;
    [Tooltip("수령 완료 시 노출(예: '수령완료' 오버레이).")]
    [SerializeField] private GameObject claimedOverlay;
    [Tooltip("미달성(잠금) 시 노출(선택).")]
    [SerializeField] private GameObject lockedOverlay;

    private readonly List<RewardSlotView> _slots = new();

    public int RewardType { get; private set; }
    public int Step { get; private set; }

    public void Bind(int rewardType, int step, string requirement, IReadOnlyList<RewardSlotView.RewardVisual> rewards,
                     CellState state, Action<RewardStepCell> onClaim) {
        RewardType = rewardType;
        Step = step;
        if (stepText != null) stepText.text = step.ToString();
        if (requirementText != null) requirementText.text = requirement;

        BuildRewardSlots(rewards);

        if (claimedOverlay != null) claimedOverlay.SetActive(state == CellState.Claimed);
        if (lockedOverlay != null) lockedOverlay.SetActive(state == CellState.Locked);
        if (claimButton != null) {
            claimButton.gameObject.SetActive(state == CellState.Claimable);
            claimButton.interactable = state == CellState.Claimable;
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => onClaim?.Invoke(this));
        }
    }

    private void BuildRewardSlots(IReadOnlyList<RewardSlotView.RewardVisual> rewards) {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();

        if (rewardSlotPrefab == null || rewardSlotParent == null || rewards == null) return;
        for (int i = 0; i < rewards.Count; i++) {
            var slot = Instantiate(rewardSlotPrefab, rewardSlotParent);
            slot.Bind(rewards[i]);
            _slots.Add(slot);
        }
    }

    public void SetClaimed() {
        if (claimedOverlay != null) claimedOverlay.SetActive(true);
        if (lockedOverlay != null) lockedOverlay.SetActive(false);
        if (claimButton != null) claimButton.gameObject.SetActive(false);
    }
}
