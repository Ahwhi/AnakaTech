using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Enums;

/// <summary>
/// 3단계: 버전 확인. C_Version 송신 후 S_Version 응답을 대기한다.
/// needUpdate 이면 ForceUpdateException, maintenance 면 MaintenanceException 으로 부트를 중단한다.
/// 점검을 이 단계에서 막으면 신규/재접속 구분 없이(클라 로그인 캐시와 무관하게) 모두 동일하게 차단된다.
/// </summary>
public sealed class VersionCheckStep : IBootStep {
    private const float TimeoutSeconds = 10f;

    private readonly INetworkService _network;
    private readonly PlatformType _platform;

    public BootState State => BootState.CheckingVersion;

    public VersionCheckStep(INetworkService network, PlatformType platform) {
        _network = network;
        _platform = platform;
    }

    public async UniTask ExecuteAsync(CancellationToken ct) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

        var req = new C_Version {
            platform = (int)_platform,
            version = Application.version   // 빌드 버전(PlayerSettings.bundleVersion). 서버 강제 업데이트 판정 기준.
        };
        _network.Send(req.Write());

        var res = await _network.WaitPacketAsync<S_Version>(cts.Token);
        if (res.needUpdate) throw new ForceUpdateException();
        if (res.maintenance) throw new MaintenanceException();
    }
}
