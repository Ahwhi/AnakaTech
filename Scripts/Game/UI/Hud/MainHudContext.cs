/// <summary>
/// 메인 씬 HUD/팝업들이 공통으로 참조하는 의존성 묶음. MainHudController 가 주입받은 서비스/상태/데이터를 모아
/// 각 패널의 Initialize 로 전달한다. 패널을 개별 DI 등록하지 않아 씬에 없는 패널이 있어도 안전하다(널 세이프).
///
/// 전투 화면 공용부(무대/상태/재화 카탈로그 등)는 베이스인 BattleHudContext 가 갖고,
/// 여기에는 메인 씬에만 있는 서비스(상점/우편/도감/의뢰 등)를 덧붙인다.
/// </summary>
public sealed class MainHudContext : BattleHudContext {
    /// <summary>메인 씬의 전투 오케스트레이터. 던전 정보/줌/덱 변경 등 방치형 전용 기능을 쓰는 패널이 참조한다.</summary>
    public readonly BattleDirector Battle;

    public readonly DungeonDatabase DungeonDb;
    public readonly HqNodeDatabase HqDb;
    public readonly IMainDataService MainData;
    public readonly IShopService Shop;
    /// <summary>인앱결제(스토어 결제 + 영수증). 지급은 서버가 하며 이 창구는 결제만 맡는다.</summary>
    public readonly IIapService Iap;
    public readonly IInventoryService Inventory;
    public readonly IMailService Mail;
    public readonly IProfileService Profile;
    public readonly IEncyclopediaService Encyclopedia;
    public readonly IChargeRewardService ChargeReward;
    public readonly ICommissionService Commission;
    public readonly IEngravingService Engraving;
    public readonly ICostumeService Costume;
    public readonly IItemService Item;
    public readonly IHeadquartersService Headquarters;
    public readonly IGuildService Guild;
    public readonly IFriendService Friend;
    public readonly IAchievementService Achievement;
    public readonly IRankingService Ranking;
    public readonly ITrainingService Training;
    public readonly IArenaService Arena;
    /// <summary>원정대(주간 5인 파티 + 장소 5곳). 원정대 팝업과 배지, 캐릭터 전투력 보고가 함께 쓴다.</summary>
    public readonly IExpeditionService Expedition;
    /// <summary>파견(덱과 별개인 편성 컨텐츠). 파견 화면과 완료 배지가 함께 쓴다.</summary>
    public readonly IDispatchService Dispatch;
    public readonly ICouponService Coupon;
    /// <summary>투자(골드 소모처 + 계정 전체 효과). 투자 화면과 전투 효과가 함께 쓴다.</summary>
    public readonly IInvestmentService Investment;
    /// <summary>멸망전(페이즈 방어 + 증강). 입구 팝업과 멸망전 씬이 함께 쓴다.</summary>
    public readonly IDoomService Doom;
    /// <summary>무한 타워(하루 1층 릴레이 등반). 입구 팝업과 배지가 함께 쓴다.</summary>
    public readonly ITowerService Tower;
    /// <summary>전선(6곳 스쿼드 + 기력 소모 단계 진행, V1.12). 전선 팝업과 배치 팝업이 함께 쓴다.</summary>
    public readonly IFrontlineService Frontline;
    /// <summary>전선 정적 데이터(이름/썸네일).</summary>
    public readonly FrontlineDatabase FrontlineDb;
    /// <summary>영웅전(소녀 1명 직접 조작 + 단계 진행, V1.12). 입구 팝업과 배지가 함께 쓴다.</summary>
    public readonly IHeroesService Heroes;
    /// <summary>패스(기력 소모 레벨 + 무료/평생 패스 보상 두 줄). 패스 화면과 배지가 함께 쓴다.</summary>
    public readonly IPassService Pass;

    /// <summary>한정 판매(플래시 세일). 메인 버튼 배지와 안내 팝업이 쓴다.</summary>
    public readonly IFlashSaleService FlashSale;
    /// <summary>채팅(전체/길드 + 시스템 알림). 메인 화면 한 줄과 채팅 패널이 함께 쓴다.</summary>
    public readonly IChatService Chat;
    /// <summary>스토리(장 스토리 19편 + 인트로). 스토리 목록 팝업과 배지가 함께 쓴다.</summary>
    public readonly IStoryService Story;

    /// <summary>다이아 유상/무상 보유 내역. 재화 상세 팝업이 쓴다.</summary>
    public readonly IDiamondDetailService DiamondDetail;
    /// <summary>메인 퀘스트(진행 중 1건 + 완료 알림). 메인 화면 안내와 완료 토스트가 함께 쓴다.</summary>
    public readonly IMainQuestService MainQuest;
    public readonly BadgeCenter Badges;
    public readonly CommissionDatabase CommissionDb;
    /// <summary>멸망전 페이즈 데이터. 입구 팝업이 컨텐츠 유무를 확인하는 데 쓴다.</summary>
    public readonly DoomDatabase DoomDb;
    public readonly SceneCatalog Scenes;
    public readonly ISceneService SceneService;

    /// <summary>로그인 세션. 계정 공개 식별자(PlayerGuid = UID) 표시에 쓴다.
    /// 세션 토큰은 자동 재로그인 자격증명이므로 어떤 UI 에도 노출하면 안 된다.</summary>
    public readonly ILoginService Login;

    /// <summary>약관 열람과 선택 동의 변경. 설정의 계정 탭이 쓴다.</summary>
    public readonly ITermsConsentFlow Terms;

    /// <summary>회원 탈퇴. 설정의 계정 탭이 쓴다.</summary>
    public readonly IAccountService Account;

    /// <summary>로그아웃. 탈퇴 완료 후 타이틀 복귀에도 같은 경로를 쓴다.</summary>
    public readonly ILogoutService Logout;

    public MainHudContext(PlayerStateStore store, BattleDirector battle, CharacterDatabase charDb,
                          DungeonDatabase dungeonDb, EquipmentDatabase equipDb, EngravingDatabase engravingDb,
                          ItemDatabase itemDb, IItemService item,
                          HqNodeDatabase hqDb, CommissionDatabase commissionDb, BattleSettings settings,
                          IMainDataService mainData, IShopService shop, IIapService iap, IInventoryService inventory,
                          IMailService mail, IProfileService profile, IEncyclopediaService encyclopedia,
                          IChargeRewardService chargeReward, ICommissionService commission,
                          IEngravingService engraving, ICostumeService costume,
                          IHeadquartersService headquarters, IGuildService guild,
                          IFriendService friend, IAchievementService achievement, IRankingService ranking,
                          ITrainingService training, IArenaService arena, IExpeditionService expedition,
                          ICouponService coupon,
                          IDispatchService dispatch,
                          IInvestmentService investment,
                          IDoomService doom, DoomDatabase doomDb, ITowerService tower,
                          IFrontlineService frontline, FrontlineDatabase frontlineDb, IHeroesService heroes, IPassService pass,
                          IFlashSaleService flashSale, IChatService chat,
                          IDiamondDetailService diamondDetail, IMainQuestService mainQuest,
                          IStoryService story,
                          BadgeCenter badges, IUIService ui, UIPopupCatalog popups, SceneCatalog scenes,
                          ISceneService sceneService, ILoginService login,
                          ITermsConsentFlow terms, IAccountService account, ILogoutService logout,
                          CurrencyCatalog currencies, ShopIconCatalog shopIcons, GradeShardPrefabCatalog gradeShardPrefabs,
                          ProfileFrameCatalog profileFrames, UIVfxCatalog uiVfx, ISoundService sound,
                          CostumeDatabase costumeDb, SynergyDatabase synergyDb,
                          CharacterAppearanceService appearance)
        : base(battle, store, charDb, equipDb, settings, ui, popups, sound,
               currencies, shopIcons, gradeShardPrefabs, profileFrames, uiVfx, engravingDb, itemDb,
               costumeDb, synergyDb, appearance) {
        Battle = battle;
        DungeonDb = dungeonDb;
        Item = item;
        HqDb = hqDb;
        MainData = mainData;
        Shop = shop;
        Iap = iap;
        Inventory = inventory;
        Mail = mail;
        Profile = profile;
        Encyclopedia = encyclopedia;
        ChargeReward = chargeReward;
        Commission = commission;
        Engraving = engraving;
        Costume = costume;
        Headquarters = headquarters;
        Guild = guild;
        Friend = friend;
        Achievement = achievement;
        Ranking = ranking;
        Training = training;
        Dispatch = dispatch;
        Arena = arena;
        Expedition = expedition;
        Coupon = coupon;
        Investment = investment;
        Doom = doom;
        DoomDb = doomDb;
        Tower = tower;
        Frontline = frontline;
        FrontlineDb = frontlineDb;
        Heroes = heroes;
        Pass = pass;
        FlashSale = flashSale;
        Chat = chat;
        DiamondDetail = diamondDetail;
        Story = story;
        MainQuest = mainQuest;
        Badges = badges;
        CommissionDb = commissionDb;
        Scenes = scenes;
        SceneService = sceneService;
        Login = login;
        Terms = terms;
        Account = account;
        Logout = logout;
    }
}
