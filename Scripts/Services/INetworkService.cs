using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ServerCore;

/// <summary>
/// 서버 연결/송수신 추상화. 소비자(부트 스텝, 로그인 등)는 NetworkManager 구현체가 아니라
/// 이 계약에만 의존한다. 전역 싱글톤 접근을 대체해 의존 방향을 단방향으로 유지한다.
/// </summary>
public interface INetworkService {
    bool IsConnected { get; }
    float LastRtt { get; }

    event Action Connected;
    event Action Disconnected;
    /// <summary>수신한 모든 패킷을 메인 스레드에서 통지한다(특정 대기자 없이 상시 수신하는 서버 푸시용).
    /// 구독자는 관심 타입만 필터링해 처리한다(예: MailService 의 S_MailNotify). 구독 해제 책임은 구독자에 있다.</summary>
    event Action<IPacket> PacketReceived;

    UniTask ConnectAsync(CancellationToken ct = default);
    /// <summary>현재 세션을 즉시 끊는다(로그아웃 등 의도적 종료). Disconnected 이벤트로 이어진다.</summary>
    void Disconnect();
    void Send(ArraySegment<byte> data);
    UniTask<T> WaitPacketAsync<T>(CancellationToken ct = default) where T : IPacket;
}
