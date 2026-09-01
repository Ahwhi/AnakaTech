using System;
using Gree.UnityWebView;
using UnityEngine;

/// <summary>
/// 앱 내부 웹뷰 한 개를 감싸는 래퍼(gree/unity-webview 의 WebViewObject 사용).
///
/// 네이티브 웹뷰는 유니티 화면 "위"에 그려지는 별도의 뷰라서, 유니티 UI 로는 가릴 수도 없고 캔버스 정렬 순서도
/// 통하지 않는다. 그래서 화면에서 웹뷰가 차지할 영역(여백)을 호출부가 직접 정해 줘야 하고, 닫기 버튼 같은
/// 조작 UI 는 그 영역 밖에 있어야 한다. 이 클래스는 그 규칙을 강제하기 위해 표시 영역을 픽셀 사각형으로만 받는다.
///
/// 생성/해제를 호출부가 실수로 겹치지 않게 인스턴스는 앱 전체에서 하나만 유지한다(새로 만들면 이전 것을 정리한다).
/// 웹뷰를 지원하지 않는 플랫폼(에디터, PC 빌드)에서는 IsSupported 가 false 이며, 이때는 호출부가 시스템
/// 브라우저로 대신 열어야 한다. 지원하지 않는 곳에서 네이티브를 호출하면 DLL 로드 오류로 에디터가 죽는다.
/// </summary>
public sealed class InAppWebView : IDisposable {
    /// <summary>이 플랫폼에서 앱 내부 웹뷰를 쓸 수 있는지. false 면 시스템 브라우저로 대신 연다.</summary>
    public static bool IsSupported {
        get {
#if UNITY_EDITOR
            // 에디터에는 안정적인 네이티브 웹뷰가 없다(윈도우 에디터는 DLL 로드 자체가 위험하다).
            return false;
#elif UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return false;
#endif
        }
    }

    private static InAppWebView _current;

    private readonly Action _onLoaded;
    private readonly Action<string> _onFailed;
    private readonly Action<string> _onExternalLink;

    private GameObject _hostObject;
    private WebViewObject _webView;
    private RectInt _lastArea;
    private bool _disposed;

    /// <summary>현재 페이지 로드가 끝났는지. 뒤로가기/새로고침 가능 여부 판단에 쓴다.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// 웹뷰를 만들고 콜백을 연결한다. 실제 표시는 SetArea + SetVisible 로 시작한다.
    /// </summary>
    /// <param name="parent">웹뷰 오브젝트를 매달 부모(보통 이 웹뷰를 여는 팝업). 부모가 파괴되면 함께 정리된다.</param>
    /// <param name="onLoaded">페이지 로드 완료.</param>
    /// <param name="onFailed">네트워크/HTTP 오류. 인자는 원인 문자열.</param>
    /// <param name="onExternalLink">우리 홈페이지 밖 링크를 눌렀을 때. 앱 안에서 열지 않고 호출부에 넘긴다.</param>
    public InAppWebView(Transform parent, Action onLoaded, Action<string> onFailed, Action<string> onExternalLink) {
        _onLoaded = onLoaded;
        _onFailed = onFailed;
        _onExternalLink = onExternalLink;

        // 동시에 두 개가 떠 있으면 어느 쪽이 화면을 덮는지 알 수 없다. 항상 마지막 하나만 남긴다.
        _current?.Dispose();
        _current = this;

        if (!IsSupported) return;

        // 시스템 웹뷰가 비활성화/삭제된 안드로이드 기기가 실제로 있다. 이때는 만들지 않고 브라우저로 유도한다.
        if (!WebViewObject.IsWebViewAvailable()) {
            _onFailed?.Invoke("WebView unavailable");
            return;
        }

        // 네이티브 플러그인이 게임오브젝트 이름으로 콜백을 보낸다(UnitySendMessage). 이름은 유일해야 한다.
        _hostObject = new GameObject("AnakaInAppWebView");
        if (parent != null) _hostObject.transform.SetParent(parent, false);

        _webView = _hostObject.AddComponent<WebViewObject>();

        _webView.Init(
            err: OnError,
            httpErr: OnError,
            ld: OnLoaded,
            started: OnStarted,
            hooked: OnHooked,
            transparent: false,
            // 약관 본문은 작은 글씨가 많아 확대가 필요하다.
            zoom: true,
            // 우리 문서는 밝은 배경 기준으로 만들어졌다. 시스템 다크 모드가 색을 뒤집지 않게 끈다.
            androidForceDarkMode: 1,
            // iOS 는 UIWebView 가 아니라 WKWebView 를 쓴다(구버전 API 는 심사에서 거절된다).
            enableWKWebView: true);

        // 안드로이드 웹뷰는 시스템 글꼴 크기 설정을 그대로 곱해 글자를 키운다(크롬은 자체 설정을 따로 쓴다).
        // 그래서 같은 문서가 브라우저에서는 정상인데 앱 안에서만 크게 보인다. 우리 문서는 글자 크기를 스스로 정하고
        // 확대는 핀치로 할 수 있으므로, 기준 배율(100%)로 고정해 브라우저와 같은 크기로 맞춘다.
        _webView.SetTextZoom(100);

        // 페이지 안의 링크까지 같은 기준으로 막는다. C# 검사만으로는 최초 주소만 통제할 수 있다.
        if (!_webView.SetURLPattern(WebLinkPolicy.AllowPattern, WebLinkPolicy.DenyPattern, WebLinkPolicy.HookPattern))
            Debug.LogWarning("[WebView] URL 패턴을 적용하지 못했습니다. 외부 링크 차단이 동작하지 않습니다.");

        _webView.SetVisibility(false);
    }

    /// <summary>문서를 연다. 허용되지 않은 주소면 열지 않고 실패로 알린다.</summary>
    public void Load(string url) {
        if (_disposed) return;

        if (!WebLinkPolicy.IsInAppAllowed(url)) {
            Debug.LogWarning($"[WebView] 허용되지 않은 주소입니다: {url}");
            _onFailed?.Invoke("Blocked URL");
            return;
        }

        IsLoaded = false;
        _webView?.LoadURL(url);
    }

    /// <summary>웹뷰가 차지할 화면 영역(픽셀, 좌하단 원점). 노치 영역은 호출부가 제외한 값을 넘긴다.</summary>
    public void SetArea(Rect screenRect) {
        if (_disposed || _webView == null) return;

        var area = new RectInt(
            Mathf.RoundToInt(screenRect.xMin),
            Mathf.RoundToInt(screenRect.yMin),
            Mathf.RoundToInt(screenRect.width),
            Mathf.RoundToInt(screenRect.height));
        if (area.Equals(_lastArea)) return;   // 매 프레임 네이티브를 호출하지 않는다.
        _lastArea = area;

        // 플러그인은 화면 가장자리로부터의 여백을 받는다(상단 기준이 y=0 이라 위아래가 뒤집힌다).
        _webView.SetMargins(
            left:   area.xMin,
            top:    Screen.height - area.yMax,
            right:  Screen.width - area.xMax,
            bottom: area.yMin);
    }

    public void SetVisible(bool visible) {
        if (_disposed) return;
        _webView?.SetVisibility(visible);
    }

    /// <summary>웹뷰 안에서 뒤로 갈 페이지가 있는지(안드로이드 뒤로가기 처리용).</summary>
    public bool CanGoBack() => !_disposed && _webView != null && _webView.CanGoBack();

    public void GoBack() {
        if (_disposed) return;
        _webView?.GoBack();
    }

    public void Reload() {
        if (_disposed) return;
        IsLoaded = false;
        _webView?.Reload();
    }

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;

        if (_current == this) _current = null;
        if (_hostObject != null) UnityEngine.Object.Destroy(_hostObject);   // WebViewObject 가 네이티브를 함께 정리한다.
        _hostObject = null;
        _webView = null;
    }

    private void OnStarted(string url) => IsLoaded = false;

    private void OnLoaded(string url) {
        IsLoaded = true;
        _onLoaded?.Invoke();
    }

    private void OnError(string message) {
        IsLoaded = false;
        _onFailed?.Invoke(message);
    }

    // 우리 홈페이지 밖 링크. 앱 안에서 열면 주소를 볼 수 없는 화면에서 남의 사이트를 보게 되므로
    // 여기서는 열지 않고, 사용자가 직접 누른 링크라는 사실만 호출부에 넘긴다.
    private void OnHooked(string url) => _onExternalLink?.Invoke(url);
}
