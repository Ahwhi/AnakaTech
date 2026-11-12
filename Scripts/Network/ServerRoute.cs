using System;

/// <summary>
/// 특정 앱 버전을 어느 게임 서버로 보낼지 적은 한 줄. CDN 의 경로표(ServerRouteTable) JSON 에서 읽는다.
///
/// 스토어 심사 중인 새 버전은 아직 라이브 서버와 패킷이 맞지 않으므로 테스트 서버로 보내야 하고,
/// 출시하는 순간에는 라이브로 보내야 한다. 심사를 통과한 빌드가 그대로 출시되기 때문에 이 결정을
/// 빌드에 박아 둘 수 없다. 그래서 접속 대상은 빌드가 아니라 경로표가 정한다.
/// </summary>
[Serializable]
public sealed class ServerRoute {
    /// <summary>대상 앱 버전(PlayerSettings.bundleVersion 과 같은 문자열). 예: "1.9"</summary>
    public string version;
    public string host;
    public int port;
    public bool useTls = true;

    public bool IsValid => !string.IsNullOrWhiteSpace(host) && port > 0 && port <= 65535;

    public bool Matches(string appVersion) =>
        !string.IsNullOrWhiteSpace(version) &&
        string.Equals(version.Trim(), appVersion?.Trim(), StringComparison.Ordinal);

    public override string ToString() => $"{(useTls ? "tls" : "tcp")}://{host}:{port} (v{version})";
}

/// <summary>
/// 경로표 전체. 여기에 없는 버전은 NetworkManager 인스펙터의 기본 접속 값(라이브)을 쓴다.
/// 그래서 평소에는 빈 목록이고, 심사 중인 버전만 잠시 적었다가 출시할 때 지운다.
///
/// 예:
/// { "routes": [ { "version": "1.9", "host": "game.ahwhistudio.com", "port": 8445, "useTls": true } ] }
/// </summary>
[Serializable]
public sealed class ServerRouteTable {
    public ServerRoute[] routes = Array.Empty<ServerRoute>();

    public ServerRoute Find(string appVersion) {
        if (routes == null) return null;
        foreach (var route in routes) {
            if (route != null && route.Matches(appVersion) && route.IsValid) return route;
        }
        return null;
    }
}
