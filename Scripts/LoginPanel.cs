using System;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UniRx;
using VContainer;
using static Enums;
using TMPro;

/// <summary>
/// 로그인 패널 예시(씬 전용 UI).
/// - BootState.LoggingIn 에 진입하면 자신을 켜고(로그인 버튼 노출), 그 외 상태에서는 끈다.
/// - 버튼 탭 시 LoginStarted 가 발행되면 "로그인 중입니다..." 전역 로딩 오버레이를 띄운다
///   (실제 인증 라운드트립이 일어나는 구간). 완료(성공/실패) 시 오버레이를 닫는다.
/// - 결과는 LoginCompleted 이벤트로 받아 실패 시 메시지 표시.
///
/// 하이어라키에서 이 오브젝트를 활성/비활성 어느 상태로 시작하든 동일하게 동작한다.
/// 비결: 구독/리스너 연결을 Start 가 아니라 [Inject] Construct 에서 한다.
///   - Start 는 비활성 오브젝트에서 절대 호출되지 않지만,
///   - VContainer 의 주입(Construct)은 비활성 오브젝트에도 호출되고,
///   - UniRx 구독 콜백은 활성 여부와 무관하게 발화되므로 LoggingIn 진입 시 스스로 SetActive(true) 로 켜진다.
/// 따라서 로그인 버튼 등 보이는 UI 는 이 오브젝트(또는 그 자식)에 두면 된다.
/// </summary>
public sealed class LoginPanel : MonoBehaviour {
    [SerializeField] private Button googleButton;
    [SerializeField] private Button appleButton;
    [SerializeField] private Button naverButton;
    [Tooltip("개발용 임시 계정 버튼(선택). 누를 때마다 새 계정이 만들어지고 같은 계정으로 다시 접속할 수 없다. " +
             "개발 빌드가 아니면 코드가 자동으로 숨긴다.")]
    [SerializeField] private Button guestButton;
    [Tooltip("실패 사유 등 표시(선택). 평소에는 보일 것이 없으므로 씬에서 꺼 두어도 된다. " +
             "보여 줄 문구가 생기면 코드가 켜고, 문구가 없어지면 다시 끈다.")]
    [SerializeField] private TextMeshProUGUI messageText;

    private IDisposable _loginLoading;   // "로그인 중" 오버레이 핸들

    private ILoginService _login;
    private IUIService _ui;
    private UIPopupCatalog _popups;
    private ISoundService _sound;

    // 주입(Construct)은 비활성 오브젝트에도 호출되므로, 모든 배선을 여기서 한다.
    [Inject]
    public void Construct(ILoginService login, IUIService ui, IBootStatus boot, UIPopupCatalog popups, ISoundService sound) {
        _login = login;
        _ui = ui;
        _popups = popups;
        _sound = sound;

        googleButton.onClick.AddListener(() => Login(LoginProvider.Google));
        appleButton .onClick.AddListener(() => Login(LoginProvider.Apple));
        naverButton .onClick.AddListener(() => Login(LoginProvider.Naver));

#if !UNITY_EDITOR && !UNITY_IOS
        // 애플 로그인은 iOS 에서만 제공한다. 다른 플랫폼에는 인증 경로가 없어 눌러도 실패만 하므로 아예 감춘다.
        // (애플이 요구하는 것도 "다른 소셜 로그인을 제공하는 iOS 앱"에 한정된다)
        if (appleButton != null) appleButton.gameObject.SetActive(false);
#endif

        // 임시 계정은 개발 빌드에서만 성립한다(서버도 Debug 빌드에서만 목 토큰을 통과시킨다).
        // 운영 빌드에 버튼이 남아 눌리는 일이 없도록 여기서 아예 숨긴다.
        if (guestButton != null) {
            guestButton.gameObject.SetActive(GuestAuthProvider.IsAvailable);
            guestButton.onClick.AddListener(() => Login(LoginProvider.Guest));
        }

        _login.LoginStarted += OnLoginStarted;
        _login.LoginCompleted += OnLoginCompleted;

        // 부트 상태 구독. 구독 즉시 현재 값으로 1회 발화되어 시작 가시성이 결정된다(보통 숨김).
        boot.State.Subscribe(OnBootStateChanged).AddTo(this);
    }

    private void OnDestroy() {
        _loginLoading?.Dispose();
        if (_login != null) {
            _login.LoginStarted -= OnLoginStarted;
            _login.LoginCompleted -= OnLoginCompleted;
        }
    }

    private void OnBootStateChanged(BootState state) {
        bool show = state == BootState.LoggingIn;
        // 자기 자신을 토글한다. 꺼져 있어도 이 구독 콜백이 호출되므로 LoggingIn 에 스스로 켜진다.
        if (gameObject.activeSelf != show) gameObject.SetActive(show);
        if (show) SetInteractable(true);
    }

    private void Login(LoginProvider provider) {
        _sound?.PlaySfx(SfxKey.ButtonClick);
        SetMessage(null);
        // Forget(): 결과는 LoginCompleted 이벤트로 처리하므로 await 불필요.
        _login.LoginAsync(provider).Forget();
    }

    // 인증 시작: 실제 "로그인 중" 구간. 전역 로딩 오버레이로 입력을 막고 진행 표시.
    private void OnLoginStarted(LoginProvider provider) {
        SetInteractable(false);
        _loginLoading?.Dispose();
        _loginLoading = _ui.ShowLoading("로그인 중입니다...");
    }

    private void OnLoginCompleted(LoginResult r) {
        _loginLoading?.Dispose();
        _loginLoading = null;

        if (r.IsSuccess) {
            // 성공: 부트 LoginStep 이 자동으로 풀려 다음 단계(Ready)로 진행된다.
            // 패널은 OnBootStateChanged(LoggingIn 이탈) 에서 자동으로 꺼짐.
            return;
        }

        SetInteractable(true); // 재시도 가능하게

        // 점검은 "연결 실패"가 아니라 "연결은 됐으나 로그인 거부" 상태. 별도 점검 팝업으로 안내한다.
        if (r.FailReason == LoginFailReason.Maintenance) {
            ShowMaintenancePopupAsync().Forget();
            return;
        }

        SetMessage(ToMessage(r.FailReason));
    }

    // 보여 줄 문구가 있을 때만 텍스트 오브젝트를 켠다.
    // 하이어라키에서 꺼 둔 채로 시작해도 메시지가 보여야 하고, 반대로 문구가 없을 때 빈 줄이 자리를 차지해서도 안 된다.
    private void SetMessage(string message) {
        if (messageText == null) return;
        messageText.text = message ?? string.Empty;
        bool show = !string.IsNullOrEmpty(message);
        if (messageText.gameObject.activeSelf != show) messageText.gameObject.SetActive(show);
    }

    private async UniTaskVoid ShowMaintenancePopupAsync() {
        var popup = await _ui.ShowPopupAsync<ConfirmPopup>(_popups.ConfirmPopup);
        // TODO: 서버가 점검 메시지/종료시각을 내려주면(S_Login 확장) 여기서 함께 표시.
        await popup.ShowAsync("서버 점검", "현재 서버 점검 중입니다.\n잠시 후 다시 시도해주세요.");
    }

    private void SetInteractable(bool on) {
        googleButton.interactable = on;
        appleButton.interactable = on;
        naverButton.interactable = on;
        if (guestButton != null) guestButton.interactable = on;
    }

    private static string ToMessage(LoginFailReason reason) => reason switch {
        LoginFailReason.Cancelled    => "로그인이 취소되었습니다.",
        LoginFailReason.Timeout      => "서버 응답이 없습니다. 다시 시도해주세요.",
        LoginFailReason.InvalidToken => "인증에 실패했습니다.",
        LoginFailReason.Banned       => "이용이 제한된 계정입니다.",
        LoginFailReason.Maintenance  => "서버 점검 중입니다. 잠시 후 다시 시도해주세요.",
        _                            => "로그인에 실패했습니다. 다시 시도해주세요.",
    };
}
