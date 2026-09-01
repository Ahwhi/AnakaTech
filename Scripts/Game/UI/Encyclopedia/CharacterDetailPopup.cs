using DG.Tweening;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 캐릭터 상세 정보 팝업(읽기 전용). 도감에서 보유 캐릭터 클릭 시 열린다(PLANNING 9).
/// 초상화/일러스트(부유) + 이름/등급/레벨/전투력/무기타입/능력치 + 스킬/스토리를 표시한다.
/// 스토리는 CharacterData 가 갖는 정적 텍스트라, 비어 있는 캐릭터는 스토리 영역을 숨긴다.
/// </summary>
public sealed class CharacterDetailPopup : UIView {
    [SerializeField] private TMP_Text nameText;
    [Tooltip("캐릭터 이명/수식어(칭호). 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("등급 표시(GradeText). 색 적용 여부는 컴포넌트의 applyColor 체크로 결정.")]
    [SerializeField] private GradeText gradeText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text combatPowerText;
    [SerializeField] private TMP_Text weaponTypeText;
    [SerializeField] private Image weaponTypeImage;
    [SerializeField] private TMP_Text statsText;
    [Tooltip("일러스트 슬롯. 덱 팝업과 동일하게 CharacterIllustrationView 를 쓴다(캐릭터별 UiFocus 프레이밍 적용).")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [Tooltip("무기 타입별 아이콘 공용 테이블(SO). 비우면 이미지 숨김.")]
    [SerializeField] private WeaponIconTable weaponIconTable;

    [Header("Skill")]
    [Tooltip("스킬 이름. 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text skillNameText;
    [Tooltip("스킬 설명. 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text skillDescText;
    [Tooltip("스킬 아이콘. 비우면 표시 안 함.")]
    [SerializeField] private Image skillIcon;

    [Header("Story")]
    [Tooltip("캐릭터 스토리 본문. 비우면 표시 안 함. 긴 글은 ScrollRect 안에 두고 여기에 그 내용 텍스트를 연결한다.")]
    [SerializeField] private TMP_Text storyText;
    [Tooltip("스토리 영역 루트(제목/스크롤 등 포함). 스토리가 비어 있으면 통째로 숨긴다. 비우면 텍스트만 갱신한다.")]
    [SerializeField] private GameObject storyRoot;

    [Header("Illustration Float")]
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatDuration = 1.5f;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private Tween _floatTween;
    private Vector3 _illustrationBasePos;
    private bool _illustrationBaseCaptured;

    public void Initialize(MainHudContext ctx, CharacterData data) {
        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
        }

        var progress = ctx.Store.GetProgress(data.Id) ?? new CharacterProgress { CharDataId = data.Id };
        var runtime = new CharacterRuntime(data, progress, ctx.Settings, ctx.Store.TotalBonusFor(data.Id, ctx.EquipDb));

        if (nameText != null) nameText.text = data.DisplayName;
        if (titleText != null) titleText.text = data.Title;
        if (gradeText != null) gradeText.SetGrade(data.Grade);
        if (levelText != null) levelText.text = progress.Level.ToString();
        if (combatPowerText != null) combatPowerText.text = Mathf.RoundToInt(runtime.CombatPower).ToString("N0");
        if (weaponTypeText != null) weaponTypeText.text = GameEnumRules.WeaponDisplayName(data.WeaponType);
        if (weaponTypeImage != null) {
            var icon = weaponIconTable != null ? weaponIconTable.Get(data.WeaponType) : null;
            weaponTypeImage.sprite = icon;
            weaponTypeImage.enabled = icon != null;
        }
        if (statsText != null) statsText.text = StatFormat.FullStats(runtime.FinalStats);

        var skill = data.Skill;
        if (skillNameText != null) skillNameText.text = skill != null ? skill.SkillName : "";
        if (skillDescText != null) skillDescText.text = skill != null ? SkillTextFormat.Colorize(skill.Description) : "";
        if (skillIcon != null) {
            skillIcon.sprite = skill != null ? skill.SkillIcon : null;
            skillIcon.enabled = skill != null && skill.SkillIcon != null;
        }

        // 스토리는 아직 안 쓴 캐릭터가 있을 수 있으므로, 비어 있으면 영역을 숨겨 빈 공간이 남지 않게 한다.
        string story = data.Story;
        if (storyText != null) storyText.text = story;
        if (storyRoot != null) storyRoot.SetActive(!string.IsNullOrEmpty(story));

        ShowIllustration(data);
    }

    // 프레이밍(관심점/확대율)은 슬롯과 원본 데이터가 정하고, 여기서는 슬롯 자체를 위아래로 띄우기만 한다.
    // 부유는 슬롯의 위치만 바꾸므로 슬롯 크기 기준으로 잡힌 일러스트 프레이밍에는 영향을 주지 않는다.
    private void ShowIllustration(CharacterData data) {
        KillIllustration();
        if (illustrationView == null) return;

        illustrationView.Bind(data);

        // 기준 위치는 최초 1회만 잡는다(부유 중에 다시 잡으면 기준이 위로 밀린다).
        if (!_illustrationBaseCaptured) {
            _illustrationBasePos = illustrationView.transform.localPosition;
            _illustrationBaseCaptured = true;
        }

        var t = illustrationView.transform;
        t.localPosition = _illustrationBasePos;
        _floatTween = t.DOLocalMoveY(_illustrationBasePos.y + floatAmplitude, floatDuration)
                       .SetEase(Ease.InOutSine)
                       .SetLoops(-1, LoopType.Yoyo);
    }

    private void KillIllustration() {
        _floatTween?.Kill();
        _floatTween = null;
        if (illustrationView != null && _illustrationBaseCaptured)
            illustrationView.transform.localPosition = _illustrationBasePos;
    }

    protected override void OnClosed() => KillIllustration();
}
