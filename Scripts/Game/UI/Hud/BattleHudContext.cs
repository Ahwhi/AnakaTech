/// <summary>
/// 전투 화면 공용 HUD 의존성 묶음. "어느 전투 무대인지"(BattleStage)와 상태 + 표시용 카탈로그만 담기 때문에
/// 메인 씬(방치 던전)과 의뢰 씬이 같은 패널/팝업 컴포넌트를 그대로 공유할 수 있다
/// (BattleCharacterPanel / DamageMeterPanel / 보상 표시 등).
///
/// 메인 씬 전용 의존성(상점/우편/도감 등)은 파생 클래스 MainHudContext 가 덧붙인다.
/// 새 전투 컨텐츠를 추가할 때는 이 클래스를 상속한 컨텍스트를 하나 만들면 되고, 공용 패널은 손대지 않는다.
/// </summary>
public class BattleHudContext {
    public readonly BattleStage Stage;
    public readonly PlayerStateStore Store;
    public readonly CharacterDatabase CharDb;
    public readonly EquipmentDatabase EquipDb;
    public readonly BattleSettings Settings;
    public readonly IUIService Ui;
    public readonly UIPopupCatalog Popups;
    public readonly ISoundService Sound;

    // 보상/아이콘 표시용 카탈로그. 어느 씬에서든 같은 규칙으로 보상을 그리기 위해 공용부에 둔다(RewardVisualResolver).
    public readonly CurrencyCatalog Currencies;
    public readonly ShopIconCatalog ShopIcons;
    public readonly GradeShardPrefabCatalog GradeShardPrefabs;
    public readonly ProfileFrameCatalog ProfileFrames;
    public readonly UIVfxCatalog UiVfx;
    /// <summary>각인 데이터. 보상으로 지급된 각인 도면의 아이콘/이름 해석에 쓰이므로 표시용 카탈로그와 함께 공용부에 둔다.</summary>
    public readonly EngravingDatabase EngravingDb;
    /// <summary>소모품 데이터. 보상 슬롯이 아이템 아이콘/이름을 같은 규칙으로 그리기 위해 공용부에 둔다.</summary>
    public readonly ItemDatabase ItemDb;
    /// <summary>코스튬 데이터. 코스튬 목록/보상 표시가 id 로 일러스트와 이름을 조회한다.</summary>
    public readonly CostumeDatabase CostumeDb;
    /// <summary>시너지 데이터. 편성 화면이 활성 시너지를 판정하고, 스탯 합산도 같은 표를 쓴다.</summary>
    public readonly SynergyDatabase SynergyDb;
    /// <summary>
    /// 캐릭터 외형 해석기(기본 일러스트 / 장착 코스튬). 초상을 그리는 화면은 CharDb 대신 이쪽을 거쳐야
    /// 내 캐릭터든 남의 캐릭터든 코스튬이 빠짐없이 반영된다.
    /// </summary>
    public readonly CharacterAppearanceService Appearance;

    public BattleHudContext(BattleStage stage, PlayerStateStore store, CharacterDatabase charDb,
                            EquipmentDatabase equipDb, BattleSettings settings, IUIService ui,
                            UIPopupCatalog popups, ISoundService sound,
                            CurrencyCatalog currencies, ShopIconCatalog shopIcons,
                            GradeShardPrefabCatalog gradeShardPrefabs, ProfileFrameCatalog profileFrames, UIVfxCatalog uiVfx,
                            EngravingDatabase engravingDb, ItemDatabase itemDb,
                            CostumeDatabase costumeDb, SynergyDatabase synergyDb,
                            CharacterAppearanceService appearance) {
        Stage = stage;
        Store = store;
        CharDb = charDb;
        EquipDb = equipDb;
        Settings = settings;
        Ui = ui;
        Popups = popups;
        Sound = sound;
        Currencies = currencies;
        ShopIcons = shopIcons;
        GradeShardPrefabs = gradeShardPrefabs;
        ProfileFrames = profileFrames;
        UiVfx = uiVfx;
        EngravingDb = engravingDb;
        ItemDb = itemDb;
        CostumeDb = costumeDb;
        SynergyDb = synergyDb;
        Appearance = appearance;
    }
}
