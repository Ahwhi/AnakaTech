using System;
using UniRx;

/// <summary>
/// 절전 모드 상태의 단일 창구. "지금은 화면을 보지 않지만 사냥은 계속된다"는 상태를 앱 전체에 알린다.
///
/// 이 클래스는 상태만 갖고 무엇을 줄일지는 모른다. 각 담당이 Active 를 구독해 스스로 부하를 내린다.
///   - 프레임/수직동기: DisplaySettings (AppLifecycle 이 연결)
///   - 사운드: SoundManager
///   - 화면 그리기(카메라/캔버스): PowerSaveHidden 을 붙인 오브젝트
/// 그래서 줄일 대상이 늘어도 이 파일이나 절전 모드를 켜는 쪽(잠금 화면)을 고칠 일이 없다.
///
/// 켜는 쪽은 Enter 가 돌려준 핸들을 Dispose 해 끈다. 여러 곳이 동시에 켜도 마지막 핸들이 풀릴 때까지 유지된다.
/// </summary>
public static class PowerSaveMode {
    private static readonly ReactiveProperty<bool> _active = new(false);
    private static int _holders;

    /// <summary>절전 모드가 켜져 있는지. 구독하면 현재 값을 즉시 한 번 받는다.</summary>
    public static IReadOnlyReactiveProperty<bool> Active => _active;

    /// <summary>절전 모드를 켠다. 돌려받은 핸들을 Dispose 하면 이 요청이 풀린다(두 번 풀어도 한 번만 반영된다).</summary>
    public static IDisposable Enter() {
        _holders++;
        _active.Value = true;
        return Disposable.Create(Exit);
    }

    private static void Exit() {
        if (_holders <= 0) return;
        _holders--;
        if (_holders == 0) _active.Value = false;
    }
}
