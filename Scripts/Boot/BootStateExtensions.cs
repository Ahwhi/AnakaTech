using static Enums;

/// <summary>
/// 부트 상태를 로딩 UI 에 표시할 사용자 문구로 변환한다.
/// 로딩 UI 는 IBootStatus.State 를 구독해 이 값을 출력만 하면 된다.
/// </summary>
public static class BootStateExtensions {
    // 로딩 스플래시에서 표시할 단계별 문구. 끝의 "..." 는 TitleSceneManager 가
    // 점 개수를 애니메이션하므로 여기에는 넣지 않는다.
    // LoggingIn 은 "로그인을 기다리는" 게이트 상태라 로딩 문구가 아니라 로그인 화면을 띄운다.
    // (실제 "로그인 중입니다..." 는 버튼 탭 후 LoginStarted 시점에 글로벌 로딩 오버레이로 표시)
    public static string ToMessage(this BootState state) => state switch {
        BootState.Initializing              => "게임을 준비하고 있습니다",
        BootState.ConnectingServer          => "서버에 접속하고 있습니다",
        BootState.CheckingVersion           => "버전 정보를 확인하고 있습니다",
        BootState.DownloadingAddressable    => "리소스를 내려받고 있습니다",
        BootState.LoggingIn                 => string.Empty,
        // 약관 동의도 사용자 입력을 기다리는 게이트 상태라 로딩 문구 대신 동의 팝업이 화면을 맡는다.
        BootState.AgreeingTerms             => string.Empty,
        BootState.Ready                     => "환영합니다",
        BootState.Failed                    => "오류가 발생했습니다",
        _ => string.Empty,
    };

    // 부트 실패 사유별 안내 문구(로딩 스플래시에 표시). 점검/업데이트/연결 실패는 사유를 명확히 알리고,
    // 분류되지 않은(예기치 못한 예외) 경우에만 일반 오류 문구로 떨어진다.
    public static string ToMessage(this BootFailReason reason) => reason switch {
        BootFailReason.Maintenance   => "서버가 점검 중입니다",
        BootFailReason.ForceUpdate   => "업데이트가 필요합니다",
        BootFailReason.CannotConnect => "서버에 연결할 수 없습니다",
        _ => "오류가 발생했습니다",
    };
}
