using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 전선 전투 화면의 상단 표시. 무대(FrontlineDirector)와 전선 현황(FrontlineService.Info)을 읽어 그린다.
///
///   전선 이름 / 레벨     지금 치는 전선과 단계
///   토벌 진행           잡은 몬스터 / 전체 몬스터(토벌 의뢰의 진행 게이지와 같은 방식)
///   남은 시간           한 판의 제한 시간(다 되면 실패)
///   재화 패널           기력(CurrencyPanel 공용 부품)
///   사용 가능 기력       오늘 전선에 더 쓸 수 있는 기력
///   획득 보상           이 판을 깼을 때 받을 경험치(투자 반영)
/// 판의 결과는 결과 팝업(FrontlineResultPopup)이 보여 준다.
/// </summary>
public sealed class FrontlineHud : MonoBehaviour {
    [Header("Frontline")]
    [SerializeField] private TMP_Text frontlineNameText;
    [Tooltip("지금 치는 단계(예: Lv.12).")]
    [SerializeField] private TMP_Text levelText;

    [Header("Progress (토벌 진행)")]
    [Tooltip("토벌 진행 게이지(MinValue=0, MaxValue=1, Interactable=off). 가득 찬 채 시작해 몬스터를 잡을수록 줄어든다.")]
    [SerializeField] private Slider progressSlider;
    [Tooltip("토벌 진행 텍스트(예: 7 / 20).")]
    [FormerlySerializedAs("remainingMonstersText")]
    [SerializeField] private TMP_Text progressText;
    [Tooltip("게이지가 목표치를 따라가는 속도. 클수록 빠르게 붙는다. 0 이하면 즉시 반영.")]
    [SerializeField] private float progressSmoothSpeed = 8f;

    [Header("Time")]
    [Tooltip("남은 시간 비율(1~0).")]
    [SerializeField] private Slider timeSlider;
    [Tooltip("남은 시간(초, 예: 12.3).")]
    [SerializeField] private TMP_Text timeText;
    [Tooltip("남은 시간이 아래 비율 이하로 떨어지면 시간 텍스트를 경고색으로 바꾼다.")]
    [SerializeField, Range(0f, 1f)] private float timeWarnRatio = 0.3f;
    [SerializeField] private Color timeWarnColor = new Color(0.95f, 0.25f, 0.25f);

    [Header("Stamina")]
    [Tooltip("기력 표시(CurrencyView 를 자식으로 둔 공용 재화 패널).")]
    [SerializeField] private CurrencyPanel currencyPanel;
    [Tooltip("오늘 전선에 더 쓸 수 있는 기력. 숫자만 쓰므로 라벨은 따로 둔다(예: 700 / 1,000).")]
    [SerializeField] private TMP_Text dailyRemainText;

    [Header("Reward")]
    [Tooltip("이 판을 깼을 때 소녀 1명이 받는 경험치 칸(투자 전선 경험치 반영).")]
    [SerializeField] private RewardSlotView stageRewardSlot;

    private FrontlineHudContext _ctx;
    private readonly CompositeDisposable _subscriptions = new();
    private Color _timeNormalColor = Color.white;

    // 화면에 그려지는 진행 게이지 값(목표치를 따라간다). 새 판은 처음부터 그린다.
    private float _progressDisplay;
    private bool _progressSettled;
    private int _shownKilled = -1;
    private int _shownTotal = -1;

    public void Initialize(FrontlineHudContext ctx) {
        _ctx = ctx;
        if (currencyPanel != null) currencyPanel.Initialize(ctx);
        if (timeText != null) _timeNormalColor = timeText.color;

        _subscriptions.Clear();
        ctx.Frontline.Info.Subscribe(_ => RefreshDailyRemain()).AddTo(_subscriptions);
        Refresh();
    }

    /// <summary>판이 새로 시작될 때 전선 이름/단계를 다시 맞추고 게이지를 처음부터 그린다.</summary>
    public void Refresh() {
        if (_ctx == null) return;
        var director = _ctx.Director;
        if (frontlineNameText != null)
            frontlineNameText.text = _ctx.FrontlineDb != null ? _ctx.FrontlineDb.NameOf(director.FrontlineId)
                                                              : FrontlineRules.DefaultName(director.FrontlineId);
        if (levelText != null) levelText.text = FrontlineRules.StageText(director.Stage);
        if (stageRewardSlot != null) {
            int exp = FrontlineRules.ExpFor(director.Stage, FrontlineRules.InvestmentPermille(_ctx.Store));
            stageRewardSlot.Bind(RewardVisualResolver.Resolve(_ctx, RewardItem.Exp(exp)));
        }
        _progressSettled = false;
        _shownKilled = -1;
        _shownTotal = -1;
    }

    private void RefreshDailyRemain() {
        if (dailyRemainText == null) return;
        var info = _ctx.Frontline.Info.Value;
        dailyRemainText.text = info != null
            ? FrontlineRules.DailyRemainText(info.DailyStaminaRemaining, info.DailyStaminaLimit)
            : "";
    }

    private void LateUpdate() {
        if (_ctx == null) return;
        var director = _ctx.Director;

        if (progressSlider != null) progressSlider.value = SmoothProgress(director.RemainingRatio);

        // 숫자는 바뀔 때만 다시 쓴다(매 프레임 문자열을 만들지 않는다).
        int killed = director.KilledMonsters;
        int total = director.TotalMonsters;
        if (progressText != null && (killed != _shownKilled || total != _shownTotal)) {
            _shownKilled = killed;
            _shownTotal = total;
            progressText.text = $"{killed} / {total}";
        }

        float timeRatio = director.TimeProgress;
        if (timeSlider != null) timeSlider.value = timeRatio;
        if (timeText != null) {
            timeText.text = director.TimeRemaining.ToString("0.0");
            timeText.color = timeRatio <= timeWarnRatio ? timeWarnColor : _timeNormalColor;
        }
    }

    // 목표치로 부드럽게 따라간다(의뢰 게이지와 같은 지수 보간). 전투가 멈추면 게이지도 함께 멈춘다.
    private float SmoothProgress(float target) {
        if (!_progressSettled || progressSmoothSpeed <= 0f) {
            _progressSettled = true;
            _progressDisplay = target;
            return _progressDisplay;
        }
        _progressDisplay = Mathf.Lerp(_progressDisplay, target, 1f - Mathf.Exp(-progressSmoothSpeed * Time.deltaTime));
        return _progressDisplay;
    }

    private void OnDestroy() => _subscriptions.Dispose();
}
