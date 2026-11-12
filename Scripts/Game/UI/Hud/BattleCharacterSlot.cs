using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 전투 캐릭터 패널의 슬롯 1개(PLANNING 2-4). 초상화/이름/레벨/경험치와
/// 쓰러졌을 때의 반투명 오버레이 + 상태 문구를 표시한다.
///
/// 쓰러짐 표시는 컨텐츠에 따라 두 가지다. 방치 던전은 시간이 지나면 부활하므로 남은 초를 세고(PLANNING 1-12),
/// 의뢰는 부활이 없어 쓰러진 채로 남으므로 전투불능 문구를 띄운다. 어느 쪽인지는 유닛 상태로 판단하며
/// (IsReviving 이면 부활 대기, 살아 있지 않은데 부활 대기도 아니면 전투불능), 문구는 BattleSettings 가 갖는다.
///
/// 레벨/경험치는 바인드 시점(런 재구성 시) 값이고, 쓰러짐 상태와 초상(변신 반영)은 매 프레임 유닛에서 폴링한다.
/// 초상은 유닛의 PortraitSprite/PortraitFocus(외형의 단일 진실 원천)를 따르며, 스프라이트/focus 가
/// 바뀐 프레임에만 다시 바인드해 폴링 비용을 참조 비교 수준으로 유지한다.
///
/// 슬롯을 누르면 표시 중인 캐릭터 id 로 Clicked 가 발생한다(전투 카메라 포커스 요청). 실제로 누구를 비출지는
/// 무대(BattleStage)가 정하므로, 슬롯은 요청만 알리고 표시(SetFocused)는 상위가 되돌려 준 결과만 그린다.
/// </summary>
public sealed class BattleCharacterSlot : MonoBehaviour {
    [SerializeField] private GameObject content;      // 캐릭터가 없을 때 숨길 루트
    [FormerlySerializedAs("portraitView")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GradeText gradeText;
    [Tooltip("등급별로 그림/색이 바뀌는 이미지들(프레임/배경/글로우 등). 각 오브젝트에 GradeGraphicStyle 을 붙여 지정. 비우면 없음.")]
    [SerializeField] private GradeGraphicStyle[] gradeStyles;
    [SerializeField] private Slider expSlider;
    [Tooltip("경험치 진행률을 백분율로 출력. 예: 15.00%")]
    [SerializeField] private TMP_Text expText;

    [Header("Gauges (머리 위 체력/스킬 바와 같은 값, 선택)")]
    [Tooltip("체력 비율(0~1). 머리 위 체력바와 같은 값(BattleUnit.HpRatio)을 매 프레임 따라간다. 비우면 표시 안 함.")]
    [SerializeField] private Slider hpSlider;
    [Tooltip("스킬 쿨타임 진행도(0=방금 사용, 1=발동 준비). 머리 위 게이지와 같은 값(BattleUnit.GaugeRatio). 비우면 표시 안 함.")]
    [SerializeField] private Slider skillSlider;
    [Tooltip("스킬이 없는 캐릭터일 때 숨길 루트(보통 skillSlider 의 부모). 비우면 skillSlider 자체를 숨긴다.")]
    [SerializeField] private GameObject skillGroup;

    [Header("Down Overlay (부활 대기 / 전투불능 공용)")]
    [Tooltip("쓰러졌을 때 켜지는 반투명 오버레이. 부활 대기와 전투불능(의뢰) 모두 이것을 쓴다.")]
    [FormerlySerializedAs("reviveOverlay")]
    [SerializeField] private GameObject downOverlay;
    [Tooltip("쓰러짐 상태 문구. 부활 대기면 BattleSettings.ReviveTextFormat, 부활이 없으면 DeadText 가 들어간다.")]
    [FormerlySerializedAs("reviveText")]
    [SerializeField] private TMP_Text downText;

    [Header("Waiting (교대 전투 전용, 선택)")]
    [Tooltip("전장 밖에서 차례를 기다리는 동안 켜지는 묶음. 교대로 나서는 컨텐츠(무한 타워)만 켠다. 꺼진 채로 둔다.")]
    [SerializeField] private GameObject waitingGroup;

    [Header("Camera Focus")]
    [Tooltip("이 슬롯을 눌러 카메라 포커스를 요청하는 버튼. 비우면 클릭으로 포커스를 옮길 수 없다.")]
    [SerializeField] private Button focusButton;
    [Tooltip("카메라가 이 캐릭터를 비추는 동안 켜지는 표시(테두리/아이콘 등).")]
    [SerializeField] private GameObject focusIndicator;

    [Header("VFX")]
    [Tooltip("레벨업 등 슬롯 VFX 재생 기준 위치(선택). 비우면 이 슬롯 루트.")]
    [SerializeField] private RectTransform vfxAnchor;

    private CharacterUnit _unit;
    private BattleSettings _settings;
    private int _charId = -1;
    private int _level;
    // 전장 밖 소녀(유닛 없음)의 체력 비율. 유닛이 있으면 쓰지 않는다.
    private float _benchHpRatio;

    // 현재 표시 중인 초상(변신 감지용 캐시). 유닛의 PortraitSprite/Focus 와 다르면 그 프레임에 다시 바인드한다.
    private Sprite _portraitSprite;
    private Vector2 _portraitFocus;

    /// <summary>이 슬롯이 표시 중인 캐릭터 id(없으면 -1).</summary>
    public int CharId => _charId;

    /// <summary>슬롯 VFX 를 재생할 대상 RectTransform.</summary>
    public RectTransform VfxAnchor => vfxAnchor != null ? vfxAnchor : (RectTransform)transform;

    /// <summary>슬롯이 눌렸을 때 표시 중인 캐릭터 id 로 발생한다(카메라 포커스 요청).</summary>
    public event Action<int> Clicked;

    private void Awake() {
        if (focusButton != null) focusButton.onClick.AddListener(RaiseClicked);
    }

    /// <summary>쓰러짐 문구의 출처(BattleSettings)를 넣는다. 패널이 컨텍스트를 받을 때 한 번 호출한다.</summary>
    public void Initialize(BattleSettings settings) => _settings = settings;

    /// <summary>카메라 포커스 표시를 켜고 끈다. 누가 포커스인지는 상위(패널)가 무대에서 받아 알려 준다.</summary>
    public void SetFocused(bool focused) {
        if (focusIndicator != null && focusIndicator.activeSelf != focused) focusIndicator.SetActive(focused);
    }

    public void Clear() {
        _unit = null;
        _charId = -1;
        _portraitSprite = null;
        if (illustrationView != null) illustrationView.Clear();
        if (content != null) content.SetActive(false);
        if (downOverlay != null) downOverlay.SetActive(false);
        SetWaiting(false);
        if (focusButton != null) focusButton.interactable = false;
        SetFocused(false);
        ApplyGauges();
    }

    /// <summary>
    /// 전장에 서지 않은 로스터 소녀를 표시한다(교대 전투). 유닛이 없으므로 쓰러짐/대기 표시는 바인드 시점 상태로 고정되고,
    /// 상태가 바뀌면 무대가 PartyChanged 로 다시 바인드시킨다. 카메라로 비출 대상이 없어 포커스 요청도 막는다.
    /// </summary>
    public void BindBenched(CharacterAppearance appearance, CharacterProgress progress, BattleRosterState state, float hpRatio) {
        var data = appearance.Character;
        if (data == null) { Clear(); return; }

        Bind(null, data, progress);
        if (illustrationView != null) illustrationView.Bind(appearance);
        if (focusButton != null) focusButton.interactable = false;

        bool down = state == BattleRosterState.Down;
        _benchHpRatio = down ? 0f : Mathf.Clamp01(hpRatio);
        if (downOverlay != null) downOverlay.SetActive(down);
        if (down && downText != null && _settings != null) downText.text = _settings.DeadText;
        SetWaiting(state == BattleRosterState.Waiting);
        ApplyGauges();
    }

    private void SetWaiting(bool on) {
        if (waitingGroup != null && waitingGroup.activeSelf != on) waitingGroup.SetActive(on);
    }

    public void Bind(CharacterUnit unit, CharacterData data, CharacterProgress progress) {
        _unit = unit;
        _charId = data.Id;
        if (content != null) content.SetActive(true);
        if (focusButton != null) focusButton.interactable = true;

        // 초상은 유닛의 현재 외형(변신 반영)을 따른다. 유닛이 없으면 데이터 원본으로 폴백.
        _portraitSprite = null;
        if (unit != null) ApplyPortrait();
        else if (illustrationView != null) illustrationView.Bind(data);
        if (nameText != null) nameText.text = data.DisplayName;
        if (gradeText != null) gradeText.SetGrade(data.Grade, progress != null && progress.LimitBroken);
        if (gradeStyles != null) {
            foreach (var style in gradeStyles) {
                if (style != null) style.SetGrade(data.Grade);
            }
        }
        SetProgressUI(progress);
        _level = progress != null ? progress.Level : 0;   // 바인드 시점 레벨(이후 상승 감지 기준).

        if (downOverlay != null) downOverlay.SetActive(false);
        SetWaiting(false);
        _benchHpRatio = 0f;
        ApplyGauges();
    }

    /// <summary>레벨/경험치 표시를 갱신하고, 레벨이 올랐으면 true 를 반환한다(레벨업 연출 판단용).</summary>
    public bool ApplyProgress(CharacterProgress progress) {
        bool leveledUp = progress != null && progress.Level > _level;
        SetProgressUI(progress);
        if (progress != null) _level = progress.Level;
        return leveledUp;
    }

    private void SetProgressUI(CharacterProgress progress) {
        if (progress == null) return;
        if (levelText != null) levelText.text = progress.Level.ToString();
        float expRatio = progress.ExpRatio;
        if (expSlider != null) expSlider.value = expRatio;
        if (expText != null) expText.text = $"{expRatio * 100f:0.00}%";
    }

    private void Update() {
        if (_unit == null) return;

        // 부활 대기중이거나(방치 던전), 부활 없이 쓰러진 채로 남았거나(의뢰) 둘 다 "쓰러짐"으로 같은 오버레이를 쓴다.
        bool reviving = _unit.IsReviving;
        bool down = reviving || !_unit.IsAlive;
        if (downOverlay != null && downOverlay.activeSelf != down)
            downOverlay.SetActive(down);
        if (down && downText != null && _settings != null)
            downText.text = reviving
                ? string.Format(_settings.ReviveTextFormat, Mathf.CeilToInt(_unit.ReviveRemaining))
                : _settings.DeadText;

        ApplyPortrait();   // 변신 시작/종료 시 초상 교체(변화가 없으면 참조 비교만)
        ApplyGauges();
    }

    // 설정(게임 탭)에서 끈 경우 두 게이지를 숨긴다. 매 프레임 읽으므로 설정을 바꾸면 그 자리에서 반영된다.
    private static bool GaugesEnabled => UserDataManager.Data == null || UserDataManager.Data.ShowSlotGauges;

    // 머리 위 바(UnitHealthBar)와 같은 값을 그대로 그린다. 값은 유닛이 갖고 있고 여기서는 읽기만 한다.
    // 쓰러진 동안은 체력이 0, 스킬 게이지는 의미가 없으므로 0 으로 둔다(부활하면 유닛 값을 다시 따른다).
    private void ApplyGauges() {
        bool enabled = GaugesEnabled;
        bool alive = _unit != null && _unit.IsAlive;

        if (hpSlider != null) {
            SetShown(hpSlider.gameObject, enabled);
            // 유닛이 없는 슬롯(전장 밖 소녀)은 바인드 때 받은 비율을 그린다.
            if (enabled) hpSlider.value = _unit == null ? _benchHpRatio : alive ? Mathf.Clamp01(_unit.HpRatio) : 0f;
        }

        if (skillSlider == null) return;
        bool hasGauge = enabled && _unit != null && _unit.HasGauge;
        SetShown(skillGroup != null ? skillGroup : skillSlider.gameObject, hasGauge);
        if (hasGauge) skillSlider.value = alive ? Mathf.Clamp01(_unit.GaugeRatio) : 0f;
    }

    private static void SetShown(GameObject go, bool shown) {
        if (go.activeSelf != shown) go.SetActive(shown);
    }

    // 빈 슬롯(캐릭터 없음)은 포커스를 옮길 대상이 없으므로 알리지 않는다.
    private void RaiseClicked() {
        if (_charId >= 0) Clicked?.Invoke(_charId);
    }

    // 유닛의 현재 초상(PortraitSprite/Focus)과 표시 중인 초상이 다르면 다시 바인드한다.
    private void ApplyPortrait() {
        if (_unit == null || illustrationView == null) return;
        var sprite = _unit.PortraitSprite;
        var focus = _unit.PortraitFocus;
        if (sprite == _portraitSprite && focus == _portraitFocus) return;

        _portraitSprite = sprite;
        _portraitFocus = focus;
        illustrationView.Bind(sprite, focus);
    }
}
