using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace ServerCore {
    /// <summary>
    /// 서버 연결을 만든다. 순서는 "주소 해석 -> TCP 연결 -> (TLS 면) 핸드셰이크 -> 세션 시작"이다.
    ///
    /// TLS 핸드셰이크는 세션이 시작되기 전에 끝나야 한다. 세션은 스트림만 보고 송수신하므로,
    /// 여기서 SslStream 을 만들어 넘기면 그 뒤로는 평문 연결과 완전히 같은 코드가 돈다.
    /// 인증서 검증 규칙은 ServerCertificateValidator 가 갖고 있어 이 클래스는 정책을 판단하지 않는다.
    /// </summary>
    public sealed class Connector {
        // 핸드셰이크까지 포함한 연결 대기 한도. 응답 없는 서버에 매달리지 않도록 둔다.
        private const int ConnectTimeoutMs = 10000;

        /// <summary>
        /// 연결을 시작한다. 성공하면 세션이 시작되고 session.OnConnected 가 호출되며,
        /// 실패하면 onFailed 로 사유를 알린다(호출부가 대기 상태를 정리할 수 있게 한다).
        /// </summary>
        public void Connect(ServerEndpoint endpoint, Func<Session> sessionFactory, Action<Exception> onFailed = null) {
            _ = ConnectAsync(endpoint, sessionFactory, onFailed);
        }

        private async Task ConnectAsync(ServerEndpoint endpoint, Func<Session> sessionFactory, Action<Exception> onFailed) {
            Socket socket = null;
            Stream stream = null;

            try {
                // TLS 는 인증서에 적힌 도메인으로만 검증할 수 있다. IP 로는 공인 인증서를 발급받을 수 없으므로,
                // 주소를 IP 로 둔 채 TLS 를 켜면 반드시 도메인 불일치로 거절된다. 원인을 알기 어려운 실패라 먼저 막는다.
                if (endpoint.UseTls && IPAddress.TryParse(endpoint.Host, out _))
                    throw new InvalidOperationException(
                        $"TLS 접속의 주소는 인증서를 발급받은 도메인이어야 합니다(현재 값은 IP: {endpoint.Host}).");

                var address = await ResolveAsync(endpoint.Host).ConfigureAwait(false);

                socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                var connect = socket.ConnectAsync(new IPEndPoint(address, endpoint.Port));
                if (await Task.WhenAny(connect, Task.Delay(ConnectTimeoutMs)).ConfigureAwait(false) != connect)
                    throw new TimeoutException($"서버 연결 시간이 초과되었습니다({endpoint}).");
                await connect.ConfigureAwait(false);   // 연결 자체의 예외를 그대로 드러낸다.

                stream = new NetworkStream(socket, ownsSocket: false);
                if (endpoint.UseTls)
                    stream = await AuthenticateAsync(stream, endpoint).ConfigureAwait(false);

                var session = sessionFactory.Invoke();
                session.Start(socket, stream);
                session.OnConnected(socket.RemoteEndPoint);
            } catch (Exception e) {
                try { stream?.Dispose(); } catch { }
                try { socket?.Close(); } catch { }

                UnityEngine.Debug.LogWarning($"[Net] 서버 연결 실패({endpoint}): {e.Message}");
                onFailed?.Invoke(e);
            }
        }

        // 도메인과 IP 문자열을 모두 받는다. IP 문자열이면 그대로 쓰고, 도메인이면 DNS 로 푼다.
        private static async Task<IPAddress> ResolveAsync(string host) {
            if (IPAddress.TryParse(host, out var parsed)) return parsed;

            var addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
            // IPv4 를 우선한다(모바일 환경에서 IPv6 만 잡히면 붙지 못하는 서버가 흔하다).
            var address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                          ?? addresses.FirstOrDefault();
            if (address == null) throw new SocketException((int)SocketError.HostNotFound);
            return address;
        }

        // TLS 핸드셰이크. 서버 이름(endpoint.Host)은 인증서의 도메인 검증 기준이자 SNI 값이므로
        // 인증서를 발급받은 도메인과 정확히 같아야 한다.
        private static async Task<Stream> AuthenticateAsync(Stream inner, ServerEndpoint endpoint) {
            // 검증 정책은 ServerCertificateValidator 가 갖는다. 핀이 비어 있으면 표준 검증만으로 판정하며,
            // 이는 기기 신뢰 저장소가 정상 동작하는 환경에서 의도된 구성이다(경고 대상이 아니다).
            var validator = new ServerCertificateValidator(endpoint.Host, endpoint.PinnedCertificateHashes);

            var ssl = new SslStream(inner, leaveInnerStreamOpen: false, validator.Validate);
            try {
                // 폐기 목록 확인은 끈다. 모바일 회선에서 OCSP 조회가 막히면 정상 인증서도 연결이 실패한다.
                // 대신 인증서 지문 검증(핀닝)으로 신뢰를 확보한다.
                // TLS 1.2 로 고정한다. 유니티가 참조하는 API 집합에는 아직 TLS 1.3 상수가 없어 명시할 수 없고,
                // 1.0/1.1 은 안전하지 않으므로 후보에서 뺀다. 유니티가 1.3 을 노출하면 여기에 추가하면 된다.
                await ssl.AuthenticateAsClientAsync(
                    endpoint.Host,
                    null,
                    SslProtocols.Tls12,
                    checkCertificateRevocation: false).ConfigureAwait(false);
            } catch {
                ssl.Dispose();
                throw;
            }
            return ssl;
        }
    }
}
