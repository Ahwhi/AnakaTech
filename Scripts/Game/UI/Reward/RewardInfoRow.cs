using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 보상 정보 표의 한 줄: "조건 라벨 + 그 조건에서 받는 보상들". 보상 칸은 공용 RewardSlotView 로 그린다.
///
/// 컨텐츠를 전혀 모른다. 라벨이 보스 레벨인지 승패인지는 이 줄을 채우는 쪽이 정하므로,
/// 조건별로 보상이 갈리는 화면이면 무엇이든 이 줄을 그대로 쓴다.
/// </summary>
public sealed class RewardInfoRow : MonoBehaviour {
    [Tooltip("조건 라벨(예: Lv.5 / 승리). 줄을 채우는 쪽이 문구를 정한다.")]
    [SerializeField] private TMP_Text labelText;
    [Tooltip("보상 슬롯이 생성될 부모(Horizontal Layout Group 권장).")]
    [SerializeField] private Transform rewardRoot;
    [Tooltip("보상 1칸 프리팹(공용 RewardSlotView).")]
    [SerializeField] private RewardSlotView rewardSlotPrefab;
    [Tooltip("지금 해당되는 줄에만 켜는 그룹(예: '현재 레벨' 표식/테두리). 비우면 표시 없음.")]
    [SerializeField] private GameObject currentGroup;

    private readonly List<RewardSlotView> _slots = new();

    /// <summary>한 줄을 채운다. isCurrent 면 현재 표식 그룹이 켜진다.</summary>
    public void Bind(BattleHudContext hud, string label, IReadOnlyList<RewardItem> rewards, bool isCurrent) {
        if (labelText != null) labelText.text = label;
        if (currentGroup != null) currentGroup.SetActive(isCurrent);

        Clear();
        if (rewardRoot == null || rewardSlotPrefab == null || rewards == null) return;

        foreach (var r in rewards) {
            var slot = Instantiate(rewardSlotPrefab, rewardRoot);
            slot.Bind(RewardVisualResolver.Resolve(hud, r));
            _slots.Add(slot);
        }
    }

    private void Clear() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
    }

    private void OnDestroy() => Clear();
}
