/// <summary>
/// 보상 1개의 구조적 표현(표시 전용). 도감 보상/충전 보상 등 여러 보상 UI가 공통으로 사용한다.
/// 실제 지급은 서버 권위이며 이 값은 아이콘/수량 표시를 위한 미러다(서버 카탈로그와 값이 일치해야 한다).
/// 새 품목이 생기면 Kind 를 추가하고 RewardVisualResolver 에 해석을 한 줄 더하면 된다.
/// </summary>
public enum RewardItemKind {
    Gold,
    Diamond,
    Stamina,
    Currency,        // 티켓 등 범용 재화(CurrencyKey 로 구분)
    UniversalShard,       // 만능조각(Grade 별). 아이콘 대신 조각 프리팹으로 표시.
    RandomUniversalShard, // 랜덤 등급 만능조각(등급 미정). 전용 아이콘(ShopIconCatalog 키)으로 표시.
    ProfileFrame,         // 프로필 테두리(RefId = 테두리 id)
    RandomEquip,          // 랜덤 장비(등급 지정 가능, RefId 미사용)
    Exp,                  // 캐릭터 경험치(재화가 아니라 지갑에 없으므로 아이콘은 ShopIconCatalog 키로 해석)
    Character,            // 캐릭터 직접 지급(RefId = 캐릭터 id). 서버 보상 목록을 그대로 표시할 때 사용.
    RandomEngraving,      // 랜덤 각인 도면(미보유 중 1종). 예고 시점엔 대상 미정이라 전용 아이콘, 지급 후엔 RefId = 각인 id.
    Item,                 // 소모품 아이템(RefId = 아이템 id, Amount = 개수). 상점/우편/이벤트/의뢰가 공유한다.
    RandomGeneralEquip,   // 랜덤 공용부위 장비(방어구/신발/악세서리, 전용무기 제외). RandomEquip 과 나오는 것이 달라 따로 둔다.
    Costume,              // 코스튬(RefId = 코스튬 id). 단품이라 수량 개념이 없다.
    Equip,                // 대상을 지정한 장비(RefId = 장비 id). 랜덤 장비와 달리 무엇이 오는지 정해져 있다.
    CharacterShard        // 특정 소녀의 조각(RefId = 캐릭터 id, Amount = 개수). 만능조각과 달리 대상이 정해져 있다.
}

public readonly struct RewardItem {
    public readonly RewardItemKind Kind;
    public readonly int Amount;        // 수량(재화/조각 등). 단품(테두리 등)은 1.
    public readonly int Grade;         // UniversalShard/RandomEquip 등급(0=N..4=XR)
    public readonly string CurrencyKey;// Currency 종류 키(normal_ticket 등)
    public readonly int RefId;         // ProfileFrame id 등 참조 id

    private RewardItem(RewardItemKind kind, int amount, int grade, string currencyKey, int refId) {
        Kind = kind; Amount = amount; Grade = grade; CurrencyKey = currencyKey; RefId = refId;
    }

    public static RewardItem Gold(int amount) => new(RewardItemKind.Gold, amount, 0, null, 0);
    public static RewardItem Diamond(int amount) => new(RewardItemKind.Diamond, amount, 0, null, 0);
    public static RewardItem Stamina(int amount) => new(RewardItemKind.Stamina, amount, 0, null, 0);
    public static RewardItem Currency(string key, int amount) => new(RewardItemKind.Currency, amount, 0, key, 0);
    public static RewardItem Shard(int grade, int amount) => new(RewardItemKind.UniversalShard, amount, grade, null, 0);
    public static RewardItem Shard(Grade grade, int amount) => Shard((int)grade, amount);
    public static RewardItem RandomShard(int amount = 1) => new(RewardItemKind.RandomUniversalShard, amount, 0, null, 0);
    public static RewardItem ProfileFrame(int id) => new(RewardItemKind.ProfileFrame, 1, 0, null, id);
    public static RewardItem RandomEquip(int grade, int amount = 1) => new(RewardItemKind.RandomEquip, amount, grade, null, 0);
    /// <summary>방어구/신발/악세서리에서만 나오는 랜덤 장비(전용무기 제외).</summary>
    public static RewardItem RandomGeneralEquip(int grade, int amount = 1) => new(RewardItemKind.RandomGeneralEquip, amount, grade, null, 0);
    public static RewardItem Exp(int amount) => new(RewardItemKind.Exp, amount, 0, null, 0);
    /// <param name="engravingId">지급이 확정된 각인 id(0=아직 대상 미정인 예고 표시).</param>
    public static RewardItem RandomEngraving(int engravingId = 0) => new(RewardItemKind.RandomEngraving, 1, 0, null, engravingId);
    public static RewardItem Item(int itemId, int amount = 1) => new(RewardItemKind.Item, amount, 0, null, itemId);
    /// <param name="costumeId">지급되는 코스튬 id. 단품이라 수량은 항상 1이다.</param>
    public static RewardItem Costume(int costumeId) => new(RewardItemKind.Costume, 1, 0, null, costumeId);
    /// <param name="equipId">지급이 확정된 장비 id(랜덤 장비와 달리 무엇이 오는지 정해져 있다).</param>
    public static RewardItem Equip(int equipId, int amount = 1) => new(RewardItemKind.Equip, amount, 0, null, equipId);
    /// <param name="charDataId">조각의 주인 소녀 id.</param>
    public static RewardItem CharacterShard(int charDataId, int amount) => new(RewardItemKind.CharacterShard, amount, 0, null, charDataId);

    /// <summary>
    /// 수량 배수를 적용한 사본(상점 수량 선택, 반복 지급 일수 등 같은 보상을 여러 번 받는 표시에 사용).
    /// 보유 여부만 갱신되는 단품(프로필 테두리/각인 도면)은 서버도 배수를 곱하지 않으므로 그대로 둔다.
    /// </summary>
    public RewardItem Multiplied(int multiplier) {
        if (multiplier <= 1) return this;
        if (Kind == RewardItemKind.ProfileFrame || Kind == RewardItemKind.RandomEngraving
            || Kind == RewardItemKind.Costume) return this;
        return new RewardItem(Kind, Amount * multiplier, Grade, CurrencyKey, RefId);
    }

    /// <summary>
    /// 서버가 내려준 지급 항목(kind/value/qty/currencyKey)을 표시용 RewardItem 으로 옮긴다.
    /// kind 는 서버 Room.ToRewardKind 와 짝을 이룬다:
    ///   0=골드 1=다이아 2=기력 3=캐릭터 4=범용재화 5=만능조각 6=장비 7=프로필테두리 8=각인도면 9=소모품
    ///   10=랜덤등급 만능조각 11=랜덤 공용부위 장비 12=코스튬 13=캐릭터 경험치 14=지정 장비 15=캐릭터 조각
    /// 수량 규약도 서버와 같다: 지갑/재화형은 value 가 금액(qty 는 배수), 개수형은 value 가 대상(등급/id)이고 qty 가 개수다.
    /// 랜덤 장비(6)는 value 가 등급이고, 지정 장비(14)는 value 가 장비 id 다(각성 재료처럼 대상이 정해진 보상).
    /// 각인도면(8)은 value 에 서버가 실제로 뽑은 각인 id 가 실려 온다.
    /// </summary>
    public static RewardItem FromGrant(int kind, int value, int qty, string currencyKey) {
        int count = qty < 1 ? 1 : qty;
        return kind switch {
            0 => Gold(value * count),
            1 => Diamond(value * count),
            2 => Stamina(value * count),
            3 => new RewardItem(RewardItemKind.Character, count, 0, null, value),
            4 => Currency(currencyKey, value * count),
            5 => Shard(value, count),
            6 => RandomEquip(value, count),
            7 => ProfileFrame(value),
            8 => RandomEngraving(value),
            9 => Item(value, count),
            10 => RandomShard(count),
            11 => RandomGeneralEquip(value, count),
            12 => Costume(value),
            13 => Exp(value * count),
            14 => Equip(value, count),
            15 => CharacterShard(value, count),
            _ => Gold(value * count)
        };
    }
}
