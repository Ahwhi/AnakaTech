using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오프라인 보상 안내 팝업.
///
/// 상점 구매 완료 팝업(PurchaseCompletePopup)을 재사용하지 않는다. 그 팝업은 "무엇을 받았는지"만 그리는데,
/// 오프라인 보상은 그 외에 비운 시간과 최대치 도달 여부를 함께 보여 줘야 하고 구매가 아니기 때문이다.
/// 다만 보상 칸 자체는 같은 공용 부품(RewardSlotView + RewardVisualResolver)을 그대로 쓴다.
///
/// 표시하는 값은 전부 서버가 정산한 결과다. 이 화면은 시간도 지급량도 계산하지 않는다.
/// 팝업을 다시 열어도 서버가 재지급하지 않으므로 보상이 늘어나지 않는다.
/// </summary>
public sealed class OfflineRewardPopup : UIView {
    [Header("Header")]
    [Tooltip("선택. 상단 제목. 배선하면 코드가 문구를 채운다(프리팹에 적어 둔 글자는 덮어쓴다).")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("선택. 비운 시간 표시(예: 3시간 12분 동안 자리를 비웠습니다).")]
    [SerializeField] private TMP_Text durationText;
    [Tooltip("선택. 최대치에 도달했을 때만 켜지는 안내 오브젝트.")]
    [SerializeField] private GameObject cappedNotice;
    [Tooltip("선택. 최대치 안내 문구. cappedNotice 안에 두면 함께 켜지고 꺼진다.")]
    [SerializeField] private TMP_Text cappedText;
    [Tooltip("선택. 투자로 늘어난 보상 안내(예: 투자 15% 적용). 투자 효과가 없으면 코드가 꺼 둔다.")]
    [SerializeField] private TMP_Text investText;

    [Header("Rewards (획득 보상)")]
    [Tooltip("보상 슬롯을 생성할 부모.")]
    [SerializeField] private Transform rewardRoot;
    [Tooltip("보상 1개를 표시할 공용 슬롯 프리팹(RewardSlotView). 상점/우편과 같은 부품이다.")]
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    [Header("Close")]
    [SerializeField] private Button okButton;

    private const string TitleMessage = "오프라인 보상";
    private const string CappedMessage = "최대 12시간 보상";
    /// <summary>투자로 늘어난 경험치 안내. {0} 에 적용 비율이 들어간다(예: 투자 15% 적용).</summary>
    private const string InvestMessageFormat = "투자 +{0} 적용";

    private readonly List<RewardSlotView> _slots = new();
    private UniTaskCompletionSource _closed;

    protected override void Awake() {
        base.Awake();
        // 배경 탭으로 닫기는 베이스(UIView.tapToCloseArea)가 처리한다.
        if (okButton != null) okButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    /// <summary>정산 결과를 그리고 팝업을 연다. 닫힐 때까지 기다린다(다른 필수 팝업과 겹치지 않게 하기 위해서다).</summary>
    public async UniTask ShowAsync(BattleHudContext hud, OfflineRewardDto reward) {
        if (titleText != null) titleText.text = TitleMessage;
        if (durationText != null) durationText.text = FormatDuration(reward.RewardDuration);

        if (cappedNotice != null) cappedNotice.SetActive(reward.Capped);
        if (cappedText != null) cappedText.text = CappedMessage;

        ApplyInvestNotice(reward);

        BuildSlots(hud, reward);

        _closed = new UniTaskCompletionSource();
        await base.ShowAsync();
        await _closed.Task;
    }

    // 0 인 보상은 칸을 만들지 않는다. 빈 칸을 두면 "받았는데 0" 처럼 읽힌다.
    private void BuildSlots(BattleHudContext hud, OfflineRewardDto reward) {
        Clear();
        if (rewardRoot == null || rewardSlotPrefab == null) return;

        if (reward.Gold > 0) AddSlot(hud, RewardItem.Gold(reward.Gold));
        if (reward.Stamina > 0) AddSlot(hud, RewardItem.Stamina(reward.Stamina));
        if (reward.Diamond > 0) AddSlot(hud, RewardItem.Diamond(reward.Diamond));
        if (reward.Exp > 0) AddSlot(hud, RewardItem.Exp(reward.Exp));
    }

    private void AddSlot(BattleHudContext hud, RewardItem item) {
        var slot = Instantiate(rewardSlotPrefab, rewardRoot);
        slot.Bind(RewardVisualResolver.Resolve(hud, item));
        _slots.Add(slot);
    }

    /// <summary>
    /// 투자로 얼마나 더 받았는지 알린다. 투자하지 않았거나 그 효과가 닿지 않는 정산이면 칸 자체를 끈다
    /// ("투자 0% 적용"은 받은 것이 없다는 뜻이 아니라 투자를 안 했다는 뜻이라, 문구로 남기면 오해를 부른다).
    ///
    /// 비율은 서버가 지급에 실제로 쓴 값이다. 지금 투자 레벨로 다시 계산하면, 정산 뒤에 투자를 올린 경우
    /// 받은 경험치와 표시 비율이 어긋난다.
    /// </summary>
    private void ApplyInvestNotice(OfflineRewardDto reward) {
        if (investText == null) return;

        bool show = reward.InvestExpPermille > 0 && reward.Exp > 0;
        investText.gameObject.SetActive(show);
        if (show) investText.text = string.Format(InvestMessageFormat, InvestmentRules.PercentLabel(reward.InvestExpPermille));
    }

    /// <summary>
    /// 보상을 받은 시간 문구. 시간과 분만 보여 준다(초는 의미가 없고 매번 달라 보이면 값이 흔들리는 것처럼 읽힌다).
    /// 최소 지급 시간이 10분이라 "0분"이 뜨는 경우는 없다.
    ///
    /// 실제로 비운 시간이 아니라 "보상으로 인정된 시간"을 쓴다. 20시간을 비워도 12시간까지만 지급되므로
    /// 실제 시간을 적으면 받은 보상과 문구가 어긋나 덜 받은 것처럼 읽힌다.
    /// </summary>
    private static string FormatDuration(System.TimeSpan span) {
        int hours = (int)span.TotalHours;
        int minutes = span.Minutes;
        string spent = hours > 0 ? $"{hours}시간 {minutes}분" : $"{minutes}분";
        return $"{spent}의 오프라인 보상을 획득했습니다.";
    }

    private void Clear() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
    }

    protected override void OnClosed() {
        Clear();
        _closed?.TrySetResult();
    }
}
