/// <summary>
/// RewardItem(구조적 보상) -> RewardSlotView.RewardVisual(아이콘 + 수량 + 이름) 해석기(공용).
/// 아이콘 소스: 재화=CurrencyCatalog, 프로필 테두리=ProfileFrameCatalog, 만능조각=GradeShardPrefabCatalog(등급별 아이콘),
/// 랜덤장비/경험치/기타=ShopIconCatalog(imageKey). 품목이 늘면 이 switch 만 확장하면 된다.
///
/// 수량은 개수형 품목이면 1개여도 항상 표시한다(어떤 칸은 수량이 있고 어떤 칸은 없으면 빠진 것처럼 보인다).
/// </summary>
public static class RewardVisualResolver {
    // 랜덤 장비 등 전용 아이콘이 없는 보상용 ShopIconCatalog 키(에디터에서 등록).
    public const string RandomEquipIconKey = "reward_random_equip";
    // 무기를 뺀 랜덤 장비(방어구/신발/악세서리). 나오는 것이 달라 아이콘도 따로 쓴다.
    public const string RandomGeneralEquipIconKey = "reward_random_general_equip";
    // 경험치는 지갑 재화가 아니라 CurrencyCatalog 에 없으므로 같은 방식으로 키 아이콘을 쓴다.
    public const string ExpIconKey = "reward_exp";
    // 랜덤 등급 만능조각(등급 미정)은 등급별 아이콘을 고를 수 없으므로 전용 키 아이콘을 쓴다.
    public const string RandomShardIconKey = "reward_random_shard";
    // 각인 도면은 예고 시점에 대상이 미정이라 전용 키 아이콘을 쓴다(지급 확정분은 각인 자체 아이콘으로 그린다).
    public const string RandomEngravingIconKey = "reward_random_engraving";

    public static RewardSlotView.RewardVisual Resolve(BattleHudContext hud, RewardItem item) {
        var cc = hud != null ? hud.Currencies : null;
        string name = DisplayName(hud, item);   // 마우스오버 툴팁용 이름(품목별로 카탈로그에서 해석).
        switch (item.Kind) {
            case RewardItemKind.Gold:
                return Icon(cc != null ? cc.Icon(CurrencyType.Gold) : null, item.Amount, name);
            case RewardItemKind.Diamond:
                return Icon(cc != null ? cc.Icon(CurrencyType.Diamond) : null, item.Amount, name);
            case RewardItemKind.Stamina:
                return Icon(cc != null ? cc.Icon(CurrencyType.Stamina) : null, item.Amount, name);
            case RewardItemKind.Currency:
                return Icon(CurrencyKeyIcon(cc, item.CurrencyKey), item.Amount, name);
            case RewardItemKind.UniversalShard: {
                var sprite = hud != null && hud.GradeShardPrefabs != null ? hud.GradeShardPrefabs.Icon((Grade)item.Grade) : null;
                return new RewardSlotView.RewardVisual(sprite, Qty(item.Amount), name);
            }
            case RewardItemKind.RandomUniversalShard: {
                var sprite = hud != null && hud.ShopIcons != null ? hud.ShopIcons.Get(RandomShardIconKey) : null;
                return new RewardSlotView.RewardVisual(sprite, Qty(item.Amount), name);
            }
            case RewardItemKind.ProfileFrame: {
                var sprite = hud != null && hud.ProfileFrames != null ? hud.ProfileFrames.Icon(item.RefId) : null;
                return new RewardSlotView.RewardVisual(sprite, Qty(item.Amount), name);
            }
            case RewardItemKind.RandomEquip: {
                var sprite = hud != null && hud.ShopIcons != null ? hud.ShopIcons.Get(RandomEquipIconKey) : null;
                return new RewardSlotView.RewardVisual(sprite, Qty(item.Amount), name);
            }
            case RewardItemKind.RandomGeneralEquip: {
                var sprite = hud != null && hud.ShopIcons != null ? hud.ShopIcons.Get(RandomGeneralEquipIconKey) : null;
                return new RewardSlotView.RewardVisual(sprite, Qty(item.Amount), name);
            }
            case RewardItemKind.RandomEngraving: {
                // 지급이 확정된 뒤에는(RefId != 0) 실제로 받은 각인 아이콘을 보여주고, 예고 표시일 때만 공용 도면 아이콘을 쓴다.
                var data = EngravingOf(hud, item.RefId);
                var sprite = data != null ? data.Icon
                           : hud != null && hud.ShopIcons != null ? hud.ShopIcons.Get(RandomEngravingIconKey) : null;
                return new RewardSlotView.RewardVisual(sprite, Qty(item.Amount), name);
            }
            case RewardItemKind.Item: {
                var data = hud != null && hud.ItemDb != null ? hud.ItemDb.Get(item.RefId) : null;
                return new RewardSlotView.RewardVisual(data != null ? data.Icon : null, Qty(item.Amount), name);
            }
            case RewardItemKind.Exp:
                return Icon(hud != null && hud.ShopIcons != null ? hud.ShopIcons.Get(ExpIconKey) : null, item.Amount, name);
            case RewardItemKind.Character: {
                var data = hud != null && hud.CharDb != null ? hud.CharDb.Get(item.RefId) : null;
                return new RewardSlotView.RewardVisual(data != null ? data.Illustration : null, Qty(item.Amount), name);
            }
            case RewardItemKind.Costume: {
                // 코스튬은 그 자체가 일러스트라 별도 아이콘 없이 코스튬 일러스트를 그대로 보여 준다.
                var data = CostumeOf(hud, item.RefId);
                return new RewardSlotView.RewardVisual(data != null ? data.Illustration : null, Qty(item.Amount), name);
            }
            case RewardItemKind.UnitSkin: {
                var data = UnitSkinOf(hud, item.RefId);
                return new RewardSlotView.RewardVisual(data != null ? data.icon : null, Qty(item.Amount), name);
            }
            case RewardItemKind.Equip: {
                // 무엇이 오는지 정해진 장비라 랜덤 장비 아이콘이 아니라 그 장비의 아이콘을 그대로 보여 준다.
                var data = hud != null && hud.EquipDb != null ? hud.EquipDb.Get(item.RefId) : null;
                return new RewardSlotView.RewardVisual(data != null ? data.Icon : null, Qty(item.Amount), name);
            }
            case RewardItemKind.CharacterShard: {
                // 조각은 그 소녀의 것이므로 소녀 일러스트로 보여 준다(만능조각과 달리 대상이 정해져 있다).
                var data = hud != null && hud.CharDb != null ? hud.CharDb.Get(item.RefId) : null;
                return new RewardSlotView.RewardVisual(data != null ? data.Illustration : null, Qty(item.Amount), name);
            }
            case RewardItemKind.LifetimePass:
                // 상품 자체를 구성품 자리에 보여 주는 표시라 상점 카드와 같은 아이콘(상품 키)을 쓴다.
                return Icon(hud != null && hud.ShopIcons != null ? hud.ShopIcons.Get(PassRules.LifetimeOfferId) : null, item.Amount, name);
            case RewardItemKind.EventToken:
                return Icon(hud != null && hud.ShopIcons != null ? hud.ShopIcons.Get(EventTokens.IconKey(item.CurrencyKey)) : null,
                            item.Amount, name);
            default:
                return new RewardSlotView.RewardVisual(null, Qty(item.Amount), name);
        }
    }

    /// <summary>보상 품목의 표시 이름(마우스오버 툴팁 등). 아이콘 해석과 같은 소스를 쓴다.</summary>
    public static string DisplayName(BattleHudContext hud, RewardItem item) {
        var cc = hud != null ? hud.Currencies : null;
        switch (item.Kind) {
            case RewardItemKind.Gold: return cc != null ? cc.DisplayName(CurrencyType.Gold) : "골드";
            case RewardItemKind.Diamond: return cc != null ? cc.DisplayName(CurrencyType.Diamond) : "다이아";
            case RewardItemKind.Stamina: return cc != null ? cc.DisplayName(CurrencyType.Stamina) : "기력";
            case RewardItemKind.Currency: return CurrencyKeyName(cc, item.CurrencyKey);
            case RewardItemKind.UniversalShard: return $"{EncyclopediaRewardRules.GradeName(item.Grade)} 만능조각";
            case RewardItemKind.RandomUniversalShard: return "랜덤 만능조각";
            case RewardItemKind.ProfileFrame: return hud != null && hud.ProfileFrames != null ? hud.ProfileFrames.DisplayName(item.RefId) : "프로필 테두리";
            case RewardItemKind.RandomEquip: return $"랜덤 {EncyclopediaRewardRules.GradeName(item.Grade)} 장비";
            case RewardItemKind.RandomGeneralEquip: return $"{EncyclopediaRewardRules.GradeName(item.Grade)} 랜덤 장비(비무기)";
            case RewardItemKind.RandomEngraving: {
                var data = EngravingOf(hud, item.RefId);
                return data != null ? $"{data.DisplayName} 각인 도면" : "랜덤 각인 도면";
            }
            case RewardItemKind.Item: {
                var data = hud != null && hud.ItemDb != null ? hud.ItemDb.Get(item.RefId) : null;
                return data != null ? data.DisplayName : "아이템";
            }
            case RewardItemKind.Exp: return "경험치";
            case RewardItemKind.Character: {
                var data = hud != null && hud.CharDb != null ? hud.CharDb.Get(item.RefId) : null;
                return data != null ? data.DisplayName : "캐릭터";
            }
            case RewardItemKind.Costume: {
                var data = CostumeOf(hud, item.RefId);
                return data != null ? $"{data.DisplayName} 코스튬" : "코스튬";
            }
            case RewardItemKind.UnitSkin: {
                var data = UnitSkinOf(hud, item.RefId);
                return data != null ? data.displayName : "아웃핏";
            }
            case RewardItemKind.Equip: {
                var data = hud != null && hud.EquipDb != null ? hud.EquipDb.Get(item.RefId) : null;
                return data != null ? data.DisplayName : "장비";
            }
            case RewardItemKind.CharacterShard: {
                var data = hud != null && hud.CharDb != null ? hud.CharDb.Get(item.RefId) : null;
                return data != null ? $"{data.DisplayName} 조각" : "소녀 조각";
            }
            case RewardItemKind.LifetimePass: return "평생 패스";
            case RewardItemKind.EventToken: return EventTokens.Name(item.CurrencyKey);
            default: return "";
        }
    }

    // 코스튬 원본. DB 미할당이거나 아직 등록되지 않은 id 면 null 이라 공용 문구로 떨어진다.
    private static CostumeData CostumeOf(BattleHudContext hud, int costumeId) =>
        costumeId != 0 && hud != null && hud.CostumeDb != null ? hud.CostumeDb.Get(costumeId) : null;

    private static UnitSkinCatalog.Entry UnitSkinOf(BattleHudContext hud, int unitSkinId) =>
        unitSkinId != 0 && hud != null && hud.CostumeDb != null && hud.CostumeDb.UnitSkins != null
            ? hud.CostumeDb.UnitSkins.Get(unitSkinId) : null;

    // 지급이 확정된 각인(id != 0)만 해석한다. 예고 표시(id == 0)나 DB 미할당이면 null 이라 공용 아이콘/문구로 떨어진다.
    private static EngravingData EngravingOf(BattleHudContext hud, int engravingId) =>
        engravingId != 0 && hud != null && hud.EngravingDb != null ? hud.EngravingDb.Get(engravingId) : null;

    private static RewardSlotView.RewardVisual Icon(UnityEngine.Sprite sprite, int amount, string name) =>
        new(sprite, Qty(amount), name);

    private static string Qty(int amount) => amount.ToString("N0");

    private static UnityEngine.Sprite CurrencyKeyIcon(CurrencyCatalog cc, string key) {
        if (cc == null) return null;
        var type = CurrencyKeys.ToType(key);
        return type.HasValue ? cc.Icon(type.Value) : null;
    }

    private static string CurrencyKeyName(CurrencyCatalog cc, string key) {
        if (cc == null) return key ?? "";
        var type = CurrencyKeys.ToType(key);
        return type.HasValue ? cc.DisplayName(type.Value) : (key ?? "");
    }
}
