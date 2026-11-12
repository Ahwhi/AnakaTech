using System;
using System.Collections.Generic;

/// <summary>
/// 패키지/세트 상품의 구성품(클라 표시용 미러). 서버 ShopCatalog 의 각 오퍼 Grants 와 값이 일치해야 한다(지급은 서버 권위).
/// OfferId -> 구성품(즉시 지급 + 선택적 반복 지급) 목록만 담는다. 구매/해금/한도 판정은 서버가 하고, 여기는 카드 표시만 한다.
/// 여기에 등록된 OfferId 는 상점에서 구성품 슬롯을 가진 PackageItemCard 로 표시된다(미등록 오퍼는 일반 카드).
/// 만능조각 등급: N0/R1/SR2/SSR3/XR4. 프로필 테두리 id: 오픈1/병아리2/부자3/회장4/강아지5/고양이6/다이아몬드7/자수정8. 서버와 함께 갱신할 것.
/// </summary>
public static class ShopPackageRewards {
    /// <summary>한 패키지의 구성품. 즉시 지급분과, 선택적으로 반복 지급분(예: 매일 접속 보상)을 그룹으로 나눠 담는다.</summary>
    public sealed class PackageInfo {
        public readonly IReadOnlyList<RewardItem> Grants;      // 즉시 지급 구성품
        public readonly IReadOnlyList<RewardItem> Recurring;   // 반복 지급 구성품(없으면 빈 배열)
        public readonly string RecurringLabel;                 // 반복 지급 그룹 헤더(예: "매일 지급")

        public PackageInfo(RewardItem[] grants, RewardItem[] recurring = null, string recurringLabel = null) {
            Grants = grants ?? Array.Empty<RewardItem>();
            Recurring = recurring ?? Array.Empty<RewardItem>();
            RecurringLabel = recurringLabel;
        }
    }

    private static readonly Dictionary<string, PackageInfo> ByOfferId = new() {
        // 특수 > 스텝업 패키지.
        ["stepup_1"] = new PackageInfo(new[] {
            RewardItem.Shard(2, 50),                        // SR 만능조각 50
            RewardItem.Diamond(3000),
            RewardItem.Item(ItemKeys.ExpCoupon30, 30),      // 경험치 쿠폰 30% 30
        }),
        ["stepup_2"] = new PackageInfo(new[] {
            RewardItem.Shard(3, 10),                        // SSR 만능조각 10
            RewardItem.Currency("special_ticket", 10),
            RewardItem.Diamond(5000),
            RewardItem.Currency(CurrencyKeys.StaminaBoostTicket, 30),
        }),
        ["stepup_3"] = new PackageInfo(new[] {
            RewardItem.Shard(3, 30),                        // SSR 만능조각 30
            RewardItem.Currency("special_ticket", 30),
            RewardItem.Diamond(10000),
            RewardItem.Currency(CurrencyKeys.TrainingTicket, 5),
        }),
        ["stepup_4"] = new PackageInfo(new[] {
            RewardItem.Shard(3, 50),                        // SSR 만능조각 50
            RewardItem.Currency("special_ticket", 50),
            RewardItem.Currency(CurrencyKeys.DispatchCertificate, 5000),
            RewardItem.ProfileFrame(7),                     // 다이아몬드 테두리
        }),

        // 한정 > 랜덤 캐릭터 만능조각(단일 구성품이지만 구성품 슬롯으로 표시). 등급은 랜덤(확률은 '확률 보기').
        ["limited_random_char_shard"] = new PackageInfo(new[] {
            RewardItem.RandomShard(1),
        }),

        // 한정 > 일간 수련 패키지(하루 3회).
        ["limited_daily_training_pack"] = new PackageInfo(new[] {
            RewardItem.Diamond(5000),
            RewardItem.Currency(CurrencyKeys.TrainingTicket, 5),
        }),

        // 한정 > 최적화 세트/패키지.
        ["limited_daily_set"] = new PackageInfo(new[] {
            RewardItem.Stamina(300),
            RewardItem.Currency(CurrencyKeys.StaminaBoostTicket, 30),
            RewardItem.Diamond(3000),
            RewardItem.Shard(3, 10),                        // SSR 만능조각 10
        }),
        ["limited_weekly_burning"] = new PackageInfo(new[] {
            RewardItem.Stamina(500),
            RewardItem.Currency(CurrencyKeys.StaminaBoostTicket, 50),
            RewardItem.Currency(CurrencyKeys.CommissionTicket, 10),
            RewardItem.Currency(CurrencyKeys.StageSweepTicket, 1000),
        }),

        // 한정 > 월간 반복 구매 패키지(주/월 한도 안에서 여러 번 구매).
        ["limited_monthly_special_ticket"] = new PackageInfo(new[] {
            RewardItem.Currency(CurrencyKeys.SpecialTicket, 10),
            RewardItem.Diamond(10000),
        }),
        ["limited_monthly_sr_shard"] = new PackageInfo(new[] {
            RewardItem.Shard(2, 50),                        // SR 만능조각 50
            RewardItem.Diamond(3000),
        }),
        ["limited_monthly_ssr_shard"] = new PackageInfo(new[] {
            RewardItem.Shard(3, 50),                        // SSR 만능조각 50
            RewardItem.Diamond(10000),
        }),
        ["limited_monthly_engraving_stone"] = new PackageInfo(new[] {
            RewardItem.Currency(CurrencyKeys.EngravingStone, 3000),
            RewardItem.Currency(CurrencyKeys.CommissionTicket, 10),
        }),
        ["limited_monthly_renown"] = new PackageInfo(new[] {
            RewardItem.Currency(CurrencyKeys.Renown, 3000),
            RewardItem.Currency(CurrencyKeys.CommissionTicket, 10),
        }),
        ["limited_weekly_set"] = new PackageInfo(new[] {
            RewardItem.Diamond(10000),
            RewardItem.Shard(3, 50),                        // SSR 만능조각 50
            RewardItem.Currency(CurrencyKeys.Renown, 3000),
            RewardItem.Currency(CurrencyKeys.EngravingStone, 3000),
        }),
        ["limited_monthly_set"] = new PackageInfo(new[] {
            RewardItem.Diamond(50000),
            RewardItem.Shard(3, 100),                       // SSR 만능조각 100
            RewardItem.Shard(2, 200),                       // SR 만능조각 200
            RewardItem.Item(ItemKeys.ExpCoupon100, 300),    // 경험치 쿠폰 100% 300
        }),
        ["limited_monthly_limit_break"] = new PackageInfo(new[] {
            RewardItem.Currency(CurrencyKeys.LimitBreakTicket, 3),
            RewardItem.Item(ItemKeys.GrowthResetTicket, 1),
            RewardItem.Shard(3, 100),                       // SSR 만능조각 100
            RewardItem.Shard(2, 200),                       // SR 만능조각 200
        }),
        // 매일 30일 패키지: 구매 즉시분 + 이후 30일간 매일 접속 보상(반복 지급은 서버 후속 업데이트 예정, 표시는 안내용).
        ["limited_daily30_pack"] = new PackageInfo(
            new[] {
                RewardItem.Stamina(5000),
                RewardItem.Currency(CurrencyKeys.StaminaBoostTicket, 500),
                RewardItem.Diamond(5000),
            },
            recurring: new[] {
                RewardItem.Stamina(500),
                RewardItem.Diamond(500),
                RewardItem.Currency(CurrencyKeys.StaminaBoostTicket, 50),
            },
            recurringLabel: "매일 지급"),
        ["limited_starter_pack"] = new PackageInfo(new[] {
            RewardItem.Gold(500000),
            RewardItem.Diamond(50000),
            RewardItem.Currency(CurrencyKeys.SpecialTicket, 50),
            RewardItem.ProfileFrame(2),                     // 병아리 테두리
        }),
        // 평생 패스는 지급 품목이 없다(구매 기록이 곧 패스 보유). 구성품 자리에는 평생 패스 아이콘 하나만 보여 준다.
        [PassRules.LifetimeOfferId] = new PackageInfo(new[] { RewardItem.LifetimePass() }),
    };

    /// <summary>해당 오퍼의 패키지 구성품. 패키지가 아니거나 미등록이면 null.</summary>
    public static PackageInfo Get(string offerId) =>
        !string.IsNullOrEmpty(offerId) && ByOfferId.TryGetValue(offerId, out var info) ? info : null;

    /// <summary>구성품 슬롯을 가진 패키지 카드로 표시할 오퍼인지.</summary>
    public static bool IsPackage(string offerId) => Get(offerId) != null;

    /// <summary>
    /// 화면에 보여 줄 구성품(카드 미리보기, 상세, 구매 확인이 같은 값을 쓴다). 미러에 등록된 상품은 미러 값을,
    /// 아니면 서버가 보낸 지급 목록을 쓴다. 지급 품목이 없는 상품(평생 패스)도 미러로 내용을 보여 줄 수 있다.
    /// </summary>
    public static IReadOnlyList<RewardItem> DisplayGrants(ShopOfferDto offer) {
        if (offer == null) return Array.Empty<RewardItem>();
        var info = Get(offer.OfferId);
        if (info != null && info.Grants.Count > 0) return info.Grants;
        return offer.Grants != null ? offer.Grants : (IReadOnlyList<RewardItem>)Array.Empty<RewardItem>();
    }

    /// <summary>
    /// 서버가 내려준 지급 목록으로 구성품 정보를 만든다(미러에 등록되지 않은 상품용).
    ///
    /// 위의 미러는 상시 상품처럼 구성이 잘 바뀌지 않는 것들을 손으로 적어 둔 표다. 반면 한정 판매처럼
    /// 계정마다 열리고 수시로 늘어나는 상품까지 손으로 옮겨 적으면 서버와 어긋나기 쉽고, 새 상품을
    /// 추가할 때마다 클라도 함께 고쳐야 한다. 그런 상품은 서버가 이미 보내 준 구성(ShopOfferDto.Grants)을
    /// 그대로 쓴다(지급하는 값과 보여 주는 값이 같은 출처가 된다).
    /// </summary>
    public static PackageInfo FromGrants(IReadOnlyList<RewardItem> grants) {
        if (grants == null || grants.Count == 0) return null;
        var array = new RewardItem[grants.Count];
        for (int i = 0; i < grants.Count; i++) array[i] = grants[i];
        return new PackageInfo(array);
    }
}
