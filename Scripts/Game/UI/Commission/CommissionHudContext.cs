/// <summary>
/// 의뢰 씬 HUD 의존성 묶음. 전투 화면 공용부(BattleHudContext)에 의뢰 씬 전용 서비스만 덧붙인다.
/// 덕분에 BattleCharacterPanel / DamageMeterPanel / 보상 표시는 메인 씬과 완전히 같은 컴포넌트를 그대로 쓴다.
/// </summary>
public sealed class CommissionHudContext : BattleHudContext {
    /// <summary>의뢰 씬의 전투 오케스트레이터(일시정지/진행 상태 조회).</summary>
    public readonly CommissionDirector Director;

    public readonly CommissionDatabase CommissionDb;
    public readonly ICommissionService Commission;
    public readonly SceneCatalog Scenes;
    public readonly ISceneService SceneService;

    public CommissionHudContext(CommissionDirector director, PlayerStateStore store, CharacterDatabase charDb,
                                EquipmentDatabase equipDb, BattleSettings settings, CommissionDatabase commissionDb,
                                ICommissionService commission, SceneCatalog scenes, ISceneService sceneService,
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
        CommissionDb = commissionDb;
        Commission = commission;
        Scenes = scenes;
        SceneService = sceneService;
    }
}
