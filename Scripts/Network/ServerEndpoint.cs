using System;
using System.Collections.Generic;

namespace ServerCore {
    /// <summary>
    /// 접속할 서버의 위치와 보안 조건. 연결을 시도하는 쪽(Connector)이 필요로 하는 정보를 한 덩어리로 넘긴다.
    ///
    /// Host 는 IP 문자열과 도메인 이름을 모두 받는다. TLS 를 켜면 이 값이 인증서의 이름 검증(SNI) 기준이 되므로,
    /// 인증서를 발급받은 도메인과 정확히 같아야 한다(IP 로는 공인 인증서를 발급받을 수 없다).
    /// </summary>
    public sealed class ServerEndpoint {
        public string Host { get; }
        public int Port { get; }

        /// <summary>TLS 사용 여부. 운영 환경에서는 반드시 켠다(개인정보/인증 토큰이 오가는 구간이다).</summary>
        public bool UseTls { get; }

        /// <summary>
        /// 신뢰할 인증서의 SHA-256 지문(Base64). 서버 인증서 자체 또는 그 발급 기관(CA) 인증서를 넣는다.
        /// 비워 두면 기기의 기본 신뢰 저장소만으로 검증하는데, 유니티는 플랫폼에 따라 루트 인증서 목록을
        /// 제대로 읽지 못하는 경우가 있어 운영 빌드에서는 값을 채워 두는 것을 전제로 한다.
        /// </summary>
        public IReadOnlyList<string> PinnedCertificateHashes { get; }

        public ServerEndpoint(string host, int port, bool useTls = false, IReadOnlyList<string> pinnedCertificateHashes = null) {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("서버 주소가 비어 있습니다.", nameof(host));
            Host = host.Trim();
            Port = port;
            UseTls = useTls;
            PinnedCertificateHashes = pinnedCertificateHashes ?? Array.Empty<string>();
        }

        public override string ToString() => $"{(UseTls ? "tls" : "tcp")}://{Host}:{Port}";
    }
}
