using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 공유 체력 게이지 전투 씬의 HUD(길드 레이드, 원정대 공용). 의뢰 씬 HUD 와 같은 구성(상단 체력 게이지 + 남은 시간)이지만
/// 하단 캐릭터 패널은 두지 않는다(짧은 도전이라 줌과 피해 표시만 보면 되고 조작할 것이 없다).
///
/// 값은 IBossGaugeSource 계약으로만 읽으므로 어느 디렉터가 오든 같은 프리팹을 그대로 쓴다.
/// 게이지는 매 프레임 폴링해 그린다(전투 값이 프레임 단위로 움직이므로 이벤트보다 폴링이 단순하다).
/// 체력 감소는 부드럽게 따라가도록 보간한다(CommissionHudPanel 과 같은 규칙).
/// </summary>
public sealed class GuildRaidHudController : MonoBehaviour {
    /// <summary>체력바가 목표 비율을 따라가는 속도(초당). 값이 클수록 즉각적이다.</summary>
    private const float HpLerpSpeed = 6f;

    [Header("Boss")]
    [SerializeField] private Slider bossHpSlider;
    [SerializeField] private TMP_Text bossNameText;
    [Tooltip("보스 체력 비율(백분율).")]
    [SerializeField] private TMP_Text bossHpPercentText;
    [SerializeField] private TMP_Text bossLevelText;

    [Header("Time")]
    [SerializeField] private Slider timeSlider;
    [SerializeField] private TMP_Text timeText;

    [Header("Damage")]
    [Tooltip("이번 도전에서 지금까지 깎은 피해량.")]
    [SerializeField] private TMP_Text dealtDamageText;
    [Tooltip("숫자가 목표값을 따라가는 속도(초당 비율). 클수록 빨리 따라잡는다. 굴러가는 동안이 곧 타격의 여운이다.")]
    [SerializeField, Min(1f)] private float damageCountSpeed = 9f;
    [Tooltip("피해가 들어올 때 숫자가 튀는 크기. 0 이면 튀지 않는다.")]
    [SerializeField, Min(0f)] private float damagePunchScale = 0.3f;
    [Tooltip("튀는 연출 길이(초). 이 간격보다 자주 맞아도 연출은 한 번만 재생한다.")]
    [SerializeField, Min(0.02f)] private float damagePunchDuration = 0.18f;
    [Tooltip("맞은 순간의 숫자 색. 곧 원래 색으로 돌아온다.")]
    [SerializeField] private Color damageHitColor = new Color(1f, 0.86f, 0.28f, 1f);

    [Header("Panels (배치한 것만 연결, 비워도 됨)")]
    [Tooltip("하단 캐릭터 슬롯. 메인/의뢰 씬과 같은 컴포넌트를 그대로 쓴다.")]
    [SerializeField] private BattleCharacterPanel battleCharacterPanel;
    [Tooltip("딜량 미터. 메인/의뢰 씬과 같은 컴포넌트를 그대로 쓴다.")]
    [SerializeField] private DamageMeterPanel damageMeterPanel;
    [Tooltip("두 손가락 핀치 줌. 줌 버튼 대신 화면을 직접 벌려 확대/축소한다.")]
    [SerializeField] private BattlePinchZoom pinchZoom;

    [Header("Exit")]
    [Tooltip("나가기 버튼. 그때까지 깎은 피해는 서버에 반영된다.")]
    [SerializeField] private Button exitButton;

    [Header("Swap Announce")]
    [Tooltip("다음 레벨 보스 등장 안내 묶음(무한 타워 합류 안내와 같은 방식). 알파로 나타났다 사라지며, 평소에는 알파 0 이다. " +
             "Blocks Raycasts 는 꺼 둔다.")]
    [SerializeField] private CanvasGroup swapGroup;
    [Tooltip("새로 나온 보스 레벨(예: Lv.3).")]
    [SerializeField] private TMP_Text swapLevelText;
    [SerializeField] private float swapFadeIn = 0.25f;
    [SerializeField] private float swapHold = 1.0f;
    [SerializeField] private float swapFadeOut = 0.4f;

    private IBossGaugeSource _director;
    private BattleHudContext _ctx;
    private float _displayedHpRatio = 1f;
    // 지금 화면에 반영된 보스 레벨 표기. 무대 값과 달라지는 순간이 "다음 레벨 보스 등장"이다.
    private string _shownLevelText;
    private Sequence _swapTween;

    // 표시 중인 피해량(실제 값을 굴러서 따라간다)과 직전 실제 값(증가 감지용).
    private long _shownDamage;
    private long _lastDamage;
    private float _punchCooldown;
    private Color _damageBaseColor;
    private bool _damageColorCaptured;
    private Tween _punchTween;
    private Tween _colorTween;

    /// <summary>나가기 요청. 씬 매니저가 피해 보고 후 메인으로 되돌린다.</summary>
    public event Action ExitRequested;

    [VContainer.Inject]
    public void Construct(IBossGaugeSource director, BattleHudContext ctx) {
        _director = director;
        _ctx = ctx;
    }

    private void Awake() {
        if (exitButton != null) exitButton.onClick.AddListener(() => ExitRequested?.Invoke());
    }

    private void Start() {
        // 전장 공용 패널들은 메인/의뢰 씬과 같은 컴포넌트라 컨텍스트만 넘기면 그대로 동작한다.
        if (_ctx == null) {
            Debug.LogError("[GuildRaidHudController] 의존성 주입이 되지 않았습니다. " +
                           "씬 LifetimeScope 의 Configure 에 BattleHudContext / IBossGaugeSource 등록과 " +
                           "RegisterComponentInHierarchy<GuildRaidHudController>() 가 있는지 확인하세요.");
            return;
        }

        if (battleCharacterPanel != null) battleCharacterPanel.Initialize(_ctx);
        if (damageMeterPanel != null) damageMeterPanel.Initialize(_ctx);
        if (pinchZoom != null) pinchZoom.Initialize(_ctx);
    }

    /// <summary>새 도전이 시작될 때 표시를 초기화한다(보간이 이전 판 값에서 이어지지 않도록).</summary>
    public void RefreshForNewRaid() {
        if (_director == null) return;

        _displayedHpRatio = _director.BossHpRatio;
        _shownLevelText = _director.BossLevelText;
        HideSwap();
        if (bossNameText != null) bossNameText.text = _director.BossName;
        if (bossLevelText != null) bossLevelText.text = _shownLevelText;

        _shownDamage = 0;
        _lastDamage = 0;
        _punchCooldown = 0f;
        if (dealtDamageText != null) dealtDamageText.text = "0";
        Apply(_displayedHpRatio);
    }

    private void Update() {
        if (_director == null) return;

        // 보스를 잡아 다음 레벨 보스가 나왔으면(V1.13) 레벨 표기를 바꾸고 게이지를 새 보스 체력에 바로 맞춘다
        // (보간하면 0 에서 가득 찬 쪽으로 차오르는 모습이 회복처럼 보인다).
        // 시작 전(RefreshForNewRaid 이전)에는 비교 기준이 없으므로 안내하지 않는다.
        string levelText = _director.BossLevelText;
        if (_shownLevelText != null && !string.Equals(_shownLevelText, levelText)) {
            _shownLevelText = levelText;
            if (bossLevelText != null) bossLevelText.text = levelText;
            _displayedHpRatio = _director.BossHpRatio;
            PlaySwap(levelText);
        }

        // 체력은 목표 비율로 부드럽게 따라간다(한 방에 크게 깎여도 게이지가 순간이동하지 않게).
        _displayedHpRatio = Mathf.Lerp(_displayedHpRatio, _director.BossHpRatio, HpLerpSpeed * Time.deltaTime);
        Apply(_displayedHpRatio);

        if (timeSlider != null) timeSlider.SetValueWithoutNotify(_director.TimeRatio);
        if (timeText != null) timeText.text = _director.TimeText;
        UpdateDealtDamage(Time.deltaTime);
    }

    /// <summary>
    /// 누적 피해량 표시. 숫자를 바로 갈아 끼우면 얼마나 아팠는지가 남지 않으므로,
    /// 목표값까지 굴러가게 하고 맞은 순간에 한 번 튀겨 준다.
    /// 타격은 초당 여러 번 들어오므로 연출은 재생 시간만큼 간격을 두고 한 번만 낸다.
    /// </summary>
    private void UpdateDealtDamage(float deltaTime) {
        if (dealtDamageText == null) return;

        if (!_damageColorCaptured) {
            _damageBaseColor = dealtDamageText.color;
            _damageColorCaptured = true;
        }

        long actual = _director.DealtDamage;
        _punchCooldown -= deltaTime;

        if (actual > _lastDamage && _punchCooldown <= 0f) {
            PlayHitFeedback();
            _punchCooldown = damagePunchDuration;
        }
        _lastDamage = actual;

        if (_shownDamage < actual) {
            // 남은 차이에 비례해 좁힌다(큰 한 방일수록 오래 굴러간다). 최소 1 씩은 움직여 멈춘 것처럼 보이지 않게 한다.
            long step = (long)Mathf.Max(1f, (actual - _shownDamage) * damageCountSpeed * deltaTime);
            _shownDamage = Math.Min(actual, _shownDamage + step);
        } else if (_shownDamage > actual) {
            _shownDamage = actual;   // 새 도전 등으로 값이 줄면 즉시 맞춘다
        }

        dealtDamageText.text = _shownDamage.ToString("N0");
    }

    private void PlayHitFeedback() {
        var rt = dealtDamageText.rectTransform;

        if (damagePunchScale > 0f) {
            _punchTween?.Kill();
            rt.localScale = Vector3.one;   // 연출이 겹쳐 크기가 누적되지 않도록 되돌리고 시작한다
            _punchTween = rt.DOPunchScale(Vector3.one * damagePunchScale, damagePunchDuration, 1, 0.6f)
                            .SetLink(gameObject);
        }

        _colorTween?.Kill();
        dealtDamageText.color = damageHitColor;
        _colorTween = dealtDamageText.DOColor(_damageBaseColor, damagePunchDuration * 1.6f)
                                     .SetLink(gameObject);
    }

    // 다음 레벨 보스 등장 안내. 연달아 잡으면 앞 안내를 끊고 새 레벨로 다시 띄운다(무한 타워 합류 안내와 같은 규칙).
    // 잠깐 보였다 사라지는 표시라 일시정지와 무관하게 흘러간다.
    private void PlaySwap(string levelText) {
        if (swapGroup == null) return;
        if (swapLevelText != null) swapLevelText.text = levelText;

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

    private void OnDestroy() => _swapTween?.Kill();

    private void Apply(float ratio) {
        if (bossHpSlider != null) {
            bossHpSlider.minValue = 0f;
            bossHpSlider.maxValue = 1f;
            bossHpSlider.SetValueWithoutNotify(ratio);
        }
        // 숫자는 보간값이 아니라 실제 비율로 보여 준다(표시와 실제가 어긋나면 안 되는 값).
        if (bossHpPercentText != null && _director != null)
            bossHpPercentText.text = $"{_director.BossHpRatio * 100f:0.0}%";
    }
}
