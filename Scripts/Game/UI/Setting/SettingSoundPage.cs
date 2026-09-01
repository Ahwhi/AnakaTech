using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 사운드 분류 페이지. 마스터/BGM/SFX 볼륨을 슬라이더로 조절한다.
/// 볼륨 값의 주인은 ISoundService(적용 + 저장까지 담당)이고, 이 페이지는 슬라이더와 서비스를 잇고
/// 현재값을 퍼센트로 보여 주기만 한다. 새 볼륨 채널이 생기면 슬라이더 한 쌍을 추가하면 된다.
/// </summary>
public sealed class SettingSoundPage : SettingPage {
    // SFX 미리듣기 최소 간격(초). 슬라이더를 끌 때 클릭음이 겹쳐 들리지 않게 한다.
    private const float PreviewInterval = 0.15f;

    [Header("Slider")]
    [Tooltip("전체 볼륨. BGM/SFX 에 곱해진다.")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Value Text (선택)")]
    [Tooltip("현재 볼륨을 퍼센트로 표시할 텍스트. 비우면 표시만 생략한다.")]
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text bgmValueText;
    [SerializeField] private TMP_Text sfxValueText;

    protected override void OnInitialize() {
        if (Sound == null) return;

        BindVolume(masterSlider, masterValueText, Sound.MasterVolume, value => Sound.MasterVolume = value);
        BindVolume(bgmSlider, bgmValueText, Sound.BgmVolume, value => Sound.BgmVolume = value);
        BindVolume(sfxSlider, sfxValueText, Sound.SfxVolume, value => Sound.SfxVolume = value);

        // 효과음은 소리를 들어야 맞출 수 있으므로 조절 중 미리듣기를 낸다(겹쳐 울리지 않도록 간격을 둔다).
        if (sfxSlider != null) {
            sfxSlider.onValueChanged.AsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(PreviewInterval))
                .Subscribe(_ => Sound.PlaySfx(SfxKey.ButtonClick))
                .AddTo(this);
        }
    }

    private void BindVolume(Slider slider, TMP_Text valueText, float current, Action<float> apply) {
        ShowPercent(valueText, current);
        BindSlider(slider, 0f, 1f, current, value => {
            apply(value);
            ShowPercent(valueText, value);
        });
    }

    private static void ShowPercent(TMP_Text text, float value) {
        if (text != null) text.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }
}
