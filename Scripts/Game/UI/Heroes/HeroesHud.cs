using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 영웅전 전투 화면의 상단 표시. 무대(HeroesDirector)를 매 프레임 읽어 그리며 아무것도 기억하지 않는다.
///   단계        지금 도전 중인 단계(예: 3단계)
///   주 게이지   보스가 나오기 전: 처치 목표까지 남은 양(가득 찬 채로 시작해 잡을수록 줄어든다) + 잡은 수(예: 12/50)
///               보스가 나온 뒤:   보스 체력(가득 찬 채로 시작) + 체력 퍼센트(예: 37%)
///   남은 시간   제한 시간(슬라이더는 1 에서 0 으로 줄어든다)
/// 조작 중인 소녀의 체력은 하단 캐릭터 패널이 보여 준다.
/// </summary>
public sealed class HeroesHud : MonoBehaviour {
    [Header("Stage")]
    [SerializeField] private TMP_Text stageText;

    [Header("Primary Gauge (처치 수 / 보스 체력)")]
    [Tooltip("보스 전에는 처치 목표까지 남은 비율, 보스가 나오면 보스 체력 비율(1~0). MinValue=0, MaxValue=1, Interactable 끔.")]
    [FormerlySerializedAs("hpSlider")]
    [SerializeField] private Slider killSlider;
    [Tooltip("보스 전에는 잡은 수(예: 12/50), 보스가 나오면 보스 체력(예: 37%).")]
    [FormerlySerializedAs("hpText")]
    [SerializeField] private TMP_Text killText;
    [Tooltip("게이지가 목표치를 따라가는 속도. 0 이하면 즉시 반영.")]
    [FormerlySerializedAs("hpSmoothSpeed")]
    [SerializeField] private float killSmoothSpeed = 10f;

    [Header("Boss Locator")]
    [Tooltip("선택. 보스가 화면 밖에 있으면 그쪽을 가리키는 화살표.")]
    [SerializeField] private OffscreenTargetIndicator bossIndicator;

    [Header("Time")]
    [Tooltip("남은 시간 비율(1~0).")]
    [SerializeField] private Slider timeSlider;
    [Tooltip("남은 시간(초).")]
    [SerializeField] private TMP_Text timeText;
    [Tooltip("시간이 늘어날 때(보스 추가 시간) 게이지가 차오르는 속도. 줄어들 때는 그대로 따라간다.")]
    [SerializeField] private float timeGainSmoothSpeed = 6f;
    [Tooltip("남은 시간이 이 값 아래로 내려가면 경고색으로 바뀐다(초).")]
    [SerializeField] private float timeWarnSeconds = 5f;
    [SerializeField] private Color timeWarnColor = new(0.90f, 0.20f, 0.20f);

    private HeroesDirector _director;
    private Color _timeDefaultColor = Color.white;
    private bool _timeColorCaptured;
    private bool _timeWarned;
    private int _shownRemainSeconds = -1;
    private float _timeDisplay = 1f;

    // 주 게이지 표시 상태. 같은 내용이면 문자열을 다시 만들지 않는다.
    private float _gaugeDisplay = 1f;
    private bool _bossMode;
    private int _shownA = -1;   // 처치 모드: 잡은 수 / 보스 모드: 퍼센트
    private int _shownB = -1;   // 처치 모드: 목표 수 / 보스 모드: 사용 안 함

    public void Initialize(HeroesDirector director) {
        _director = director;
        if (timeText != null && !_timeColorCaptured) {
            _timeDefaultColor = timeText.color;
            _timeColorCaptured = true;
        }
        Refresh();
    }

    /// <summary>판이 새로 시작될 때(다음 단계/재도전) 단계 문구와 경고 상태, 주 게이지를 다시 맞춘다.</summary>
    public void Refresh() {
        if (_director == null) return;
        if (stageText != null) stageText.text = HeroesRules.StageText(_director.Stage);

        _timeWarned = false;
        if (timeText != null && _timeColorCaptured) timeText.color = _timeDefaultColor;
        _shownRemainSeconds = -1;
        _timeDisplay = _director.TimeProgress;   // 새 판은 가득 찬 시간에서 바로 시작한다(차오르는 연출 없이).

        // 새 판은 처치 모드, 가득 찬 게이지에서 시작한다(이전 판의 잔량에서 차오르는 것처럼 보이지 않게).
        _bossMode = false;
        ResetGauge();
    }

    private void LateUpdate() {
        if (_director == null) return;

        DrawPrimary();

        if (bossIndicator != null) {
            var boss = _director.BossUnit;
            bossIndicator.SetTarget(boss != null ? boss.transform : null);
        }

        DrawTimeGauge();

        // 숫자는 초가 바뀔 때만 다시 쓴다(매 프레임 문자열을 만들지 않는다).
        int remain = Mathf.CeilToInt(_director.RemainingTime);
        if (timeText != null && remain != _shownRemainSeconds) {
            _shownRemainSeconds = remain;
            timeText.text = remain.ToString();
        }

        bool warn = _director.RemainingTime <= timeWarnSeconds;
        if (warn == _timeWarned || timeText == null) return;
        _timeWarned = warn;
        timeText.color = warn ? timeWarnColor : _timeDefaultColor;
    }

    private void DrawPrimary() {
        // 보스가 나오는 순간 보스 체력 표시로 바꾸고 가득 찬 게이지부터 다시 그린다.
        bool boss = _director.BossAppeared;
        if (boss != _bossMode) {
            _bossMode = boss;
            ResetGauge();
        }

        if (_bossMode) {
            float ratio = _director.BossHpRatio;
            SmoothGauge(ratio);
            int percent = HeroesRules.BossHpPercent(ratio);
            if (killText != null && percent != _shownA) {
                _shownA = percent;
                killText.text = HeroesRules.BossHpText(percent);
            }
            return;
        }

        int kills = _director.Kills;
        int target = _director.KillTarget;
        // 판이 다시 시작돼 처치 수가 줄었으면 보간하지 않고 바로 맞춘다.
        if (kills < _shownA) _gaugeDisplay = _director.KillRemainProgress;
        SmoothGauge(_director.KillRemainProgress);
        if (killText != null && (kills != _shownA || target != _shownB)) {
            _shownA = kills;
            _shownB = target;
            killText.text = HeroesRules.KillCountText(kills, target);
        }
    }

    // 시간 게이지. 줄어드는 것은 그대로 따라가고, 늘어날 때(보스 추가 시간)만 부드럽게 차오른다.
    private void DrawTimeGauge() {
        if (timeSlider == null) return;
        float target = _director.TimeProgress;
        if (target <= _timeDisplay || timeGainSmoothSpeed <= 0f) _timeDisplay = target;
        else _timeDisplay = Mathf.Lerp(_timeDisplay, target, 1f - Mathf.Exp(-timeGainSmoothSpeed * Time.deltaTime));
        timeSlider.value = _timeDisplay;
    }

    private void ResetGauge() {
        _gaugeDisplay = 1f;
        _shownA = -1;
        _shownB = -1;
        if (killSlider != null) killSlider.value = 1f;
    }

    // 목표치로 부드럽게 따라간다. 프레임률이 달라도 같은 속도로 붙도록 지수 보간을 쓴다.
    private void SmoothGauge(float target) {
        if (killSmoothSpeed <= 0f) _gaugeDisplay = target;
        else _gaugeDisplay = Mathf.Lerp(_gaugeDisplay, target, 1f - Mathf.Exp(-killSmoothSpeed * Time.deltaTime));
        if (killSlider != null) killSlider.value = _gaugeDisplay;
    }
}
