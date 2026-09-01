using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 의뢰 씬 상단 게이지 패널. 주 게이지(보스 체력 / 토벌 진행도 / 의뢰인 체력) + 남은 시간 게이지를 그린다.
///
/// 의뢰 종류를 전혀 알지 않는다. 디렉터가 만들어 주는 CommissionHudState 만 읽어 그리므로,
/// 새 의뢰 종류가 생겨도 이 패널은 수정할 필요가 없다(모드 쪽에서 무엇을 담을지만 정한다).
/// </summary>
public sealed class CommissionHudPanel : MonoBehaviour {
    [Header("Primary Gauge (보스 체력 / 토벌 진행도 / 의뢰인 체력)")]
    [Tooltip("주 게이지 슬라이더(MinValue=0, MaxValue=1, Interactable=off).")]
    [SerializeField] private Slider primarySlider;
    [Tooltip("주 게이지 옆 텍스트(예: 72.4% 또는 37 / 100).")]
    [SerializeField] private TMP_Text primaryText;
    [Tooltip("주 게이지 위 제목(예: 보스 이름 / 토벌 진행 / 의뢰인).")]
    [SerializeField] private TMP_Text primaryLabel;

    [Header("Time Gauge")]
    [SerializeField] private Slider timeSlider;
    [SerializeField] private TMP_Text timeText;
    [Tooltip("남은 시간이 아래 비율 이하로 떨어지면 시간 텍스트를 경고색으로 바꾼다.")]
    [SerializeField, Range(0f, 1f)] private float timeWarnRatio = 0.3f;
    [SerializeField] private Color timeWarnColor = new Color(0.95f, 0.25f, 0.25f);

    [Header("Title")]
    [Tooltip("의뢰 이름(예: 레이드 의뢰 3단계). 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text commissionNameText;

    [Header("Animation")]
    [Tooltip("주 게이지가 목표치를 따라가는 속도. 클수록 빠르게 붙는다. 0 이하면 즉시 반영(애니메이션 없음). " +
             "남은 시간 게이지는 원래 연속으로 줄어들어 보간이 필요 없다.")]
    [SerializeField] private float primarySmoothSpeed = 8f;

    private CommissionHudContext _ctx;
    private Color _timeNormalColor = Color.white;

    // 화면에 그려지는 주 게이지 값(목표치를 따라간다). 실제 값은 state.PrimaryRatio 이고 이건 표시용이다.
    private float _primaryDisplay;
    private bool _primarySettled;

    public void Initialize(CommissionHudContext ctx) {
        _ctx = ctx;
        if (timeText != null) _timeNormalColor = timeText.color;
        RefreshTitle();
    }

    /// <summary>의뢰가 바뀌었을 때(다음 단계 진행) 제목을 다시 그린다.</summary>
    public void RefreshTitle() {
        // 새 의뢰는 게이지를 처음부터 그린다. 보간을 유지하면 이전 단계의 잔량에서 기어오는 것처럼 보인다.
        _primarySettled = false;

        if (commissionNameText == null) return;
        var data = _ctx != null ? _ctx.Director.Current : null;
        commissionNameText.text = data != null ? data.DisplayName : "";
    }

    private void Update() {
        if (_ctx == null) return;

        var state = _ctx.Director.BuildHudState();

        if (primarySlider != null) primarySlider.value = SmoothPrimary(state.PrimaryRatio);
        if (primaryText != null) primaryText.text = state.PrimaryText;
        if (primaryLabel != null) primaryLabel.text = state.PrimaryLabel;

        if (timeSlider != null) timeSlider.value = state.TimeRatio;
        if (timeText != null) {
            timeText.text = state.TimeText;
            timeText.color = state.TimeRatio <= timeWarnRatio ? timeWarnColor : _timeNormalColor;
        }
    }

    // 목표치로 부드럽게 따라간다. 프레임률이 달라도 같은 속도로 붙도록 지수 보간을 쓴다.
    // 전투가 멈추면(Time.timeScale=0) 게이지도 함께 멈추도록 deltaTime 을 그대로 쓴다.
    private float SmoothPrimary(float target) {
        if (!_primarySettled || primarySmoothSpeed <= 0f) {
            _primarySettled = true;
            _primaryDisplay = target;
            return _primaryDisplay;
        }

        _primaryDisplay = Mathf.Lerp(_primaryDisplay, target, 1f - Mathf.Exp(-primarySmoothSpeed * Time.deltaTime));
        return _primaryDisplay;
    }
}
