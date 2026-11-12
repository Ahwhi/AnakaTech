/// <summary>
/// 다음 연결에 사용할 접속 대상을 정하는 창구. 경로표를 읽는 쪽(ServerRouteStep)은 NetworkManager 구현을 모르고
/// 이 계약에만 의존한다. 연결/송수신 계약(INetworkService)과 소비자가 달라 분리해 둔다.
/// </summary>
public interface IServerRouteTarget {
    /// <summary>다음 ConnectAsync 부터 이 대상으로 접속한다. null 이면 인스펙터 기본 접속 값으로 되돌린다.</summary>
    void ApplyRoute(ServerRoute route);
}
