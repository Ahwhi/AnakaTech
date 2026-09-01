using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 확인 전용 안내 팝업(공용). 확인 버튼 하나만 두어 메시지를 보여주고, 사용자가 확인을 누르면 닫힌다(취소 없음).
/// 호출부는 await 로 "사용자가 확인할 때까지" 기다린 뒤 후속 동작(타이틀 이동/부트 재시도 등)을 이어서 수행한다.
///   var popup = await UI.ShowPopupAsync&lt;ConfirmPopup&gt;(_popups.ConfirmPopup);
///   await popup.ShowAsync("연결 끊김", "서버와의 연결이 끊어졌습니다.");
///
/// 대기는 "확인 버튼"이 아니라 "닫힘"을 기다린다. 이 팝업에는 확인 말고 다른 선택지가 없으므로
/// 어떤 경로로 닫혔는지(확인 버튼 / 배경 탭 / 뒤로가기)가 호출부의 후속 동작을 가르지 않는다.
/// 버튼만 기다리면 배경 탭으로 닫혔을 때 대기가 영영 풀리지 않아, 안내만 사라지고 후속 동작(타이틀 이동 등)이
/// 일어나지 않은 채 그 화면에 남게 된다.
/// </summary>
public sealed class ConfirmPopup : UIView {
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button okButton;

    private UniTaskCompletionSource _result;

    protected override void Awake() {
        base.Awake();
        okButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    /// <summary>내용을 채우고 페이드 인한 뒤, 팝업이 닫힐 때까지 대기.</summary>
    public async UniTask ShowAsync(string title, string message) {
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;

        _result = new UniTaskCompletionSource();
        await base.ShowAsync();        // 베이스 페이드 인
        await _result.Task;            // 닫힘 대기
    }

    // 확인 버튼, 배경 탭(UIView.tapToCloseArea), 뒤로가기가 모두 여기로 모인다.
    protected override void OnClosed() => _result?.TrySetResult();
}
