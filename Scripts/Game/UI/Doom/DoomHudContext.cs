/// <summary>
/// 멸망전 씬 HUD 의존성 묶음. 전투 화면 공용부(BattleHudContext)에 멸망전 전용만 덧붙인다.
/// 덕분에 BattleCharacterPanel 과 보상 표시는 메인/의뢰 씬과 같은 컴포넌트를 그대로 쓴다.
/// </summary>
public sealed class DoomHudContext : BattleHudContext {
    /// <summary>멸망전 씬의 전투 오케스트레이터(페이즈 진행/점수/증강 조회).</summary>
    public readonly DoomDirector Director;

    public readonly DoomDatabase DoomDb;
    public readonly IDoomService Doom;
    public readonly SceneCatalog Scenes;
    public readonly ISceneService SceneService;

    public DoomHudContext(DoomDirector director, PlayerStateStore store, CharacterDatabase charDb,
                          EquipmentDatabase equipDb, BattleSettings settings, DoomDatabase doomDb,
                          IDoomService doom, SceneCatalog scenes, ISceneService sceneService,
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
        DoomDb = doomDb;
        Doom = doom;
        Scenes = scenes;
        SceneService = sceneService;
    }
}
