using System;

/// <summary>
/// 사용자가 다운로드 동의 팝업에서 "종료"를 선택해 부트를 중단시키는 예외.
/// Bootstrapper 에서 조용히 잡는다(앱을 종료하는 정상 흐름이라 실패 팝업을 띄우지 않는다).
/// </summary>
public sealed class DownloadDeclinedException : Exception {
    public DownloadDeclinedException() : base("User declined the download.") { }
}
