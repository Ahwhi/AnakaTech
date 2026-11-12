using System;
using UnityEngine;

/// <summary>
/// 소프트 키보드가 가리는 높이(화면 픽셀). 부르는 쪽은 "키보드가 화면을 얼마나 덮었나"만 물으면 되고
/// 어느 API 가 답했는지는 알 필요가 없다. 플랫폼 경계를 여기 한 곳으로 모은다.
///
/// 답이 두 갈래인 이유는 한 API 가 두 플랫폼을 덮지 못하기 때문이다.
///
///   iOS      TouchScreenKeyboard.area 를 OS 가 채워 주며 값이 정확하다.
///   Android  TouchScreenKeyboard.visible 이 true 인데도 area 가 (0,0,0,0) 으로 온다.
///            유니티가 그 값을 구하지 못하므로 액티비티의 윈도우에 직접 묻는다.
///            갤럭시 실측(2340x1080 가로): area.height = 0, 윈도우 인셋 = 578.
///
/// 1.7 의 키보드 작업이 아이폰에서만 동작하고 갤럭시에서는 아무 일도 하지 않은 원인이 정확히 이것이다.
/// 높이가 0 으로 오니 모든 호출부가 "비킬 것이 없다"고 올바르게 판단했다.
///
/// 안드로이드는 두 가지로 묻는다. 정확한 쪽이 모든 버전에 있지는 않기 때문이다.
///   1) WindowInsets.Type.ime()          API 30 이상. 키보드만 보고하므로 내비게이션 바가 섞이지 않는다.
///   2) getWindowVisibleDisplayFrame()   모든 버전에서 되지만 그 간격에 내비게이션 바가 포함되므로,
///                                       키보드라고 보기에 충분히 큰 경우에만 받아들인다.
/// 실측에서 두 값이 똑같이 578 이 나왔으므로 2) 는 믿을 만한 대비책이다.
///
/// 답은 프레임당 한 번만 구한다. 같은 프레임에 여러 패널이 물을 수 있고 JNI 호출은 공짜가 아니다.
/// </summary>
public static class SoftKeyboard {
    /// <summary>화면에서 이 비율보다 작은 간격은 키보드가 아니라 내비게이션 바로 본다.</summary>
    private const float MinKeyboardShare = 0.15f;

    private static int _frame = -1;
    private static float _height;

    /// <summary>키보드가 덮은 높이(화면 픽셀). 닫혀 있거나 알 수 없으면 0.</summary>
    public static float Height {
        get {
            if (Time.frameCount != _frame) {
                _frame = Time.frameCount;
                _height = Resolve();
            }
            return _height;
        }
    }

    /// <summary>키보드가 올라와 있고 그 높이를 알고 있는지.</summary>
    public static bool IsOpen => Height > 0f;

    /// <summary>
    /// 이 기기에 소프트 키보드가 있는지(모바일이면 true, 에디터/PC 면 false).
    /// 초점을 잡는 것이 곧 키보드를 띄우는 일이 되는 환경인지 가리는 데 쓴다.
    /// </summary>
    public static bool IsSupported => TouchScreenKeyboard.isSupported;

    private static float Resolve() {
        if (!TouchScreenKeyboard.isSupported) return 0f;   // 에디터/데스크톱: 소프트 키보드 자체가 없다

#if UNITY_ANDROID && !UNITY_EDITOR
        // 윈도우에 먼저 묻는다. 안드로이드에서는 이것만 동작하고, 키보드가 올라왔는지도 스스로 알기 때문에
        // TouchScreenKeyboard.visible 을 믿지 않아도 된다.
        float native = AndroidHeight();
        if (native > 0f) return native;
#endif

        // 유니티의 답. iOS 에서 정확하며, 안드로이드에서 언젠가 동작하게 되면 그대로 쓰인다.
        if (!TouchScreenKeyboard.visible) return 0f;
        float area = TouchScreenKeyboard.area.height;
        return area > 0f ? area : 0f;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject _decorView;
    private static int _imeTypeMask = -1;

    // 읽기는 잠깐 실패할 수도(액티비티 교체 중) 영영 실패할 수도(기기가 거부) 있다. 몇 번은 다시 시도해
    // 일시적 실패로 기능이 죽지 않게 하고, 그 뒤에는 멈춰서 영구 실패가 매 프레임 로그와 할당을 내지 않게 한다.
    private const int MaxFailures = 3;
    private static int _failures;
    private static bool Unavailable => _failures >= MaxFailures;

    private static float AndroidHeight() {
        if (Unavailable) return 0f;
        if (_decorView == null && !CacheWindow()) return 0f;

        try {
            int decorHeight = _decorView.Call<int>("getHeight");
            if (decorHeight <= 0) return 0f;

            // 안드로이드는 윈도우 픽셀로 답한다. 실측 기기에서는 Screen.height 와 같았지만 해상도 스케일링이
            // 걸린 빌드에서는 어긋나므로, 같다고 가정하지 않고 환산한다.
            float toScreen = (float)Screen.height / decorHeight;

            // 1) 키보드가 덮은 양 그 자체.
            if (_imeTypeMask >= 0) {
                using var insets = _decorView.Call<AndroidJavaObject>("getRootWindowInsets");
                if (insets != null && insets.Call<bool>("isVisible", _imeTypeMask)) {
                    using var ime = insets.Call<AndroidJavaObject>("getInsets", _imeTypeMask);
                    int bottom = ime.Get<int>("bottom");
                    if (bottom > 0) return bottom * toScreen;
                }
            }

            // 2) 보이는 영역 아래로 가려진 부분. 내비게이션 바가 섞여 있으므로 그것으로 보기에는
            //    너무 큰 경우에만 받아들인다.
            using var rect = new AndroidJavaObject("android.graphics.Rect");
            _decorView.Call("getWindowVisibleDisplayFrame", rect);
            int gap = decorHeight - rect.Get<int>("bottom");
            if (gap > decorHeight * MinKeyboardShare) return gap * toScreen;

            return 0f;
        } catch (Exception e) {
            // 뷰를 읽지 못했다. 캐시를 버려 다음 시도가 깨끗한 상태에서 시작하게 한다.
            _failures++;
            Debug.LogWarning($"[SoftKeyboard] 윈도우 조회 실패 ({_failures}/{MaxFailures}): {e.Message}");
            _decorView?.Dispose();
            _decorView = null;
            _imeTypeMask = -1;
            return 0f;
        }
    }

    private static bool CacheWindow() {
        try {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var window = activity.Call<AndroidJavaObject>("getWindow");
            _decorView = window.Call<AndroidJavaObject>("getDecorView");

            // WindowInsets.Type.ime() 은 API 30 부터 있다. 그 아래 기기는 위의 2) 로 넘어간다.
            try {
                using var type = new AndroidJavaClass("android.view.WindowInsets$Type");
                _imeTypeMask = type.CallStatic<int>("ime");
            } catch {
                _imeTypeMask = -1;
            }

            if (_decorView != null) {
                _failures = 0;   // 한 번 제대로 읽으면 앞선 실패는 지운다
                return true;
            }
            _failures++;
            return false;
        } catch (Exception e) {
            _failures++;
            Debug.LogWarning($"[SoftKeyboard] 윈도우를 쓸 수 없음 ({_failures}/{MaxFailures}): {e.Message}");
            return false;
        }
    }
#endif
}
