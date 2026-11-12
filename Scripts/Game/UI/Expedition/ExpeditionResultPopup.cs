using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원정대 결과 팝업. 한 판에서 깎은 피해량, 보고 후 장소 상태, 참여 보상을 보여 준다.
/// 길드 레이드 결과 팝업과 같은 구성이라 그 프리팹을 복사해 스크립트만 바꿔 쓸 수 있다.
/// </summary>
public sealed class ExpeditionResultPopup : UIView {
    [Header("Result")]
    [Tooltip("결과 문구(레벨 상승 / 시간 종료 / 전멸).")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("이번 판에서 깎은 피해량.")]
    [SerializeField] private TMP_Text dealtDamageText;

    [Header("Location (서버 반영 후 상태)")]
    [Tooltip("보고 후 서버가 알려준 장소 상태 묶음. 응답을 못 받았으면 통째로 끈다.")]
    [SerializeField] private GameObject locationGroup;
    [SerializeField] private TMP_Text locationNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider hpSlider;
    [Tooltip("이번 판으로 레벨이 올랐을 때 켜는 표시.")]
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

    /// <param name="report">결과 보고 응답. 통신에 실패했으면 null 이고, 그때는 장소 상태와 보상을 감춘다.</param>
    public async UniTask ShowAsync(ExpeditionResult result, string locationName, long dealtDamage,
                                   ExpeditionDamageDto report, BattleHudContext hud) {
        bool hasReport = report != null && report.IsSuccess;
        bool levelUp = hasReport && report.LevelUp;

        // 레벨이 올랐는지는 서버가 판정한다(그 사이 다른 판정으로 체력이 달라졌을 수 있다).
        if (titleText != null) titleText.text = levelUp
            ? (ExpeditionRules.IsCleared(report.Level) ? "클리어!" : "레벨 상승!")
            : result == ExpeditionResult.Wiped ? "전멸" : "시간 종료";
        if (dealtDamageText != null) dealtDamageText.text = dealtDamage.ToString("N0");

        if (locationGroup != null) locationGroup.SetActive(hasReport);
        if (levelUpLabel != null) levelUpLabel.SetActive(levelUp);
        if (locationNameText != null) locationNameText.text = locationName;

        if (hasReport) {
            bool cleared = ExpeditionRules.IsCleared(report.Level);
            if (levelText != null) levelText.text = ExpeditionRules.LevelText(report.Level);
            if (hpText != null) {
                hpText.gameObject.SetActive(!cleared);
                hpText.text = $"{report.Hp:N0} / {report.MaxHp:N0}";
            }
            if (hpSlider != null) {
                hpSlider.gameObject.SetActive(!cleared);
                hpSlider.minValue = 0f;
                hpSlider.maxValue = 1f;
                hpSlider.SetValueWithoutNotify(report.MaxHp > 0 ? Mathf.Clamp01((float)report.Hp / report.MaxHp) : 0f);
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
