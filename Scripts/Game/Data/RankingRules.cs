/// <summary>
/// 랭킹 표시 규칙(클라 전용). 순위 산정은 서버 스냅샷이 하므로 여기에는 화면에 쓰는 문구만 둔다.
/// 순위 표기가 랭킹 팝업과 프로필 팝업 양쪽에 나오므로 한 곳에서 정한다.
/// </summary>
public static class RankingRules {
    /// <summary>순위권 밖(집계에 없거나 아직 집계 전)일 때 쓰는 문구.</summary>
    public const string NoRankText = "순위 없음";

    public static string CategoryName(RankingCategory category) => category switch {
        RankingCategory.GuildCombatPower => "길드 랭킹",
        RankingCategory.Achievement => "업적 랭킹",
        RankingCategory.ArenaScore => "쟁탈전 랭킹",
        RankingCategory.RepresentativePower => "대표 소녀 랭킹",
        RankingCategory.DoomScore => "멸망전 랭킹",
        _ => "유저 랭킹"
    };

    /// <summary>"3위" 형태. 0 이면 순위권 밖이라 안내 문구가 나간다.</summary>
    public static string RankText(int rank) => rank > 0 ? $"{rank:N0}위" : NoRankText;

    /// <summary>"3" 형태(숫자만). 순위 열이 따로 있는 목록에서 쓴다. 0 이면 자리만 채운다.</summary>
    public static string RankNumberText(int rank) => rank > 0 ? rank.ToString("N0") : "-";
}
