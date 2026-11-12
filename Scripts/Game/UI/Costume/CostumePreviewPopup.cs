using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 코스튬 미리보기(보기 전용). 상점의 코스튬 상품에서 열어, 사기 전에 전신 일러스트와 전투 모습,
/// 장착 효과를 크게 확인하게 한다. 구매는 여기서 하지 않는다(결제 흐름은 상점 한 곳에만 둔다).
///
/// 같은 캐릭터의 기본 외형과 번갈아 볼 수 있다(비교 버튼). 효과 문구 규칙은 덱의 코스튬 화면과 같다
/// (CostumeData.EffectSummary / CostumeSlotView.NoEffectText).
/// </summary>
public sealed class CostumePreviewPopup : UIView {
    [Header("Look")]
    [Tooltip("전신 일러스트. 잘리지 않게 보려면 이 뷰의 Default Fit 을 Contain 으로 둔다.")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [Tooltip("전투에서 보이는 모습(전투용 그림, 없으면 일러스트). Preserve Aspect 를 켜 둔다. 선택.")]
    [SerializeField] private Image battleImage;

    [Header("Info")]
    [Tooltip("코스튬 이름.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("주인 캐릭터 이름. 선택.")]
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private GradeText gradeText;
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("장착 효과 요약(덱의 코스튬 화면과 같은 문구).")]
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private Color effectColor = Color.white;
    [SerializeField] private Color noEffectColor = Color.black;

    [Header("Compare")]
    [Tooltip("기본 외형 / 코스튬 외형을 번갈아 보는 버튼. 선택.")]
    [SerializeField] private Button compareButton;
    [Tooltip("비교 버튼 문구. 지금 코스튬을 보고 있으면 '기본 보기', 기본을 보고 있으면 '코스튬 보기'가 된다.")]
    [SerializeField] private TMP_Text compareLabel;
    [SerializeField] private string showDefaultLabel = "기본 보기";
    [SerializeField] private string showCostumeLabel = "코스튬 보기";

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private MainHudContext _hud;
    private CharacterData _character;
    private CostumeData _costume;
    private bool _showingDefault;

    protected override void Awake() {
        base.Awake();
        if (compareButton != null) compareButton.onClick.AddListener(() => { PlayClick(); ToggleCompare(); });
        if (closeButton != null) closeButton.onClick.AddListener(() => { PlayClick(); Dismiss(); });
    }

    /// <summary>ShowAsync 전에 부른다. 코스튬이 없는 id 면 빈 화면이 된다.</summary>
    public void Bind(MainHudContext hud, int costumeId) {
        _hud = hud;
        _costume = hud != null && hud.CostumeDb != null ? hud.CostumeDb.Get(costumeId) : null;
        _character = _costume != null && hud.CharDb != null ? hud.CharDb.Get(_costume.CharacterId) : null;
        _showingDefault = false;

        BindInfo();
        BindLook();
    }

    private void BindInfo() {
        if (nameText != null) nameText.text = _costume != null ? _costume.DisplayName : "";
        if (characterNameText != null) characterNameText.text = _character != null ? _character.DisplayName : "";

        if (gradeText != null) {
            gradeText.gameObject.SetActive(_costume != null);
            if (_costume != null) gradeText.SetGrade(_costume.Grade);
        }

        if (descriptionText != null) {
            string desc = _costume != null ? _costume.Description : "";
            descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(desc));
            descriptionText.text = desc;
        }

        if (effectText != null) {
            string summary = _costume != null ? _costume.EffectSummary() : "";
            bool hasEffect = !string.IsNullOrEmpty(summary);
            effectText.text = hasEffect ? summary : CostumeSlotView.NoEffectText;
            effectText.color = hasEffect ? effectColor : noEffectColor;
        }

        // 주인 캐릭터를 모르면 기본 외형이 없으므로 비교할 수 없다.
        if (compareButton != null) compareButton.gameObject.SetActive(_character != null && _costume != null);
    }

    // 지금 보는 쪽(코스튬/기본)의 일러스트와 전투 모습을 그린다.
    private void BindLook() {
        var appearance = new CharacterAppearance(_character, _showingDefault ? null : _costume);
        // 주인 캐릭터를 못 찾으면 코스튬 자체의 그림만이라도 보여 준다.
        Sprite illustration = _character != null ? appearance.Illustration : _costume != null ? _costume.Illustration : null;
        Sprite battle = _character != null ? appearance.BattleSprite : _costume != null ? _costume.BattleSprite : null;

        if (illustrationView != null) {
            // 미리보기는 전신을 봐야 하므로 그림의 프레이밍(Crop)이 아니라 뷰의 기본 프레이밍을 쓴다.
            if (illustration != null) illustrationView.Bind(illustration);
            else illustrationView.Clear();
        }
        if (battleImage != null) {
            battleImage.sprite = battle;
            battleImage.enabled = battle != null;
        }
        if (compareLabel != null) compareLabel.text = _showingDefault ? showCostumeLabel : showDefaultLabel;
    }

    private void ToggleCompare() {
        if (_character == null || _costume == null) return;
        _showingDefault = !_showingDefault;
        BindLook();
    }

    private void PlayClick() {
        if (_hud != null && _hud.Sound != null) _hud.Sound.PlaySfx(SfxKey.ButtonClick);
    }
}
