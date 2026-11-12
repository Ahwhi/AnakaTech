using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 수량 선택 팝업(공용). 1 ~ 최대 사이에서 수량을 고르고 확인하면 그 수량을, 취소하거나 그냥 닫으면 null 을 돌려준다.
/// 고른 수량으로 무엇을 할지는 모른다(서버 요청은 이 팝업을 연 쪽이 한다).
///   int? count = await popup.ShowAsync("수량 선택", item.DisplayName, owned);
/// </summary>
public sealed class QuantitySelectPopup : UIView {
    [SerializeField] private TMP_Text titleText;
    [Tooltip("선택. 무엇의 수량인지 안내(아이템 이름 등).")]
    [SerializeField] private TMP_Text messageText;

    [Header("Quantity")]
    [Tooltip("수량 슬라이더(WholeNumbers=on). 최소 1, 최대는 코드가 정한다.")]
    [SerializeField] private Slider quantitySlider;
    [Tooltip("고른 수량 표시. 최대도 함께 보여 준다(예: 12 / 50).")]
    [SerializeField] private TMP_Text quantityValueText;
    [Tooltip("선택. 수량 1 감소. ButtonHoldRepeater 를 붙이면 꾹 눌러 빠르게 줄일 수 있다.")]
    [SerializeField] private Button minusButton;
    [Tooltip("선택. 수량 1 증가. ButtonHoldRepeater 를 붙이면 꾹 눌러 빠르게 늘릴 수 있다.")]
    [SerializeField] private Button plusButton;
    [Tooltip("선택. 최소(1)로.")]
    [SerializeField] private Button minButton;
    [Tooltip("선택. 최대로.")]
    [SerializeField] private Button maxButton;

    [Header("Buttons")]
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    private UniTaskCompletionSource<int?> _result;
    private int _max = 1;
    private int _count = 1;
    private Func<int, string> _describe;

    protected override void Awake() {
        base.Awake();
        if (quantitySlider != null) quantitySlider.onValueChanged.AddListener(v => SetCount(Mathf.RoundToInt(v)));
        if (minusButton != null) minusButton.onClick.AddListener(() => SetCount(_count - 1));
        if (plusButton != null) plusButton.onClick.AddListener(() => SetCount(_count + 1));
        if (minButton != null) minButton.onClick.AddListener(() => SetCount(1));
        if (maxButton != null) maxButton.onClick.AddListener(() => SetCount(_max));
        if (okButton != null) okButton.onClick.AddListener(() => Resolve(_count));
        if (cancelButton != null) cancelButton.onClick.AddListener(() => Resolve(null));
    }

    /// <param name="max">고를 수 있는 최대 수량(보유 개수 등). 1 미만이면 1 로 본다.</param>
    /// <param name="initial">처음 골라 둘 수량(범위 밖이면 잘린다).</param>
    /// <param name="describe">
    /// 선택. 수량이 바뀔 때마다 안내 문구를 다시 만든다(예: 캡슐 수량에 따른 레벨 변화, rich text).
    /// 넘기면 message 대신 이 결과를 보여 준다.
    /// </param>
    public async UniTask<int?> ShowAsync(string title, string message, int max, int initial = 1,
                                         Func<int, string> describe = null) {
        if (titleText != null) titleText.text = title;
        _describe = describe;
        SetMessage(message);

        _max = Mathf.Max(1, max);
        if (quantitySlider != null) {
            quantitySlider.wholeNumbers = true;
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = _max;
        }
        SetCount(initial);

        _result = new UniTaskCompletionSource<int?>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void SetCount(int value) {
        _count = Mathf.Clamp(value, 1, _max);
        if (quantitySlider != null) quantitySlider.SetValueWithoutNotify(_count);
        if (quantityValueText != null) quantityValueText.text = $"{_count:N0} / {_max:N0}";
        if (minusButton != null) minusButton.interactable = _count > 1;
        if (minButton != null) minButton.interactable = _count > 1;
        if (plusButton != null) plusButton.interactable = _count < _max;
        if (maxButton != null) maxButton.interactable = _count < _max;
        if (_describe != null) SetMessage(_describe(_count));
    }

    private void SetMessage(string message) {
        if (messageText == null) return;
        messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        messageText.text = message ?? "";
    }

    private void Resolve(int? value) {
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }

    // 확인/취소 외의 경로(배경 탭, 일괄 닫힘)로 닫혀도 결과를 확정해 기다리는 쪽이 멈추지 않게 한다(취소로 간주).
    protected override void OnClosed() => _result?.TrySetResult(null);
}
