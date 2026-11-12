using UnityEngine;

/// <summary>
/// 화면(그래픽) 설정의 단일 창구. 저장값(GameData)을 엔진 설정으로 옮기는 규칙만 갖는다.
/// 앱 시작(AppLifecycle/AppInitializeStep)과 설정 변경(SettingDisplayPage)이 모두 Apply 를 부르므로,
/// "저장값 -> 실제 화면"의 규칙은 이 파일 한 곳에만 존재한다(UI 는 무엇을 적용하는지 알 필요가 없다).
///
/// 선택지는 아래 옵션표가 단일 진실 원천이다. UI 는 이 표와 순서로만 짝지어지므로,
/// 선택지를 늘릴 때 표에 한 칸 추가하고 UI 에 버튼 하나를 같은 자리에 맞춰 두면 된다.
/// </summary>
public static class DisplaySettings {
    /// <summary>목표 프레임 옵션값. 설정 UI 의 라디오 버튼과 같은 순서로 짝지어진다.</summary>
    public static readonly int[] FrameRateOptions = { 30, 60 };

    /// <summary>렌더 해상도 배율 옵션값(1 = 기기 해상도). 설정 UI 의 라디오 버튼과 같은 순서로 짝지어진다.</summary>
    public static readonly float[] RenderScaleOptions = { 1f, 0.85f, 0.7f };

    /// <summary>자동 화면 잠금 대기 시간 옵션값(초). 0 은 "사용 안 함"을 뜻한다.</summary>
    public static readonly int[] AutoLockOptions = { 0, 60, 300 };

    /// <summary>허용하는 최소 렌더 해상도 배율. 세이브 값 검증에도 쓴다.</summary>
    public const float MinRenderScale = 0.5f;

    /// <summary>
    /// UI 좌우 여백의 최대치(캔버스 기준 단위). 슬라이더 범위와 세이브 값 검증이 같은 값을 본다.
    /// 이 위로 더 넓히고 싶으면 여기 한 곳만 고치면 UI 와 저장 검증이 함께 따라온다.
    /// </summary>
    public const float MaxUiSideInset = 50f;

    /// <summary>
    /// UI 좌우 여백의 최소치(음수). 음수는 UI 를 Safe Area 바깥으로 되밀어 화면을 더 넓게 쓰게 한다.
    /// 노치나 둥근 모서리 때문에 Safe Area 가 넓게 잡히는 기기에서 양옆이 남는 것이 싫은 유저를 위한 범위다.
    /// 기본값은 여전히 0(Safe Area 그대로)이라 아무것도 조작하지 않은 유저의 화면은 달라지지 않는다.
    /// </summary>
    public const float MinUiSideInset = -50f;

    // 기기 기본 프레임. 모바일은 발열/배터리를 고려해 60 으로 두고, PC 는 상한만 걸어 둔다.
#if UNITY_ANDROID || UNITY_IOS
    private const int PlatformDefaultFrameRate = 60;
#else
    private const int PlatformDefaultFrameRate = 144;
#endif

    // 렌더 해상도 배율의 기준이 되는 기기 네이티브 해상도. 배율을 여러 번 바꿔도 기준이 흔들리지 않도록 한 번만 기억한다.
    private static int _nativeWidth;
    private static int _nativeHeight;

    // 절전 모드(PowerSaveMode) 동안의 프레임 상한. 설정 화면의 최저 옵션(30)과 같은 값이다.
    // 전투 쿨타임은 프레임 단위로 끊겨 흐르므로(남은 시간을 다음 쿨타임에 넘기지 않는다) 프레임이 낮을수록
    // 공속이 빠른 유닛의 공격 횟수가 조금씩 줄어든다. 30 은 라이브에서 유저가 이미 고를 수 있는 값이라
    // 전투 결과가 검증된 범위지만, 그 아래는 사냥 속도(시간당 보상)가 달라질 수 있어 내리지 않는다.
    private const int PowerSaveFrameRate = 30;

    // 지금 적용 중인 UI 좌우 여백. SafeAreaFitter 가 매 프레임 읽어 비교하므로 세이브를 직접 뒤지지 않고
    // 검증까지 끝낸 값을 여기 한 번만 캐시해 둔다(세이브 로드 전에도 안전한 기본값 0 을 갖는다).
    private static float _uiSideInset;

    /// <summary>
    /// 렌더 해상도 배율을 제공하는 플랫폼인지. PC 는 해상도를 바꾸면 창 크기까지 바뀌어 버리므로 제공하지 않는다.
    /// 이 값이 false 면 설정 UI 도 해당 항목을 감춘다.
    /// </summary>
    public static bool SupportsRenderScale => Application.isMobilePlatform;

    /// <summary>
    /// 지금 적용할 UI 좌우 여백(MinUiSideInset ~ MaxUiSideInset). SafeAreaFitter 가 이 값만 보고 자기 영역을
    /// 좁히거나(양수) 넓힌다(음수).
    /// 값이 바뀌었는지는 각 Fitter 가 스스로 확인하므로, 여기서 화면의 존재를 알거나 누군가에게 알릴 필요가 없다.
    /// </summary>
    public static float UiSideInset => _uiSideInset;

    /// <summary>저장된 화면 설정을 실제 화면에 모두 적용한다. 세이브 로드 전(Data == null)이면 기본 동작으로 맞춘다.</summary>
    public static void Apply() {
        var data = UserDataManager.Data;

        ApplyVSync(data != null && data.VSync);
        ApplyFrameRate(data != null ? data.FrameRateLimit : 0);
        ApplySleepTimeout();
        ApplyRenderScale(data != null ? data.RenderScale : 1f);
        ApplyUiSideInset(data != null ? data.UiSideInset : 0f);
    }

    /// <summary>
    /// UI 좌우 여백을 바꾼다(설정 화면의 슬라이더 전용 창구). 저장값과 적용값을 함께 갱신한다.
    ///
    /// 슬라이더는 드래그 중 매 프레임 들어오므로 Apply 전체를 다시 돌리지 않는다. 여백은 다른 화면 설정과
    /// 얽히는 것이 없어 이 값만 갈아 끼우면 되고, 해상도 재설정처럼 비싼 일을 끌고 들어오지 않는다.
    /// </summary>
    public static void SetUiSideInset(float value) {
        ApplyUiSideInset(value);

        var data = UserDataManager.Data;
        if (data != null) data.UiSideInset = _uiSideInset;
    }

    /// <summary>
    /// 저장된 목표 프레임에 해당하는 옵션 위치. 저장값이 "기기 기본"(0)이거나 옵션표에 없는 값이면
    /// 실제로 적용 중인 프레임에 가장 가까운 옵션을 고른다(라디오 버튼이 항상 하나는 선택되도록).
    /// </summary>
    public static int FrameRateIndex(int value) =>
        NearestIndex(FrameRateOptions, value > 0 ? value : PlatformDefaultFrameRate);

    /// <summary>저장된 자동 잠금 대기 시간에 해당하는 옵션 위치(0 = 사용 안 함).</summary>
    public static int AutoLockIndex(int seconds) => NearestIndex(AutoLockOptions, seconds);

    /// <summary>저장된 렌더 해상도 배율에 가장 가까운 옵션 위치. 옵션표가 바뀌어 값이 어긋나도 가장 비슷한 항목을 고른다.</summary>
    public static int RenderScaleIndex(float value) {
        int nearest = 0;
        float bestGap = float.MaxValue;
        for (int i = 0; i < RenderScaleOptions.Length; i++) {
            float gap = Mathf.Abs(RenderScaleOptions[i] - value);
            if (gap >= bestGap) continue;
            bestGap = gap;
            nearest = i;
        }
        return nearest;
    }

    // 옵션표가 바뀌어 저장값이 어긋나도 항상 하나는 선택되도록, 가장 가까운 항목을 고른다.
    private static int NearestIndex(int[] options, int target) {
        int nearest = 0;
        int bestGap = int.MaxValue;
        for (int i = 0; i < options.Length; i++) {
            int gap = Mathf.Abs(options[i] - target);
            if (gap >= bestGap) continue;
            bestGap = gap;
            nearest = i;
        }
        return nearest;
    }

    // 수직 동기화를 켜면 엔진이 주사율에 맞춰 그리므로 목표 프레임 값은 무시된다(설정 UI 가 그 사실을 알려 준다).
    // 절전 모드에서는 끈다. 켜 둔 채로는 고주사율 기기(120Hz)에서 프레임 상한이 먹지 않는다.
    private static void ApplyVSync(bool on) =>
        QualitySettings.vSyncCount = on && !PowerSaveMode.Active.Value ? 1 : 0;

    // 절전 모드에서는 유저 설정과 절전 상한 중 낮은 쪽을 쓴다(이미 30 으로 둔 유저는 그대로다).
    private static void ApplyFrameRate(int limit) {
        int target = limit > 0 ? limit : PlatformDefaultFrameRate;
        Application.targetFrameRate = PowerSaveMode.Active.Value ? Mathf.Min(target, PowerSaveFrameRate) : target;
    }

    // 들어온 값을 허용 범위로 정리해 캐시에 넣는다. NaN 은 비교가 전부 false 라 Clamp 를 그냥 통과하므로
    // 먼저 걸러 낸다(NaN 이 레이아웃에 들어가면 그 아래 UI 가 통째로 사라진다).
    private static void ApplyUiSideInset(float value) =>
        _uiSideInset = float.IsNaN(value) ? 0f : Mathf.Clamp(value, MinUiSideInset, MaxUiSideInset);

    // 화면은 항상 켜 둔다. 화면이 꺼지면 모바일 OS 가 앱을 멈춰 방치 사냥도 함께 멈추기 때문이다.
    // 절전 모드도 예외가 아니다(절전 모드는 "화면을 켠 채 최소 부하로 계속 사냥"이다).
    private static void ApplySleepTimeout() => Screen.sleepTimeout = SleepTimeout.NeverSleep;

    // 기기 네이티브 해상도에 배율을 곱한 크기로 렌더한다. 화면 크기(UI 레이아웃)는 그대로고 픽셀 수만 줄어든다.
    private static void ApplyRenderScale(float scale) {
        if (!SupportsRenderScale) return;

        float clamped = Mathf.Clamp(scale, MinRenderScale, 1f);
        // 배율이 1 이면 기기가 준 해상도가 곧 목표값이다. 여기서 SetResolution 을 부르면
        // 얻는 것 없이 렌더 타겟만 다시 만들어지므로(모바일에서는 그 비용이 크다) 손대지 않는다.
        if (clamped >= 1f) return;

        if (_nativeWidth <= 0 || _nativeHeight <= 0) {
            _nativeWidth = Display.main.systemWidth;
            _nativeHeight = Display.main.systemHeight;
        }
        if (_nativeWidth <= 0 || _nativeHeight <= 0) return;

        // Display.main 은 기기 기준 값이라 세로 기준으로 올 수 있는 반면 Screen 은 현재 방향 기준이다.
        // 둘을 그대로 비교하면 가로 게임에서 항상 어긋나, 매번 세로 해상도를 강제로 지정하게 된다.
        // 긴 변/짧은 변으로 풀어 현재 방향에 맞춘 뒤 비교한다.
        int longSide = Mathf.Max(_nativeWidth, _nativeHeight);
        int shortSide = Mathf.Min(_nativeWidth, _nativeHeight);
        bool landscape = Screen.width >= Screen.height;

        int width = Mathf.Max(1, Mathf.RoundToInt((landscape ? longSide : shortSide) * clamped));
        int height = Mathf.Max(1, Mathf.RoundToInt((landscape ? shortSide : longSide) * clamped));
        if (Screen.width == width && Screen.height == height) return;

        Screen.SetResolution(width, height, Screen.fullScreenMode);
    }
}
