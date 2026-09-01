#if UNITY_EDITOR
using System.Text;
using UnityEngine;

/// <summary>
/// 실행 환경 요약. 기기에서만 재현되는 문제는 로그에 환경이 남아 있지 않으면 추적할 수 없다.
/// 부팅 직후 한 번 남겨 두면, 기기 콘솔 로그만 받아도 화면/그래픽 설정을 그대로 확인할 수 있다.
///
/// 특히 Display.main 계열과 Screen 계열의 값이 서로 다른지가 중요하다.
/// 두 값이 어긋나면 화면 해상도를 다시 지정하게 되고, 그때 렌더 타겟이 통째로 재할당된다.
/// </summary>
public static class SystemReport {
    /// <summary>환경 요약을 로그로 남긴다.</summary>
    public static void Log() => Debug.Log(Build());

    /// <summary>환경 요약 문자열. 버그 리포트에 그대로 붙일 수 있는 형태.</summary>
    public static string Build() {
        var sb = new StringBuilder();
        sb.AppendLine("[System] 실행 환경");

        sb.AppendLine($"  앱          {Application.productName} {Application.version} / {Application.platform} / {(Debug.isDebugBuild ? "Development" : "Release")}");
        sb.AppendLine($"  기기        {SystemInfo.deviceModel} / {SystemInfo.operatingSystem}");
        sb.AppendLine($"  CPU         {SystemInfo.processorType} x{SystemInfo.processorCount}");
        sb.AppendLine($"  시스템 RAM  {SystemInfo.systemMemorySize} MB");

        sb.AppendLine($"  그래픽 API  {SystemInfo.graphicsDeviceType} / {SystemInfo.graphicsDeviceVersion}");
        sb.AppendLine($"  GPU         {SystemInfo.graphicsDeviceName} / VRAM {SystemInfo.graphicsMemorySize} MB");
        sb.AppendLine($"  텍스처      최대 {SystemInfo.maxTextureSize} / NPOT {SystemInfo.npotSupport} / 복사 {SystemInfo.copyTextureSupport}");

        // 아래 두 줄이 어긋나면 해상도 재지정이 일어난다. 기기에서만 어긋나는 경우가 있어 반드시 함께 본다.
        sb.AppendLine($"  Screen      {Screen.width}x{Screen.height} dpi {Screen.dpi} / 방향 {Screen.orientation} / 전체화면 {Screen.fullScreenMode}");
        sb.AppendLine($"  Screen.res  {Screen.currentResolution}");
        sb.AppendLine($"  Display.main system {Display.main.systemWidth}x{Display.main.systemHeight} / rendering {Display.main.renderingWidth}x{Display.main.renderingHeight}");
        sb.AppendLine($"  SafeArea    {Screen.safeArea}");

        sb.AppendLine($"  프레임      target {Application.targetFrameRate} / vSync {QualitySettings.vSyncCount}");
        sb.AppendLine($"  품질        {QualitySettings.names[QualitySettings.GetQualityLevel()]}");

        return sb.ToString();
    }
}
#endif
