using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 원정대 씬 컴포지션 루트(GuildRaidLifetimeScope 와 같은 구조/이유).
/// 씬이 바뀌면 스코프가 통째로 파괴되므로 진입 시 서버에서 내 데이터와 파티원 편성을 다시 받는다(ExpeditionSceneManager).
///
/// 인스펙터 할당: 아래 SO 필드 전부 + Parent > Type = GameLifetimeScope.
/// 씬 배치: ExpeditionDirector, GuildRaidHudController, ExpeditionSceneManager 가 이 씬 하이어라키에 있어야 한다.
/// </summary>
public sealed class ExpeditionLifetimeScope : LifetimeScope {
    [Header("Domain Databases (SO)")]
    [SerializeField] private BattleSettings battleSettings;
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private EngravingDatabase engravingDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [Tooltip("코스튬 데이터베이스. 내 캐릭터와 파티원의 코스튬 외형/장착 효과에 쓴다.")]
    [SerializeField] private CostumeDatabase costumeDatabase;
    [Tooltip("시너지 데이터베이스. 각 캐릭터는 자기 주인의 편성 기준으로 시너지를 받는다.")]
    [SerializeField] private SynergyDatabase synergyDatabase;
    [SerializeField] private HqNodeDatabase hqNodeDatabase;
    [Tooltip("던전 데이터베이스. 전투 BGM 을 찾는 데 쓴다. 비우면 공용 전투 BGM 이 나온다.")]
    [SerializeField] private DungeonDatabase dungeonDatabase;
    [Tooltip("원정대 장소 데이터베이스. 장소별 배경과 레벨별 몬스터 구성을 담는다.")]
    [SerializeField] private ExpeditionDatabase expeditionDatabase;

    [Header("Catalogs (SO)")]
    [SerializeField] private CurrencyCatalog currencyCatalog;
    [SerializeField] private ShopIconCatalog shopIconCatalog;
    [SerializeField] private GradeColorCatalog gradeColorCatalog;
    [SerializeField] private GradeShardPrefabCatalog gradeShardPrefabCatalog;
    [SerializeField] private ProfileFrameCatalog profileFrameCatalog;
    [SerializeField] private SignatureBonusCatalog signatureBonusCatalog;
    [SerializeField] private BadgeCatalog badgeCatalog;
    [SerializeField] private UIVfxCatalog uiVfxCatalog;
    [Tooltip("결과 팝업을 여는 데 쓴다.")]
    [SerializeField] private UIPopupCatalog popupCatalog;
    [Tooltip("씬 참조 카탈로그. 전투 종료 후 돌아갈 메인 씬을 여기서 얻는다.")]
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
        builder.RegisterInstance(expeditionDatabase != null ? expeditionDatabase : ScriptableObject.CreateInstance<ExpeditionDatabase>());
        builder.RegisterInstance(currencyCatalog);
        builder.RegisterInstance(shopIconCatalog);
        builder.RegisterInstance(gradeShardPrefabCatalog);
        builder.RegisterInstance(profileFrameCatalog);
        builder.RegisterInstance(popupCatalog);
        builder.RegisterInstance(sceneCatalog);
        builder.RegisterInstance(uiVfxCatalog != null ? uiVfxCatalog : ScriptableObject.CreateInstance<UIVfxCatalog>());

        // 2) 플레이어 상태 + 통신 서비스(내 캐릭터 최종 스탯 계산과 원정대 통신에 필요한 것만).
        builder.Register<PlayerStateStore>(Lifetime.Singleton);
        builder.Register<CharacterAppearanceService>(Lifetime.Singleton);
        builder.Register<MainDataService>(Lifetime.Singleton).As<IMainDataService>();
        builder.Register<InventoryService>(Lifetime.Singleton).As<IInventoryService>();
        builder.Register<EngravingService>(Lifetime.Singleton).As<IEngravingService>();
        builder.Register<CostumeService>(Lifetime.Singleton).As<ICostumeService>();
        builder.Register<TrainingService>(Lifetime.Singleton).As<ITrainingService>();
        builder.Register<DispatchService>(Lifetime.Singleton).As<IDispatchService>();
        builder.Register<HeadquartersService>(Lifetime.Singleton).As<IHeadquartersService>();
        builder.Register<InvestmentService>(Lifetime.Singleton).As<IInvestmentService>();
        builder.Register<ExpeditionService>(Lifetime.Singleton).As<IExpeditionService>();

        // 2-1) HUD 공용 의존성 묶음. 결과 팝업의 보상 표시가 이 묶음으로 아이콘/이름을 해석한다.
        builder.Register<BattleHudContext>(Lifetime.Singleton);

        // 3) 씬에 배치된 컴포넌트. HUD 는 길드 레이드와 같은 프리팹을 IBossGaugeSource 계약으로 공유한다.
        builder.RegisterComponentInHierarchy<ExpeditionDirector>().AsSelf().As<BattleStage>().As<IBossGaugeSource>();
        builder.RegisterComponentInHierarchy<GuildRaidHudController>();
        builder.RegisterComponentInHierarchy<ExpeditionSceneManager>();
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
        Check(missing, expeditionDatabase, nameof(expeditionDatabase));
        Check(missing, currencyCatalog, nameof(currencyCatalog));
        Check(missing, shopIconCatalog, nameof(shopIconCatalog));
        Check(missing, gradeShardPrefabCatalog, nameof(gradeShardPrefabCatalog));
        Check(missing, profileFrameCatalog, nameof(profileFrameCatalog));
        Check(missing, popupCatalog, nameof(popupCatalog));
        Check(missing, sceneCatalog, nameof(sceneCatalog));

        if (missing.Length > 0)
            Debug.LogError($"[ExpeditionLifetimeScope] 인스펙터 미할당: {missing}");
    }

    private static void Check(System.Text.StringBuilder sb, Object asset, string field) {
        if (asset != null) return;
        if (sb.Length > 0) sb.Append(", ");
        sb.Append(field);
    }
}
