using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 전선 씬 컴포지션 루트(TowerLifetimeScope 와 같은 구조/이유, V1.12).
/// 씬이 바뀌면 스코프가 통째로 파괴되므로 진입 시 서버에서 내 데이터를 다시 받는다(FrontlineSceneManager).
///
/// 인스펙터 할당: 아래 SO 필드 전부 + Parent > Type = GameLifetimeScope.
/// 씬 배치: FrontlineDirector, FrontlineHudController, FrontlineSceneManager 가 이 씬 하이어라키에 있어야 자동 주입된다.
/// </summary>
public sealed class FrontlineLifetimeScope : LifetimeScope {
    [Header("Domain Databases (SO)")]
    [SerializeField] private BattleSettings battleSettings;
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private EngravingDatabase engravingDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [Tooltip("코스튬 데이터베이스. 코스튬 일러스트/장착 효과를 id 로 조회한다.")]
    [SerializeField] private CostumeDatabase costumeDatabase;
    [Tooltip("시너지 데이터베이스. 스쿼드 시너지의 조건/효과를 담는다.")]
    [SerializeField] private SynergyDatabase synergyDatabase;
    [SerializeField] private HqNodeDatabase hqNodeDatabase;
    [Tooltip("던전 데이터베이스. 전투 BGM 을 찾는 데 쓴다. 비우면 공용 전투 BGM 이 나온다.")]
    [SerializeField] private DungeonDatabase dungeonDatabase;
    [Tooltip("전선 데이터베이스. 전선별 이름/배경/몬스터 구성과 단계 배율을 담는다.")]
    [SerializeField] private FrontlineDatabase frontlineDatabase;

    [Header("Catalogs (SO)")]
    [SerializeField] private CurrencyCatalog currencyCatalog;
    [SerializeField] private ShopIconCatalog shopIconCatalog;
    [SerializeField] private GradeColorCatalog gradeColorCatalog;
    [SerializeField] private GradeShardPrefabCatalog gradeShardPrefabCatalog;
    [SerializeField] private ProfileFrameCatalog profileFrameCatalog;
    [SerializeField] private SignatureBonusCatalog signatureBonusCatalog;
    [SerializeField] private BadgeCatalog badgeCatalog;
    [SerializeField] private UIVfxCatalog uiVfxCatalog;
    [Tooltip("씬 참조 카탈로그. 전선을 마친 뒤 돌아갈 메인 씬을 여기서 얻는다.")]
    [SerializeField] private SceneCatalog sceneCatalog;

    protected override void Configure(IContainerBuilder builder) {
        WarnIfUnassigned();

        // 정적 기본값 주입(다른 씬 스코프와 동일 규칙).
        if (gradeColorCatalog != null) GradeText.SetDefaultCatalog(gradeColorCatalog);
        if (signatureBonusCatalog != null) EquipmentRules.SetSignatureBonusCatalog(signatureBonusCatalog);
        if (badgeCatalog != null) BadgeCatalog.SetDefault(badgeCatalog);

        // 1) 정적 도메인 데이터.
        builder.RegisterInstance(battleSettings);
        builder.RegisterInstance(characterDatabase);
        builder.RegisterInstance(equipmentDatabase);
        builder.RegisterInstance(engravingDatabase);
        builder.RegisterInstance(itemDatabase);
        builder.RegisterInstance(costumeDatabase != null ? costumeDatabase : ScriptableObject.CreateInstance<CostumeDatabase>());
        builder.RegisterInstance(synergyDatabase != null ? synergyDatabase : ScriptableObject.CreateInstance<SynergyDatabase>());
        builder.RegisterInstance(hqNodeDatabase);
        builder.RegisterInstance(dungeonDatabase != null ? dungeonDatabase : ScriptableObject.CreateInstance<DungeonDatabase>());
        // 미할당이면 몬스터 구성이 없어 판이 즉시 끝난다. 씬 자체는 떠야 원인을 볼 수 있으므로 빈 것으로 폴백한다.
        builder.RegisterInstance(frontlineDatabase != null ? frontlineDatabase : ScriptableObject.CreateInstance<FrontlineDatabase>());
        builder.RegisterInstance(currencyCatalog);
        builder.RegisterInstance(shopIconCatalog);
        builder.RegisterInstance(gradeShardPrefabCatalog);
        builder.RegisterInstance(profileFrameCatalog);
        builder.RegisterInstance(sceneCatalog);
        builder.RegisterInstance(uiVfxCatalog != null ? uiVfxCatalog : ScriptableObject.CreateInstance<UIVfxCatalog>());

        // 2) 플레이어 상태 + 통신 서비스(캐릭터 최종 스탯 계산과 전선 통신에 필요한 것만).
        builder.Register<PlayerStateStore>(Lifetime.Singleton);
        builder.Register<CharacterAppearanceService>(Lifetime.Singleton);
        builder.Register<MainDataService>(Lifetime.Singleton).As<IMainDataService>();
        builder.Register<InventoryService>(Lifetime.Singleton).As<IInventoryService>();
        builder.Register<EngravingService>(Lifetime.Singleton).As<IEngravingService>();
        builder.Register<CostumeService>(Lifetime.Singleton).As<ICostumeService>();
        builder.Register<TrainingService>(Lifetime.Singleton).As<ITrainingService>();
        builder.Register<HeadquartersService>(Lifetime.Singleton).As<IHeadquartersService>();
        builder.Register<InvestmentService>(Lifetime.Singleton).As<IInvestmentService>();
        builder.Register<FrontlineService>(Lifetime.Singleton).As<IFrontlineService>();

        // 2-1) HUD 공용 의존성 묶음.
        builder.Register<FrontlineHudContext>(Lifetime.Singleton);

        // 3) 씬에 배치된 컴포넌트. 여기 빠지면 [Inject] Construct 가 호출되지 않아 컨텍스트가 null 로 남는다.
        builder.RegisterComponentInHierarchy<FrontlineDirector>();
        builder.RegisterComponentInHierarchy<FrontlineHudController>();
        builder.RegisterComponentInHierarchy<FrontlineSceneManager>();
    }

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
        Check(missing, frontlineDatabase, nameof(frontlineDatabase));
        Check(missing, currencyCatalog, nameof(currencyCatalog));
        Check(missing, shopIconCatalog, nameof(shopIconCatalog));
        Check(missing, gradeShardPrefabCatalog, nameof(gradeShardPrefabCatalog));
        Check(missing, profileFrameCatalog, nameof(profileFrameCatalog));
        Check(missing, sceneCatalog, nameof(sceneCatalog));

        if (missing.Length > 0)
            Debug.LogError($"[FrontlineLifetimeScope] 인스펙터 미할당: {missing}");
    }

    private static void Check(System.Text.StringBuilder sb, Object asset, string field) {
        if (asset != null) return;
        if (sb.Length > 0) sb.Append(", ");
        sb.Append(field);
    }
}
