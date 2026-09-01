using TMPro;
using UniRx;
using UnityEngine;
using VContainer;
using static Enums;

/// <summary>
/// 로그인 완료(부트 Ready) 시 인스펙터에 할당한 패널을 보이게 하는 컴포넌트(타이틀 씬용).
/// 부트 상태(IBootStatus)를 구독해 Ready 일 때만 패널을 켜고, 그 외(로그인 전/재로그인 등)에는 끈다.
/// Ready 로 켜질 때 로그인 세션 정보(닉네임, UID, 로그인 방식)를 정보 텍스트에 출력한다.
///
/// 인스펙터에서 패널을 활성/비활성 어느 상태로 두든, 시작 시 강제로 숨기고 Ready 에서 노출한다.
/// 이 컴포넌트는 항상 켜져 있는 오브젝트(예: Canvas/매니저)에 두고, 패널만 참조로 연결한다.
/// 씬 LifetimeScope 에 RegisterComponentInHierarchy 로 등록해야 주입된다.
/// </summary>
public sealed class LoginCompletePanel : MonoBehaviour {
    [Tooltip("로그인 완료 시 보일 버튼 패널. 평소엔 숨긴다.")]
    [SerializeField] private GameObject panel;

    [Tooltip("패널 자식의 로그인 정보 텍스트(닉네임/UID/로그인 방식). 선택 사항.")]
    [SerializeField] private TMP_Text informationText;

    // 정보 텍스트 색(항목 이름 / 값 / 아직 값이 없는 항목).
    private const string LabelColor = "#C6C6C6";
    private const string ValueColor = "#FFFFFF";
    private const string EmptyColor = "#8A8A8A";

    private IBootStatus _boot;
    private ILoginService _login;

    [Inject]
    public void Construct(IBootStatus boot, ILoginService login) {
        _boot = boot;
        _login = login;
    }

    private void Awake() {
        // 인스펙터 초기 상태와 무관하게 시작은 숨김.
        if (panel != null) panel.SetActive(false);
    }

    private void Start() {
        _boot.State
            .Subscribe(state => {
                var ready = state == BootState.Ready;
                if (panel != null) panel.SetActive(ready);
                if (ready) RefreshInformation();
            })
            .AddTo(this);
    }

    // 로그인 세션 정보를 정보 텍스트에 출력한다.
    // UID 로는 서버가 "공개 식별자"로 설계한 GUID(PlayerGuid)를 쓴다.
    //   - 서버 내부 PK(UserId, 순차 정수)는 가입자 수 추정/열거(IDOR) 우려로 외부 노출 금지(서버 DataModel 주석).
    //   - GUID 는 추측 불가능하므로 클라/CS/방송 노출에 안전하다.
    // 세션 토큰(SessionToken)은 자동 재로그인 자격증명이므로 절대 출력하지 않는다.
    //
    // 닉네임은 로그인 응답(S_Login)이 내려준 값을 쓴다. 타이틀은 메인 진입 전이라 계정 스냅샷
    // (S_MainData)이 아직 없고, 첫 메인 진입에서 지갑이 만들어지는 신규 계정은 닉네임 자체가 없다.
    private void RefreshInformation() {
        if (informationText == null || _login == null || !_login.IsLoggedIn) return;

        informationText.text =
            Line("닉네임", _login.Nickname, "미설정") + "\n" +
            Line("UID", _login.PlayerGuid) + "\n" +
            Line("로그인 방식", _login.Provider.ToString());
    }

    // "이름: 값" 한 줄. 값이 비어 있으면 대체 문구를 흐린 색으로 보여 준다(빈 줄로 남기지 않는다).
    private static string Line(string label, string value, string emptyText = "-") {
        bool empty = string.IsNullOrEmpty(value);
        string color = empty ? EmptyColor : ValueColor;
        string body = empty ? emptyText : value;
        return $"<color={LabelColor}>{label}:</color> <color={color}>{body}</color>";
    }
}
