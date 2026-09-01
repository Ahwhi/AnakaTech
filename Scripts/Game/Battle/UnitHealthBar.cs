using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 형태만 담당하는 유닛 체력바 위젯(공유 캔버스에 풀링되어 배치된다).
/// 로직/타게팅은 전혀 모르고, 오버레이가 넘겨주는 수치/표시 상태만 반영한다.
/// 체력/게이지는 Slider(value 0~1)로 그린다. Slider 는 인터랙션 없이 표시 전용으로 사용한다
/// (Interactable=off, MinValue=0, MaxValue=1 로 세팅).
/// </summary>
public sealed class UnitHealthBar : MonoBehaviour {
    [Tooltip("바 루트 RectTransform(위치 갱신 대상). 비우면 자신의 RectTransform.")]
    [SerializeField] private RectTransform rect;
    [Tooltip("체력 슬라이더(MinValue=0, MaxValue=1, Interactable=off).")]
    [SerializeField] private Slider hpSlider;
    [Tooltip("스킬 게이지 루트(캐릭터만 표시, 몬스터는 숨김).")]
    [SerializeField] private GameObject gaugeRoot;
    [Tooltip("스킬 게이지 슬라이더(MinValue=0, MaxValue=1, Interactable=off).")]
    [SerializeField] private Slider gaugeSlider;

    /// <summary>위치 갱신에 사용할 RectTransform.</summary>
    public RectTransform Rect => rect != null ? rect : (RectTransform)transform;

    private void Awake() {
        if (rect == null) rect = (RectTransform)transform;
    }

    /// <summary>게이지 표시 여부를 유닛 종류에 맞게 1회 설정한다(등록 시 호출).</summary>
    public void ConfigureGauge(bool hasGauge) {
        if (gaugeRoot != null) gaugeRoot.SetActive(hasGauge);
    }

    public void ApplyHp(float ratio) {
        if (hpSlider != null) hpSlider.value = Mathf.Clamp01(ratio);
    }

    public void ApplyGauge(float ratio) {
        if (gaugeSlider != null) gaugeSlider.value = Mathf.Clamp01(ratio);
    }

    /// <summary>바 전체 표시/숨김(사망/부활 시). 불필요한 SetActive 호출은 생략한다.</summary>
    public void SetVisible(bool visible) {
        if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
    }
}
