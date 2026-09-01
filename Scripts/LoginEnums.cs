/// <summary>
/// 소셜 로그인 제공자. 값은 PDL C_Login.provider 및 서버 Server.Auth.LoginProvider 와 일치.
/// </summary>
public enum LoginProvider {
    None   = 0,
    Google = 1,
    Apple  = 2,
    Naver  = 3,
    /// <summary>
    /// 개발용 임시 계정. 누를 때마다 새 계정이 만들어지고 같은 계정으로 다시 접속할 수 없다.
    /// 길드처럼 여러 계정이 필요한 컨텐츠를 목 로그인 3개만으로 시험하기 어려워 둔 통로다.
    /// </summary>
    Guest  = 4,
}

/// <summary>
/// 로그인 실패 사유.
/// 1~99  : 클라이언트 측(취소/SDK 오류/타임아웃 등) — 서버까지 가기 전에 발생.
/// 100~  : 서버 S_Login.resultCode 그대로 매핑(서버 LoginResultCode 와 값 일치).
/// </summary>
public enum LoginFailReason {
    None                = 0,

    // ── 클라이언트 ──
    Cancelled           = 1,   // 사용자가 소셜 로그인 창을 닫음
    ProviderError       = 2,   // 네이티브 SDK 오류
    UnsupportedProvider = 3,   // 등록되지 않은 provider
    Busy                = 4,   // 이미 로그인 진행 중
    Timeout             = 5,   // 서버 응답 타임아웃

    // ── 서버 ──
    InvalidToken        = 100, // 토큰 검증 실패
    Banned              = 101, // 정지 계정
    ServerError         = 102, // 서버 내부 오류
    Maintenance         = 103, // 서버 점검 중(연결은 됐으나 로그인 거부). 서버 S_Login.resultCode 와 값 일치
}
