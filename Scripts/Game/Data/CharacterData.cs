using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 캐릭터 정적 데이터(밸런스/표현). 서버와는 id 만 공유한다.
/// 레벨/경험치/별레벨/투자스탯 등 "성장치"는 서버 권위이며 여기에 담지 않는다(런타임에 합산).
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "Anaka/Data/Character")]
public sealed class CharacterData : ScriptableObject, IIllustrationSource {
    [Header("Identity")]
    [Tooltip("서버와 공유하는 고정 인덱스. 절대 재사용/변경 금지.")]
    [SerializeField] private int id;
    [SerializeField] private string displayName;
    [Tooltip("영문 표기명(로마자). 가챠 픽업 화면처럼 이름을 크게 보여주는 곳에서 보조 표기로 쓴다.")]
    [SerializeField] private string englishName;
    [Tooltip("일본어 표기명. 가챠 픽업 화면 등에서 보조 표기로 쓴다.")]
    [SerializeField] private string japaneseName;
    [Tooltip("캐릭터 이명/수식어(서브컬쳐 게임의 '칭호'). 예: '견습 기사', '흑익의 타락천사'. 이름 옆/아래에 보조 표기용.")]
    [SerializeField] private string title;
    [SerializeField] private Grade grade;
    [SerializeField] private CharacterType type;

    [Header("Story")]
    [Tooltip("캐릭터 소개 스토리(도감 상세에서 노출). 입력한 줄바꿈이 그대로 표시된다. " +
             "비우면 상세 팝업의 스토리 영역이 통째로 숨겨진다.")]
    [TextArea(4, 20)]
    [SerializeField] private string story;

    [Header("Base Stats (레벨1/별1 기준)")]
    [Tooltip("주의: Range(사거리)는 여기 값이 무시된다. 캐릭터 사거리는 무기타입 고정값(BattleSettings.RangeFor)으로 " +
             "CharacterRuntime 에서 덮어쓴다. 그 외 스탯만 여기서 설정한다.")]
    [SerializeField] private CombatStats baseStats;

    [Header("Visuals")]
    [Tooltip("원본 일러스트 스프라이트. 초상화 슬롯은 이 이미지를 마스킹해 재사용하고(CharacterIllustrationView), 덱/도감 상세에서는 부유 표시한다(PLANNING 5-7).")]
    [SerializeField] private Sprite illustration;
    [Tooltip("전투 유닛 Illust 오버라이드 스프라이트. 비우면 위의 Illustration 을 그대로 사용한다(할당 시에만 대체).")]
    [FormerlySerializedAs("portrait")]
    [SerializeField] private Sprite battleBodySprite;

    [Header("UI Illustration Focus")]
    [Tooltip("일러스트 내 관심점(보통 얼굴 중심)의 정규화 좌표. (0,0)=좌하단, (1,1)=우상단. " +
             "UI 초상 슬롯(CharacterIllustrationView)이 이 점을 슬롯 기준점에 맞춰 크롭한다. " +
             "슬롯 크기/화면 해상도와 무관하게 항상 같은 부위가 잡힌다.")]
    [SerializeField] private Vector2 uiFocus = new Vector2(0.5f, 0.75f);

    [Header("Basic Attack")]
    [Tooltip("기본공격 방식/연출 SO(근접/원거리 등). 데미지 배수/VFX/사운드/발사체를 이 SO 가 담당한다. " +
             "비우면 공격이 나가지 않는다(경고 로그).")]
    [SerializeField] private BasicAttack basicAttack;
    [Tooltip("기본공격 시 몸체 반동 연출을 켠다. 총잡이는 이 값과 관계없이 항상 반동이 있다. " +
             "기본공격 SO 는 여러 캐릭터가 같이 쓰므로 반동 여부는 캐릭터 단위로 둔다.")]
    [SerializeField] private bool attackRecoil;

    [Header("Skill")]
    [Tooltip("스킬 쿨타임이 찰 때 기본공격 대신 발동하는 고유 스킬. 시전 연출은 스킬 SO 의 Cast VFX 가 담당한다.")]
    [SerializeField] private UnitSkill skill;
    [Tooltip("한계돌파 후 위 스킬 대신 쓰는 강화 스킬(선택). 비우면 한계돌파해도 위 스킬을 그대로 쓴다.")]
    [SerializeField] private UnitSkill limitBreakSkill;

    [Header("Personal Color")]
    [Tooltip("캐릭터 퍼스널 컬러(기본 흰색). 스킬 말풍선 텍스트 색 등 개인 표현에 사용한다.")]
    [SerializeField] private Color personalColor = Color.white;

    public int Id => id;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    /// <summary>영문 표기명(로마자). 미설정이면 빈 문자열(표시측이 해당 줄을 숨긴다).</summary>
    public string EnglishName => englishName ?? "";
    /// <summary>일본어 표기명. 미설정이면 빈 문자열(표시측이 해당 줄을 숨긴다).</summary>
    public string JapaneseName => japaneseName ?? "";
    /// <summary>캐릭터 이명/수식어(칭호). 비어 있으면 빈 문자열.</summary>
    public string Title => title ?? "";
    /// <summary>캐릭터 소개 스토리. 미설정이면 빈 문자열(표시측이 스토리 영역을 숨긴다).</summary>
    public string Story => story ?? "";
    public Grade Grade => grade;
    public CharacterType Type => type;
    public WeaponType WeaponType => GameEnumRules.WeaponFor(type);
    public CombatStats BaseStats => baseStats;
    public Sprite Illustration => illustration;
    /// <summary>일러스트 내 관심점(정규화 0~1). UI 슬롯이 이 점을 기준으로 크롭한다.</summary>
    public Vector2 UiFocus => uiFocus;
    /// <summary>캐릭터 일러스트는 전신 그림에서 원하는 부위만 뽑아 쓰므로 항상 크롭 프레이밍이다.</summary>
    public IllustrationFit Fit => IllustrationFit.Crop;
    public Sprite BattleBodySprite => battleBodySprite;
    /// <summary>전투 유닛에 실제로 표시할 스프라이트. BattleBodySprite 가 있으면 그것을, 없으면 Illustration 을 사용한다.</summary>
    public Sprite BattleSprite => battleBodySprite != null ? battleBodySprite : illustration;
    public BasicAttack BasicAttack => basicAttack;
    /// <summary>기본공격 시 몸체 반동 연출을 쓰는지. 총잡이는 항상, 그 외에는 캐릭터 설정으로 켠다.</summary>
    public bool HasAttackRecoil => type == CharacterType.Gunner || attackRecoil;
    /// <summary>기본 스킬(한계돌파 전). 실제로 쓸 스킬이 필요하면 SkillFor 를 쓴다.</summary>
    public UnitSkill Skill => skill;
    /// <summary>한계돌파 스킬. 할당하지 않았으면 null.</summary>
    public UnitSkill LimitBreakSkill => limitBreakSkill;

    /// <summary>
    /// 지금 쓸 스킬. 한계돌파했고 한계돌파 스킬이 있으면 그것을, 아니면 기본 스킬을 쓴다.
    /// 전투/전투력/표시가 모두 이 한 곳을 지나므로 "어느 스킬을 쓰는가"의 규칙이 어긋나지 않는다.
    /// </summary>
    public UnitSkill SkillFor(bool limitBroken) =>
        limitBroken && limitBreakSkill != null ? limitBreakSkill : skill;

    /// <summary>이 캐릭터가 기본공격이나 지금 쓰는 스킬로 그 상태이상을 거는지.</summary>
    public bool Inflicts(StatusEffectType type, bool limitBroken) {
        if (basicAttack != null)
            foreach (var s in basicAttack.StatusEffects)
                if (s.IsValid && s.Type == type) return true;

        var current = SkillFor(limitBroken);
        if (current != null)
            foreach (var s in current.InflictedStatusEffects)
                if (s.Type == type) return true;
        return false;
    }
    /// <summary>퍼스널 컬러. 스킬 말풍선 텍스트 색 등에 사용. 미설정(알파 0)인 레거시 에셋은 흰색으로 폴백한다.</summary>
    public Color PersonalColor => personalColor.a <= 0f ? Color.white : personalColor;
}
