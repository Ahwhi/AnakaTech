using System.Collections.Generic;

/// <summary>
/// 패스 규칙(표시용 미러). 실제 레벨 계산과 지급은 전부 서버 Server.Data.PassCatalog 가 확정하며,
/// 여기는 보상 트랙과 탭을 그리기 위한 사본이다. 두 표의 수치와 보상이 같아야 한다.
///
/// 레벨은 칸(slot) 번호로 센다. 0 번 칸은 패스 시작 보상(기력 없이 받는다)이고, n 번 칸은 기력을
/// StaminaPerLevel * n 만큼 써야 열린다. 한 사이클(탭 하나)은 LevelsPerCycle 레벨이다.
///   사이클 1 = 0 ~ 50(51칸), 사이클 2 = 51 ~ 100(50칸), 사이클 3 = 101 ~ 150 ...
/// 수령 기록은 줄마다 "앞에서부터 몇 칸을 받았는가"(claimedCount) 하나다.
/// </summary>
public static class PassRules {
    public const int StaminaPerLevel = 500;
    public const int LevelsPerCycle = 50;
    public const int MaxCycles = 200;   // 서버 PassCatalog.MaxCycles 와 같아야 한다(V1.12: 20 -> 200)
    public const int MaxLevel = LevelsPerCycle * MaxCycles;

    /// <summary>평생 패스 상품 id. 서버 PassCatalog.LifetimeOfferId 와 같아야 한다.</summary>
    public const string LifetimeOfferId = "limited_lifetime_pass";

    /// <summary>평생 패스가 놓인 상점 위치(대분류/중분류). 서버 ShopCatalog 의 평생 패스 Major/Sub 와 같아야 한다.</summary>
    public const string ShopMajor = "limited";
    public const string ShopSub = "account";

    // 장비 보상(EquipmentData id).
    private const int SsrArmor = 2004;
    private const int SsrShoes = 3004;
    private const int SsrAccessory = 4004;

    /// <summary>한 칸(레벨)의 보상. 무료 줄과 패스 줄이 같은 표를 쓴다.</summary>
    public static RewardItem RewardOf(int level) {
        if (level <= 0) return RewardItem.Diamond(10000);

        int step = (level - 1) % LevelsPerCycle + 1;
        return step switch {
            5 => RewardItem.Item(ItemKeys.ExpExtractTicket, 1),
            10 => RewardItem.Item(ItemKeys.ExpCoupon50, 500),
            15 => RewardItem.Currency(CurrencyKeys.StaminaBoostTicket, 100),
            20 => RewardItem.Currency(CurrencyKeys.LimitBreakTicket, 1),
            25 => RewardItem.Equip(SsrArmor),
            30 => RewardItem.Shard(Grade.SSR, 50),
            35 => RewardItem.Equip(SsrAccessory),
            40 => RewardItem.Currency(CurrencyKeys.SpecialTicket, 50),
            45 => RewardItem.Equip(SsrShoes),
            50 => RewardItem.Currency(CurrencyKeys.GachaMileage, 10000),
            _ => RewardItem.Diamond(1000),
        };
    }

    /// <summary>쓴 기력으로 도달한 레벨(0 ~ MaxLevel).</summary>
    public static int LevelOf(long staminaSpent) {
        long level = staminaSpent / StaminaPerLevel;
        return level < 0 ? 0 : level > MaxLevel ? MaxLevel : (int)level;
    }

    /// <summary>탭(사이클, 1부터)의 첫 칸. 사이클 1 만 시작 보상(0 번 칸)을 포함한다.</summary>
    public static int FirstLevelOf(int cycle) => cycle <= 1 ? 0 : (cycle - 1) * LevelsPerCycle + 1;

    /// <summary>탭(사이클, 1부터)의 마지막 칸.</summary>
    public static int LastLevelOf(int cycle) => cycle * LevelsPerCycle;

    /// <summary>
    /// 지금 보여 줄 탭 수. 한 사이클을 다 채우면(50, 100, ...) 다음 탭이 열린다. 상한은 MaxCycles.
    /// </summary>
    public static int VisibleCycles(int level) {
        int cycles = level / LevelsPerCycle + 1;
        return cycles > MaxCycles ? MaxCycles : cycles;
    }

    /// <summary>그 칸을 받을 수 있는 상태인지(도달했고 아직 안 받음).</summary>
    public static bool IsClaimable(int slot, int level, int claimedCount) => slot <= level && slot >= claimedCount;

    /// <summary>그 탭에 받을 수 있는 칸이 하나라도 있는지(탭 배지).</summary>
    public static bool CycleHasClaimable(int cycle, PassInfoDto info) {
        if (info == null) return false;
        int first = FirstLevelOf(cycle);
        int last = System.Math.Min(LastLevelOf(cycle), info.Level);
        if (first > last) return false;

        // 받은 칸 수는 앞에서부터 이어지므로, 이 탭의 마지막 도달 칸이 아직 안 받은 칸이면 탭 안에 받을 것이 있다.
        if (last >= info.FreeClaimedCount) return true;
        return info.HasPass && last >= info.PassClaimedCount;
    }

    /// <summary>
    /// 평생 패스를 사면 지금 바로 더 받을 수 있는 보상(패스 줄의 0 ~ level 칸). 같은 종류는 하나로 합친다.
    /// 예) 레벨 15 : 다이아 22,000 + 경험치 추출권 1 + 경험치 쿠폰 50% 500 + 기력 부스트권 100.
    /// </summary>
    public static List<RewardItem> PassRowRewardsUpTo(int level) {
        var merged = new List<RewardItem>();
        for (int slot = 0; slot <= level; slot++) {
            var r = RewardOf(slot);
            int i = merged.FindIndex(m => m.Kind == r.Kind && m.Grade == r.Grade
                                          && m.RefId == r.RefId && m.CurrencyKey == r.CurrencyKey);
            if (i < 0) merged.Add(r);
            else merged[i] = merged[i].WithAmount(merged[i].Amount + r.Amount);
        }
        return merged;
    }
}
