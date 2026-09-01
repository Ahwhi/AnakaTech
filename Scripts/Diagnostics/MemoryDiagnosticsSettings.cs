#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 메모리 진단 설정(에디터 전용). 개발자 개인 취향이고 프로젝트에 공유할 값이 아니므로
/// 에셋이 아니라 EditorPrefs 에 둔다. 에셋을 만들면 팀원 사이에서 켜짐/꺼짐이 커밋으로 오가게 된다.
///
/// 켜고 끄는 창구는 Tools > 아나카 > 메모리 진단 메뉴다.
/// </summary>
public static class MemoryDiagnosticsSettings {
    private const string EnabledKey = "Anaka.MemoryDiagnostics.Enabled";
    private const string OverlayKey = "Anaka.MemoryDiagnostics.Overlay";
    private const string BudgetKey  = "Anaka.MemoryDiagnostics.BudgetMB";
    private const string SpikeKey   = "Anaka.MemoryDiagnostics.SpikeMB";

    /// <summary>진단 자체를 켤지. 기본값은 꺼짐이라, 평소 개발에는 아무 영향이 없다.</summary>
    public static bool Enabled {
        get => EditorPrefs.GetBool(EnabledKey, false);
        set => EditorPrefs.SetBool(EnabledKey, value);
    }

    /// <summary>화면에 실시간 오버레이를 띄울지.</summary>
    public static bool ShowOverlay {
        get => EditorPrefs.GetBool(OverlayKey, true);
        set => EditorPrefs.SetBool(OverlayKey, value);
    }

    /// <summary>이만큼(MB) 넘으면 위험으로 보고 덤프를 남긴다. iOS 기기 한도보다 낮게 잡는다.</summary>
    public static int BudgetMegabytes {
        get => EditorPrefs.GetInt(BudgetKey, 1400);
        set => EditorPrefs.SetInt(BudgetKey, value);
    }

    /// <summary>한 샘플 사이에 이만큼(MB) 늘면 급증으로 본다.</summary>
    public static int SpikeMegabytes {
        get => EditorPrefs.GetInt(SpikeKey, 128);
        set => EditorPrefs.SetInt(SpikeKey, value);
    }

    /// <summary>자산 보고서에 나열할 상위 개수.</summary>
    public const int TopAssetCount = 25;

    public static long BudgetBytes => (long)BudgetMegabytes * 1024L * 1024L;
    public static long SpikeBytes => (long)SpikeMegabytes * 1024L * 1024L;
}
#endif
