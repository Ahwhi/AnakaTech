using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 길드 레이드 결과 팝업. 한 판에서 깎은 피해량과 보스의 현재 상태를 보여 준다.
/// 선택지가 없는 결과 표시라(다음 도전은 길드 팝업에서 다시 들어간다) 확인 하나로 닫고 메인으로 돌아간다.
/// </summary>
public sealed class GuildRaidResultPopup : UIView {
    [Header("Result")]
    [Tooltip("결과 문구(처치 / 시간 종료).")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("이번 도전에서 깎은 피해량.")]
    [SerializeField] private TMP_Text dealtDamageText;

    [Header("Boss (서버 반영 후 상태)")]
    [Tooltip("보고 후 서버가 알려준 보스 상태 묶음. 응답을 못 받았으면 통째로 끈다.")]
    [SerializeField] private GameObject bossGroup;
    [SerializeField] private TMP_Text bossLevelText;
    [SerializeField] private TMP_Text bossHpText;
    [SerializeField] private Slider bossHpSlider;
    [Tooltip("이번 도전으로 보스를 잡아 레벨이 올랐을 때 켜는 표시.")]
    [SerializeField] private GameObject levelUpLabel;

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

    /// <param name="report">피해 보고 응답. 통신에 실패했으면 null 이고, 그때는 보스 상태와 보상을 감춘다.</param>
    /// <param name="hud">보상 아이콘/이름을 해석할 컨텍스트.</param>
    public async UniTask ShowAsync(GuildRaidResult result, long dealtDamage, GuildRaidDamageDto report,
                                   BattleHudContext hud) {
        if (titleText != null) titleText.text = result switch {
            GuildRaidResult.Defeated => "보스 토벌!",
            GuildRaidResult.Wiped => "전멸",
            _ => "시간 종료"
        };
        if (dealtDamageText != null) dealtDamageText.text = dealtDamage.ToString("N0");

        bool hasReport = report != null && report.IsSuccess;
        if (bossGroup != null) bossGroup.SetActive(hasReport);
        if (levelUpLabel != null) levelUpLabel.SetActive(hasReport && report.BossDefeated);

        if (hasReport) {
            if (bossLevelText != null) bossLevelText.text = $"Lv.{report.BossLevel}";
            if (bossHpText != null) bossHpText.text = $"{report.BossHp:N0} / {report.BossMaxHp:N0}";
            if (bossHpSlider != null) {
                bossHpSlider.minValue = 0f;
                bossHpSlider.maxValue = 1f;
                bossHpSlider.SetValueWithoutNotify(report.BossMaxHp > 0
                    ? Mathf.Clamp01((float)report.BossHp / report.BossMaxHp) : 0f);
            }
        }

        BuildRewards(hasReport ? report.Granted : null, hud);

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
