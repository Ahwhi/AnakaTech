using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using static Enums;

/// <summary>
/// 화면 잠금(절전) 모드. 화면 전체를 덮어 오조작을 막고 현재 시각과 방치 진행 현황, 배터리를 보여 준다.
/// 방치형이라 게임은 뒤에서 그대로 돌아간다(타임스케일을 건드리지 않는다).
///
/// 잠금 화면이 떠 있는 동안 절전 모드(PowerSaveMode)를 켠다. 화면은 꺼지지 않은 채로 사냥을 계속하고,
/// 뒤의 전투/HUD 그리기, 소리, 프레임을 각 담당이 스스로 줄인다. 이 화면은 "켜고 끈다"만 알리고
/// 무엇을 줄이는지는 모른다(PowerSaveMode 참고).
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

    [Header("Status")]
    [Tooltip("방치 진행 현황(스테이지/기력/부스트/쓴 기력/지속 시간). 비워 두면 표시하지 않는다.")]
    [SerializeField] private ScreenLockStatusPanel statusPanel;

    private MainHudContext _ctx;
    private IDisposable _powerSave;

    // 잠금 화면이 살아 있는 동안만 절전 모드를 켠다. 활성/비활성에 맞춰 두므로 어떤 경로로 닫히든 원복된다.
    private void OnEnable() => _powerSave = PowerSaveMode.Enter();
    private void OnDisable() => ExitPowerSave();

    private void ExitPowerSave() {
        _powerSave?.Dispose();
        _powerSave = null;
    }

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;

        if (statusPanel != null) statusPanel.Initialize(ctx);

        RefreshClock();
        // 표시 형식에 초가 들어갈 수도 있으므로 1초마다 갱신한다. 타임스케일과 무관하게 흘러야 하므로 실시간 스케줄러를 쓴다.
        Observable.Interval(TimeSpan.FromSeconds(1), Scheduler.MainThreadIgnoreTimeScale)
            .Subscribe(_ => RefreshClock())
            .AddTo(this);

        if (unlockButton != null)
            unlockButton.Completed.Subscribe(_ => Unlock()).AddTo(this);
    }

    private void Unlock() {
        // 절전 모드에서는 소리가 나지 않으므로, 해제 효과음이 들리도록 닫힘 연출을 기다리지 않고 먼저 푼다.
        ExitPowerSave();
        _ctx?.Sound.PlaySfx(SfxKey.ButtonClick);
        CloseAsync().Forget();
    }

    private void RefreshClock() {
        var now = DateTime.Now;
        if (clockText != null) clockText.text = now.ToString(clockFormat);
        if (dateText != null) dateText.text = now.ToString(dateFormat);
    }
}
