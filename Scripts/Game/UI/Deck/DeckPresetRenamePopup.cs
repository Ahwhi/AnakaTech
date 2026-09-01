using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 편성 프리셋 이름 변경 팝업. 입력한 이름을 await 로 돌려주고, 취소하면 null 을 돌려준다.
/// 빈 이름으로 확인하면 "기본 이름으로 되돌리기"를 뜻한다(서버가 빈 문자열로 저장하고 클라가 "프리셋 N" 으로 표시).
///
/// 통신은 하지 않는다. 저장은 호출측(DeckPresetPopup)이 서버에 요청하고 결과를 반영한다(역할 분리).
/// </summary>
public sealed class DeckPresetRenamePopup : UIView {
    [SerializeField] private TMP_Text titleText;
    [Tooltip("이름 입력 칸. 최대 길이는 코드가 DeckPresetRules 기준으로 맞춘다.")]
    [SerializeField] private TMP_InputField inputField;
    [Tooltip("입력 길이 표시(예: 4 / 12). 선택.")]
    [SerializeField] private TMP_Text lengthText;
    [Tooltip("비워두면 기본 이름으로 돌아간다는 안내(선택). 입력이 비었을 때만 켜진다.")]
    [SerializeField] private GameObject emptyHint;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    private UniTaskCompletionSource<string> _result;

    protected override void Awake() {
        base.Awake();
        if (okButton != null) okButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(() => Resolve(null));
        if (inputField != null) inputField.onValueChanged.AddListener(_ => RefreshInputState());
    }

    /// <param name="slot">이름을 바꿀 칸(0-based). 제목 표시에만 쓴다.</param>
    /// <param name="currentName">현재 지정된 이름(없으면 빈 문자열). 입력 칸의 초기값이 된다.</param>
    /// <returns>확인 시 입력한 이름(빈 문자열이면 기본 이름으로 되돌리기), 취소/외부 닫힘 시 null.</returns>
    public async UniTask<string> ShowAsync(int slot, string currentName) {
        if (titleText != null) titleText.text = $"프리셋 {slot + 1} 이름";

        if (inputField != null) {
            // 서버가 어차피 잘라 저장하므로 입력 단계에서 같은 길이로 막아 "쓴 대로 저장되지 않는" 경험을 없앤다.
            inputField.characterLimit = DeckPresetRules.NameMaxLength;
            inputField.SetTextWithoutNotify(currentName ?? "");
        }
        RefreshInputState();

        _result = new UniTaskCompletionSource<string>();
        await base.ShowAsync();

        // 팝업이 완전히 뜬 뒤에 포커스를 준다(연출 중에 주면 키보드가 먼저 올라와 화면이 튄다).
        if (inputField != null) inputField.ActivateInputField();

        return await _result.Task;
    }

    private void RefreshInputState() {
        string raw = inputField != null ? inputField.text : "";
        string sanitized = DeckPresetRules.Sanitize(raw);

        if (lengthText != null) lengthText.text = $"{raw.Length} / {DeckPresetRules.NameMaxLength}";
        if (emptyHint != null) emptyHint.SetActive(sanitized.Length == 0);
        // 빈 이름도 "기본으로 되돌리기"라는 유효한 선택이므로 확인 버튼을 막지 않는다.
    }

    private void Confirm() {
        string name = DeckPresetRules.Sanitize(inputField != null ? inputField.text : "");
        Resolve(name);
    }

    private void Resolve(string value) {
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }

    // 버튼 외의 경로(배경 탭, 일괄 닫힘)로 닫혀도 반드시 결과를 확정한다(취소=null).
    protected override void OnClosed() {
        _result?.TrySetResult(null);
    }
}
