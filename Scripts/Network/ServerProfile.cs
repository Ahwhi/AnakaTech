using System;
using UnityEngine;

/// <summary>
/// 접속할 서버 한 벌의 설정(개발 PC / 라이브 등). NetworkManager 인스펙터에 목록으로 두고 버튼으로 골라 적용한다.
///
/// 접속 값을 손으로 바꿔 가며 쓰면 라이브 주소로 개발 서버에 붙거나(포트 불일치로 실패) 그 반대의 실수가 난다.
/// 특히 TLS 는 주소와 짝이 맞아야 하므로(도메인 + 443 + 켬 / 로컬 IP + 7777 + 끔) 한 벌씩 통째로 바꾸는 편이 안전하다.
/// </summary>
[Serializable]
public sealed class ServerProfile {
    [Tooltip("인스펙터 버튼에 표시할 이름. 예: 개발 PC, 라이브(AWS)")]
    public string displayName = "새 프로필";

    [Tooltip("서버 주소. TLS 를 켜면 인증서를 발급받은 도메인이어야 한다.")]
    public string host = "127.0.0.1";

    public int port = 7777;

    [Tooltip("전송 구간 TLS 암호화. 라이브는 켜고, 평문으로 도는 개발 PC 서버는 끈다.")]
    public bool useTls;

    [Tooltip("선택 사항. 비워 두면 기기의 표준 인증서 검증만 사용한다(권장).")]
    public string[] pinnedCertificateHashes = Array.Empty<string>();
}
