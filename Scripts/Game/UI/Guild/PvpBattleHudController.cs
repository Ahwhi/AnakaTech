using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유저 대 유저 전투 씬의 HUD. 양측 대표 초상과 파티 체력 총합을 좌우로 보여 주고 남은 시간을 센다.
/// 하단 캐릭터 슬롯/딜량 미터/줌은 메인ㆍ의뢰 씬과 같은 공용 패널을 그대로 붙인다.
///
/// 값은 매 프레임 디렉터에서 폴링하며, 체력은 부드럽게 따라가도록 보간한다(다른 전투 HUD 와 같은 규칙).
/// </summary>
public sealed class PvpBattleHudController : MonoBehaviour {
    private const float HpLerpSpeed = 6f;

    [Header("My Team")]
    [SerializeField] private Slider myHpSlider;
    [Tooltip("내 파티 남은 체력 비율(백분율).")]
    [SerializeField] private TMP_Text myHpPercentText;
    [SerializeField] private TMP_Text myNameText;
    [Tooltip("내 대표 캐릭터 초상. 관심점 프레이밍이 적용되도록 raw Image 가 아니라 이 컴포넌트를 쓴다.")]
    [SerializeField] private CharacterIllustrationView myPortraitView;
    [Tooltip("선택. 내 프로필 테두리.")]
    [SerializeField] private ProfileFrameView myFrameView;

    [Header("Opponent")]
    [SerializeField] private Slider enemyHpSlider;
    [Tooltip("상대 파티 남은 체력 비율(백분율).")]
    [SerializeField] private TMP_Text enemyHpPercentText;
    [SerializeField] private TMP_Text opponentNameText;
    [Tooltip("상대 대표 캐릭터 초상.")]
    [SerializeField] private CharacterIllustrationView opponentPortraitView;
    [Tooltip("선택. 상대 프로필 테두리.")]
    [SerializeField] private ProfileFrameView opponentFrameView;

    [Header("Time")]
    [SerializeField] private Slider timeSlider;
    [SerializeField] private TMP_Text timeText;

    [Header("Panels (배치한 것만 연결, 비워도 됨)")]
    [Tooltip("하단 캐릭터 슬롯. 메인/의뢰 씬과 같은 컴포넌트를 그대로 쓴다.")]
    [SerializeField] private BattleCharacterPanel battleCharacterPanel;
    [Tooltip("딜량 미터. 메인/의뢰 씬과 같은 컴포넌트를 그대로 쓴다.")]
    [SerializeField] private DamageMeterPanel damageMeterPanel;
    [Tooltip("두 손가락 핀치 줌. 줌 버튼 대신 화면을 직접 벌려 확대/축소한다.")]
    [SerializeField] private BattlePinchZoom pinchZoom;

    private PvpBattleDirector _director;
    private BattleHudContext _ctx;
    private float _myDisplayed = 1f;
    private float _enemyDisplayed = 1f;

    [VContainer.Inject]
    public void Construct(PvpBattleDirector director, BattleHudContext ctx) {
        _director = director;
        _ctx = ctx;
    }

    private void Start() {
        // 전장 공용 패널들은 메인/의뢰 씬과 같은 컴포넌트라 컨텍스트만 넘기면 그대로 동작한다.
        if (_ctx == null) {
            Debug.LogError("[PvpBattleHudController] 의존성 주입이 되지 않았습니다. " +
                           "PvpBattleLifetimeScope 의 Configure 에 BattleHudContext 등록과 " +
                           "RegisterComponentInHierarchy<PvpBattleHudController>() 가 있는지 확인하세요.");
            return;
        }

        if (battleCharacterPanel != null) battleCharacterPanel.Initialize(_ctx);
        if (damageMeterPanel != null) damageMeterPanel.Initialize(_ctx);
        if (pinchZoom != null) pinchZoom.Initialize(_ctx);
    }

    /// <summary>새 전투가 시작될 때 표시를 초기화한다(보간이 이전 판 값에서 이어지지 않도록).</summary>
    public void RefreshForNewBattle() {
        if (_director == null) return;

        _myDisplayed = _director.MyHpRatio;
        _enemyDisplayed = _director.EnemyHpRatio;

        var store = _ctx != null ? _ctx.Store : null;
        if (myNameText != null && store != null) myNameText.text = store.Nickname.Value;
        if (opponentNameText != null) opponentNameText.text = _director.OpponentName;

        // 초상은 한 판 동안 바뀌지 않으므로 시작할 때 한 번만 붙인다.
        BindMyPortrait(myPortraitView, store != null ? store.Deck.RepresentativeCharId : 0);
        BindPortrait(opponentPortraitView, _director.OpponentRepresentativeCharId,
                     _director.OpponentRepresentativeCostumeId);
        // 테두리는 한 판 동안 바뀌지 않는 고정값이라 구독형(BindReactive)이 아니라 1회 표시로 붙인다.
        if (_ctx != null) {
            if (myFrameView != null && store != null)
                myFrameView.Bind(_ctx.ProfileFrames, store.EquippedProfileFrameId.Value);
            if (opponentFrameView != null)
                opponentFrameView.Bind(_ctx.ProfileFrames, _director.OpponentProfileFrameId);
        }

        Apply();
    }

    // 내 대표 캐릭터 초상(내가 장착한 코스튬 반영).
    private void BindMyPortrait(CharacterIllustrationView view, int charDataId) {
        if (view == null || _ctx == null || _ctx.Appearance == null) { view?.Clear(); return; }
        BindAppearance(view, _ctx.Appearance.Mine(charDataId));
    }

    // 상대 대표 캐릭터 초상(서버가 준 코스튬 id 반영). 상대가 코스튬을 끼고 있으면 그 일러스트로 보인다.
    private void BindPortrait(CharacterIllustrationView view, int charDataId, int costumeId) {
        if (view == null || _ctx == null || _ctx.Appearance == null) { view?.Clear(); return; }
        BindAppearance(view, _ctx.Appearance.Other(charDataId, costumeId));
    }

    // 외형을 그대로 넘겨야 관심점/프레이밍 방식이 함께 적용된다(코스튬은 자기 관심점을 갖는다).
    private static void BindAppearance(CharacterIllustrationView view, CharacterAppearance appearance) {
        if (appearance.Character != null) view.Bind(appearance);
        else view.Clear();
    }

    private void Update() {
        if (_director == null) return;

        _myDisplayed = Mathf.Lerp(_myDisplayed, _director.MyHpRatio, HpLerpSpeed * Time.deltaTime);
        _enemyDisplayed = Mathf.Lerp(_enemyDisplayed, _director.EnemyHpRatio, HpLerpSpeed * Time.deltaTime);
        Apply();

        if (timeSlider != null) timeSlider.SetValueWithoutNotify(_director.TimeRatio);
        if (timeText != null) timeText.text = _director.TimeText;
    }

    private void Apply() {
        SetRatio(myHpSlider, _myDisplayed);
        SetRatio(enemyHpSlider, _enemyDisplayed);

        // 숫자는 보간값이 아니라 실제 비율로 보여 준다(표시와 실제가 어긋나면 안 되는 값).
        if (myHpPercentText != null) myHpPercentText.text = Percent(_director.MyHpRatio);
        if (enemyHpPercentText != null) enemyHpPercentText.text = Percent(_director.EnemyHpRatio);
    }

    private static string Percent(float ratio) => $"{Mathf.Clamp01(ratio) * 100f:0.0}%";

    private static void SetRatio(Slider slider, float ratio) {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(ratio);
    }
}
