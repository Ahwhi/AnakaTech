using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

/// <summary>
/// 게임 도중 끊긴 연결을 화면 전환 없이 되살리는 계약.
/// 끊김 처리(ConnectionGuard)가 타이틀로 보내기 전에 먼저 시도하고, 세션에 묶인 상태를 다시 맞춰야 하는
/// 쪽(하트비트, 메인 화면 등)은 Recovered 를 구독한다.
/// </summary>
public interface ISessionRecovery {
    /// <summary>재연결과 재로그인이 끝나 새 세션으로 이어졌을 때 한 번 흐른다.</summary>
    IObservable<Unit> Recovered { get; }

    /// <summary>
    /// 연결 -> 버전 확인 -> 저장된 토큰으로 재로그인 -> 약관 상태 확인 순으로 세션을 다시 연다.
    /// 사용자의 선택이 필요한 경우(업데이트/점검/토큰 만료/약관 개정)는 여기서 처리하지 않고 false 를 돌려준다.
    /// 그 경우는 타이틀의 부트 흐름이 화면과 함께 처리한다.
    /// </summary>
    UniTask<bool> TryRecoverAsync(CancellationToken ct = default);
}

/// <summary>
/// 세션 복구 구현. 부트의 연결/버전 확인 단계를 그대로 재사용해 접속 절차가 두 갈래로 갈라지지 않게 한다.
///
/// 주로 앱을 잠시 내려 두었다 돌아온 경우를 위한 것이다. 백그라운드에서는 핑을 보내지 못해 서버가 무응답
/// 세션을 정리하므로(SessionIdleWatchdog) 돌아오는 순간 끊김이 감지된다. 이때 타이틀을 거치지 않고
/// 같은 계정으로 다시 붙인다. 타이틀 복귀도 결국 같은 토큰으로 자동 로그인하므로 권한상 차이는 없다.
///
/// 돌아온 직후에는 기기 네트워크가 아직 깨어나지 않았을 수 있어 몇 번 간격을 두고 다시 시도한다.
/// </summary>
public sealed class SessionRecovery : ISessionRecovery, IDisposable {
    private static readonly float[] RetryDelaySeconds = { 0f, 1f, 2f };

    private readonly INetworkService _net;
    private readonly ILoginService _login;
    private readonly ITermsService _terms;
    private readonly ConnectStep _connect;
    private readonly VersionCheckStep _version;
    private readonly Subject<Unit> _recovered = new();

    public IObservable<Unit> Recovered => _recovered;

    public SessionRecovery(INetworkService net, ILoginService login, ITermsService terms,
                           ConnectStep connect, VersionCheckStep version) {
        _net = net;
        _login = login;
        _terms = terms;
        _connect = connect;
        _version = version;
    }

    public async UniTask<bool> TryRecoverAsync(CancellationToken ct = default) {
        foreach (float delay in RetryDelaySeconds) {
            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), ignoreTimeScale: true, cancellationToken: ct);

            var result = await TryOnceAsync(ct);
            if (result == Attempt.Success) {
                _recovered.OnNext(Unit.Default);
                return true;
            }
            if (result == Attempt.NeedsTitle) break;
        }

        // 반쯤 열린 연결을 남기면 타이틀의 부트가 "이미 연결됨"으로 보고 그 위에서 진행한다. 깨끗이 끊고 넘긴다.
        if (_net.IsConnected) _net.Disconnect();
        return false;
    }

    private enum Attempt { Success, Retry, NeedsTitle }

    private async UniTask<Attempt> TryOnceAsync(CancellationToken ct) {
        try {
            await _connect.ExecuteAsync(ct);
            await _version.ExecuteAsync(ct);

            // 토큰이 없거나 서버가 거절했다. 다시 시도해도 같으므로 로그인 화면이 있는 타이틀로 넘긴다.
            if (!await _login.TryAutoLoginAsync(ct)) return Attempt.NeedsTitle;

            // 서버는 이 질의로 세션의 약관 동의 여부를 세운다. 빠뜨리면 이후 게임 진입 요청이 거절된다.
            // 그 사이 약관이 개정됐다면 동의 화면이 필요하므로 타이틀로 넘긴다.
            var terms = await _terms.GetStatusAsync(ct);
            return terms.NeedAgreement ? Attempt.NeedsTitle : Attempt.Success;
        } catch (ForceUpdateException) {
            return Attempt.NeedsTitle;
        } catch (MaintenanceException) {
            return Attempt.NeedsTitle;
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            throw;
        } catch (Exception e) {
            // 연결 거절, 응답 시간 초과 등. 다음 시도는 새 연결에서 시작한다.
            Debug.LogWarning($"[SessionRecovery] 재연결 실패: {e.Message}");
            if (_net.IsConnected) _net.Disconnect();
            return Attempt.Retry;
        }
    }

    public void Dispose() => _recovered.Dispose();
}
