using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 확인/취소 팝업(공용). 확인=true, 취소=false 를 await 로 돌려준다.
/// "현재 던전을 종료하시겠습니까?"(PLANNING 1-3), "다이아가 부족합니다. 구매하시겠습니까?"(PLANNING 10) 등 재사용.
///   var yes = await popup.ShowAsync("던전 종료", "현재 진행중인 던전을 종료하시겠습니까?");
/// </summary>
public sealed class ConfirmCancelPopup : UIView {
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    private UniTaskCompletionSource<bool> _result;

    protected override void Awake() {
        base.Awake();
        if (okButton != null) okButton.onClick.AddListener(() => Resolve(true));
        if (cancelButton != null) cancelButton.onClick.AddListener(() => Resolve(false));
    }

    public async UniTask<bool> ShowAsync(string title, string message) {
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;

        _result = new UniTaskCompletionSource<bool>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void Resolve(bool value) {
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }

    // OK/취소 외의 경로(배경 탭, 외부 CloseAllPopups 등)로 닫혀도 반드시 결과를 확정해,
    // await 중인 호출부가 영원히 멈추는 것을 막는다(취소로 간주).
    protected override void OnClosed() {
        _result?.TrySetResult(false);
    }
}
