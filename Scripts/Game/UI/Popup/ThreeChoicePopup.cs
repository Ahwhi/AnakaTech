using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3선택 확인 팝업(공용). ConfirmCancelPopup 의 선택지가 하나 더 필요한 경우에 쓴다.
/// 결과는 선택한 버튼의 인덱스(0=첫째, 1=둘째, 2=셋째)이며, 배경 탭/외부 닫힘 등 취소 경로는 Cancelled(-1) 이다.
///
/// 예) 각성 재료에 이미 각성된 장비가 섞였을 때:
///   var r = await popup.ShowAsync("경고", "이미 각성된 장비가 재료에 포함되어 있습니다.", "진행", "해당 장비 제외", "취소");
/// </summary>
public sealed class ThreeChoicePopup : UIView {
    /// <summary>취소(선택 없이 닫힘) 결과값.</summary>
    public const int Cancelled = -1;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;

    [Tooltip("첫째 선택 버튼(결과 0).")]
    [SerializeField] private Button firstButton;
    [Tooltip("둘째 선택 버튼(결과 1).")]
    [SerializeField] private Button secondButton;
    [Tooltip("셋째 선택 버튼(결과 2). 보통 '취소'.")]
    [SerializeField] private Button thirdButton;

    [Tooltip("각 버튼의 라벨. 비워두면 프리팹에 적힌 문구를 그대로 쓴다.")]
    [SerializeField] private TMP_Text firstLabel;
    [SerializeField] private TMP_Text secondLabel;
    [SerializeField] private TMP_Text thirdLabel;

    private UniTaskCompletionSource<int> _result;

    protected override void Awake() {
        base.Awake();
        if (firstButton != null) firstButton.onClick.AddListener(() => Resolve(0));
        if (secondButton != null) secondButton.onClick.AddListener(() => Resolve(1));
        if (thirdButton != null) thirdButton.onClick.AddListener(() => Resolve(2));
    }

    public async UniTask<int> ShowAsync(string title, string message,
                                       string first, string second, string third) {
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
        SetLabel(firstLabel, first);
        SetLabel(secondLabel, second);
        SetLabel(thirdLabel, third);

        _result = new UniTaskCompletionSource<int>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private static void SetLabel(TMP_Text label, string text) {
        if (label != null && !string.IsNullOrEmpty(text)) label.text = text;
    }

    private void Resolve(int value) {
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }

    // 버튼 외의 경로(배경 탭, 외부 CloseAllPopups 등)로 닫혀도 반드시 결과를 확정해,
    // await 중인 호출부가 영원히 멈추는 것을 막는다(취소로 간주).
    protected override void OnClosed() {
        _result?.TrySetResult(Cancelled);
    }
}
