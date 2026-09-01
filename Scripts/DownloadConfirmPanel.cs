using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 다운로드 동의 인씬 팝업(타이틀 씬 하이어라키 배치). 어드레서블 다운로드 "전"에 떠야 하므로 원격 자산에 의존하지 않고
/// 씬에 동봉(로컬)된다. 용량(강조색)과 예상 시간을 보여주고 동의/종료 버튼 결과를 UniTask&lt;bool&gt; 로 돌려준다.
///
/// 표시/숨김은 GameObject 활성 토글로 처리한다. 비활성 상태로 씬을 시작해도 되도록 버튼 연결은 첫 ShowAsync 에서 지연 초기화한다.
/// 클릭음을 내기 위한 사운드 서비스는 루트 스코프에서 스스로 주입받는다(RootInjectedBehaviour).
/// </summary>
public sealed class DownloadConfirmPanel : RootInjectedBehaviour {
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TextMeshProUGUI messageText;    // 용량(강조색 rich text) 포함 안내 문구
    [SerializeField] private TextMeshProUGUI estimateText;   // 예상 시간
    [SerializeField] private Button agreeButton;             // 동의 -> 다운로드
    [SerializeField] private Button quitButton;              // 종료 -> 앱 종료

    [Header("Tuning")]
    [Tooltip("예상 시간 계산용 가정 다운로드 속도(MB/s). 네트워크에 따라 다르므로 대략치.")]
    [SerializeField] private float assumedMBps = 1.5f;
    [Tooltip("용량 강조색(초록).")]
    [SerializeField] private string sizeColorHex = "#39D353";

    private UniTaskCompletionSource<bool> _tcs;
    private bool _wired;

    private ISoundService _sound;

    [Inject]
    public void Construct(ISoundService sound) => _sound = sound;

    /// <summary>용량/예상시간을 채우고 표시한 뒤, 사용자가 버튼을 누를 때까지 대기한다. true=동의, false=종료.</summary>
    public UniTask<bool> ShowAsync(long sizeBytes) {
        _tcs = new UniTaskCompletionSource<bool>();
        EnsureWired();   // 비활성으로 시작해도 동작하도록 버튼 연결을 지연 초기화한다(Awake 에 의존하지 않음).

        string size = DownloadInfo.FormatBytes(sizeBytes);
        if (messageText != null)
            messageText.text = $"<color={sizeColorHex}>{size}</color> 의 데이터를 다운로드해야 합니다.\n다운로드 하시겠습니까?";
        if (estimateText != null)
            estimateText.text = $"예상 시간: {FormatEstimate(sizeBytes)}";

        gameObject.SetActive(true);
        if (group != null) { group.alpha = 1f; group.blocksRaycasts = true; group.interactable = true; }
        return _tcs.Task;
    }

    private void Close(bool agreed) {
        _sound?.PlaySfx(SfxKey.ButtonClick);
        if (group != null) { group.blocksRaycasts = false; group.interactable = false; }
        gameObject.SetActive(false);
        _tcs?.TrySetResult(agreed);
    }

    // 버튼 연결은 한 번만. 비활성 GameObject 로 시작하면 Awake 가 실행되지 않으므로 첫 ShowAsync 에서 보장한다.
    private void EnsureWired() {
        if (_wired) return;
        if (agreeButton != null) agreeButton.onClick.AddListener(() => Close(true));
        if (quitButton != null) quitButton.onClick.AddListener(() => Close(false));
        _wired = true;
    }

    private string FormatEstimate(long sizeBytes) {
        float mb = sizeBytes / (1024f * 1024f);
        float sec = assumedMBps > 0 ? mb / assumedMBps : 0f;
        if (sec < 1f) return "약 1초";
        if (sec < 60f) return $"약 {Mathf.CeilToInt(sec)}초";
        return $"약 {Mathf.CeilToInt(sec / 60f)}분";
    }
}
