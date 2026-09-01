#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개발 빌드 전용 화면 로그. 케이블을 연결해 logcat/Xcode 콘솔을 볼 수 없는 상황에서 기기 화면만으로
/// 로그를 읽기 위한 창구다.
///
/// 씬이나 프리팹을 건드리지 않고 스스로 캔버스를 만들어 뜬다(MemoryOverlay 와 같은 방식). 진단을 켜려고
/// 씬 구성을 바꾸면 그 변경 자체가 테스트 대상에 섞이고, 빼는 것을 잊으면 그대로 출시되기 때문이다.
///
/// 평소에 화면을 가리지 않도록 새 로그가 올라올 때만 나타나고, 마지막 줄이 수명을 넘기면 스스로 사라진다.
/// 레이캐스트 대상이 아니므로 게임 입력을 가로채는 일도 없다.
///
/// 기기에서 돌린 개발 빌드에서만 존재한다(DEVELOPMENT_BUILD && !UNITY_EDITOR).
///   - 출시 빌드   : 이 파일 전체가 컴파일되지 않는다.
///   - 에디터 게임뷰: 콘솔 창에서 같은 로그를 그대로 볼 수 있으므로 화면을 덮을 이유가 없다.
///     (게임뷰를 가리면 정작 확인하려던 연출/UI 테스트를 방해한다)
/// 기기에서 이 화면을 보려면 Build Settings 의 Development Build 를 켜고 빌드해야 한다.
/// </summary>
public sealed class DeviceLogOverlay : MonoBehaviour {
    private const int SortingOrder = 32750;   // 어떤 팝업/로딩보다 위, 메모리 오버레이(32760)보다는 아래.
    private const int MaxLines = 14;          // 화면을 절반 이상 덮지 않는 선.
    private const float LineLifetime = 12f;   // 이 시간이 지난 줄은 지운다.
    private const int MaxLineLength = 200;    // 스택 한 줄이 통째로 들어와 화면을 채우는 것을 막는다.

    private readonly List<Entry> _entries = new();
    private readonly StringBuilder _builder = new();

    private GameObject _panel;
    private TMP_Text _text;
    private bool _dirty;

    private readonly struct Entry {
        public readonly string Text;
        public readonly float ExpiresAt;

        public Entry(string text, float expiresAt) {
            Text = text;
            ExpiresAt = expiresAt;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() {
        var go = new GameObject("[DeviceLogOverlay]");
        DontDestroyOnLoad(go);
        go.AddComponent<DeviceLogOverlay>();
    }

    private void Awake() {
        BuildView();
        // 스레드 버전(logMessageReceivedThreaded)이 아니라 메인 스레드 버전을 쓴다. 여기서 UI 를 만지기 때문이다.
        Application.logMessageReceived += OnLogMessage;
    }

    private void OnDestroy() => Application.logMessageReceived -= OnLogMessage;

    // 캔버스와 텍스트를 코드로 구성한다. 폰트는 TMP 기본 폰트(프로젝트 한글 폰트)를 쓰므로 배선이 필요 없다.
    private void BuildView() {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(transform, false);

        var background = _panel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.62f);
        background.raycastTarget = false;   // 게임 입력을 절대 가로채지 않는다.

        var panelRect = (RectTransform)_panel.transform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0f, 520f);

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(_panel.transform, false);

        _text = textObject.AddComponent<TextMeshProUGUI>();
        _text.font = ResolveFont();
        _text.fontSize = 22f;
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.overflowMode = TextOverflowModes.Truncate;
        _text.raycastTarget = false;
        _text.richText = true;

        var textRect = _text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 12f);
        textRect.offsetMax = new Vector2(-16f, -12f);

        _panel.SetActive(false);   // 보여 줄 것이 생길 때만 나타난다.
    }

    // TMP 기본 폰트(프로젝트 설정의 한글 폰트). 설정이 비어 있으면 TMP 패키지 기본 폰트로 물러선다.
    private static TMP_FontAsset ResolveFont() =>
        TMP_Settings.defaultFontAsset != null
            ? TMP_Settings.defaultFontAsset
            : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

    private void OnLogMessage(string condition, string stackTrace, LogType type) {
        if (_entries.Count >= MaxLines) _entries.RemoveAt(0);

        string body = condition ?? "";
        if (body.Length > MaxLineLength) body = body.Substring(0, MaxLineLength) + "...";

        _entries.Add(new Entry($"<color={ColorOf(type)}>{body}</color>", Time.unscaledTime + LineLifetime));
        _dirty = true;
    }

    private static string ColorOf(LogType type) => type switch {
        LogType.Warning => "#FFD24D",
        LogType.Log     => "#DDDDDD",
        _               => "#FF6060",   // Error / Exception / Assert
    };

    private void Update() {
        // 수명이 지난 줄부터 지운다(추가 순서가 곧 만료 순서라 앞에서부터 보면 된다).
        while (_entries.Count > 0 && _entries[0].ExpiresAt <= Time.unscaledTime) {
            _entries.RemoveAt(0);
            _dirty = true;
        }

        if (!_dirty) return;
        _dirty = false;

        if (_entries.Count == 0) {
            if (_panel.activeSelf) _panel.SetActive(false);
            return;
        }

        _builder.Clear();
        for (int i = 0; i < _entries.Count; i++) {
            if (i > 0) _builder.Append('\n');
            _builder.Append(_entries[i].Text);
        }
        _text.text = _builder.ToString();

        if (!_panel.activeSelf) _panel.SetActive(true);
    }
}
#endif
