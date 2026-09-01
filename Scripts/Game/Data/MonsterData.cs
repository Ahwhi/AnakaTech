using UnityEngine;

/// <summary>
/// 몬스터 정적 데이터(PLANNING 4). 던전의 SpawnEntry 에서 참조된다.
/// 몬스터는 성장/치명타 개념이 없으므로 CombatStats 중 6대 스탯만 의미가 있다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterData", menuName = "Anaka/Data/Monster")]
public sealed class MonsterData : ScriptableObject {
    [Header("Identity")]
    [SerializeField] private int id;
    [SerializeField] private string displayName;

    [Header("Stats (공격/방어/체력/사거리/공속/이동속도)")]
    [SerializeField] private CombatStats baseStats;

    [Header("Visuals")]
    [Tooltip("원본 일러스트 스프라이트(CharacterData 와 동일 역할). 던전 정보 등 UI 표시에 사용한다. 전투 본체는 BodyPrefab 이 담당한다.")]
    [SerializeField] private Sprite illustration;
    [Tooltip("전투에 스폰되는 몬스터 본체 프리팹(스프라이트/애니).")]
    [SerializeField] private GameObject bodyPrefab;

    [Header("Battle Illustration Focus")]
    [Tooltip("전투 마스크 일러스트의 프레이밍(관심점 + 크기 배율). 본체 프리팹은 창 규격만 갖고, " +
             "몬스터마다 다른 구도/크기는 여기서 준다. 여러 몬스터가 같은 BodyPrefab 을 공유할 수 있는 이유.")]
    [SerializeField] private IllustFocus battleFocus = IllustFocus.Default;
    [Tooltip("몸통 반경(월드 units). 이 몬스터가 전투에서 차지하는 크기로, 사거리/접근/겹침 판정이 " +
             "중심이 아니라 이 반경의 표면 기준이 된다. 덩치를 키운 몬스터에 화면상 반지름을 넣으면 " +
             "적이 중심까지 들어오지 않고 몸통 앞에서 때린다. 0 이면 BodyPrefab 에 설정된 값을 쓴다.")]
    [SerializeField] private float bodyRadius;

    [Header("Basic Attack")]
    [Tooltip("기본공격 방식/연출 SO(근접/원거리 등). 데미지 배수/VFX/사운드/발사체를 이 SO 가 담당한다. " +
             "캐릭터와 같은 SO 를 공유해도 되고, 몬스터 전용 SO 를 만들어도 된다. 비우면 공격이 나가지 않는다(경고 로그).")]
    [SerializeField] private BasicAttack basicAttack;

    [Header("Skill (선택)")]
    [Tooltip("스킬 쿨타임이 찰 때 기본공격 대신 발동하는 고유 스킬. 비우면 기본공격만 한다(게이지 바도 표시되지 않는다). " +
             "캐릭터 스킬 SO 와 동일한 자산을 그대로 쓸 수 있다(변신 스킬은 몬스터가 미지원이라 무시된다).")]
    [SerializeField] private UnitSkill skill;
    [Tooltip("스킬 말풍선 텍스트 색(캐릭터의 퍼스널 컬러와 같은 역할).")]
    [SerializeField] private Color skillBubbleColor = Color.white;

    [Header("AI")]
    [Tooltip("타게팅 방식. Nearest 는 가장 가까운 적, PriorityTarget 은 전장이 지정한 우선 대상(호위 의뢰의 의뢰인 등)을 노린다. " +
             "우선 대상이 없는 컨텐츠에서는 PriorityTarget 몬스터도 자동으로 Nearest 처럼 동작한다.")]
    [SerializeField] private MonsterTargeting targeting = MonsterTargeting.Nearest;

    public int Id => id;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public CombatStats BaseStats => baseStats;
    public Sprite Illustration => illustration;
    /// <summary>전투 마스크 일러스트의 프레이밍 값(관심점/크기 배율).</summary>
    public IllustFocus BattleFocus => battleFocus;
    /// <summary>개체별 몸통 반경(0 이면 프리팹 값 사용).</summary>
    public float BodyRadius => bodyRadius;
    public GameObject BodyPrefab => bodyPrefab;
    public BasicAttack BasicAttack => basicAttack;
    public UnitSkill Skill => skill;
    /// <summary>스킬 말풍선 텍스트 색. 미설정(알파 0)인 레거시 에셋은 흰색으로 폴백한다.</summary>
    public Color SkillBubbleColor => skillBubbleColor.a <= 0f ? Color.white : skillBubbleColor;
    /// <summary>이 몬스터의 타게팅 방식.</summary>
    public MonsterTargeting Targeting => targeting;
}

/// <summary>
/// 몬스터 타게팅 방식. 대상 "선택 규칙"만 데이터로 두고, 실제 우선 대상이 무엇인지는 전장(IBattleContext)이 정한다.
/// 덕분에 같은 몬스터 데이터가 던전에서는 평범하게, 호위 의뢰에서는 돌격병으로 동작한다.
/// </summary>
public enum MonsterTargeting {
    Nearest,          // 가장 가까운 적(기본)
    PriorityTarget    // 전장이 지정한 우선 대상(없으면 Nearest 로 폴백)
}
