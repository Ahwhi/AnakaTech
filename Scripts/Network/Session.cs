using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore {
    public abstract class PacketSession : Session {
        public static readonly int HeaderSize = 2;
        public sealed override int OnRecv(ArraySegment<byte> buffer) {
            int processLen = 0;
            int packetCount = 0; // 로그를 찍기 위함

            while (true) {
                // 최소한 헤더는 파싱할 수 있는지 확인
                if (buffer.Count < HeaderSize) {
                    break;
                }
                // 패킷이 완전체로 도착했는지 확인
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (buffer.Count < dataSize) {
                    break;
                }

                //여기까지 왔으면 패킷 조립 가능
                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));
                packetCount++;
                processLen += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }

            return processLen;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }

    /// <summary>
    /// 서버와의 송수신을 담당하는 세션.
    ///
    /// 소켓을 직접 다루지 않고 스트림 위에서 동작한다. 평문 연결이면 NetworkStream 이, TLS 연결이면 그 위에 씌운
    /// SslStream 이 들어온다. 두 경우의 코드 경로가 완전히 같으므로 "개발은 평문, 운영은 TLS" 같은 구성에서도
    /// 송수신 로직이 갈라지지 않는다. 어떤 스트림을 넘길지는 Connector 가 정한다.
    ///
    /// 송신은 반드시 한 번에 하나씩 처리한다(SendLoop 하나만 도는 구조). TLS 는 하나의 스트림에 동시에 쓰면
    /// 레코드가 섞여 연결이 깨지므로, 큐에 넣고 순서대로 내보내는 이 구조가 전제 조건이다.
    /// </summary>
    public abstract class Session {
        Socket _socket;
        Stream _stream;
        EndPoint _remoteEndPoint;      // 소켓을 닫은 뒤에도 알려 줘야 하므로 미리 받아 둔다.
        int _disconnected = 0;

        RecvBuffer _recvBuffer = new RecvBuffer(65535); // 패킷 사이즈. 작으면 다 못받음

        object _lock = new object();

        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>(); // 큐 클래스는 선입선출
        bool _sending;   // 송신 루프가 도는 중인지. 큐가 비면 루프가 스스로 멈춘다.

        public abstract void OnConnected(EndPoint endPoint);

        public abstract int OnRecv(ArraySegment<byte> buffer);

        public abstract void OnSend(int numOfBytes);

        public abstract void OnDisconnected(EndPoint endPoint);

        void Clear() {
            lock (_lock) {
                _sendQueue.Clear();
            }
        }

        /// <summary>연결이 준비된 소켓과 그 위의 스트림으로 세션을 시작한다(TLS 핸드셰이크는 이미 끝난 상태여야 한다).</summary>
        public void Start(Socket socket, Stream stream) {
            _socket = socket;
            _stream = stream;
            try {
                _remoteEndPoint = socket.RemoteEndPoint;
            } catch {
                _remoteEndPoint = null;
            }

            _ = RecvLoopAsync();
        }

        public void Send(ArraySegment<byte> sendBuff) //일반 센드
        {
            lock (_lock) {
                if (_disconnected == 1) return;
                _sendQueue.Enqueue(sendBuff);
                if (_sending) return;
                _sending = true;
            }
            _ = SendLoopAsync();
        }

        public void Send(List<ArraySegment<byte>> sendBuffList) //리스트 센드(잡 큐)
        {
            if (sendBuffList.Count == 0) {
                return;
            }

            lock (_lock) {
                if (_disconnected == 1) return;
                foreach (ArraySegment<byte> sendBuff in sendBuffList) {
                    _sendQueue.Enqueue(sendBuff);
                }
                if (_sending) return;
                _sending = true;
            }
            _ = SendLoopAsync();
        }

        public void Disconnect() {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1) {
                return;
            }

            // 연결이 성립하기 전(Start 이전)에 정리되는 경우가 있다(앱 종료, 로그아웃 등).
            // 시작한 적 없는 세션까지 끊김을 알리면 연결된 적 없는 상태에서 재접속 처리가 도는 오작동이 된다.
            if (_socket == null) {
                Clear();
                return;
            }

            OnDisconnected(_remoteEndPoint);

            // 소켓을 먼저 닫는다. TLS 스트림을 먼저 Dispose 하면 종료 알림(close_notify)을 보내려 하는데,
            // 연결이 이미 죽었거나 회선이 느리면 그 전송이 호출한 스레드를 붙잡는다. 로그아웃처럼 메인 스레드에서
            // 끊는 경로에서는 그대로 화면이 멈춘다. 소켓을 먼저 닫으면 전송 시도가 즉시 실패해 붙잡히지 않는다.
            try { _socket?.Shutdown(SocketShutdown.Both); } catch { }
            try { _socket?.Close(); } catch { }
            try { _stream?.Dispose(); } catch { }

            Clear();
        }

        #region 네트워크 통신

        async Task SendLoopAsync() {
            while (true) {
                ArraySegment<byte> buff;
                lock (_lock) {
                    if (_disconnected == 1 || _sendQueue.Count == 0) {
                        _sending = false;
                        return;
                    }
                    buff = _sendQueue.Dequeue();
                }

                try {
                    await _stream.WriteAsync(buff.Array, buff.Offset, buff.Count).ConfigureAwait(false);
                } catch (Exception e) {
                    if (_disconnected == 0) UnityEngine.Debug.LogWarning($"[Net] 송신 실패: {e.Message}");
                    lock (_lock) { _sending = false; }
                    Disconnect();
                    return;
                }

                try {
                    OnSend(buff.Count);
                } catch (Exception e) {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        async Task RecvLoopAsync() {
            while (_disconnected == 0) {
                //버퍼 유효 범위 설정
                _recvBuffer.Clean();
                ArraySegment<byte> segment = _recvBuffer.WriteSegment;

                int read;
                try {
                    read = await _stream.ReadAsync(segment.Array, segment.Offset, segment.Count).ConfigureAwait(false);
                } catch (Exception e) {
                    if (_disconnected == 0) UnityEngine.Debug.LogWarning($"[Net] 수신 실패: {e.Message}");
                    Disconnect();
                    return;
                }

                // 0 바이트 = 서버가 정상적으로 연결을 닫았다는 뜻.
                if (read <= 0) {
                    Disconnect();
                    return;
                }

                try {
                    //Write 커서 이동
                    if (_recvBuffer.OnWrite(read) == false) {
                        Disconnect();
                        return;
                    }

                    //컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
                    int processLen = OnRecv(_recvBuffer.ReadSegment);
                    if (processLen < 0 || _recvBuffer.DataSize < processLen) {
                        Disconnect();
                        return;
                    }

                    //Read 커서 이동
                    if (_recvBuffer.OnRead(processLen) == false) {
                        Disconnect();
                        return;
                    }
                } catch (Exception e) {
                    UnityEngine.Debug.LogException(e);
                    Disconnect();
                    return;
                }
            }
        }

        #endregion
    }
}
