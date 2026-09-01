using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 로그인 결과(성공/실패 + 세션 정보). UI 에 반환하는 값 타입.
/// </summary>
public readonly struct LoginResult {
    public bool IsSuccess { get; }
    public LoginFailReason FailReason { get; }
    public long UserId { get; }
    public string PlayerGuid { get; }
    public bool IsNewUser { get; }

    private LoginResult(bool ok, LoginFailReason reason, long userId, string playerGuid, bool isNewUser) {
        IsSuccess = ok;
        FailReason = reason;
        UserId = userId;
        PlayerGuid = playerGuid;
        IsNewUser = isNewUser;
    }

    public static LoginResult Success(long userId, string playerGuid, bool isNewUser)
        => new(true, LoginFailReason.None, userId, playerGuid, isNewUser);

    public static LoginResult Fail(LoginFailReason reason)
        => new(false, reason, 0, null, false);
}

/// <summary>
/// 소셜 로그인 오케스트레이터. ILoginService 로 주입받아 사용한다.
///   1) provider별 네이티브 인증(ISocialAuthProvider)으로 토큰 획득
///   2) C_Login 송신 후 S_Login 을 WaitPacketAsync 로 대기 (S_Version 과 동일 패턴)
///   3) 세션 정보 보관 + 이벤트 발행
/// 로그인 UI 는 버튼 탭에서 LoginAsync(provider) 만 호출하면 된다.
/// 부트 LoginStep 은 WaitUntilLoggedInAsync 로 성공까지 대기.
/// </summary>
public sealed class LoginManager : MonoBehaviour, ILoginService {
    private const float ServerTimeoutSeconds = 10f;

    private readonly Dictionary<LoginProvider, ISocialAuthProvider> _providers = new();

    private INetworkService _net;

    private UniTaskCompletionSource _loggedInTcs;
    private int _busy; // 0/1 재진입 가드

    public bool IsLoggedIn { get; private set; }
    public long UserId { get; private set; }
    public string PlayerGuid { get; private set; }
    public string SessionToken { get; private set; }
    public string Nickname { get; private set; } = string.Empty;
    public bool IsNewUser { get; private set; }
    public LoginProvider Provider { get; private set; }

    /// <summary>인증 시작(=스피너 표시 등). provider 전달.</summary>
    public event Action<LoginProvider> LoginStarted;
    /// <summary>성공/실패 모두 발행. UI 는 이걸 구독해 결과 화면을 분기.</summary>
    public event Action<LoginResult> LoginCompleted;

    [Header("Social Auth")]
    [Tooltip("소셜 로그인 클라이언트 ID 설정 에셋(Create > Anaka > Social Auth Config). 에디터 목 로그인에는 필요 없다.")]
    [SerializeField] private SocialAuthConfig socialAuthConfig;

    private void Awake() {
        RegisterProvider(new GoogleAuthProvider(socialAuthConfig));
        RegisterProvider(new AppleAuthProvider());
        RegisterProvider(new NaverAuthProvider(socialAuthConfig));
        // 개발용 임시 계정. 운영 빌드에서는 AuthenticateAsync 가 거절하고 로그인 UI 도 버튼을 숨긴다.
        RegisterProvider(new GuestAuthProvider());
    }

    private void RegisterProvider(ISocialAuthProvider p) => _providers[p.Provider] = p;

    /// <summary>
    /// VContainer 가 네트워크 서비스를 메서드 주입한다(형제 매니저를 전역 싱글톤으로 끌어오지 않는 단방향 배선).
    /// 연결이 끊기면 서버 세션이 사라지므로 로그인 상태를 무효화한다 -> 타이틀 복귀 후 재부팅 시 LoginStep 이 다시 로그인을 기다린다.
    /// </summary>
    [Inject]
    public void Construct(INetworkService net) {
        _net = net;
        _net.Disconnected += OnNetworkDisconnected;
    }

    private void OnDestroy() {
        if (_net != null) _net.Disconnected -= OnNetworkDisconnected;
    }

    private void OnNetworkDisconnected() {
        IsLoggedIn = false;

        // 로그인 대기(부트 LoginStep)는 취소하지 않는다.
        // 소셜 로그인은 브라우저를 다녀오는 동안 몇 분이 걸릴 수 있고 그 사이 연결이 끊기는 것은 흔한 일인데,
        // 여기서 대기를 취소하면 사용자가 버튼을 누르기도 전에 부트가 "서버에 연결할 수 없습니다"로 죽어 버린다.
        // 대신 로그인 버튼을 누르는 시점에 연결을 다시 확보한다(LoginAsync 참고).
        // TODO: SessionToken 영속화 시 토큰으로 무중단 재로그인(silent re-login)을 시도할 수 있다.
    }

    // ── 부트 LoginStep 용: 로그인 성공까지 대기 ─────────────────
    public UniTask WaitUntilLoggedInAsync(CancellationToken ct) {
        if (IsLoggedIn) return UniTask.CompletedTask;

        // 부트가 로그인을 기다릴 때마다 대기 토큰을 새로 만든다.
        // 앞선 부트에서 완료된 토큰을 재사용하면(로그아웃 후 타이틀 복귀) 로그인하지 않았는데도 이 단계가 즉시
        // 통과해 로그인 화면을 건너뛴다. 그 뒤 단계는 인증되지 않은 세션으로 서버 응답을 기다리다 타임아웃한다.
        _loggedInTcs = new UniTaskCompletionSource();
        return _loggedInTcs.Task.AttachExternalCancellation(ct);
    }

    // ── 자동 로그인 ─────────────────────────────────────────────
    /// <summary>
    /// 저장된 세션 토큰으로 소셜 인증 없이 로그인한다. 토큰이 없거나 서버가 거절하면 false 를 돌려주고,
    /// 호출부(부트)는 평소대로 로그인 화면을 띄운다. 실패 자체는 정상 흐름이라 오류로 다루지 않는다.
    /// </summary>
    public async UniTask<bool> TryAutoLoginAsync(CancellationToken ct = default) {
        if (IsLoggedIn) return true;
        if (!SessionTokenStore.HasToken) return false;

        if (Interlocked.Exchange(ref _busy, 1) == 1) return false;
        try {
            var provider = SessionTokenStore.Provider;
            LoginStarted?.Invoke(provider);

            var res = await SendLoginAsync(provider, authToken: string.Empty,
                                           sessionToken: SessionTokenStore.Token, ct);
            if (res == null) return false;

            if (res.resultCode != 0) {
                // 만료/폐기된 토큰이다. 남겨 두면 매번 실패만 반복하므로 지운다.
                SessionTokenStore.Clear();
                LoginCompleted?.Invoke(LoginResult.Fail((LoginFailReason)res.resultCode));
                return false;
            }

            Complete(res, provider);
            return true;
        } finally {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    // ── 로그인 UI 버튼에서 호출 ─────────────────────────────────
    public async UniTask<LoginResult> LoginAsync(LoginProvider provider, CancellationToken ct = default) {
        if (IsLoggedIn)
            return LoginResult.Success(UserId, PlayerGuid, IsNewUser);

        // 중복 탭 방지(여러 provider 버튼 연타 등).
        if (Interlocked.Exchange(ref _busy, 1) == 1)
            return LoginResult.Fail(LoginFailReason.Busy);

        try {
            if (!_providers.TryGetValue(provider, out var social))
                return Fail(LoginResult.Fail(LoginFailReason.UnsupportedProvider));

            LoginStarted?.Invoke(provider);

            // 1) 네이티브 소셜 인증 — 사용자 상호작용이라 짧은 타임아웃 없이 ct 로만 취소.
            SocialAuthResult auth;
            try {
                auth = await social.AuthenticateAsync(ct);
            } catch (OperationCanceledException) when (!ct.IsCancellationRequested) {
                return Fail(LoginResult.Fail(LoginFailReason.Cancelled));
            } catch (Exception e) {
                Debug.LogException(e);
                return Fail(LoginResult.Fail(LoginFailReason.ProviderError));
            }

            // 2) 서버 검증. 소셜 토큰만 보내고 세션 토큰은 비운다(새로 발급받는 경로).
            var res = await SendLoginAsync(provider, auth.Token, sessionToken: string.Empty, ct);
            if (res == null) return Fail(LoginResult.Fail(LoginFailReason.Timeout));

            if (res.resultCode != 0)
                return Fail(LoginResult.Fail((LoginFailReason)res.resultCode));

            return Complete(res, provider);
        } finally {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    /// <summary>
    /// 연결을 확보하고 C_Login 을 보낸 뒤 S_Login 을 기다린다. 연결 실패/응답 없음이면 null.
    /// 자동 로그인과 소셜 로그인이 같은 경로를 쓰도록 보내는 부분만 떼어 둔다.
    /// </summary>
    private async UniTask<S_Login> SendLoginAsync(LoginProvider provider, string authToken, string sessionToken, CancellationToken ct) {
        // 브라우저를 다녀오는 사이 연결이 끊겼을 수 있으므로, 보내기 전에 살아 있는지 확인한다.
        // 끊긴 소켓으로 보내면 서버에는 아무 기록도 남지 않고 클라만 응답을 기다리다 타임아웃한다.
        if (!_net.IsConnected) {
            try {
                await _net.ConnectAsync(ct);
            } catch (OperationCanceledException) when (!ct.IsCancellationRequested) {
                return null;
            } catch (Exception e) {
                Debug.LogWarning($"[Login] 재연결 실패: {e.Message}");
                return null;
            }
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(ServerTimeoutSeconds));

        _net.Send(new C_Login {
            provider     = (int)provider,
            authToken    = authToken ?? string.Empty,
            sessionToken = sessionToken ?? string.Empty,
        }.Write());

        try {
            return await _net.WaitPacketAsync<S_Login>(cts.Token);
        } catch (OperationCanceledException) when (!ct.IsCancellationRequested) {
            return null;
        }
    }

    // 로그인 성공 확정. 두 경로(소셜/자동)가 같은 뒷처리를 하도록 한곳에 모은다.
    private LoginResult Complete(S_Login res, LoginProvider provider) {
        IsLoggedIn = true;
        UserId = res.userId;
        PlayerGuid = res.playerGuid;
        SessionToken = res.sessionToken;
        Nickname = res.nickname ?? string.Empty;
        IsNewUser = res.isNewUser;
        Provider = provider;

        // 로컬 세이브는 기기당 하나라, 계정별 항목(NEW 배지 봤음/이벤트 열람/카메라 포커스 등)이
        // 다른 계정과 섞이지 않도록 지금 계정을 알려 준다. 이후 UserDataManager.Account 가 이 계정 칸을 가리킨다.
        UserDataManager.SetAccount(PlayerGuid);

        // 다음 실행에서 소셜 인증 없이 들어올 수 있도록 발급받은 토큰을 남긴다.
        // 서버는 쓸 때마다 새 토큰으로 바꿔 주므로 여기서도 매번 갱신한다.
        SessionTokenStore.Save(res.sessionToken, provider);

        var ok = LoginResult.Success(UserId, PlayerGuid, IsNewUser);
        _loggedInTcs?.TrySetResult(); // 부트 LoginStep 깨우기
        LoginCompleted?.Invoke(ok);
        return ok;
    }

    private LoginResult Fail(LoginResult r) {
        LoginCompleted?.Invoke(r);
        return r;
    }
}
