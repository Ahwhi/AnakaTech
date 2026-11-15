using System.Collections.Generic;

/// <summary>
/// 길드 대항전의 클라 표시 규칙(서버 GuildCatalog 미러). 판정은 서버 권위이며,
/// 여기에는 응답을 기다리지 않고 바로 표시해야 하는 값과 실패 문구만 둔다.
/// </summary>
public static class GuildWarRules {
    /// <summary>1인당 공격 가능 횟수. 서버 GuildCatalog.WarAttacksPerMember 와 같아야 한다.</summary>
    public const int AttacksPerMember = 3;

    /// <summary>주간 매칭 요일. 서버 GuildCatalog.WarMatchDay 와 같아야 한다.</summary>
    public const System.DayOfWeek MatchDay = System.DayOfWeek.Saturday;
    /// <summary>주간 매칭 시각(KST). 서버 GuildCatalog.WarMatchHour 와 같아야 한다.</summary>
    public const int MatchHour = 17;

    /// <summary>
    /// 경기가 끝난 뒤 길드원 1명이 받는 보상(보상 정보 표시용). 서버 GuildCatalog.WarWinRewards /
    /// WarLoseRewards 와 구성/수치가 일치해야 한다. 지급은 서버 권위이며 여기 값은 예고용 미러다.
    /// </summary>
    public static IReadOnlyList<RewardItem> WinRewards => new[] {
        RewardItem.Currency(CurrencyKeys.GuildPoint, 10000),
        RewardItem.Diamond(1000),
        RewardItem.Currency(CurrencyKeys.DispatchCertificate, 1000),
    };

    public static IReadOnlyList<RewardItem> LoseRewards => new[] {
        RewardItem.Currency(CurrencyKeys.GuildPoint, 5000),
        RewardItem.Diamond(500),
        RewardItem.Currency(CurrencyKeys.DispatchCertificate, 500),
    };

    /// <summary>상대가 없을 때 보여 줄 안내 문구.</summary>
    public static string NoMatchMessage(GuildWarNoMatchReason reason) => reason switch {
        GuildWarNoMatchReason.Matching => "상대를 찾고 있습니다.",
        GuildWarNoMatchReason.Skipped  => "이번 주는 맞설 길드를 찾지 못했습니다.",
        _                              => "아직 대항전 상대가 정해지지 않았습니다."
    };

    /// <summary>서버 GuildRepository.WarFail 값 -> 안내 문구. 그 외 코드는 길드 공통 문구로 넘긴다.</summary>
    public static string FailMessage(int failReason) => failReason switch {
        40 => "진행 중인 대항전이 없습니다.",
        41 => "지금은 경기 시간이 아닙니다.",
        42 => "공격 횟수를 모두 사용했습니다.",
        // 43 은 비어 있다(예전 "쓰러진 상태에서는 공격 불가". 격파당해도 공격할 수 있게 바뀌었다).
        44 => "이미 격파된 상대입니다.",
        45 => "공격할 수 없는 대상입니다.",
        46 => "이미 승부가 결정되었습니다.",
        _ => GuildRules.FailMessage(failReason)
    };
}
