using System;

/// <summary>
/// 서버 점검 중임을 버전 체크 단계에서 받았을 때 부트 파이프라인을 중단시키는 예외.
/// Bootstrapper 에서 잡아 BootFailReason.Maintenance 로 분류하고, 타이틀 UI 가 점검 팝업으로 분기한다.
/// 점검은 "연결 실패"가 아니라 "연결/버전은 됐으나 서버가 점검 중"인 상태다.
/// </summary>
public sealed class MaintenanceException : Exception {
    public MaintenanceException() : base("Server is under maintenance.") { }
}
