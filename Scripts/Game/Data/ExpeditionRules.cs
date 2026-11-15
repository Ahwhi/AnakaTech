using System.Collections.Generic;

/// <summary>원정대 장소. 값이 패킷에 int 로 실리므로 서버 ExpeditionLocation 과 순서가 같아야 한다.</summary>
public enum ExpeditionLocation {
    CommTower = 0,     // 통신탑
    SupplyDepot = 1,   // 보급창고
    Recruitment = 2,   // 모집사무실
    Vault = 3,         // 금고
    Market = 4         // 장터
}

/// <summary>
/// 원정대의 클라 표시/진행 규칙(서버 ExpeditionCatalog 미러). 판정과 지급은 서버 권위이며,
/// 여기에는 화면과 전투 씬이 서버 응답을 기다리지 않고 바로 써야 하는 값만 둔다.
/// </summary>
public static class ExpeditionRules {
    public const int PartySize = 5;
    public const long MinCombatPower = 100_000;

    /// <summary>매칭/주간 정산 요일(00:00 KST). 서버 ExpeditionCatalog.MatchDay 와 같아야 한다.</summary>
    public const System.DayOfWeek MatchDay = System.DayOfWeek.Wednesday;

    /// <summary>하루 입장 횟수. 서버 ExpeditionCatalog.DailyEntries 와 같아야 한다.</summary>
    public const int DailyEntries = 1;

    /// <summary>장소별 최고 레벨. 서버 ExpeditionCatalog.MaxLevel 과 같아야 한다.</summary>
    public const int MaxLevel = 7;

    /// <summary>클리어 상태의 저장 레벨이자 진행 슬라이더의 최댓값(MaxLevel + 1).</summary>
    public const int ClearedLevel = MaxLevel + 1;

    /// <summary>클리어한 장소의 주간 보상 계산 레벨.</summary>
    public const int ClearedRewardLevel = 10;

    /// <summary>1회 전투 제한 시간(초). 서버 ExpeditionCatalog.BattleSeconds 와 같아야 한다.</summary>
    public const int BattleSeconds = 20;   // V1.14: 30 -> 20초

    public static readonly ExpeditionLocation[] Locations = {
        ExpeditionLocation.CommTower, ExpeditionLocation.SupplyDepot, ExpeditionLocation.Recruitment,
        ExpeditionLocation.Vault, ExpeditionLocation.Market,
    };

    public static bool IsCleared(int level) => level > MaxLevel;

    /// <summary>레벨 표기. 클리어했으면 "클리어", 아니면 "Lv.n".</summary>
    public static string LevelText(int level) => IsCleared(level) ? "클리어" : $"Lv.{level}";

    /// <summary>주간 보상 계산 레벨(서버 RewardLevel 과 같은 규칙).</summary>
    public static int RewardLevel(int level) => IsCleared(level) ? ClearedRewardLevel : (level < 1 ? 1 : level);

    /// <summary>장소를 한 번 쳤을 때 즉시 받는 참여 보상(성패 무관). 서버 AttackRewards 와 일치해야 한다.</summary>
    public static IReadOnlyList<RewardItem> AttackRewards(ExpeditionLocation location) => location switch {
        ExpeditionLocation.CommTower => new[] { RewardItem.Currency(CurrencyKeys.CommissionTicket, 1) },
        ExpeditionLocation.SupplyDepot => new[] {
            RewardItem.Item(ItemKeys.ExpCoupon50, 10),
            RewardItem.Currency(CurrencyKeys.StageSweepTicket, 10),
        },
        ExpeditionLocation.Recruitment => new[] { RewardItem.Currency(CurrencyKeys.SpecialTicket, 1) },
        ExpeditionLocation.Vault => new[] { RewardItem.Gold(100_000), RewardItem.Diamond(100) },
        ExpeditionLocation.Market => new[] { RewardItem.Shard(Grade.SR, 2), RewardItem.Shard(Grade.SSR, 1) },
        _ => System.Array.Empty<RewardItem>()
    };

    /// <summary>주간 보상(레벨 x 기준량, 클리어는 ClearedRewardLevel). 서버 WeeklyRewards 와 일치해야 한다.</summary>
    public static IReadOnlyList<RewardItem> WeeklyRewards(ExpeditionLocation location, int level) {
        int lv = RewardLevel(level);
        return location switch {
            ExpeditionLocation.CommTower => new[] { RewardItem.Currency(CurrencyKeys.CommissionTicket, 5 * lv) },
            ExpeditionLocation.SupplyDepot => new[] {
                RewardItem.Item(ItemKeys.ExpCoupon50, 100 * lv),
                RewardItem.Currency(CurrencyKeys.StageSweepTicket, 100 * lv),
            },
            ExpeditionLocation.Recruitment => new[] { RewardItem.Currency(CurrencyKeys.SpecialTicket, 5 * lv) },
            ExpeditionLocation.Vault => new[] { RewardItem.Gold(1_000_000 * lv), RewardItem.Diamond(1_000 * lv) },
            ExpeditionLocation.Market => new[] {
                RewardItem.Shard(Grade.SR, 10 * lv),
                RewardItem.Shard(Grade.SSR, 5 * lv),
            },
            _ => System.Array.Empty<RewardItem>()
        };
    }

    /// <summary>서버 실패 사유(ExpeditionRepository.Fail) 문구.</summary>
    public static string FailMessage(int failReason, string busyNickname = null) => failReason switch {
        1 => "로그인 정보가 없습니다.",
        2 => "이번 주 원정대에 매칭되지 않았습니다.",
        3 => "오늘은 이미 원정대에 참여했습니다.",
        4 => string.IsNullOrEmpty(busyNickname) ? "다른 파티원이 공략 중입니다." : $"{busyNickname} 님이 공략 중입니다.",
        5 => "이미 클리어한 장소입니다.",
        6 => "알 수 없는 장소입니다.",
        7 => "입장 기록이 없습니다.",
        _ => "원정대 처리에 실패했습니다."
    };
}
