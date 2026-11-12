using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 분류 페이지. 목표 프레임(30/60), 수직 동기화, 렌더 해상도 배율, 자동 화면 잠금 대기 시간을 다룬다.
/// 저장은 GameData 에, 실제 적용은 DisplaySettings 에 맡기므로 이 페이지는 UI 배선만 담당한다.
/// 선택지는 DisplaySettings 의 옵션표가 단일 진실 원천이라, 값을 늘려도 여기를 고칠 일이 없다.
///
/// 화면 꺼짐은 설정 항목이 아니다. 항상 막아 두고(꺼지면 방치 사냥이 멈춘다) 잠금 중에는 절전 모드로 부하만 줄이므로,
/// 유저가 정할 것은 "언제 자동으로 잠글지" 하나뿐이다.
/// </summary>
public sealed class SettingDisplayPage : SettingPage {
    [Tooltip("목표 프레임 라디오 버튼. DisplaySettings.FrameRateOptions 와 같은 순서(30, 60)로 넣는다.")]
    [SerializeField] private Toggle[] frameRateToggles;
    [Tooltip("수직 동기화 토글. 켜면 목표 프레임 대신 화면 주사율을 따른다.")]
    [SerializeField] private Toggle vSyncToggle;

    [Tooltip("렌더 해상도 라디오 버튼. DisplaySettings.RenderScaleOptions 와 같은 순서(높음, 보통, 낮음)로 넣는다.")]
    [SerializeField] private Toggle[] renderScaleToggles;
    [Tooltip("렌더 해상도 항목 한 줄(설명 라벨 포함). 배율을 제공하지 않는 플랫폼에서는 통째로 감춘다.")]
    [SerializeField] private GameObject renderScaleRow;

    [Tooltip("자동 화면 잠금 대기 시간 라디오 버튼. DisplaySettings.AutoLockOptions 와 같은 순서(사용 안 함, 1분, 5분)로 넣는다.")]
    [SerializeField] private Toggle[] autoLockToggles;

    [Tooltip("UI 좌우 여백 슬라이더. 양수는 화면 양옆의 UI 를 안쪽으로 당겨 기기 가장자리에 걸리는 것을 피하고, " +
             "음수는 반대로 Safe Area 바깥까지 넓혀 화면을 더 크게 쓴다(0 이 기본). " +
             "범위는 DisplaySettings 의 Min/MaxUiSideInset 이 정하므로 인스펙터에서 따로 맞출 필요가 없다.")]
    [SerializeField] private Slider uiSideInsetSlider;
    [Tooltip("현재 여백 값을 보여 줄 텍스트(선택). 비우면 표시만 생략한다.")]
    [SerializeField] private TMP_Text uiSideInsetValueText;

    protected override void OnInitialize() {
        var data = UserDataManager.Data;
        if (data == null) return;

        BindRadio(frameRateToggles, DisplaySettings.FrameRateOptions,
            DisplaySettings.FrameRateIndex(data.FrameRateLimit),
            fps => {
                data.FrameRateLimit = fps;
                DisplaySettings.Apply();
            });

        BindToggle(vSyncToggle, data.VSync, value => {
            data.VSync = value;
            DisplaySettings.Apply();
            RefreshFrameRateEnabled();
        });

        // 자동 잠금은 저장만 하면 된다. 값을 매번 읽는 것은 감시자(ScreenLockWatcher)라 즉시 반영된다.
        BindRadio(autoLockToggles, DisplaySettings.AutoLockOptions,
            DisplaySettings.AutoLockIndex(data.AutoLockSeconds),
            seconds => data.AutoLockSeconds = seconds);

        BindRenderScale(data);
        BindUiSideInset(data);
        RefreshFrameRateEnabled();
    }

    // UI 좌우 여백. 값의 검증과 적용 창구는 DisplaySettings 이므로 여기서는 슬라이더를 잇고 숫자만 보여 준다.
    // 화면에 붙어 있는 SafeAreaFitter 들이 스스로 값을 확인해 따라오므로, 이 페이지는 누가 그 여백을
    // 쓰는지 알 필요가 없다(설정 화면이 UI 전체를 뒤지는 일이 생기지 않는다).
    private void BindUiSideInset(GameData data) {
        ShowInset(data.UiSideInset);
        BindSlider(uiSideInsetSlider, DisplaySettings.MinUiSideInset, DisplaySettings.MaxUiSideInset, data.UiSideInset, value => {
            DisplaySettings.SetUiSideInset(value);
            // 슬라이더가 준 값이 아니라 창구가 정리한 값을 보여 준다(범위를 벗어난 값이 화면에만 남지 않게).
            ShowInset(DisplaySettings.UiSideInset);
        });
    }

    private void ShowInset(float value) {
        if (uiSideInsetValueText != null) uiSideInsetValueText.text = Mathf.RoundToInt(value).ToString();
    }

    // 렌더 해상도 배율은 창 크기까지 바뀌는 PC 에서는 제공하지 않는다. 지원하지 않으면 항목 자체를 감춘다.
    private void BindRenderScale(GameData data) {
        bool supported = DisplaySettings.SupportsRenderScale;

        // 설명 라벨까지 함께 감추도록 한 줄(row)을 통째로 끄는 것이 기본이고, 없으면 버튼만 끈다.
        if (renderScaleRow != null) renderScaleRow.SetActive(supported);
        else if (renderScaleToggles != null)
            foreach (var toggle in renderScaleToggles)
                if (toggle != null) toggle.gameObject.SetActive(supported);

        if (!supported) return;

        BindRadio(renderScaleToggles, DisplaySettings.RenderScaleOptions,
            DisplaySettings.RenderScaleIndex(data.RenderScale),
            scale => {
                data.RenderScale = scale;
                DisplaySettings.Apply();
            });
    }

    // 수직 동기화를 켜면 목표 프레임 값은 무시되므로, 조작할 수 없다는 것을 라디오 버튼 상태로 알린다.
    private void RefreshFrameRateEnabled() {
        if (frameRateToggles == null) return;

        var data = UserDataManager.Data;
        bool usable = data == null || !data.VSync;
        foreach (var toggle in frameRateToggles)
            if (toggle != null) toggle.interactable = usable;
    }
}
