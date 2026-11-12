using DG.Tweening;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 멸망전 전투 화면의 상단 표시. 데미지 미터는 두지 않는다(소녀가 보유 전원이라 목록이 화면을 덮는다).
///
/// 보여 주는 것은 여섯이다.
///   처치 게이지  이 페이즈에 남은 몬스터(가득 찬 상태에서 줄어든다)
///   시간 게이지  이 페이즈의 남은 시간
///   페이즈       지금 몇 페이즈인지
///   점수         그 판에서 지금까지 처치한 누적 수 + 역대 최고 점수
///   생존         쓰러지지 않은 소녀 수(멸망전은 부활이 없어 줄기만 한다)
///   증강 목록    지금까지 고른 효과를 한 줄씩, 종류별 색으로
///
/// 무대를 매 프레임 읽어 그린다. 무대가 값의 원천이므로 HUD 는 아무것도 기억하지 않는다.
/// 예외가 역대 최고 점수 하나인데, 그것만은 계정 정보라 전투가 아니라 씬 매니저가 넣어 준다.
/// </summary>
public sealed class DoomHud : MonoBehaviour {
    [Header("Progress")]
    [Tooltip("이 페이즈에 남은 몬스터 비율(1~0). 토벌 의뢰처럼 가득 찬 상태에서 줄어든다.")]
    [SerializeField] private Slider progressSlider;
    [Tooltip("진행 문구(예: 12 / 40).")]
    [SerializeField] private TMP_Text progressText;

    [Header("Time")]
    [Tooltip("이 페이즈의 남은 시간 비율(0~1). 시간이 흐를수록 줄어든다.")]
    [SerializeField] private Slider timeSlider;
    [Tooltip("남은 시간(예: 0:42).")]
    [SerializeField] private TMP_Text timeText;
    [Tooltip("남은 시간이 이 값 아래로 내려가면 경고색으로 바뀐다(초).")]
    [SerializeField] private float timeWarnSeconds = 10f;
    [Tooltip("경고 상태의 시간 글자 색. 시간 게이지의 Fill 에도 같이 적용된다.")]
    [SerializeField] private Color timeWarnColor = new(0.90f, 0.20f, 0.20f);

    [Header("Phase / Score")]
    [SerializeField] private TMP_Text phaseText;
    [Tooltip("그 판에서 지금까지 처치한 누적 수(= 점수).")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("역대 최고 점수. 이번 판이 기록을 넘어서면 그때부터는 현재 점수를 그대로 따라간다.")]
    [SerializeField] private TMP_Text bestScoreText;
    [Tooltip("이번 판이 최고 기록을 넘어선 동안 켜 두는 오브젝트(NEW RECORD 등). 선택.")]
    [SerializeField] private GameObject newRecordGroup;

    [Header("Score Feedback")]
    [Tooltip("숫자가 목표값을 따라가는 속도(초당 비율). 클수록 빨리 따라잡는다. 굴러가는 동안이 곧 처치의 여운이다.")]
    [SerializeField, Min(1f)] private float scoreCountSpeed = 9f;
    [Tooltip("점수가 오를 때 숫자가 튀는 크기. 0 이면 튀지 않는다.")]
    [SerializeField, Min(0f)] private float scorePunchScale = 0.3f;
    [Tooltip("튀는 연출 길이(초). 이 간격보다 자주 잡아도 연출은 한 번만 재생한다.")]
    [SerializeField, Min(0.02f)] private float scorePunchDuration = 0.18f;
    [Tooltip("점수가 오른 순간의 숫자 색. 곧 원래 색으로 돌아온다.")]
    [SerializeField] private Color scoreHitColor = new(1f, 0.86f, 0.28f, 1f);

    [Header("Survivors")]
    [Tooltip("쓰러지지 않은 소녀 수(예: 27 / 40). 라벨은 바깥에 따로 붙인다.")]
    [SerializeField] private TMP_Text aliveText;

    [Header("Augments")]
    [Tooltip("고른 증강을 한 줄씩(개행) 모아 쓰는 텍스트 하나. 종류별 색은 리치 텍스트로 넣으므로 " +
             "TMP 의 Rich Text 옵션이 켜져 있어야 한다.")]
    [SerializeField] private TMP_Text augmentText;

    private DoomDirector _director;

    // 역대 최고 점수(계정 정보). 씬 매니저가 서버에서 받아 넣어 준다.
    private int _bestScore;

    private Color _timeDefaultColor = Color.white;
    private Color _timeFillDefaultColor = Color.white;
    private Graphic _timeFill;
    private bool _capturedTimeColors;
    private bool _timeWarned;

    // 굴러가는 점수 표시. _shownScore 가 화면 값이고 무대의 Score 가 목표값이다.
    private int _shownScore;
    private int _lastScore;
    private float _punchCooldown;
    private Color _scoreBaseColor = Color.white;
    private bool _scoreColorCaptured;
    private Tween _punchTween;
    private Tween _colorTween;

    // 마지막으로 그린 증강 상태의 번호. 바뀐 때만 문자열을 다시 만든다.
    private int _shownAugmentVersion = -1;

    public void Initialize(DoomDirector director) {
        _director = director;
        CaptureTimeColors();

        _shownAugmentVersion = -1;   // 다음 갱신에서 반드시 다시 그리게 한다.
        _timeWarned = false;

        // 판을 새로 시작하면 굴러가던 숫자도 0 에서 다시 시작해야 한다(앞 판 점수에서 내려오면 안 된다).
        _punchTween?.Kill();
        _colorTween?.Kill();
        if (scoreText != null && _scoreColorCaptured) {
            scoreText.color = _scoreBaseColor;
            scoreText.rectTransform.localScale = Vector3.one;
        }
        _shownScore = 0;
        _lastScore = 0;
        _punchCooldown = 0f;

        if (newRecordGroup != null) newRecordGroup.SetActive(false);
    }

    /// <summary>
    /// 역대 최고 점수를 넣는다. 서버에서 받아 오는 값이라 전투가 알 수 없고, 씬 매니저가 넘겨 준다.
    /// 판을 다시 시작할 때도 갱신된 기록으로 다시 불러 주면 된다.
    /// </summary>
    public void SetBestScore(int bestScore) => _bestScore = bestScore;

    private void CaptureTimeColors() {
        if (_capturedTimeColors) return;
        _capturedTimeColors = true;

        if (timeText != null) _timeDefaultColor = timeText.color;
        if (timeSlider != null && timeSlider.fillRect != null) {
            _timeFill = timeSlider.fillRect.GetComponent<Graphic>();
            if (_timeFill != null) _timeFillDefaultColor = _timeFill.color;
        }
    }

    private void LateUpdate() {
        if (_director == null) return;

        if (progressSlider != null) progressSlider.value = _director.PhaseRemainingRatio;
        if (progressText != null) progressText.text = _director.PhaseProgressText;
        if (phaseText != null) phaseText.text = $"{_director.PhaseNumber} 페이즈";
        if (aliveText != null) aliveText.text = $"{_director.AliveCount} / {_director.PartyCount}";

        RefreshScore(Time.deltaTime);
        RefreshTime();
        RefreshAugments();
    }

    /// <summary>
    /// 점수 표시. 숫자를 바로 갈아 끼우면 방금 몇 마리를 잡았는지가 남지 않으므로,
    /// 목표값까지 굴러가게 하고 오른 순간에 한 번 튀겨 준다(길드 레이드의 누적 피해량과 같은 방식).
    /// 후반 페이즈에서는 초당 여러 마리가 죽으므로 연출은 재생 시간만큼 간격을 두고 한 번만 낸다.
    ///
    /// 최고 점수 칸은 굴러가는 값을 따라간다. 실제 점수를 쓰면 기록을 넘어선 순간 최고 점수만 먼저 튀어
    /// 현재 점수보다 커 보인다.
    /// </summary>
    private void RefreshScore(float deltaTime) {
        int score = _director.Score;
        bool isNewRecord = score > _bestScore;

        if (newRecordGroup != null && newRecordGroup.activeSelf != isNewRecord)
            newRecordGroup.SetActive(isNewRecord);

        if (scoreText == null) {
            _shownScore = score;
            _lastScore = score;
        } else {
            if (!_scoreColorCaptured) {
                _scoreBaseColor = scoreText.color;
                _scoreColorCaptured = true;
            }

            _punchCooldown -= deltaTime;
            if (score > _lastScore && _punchCooldown <= 0f) {
                PlayScoreFeedback();
                _punchCooldown = scorePunchDuration;
            }
            _lastScore = score;

            if (_shownScore < score) {
                // 남은 차이에 비례해 좁힌다(한 번에 많이 잡을수록 오래 굴러간다).
                // 최소 1 씩은 움직여 멈춘 것처럼 보이지 않게 한다.
                int step = Mathf.Max(1, Mathf.RoundToInt((score - _shownScore) * scoreCountSpeed * deltaTime));
                _shownScore = Mathf.Min(score, _shownScore + step);
            } else if (_shownScore > score) {
                _shownScore = score;   // 재도전 등으로 값이 줄면 즉시 맞춘다
            }

            scoreText.text = _shownScore.ToString("N0");
        }

        if (bestScoreText != null)
            bestScoreText.text = (isNewRecord ? Mathf.Max(_shownScore, _bestScore) : _bestScore).ToString("N0");
    }

    private void PlayScoreFeedback() {
        var rt = scoreText.rectTransform;

        if (scorePunchScale > 0f) {
            _punchTween?.Kill();
            rt.localScale = Vector3.one;   // 연출이 겹쳐 크기가 누적되지 않도록 되돌리고 시작한다
            _punchTween = rt.DOPunchScale(Vector3.one * scorePunchScale, scorePunchDuration, 1, 0.6f)
                            .SetLink(gameObject);
        }

        _colorTween?.Kill();
        scoreText.color = scoreHitColor;
        _colorTween = scoreText.DOColor(_scoreBaseColor, scorePunchDuration * 1.6f)
                               .SetLink(gameObject);
    }

    private void RefreshTime() {
        float remain = _director.RemainingTime;

        if (timeSlider != null) timeSlider.value = _director.TimeProgress;
        if (timeText != null)
            timeText.text = $"{Mathf.FloorToInt(remain / 60f)}:{Mathf.FloorToInt(remain % 60f):00}";

        // 색은 경고 상태가 바뀔 때만 건드린다(매 프레임 대입하면 머티리얼이 계속 더러워진다).
        bool warn = remain <= timeWarnSeconds;
        if (warn == _timeWarned) return;
        _timeWarned = warn;

        if (timeText != null) timeText.color = warn ? timeWarnColor : _timeDefaultColor;
        if (_timeFill != null) _timeFill.color = warn ? timeWarnColor : _timeFillDefaultColor;
    }

    /// <summary>
    /// 증강 목록. 텍스트 하나에 개행으로 쌓고 줄마다 리치 텍스트로 색을 넣는다.
    /// 같은 종류를 또 먹으면 줄이 늘지 않고 그 줄의 누적치만 올라간다.
    ///
    /// 상태 번호가 바뀐 때만 다시 만든다. 매 프레임 문자열을 조립하면 40유닛 전투에서 쓰레기가 쌓인다.
    /// </summary>
    private void RefreshAugments() {
        if (augmentText == null) return;

        var augments = _director.Augments;
        if (augments.Version == _shownAugmentVersion) return;
        _shownAugmentVersion = augments.Version;

        var order = augments.Order;
        if (order.Count == 0) { augmentText.text = ""; return; }

        var sb = new StringBuilder(order.Count * 32);
        for (int i = 0; i < order.Count; i++) {
            var kind = order[i];
            if (i > 0) sb.Append('\n');
            sb.Append("<color=#")
              .Append(ColorUtility.ToHtmlStringRGB(DoomAugmentRules.Get(kind).Color))
              .Append('>')
              .Append(DoomAugmentRules.DescribeTotal(kind, augments.TotalOf(kind)))
              .Append("</color>");
        }
        augmentText.text = sb.ToString();
    }
}
