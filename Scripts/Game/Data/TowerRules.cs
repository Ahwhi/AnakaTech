using System.Collections.Generic;

/// <summary>
/// 무한 타워 규칙의 클라 미러. 층/보상/하루 횟수의 실제 판정은 서버(Server.Data.TowerCatalog)가 하며,
/// 여기 값은 화면 표시와 전투 진행(합류 시간)에 쓴다. 두 곳의 값이 같아야 한다.
///
/// 전투는 누적 합류식이다(V1.12). 1번 소녀가 혼자 시작하고 JoinSeconds 마다 다음 순번 소녀가 같은 전장에 합류한다.
/// 먼저 선 소녀는 빠지지 않는다. 선 소녀가 전부 쓰러지면 남은 소녀가 시간을 기다리지 않고 바로 합류한다.
/// 전원이 합류한 뒤 모두 쓰러지면 패배, BattleSeconds 를 버티면 등반 성공이다.
/// </summary>
public static class TowerRules {
    public const int MaxFloor = 9999;

    /// <summary>하루에 오를 수 있는 층 수.</summary>
    public const int DailyClimbs = 1;

    /// <summary>한 판의 전투 시간(초). 서버 TowerCatalog.BattleSeconds 와 같아야 한다.</summary>
    public const float BattleSeconds = 50f;

    /// <summary>다음 소녀가 합류하기까지의 시간(초).</summary>
    public const float JoinSeconds = 10f;

    /// <summary>편성 인원(합류 순번 수). 덱 편성과 같은 5칸이다.</summary>
    public const int PartySize = DeckEditSession.Capacity;

    /// <summary>층을 오를 때마다 받는 스테이지 소탕권 = 깬 층 x 이 값.</summary>
    public const int SweepTicketPerFloor = 100;

    /// <summary>이 층 단위마다(10, 20, ...) 다이아를 더 준다.</summary>
    public const int MilestoneFloors = 10;
    public const int MilestoneDiamond = 1000;

    /// <summary>그 층을 깼을 때의 보상(표시용). 서버 TowerCatalog.RewardsFor 와 같아야 한다.</summary>
    public static List<RewardItem> RewardsFor(int floor) {
        var list = new List<RewardItem> {
            RewardItem.Currency(CurrencyKeys.StageSweepTicket, SweepTicketPerFloor * System.Math.Max(1, floor)),
        };
        if (floor > 0 && floor % MilestoneFloors == 0) list.Add(RewardItem.Diamond(MilestoneDiamond));
        return list;
    }

    public static string FloorText(int floor) => $"{floor}층";

    /// <summary>다음 합류까지 남은 시간 표기. 합류할 소녀가 없으면 버텨야 할 남은 시간만 쓴다. 예: "7초후 합류".</summary>
    public static string RelayText(int seconds, bool lastRunner) => lastRunner ? $"{seconds}초" : $"{seconds}초후 합류";

    /// <summary>오늘 오를 수 있는 층 수 표기(라벨은 프리팹에서 따로 둔다). 예: "1/1 층".</summary>
    public static string ClimbCountText(bool canClimb) => $"{(canClimb ? DailyClimbs : 0)}/{DailyClimbs} 층";

    public static string EnterFailMessage(int fail) => fail switch {
        1 => "오늘은 이미 등반했습니다.",
        3 => "층 정보가 바뀌었습니다. 다시 시도해주세요.",
        4 => "최고층에 도달했습니다.",
        _ => "무한 타워에 입장하지 못했습니다."
    };

    public static string ClearFailMessage(int fail) => fail switch {
        1 => "오늘은 이미 등반했습니다.",
        3 => "층 정보가 바뀌었습니다.",
        4 => "등반 기록을 확인하지 못했습니다.",
        _ => "등반 결과를 반영하지 못했습니다."
    };
}
