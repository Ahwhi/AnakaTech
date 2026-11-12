using System.Collections.Generic;

/// <summary>
/// 누적 충전 보상 티어(클라 표시용 미러). 서버 ChargeRewardCatalog 와 임계치/보상이 일치해야 한다(수령 판정은 서버 권위).
/// 임계치 + 보상 구성(RewardItem)만 담는다. 티어 순서(1-based)는 서버와 동일해야 한다.
/// 프로필 테두리 id 규약: 오픈1/병아리2/부자3/회장4/강아지5/고양이6/다이아몬드7/자수정8. 만능조각 등급: N0/R1/SR2/SSR3/XR4.
///
/// 티어 순서(1-based)가 곧 서버 수령 기록의 키다. 중간에 티어를 끼워 넣으면 뒤 인덱스가 밀리므로
/// 서버 ChargeRewardCatalog 와 반드시 같은 순서로 유지해야 한다.
/// </summary>
public static class ChargeRewardRules {
    public struct TierInfo {
        public int Threshold;            // 누적 충전액(원)
        public RewardItem[] Rewards;     // 보상 구성(복합 가능)
        public TierInfo(int threshold, params RewardItem[] rewards) { Threshold = threshold; Rewards = rewards; }
    }

    public static readonly List<TierInfo> Tiers = new() {
        new TierInfo(1200, RewardItem.Diamond(10000)),
        new TierInfo(3000, RewardItem.Gold(100000)),
        new TierInfo(5000, RewardItem.Currency(CurrencyKeys.DispatchCertificate, 1000)),
        new TierInfo(10000, RewardItem.Shard(2, 50)),                 // SR 만능조각 50
        new TierInfo(20000, RewardItem.Item(ItemKeys.ExpCoupon30, 100)),  // 경험치 쿠폰 30% 100
        new TierInfo(30000, RewardItem.Shard(3, 50)),                 // SSR 만능조각 50
        new TierInfo(50000, RewardItem.ProfileFrame(3),               // 부자 테두리
                            RewardItem.Currency(CurrencyKeys.GachaMileage, 5000)),
        new TierInfo(80000, RewardItem.Currency("special_ticket", 10)),
        new TierInfo(100000, RewardItem.Shard(2, 100),                // SR 만능조각 100
                             RewardItem.Currency(CurrencyKeys.GachaMileage, 5000)),
        new TierInfo(150000, RewardItem.Currency("special_ticket", 30)),
        new TierInfo(200000, RewardItem.Diamond(30000),
                             RewardItem.Currency(CurrencyKeys.GachaMileage, 5000)),
        new TierInfo(250000, RewardItem.Item(ItemKeys.ExpCoupon50, 100)), // 경험치 쿠폰 50% 100
        new TierInfo(300000, RewardItem.Shard(3, 100)),               // SSR 만능조각 100
        new TierInfo(500000, RewardItem.Diamond(50000),
                             RewardItem.Currency(CurrencyKeys.GachaMileage, 10000)),
        new TierInfo(800000, RewardItem.Shard(2, 200)),               // SR 만능조각 200
        new TierInfo(1000000, RewardItem.Currency("special_ticket", 50), RewardItem.ProfileFrame(8)), // 특수 모집권 50 + 자수정 테두리
        new TierInfo(1500000, RewardItem.Diamond(100000),
                              RewardItem.Currency(CurrencyKeys.GachaMileage, 20000)),
        new TierInfo(2000000, RewardItem.Currency("special_ticket", 100),
                              RewardItem.Currency(CurrencyKeys.GachaMileage, 30000)),
        new TierInfo(2500000, RewardItem.Shard(3, 100), RewardItem.Diamond(200000)), // SSR 만능조각 100 + 다이아 200,000
        new TierInfo(3000000, RewardItem.Currency("special_ticket", 150),
                              RewardItem.Currency(CurrencyKeys.GachaMileage, 40000)),
        new TierInfo(3500000, RewardItem.Shard(3, 200)),              // SSR 만능조각 200
        new TierInfo(4000000, RewardItem.Currency("special_ticket", 200), RewardItem.Diamond(300000)),
        new TierInfo(4500000, RewardItem.Shard(3, 300),               // SSR 만능조각 300
                              RewardItem.Currency(CurrencyKeys.GachaMileage, 50000)),
        new TierInfo(5000000, RewardItem.Currency("special_ticket", 300), RewardItem.ProfileFrame(4)), // 특수 모집권 300 + 회장 테두리
    };
}
