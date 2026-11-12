using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무한 타워 전투 화면의 상단 표시. 무대(TowerDirector)를 매 프레임 읽어 그리며 아무것도 기억하지 않는다.
///
///   층            지금 오르는 층
///   보너스        이 층의 보너스(리치 텍스트, 라벨 없이 내용만)
///   전체 시간     판의 남은 시간(다 버티면 승리)
///   합류 시간     다음 소녀가 합류하기까지 남은 시간(전원이 합류했으면 판의 남은 시간)
///   순번          지금까지 몇 명이 합류했는지(예: 2 / 5)
///   합류 안내     합류한 소녀의 이름을 화면 가운데에 크게 띄웠다가 사라진다(판의 첫 소녀는 띄우지 않는다)
/// </summary>
public sealed class TowerHud : MonoBehaviour {
    [Header("Floor")]
    [SerializeField] private TMP_Text floorText;
    [Tooltip("층 보너스. 색은 리치 텍스트로 넣으므로 TMP 의 Rich Text 옵션이 켜져 있어야 한다.")]
    [SerializeField] private TMP_Text bonusText;

    [Header("Time")]
    [Tooltip("판의 남은 시간 비율(1~0).")]
    [SerializeField] private Slider timeSlider;
    [Tooltip("판의 남은 시간(초).")]
    [SerializeField] private TMP_Text timeText;
    [Tooltip("남은 시간이 이 값 아래로 내려가면 경고색으로 바뀐다(초).")]
    [SerializeField] private float timeWarnSeconds = 10f;
    [SerializeField] private Color timeWarnColor = new(0.90f, 0.20f, 0.20f);

    [Header("Relay")]
    [Tooltip("다음 소녀가 합류하기까지 남은 시간 비율(1~0).")]
    [SerializeField] private Slider relaySlider;
    [Tooltip("다음 소녀가 합류하기까지 남은 시간. 예: 7초후 합류(전원이 합류했으면 남은 시간만).")]
    [SerializeField] private TMP_Text relayTimeText;
    [Tooltip("합류한 소녀 수(예: 2 / 5). 라벨은 바깥에 따로 붙인다.")]
    [SerializeField] private TMP_Text runnerText;
    [Tooltip("선택. 전원이 합류했을 때만 켜는 오브젝트(전원 합류 안내 등).")]
    [SerializeField] private GameObject lastRunnerGroup;

    [Header("Swap Announce")]
    [Tooltip("합류 안내 묶음. 알파로 나타났다 사라지며, 평소에는 알파 0 이다. Blocks Raycasts 는 꺼 둔다.")]
    [SerializeField] private CanvasGroup swapGroup;
    [Tooltip("합류한 소녀의 이름.")]
    [SerializeField] private TMP_Text swapNameText;
    [SerializeField] private float swapFadeIn = 0.25f;
    [SerializeField] private float swapHold = 1.0f;
    [SerializeField] private float swapFadeOut = 0.4f;

    private TowerDirector _director;
    private Color _timeDefaultColor = Color.white;
    private bool _timeColorCaptured;
    private bool _timeWarned;
    private int _shownRemainSeconds = -1;
    private int _shownRelaySeconds = -1;
    private bool _shownLastRunner;
    private Sequence _swapTween;

    public void Initialize(TowerDirector director) {
        if (_director != null) _director.MemberJoined -= OnMemberJoined;
        _director = director;
        if (_director != null) _director.MemberJoined += OnMemberJoined;
        HideSwap();
        if (timeText != null && !_timeColorCaptured) {
            _timeDefaultColor = timeText.color;
            _timeColorCaptured = true;
        }
        Refresh();
    }

    /// <summary>판이 새로 시작될 때(재도전) 층/보너스 문구와 경고 상태를 다시 맞춘다.</summary>
    public void Refresh() {
        if (_director == null) return;
        HideSwap();
        if (floorText != null) floorText.text = TowerRules.FloorText(_director.Floor);
        if (bonusText != null) bonusText.text = _director.Bonus.Describe();

        _timeWarned = false;
        if (timeText != null && _timeColorCaptured) timeText.color = _timeDefaultColor;
        _shownRemainSeconds = -1;
        _shownRelaySeconds = -1;
    }

    private void OnDestroy() {
        if (_director != null) _director.MemberJoined -= OnMemberJoined;
        _swapTween?.Kill();
    }

    // 합류 안내. 합류가 연달아 일어나면(전장이 비어 바로 다음 소녀가 섬) 앞 안내를 끊고 새 이름으로 다시 띄운다.
    // 안내는 잠깐 보였다 사라지는 표시라 일시정지와 무관하게 흘러간다.
    private void OnMemberJoined(CharacterData data) {
        if (swapGroup == null) return;
        if (swapNameText != null) swapNameText.text = data != null ? data.DisplayName : "";

        _swapTween?.Kill();
        swapGroup.alpha = 0f;
        _swapTween = DOTween.Sequence()
            .Append(swapGroup.DOFade(1f, swapFadeIn))
            .AppendInterval(swapHold)
            .Append(swapGroup.DOFade(0f, swapFadeOut))
            .SetLink(gameObject);
    }

    private void HideSwap() {
        _swapTween?.Kill();
        if (swapGroup != null) swapGroup.alpha = 0f;
    }

    private void LateUpdate() {
        if (_director == null) return;

        if (timeSlider != null) timeSlider.value = _director.TimeProgress;
        if (relaySlider != null) relaySlider.value = _director.RelayProgress;
        if (runnerText != null) runnerText.text = $"{_director.RunnerNumber} / {_director.RunnerCount}";
        if (lastRunnerGroup != null && lastRunnerGroup.activeSelf != _director.IsLastRunner)
            lastRunnerGroup.SetActive(_director.IsLastRunner);

        // 숫자는 초가 바뀔 때만 다시 쓴다(매 프레임 문자열을 만들지 않는다).
        int remain = Mathf.CeilToInt(_director.RemainingTime);
        if (timeText != null && remain != _shownRemainSeconds) {
            _shownRemainSeconds = remain;
            timeText.text = remain.ToString();
        }

        // 마지막 생존자가 되면 같은 초라도 문구가 바뀌므로 그 상태도 함께 비교한다.
        int relay = Mathf.CeilToInt(_director.RelayRemaining);
        bool last = _director.IsLastRunner;
        if (relayTimeText != null && (relay != _shownRelaySeconds || last != _shownLastRunner)) {
            _shownRelaySeconds = relay;
            _shownLastRunner = last;
            relayTimeText.text = TowerRules.RelayText(relay, last);
        }

        bool warn = _director.RemainingTime <= timeWarnSeconds;
        if (warn == _timeWarned || timeText == null) return;
        _timeWarned = warn;
        timeText.color = warn ? timeWarnColor : _timeDefaultColor;
    }
}
