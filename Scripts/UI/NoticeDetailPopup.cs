using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게시글 상세 팝업(UIView). 제목/작성자/작성일/본문을 보여주고, 이미지가 있으면 RemoteImage 로 받아 표시한다.
/// 이벤트 글이면 진행 기간도 함께 노출한다.
/// </summary>
public sealed class NoticeDetailPopup : UIView {
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI authorText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI contentText;
    [Tooltip("이벤트 진행 기간(이벤트가 아니거나 기간이 없으면 숨김).")]
    [SerializeField] private TextMeshProUGUI periodText;

    [Header("Image")]
    [Tooltip("본문 이미지(없으면 자동 숨김).")]
    [SerializeField] private RemoteImage image;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    [Header("Content Markdown")]
    [Tooltip("본문의 마크다운 표현 설정(제목 크기/목록 들여쓰기/문단 여백 등). " +
             "본문은 원격에 마크다운으로 보관되므로 그대로 뿌리면 '#' 과 '**' 가 글자로 보인다.")]
    [SerializeField] private MarkdownRichTextStyle contentStyle = new();

    [Header("Label Style (작성자 / 작성일 / 기간)")]
    [Tooltip("'작성일:' 같은 라벨 부분을 굵게 표시할지. 값 부분은 텍스트 컴포넌트의 기본 굵기를 그대로 쓴다.")]
    [SerializeField] private bool boldLabel = true;
    [Tooltip("라벨에 색을 따로 줄지. 끄면 텍스트 컴포넌트의 색을 그대로 쓴다.")]
    [SerializeField] private bool useLabelColor;
    [SerializeField] private Color labelColor = Color.white;
    [Tooltip("값에 색을 따로 줄지. 끄면 텍스트 컴포넌트의 색을 그대로 쓴다.")]
    [SerializeField] private bool useValueColor;
    [SerializeField] private Color valueColor = Color.white;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    /// <summary>데이터로 내용을 채우고 페이드 인한 뒤, 이미지가 있으면 비동기로 로드한다.</summary>
    public async UniTask ShowAsync(NoticeData data) {
        if (titleText != null) titleText.text = data.title;
        if (authorText != null) authorText.text = Labeled("작성자", data.author);
        if (dateText != null) dateText.text = Labeled("작성일", data.CreatedAtKst.ToString("yyyy.MM.dd HH:mm"));
        // 본문만 마크다운을 해석한다. 원격에 마크다운으로 보관되는 것은 본문뿐이고,
        // 아래 라벨 줄들은 화면이 만드는 정보라 문서 표기와 섞이면 안 된다.
        if (contentText != null) contentText.text = MarkdownRichText.ToRichText(data.content, contentStyle);

        if (periodText != null) {
            // 기간 문구 규칙(한국 시간, 종료일 미정 표기)은 이벤트 페이지 헤더와 같은 EventPeriod 를 쓴다.
            string period = data.Category == NoticeCategory.Event ? EventPeriod.Format(data) : string.Empty;
            bool showPeriod = !string.IsNullOrEmpty(period);
            periodText.gameObject.SetActive(showPeriod);
            if (showPeriod) periodText.text = Labeled("기간", period);
        }

        await base.ShowAsync();

        // 활성화된 뒤에 강제로 메시/레이아웃을 다시 계산해 ContentSizeFitter 가 본문 크기를 즉시 반영하게 한다.
        if (contentText != null) {
            contentText.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentText.rectTransform);
        }

        if (image != null) await image.LoadAsync(data.imageUrl, this.GetCancellationTokenOnDestroy());
    }

    /// <summary>
    /// "라벨: 값" 한 줄을 만든다. 라벨은 무엇을 가리키는지 알려 주는 안내라 굵게 두고,
    /// 값은 실제로 읽는 대상이라 본문과 같은 굵기로 남긴다.
    ///
    /// 본문 마크다운과 따로 처리하는 이유: 본문은 원격 문서라 표현이 문서에 딸려 오지만,
    /// 이 줄들은 게시글 정보로 화면이 만들어 내는 것이라 문서 표기의 영향을 받으면 안 된다.
    /// </summary>
    private string Labeled(string label, string value) {
        string head = $"{label}:";
        if (boldLabel) head = $"<b>{head}</b>";
        if (useLabelColor) head = $"<color=#{ColorUtility.ToHtmlStringRGBA(labelColor)}>{head}</color>";

        string body = value ?? string.Empty;
        if (useValueColor) body = $"<color=#{ColorUtility.ToHtmlStringRGBA(valueColor)}>{body}</color>";

        return $"{head} {body}";
    }

    protected override void OnClosed() {
        if (image != null) image.Clear();
    }
}
