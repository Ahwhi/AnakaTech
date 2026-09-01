using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 의뢰 씬 컴포지션 루트. 루트 스코프(GameLifetimeScope)의 자식으로 동작하며, 의뢰 전투에 필요한
/// 도메인 데이터베이스(SO)와 서비스/상태를 등록한다.
///
/// 메인 씬 스코프와 같은 것을 여럿 등록하는 이유: 씬이 바뀌면 그 씬의 스코프가 통째로 파괴되어
/// PlayerStateStore 등 씬 스코프 싱글톤이 사라진다. 그래서 의뢰 씬도 자기 몫의 상태/서비스를 갖고
/// 진입 시 서버에서 스냅샷을 다시 받는다(CommissionSceneManager).
///
/// 인스펙터 할당: 아래 SO 필드 전부 + Parent > Type = GameLifetimeScope.
/// 씬 배치: CommissionDirector, CommissionSceneManager, CommissionHudController 가 이 씬 하이어라키에 있어야
/// 자동 주입된다(하나라도 빠지면 주입이 안 돼 HUD 초기화에서 NullReference 가 난다).
/// </summary>
public sealed class CommissionLifetimeScope : LifetimeScope {
    [Header("Domain Databases (SO)")]
    [SerializeField] private BattleSettings battleSettings;
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private EngravingDatabase engravingDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [Tooltip("코스튬 데이터베이스. 코스튬 일러스트/장착 효과를 id 로 조회한다(전투/표시 공용).")]
    [SerializeField] private CostumeDatabase costumeDatabase;
    [Tooltip("시너지 데이터베이스. 편성 시너지의 조건/효과를 담는다(활성 판정과 스탯 합산 공용).")]
    [SerializeField] private SynergyDatabase synergyDatabase;
    [SerializeField] private HqNodeDatabase hqNodeDatabase;
    [Tooltip("던전 데이터베이스. 이 씬에는 던전이 없지만, 진행 중인 스테이지가 속한 장(DungeonGroupData)의 전투 BGM 을 " +
             "찾는 데 쓴다. 비우면 공용 전투 BGM 이 나온다.")]
    [SerializeField] private DungeonDatabase dungeonDatabase;
    [SerializeField] private CommissionDatabase commissionDatabase;

    [Header("Catalogs (SO)")]
    [SerializeField] private CurrencyCatalog currencyCatalog;
    [SerializeField] private ShopIconCatalog shopIconCatalog;
    [SerializeField] private GradeColorCatalog gradeColorCatalog;
    [SerializeField] private GradeShardPrefabCatalog gradeShardPrefabCatalog;
    [FormerlySerializedAs("nameplateCatalog")]
    [SerializeField] private ProfileFrameCatalog profileFrameCatalog;
    [SerializeField] private SignatureBonusCatalog signatureBonusCatalog;
    [SerializeField] private BadgeCatalog badgeCatalog;
    [SerializeField] private UIVfxCatalog uiVfxCatalog;
    [Tooltip("씬 참조 카탈로그. '나가기' 로 돌아갈 메인 씬을 여기서 얻는다.")]
    [SerializeField] private SceneCatalog sceneCatalog;

    protected override void Configure(IContainerBuilder builder) {
        // 인스펙터 할당 누락을 조용히 넘기면 주입 실패가 엉뚱한 곳(HUD 초기화)에서 NullReference 로 터진다.
        // 어느 필드가 비었는지 먼저 알려 주고, 그래도 만들 수 있는 만큼은 등록해 나머지 원인 파악을 돕는다.
        WarnIfUnassigned();

        // 정적 기본값 주입(메인 씬 스코프와 동일 규칙). 씬을 오갈 때마다 같은 카탈로그로 다시 설정된다.
        if (gradeColorCatalog != null) GradeText.SetDefaultCatalog(gradeColorCatalog);
        if (signatureBonusCatalog != null) EquipmentRules.SetSignatureBonusCatalog(signatureBonusCatalog);
        if (badgeCatalog != null) BadgeCatalog.SetDefault(badgeCatalog);

        // 1) 정적 도메인 데이터.
        builder.RegisterInstance(battleSettings);
        builder.RegisterInstance(characterDatabase);
        builder.RegisterInstance(equipmentDatabase);
        builder.RegisterInstance(engravingDatabase);
        builder.RegisterInstance(itemDatabase);
        // 미할당이어도 씬 전체가 죽지 않도록 빈 데이터베이스로 폴백한다(코스튬만 안 보인다).
        builder.RegisterInstance(costumeDatabase != null ? costumeDatabase : ScriptableObject.CreateInstance<CostumeDatabase>());
        // 미할당이어도 씬 전체가 죽지 않도록 빈 데이터베이스로 폴백한다(시너지만 안 걸린다).
        builder.RegisterInstance(synergyDatabase != null ? synergyDatabase : ScriptableObject.CreateInstance<SynergyDatabase>());
        builder.RegisterInstance(hqNodeDatabase);
        // 미할당이어도 씬은 떠야 하므로 빈 레지스트리로 대체한다(그룹을 못 찾으면 공용 전투 BGM 으로 떨어진다).
        builder.RegisterInstance(dungeonDatabase != null ? dungeonDatabase : ScriptableObject.CreateInstance<DungeonDatabase>());
        builder.RegisterInstance(commissionDatabase);
        builder.RegisterInstance(currencyCatalog);
        builder.RegisterInstance(shopIconCatalog);
        builder.RegisterInstance(gradeShardPrefabCatalog);
        builder.RegisterInstance(profileFrameCatalog);
        builder.RegisterInstance(sceneCatalog);
        // 미할당이어도 컨텍스트 생성이 실패하지 않도록 빈 카탈로그로 폴백(VFX 만 안 나옴).
        builder.RegisterInstance(uiVfxCatalog != null ? uiVfxCatalog : ScriptableObject.CreateInstance<UIVfxCatalog>());

        // 2) 플레이어 상태 + 통신 서비스(의뢰 전투에 필요한 것만).
        builder.Register<PlayerStateStore>(Lifetime.Singleton);
        // 캐릭터 외형 해석기(기본 일러스트 / 장착 코스튬). 초상을 그리는 모든 화면이 이 하나를 통과한다.
        builder.Register<CharacterAppearanceService>(Lifetime.Singleton);
        builder.Register<MainDataService>(Lifetime.Singleton).As<IMainDataService>();
        builder.Register<InventoryService>(Lifetime.Singleton).As<IInventoryService>();
        builder.Register<EngravingService>(Lifetime.Singleton).As<IEngravingService>();
        builder.Register<CostumeService>(Lifetime.Singleton).As<ICostumeService>();
        // 수련 효과도 캐릭터 최종 스탯에 직접 들어가므로 이 씬에서도 받아 와야 한다
        // (없으면 내 파티만 수련 보정이 빠진 채로 싸운다).
        builder.Register<TrainingService>(Lifetime.Singleton).As<ITrainingService>();
        builder.Register<DispatchService>(Lifetime.Singleton).As<IDispatchService>();
        builder.Register<HeadquartersService>(Lifetime.Singleton).As<IHeadquartersService>();
        // 투자는 전투 효과(선제 스킬 게이지/스쿼드 재생)를 주므로 전투 씬에서도 레벨을 받아야 한다.
        builder.Register<InvestmentService>(Lifetime.Singleton).As<IInvestmentService>();
        builder.Register<CommissionService>(Lifetime.Singleton).As<ICommissionService>();

        // 2-1) HUD 공용 의존성 묶음(메인의 MainHudContext 와 같은 역할).
        builder.Register<CommissionHudContext>(Lifetime.Singleton);

        // 3) 씬에 배치된 컴포넌트를 주입 대상으로 등록한다.
        //    여기 빠지면 [Inject] Construct 가 호출되지 않아 컨텍스트가 null 로 남는다.
        builder.RegisterComponentInHierarchy<CommissionDirector>();
        builder.RegisterComponentInHierarchy<CommissionHudController>();
        builder.RegisterComponentInHierarchy<CommissionSceneManager>();
    }

    // 미할당 SO 를 한 번에 모아 경고한다(하나씩 터지며 원인을 좁히는 시간을 줄이기 위함).
    private void WarnIfUnassigned() {
        var missing = new System.Text.StringBuilder();
        Check(missing, battleSettings, nameof(battleSettings));
        Check(missing, characterDatabase, nameof(characterDatabase));
        Check(missing, equipmentDatabase, nameof(equipmentDatabase));
        Check(missing, engravingDatabase, nameof(engravingDatabase));
        Check(missing, itemDatabase, nameof(itemDatabase));
        Check(missing, costumeDatabase, nameof(costumeDatabase));
        Check(missing, hqNodeDatabase, nameof(hqNodeDatabase));
        Check(missing, dungeonDatabase, nameof(dungeonDatabase));
        Check(missing, commissionDatabase, nameof(commissionDatabase));
        Check(missing, currencyCatalog, nameof(currencyCatalog));
        Check(missing, shopIconCatalog, nameof(shopIconCatalog));
        Check(missing, gradeShardPrefabCatalog, nameof(gradeShardPrefabCatalog));
        Check(missing, profileFrameCatalog, nameof(profileFrameCatalog));
        Check(missing, sceneCatalog, nameof(sceneCatalog));

        if (missing.Length > 0)
            Debug.LogError($"[CommissionLifetimeScope] 인스펙터 미할당: {missing}");
    }

    private static void Check(System.Text.StringBuilder sb, Object asset, string field) {
        if (asset != null) return;
        if (sb.Length > 0) sb.Append(", ");
        sb.Append(field);
    }
}
