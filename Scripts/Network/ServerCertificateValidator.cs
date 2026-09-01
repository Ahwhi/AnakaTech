using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ServerCore {
    /// <summary>
    /// TLS 핸드셰이크에서 서버 인증서를 검증하는 정책.
    ///
    /// 검증은 항상 표준 검증을 먼저 통과해야 한다. 즉 기기의 신뢰 저장소로 체인이 검증되고, 도메인이 일치하고,
    /// 유효기간 안이어야 한다. 어떤 설정으로도 이 조건을 건너뛰거나 완화할 수 없다.
    ///
    /// 지문(핀)을 설정하면 그 위에 조건이 하나 더 붙는다. "표준 검증을 통과한 인증서 중에서도 우리가 지정한
    /// 인증서(또는 그 발급 기관)여야 한다"는 제한이다. 핀은 검증을 대신하는 장치가 아니라 좁히는 장치이므로,
    /// 핀이 틀리면 연결이 거절되고 핀이 맞아도 표준 검증 실패는 구제되지 않는다.
    ///
    /// 핀을 비워 두면 표준 검증만으로 판정한다. 기기의 신뢰 저장소가 정상 동작하는 환경에서는 이것으로 충분하며,
    /// 인증서 발급 기관이 바뀌어도 앱 업데이트가 필요 없다는 장점이 있다.
    ///
    /// 로그에는 도메인과 오류 종류만 남긴다. 세션 토큰이나 인증 토큰 같은 값은 이 계층을 지나가지도 않는다.
    /// </summary>
    public sealed class ServerCertificateValidator {
        private readonly HashSet<string> _pins = new HashSet<string>(StringComparer.Ordinal);
        private readonly string _host;

        public ServerCertificateValidator(string host, IReadOnlyList<string> pinnedCertificateHashes) {
            _host = string.IsNullOrEmpty(host) ? "(알 수 없음)" : host;
            if (pinnedCertificateHashes == null) return;
            foreach (var pin in pinnedCertificateHashes) {
                if (string.IsNullOrWhiteSpace(pin)) continue;
                _pins.Add(pin.Trim());
            }
        }

        public bool HasPins => _pins.Count > 0;

        /// <summary>SslStream 의 RemoteCertificateValidationCallback 으로 연결한다.</summary>
        public bool Validate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
            if (certificate == null) {
                Reject("서버가 인증서를 제시하지 않았습니다");
                return false;
            }

            // 1) 표준 검증. 도메인 불일치, 체인 오류(신뢰할 수 없는 발급자/만료/서명 오류 등)는 전부 여기서 걸린다.
            //    핀 설정 여부와 무관하게 통과해야 한다.
            if (errors != SslPolicyErrors.None) {
                Reject($"표준 인증서 검증 실패({errors})");
                LogChainStatus(chain);
                return false;
            }

            // 2) 유효기간 재확인. 체인 검증이 이미 확인하는 항목이지만, 플랫폼이 체인 정보를 주지 않는 경우까지
            //    같은 기준으로 막기 위해 인증서 자체의 기간을 직접 본다.
            if (!IsWithinValidityPeriod(certificate)) return false;

            // 3) 핀이 있으면 추가 제한을 적용한다. 표준 검증을 통과한 인증서 중에서도 지정한 것이어야 한다.
            if (HasPins && !MatchesPin(certificate, chain)) {
                Reject("인증서가 등록된 지문(핀)과 일치하지 않습니다");
                return false;
            }

            return true;
        }

        // 인증서의 유효 시작/만료 시각을 UTC 로 비교한다.
        private bool IsWithinValidityPeriod(X509Certificate certificate) {
            X509Certificate2 cert2 = certificate as X509Certificate2;
            if (cert2 == null) {
                try {
                    cert2 = new X509Certificate2(certificate.GetRawCertData());
                } catch (Exception e) {
                    Reject($"인증서를 해석하지 못했습니다({e.GetType().Name})");
                    return false;
                }
            }

            var now = DateTime.UtcNow;
            if (now < cert2.NotBefore.ToUniversalTime()) {
                Reject($"인증서의 유효 시작 시각이 아직 오지 않았습니다(기기 시계 확인 필요, 시작 {cert2.NotBefore.ToUniversalTime():yyyy-MM-dd})");
                return false;
            }
            if (now > cert2.NotAfter.ToUniversalTime()) {
                Reject($"인증서가 만료되었습니다(만료 {cert2.NotAfter.ToUniversalTime():yyyy-MM-dd})");
                return false;
            }
            return true;
        }

        // 서버 인증서와 체인에 포함된 상위 인증서(중간/루트 CA) 중 하나라도 핀과 맞으면 통과다.
        // 발급 기관을 핀으로 두면 서버 인증서가 갱신되어도(짧은 주기로 갱신되는 무료 인증서 등) 앱을 다시 낼 필요가 없다.
        private bool MatchesPin(X509Certificate certificate, X509Chain chain) {
            if (_pins.Contains(Fingerprint(certificate.GetRawCertData()))) return true;

            if (chain?.ChainElements == null) return false;
            foreach (X509ChainElement element in chain.ChainElements) {
                if (element.Certificate == null) continue;
                if (_pins.Contains(Fingerprint(element.Certificate.GetRawCertData()))) return true;
            }
            return false;
        }

        // 표준 검증이 실패했을 때 원인을 좁힐 수 있도록 체인 상태를 함께 남긴다(값이 아니라 상태 이름만).
        private static void LogChainStatus(X509Chain chain) {
            if (chain?.ChainStatus == null) return;
            foreach (var status in chain.ChainStatus) {
                if (status.Status == X509ChainStatusFlags.NoError) continue;
                UnityEngine.Debug.LogError($"[TLS] 체인 상태: {status.Status} ({status.StatusInformation?.Trim()})");
            }
        }

        private void Reject(string reason) =>
            UnityEngine.Debug.LogError($"[TLS] {_host} 연결을 거절했습니다. 사유: {reason}");

        /// <summary>인증서(DER 전체)의 SHA-256 지문을 Base64 로. 운영 문서의 openssl 명령과 같은 값이 나온다.</summary>
        public static string Fingerprint(byte[] rawCertData) {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(rawCertData));
        }
    }
}
