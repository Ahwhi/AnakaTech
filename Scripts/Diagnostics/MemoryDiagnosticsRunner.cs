#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// 메모리 진단 실행기(에디터 전용). 플레이 시작 시 설정이 켜져 있을 때만 스스로 붙는다.
///
/// 하는 일은 셋이다.
///   1) 매 프레임 측정한다(부팅 중 순간 폭증을 놓치지 않기 위해).
///   2) 한 프레임 사이 증가폭이 임계치를 넘으면 급증으로 보고 자산 보고서를 남긴다.
///   3) 예산을 넘으면 에러 로그와 함께 타임라인과 자산 보고서를 남긴다.
///
/// 측정/보고 방법은 MemoryDiagnostics 가, 언제 재고 언제 경보할지는 여기가 정한다.
/// 씬이나 프리팹을 건드리지 않고 코드로만 붙으므로, 진단을 켜고 끄는 데 프로젝트 변경이 필요 없다.
/// </summary>
public sealed class MemoryDiagnosticsRunner : MonoBehaviour {
    private MemoryDiagnostics _diagnostics;
    private long _previousFootprint;
    private bool _budgetReported;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() {
        if (!MemoryDiagnosticsSettings.Enabled) return;

        var go = new GameObject("[MemoryDiagnostics]");
        DontDestroyOnLoad(go);
        go.AddComponent<MemoryDiagnosticsRunner>();
    }

    private void Awake() {
        _diagnostics = MemoryDiagnostics.Begin();
        _previousFootprint = _diagnostics.Current.Footprint;

        // 기기/화면/그래픽 환경을 먼저 남긴다. 메모리 수치만으로는 어떤 환경이었는지 알 수 없다.
        SystemReport.Log();
        _diagnostics.Mark("앱 시작");

        if (MemoryDiagnosticsSettings.ShowOverlay) MemoryOverlay.Create(_diagnostics);
    }

    private void OnDestroy() => MemoryDiagnostics.End();

    private void LateUpdate() {
        var usage = _diagnostics.Sample();
        long delta = usage.Footprint - _previousFootprint;
        _previousFootprint = usage.Footprint;

        if (delta >= MemoryDiagnosticsSettings.SpikeBytes) ReportSpike(usage, delta);

        // 예산은 절대값이 아니라 기준선 대비 증가분으로 본다.
        // 에디터에서는 기준선에 에디터 프로세스 자신의 사용량이 들어 있어 절대값이 무의미하기 때문이다.
        if (!_budgetReported && _diagnostics.AppFootprint >= MemoryDiagnosticsSettings.BudgetBytes)
            ReportBudgetExceeded(usage);
    }

    private void ReportSpike(MemoryUsage usage, long delta) {
        Debug.LogWarning($"[Memory] 급증 감지: +{DownloadInfo.FormatBytes(delta)} " +
                         $"(앱 사용량 {DownloadInfo.FormatBytes(_diagnostics.AppFootprint)}, {usage.Time:0.00}s)");
        Debug.Log(_diagnostics.BuildAssetReport(MemoryDiagnosticsSettings.TopAssetCount));
    }

    private void ReportBudgetExceeded(MemoryUsage usage) {
        _budgetReported = true;   // 한 번만 보고한다. 계속 남기면 로그가 묻힌다.
        Debug.LogError($"[Memory] 예산 초과: 앱 사용량 {DownloadInfo.FormatBytes(_diagnostics.AppFootprint)} " +
                       $"(예산 {DownloadInfo.FormatBytes(MemoryDiagnosticsSettings.BudgetBytes)}, {usage.Time:0.00}s).");
        Debug.Log(_diagnostics.BuildTimeline());
        Debug.Log(_diagnostics.BuildAssetReport(MemoryDiagnosticsSettings.TopAssetCount));
    }
}
#endif
