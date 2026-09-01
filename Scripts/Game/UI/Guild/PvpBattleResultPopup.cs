using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유저 대 유저 전투 결과 팝업(라이트 결과 표시). 선택지 없이 확인 하나로 닫고 원래 화면으로 돌아간다.
/// 어느 컨텐츠에서 왔든 표시할 것은 승패와 상대 이름뿐이라 컨텐츠를 구분하지 않는다.
/// </summary>
public sealed class PvpBattleResultPopup : UIView {
    [Tooltip("결과 문구(승리 / 패배).")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("상대 이름.")]
    [SerializeField] private TMP_Text opponentText;
    [Tooltip("승리 시 켜는 연출 그룹(선택).")]
    [SerializeField] private GameObject winGroup;
    [Tooltip("패배 시 켜는 연출 그룹(선택).")]
    [SerializeField] private GameObject loseGroup;
    [Header("Rewards")]
    [Tooltip("참여 보상 슬롯이 생성될 부모. 보상이 없으면 비활성된다.")]
    [SerializeField] private Transform rewardParent;
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    [SerializeField] private Button confirmButton;

    private UniTaskCompletionSource _closed;
    private readonly List<RewardSlotView> _slots = new();

    protected override void Awake() {
        base.Awake();
        if (confirmButton != null) confirmButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    /// <param name="rewards">참여 보상(즉시 지급분). 없으면 보상 영역을 감춘다.</param>
    /// <param name="hud">보상 아이콘/이름을 해석할 컨텍스트.</param>
    public async UniTask ShowAsync(bool win, string opponentName, List<RewardItem> rewards, BattleHudContext hud) {
        if (titleText != null) titleText.text = win ? "승리" : "패배";
        if (opponentText != null) opponentText.text = opponentName;
        if (winGroup != null) winGroup.SetActive(win);
        if (loseGroup != null) loseGroup.SetActive(!win);

        BuildRewards(rewards, hud);

        _closed = new UniTaskCompletionSource();
        await base.ShowAsync();
        await _closed.Task;
    }

    // 보상 목록은 서버가 준 그대로 그린다(항목이 늘어도 이 코드는 그대로다).
    private void BuildRewards(List<RewardItem> rewards, BattleHudContext hud) {
        int count = rewards != null ? rewards.Count : 0;
        if (rewardParent != null) rewardParent.gameObject.SetActive(count > 0);
        if (rewardSlotPrefab == null || rewardParent == null) return;

        while (_slots.Count < count) _slots.Add(Instantiate(rewardSlotPrefab, rewardParent));

        for (int i = 0; i < _slots.Count; i++) {
            bool used = i < count;
            _slots[i].gameObject.SetActive(used);
            if (used) _slots[i].Bind(RewardVisualResolver.Resolve(hud, rewards[i]));
        }
    }

    protected override void OnClosed() => _closed?.TrySetResult();
}
