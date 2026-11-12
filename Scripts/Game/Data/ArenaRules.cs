using System.Collections.Generic;

/// <summary>
/// 쟁탈전의 클라 표시 규칙(서버 ArenaCatalog 미러). 판정과 지급은 서버 권위이며,
/// 여기에는 응답을 기다리지 않고 바로 그려야 하는 값과 화면 문구만 둔다.
/// </summary>
public static class ArenaRules {
    /// <summary>쟁탈전 상점 상품의 대분류 키(서버 ShopCatalog 의 Major 와 일치해야 한다).</summary>
    public const string ShopMajor = "arena";

    /// <summary>하루 무료 매칭 횟수. 서버 ArenaCatalog.FreeEntriesPerDay 와 같아야 한다.</summary>
    public const int FreeEntriesPerDay = 3;

    /// <summary>
    /// 매칭이 잡힌 뒤 전투가 자동으로 시작되기까지의 카운트다운(초).
    ///
    /// 상대를 확인할 시간은 주되 물릴 수는 없게 하는 값이다. 매칭 시점에 서버가 이미 횟수를 깎고
    /// "진행 중"으로 표시하므로, 여기서 빠져나가도 그 판은 패배로 굳는다(도망칠 이유가 없다).
    /// </summary>
    public const int MatchCountdownSeconds = 5;

    /// <summary>승패 코인. 서버 ArenaCatalog 와 같아야 한다(보상 정보 표시용).</summary>
    public const int WinCoin = 100;
    public const int LoseCoin = 50;

    /// <summary>일일 보상 = 그날 매칭 횟수 x 이 값(코인).</summary>
    public const int DailyCoinPerEntry = 100;

    /// <summary>점수 변동의 하한/상한(절대값).</summary>
    public const int MinScoreDelta = 1;
    public const int MaxScoreDelta = 100;

    /// <summary>
    /// 전투 결과를 보고할 때 실어 보내는 체력 값(백분율 x 10). 소수를 패킷에 싣지 않기 위한 단위이며,
    /// 반올림과 1~100 자르기는 서버가 한다(점수 규칙이 두 곳에 생기지 않게).
    /// </summary>
    public static int ToHpPercentTenth(float hpRatio01) {
        int tenth = UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Clamp01(hpRatio01) * 1000f);
        return tenth;
    }

    // ── 표시 ────────────────────────────────────────
    /// <summary>승률 색(밝은 배경용 짙은 회색).</summary>
    public const string RateColorDark = "#464646";
    /// <summary>승률 색(어두운 배경용 밝은 회색).</summary>
    public const string RateColorLight = "#BEBEBE";

    /// <summary>"12전 8승 4패 (66.7%)" 형태의 전적 문구. 승률은 rateColor 로 칠한다(기본 짙은 회색).</summary>
    public static string RecordText(int wins, int losses, string rateColor = RateColorDark) {
        int total = wins + losses;
        float rate = total > 0 ? (float)wins / total * 100f : 0f;
        return $"{total}전 {wins}승 {losses}패 <color={rateColor}>({rate:0.0}%)</color>";
    }

    /// <summary>"2 / 3" 형태의 남은 무료 매칭 횟수 문구.</summary>
    public static string FreeEntryText(int usedEntries) {
        int left = FreeEntriesPerDay - usedEntries;
        if (left < 0) left = 0;
        return $"{left} / {FreeEntriesPerDay}";
    }

    /// <summary>오늘 무료 매칭이 남아 있는지(배지 근거).</summary>
    public static bool HasFreeEntry(int usedEntries) => usedEntries < FreeEntriesPerDay;

    public static string ScoreText(int score) => $"{score:N0}";

    /// <summary>점수 변동 문구("+54" / "-77"). 0 이면 변동 없음.</summary>
    public static string ScoreDeltaText(int delta) => delta > 0 ? $"+{delta}" : delta.ToString();

    // ── 보상 정보(공용 보상 정보 팝업에 넘길 표) ─────────
    /// <summary>주간 순위 보상 구간(서버 ArenaCatalog.WeeklyRankTable 미러). 순위 상한과 코인.</summary>
    private static readonly (int MaxRank, int Coin)[] WeeklyRankCoins = {
        (1, 5000),
        (3, 4000),
        (10, 3000),
        (100, 2000),
        (500, 1000),
    };

    /// <summary>순위권 밖(또는 501위 이하)이 받는 주간 보상.</summary>
    public const int WeeklyCoinDefault = 500;

    /// <summary>
    /// 보상 정보 팝업에 그릴 줄 목록. 승패 즉시 보상 / 일일 보상 / 주간 순위 보상을 한 표로 보여 준다.
    /// 표를 만드는 규칙이 여기 있고 그리는 것은 공용 팝업이라, 화면은 이 목록만 받아 가면 된다.
    /// </summary>
    public static List<(string Label, IReadOnlyList<RewardItem> Rewards)> RewardTable() {
        var rows = new List<(string, IReadOnlyList<RewardItem>)> {
            ("승리", Coins(WinCoin)),
            ("패배", Coins(LoseCoin)),
            ("일일 (1회 참여당)", Coins(DailyCoinPerEntry)),
        };

        int previous = 0;
        foreach (var (maxRank, coin) in WeeklyRankCoins) {
            string label = maxRank == previous + 1 ? $"주간 {maxRank}위" : $"주간 {previous + 1}~{maxRank}위";
            // 10위까지는 순위 테두리(다음 주간 보상까지 유효)를 함께 받는다. 구간 경계가 코인 구간과 같다.
            int frame = WeeklyRankFrame(maxRank);
            rows.Add((label, frame != 0
                ? new[] { RewardItem.Currency(CurrencyKeys.ArenaCoin, coin), RewardItem.ProfileFrame(frame) }
                : Coins(coin)));
            previous = maxRank;
        }
        rows.Add(($"주간 {previous + 1}위 이하", Coins(WeeklyCoinDefault)));
        return rows;
    }

    /// <summary>주간 순위 테두리 id(서버 ArenaCatalog.WeeklyRankFrame 미러). 0 = 없음.</summary>
    public static int WeeklyRankFrame(int rank) => rank switch {
        1 => 25,
        >= 2 and <= 3 => 26,
        >= 4 and <= 10 => 27,
        _ => 0
    };

    // ── 역대 전적 ────────────────────────────────────
    /// <summary>시즌 키("2026-11") -> "2026년 11월". 형식이 다르면 원문 그대로.</summary>
    public static string SeasonName(string seasonKey) {
        if (string.IsNullOrEmpty(seasonKey) || seasonKey.Length < 7) return seasonKey ?? "";
        return int.TryParse(seasonKey.Substring(0, 4), out int y) && int.TryParse(seasonKey.Substring(5, 2), out int m)
            ? $"{y}년 {m}월" : seasonKey;
    }

    private static IReadOnlyList<RewardItem> Coins(int amount) =>
        new[] { RewardItem.Currency(CurrencyKeys.ArenaCoin, amount) };

    // ── 실패 안내 ────────────────────────────────────
    /// <summary>서버 GameDataRepository.ArenaFail 값 -> 안내 문구. 값이 서버와 일치해야 한다.</summary>
    public static string FailMessage(int failReason) => failReason switch {
        1 => "로그인이 필요합니다.",
        2 => "오늘 매칭 횟수를 모두 사용했습니다.",
        3 => "다이아가 부족합니다.",
        4 => "맞설 상대를 찾지 못했습니다.",
        5 => "편성된 소녀가 없습니다.",
        6 => "잘못된 상대입니다.",
        _ => "쟁탈전 처리에 실패했습니다."
    };

    /// <summary>매칭 확인 문구(유료 매칭에서만 묻는다).</summary>
    public static string PaidMatchConfirmMessage(int cost) =>
        $"다이아 {cost:N0}을(를) 사용해 추가로 매칭하시겠습니까?";
}
