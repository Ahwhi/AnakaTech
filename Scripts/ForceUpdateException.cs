using System;

/// <summary>
/// 서버가 강제 업데이트를 요구할 때 부트 파이프라인을 중단시키는 예외.
/// Bootstrapper 에서 잡아 스토어 이동 UI 등으로 분기한다.
/// </summary>
public sealed class ForceUpdateException : Exception {
    public ForceUpdateException() : base("Force update required.") { }
}
