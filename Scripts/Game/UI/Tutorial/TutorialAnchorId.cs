/// <summary>
/// 튜토리얼 가이드가 가리킬 수 있는 UI 지점의 식별자.
///
/// 값(정수)이 프리팹/씬에 직렬화되므로 기존 값은 절대 바꾸지 말고, 새 지점은 빈 번호에 추가한다.
/// 구간을 나눠 둔 이유는 화면별로 번호를 몰아 두면 추가할 때 어디에 넣을지 고민할 필요가 없기 때문이다.
///
/// 이 목록은 "가리킬 수 있는 곳"의 사전일 뿐이고, "어떤 퀘스트가 어디를 가리키는지"는
/// TutorialGuideRoutes(SO)가 정한다. 즉 경로가 바뀌어도 코드는 그대로다.
/// </summary>
public enum TutorialAnchorId {
    None = 0,

    // 메인 화면(HUD) ─────────────────────────────
    MainQuestClaim = 1,            // 메인 퀘스트 보상 수령(완료 상태에서 가리킨다)
    MainHamburgerMenu = 2,         // 햄버거 버튼(슬라이드 메뉴 열기)
    MainGachaButton = 3,
    MainShopButton = 4,
    MainDeckButton = 5,
    MainCommissionButton = 6,
    MainHeadquartersButton = 7,
    MainTrainingButton = 8,
    MainArenaButton = 9,
    MainGuildButton = 10,
    MainFriendButton = 11,
    MainDungeonSelectButton = 12,
    MainBoostButton = 13,          // 기력 부스트
    MainSweepButton = 14,          // 스테이지 소탕
    MainEncyclopediaButton = 15,
    MainAchievementButton = 16,
    MainMailButton = 17,
    MainAutoBattleButton = 18,     // 자동 전투 모드 토글(사냥 / 도전)
    MainProfileButton = 19,        // 좌상단 프로필(닉네임/전투력 표시)을 눌러 프로필 팝업 열기

    // 모집(가챠) ────────────────────────────────
    GachaCharacterBanner = 20,     // 가챠 메뉴에서 소녀 모집 배너
    GachaEquipBanner = 21,         // 가챠 메뉴에서 장비 모집 배너
    GachaDrawButton = 22,          // 실제 모집 실행 버튼

    // 편성 / 성장 ───────────────────────────────
    DeckCharacterCard = 30,        // 덱 팝업 좌측 소녀 카드(여러 개면 첫 카드)
    DeckCompositionButton = 31,    // 편성 패널 열기
    DeckCompositionSlot = 32,      // 진형 슬롯(편성 지점)
    DeckGrowButton = 33,           // 육성 패널 열기
    DeckEquipButton = 34,          // 장비 패널 열기
    DeckItemUseButton = 35,        // 아이템 사용 진입
    DeckCharacterCardUnassigned = 36,   // 아직 편성하지 않은 소녀 카드(편성 유도 전용. 제외 조건으로 편성된 카드를 뺀다)
    GrowthStatPage = 40,           // 육성의 스탯 페이지 탭
    GrowthStatPlus = 41,           // 스탯 투자 + 버튼
    GrowthAutoStat = 42,           // 스탯 자동 투자 설정
    GrowthStarPage = 43,           // 육성의 별레벨 페이지 탭
    GrowthStarUpButton = 44,       // 별레벨 승급 실행

    // 장비 / 각인 ───────────────────────────────
    EquipmentBagButton = 50,       // 가방 열기
    EquipmentBagItem = 51,         // 가방 속 장비 하나
    EquipmentEquipButton = 52,     // 장착 실행
    EquipmentEnhanceButton = 53,   // 강화 실행
    EquipmentAwakenButton = 54,    // 각성 실행
    EquipmentEngravingPage = 55,   // 각인 페이지 탭
    EngravingSlot = 56,            // 각인 슬롯(선택 팝업 열기)
    EngravingEquipButton = 57,     // 각인 장착 실행
    EquipmentEquippedSlot = 58,    // 착용 중인 장비 슬롯(누르면 상세가 열려 강화/각성으로 이어진다)
    EquipmentAwakenConfirm = 59,   // 각성 패널의 각성 실행

    // 의뢰 ─────────────────────────────────────
    CommissionRaidTab = 60,
    CommissionSubjugationTab = 61,
    CommissionEscortTab = 62,
    CommissionEnterButton = 63,    // 의뢰 도전 시작

    // 용병단 본부 ───────────────────────────────
    HeadquartersNode = 70,         // 올릴 수 있는 노드
    HeadquartersUpgradeButton = 71,

    // 수련 ─────────────────────────────────────
    TrainingSlot = 80,
    TrainingStartButton = 81,
    TrainingCharacterCard = 82,    // 수련 보낼 소녀 카드
    TrainingInstantButton = 83,    // 수련 즉시 완료(완료권 사용)
    TrainingClaimButton = 84,      // 수련 완료 수령(퀘스트는 이 시점에 진행된다)

    // 쟁탈전 ────────────────────────────────────
    ArenaMatchButton = 90,

    // 길드 ─────────────────────────────────────
    GuildSearchButton = 100,
    GuildJoinButton = 101,
    GuildRaidTab = 102,
    GuildRaidEnterButton = 103,

    // 친구 ─────────────────────────────────────
    FriendSearchTab = 110,
    FriendRequestButton = 111,

    // 아이템 ────────────────────────────────────
    ItemUseSlot = 120,
    ItemUseConfirmButton = 121,

    // 프로필 / 닉네임 ───────────────────────────
    ProfileNicknameButton = 130,   // 프로필 팝업의 닉네임 변경 진입
    NicknameCheckButton = 131,     // 닉네임 변경 팝업의 중복확인(통과해야 변경 버튼이 열린다)
    NicknameConfirmButton = 132,   // 닉네임 변경 실행

    // 스테이지 소탕 ─────────────────────────────
    StageSweepConfirmButton = 140  // 소탕 팝업의 소탕 실행
}
