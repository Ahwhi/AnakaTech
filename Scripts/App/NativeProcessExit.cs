#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// 엔진 정리 단계를 밟지 않고 프로세스를 즉시 끝내는 플랫폼 브리지.
///
/// iOS 는 종료 시 엔진 정리 마지막 단계에서 죽는 엔진 버그가 있어(UUM-143137,
/// 자세한 내용은 Plugins/iOS/AnakaProcessExit.mm 주석) 그 단계를 건너뛰어야 한다.
/// 지원하지 않는 플랫폼에서는 아무 일도 하지 않으므로, 호출부에 조건부 컴파일이 번지지 않는다.
/// </summary>
public static class NativeProcessExit {
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void AnakaProcessExitNow(int status);

    /// <summary>지원 플랫폼에서는 돌아오지 않는다. 호출 이후의 코드는 실행되지 않는다.</summary>
    public static void Now(int status = 0) => AnakaProcessExitNow(status);
#else
    public static void Now(int status = 0) { }
#endif
}
