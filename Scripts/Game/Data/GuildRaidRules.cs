using System.Collections.Generic;

/// <summary>
/// 길드 레이드의 클라 표시/진행 규칙(서버 GuildCatalog 미러). 판정은 서버 권위이며,
/// 여기에는 전투 씬이 서버 응답을 기다리지 않고 바로 써야 하는 값만 둔다.
/// </summary>
public static class GuildRaidRules {
    /// <summary>1회 도전 제한 시간(초). 서버 GuildCatalog.RaidBattleSeconds 와 같아야 한다.</summary>
    public const int BattleSeconds = 20;

    /// <summary>보스 최고 레벨. 서버 GuildCatalog.RaidMaxBossLevel 과 같아야 한다.</summary>
    public const int MaxBossLevel = 999;

    public const int FreeEntries = 1;

    // 보스 최대 체력(서버 GuildCatalog.RaidBossHpByLevel / RaidBossHpSteps 미러). 전투 중 보스를 잡으면 다음 레벨 보스를
    // 그 자리에서 이어 내보내야 해서(V1.13) 서버 응답 없이 체력을 알아야 한다. 판정과 기록은 여전히 서버가 한다.
    private static readonly long[] BossHpByLevel = { 100_000, 500_000 };

    // 구간별 레벨당 증가폭. FromLevel 부터 다음 구간 전까지 레벨마다 Step 씩 오른다(100레벨부터 1천만).
    private static readonly (int FromLevel, long Step)[] BossHpSteps = {
        (3, 1_000_000),
        (100, 10_000_000),
    };

    /// <summary>보스 레벨의 최대 체력. 서버 GuildCatalog.RaidBossMaxHp 와 같아야 한다.</summary>
    public static long BossMaxHp(int bossLevel) {
        int lv = System.Math.Clamp(bossLevel, 1, MaxBossLevel);
        if (lv <= BossHpByLevel.Length) return BossHpByLevel[lv - 1];

        long hp = 0;
        for (int i = 0; i < BossHpSteps.Length; i++) {
            int from = BossHpSteps[i].FromLevel;
            if (lv < from) break;
            int to = i + 1 < BossHpSteps.Length ? BossHpSteps[i + 1].FromLevel - 1 : lv;
            hp += (long)(System.Math.Min(lv, to) - from + 1) * BossHpSteps[i].Step;
        }
        return hp;
    }

    /// <summary>다른 길드원이 도전 중일 때의 안내(한 번에 한 명만 도전할 수 있다).</summary>
    public static string OccupiedMessage(string nickname) =>
        $"{(string.IsNullOrEmpty(nickname) ? "다른 길드원" : nickname)}님이 레이드에 도전 중입니다.\n도전이 끝난 뒤 참여할 수 있습니다.";

    /// <summary>
    /// 정산 보상에서 길드포인트 외 항목(골드/기력 부스트권)이 더 늘지 않는 레벨. 이후로는 길드포인트만 늘어난다.
    /// 서버 GuildCatalog.RaidRewardGrowthCapLevel 과 같아야 한다.
    /// </summary>
    public const int RewardGrowthCapLevel = 200;

    /// <summary>
    /// 정산 보상에서 길드포인트도 더 늘지 않는 레벨. 이후로는 모든 보상이 고정된다.
    /// 서버 GuildCatalog.RaidGuildPointCapLevel 과 같아야 한다.
    /// </summary>
    public const int GuildPointCapLevel = 500;

    /// <summary>보상 정보 표에 한 번에 보여 줄 레벨 수.</summary>
    public const int RewardPreviewLevels = 10;

    /// <summary>
    /// 보상 정보 표의 첫 레벨. 지금 도전 중인 레벨부터 RewardPreviewLevels 개를 보여 준다(15레벨이면 15~24).
    /// 만렙 근처에서는 표가 만렙을 넘지 않도록 시작을 앞으로 당겨 줄 수를 유지한다(999레벨이면 990~999).
    ///
    /// 만렙까지 전부 그리지 않는 이유: 보상이 레벨에서 바로 정해지므로(DailyRewards) 뒤를 다 그려도 알 수 있는 것이
    /// 늘지 않는 반면, 줄마다 오브젝트를 만드는 비용은 그대로 든다. 지금 도전 중인 구간만 보여 주면 충분하다.
    /// </summary>
    public static int RewardPreviewFirstLevel(int currentLevel) {
        // 표가 만렙에서 끝나는 시작 레벨. 만렙이 표보다 짧으면 언제나 1레벨부터다.
        int latestStart = MaxBossLevel - RewardPreviewLevels + 1;
        if (latestStart < 1) latestStart = 1;

        // 현황을 아직 못 받았으면 currentLevel 이 0 이다. 이때는 처음부터 보여 준다.
        int first = currentLevel < 1 ? 1 : currentLevel;
        return first > latestStart ? latestStart : first;
    }

    /// <summary>보상 정보 표의 마지막 레벨(첫 레벨에서 파생된다).</summary>
    public static int RewardPreviewLastLevel(int currentLevel) {
        int last = RewardPreviewFirstLevel(currentLevel) + RewardPreviewLevels - 1;
        return last > MaxBossLevel ? MaxBossLevel : last;
    }

    /// <summary>
    /// 자정 정산 때 길드원 1명이 받는 보상(보상 정보 표시용). 서버 GuildCatalog.RaidDailyRewards 와
    /// 구성/수치가 일치해야 한다. 실제 지급은 서버가 하고 여기 값은 "무엇을 받게 되는지" 미리 보여 주는 미러다
    /// (도감/충전 보상 예고를 클라 규칙으로 그리는 것과 같은 방식).
    /// </summary>
    public static IReadOnlyList<RewardItem> DailyRewards(int bossLevel) {
        int lv = bossLevel < 1 ? 1 : (bossLevel > MaxBossLevel ? MaxBossLevel : bossLevel);
        int capped = lv > RewardGrowthCapLevel ? RewardGrowthCapLevel : lv;
        int pointLevel = lv > GuildPointCapLevel ? GuildPointCapLevel : lv;
        return new[] {
            RewardItem.Currency(CurrencyKeys.GuildPoint, 100 * pointLevel),
            RewardItem.Gold(10000 * capped),
            RewardItem.Currency(CurrencyKeys.StaminaBoostTicket, capped),
        };
    }
}
