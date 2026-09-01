using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 설정 팝업의 분류 탭 한 장(사운드/화면/게임 ...). 페이지는 자기 분류의 항목만 알고,
/// 탭 전환과 열고 닫기는 SettingPopup 이 맡는다(페이지끼리 서로를 모른다).
///
/// 새 분류를 추가할 때는 이 클래스를 상속한 컴포넌트를 만들어 페이지 오브젝트에 붙이고
/// SettingPopup 의 분류 목록에 넣기만 하면 되며, 팝업 코드는 고치지 않는다.
///
/// 아래 바인딩 함수들은 "현재값 표시 -> 변경 구독 -> 저장 요청"이라는 설정 UI 공통 절차를 담아
/// 페이지마다 같은 코드를 반복하지 않게 한다. 구독은 페이지가 파괴될 때 함께 정리된다.
/// </summary>
public abstract class SettingPage : MonoBehaviour {
    /// <summary>
    /// HUD 공용 의존성 묶음. 대부분의 페이지는 사운드만 쓰지만, 계정 탭처럼 서버 통신이 필요한 페이지도 있다.
    /// 팝업은 어드레서블로 인스턴스화되어 컨테이너 주입 대상이 아니므로, 다른 팝업과 같은 방식으로 컨텍스트를 넘겨받는다.
    /// </summary>
    protected MainHudContext Ctx { get; private set; }

    /// <summary>클릭음/미리듣기용. 대부분의 페이지는 이것만 쓴다.</summary>
    protected ISoundService Sound => Ctx?.Sound;

    /// <summary>SettingPopup 이 호출한다. 페이지 오브젝트가 꺼져 있어도 안전하다(활성화 시점과 무관).</summary>
    public void Initialize(MainHudContext ctx) {
        Ctx = ctx;
        OnInitialize();
    }

    /// <summary>페이지의 항목들을 현재 저장값으로 맞추고 변경을 구독한다.</summary>
    protected abstract void OnInitialize();

    protected void PlayClick() => Sound?.PlaySfx(SfxKey.ButtonClick);

    /// <summary>토글을 현재값으로 맞추고, 이후 변경을 apply 로 넘긴다(클릭음 + 저장 요청 포함).</summary>
    protected void BindToggle(Toggle toggle, bool current, Action<bool> apply) {
        if (toggle == null || apply == null) return;

        toggle.SetIsOnWithoutNotify(current);
        toggle.onValueChanged.AsObservable()
            .Subscribe(value => {
                PlayClick();
                apply(value);
                UserDataManager.RequestSave();
            })
            .AddTo(this);
    }

    /// <summary>
    /// 라디오 버튼(토글 묶음) 바인딩. 선택지가 3개 이하인 항목은 한눈에 보이도록 이쪽을 쓴다(4개 이상이면 BindDropdown).
    /// toggles[i] 는 values[i] 를 뜻하며, 현재 선택만 켜진 상태로 시작한다.
    /// 하나를 켜면 나머지는 자동으로 꺼지고, 켜져 있는 것을 다시 눌러 끄는 조작은 되돌린다(항상 하나는 선택된 상태).
    /// 배타 선택을 코드가 보장하므로 ToggleGroup 은 붙여도 되고 안 붙여도 된다.
    /// 선택지가 늘면 토글 배열과 값 배열에 같은 순서로 한 칸씩만 추가하면 된다.
    /// </summary>
    protected void BindRadio<T>(IReadOnlyList<Toggle> toggles, IReadOnlyList<T> values, int currentIndex, Action<T> apply) {
        if (toggles == null || values == null || apply == null) return;

        int count = Mathf.Min(toggles.Count, values.Count);
        for (int i = 0; i < count; i++) {
            var toggle = toggles[i];
            if (toggle == null) continue;

            toggle.SetIsOnWithoutNotify(i == currentIndex);

            int index = i;
            toggle.onValueChanged.AsObservable()
                .Subscribe(on => {
                    // 꺼짐 알림은 다른 토글이 켜지면서 생긴 것이라 무시한다.
                    // 다만 켜져 있던 것을 직접 눌러 끈 경우(아무것도 선택되지 않음)에는 되돌린다.
                    if (!on) {
                        if (!AnyOn(toggles, count)) toggle.SetIsOnWithoutNotify(true);
                        return;
                    }

                    PlayClick();
                    SelectOnly(toggles, count, index);
                    apply(values[index]);
                    UserDataManager.RequestSave();
                })
                .AddTo(this);
        }
    }

    private static bool AnyOn(IReadOnlyList<Toggle> toggles, int count) {
        for (int i = 0; i < count; i++)
            if (toggles[i] != null && toggles[i].isOn) return true;
        return false;
    }

    // 선택된 것만 남기고 끈다. 알림 없이 바꿔 연쇄 콜백이 일어나지 않게 한다.
    private static void SelectOnly(IReadOnlyList<Toggle> toggles, int count, int index) {
        for (int i = 0; i < count; i++) {
            if (i == index || toggles[i] == null) continue;
            toggles[i].SetIsOnWithoutNotify(false);
        }
    }

    /// <summary>
    /// 드롭다운 항목을 문구 목록으로 채우고 현재 선택을 맞춘 뒤, 선택 변경을 apply(선택 위치)로 넘긴다.
    /// 항목을 코드에서 채우므로 선택지가 늘어도 인스펙터를 다시 손볼 필요가 없다.
    /// 선택지가 4개 이상이라 라디오 버튼으로 늘어놓으면 자리를 너무 차지하는 항목에만 쓴다(3개 이하는 BindRadio).
    /// </summary>
    protected void BindDropdown(TMP_Dropdown dropdown, IReadOnlyList<string> labels, int current, Action<int> apply) {
        if (dropdown == null || labels == null || labels.Count == 0 || apply == null) return;

        var options = new List<string>(labels.Count);
        for (int i = 0; i < labels.Count; i++) options.Add(labels[i]);

        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(Mathf.Clamp(current, 0, options.Count - 1));
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AsObservable()
            .Subscribe(index => {
                PlayClick();
                apply(index);
                UserDataManager.RequestSave();
            })
            .AddTo(this);
    }

    /// <summary>
    /// 슬라이더 범위를 잡고 현재값으로 맞춘 뒤, 값 변경을 apply 로 넘긴다(저장 요청 포함).
    /// 드래그 중 연속으로 들어오므로 클릭음은 내지 않는다.
    /// </summary>
    protected void BindSlider(Slider slider, float min, float max, float current, Action<float> apply) {
        if (slider == null || apply == null) return;

        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(Mathf.Clamp(current, min, max));
        slider.onValueChanged.AsObservable()
            .Subscribe(value => {
                apply(value);
                UserDataManager.RequestSave();
            })
            .AddTo(this);
    }
}
