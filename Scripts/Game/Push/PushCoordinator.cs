using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>원격 알림 사용 여부를 실제 상태와 맞추는 창구.</summary>
public interface IPushCoordinator {
    /// <summary>
    /// 지금의 동의 상태와 OS 권한을 보고 서버 등록을 맞춘다.
    /// 로그인 직후, 동의 변경 직후, 앱 복귀 시에 부른다. 여러 번 불러도 안전하다.
    /// </summary>
    UniTask SyncAsync(CancellationToken ct = default);

    /// <summary>
    /// 사용자가 알림을 켜려 할 때. OS 권한을 요청한 뒤 등록까지 한다.
    /// 권한이 거부되면 false 를 돌려주고 서버에는 아무것도 등록하지 않는다.
    /// </summary>
    UniTask<bool> EnableAsync(CancellationToken ct = default);

    /// <summary>로그아웃/계정 전환 전에 이 기기의 등록을 지운다.</summary>
    UniTask DisableAsync(CancellationToken ct = default);

    /// <summary>알림을 눌러 앱이 열렸을 때의 이동 대상(PushRules 의 action). 아직 없으면 빈 문자열.</summary>
    string TakePendingAction();
}

/// <summary>
/// 동의 + OS 권한 + 토큰을 하나의 규칙으로 묶는다.
///
///   서버에 발송 주소를 등록하는 조건 = 광고성 정보 수신 동의 O 그리고 OS 알림 권한 O
///
/// 둘 중 하나라도 없으면 등록하지 않고, 이미 있던 등록은 지운다. 이 게임의 원격 알림은
/// 오프라인 보상 최대치 안내(광고성) 하나뿐이라 그 동의가 곧 원격 알림 사용 여부다(PushRules 주석 참고).
///
/// 동의가 있어도 OS 권한이 거부돼 있으면 등록하지 않는다. 보낼 수 없는 주소를 들고 있을 이유가 없고,
/// 그 주소는 그 자체로 기기 식별자이기 때문이다.
/// </summary>
public sealed class PushCoordinator : IPushCoordinator, IDisposable {
    private readonly IPushProvider _provider;
    private readonly IPushTokenService _tokens;
    private readonly ITermsService _terms;

    private readonly object _lock = new();
    private string _pendingAction = "";
    private string _registeredToken = "";

    public PushCoordinator(IPushTokenService tokens, ITermsService terms) {
        _provider = PushProviderFactory.Create();
        _tokens = tokens;
        _terms = terms;

        _provider.TokenRefreshed += OnTokenRefreshed;
        _provider.NotificationOpened += OnNotificationOpened;
    }

    public string TakePendingAction() {
        // iOS 는 앱이 꺼진 상태에서 알림으로 열린 경우를 네이티브가 보관하므로 여기서 한 번 끌어온다.
#if UNITY_IOS && !UNITY_EDITOR
        if (_provider is IosApnsProvider ios) ios.PumpPendingAction();
#endif
        lock (_lock) {
            string action = _pendingAction;
            _pendingAction = "";
            return action;
        }
    }

    public async UniTask SyncAsync(CancellationToken ct = default) {
        if (!_provider.IsSupported) return;

        bool wanted;
        try {
            wanted = await IsMarketingAgreedAsync(ct);
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            // 동의 상태를 모르면 아무것도 바꾸지 않는다. 모르는 채로 등록하거나 지우는 것이 더 나쁘다.
            Debug.LogWarning($"[Push] 동의 상태를 확인하지 못해 동기화를 건너뜁니다: {e.Message}");
            return;
        }

        if (!wanted) { await RemoveAsync(ct); return; }

        // 동의는 계정에 남아 있는데 그 기기에는 OS 권한이 없는 경우가 있다. 앱을 지웠다 다시 깔거나
        // 새 기기로 옮기면 계정의 동의는 그대로인데 OS 권한만 초기화되기 때문이다.
        //
        // 여기서 묻지 않으면 "동의는 켜져 있는데 알림은 영영 오지 않는" 상태가 된다. 유저 입장에서는
        // 이미 켜 둔 것이라 설정을 봐도 이상한 곳이 없어 스스로 고칠 수가 없다.
        //
        // 물어도 안전하다. 이미 허용됐으면 팝업 없이 true 를 돌려주고, 한 번 거부된 상태면 팝업 없이
        // false 를 돌려준다(OS 가 다시 묻지 않는다). 즉 실제로 팝업이 뜨는 것은 아직 정해지지 않은
        // 설치, 다시 말해 방금 깐 앱뿐이다. 그 시점은 유저가 수신 동의를 켠 직후라 묻기에 자연스럽다.
        if (!await _provider.RequestPermissionAsync(ct)) { await RemoveAsync(ct); return; }

        await RegisterAsync(ct);
    }

    public async UniTask<bool> EnableAsync(CancellationToken ct = default) {
        if (!_provider.IsSupported) return false;

        if (!await _provider.RequestPermissionAsync(ct)) return false;
        return await RegisterAsync(ct);
    }

    public async UniTask DisableAsync(CancellationToken ct = default) {
        if (!_provider.IsSupported) return;
        await RemoveAsync(ct);
    }

    // 토큰을 받아 서버에 등록한다. 토큰을 받지 못하면 아무것도 등록하지 않는다.
    private async UniTask<bool> RegisterAsync(CancellationToken ct) {
        string token;
        try {
            token = await _provider.AcquireTokenAsync(ct);
        } catch (OperationCanceledException) {
            return false;
        }
        if (string.IsNullOrEmpty(token)) return false;

        bool ok;
        try {
            ok = await _tokens.RegisterAsync(PushInstallation.Id, _provider.Platform, token,
                                             _provider.ApnsEnvironment, ct);
        } catch (OperationCanceledException) {
            return false;
        } catch (Exception e) {
            Debug.LogWarning($"[Push] 발송 주소 등록 실패: {e.Message}");
            return false;
        }

        // 등록에 성공한 토큰만 기억한다. 갱신 이벤트가 같은 값을 다시 보낼 때 중복 등록을 피하기 위한 것이다.
        if (ok) lock (_lock) _registeredToken = token;
        return ok;
    }

    // 서버 등록을 지우고 로컬 토큰도 폐기한다. 순서가 중요하다. 서버를 먼저 지워야
    // 토큰을 잃어버려 서버에만 남는 상태가 생기지 않는다.
    private async UniTask RemoveAsync(CancellationToken ct) {
        lock (_lock) _registeredToken = "";

        try {
            await _tokens.UnregisterAsync(PushInstallation.Id, ct);
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogWarning($"[Push] 발송 주소 해제 실패: {e.Message}");
        }

        try {
            await _provider.ReleaseTokenAsync(ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogWarning($"[Push] 로컬 토큰 폐기 실패: {e.Message}");
        }
    }

    private async UniTask<bool> IsMarketingAgreedAsync(CancellationToken ct) {
        var status = await _terms.GetStatusAsync(ct);
        return status?.Find(TermsRules.MarketingKey)?.IsAgreed ?? false;
    }

    // 제공자가 토큰을 새로 발급했다. 등록해 둔 값과 다르면 서버를 갱신한다.
    private void OnTokenRefreshed(string token) {
        if (string.IsNullOrEmpty(token)) return;
        lock (_lock) { if (token == _registeredToken) return; }

        // 갱신은 배경에서 처리한다. 실패해도 다음 동기화(로그인/복귀)에서 다시 맞춰진다.
        SyncAsync(CancellationToken.None).Forget();
    }

    private void OnNotificationOpened(string action) {
        if (!PushRules.IsKnownAction(action)) return;
        lock (_lock) _pendingAction = action;
    }

    public void Dispose() {
        _provider.TokenRefreshed -= OnTokenRefreshed;
        _provider.NotificationOpened -= OnNotificationOpened;
        if (_provider is IDisposable d) d.Dispose();
    }
}
