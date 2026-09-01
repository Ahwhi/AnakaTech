using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 채팅 신고 팝업. 어떤 발언을 누가 했는지 보여 주고, 사유를 골라 신고한다.
///
/// 신고 내용(발언 원문)은 보내지 않는다. 서버가 보관 기록에서 직접 가져오므로, 화면에 보이던 것과
/// 다른 내용을 적어 보낼 수 없다. 여기서 보내는 것은 "어느 발언을, 왜" 뿐이다.
///
/// 사유는 서버가 값으로만 다루고 문구는 이 화면이 갖는다. 문구를 고치는 데 서버 배포가 필요 없도록
/// 하기 위한 것이며, 다른 안내 문구들과 같은 규칙이다.
/// </summary>
public sealed class ChatReportPopup : UIView {
    [Header("신고 대상")]
    [Tooltip("선택. 신고할 상대의 닉네임을 채운다.")]
    [SerializeField] private TMP_Text targetNicknameText;
    [Tooltip("선택. 신고할 발언 내용을 그대로 보여 준다(무엇을 신고하는지 확인시키기 위한 것).")]
    [SerializeField] private TMP_Text messageText;

    [Header("사유")]
    [Tooltip("사유 선택 토글. 순서대로 욕설/부적절, 홍보/매크로, 불건전한 닉네임, 기타 다.")]
    [SerializeField] private Toggle[] reasonToggles;
    [Tooltip("기타를 골랐을 때만 켜지는 입력칸.")]
    [SerializeField] private TMP_InputField reasonInput;
    [Tooltip("선택. 기타 입력칸을 감싸는 오브젝트. 배선하면 기타를 고를 때만 켜진다.")]
    [SerializeField] private GameObject reasonInputGroup;

    [Header("Buttons")]
    [SerializeField] private Button submitButton;
    [SerializeField] private Button cancelButton;

    /// <summary>기타 사유 입력 최대 길이. 서버 저장 한도와 맞춘다.</summary>
    private const int MaxReasonLength = 256;

    private MainHudContext _hud;
    private ChatMessageDto _message;
    private UniTaskCompletionSource _closed;

    protected override void Awake() {
        base.Awake();
        // 배경 탭으로 닫기는 베이스(UIView.tapToCloseArea)가 처리한다.
        if (cancelButton != null) cancelButton.onClick.AddListener(() => CloseAsync().Forget());
        if (submitButton != null) submitButton.onClick.AddListener(Submit);
        if (reasonInput != null) reasonInput.characterLimit = MaxReasonLength;

        BindReasonToggles();
    }

    /// <summary>신고 대상을 그리고 팝업을 연다. 닫힐 때까지 기다린다.</summary>
    public async UniTask ShowAsync(MainHudContext hud, ChatMessageDto message) {
        _hud = hud;
        _message = message;

        if (targetNicknameText != null) targetNicknameText.text = message?.Nickname ?? "";
        if (messageText != null) messageText.text = message?.Text ?? "";

        // 처음에는 첫 사유를 골라 둔다. 아무것도 고르지 않은 채로 신고를 누르는 상태를 만들지 않는다.
        if (reasonToggles != null && reasonToggles.Length > 0 && reasonToggles[0] != null)
            reasonToggles[0].isOn = true;
        if (reasonInput != null) reasonInput.text = "";
        ApplyReasonInputVisibility();

        _closed = new UniTaskCompletionSource();
        await base.ShowAsync();
        await _closed.Task;
    }

    // 기타를 골랐을 때만 입력칸을 보여 준다. 항상 띄워 두면 무엇을 적어야 하는지 헷갈린다.
    private void BindReasonToggles() {
        if (reasonToggles == null) return;
        foreach (var toggle in reasonToggles) {
            if (toggle == null) continue;
            toggle.onValueChanged.AddListener(_ => ApplyReasonInputVisibility());
        }
    }

    private void ApplyReasonInputVisibility() {
        bool other = SelectedReason() == ChatReportReason.Other;
        if (reasonInputGroup != null) reasonInputGroup.SetActive(other);
        else if (reasonInput != null) reasonInput.gameObject.SetActive(other);
    }

    /// <summary>지금 고른 사유. 아무것도 켜져 있지 않으면 첫 사유로 본다.</summary>
    private ChatReportReason SelectedReason() {
        if (reasonToggles == null) return ChatReportReason.Abuse;

        for (int i = 0; i < reasonToggles.Length; i++)
            if (reasonToggles[i] != null && reasonToggles[i].isOn) return (ChatReportReason)i;

        return ChatReportReason.Abuse;
    }

    private void Submit() {
        if (_hud == null || _message == null) return;

        var reason = SelectedReason();
        string text = reasonInput != null ? reasonInput.text?.Trim() ?? "" : "";

        // 기타는 무엇이 문제인지 적어야 처리할 수 있다. 빈 채로 접수하면 운영자가 판단할 근거가 없다.
        if (reason == ChatReportReason.Other && string.IsNullOrEmpty(text)) {
            _hud.Ui.ShowToast("사유를 입력해 주세요.");
            return;
        }

        if (submitButton != null) submitButton.interactable = false;
        SubmitAsync(reason, text, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid SubmitAsync(ChatReportReason reason, string text, CancellationToken ct) {
        try {
            var result = await _hud.Chat.ReportAsync(_message.MessageId, reason, text, ct);
            _hud.Ui.ShowToast(result.Message);
            if (result.IsSuccess) await CloseAsync();
            else if (submitButton != null) submitButton.interactable = true;
        } catch (System.OperationCanceledException) {
        } catch (System.Exception e) {
            Debug.LogWarning($"[Chat] 신고 실패: {e.Message}");
            _hud.Ui.ShowToast("신고를 접수하지 못했습니다.");
            if (submitButton != null) submitButton.interactable = true;
        }
    }

    protected override void OnClosed() => _closed?.TrySetResult();
}
