using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using static Enums;

/// <summary>
/// 화면 잠금(잠수) 모드. 화면 전체를 덮어 오조작을 막고 현재 시각을 시계로 보여 준다.
/// 방치형이라 게임은 뒤에서 그대로 돌아간다(타임스케일을 건드리지 않는다).
///
/// 잠금 중에는 화면 절전을 허용한다. 평소에는 화면이 꺼지지 않게 막아 두지만(방치 화면을 계속 보는 게임),
/// "잠갔다"는 것은 곧 "지금은 안 본다"는 뜻이기 때문이다. 이 규칙 덕분에 절전 설정을 따로 둘 필요가 없고,
/// 유저가 정할 것은 자동 잠금 대기 시간 하나로 끝난다.
///
/// 여는 경로는 둘이고 모두 이 프리팹을 그대로 띄운다.
///   - 수동: 메뉴의 잠금 버튼(HudMenuButton 에 이 프리팹을 지정)
///   - 자동: ScreenLockWatcher 가 무입력 시간을 재서 연다
/// </summary>
public sealed class ScreenLockPopup : UIView, IHudPopup {
    [Header("Clock")]
    [Tooltip("시각 표시.")]
    [SerializeField] private TMP_Text clockText;
    [Tooltip("날짜 표시(선택).")]
    [SerializeField] private TMP_Text dateText;
    [Tooltip("시각 표시 형식. 12시간제로 바꾸려면 hh:mm tt 처럼 고친다.")]
    [SerializeField] private string clockFormat = "HH:mm";
    [Tooltip("날짜 표시 형식.")]
    [SerializeField] private string dateFormat = "M월 d일";

    [Header("Unlock")]
    [Tooltip("길게 눌러 잠금을 푸는 버튼. 실수로 풀리지 않도록 한 번의 탭으로는 열리지 않게 한다.")]
    [SerializeField] private UIHoldButton unlockButton;

    private MainHudContext _ctx;

    // 잠금 화면이 살아 있는 동안만 절전을 허용한다. 활성/비활성에 맞춰 두므로 어떤 경로로 닫히든 원복된다.
    private void OnEnable() => DisplaySettings.SetSleepAllowed(true);
    private void OnDisable() => DisplaySettings.SetSleepAllowed(false);

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;

        RefreshClock();
        // 표시 형식에 초가 들어갈 수도 있으므로 1초마다 갱신한다. 타임스케일과 무관하게 흘러야 하므로 실시간 스케줄러를 쓴다.
        Observable.Interval(TimeSpan.FromSeconds(1), Scheduler.MainThreadIgnoreTimeScale)
            .Subscribe(_ => RefreshClock())
            .AddTo(this);

        if (unlockButton != null)
            unlockButton.Completed.Subscribe(_ => Unlock()).AddTo(this);
    }

    private void Unlock() {
        _ctx?.Sound.PlaySfx(SfxKey.ButtonClick);
        CloseAsync().Forget();
    }

    private void RefreshClock() {
        var now = DateTime.Now;
        if (clockText != null) clockText.text = now.ToString(clockFormat);
        if (dateText != null) dateText.text = now.ToString(dateFormat);
    }
}
