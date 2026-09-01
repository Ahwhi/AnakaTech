using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 진행 중 다른 던전을 선택했을 때의 분기 팝업(PLANNING 1-3).
///   - Reserve  : 변경 예약. 현재 던전을 끝내고 보상을 받은 뒤 자동으로 전환.
///   - Immediate: 즉시 변경. 현재 런을 버리고 바로 전환(보상 없음).
///   - Cancel   : 변경하지 않음.
/// 선택 결과를 await 로 돌려준다.
///   var r = await popup.ShowAsync("던전 변경", "...");
/// </summary>
public sealed class DungeonChangePopup : UIView {
    public enum Result { Cancel, Reserve, Immediate }

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [Tooltip("변경 예약(현재 던전 보상 후 변경).")]
    [SerializeField] private Button reserveButton;
    [Tooltip("즉시 변경(보상 없음).")]
    [SerializeField] private Button immediateButton;
    [SerializeField] private Button cancelButton;

    private UniTaskCompletionSource<Result> _result;

    protected override void Awake() {
        base.Awake();
        if (reserveButton != null) reserveButton.onClick.AddListener(() => Resolve(Result.Reserve));
        if (immediateButton != null) immediateButton.onClick.AddListener(() => Resolve(Result.Immediate));
        if (cancelButton != null) cancelButton.onClick.AddListener(() => Resolve(Result.Cancel));
    }

    public async UniTask<Result> ShowAsync(string title, string message) {
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;

        _result = new UniTaskCompletionSource<Result>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void Resolve(Result value) {
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }
}
