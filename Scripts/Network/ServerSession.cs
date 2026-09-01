using ServerCore;
using System;
using System.Net;

namespace DummyClient {

    /// <summary>
    /// 소켓 콜백을 NetworkManager 의 internal 메서드로 위임만 한다.
    /// NetworkManager 의 내부 상태(_isConnected, _connectTcs 등)에 직접 접근하지 않는다.
    /// </summary>
    class ServerSession : PacketSession {
        private readonly NetworkManager _network;

        public ServerSession(NetworkManager network) {
            _network = network;
        }

        public override void OnConnected(EndPoint endPoint)
            => _network.OnSessionConnected(endPoint);

        public override void OnDisconnected(EndPoint endPoint)
            => _network.OnSessionDisconnected(endPoint);

        public override void OnRecvPacket(ArraySegment<byte> buffer)
            => PacketManager.Instance.OnRecvPacket(this, buffer, (s, p) => PacketQueue.Instance.Push(p));

        public override void OnSend(int numOfBytes) { }
    }
}
