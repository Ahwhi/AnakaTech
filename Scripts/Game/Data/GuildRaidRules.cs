using System.Collections.Generic;

/// <summary>
/// 길드 레이드의 클라 표시/진행 규칙(서버 GuildCatalog 미러). 판정은 서버 권위이며,
/// 여기에는 전투 씬이 서버 응답을 기다리지 않고 바로 써야 하는 값만 둔다.
/// </summary>
public static class GuildRaidRules {
    /// <summary>1회 도전 제한 시간(초). 서버 GuildCatalog.RaidBattleSeconds 와 같아야 한다.</summary>
    public const int BattleSeconds = 30;

    public const int MaxBossLevel = 99;
    public const int BossHpPerLevel = 100000;
    public const int FreeEntries = 1;

    /// <summary>보스 최대 체력(표시용 계산). 실제 값은 서버가 내려준다.</summary>
    public static long BossMaxHp(int bossLevel) {
        int lv = bossLevel < 1 ? 1 : (bossLevel > MaxBossLevel ? MaxBossLevel : bossLevel);
        return (long)lv * BossHpPerLevel;
    }

    /// <summary>
    /// 자정 정산 때 길드원 1명이 받는 보상(보상 정보 표시용). 서버 GuildCatalog.RaidDailyRewards 와
    /// 구성/수치가 일치해야 한다. 실제 지급은 서버가 하고 여기 값은 "무엇을 받게 되는지" 미리 보여 주는 미러다
    /// (도감/충전 보상 예고를 클라 규칙으로 그리는 것과 같은 방식).
    /// </summary>
    public static IReadOnlyList<RewardItem> DailyRewards(int bossLevel) {
        int lv = bossLevel < 1 ? 1 : (bossLevel > MaxBossLevel ? MaxBossLevel : bossLevel);
        return new[] {
            RewardItem.Currency(CurrencyKeys.GuildPoint, 100 * lv),
            RewardItem.Gold(10000 * lv),
            RewardItem.Currency(CurrencyKeys.StaminaBoostTicket, lv),
        };
    }
}
