using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 메인 씬 컴포지션 루트. 루트 스코프(GameLifetimeScope)의 자식으로 동작하며, 전투/데이터에 필요한
/// 도메인 데이터베이스(SO)와 서비스/상태를 등록한다. 루트가 등록한 서비스(INetworkService/ISoundService/
/// IUIService 등)는 자식 스코프에서 그대로 주입된다.
///
/// 인스펙터 할당: 데이터베이스 SO 5종(BattleSettings/Character/Monster/Equipment/Dungeon).
/// 씬 배치: BattleDirector, MainSceneManager 는 이 씬 하이어라키에 있어야 자동 주입된다.
/// </summary>
public sealed class MainLifetimeScope : LifetimeScope {
    [Header("Domain Databases (SO)")]
    [SerializeField] private BattleSettings battleSettings;
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private MonsterDatabase monsterDatabase;
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private DungeonDatabase dungeonDatabase;
    [Tooltip("각인 데이터베이스. 각인 효과(캐릭터 스탯 보정)와 각인 UI 가 id 로 조회한다.")]
    [SerializeField] private EngravingDatabase engravingDatabase;
    [Tooltip("소모품 데이터베이스. 아이템 사용 목록과 보상 슬롯이 id 로 조회한다.")]
    [SerializeField] private ItemDatabase itemDatabase;
    [Tooltip("코스튬 데이터베이스. 코스튬 일러스트/장착 효과를 id 로 조회한다(전투/표시 공용).")]
    [SerializeField] private CostumeDatabase costumeDatabase;
    [Tooltip("시너지 데이터베이스. 편성 시너지의 조건/효과를 담는다(활성 판정과 스탯 합산 공용).")]
    [SerializeField] private SynergyDatabase synergyDatabase;
    [Tooltip("용병단 본부 노드 데이터베이스. 계정 전체 스탯 보너스와 노드 트리 UI 가 사용한다.")]
    [SerializeField] private HqNodeDatabase hqNodeDatabase;
    [Tooltip("의뢰 데이터베이스. 의뢰 목록/씬 전투 구성이 종류-단계로 조회한다.")]
    [SerializeField] private CommissionDatabase commissionDatabase;
    [Tooltip("멸망전 페이즈 데이터베이스. 페이즈 번호로 등장 몬스터 구성을 조회한다.")]
    [SerializeField] private DoomDatabase doomDatabase;
    [Tooltip("전선 데이터베이스. 전선 팝업의 이름/썸네일을 조회한다(V1.12).")]
    [SerializeField] private FrontlineDatabase frontlineDatabase;
    [Tooltip("재화 표시 정보(아이콘/이름) 카탈로그. 재화 UI(CurrencyView)가 타입으로 조회한다.")]
    [SerializeField] private CurrencyCatalog currencyCatalog;
    [Tooltip("상점 상품 이미지 카탈로그. 오퍼의 ImageKey 로 상품 스프라이트를 조회한다.")]
    [SerializeField] private ShopIconCatalog shopIconCatalog;
    [Tooltip("등급별 텍스트 색 카탈로그. 등급 표시(GradeText)의 기본 색 소스로 등록된다.")]
    [SerializeField] private GradeColorCatalog gradeColorCatalog;
    [Tooltip("순위 구간별 텍스트 색 카탈로그. 순위 표시(RankText)의 기본 색 소스로 등록된다.")]
    [SerializeField] private RankColorCatalog rankColorCatalog;
    [Tooltip("등급별 조각(만능조각) 프리팹 카탈로그. 조각 표시(ShardPopup 등)가 등급으로 프리팹을 조회한다.")]
    [SerializeField] private GradeShardPrefabCatalog gradeShardPrefabCatalog;
    [Tooltip("프로필 테두리 카탈로그. 프로필/테두리 UI 가 id 로 스프라이트를 조회한다.")]
    [FormerlySerializedAs("nameplateCatalog")]
    [SerializeField] private ProfileFrameCatalog profileFrameCatalog;
    [Tooltip("전용무기 장착 보너스 카탈로그. 미할당이면 등급별 최종데미지 기본값(N 10 ~ XR 30)이 적용된다.")]
    [SerializeField] private SignatureBonusCatalog signatureBonusCatalog;
    [Tooltip("배지 프리팹(Unread=NEW / Claimable=빨간 동그라미) 카탈로그. 모든 BadgeView 의 기본값으로 등록된다(개별 할당 불필요).")]
    [SerializeField] private BadgeCatalog badgeCatalog;
    [Tooltip("UI 파티클 VFX 카탈로그(가챠 착지/레벨업 등). 미할당이면 빈 카탈로그로 폴백(VFX 없이 안전).")]
    [SerializeField] private UIVfxCatalog uiVfxCatalog;
    [Tooltip("씬 참조 카탈로그. 의뢰 팝업이 의뢰 씬으로 전환할 때 사용한다.")]
    [SerializeField] private SceneCatalog sceneCatalog;

    protected override void Configure(IContainerBuilder builder) {
        // 등급 색 카탈로그를 GradeText 의 기본값으로 등록(등급 표시가 프리팹마다 SO 를 지정하지 않아도 동작).
        if (gradeColorCatalog != null) GradeText.SetDefaultCatalog(gradeColorCatalog);
        // 순위 색 카탈로그를 RankText 의 기본값으로 등록(순위 표시가 프리팹마다 SO 를 지정하지 않아도 동작).
        if (rankColorCatalog != null) RankText.SetDefaultCatalog(rankColorCatalog);
        // 전용무기 보너스 카탈로그를 EquipmentRules 의 기본값으로 등록(장비 스탯 합산은 정적 경로라 여기서 1회 주입).
        if (signatureBonusCatalog != null) EquipmentRules.SetSignatureBonusCatalog(signatureBonusCatalog);
        // 배지 프리팹 카탈로그를 BadgeView 의 기본값으로 등록(배지마다 프리팹을 개별 할당하지 않아도 동작).
        if (badgeCatalog != null) BadgeCatalog.SetDefault(badgeCatalog);

        // 1) 정적 도메인 데이터(키 방식 아님, 인스펙터 할당 후 주입 - 규칙 5).
        builder.RegisterInstance(battleSettings);
        builder.RegisterInstance(characterDatabase);
        builder.RegisterInstance(monsterDatabase);
        builder.RegisterInstance(equipmentDatabase);
        builder.RegisterInstance(dungeonDatabase);
        builder.RegisterInstance(engravingDatabase);
        builder.RegisterInstance(itemDatabase);
        // 미할당이어도 씬 전체가 죽지 않도록 빈 데이터베이스로 폴백한다(코스튬만 안 보인다).
        builder.RegisterInstance(costumeDatabase != null ? costumeDatabase : ScriptableObject.CreateInstance<CostumeDatabase>());
        // 미할당이어도 씬 전체가 죽지 않도록 빈 데이터베이스로 폴백한다(시너지만 안 걸린다).
        builder.RegisterInstance(synergyDatabase != null ? synergyDatabase : ScriptableObject.CreateInstance<SynergyDatabase>());
        builder.RegisterInstance(hqNodeDatabase);
        builder.RegisterInstance(commissionDatabase);
        // 미할당이어도 메인 씬이 죽지 않도록 빈 데이터베이스로 폴백한다(멸망전 입장만 막힌다).
        builder.RegisterInstance(doomDatabase != null ? doomDatabase : ScriptableObject.CreateInstance<DoomDatabase>());
        // 미할당이어도 메인 씬이 죽지 않도록 빈 데이터베이스로 폴백한다(전선 카드가 기본 이름으로만 보인다).
        builder.RegisterInstance(frontlineDatabase != null ? frontlineDatabase : ScriptableObject.CreateInstance<FrontlineDatabase>());
        builder.RegisterInstance(currencyCatalog);
        builder.RegisterInstance(shopIconCatalog);
        builder.RegisterInstance(gradeShardPrefabCatalog);
        builder.RegisterInstance(profileFrameCatalog);
        builder.RegisterInstance(sceneCatalog);
        // 미할당이어도 MainHudContext 생성이 실패하지 않도록 빈 카탈로그로 폴백(VFX 만 안 나옴).
        builder.RegisterInstance(uiVfxCatalog != null ? uiVfxCatalog : ScriptableObject.CreateInstance<UIVfxCatalog>());

        // 2) 플레이어 상태(단일 소스) + 메인 통신 서비스.
        builder.Register<PlayerStateStore>(Lifetime.Singleton);
        // 캐릭터 외형 해석기(기본 일러스트 / 장착 코스튬). 초상을 그리는 모든 화면이 이 하나를 통과한다.
        builder.Register<CharacterAppearanceService>(Lifetime.Singleton);
        builder.Register<MainDataService>(Lifetime.Singleton).As<IMainDataService>();
        builder.Register<ShopService>(Lifetime.Singleton).As<IShopService>();
        // 인앱결제. 스토어 연결과 결제만 맡고 지급은 서버가 한다.
        // 미확정 결제 복구를 위해 씬보다 오래 살아야 하므로 싱글턴으로 둔다.
        builder.Register<IapService>(Lifetime.Singleton).As<IIapService>();
        builder.Register<InventoryService>(Lifetime.Singleton).As<IInventoryService>();
        builder.Register<MailService>(Lifetime.Singleton).As<IMailService>();
        builder.Register<ProfileService>(Lifetime.Singleton).As<IProfileService>();
        builder.Register<EncyclopediaService>(Lifetime.Singleton).As<IEncyclopediaService>();
        builder.Register<ChargeRewardService>(Lifetime.Singleton).As<IChargeRewardService>();
        builder.Register<CommissionService>(Lifetime.Singleton).As<ICommissionService>();
        builder.Register<EngravingService>(Lifetime.Singleton).As<IEngravingService>();
        builder.Register<CostumeService>(Lifetime.Singleton).As<ICostumeService>();
        builder.Register<ItemService>(Lifetime.Singleton).As<IItemService>();
        builder.Register<HeadquartersService>(Lifetime.Singleton).As<IHeadquartersService>();
        builder.Register<GuildService>(Lifetime.Singleton).As<IGuildService>();
        builder.Register<FriendService>(Lifetime.Singleton).As<IFriendService>();
        builder.Register<AchievementService>(Lifetime.Singleton).As<IAchievementService>();
        builder.Register<TrainingService>(Lifetime.Singleton).As<ITrainingService>();
        builder.Register<DispatchService>(Lifetime.Singleton).As<IDispatchService>();
        builder.Register<ArenaService>(Lifetime.Singleton).As<IArenaService>();
        // 원정대(주간 5인 파티). 오늘 칠 수 있는지가 배지 근거라 BadgeCenter 도 이 서비스를 본다.
        builder.Register<ExpeditionService>(Lifetime.Singleton).As<IExpeditionService>();
        builder.Register<CouponService>(Lifetime.Singleton).As<ICouponService>();
        // 투자(골드 소모처 + 계정 전체 효과). 전투 효과는 스토어에 담긴 레벨을 근거로 전투가 적용한다.
        builder.Register<InvestmentService>(Lifetime.Singleton).As<IInvestmentService>();
        // 한정 판매는 서버 푸시(S_FlashSaleNotify)를 상시 구독하므로 화면과 무관하게 살아 있어야 한다.
        builder.Register<FlashSaleService>(Lifetime.Singleton).As<IFlashSaleService>();
        // 채팅도 서버 푸시를 상시 받아야 하므로 화면과 무관하게 살아 있어야 한다(창을 닫아도 대화가 쌓인다).
        builder.Register<ChatService>(Lifetime.Singleton).As<IChatService>();
        builder.Register<RankingService>(Lifetime.Singleton).As<IRankingService>();
        // 멸망전은 최고 점수 조회와 보상 수령만 오간다(한 판의 진행은 전부 클라가 들고 있다).
        builder.Register<DoomService>(Lifetime.Singleton).As<IDoomService>();
        // 무한 타워도 층/보너스 조회와 등반 보고만 오간다(릴레이 진행은 클라가 들고 있다).
        builder.Register<TowerService>(Lifetime.Singleton).As<ITowerService>();
        // 전선도 스쿼드/단계 조회와 입장/종료 보고만 오간다(한 판의 전투는 클라가 치른다).
        builder.Register<FrontlineService>(Lifetime.Singleton).As<IFrontlineService>();
        // 영웅전도 단계/도전 횟수 조회와 입장/클리어 보고만 오간다(직접 조작 전투는 클라가 치른다).
        builder.Register<HeroesService>(Lifetime.Singleton).As<IHeroesService>();
        // 패스는 서버 푸시(S_PassInfo)를 상시 받아 배지를 켜므로 화면과 무관하게 살아 있어야 한다.
        builder.Register<PassService>(Lifetime.Singleton).As<IPassService>();

        builder.Register<OfflineRewardService>(Lifetime.Singleton).As<IOfflineRewardService>();
        builder.Register<DiamondDetailService>(Lifetime.Singleton).As<IDiamondDetailService>();
        // 메인 퀘스트도 서버 푸시(S_MainQuest / S_MainQuestNotify)를 상시 구독한다. 퀘스트는 다른 화면에서 한
        // 행동 때문에 완료되므로, 화면이 열려 있을 때만 살아 있으면 그 완료를 놓친다.
        builder.Register<MainQuestService>(Lifetime.Singleton).As<IMainQuestService>();

        // 알림 배지 허브(도감/충전 보상 수령가능 + 가챠 로테이션/상점 신규 New + 이벤트). 배지(BadgeView)가 주입받는다.
        builder.Register<BadgeCenter>(Lifetime.Singleton);

        // 2-1) HUD 공용 의존성 묶음. DI 로 등록해두면 HUD 패널뿐 아니라 자가 주입 버튼(HudMenuButton 등)이
        //      인스펙터 참조 없이 [Inject] 로 받아 팝업을 열 수 있다(확장 시 컨트롤러 수정 불필요).
        builder.Register<MainHudContext>(Lifetime.Singleton);

        // 오프라인 보상 표시 흐름. HUD 묶음(아이콘 해석용)을 쓰므로 그 뒤에 등록한다.
        builder.Register<OfflineRewardPresenter>(Lifetime.Singleton).As<IOfflineRewardPresenter>();

        // 3) 씬에 배치된 전투 오케스트레이터/매니저/HUD 컨트롤러를 등록해 주입 대상으로 만든다.
        //    HUD 각 패널은 MainHudController 가 구동하므로 개별 등록하지 않는다(씬에 없어도 안전).
        builder.RegisterComponentInHierarchy<BattleDirector>();
        builder.RegisterComponentInHierarchy<MainSceneManager>();
        builder.RegisterComponentInHierarchy<MainHudController>();
    }
}
