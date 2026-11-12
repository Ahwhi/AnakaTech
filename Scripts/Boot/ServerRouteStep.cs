using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static Enums;

/// <summary>
/// 연결 직전 단계: 원격 경로표에서 이 앱 버전의 접속 대상을 찾아 NetworkManager 에 적용한다.
///
/// 경로표에 이 버전이 없거나, 조회가 실패하거나, 설정이 비어 있으면 기본 접속 값(라이브)으로 되돌린다.
/// 이 단계는 어떤 경우에도 부트를 실패시키지 않는다(앱 수명 취소만 그대로 전파한다).
/// 재접속으로 부트가 다시 돌 때마다 새로 조회하므로, 출시 때 경로표에서 버전을 지우면 다음 접속부터 라이브로 간다.
/// </summary>
public sealed class ServerRouteStep : IBootStep {
    private readonly ServerRouteConfig _config;
    private readonly IServerRouteTarget _target;

    // 연결 단계와 한 덩어리로 보이게 같은 상태를 쓴다(로딩 UI 문구 유지).
    public BootState State => BootState.ConnectingServer;

    public ServerRouteStep(ServerRouteConfig config, IServerRouteTarget target) {
        _config = config;
        _target = target;
    }

    public async UniTask ExecuteAsync(CancellationToken ct) {
        if (_config == null || !_config.IsActive) {
            _target.ApplyRoute(null);
            return;
        }

        // Unity API 는 await 전에 읽어 둔다.
        string appVersion = Application.version;
        ServerRoute route = null;

        try {
            var table = await FetchAsync(ct);
            route = table?.Find(appVersion);
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            throw;
        } catch (Exception e) {
            Debug.LogWarning($"[ServerRoute] 경로표 조회 실패, 기본 접속 값으로 진행: {e.Message}");
        }

        await UniTask.SwitchToMainThread();
        if (route != null) Debug.Log($"[ServerRoute] v{appVersion} -> {route}");
        _target.ApplyRoute(route);
    }

    private async UniTask<ServerRouteTable> FetchAsync(CancellationToken ct) {
        // 경로표는 출시 순간에 바뀌어야 하므로 중간 캐시를 거치지 않게 매번 다른 주소로 요청한다.
        string url = _config.TableUrl;
        url += (url.Contains("?") ? "&" : "?") + "t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        using var request = UnityWebRequest.Get(url);
        request.timeout = _config.TimeoutSeconds;
        request.SetRequestHeader("Cache-Control", "no-cache");

        // 실패 응답(404 등)은 UniTask 가 예외로 던지며, 호출부에서 기본 접속 값으로 처리한다.
        await request.SendWebRequest().WithCancellation(ct);
        return JsonUtility.FromJson<ServerRouteTable>(request.downloadHandler.text);
    }
}
