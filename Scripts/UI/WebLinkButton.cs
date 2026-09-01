using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 웹 링크 버튼(재사용). 아무 버튼 오브젝트에 붙이고 주소를 넣으면 누를 때 기기의 기본 브라우저로 그 주소를 연다.
/// 타이틀의 공식 홈페이지/커뮤니티 링크처럼 "앱 밖으로 내보내는" 버튼이 쓴다.
///
/// 앱 안 웹뷰(InAppWebView)를 쓰지 않는 이유: 주소창이 없는 화면에서 남의 사이트를 보여 주지 않는다는
/// 기존 방침(WebLinkPolicy)과 같다. 브라우저로 넘기면 사용자가 주소를 직접 확인할 수 있고,
/// 로그인이 걸린 사이트(커뮤니티 등)도 기기에 이미 남아 있는 세션을 그대로 쓴다.
///
/// Application.OpenURL 은 안드로이드/iOS 모두 기본 브라우저를 띄우므로 플랫폼 분기가 필요 없다.
/// 다만 어떤 주소든 열어 주는 함수라, 인스펙터에 잘못 적힌 주소가 그대로 나가지 않도록
/// 다른 화면과 같은 기준(WebLinkPolicy)으로 https 인지 확인한 뒤에만 넘긴다.
///
/// 루트 스코프에서 스스로 주입받으므로(RootInjectedBehaviour) 씬 스코프에 등록하지 않는다.
/// 버튼이 여러 개여도 인스펙터 배선은 주소를 적는 것뿐이다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class WebLinkButton : RootInjectedBehaviour {
    [SerializeField] private Button button;

    [Tooltip("열 주소. https 로 시작해야 하며, 기기의 기본 브라우저에서 열린다.")]
    [SerializeField] private string url;

    private ISoundService _sound;

    [Inject]
    public void Construct(ISoundService sound) {
        _sound = sound;
    }

    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(Open);
    }

    /// <summary>여는 주소를 코드에서 정할 때 쓴다(공지 배너처럼 주소가 서버에서 오는 경우).</summary>
    public void SetUrl(string value) => url = value;

    private void Open() {
        // 주소가 비었거나 https 가 아니면 아무 일도 하지 않는다. 조용히 넘어가면 배선 실수를 알 수 없으므로 남긴다.
        if (!WebLinkPolicy.IsExternalBrowserAllowed(url)) {
            Debug.LogWarning($"[WebLink] 열 수 없는 주소입니다(https 만 허용): {url}", this);
            return;
        }

        _sound?.PlaySfx(SfxKey.ButtonClick);
        Application.OpenURL(url);
    }

    private void OnDestroy() {
        if (button != null) button.onClick.RemoveListener(Open);
    }
}
