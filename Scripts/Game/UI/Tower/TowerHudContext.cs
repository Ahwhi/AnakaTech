/// <summary>
/// 무한 타워 씬 HUD 의존성 묶음. 전투 화면 공용부(BattleHudContext)에 무한 타워 전용만 덧붙인다.
/// 덕분에 BattleCharacterPanel 과 보상 표시는 다른 전투 씬과 같은 컴포넌트를 그대로 쓴다.
/// </summary>
public sealed class TowerHudContext : BattleHudContext {
    /// <summary>무한 타워 씬의 전투 오케스트레이터(합류 진행/남은 시간/층 보너스 조회).</summary>
    public readonly TowerDirector Director;

    public readonly TowerDatabase TowerDb;
    public readonly ITowerService Tower;
    public readonly SceneCatalog Scenes;
    public readonly ISceneService SceneService;

    public TowerHudContext(TowerDirector director, PlayerStateStore store, CharacterDatabase charDb,
                           EquipmentDatabase equipDb, BattleSettings settings, TowerDatabase towerDb,
                           ITowerService tower, SceneCatalog scenes, ISceneService sceneService,
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
        TowerDb = towerDb;
        Tower = tower;
        Scenes = scenes;
        SceneService = sceneService;
    }
}
