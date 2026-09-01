#if UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 실시간 메모리 표시 오버레이(에디터 전용 뷰). 씬이나 프리팹을 건드리지 않고 스스로 캔버스를 만들어 뜬다.
/// 진단 도구를 쓰려고 씬 구성을 바꾸면, 정작 그 씬 구성이 측정 대상이라 결과가 흐려지기 때문이다.
///
/// 조작
///   F3   오버레이 표시 토글
///   F4   자산 보고서를 로그로 덤프(종류별 합계 + 큰 자산 상위 N)
///   F5   구간별 메모리 변화 타임라인을 로그로 덤프
///
/// 레이캐스터를 붙이지 않으므로 게임 입력을 가로채지 않는다.
/// </summary>
public sealed class MemoryOverlay : MonoBehaviour {
    private const int SortingOrder = 32760;   // 어떤 팝업보다도 위. 진단 정보는 항상 보여야 한다.

    private MemoryDiagnostics _diagnostics;
    private TMP_Text _text;

    /// <summary>오버레이를 만들어 씬 전환에도 살아남게 한다.</summary>
    public static MemoryOverlay Create(MemoryDiagnostics diagnostics) {
        var go = new GameObject("[MemoryOverlay]");
        DontDestroyOnLoad(go);

        var overlay = go.AddComponent<MemoryOverlay>();
        overlay._diagnostics = diagnostics;
        overlay.BuildView();
        return overlay;
    }

    // 캔버스와 텍스트를 코드로 구성한다. 폰트는 TMP 기본 폰트를 쓰므로 별도 할당이 필요 없다.
    private void BuildView() {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(transform, false);

        _text = textObject.AddComponent<TextMeshProUGUI>();
        _text.font = ResolveFont();
        _text.fontSize = 22f;
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.raycastTarget = false;
        _text.richText = true;

        var rect = _text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(24f, 24f);
        rect.offsetMax = new Vector2(-24f, -24f);
    }

    // TMP 기본 폰트. 프로젝트 설정에 없으면 TMP 패키지 기본 폰트를 리소스에서 찾는다.
    private static TMP_FontAsset ResolveFont() =>
        TMP_Settings.defaultFontAsset != null
            ? TMP_Settings.defaultFontAsset
            : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

    private void Update() {
        Render();
        HandleKeyboard();
    }

    private void Render() {
        if (_text == null || !_text.enabled) return;

        // 표시 기준은 기준선 대비 증가분이다. 에디터에서는 절대값에 에디터 자신의 사용량이 섞여 있어
        // 그대로 보면 게임이 수 GB 를 쓰는 것처럼 오해하게 된다.
        var usage = _diagnostics.Current;
        long app = _diagnostics.AppFootprint;
        long budget = MemoryDiagnosticsSettings.BudgetBytes;
        float ratio = budget > 0 ? (float)app / budget : 0f;
        string color = ratio >= 1f ? "#FF4040" : ratio >= 0.75f ? "#FFC000" : "#40FF80";

        _text.text =
            $"<color={color}>APP {DownloadInfo.FormatBytes(app)}</color>" +
            $"  / 예산 {DownloadInfo.FormatBytes(budget)} ({ratio:P0})\n" +
            $"앱 최대 {DownloadInfo.FormatBytes(_diagnostics.PeakAppFootprint)} @ {_diagnostics.Peak.Time:0.00}s\n" +
            $"프로세스 전체 {DownloadInfo.FormatBytes(usage.Footprint)} (기준선 {DownloadInfo.FormatBytes(_diagnostics.Baseline.Footprint)})\n" +
            usage.ToDetailString().Replace(" | ", "\n") +
            "\nF3 표시 / F4 자산덤프 / F5 타임라인";
    }

    private void HandleKeyboard() {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[Key.F3].wasPressedThisFrame) _text.enabled = !_text.enabled;
        if (keyboard[Key.F4].wasPressedThisFrame)
            Debug.Log(_diagnostics.BuildAssetReport(MemoryDiagnosticsSettings.TopAssetCount));
        if (keyboard[Key.F5].wasPressedThisFrame) Debug.Log(_diagnostics.BuildTimeline());
    }
}
#endif
