/// <summary>
/// 전선 씬 HUD 의존성 묶음. 전투 화면 공용부(BattleHudContext)에 전선 전용만 덧붙인다.
/// 덕분에 BattleCharacterPanel, 재화 패널, 보상 표시는 다른 전투 씬과 같은 컴포넌트를 그대로 쓴다.
/// </summary>
public sealed class FrontlineHudContext : BattleHudContext {
    /// <summary>전선 씬의 전투 오케스트레이터(한 판의 진행/남은 몬스터).</summary>
    public readonly FrontlineDirector Director;

    public readonly FrontlineDatabase FrontlineDb;
    public readonly IFrontlineService Frontline;
    public readonly SceneCatalog Scenes;
    public readonly ISceneService SceneService;

    public FrontlineHudContext(FrontlineDirector director, PlayerStateStore store, CharacterDatabase charDb,
                               EquipmentDatabase equipDb, BattleSettings settings, FrontlineDatabase frontlineDb,
                               IFrontlineService frontline, SceneCatalog scenes, ISceneService sceneService,
                               IUIService ui, UIPopupCatalog popups, ISoundService sound,
                               CurrencyCatalog currencies, ShopIconCatalog shopIcons,
                               GradeShardPrefabCatalog gradeShardPrefabs, ProfileFrameCatalog profileFrames, UIVfxCatalog uiVfx,
                               EngravingDatabase engravingDb, ItemDatabase itemDb,
                               CostumeDatabase costumeDb, SynergyDatabase synergyDb,
                               CharacterAppearanceService appearance)
        : base(director, store, charDb, equipDb, settings, ui, popups, sound,
               currencies, shopIcons, gradeShardPrefabs, profileFrames, uiVfx, engravingDb, itemDb,
               costumeDb, synergyDb, appearance) {
        Director = director;
        FrontlineDb = frontlineDb;
        Frontline = frontline;
        Scenes = scenes;
        SceneService = sceneService;
    }
}
