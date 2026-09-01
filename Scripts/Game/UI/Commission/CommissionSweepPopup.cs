using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 의뢰 즉시해결 팝업. 보유 의뢰해결권 범위 안에서 사용 장수를 고르고(슬라이더 / - + / 최대),
/// 고른 장수만큼의 예정 보상을 미리 보여준다. 확인하면 사용 장수를, 취소하면 0 을 await 로 돌려준다.
///
/// 보상 미리보기 값은 서버 CommissionCatalog 를 미러한 CommissionRewardRules 에서 나온다
/// (실제 지급은 서버가 확정하므로 표시와 지급이 어긋나지 않게 한 곳에서 계산한다).
/// </summary>
public sealed class CommissionSweepPopup : UIView {
    [Header("Header")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("반복할 단계 안내(예: 토벌 의뢰 4단계 반복).")]
    [SerializeField] private TMP_Text subtitleText;

    [Header("Quantity")]
    [Tooltip("사용 장수 슬라이더(WholeNumbers=on). 최소 1, 최대는 보유 장수로 코드가 설정한다.")]
    [SerializeField] private Slider quantitySlider;
    [SerializeField] private TMP_Text quantityValueText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [Tooltip("보유 장수 전부를 선택하는 버튼.")]
    [SerializeField] private Button maxButton;

    [Header("Cost (확인 버튼 안)")]
    [Tooltip("확인 버튼 안의 소모 수량 텍스트. 선택한 장수를 그대로 보여주고, 보유가 모자라면 부족 색으로 바뀐다.")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("확인 버튼 안의 의뢰해결권 아이콘. 재화 카탈로그에서 자동으로 채운다(선택).")]
    [SerializeField] private Image ticketIcon;
    [Tooltip("선택. 의뢰해결권 부족 시 노출할 안내 오브젝트(예: '의뢰해결권이 부족합니다').")]
    [SerializeField] private GameObject insufficientHint;
    [Tooltip("의뢰해결권 부족 시 소모 수량 텍스트 색.")]
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.20f, 0.20f, 1f);

    [Header("Reward Preview")]
    [SerializeField] private Transform rewardParent;
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    [Header("Buttons")]
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    private UniTaskCompletionSource<int> _result;
    private readonly List<RewardSlotView> _slots = new();

    private BattleHudContext _ctx;
    private CommissionType _type;
    private int _stage;
    private int _owned;
    private int _count = 1;

    private Color _costDefaultColor = Color.white;
    private bool _capturedCostColor;

    protected override void Awake() {
        base.Awake();
        if (costText != null && !_capturedCostColor) {
            _costDefaultColor = costText.color;
            _capturedCostColor = true;
        }
        if (okButton != null) okButton.onClick.AddListener(() => Resolve(_count));
        if (cancelButton != null) cancelButton.onClick.AddListener(() => Resolve(0));
        if (minusButton != null) minusButton.onClick.AddListener(() => SetCount(_count - 1));
        if (plusButton != null) plusButton.onClick.AddListener(() => SetCount(_count + 1));
        if (maxButton != null) maxButton.onClick.AddListener(() => SetCount(_owned));
        if (quantitySlider != null) quantitySlider.onValueChanged.AddListener(v => SetCount(Mathf.RoundToInt(v)));
    }

    /// <param name="stage">반복할 단계(클리어한 최고 단계).</param>
    /// <param name="ownedTickets">보유 의뢰해결권.</param>
    /// <returns>확인 시 사용 장수(>=1), 취소/외부 닫힘 시 0.</returns>
    public async UniTask<int> ShowAsync(BattleHudContext ctx, CommissionType type, int stage, int ownedTickets) {
        _ctx = ctx;
        _type = type;
        _stage = stage;
        _owned = Mathf.Max(0, ownedTickets);

        if (titleText != null) titleText.text = "즉시해결";
        if (subtitleText != null) subtitleText.text = $"{CommissionRewardRules.TypeName(type)} {stage}단계 반복";
        CurrencyIcon.Apply(ticketIcon, ctx.Currencies, CurrencyType.CommissionTicket);

        // 보유가 0이면 조작 자체를 막는다(확인 버튼 비활성/부족 표시는 SetCount 가 처리).
        bool usable = _owned > 0;
        if (quantitySlider != null) {
            quantitySlider.wholeNumbers = true;
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = Mathf.Max(1, _owned);
            quantitySlider.interactable = usable;
        }
        if (minusButton != null) minusButton.interactable = usable;
        if (plusButton != null) plusButton.interactable = usable;
        if (maxButton != null) maxButton.interactable = usable;

        SetCount(1);

        _result = new UniTaskCompletionSource<int>();
        await base.ShowAsync();
        return await _result.Task;
    }

    // 보유 장수 안으로 클램프하고 표시/보상 미리보기를 갱신한다.
    private void SetCount(int value) {
        _count = Mathf.Clamp(value, 1, Mathf.Max(1, _owned));
        if (quantitySlider != null) quantitySlider.SetValueWithoutNotify(_count);
        if (quantityValueText != null) quantityValueText.text = _count.ToString();

        // 소모량은 항상 보유량 이하로 클램프되므로, 부족해지는 경우는 보유가 0일 때뿐이다
        // (그때도 최소 1장은 선택된 상태라 부족으로 잡혀 버튼이 잠긴다).
        bool insufficient = _owned < _count;
        if (costText != null) {
            costText.text = _count.ToString("N0");
            costText.color = insufficient ? insufficientColor : _costDefaultColor;
        }
        if (insufficientHint != null) insufficientHint.SetActive(insufficient);
        if (okButton != null) okButton.interactable = !insufficient;

        var rewards = CommissionRewardRules.ScaleRewards(
            CommissionRewardRules.SweepRewards(_type, _stage), _count);
        BuildRewards(rewards);
    }

    // 슬롯을 필요한 만큼만 만들어 재사용한다(수량을 바꿀 때마다 갱신되므로 재생성하지 않는다).
    private void BuildRewards(IReadOnlyList<RewardItem> rewards) {
        if (rewardSlotPrefab == null || rewardParent == null) return;

        while (_slots.Count < rewards.Count) _slots.Add(Instantiate(rewardSlotPrefab, rewardParent));

        for (int i = 0; i < _slots.Count; i++) {
            bool used = i < rewards.Count;
            _slots[i].gameObject.SetActive(used);
            if (used) _slots[i].Bind(RewardVisualResolver.Resolve(_ctx, rewards[i]));
        }
    }

    private void Resolve(int count) {
        _result?.TrySetResult(count);
        CloseAsync().Forget();
    }

    // 버튼 외의 경로(배경 탭, 일괄 닫힘)로 닫혀도 반드시 결과를 확정한다(취소=0).
    protected override void OnClosed() {
        _result?.TrySetResult(0);
    }
}
