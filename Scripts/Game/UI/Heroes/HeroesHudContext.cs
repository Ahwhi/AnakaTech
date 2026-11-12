/// <summary>
/// 영웅전 씬 HUD 의존성 묶음. 전투 화면 공용부(BattleHudContext)에 영웅전 전용만 덧붙인다.
/// 덕분에 보상 표시/일시정지 팝업은 다른 전투 씬과 같은 컴포넌트를 그대로 쓴다.
/// </summary>
public sealed class HeroesHudContext : BattleHudContext {
    /// <summary>영웅전 씬의 전투 오케스트레이터(남은 시간/단계/조작 소녀 조회).</summary>
    public readonly HeroesDirector Director;

    public readonly IHeroesService Heroes;
    public readonly SceneCatalog Scenes;
    public readonly ISceneService SceneService;

    public HeroesHudContext(HeroesDirector director, PlayerStateStore store, CharacterDatabase charDb,
                            EquipmentDatabase equipDb, BattleSettings settings,
                            IHeroesService heroes, SceneCatalog scenes, ISceneService sceneService,
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
        Heroes = heroes;
        Scenes = scenes;
        SceneService = sceneService;
    }
}
