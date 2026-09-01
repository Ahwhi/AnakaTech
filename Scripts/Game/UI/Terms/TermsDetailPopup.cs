using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 약관 전문 팝업. 동의 목록이나 설정 화면에서 상세 보기를 누르면 열린다.
///
/// 본문은 앱이 그리지 않고 공식 홈페이지의 문서를 앱 내부 웹뷰로 그대로 보여 준다. 법률 문서는 오탈자 수정이나
/// 개정이 잦은데, 앱에 본문을 넣으면 스토어 심사를 거쳐야 반영되기 때문이다. 주소는 서버가 내려준다(TermsCatalog).
///
/// 네이티브 웹뷰는 유니티 화면 위에 그려져 유니티 UI 를 덮는다. 그래서 이 팝업의 조작 UI(제목/닫기/새로고침)는
/// 반드시 웹뷰 영역(viewArea) 밖에 있어야 하며, 웹뷰는 viewArea 의 화면 좌표만큼만 자리를 차지한다.
/// 화면 회전이나 해상도 변경이 있어도 매 프레임 영역을 다시 계산하므로 따로 처리할 것이 없다.
///
/// 웹뷰를 쓸 수 없는 환경(에디터, PC)에서는 이 팝업을 열지 않는다. 호출부(TermsConsentFlow)가 시스템
/// 브라우저로 대신 연다.
/// </summary>
public sealed class TermsDetailPopup : UIView {
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [Tooltip("문서를 못 불러왔을 때 안내 문구를 표시할 텍스트(선택). 오류 그룹 안에 둔다.")]
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("WebView")]
    [Tooltip("웹뷰가 표시될 영역. 이 사각형의 화면 좌표만큼 네이티브 웹뷰가 자리잡는다. " +
             "제목/닫기 버튼은 반드시 이 영역 밖에 배치할 것(웹뷰가 유니티 UI 를 덮는다).")]
    [SerializeField] private RectTransform viewArea;

    [Header("States (상호 배타적으로 켜진다)")]
    [SerializeField] private GameObject loadingGroup;
    [SerializeField] private GameObject errorGroup;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [Tooltip("문서를 못 받았을 때 다시 시도하는 버튼(오류 그룹 안).")]
    [SerializeField] private Button retryButton;
    [Tooltip("현재 문서를 다시 불러오는 버튼(선택).")]
    [SerializeField] private Button refreshButton;
    [Tooltip("앱 안에서 문서를 못 볼 때 시스템 브라우저로 여는 버튼(선택). 오류 그룹 안에 둔다.")]
    [SerializeField] private Button browserButton;

    private InAppWebView _webView;
    private Canvas _canvas;
    private string _url;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
        if (retryButton != null) retryButton.onClick.AddListener(Reload);
        if (refreshButton != null) refreshButton.onClick.AddListener(Reload);
        if (browserButton != null) browserButton.onClick.AddListener(OpenInSystemBrowser);
    }

    /// <summary>제목과 문서 주소를 받아 열고, 로드가 끝나면 웹뷰를 보여 준다.</summary>
    public async UniTask ShowAsync(string title, string url) {
        _url = url;
        if (titleText != null) titleText.text = title;

        SetState(loading: true);
        _canvas = GetComponentInParent<Canvas>();

        // 웹뷰는 팝업의 자식으로 둔다. 팝업이 파괴되면(씬 전환 포함) 네이티브도 함께 정리된다.
        _webView = new InAppWebView(transform, OnLoaded, OnFailed, OpenExternal);
        UpdateArea();
        _webView.Load(url);

        await base.ShowAsync();
    }

    private void LateUpdate() {
        // 화면 회전/해상도 변경/레이아웃 갱신을 따로 감지하지 않고 매 프레임 계산한다.
        // 값이 그대로면 네이티브 호출은 일어나지 않는다(InAppWebView.SetArea).
        UpdateArea();
    }

    /// <summary>
    /// 뒤로가기: 웹뷰 안에서 되돌아갈 페이지가 있으면 먼저 되돌리고, 더 갈 곳이 없을 때 팝업을 닫는다.
    /// 키 입력 자체는 전역 창구(BackKeyRouter)가 읽어 가장 위 팝업인 이 화면에만 전달한다.
    /// </summary>
    public override void OnBackKey() {
        if (_webView != null && _webView.CanGoBack()) {
            SetState(loading: true);
            _webView.SetVisible(false);
            _webView.GoBack();
            return;
        }

        CloseAsync().Forget();
    }

    private void UpdateArea() {
        if (_webView == null || viewArea == null) return;
        _webView.SetArea(CalculateScreenRect());
    }

    /// <summary>웹뷰 영역의 화면 좌표(픽셀). 노치/홈 인디케이터를 침범하지 않도록 안전 영역과 교차시킨다.</summary>
    private Rect CalculateScreenRect() {
        var corners = new Vector3[4];
        viewArea.GetWorldCorners(corners);

        // 오버레이 캔버스는 카메라가 없다(월드 좌표가 곧 화면 좌표).
        var cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        var safe = Screen.safeArea;
        float xMin = Mathf.Max(Mathf.Min(min.x, max.x), safe.xMin);
        float yMin = Mathf.Max(Mathf.Min(min.y, max.y), safe.yMin);
        float xMax = Mathf.Min(Mathf.Max(min.x, max.x), safe.xMax);
        float yMax = Mathf.Min(Mathf.Max(min.y, max.y), safe.yMax);

        return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
    }

    private void Reload() {
        if (_webView == null) return;
        SetState(loading: true);
        _webView.SetVisible(false);
        // 로드 자체가 막혔던 경우(잘못된 주소/차단)에는 다시 열어야 한다. Reload 는 되돌릴 페이지가 없다.
        if (_webView.IsLoaded) _webView.Reload();
        else _webView.Load(_url);
    }

    private void OnLoaded() {
        SetState();
        _webView?.SetVisible(true);
    }

    // 조회 실패는 동의와 무관하다. 문서를 못 봤다는 사실만 알리고 사용자가 다시 시도하거나 브라우저로 열게 둔다.
    private void OnFailed(string reason) {
        Debug.LogWarning($"[Terms] 약관 문서를 불러오지 못했습니다({_url}): {reason}");
        _webView?.SetVisible(false);
        if (errorText != null) errorText.text = TermsRules.DocumentLoadFailed;
        SetState(error: true);
    }

    private void OpenInSystemBrowser() => OpenExternal(_url);

    // 홈페이지 밖 링크(문서 안의 외부 링크 포함)는 앱 안에서 열지 않는다. 주소창이 없는 화면에서
    // 남의 사이트를 보여 주지 않기 위해서다.
    private static void OpenExternal(string url) {
        if (!WebLinkPolicy.IsExternalBrowserAllowed(url)) return;
        Application.OpenURL(url);
    }

    private void SetState(bool loading = false, bool error = false) {
        if (loadingGroup != null) loadingGroup.SetActive(loading);
        if (errorGroup != null) errorGroup.SetActive(error);
    }

    protected override void OnClosed() => DisposeWebView();

    private void OnDestroy() => DisposeWebView();

    private void DisposeWebView() {
        _webView?.Dispose();
        _webView = null;
    }
}
