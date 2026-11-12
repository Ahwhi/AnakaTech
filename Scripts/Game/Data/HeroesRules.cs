using System.Collections.Generic;

/// <summary>
/// 영웅전 규칙의 클라 미러(V1.12). 단계/보상/하루 도전 횟수의 실제 판정은 서버(Server.Data.HeroesCatalog)가 하며,
/// 여기 값은 화면 표시와 전투 진행(처치 목표/제한 시간)에 쓴다. 두 곳의 값이 같아야 한다.
///
/// 소녀 1명을 직접 조작해 BattleSeconds 안에 그 단계 몬스터를 전부 잡으면 클리어다(마릿수는 HeroesDatabase).
/// 소녀가 쓰러지거나 시간이 다 되면 실패. 하루 도전 횟수는 입장할 때 1회 쓴다(성패 무관).
/// </summary>
public static class HeroesRules {
    public const int MaxStage = 50;

    /// <summary>하루 도전 횟수.</summary>
    public const int DailyEntries = 3;

    /// <summary>한 판의 제한 시간(초). 서버 HeroesCatalog.BattleSeconds 와 같아야 한다.</summary>
    public const float BattleSeconds = 30f;

    /// <summary>
    /// 몬스터 공격에 직접 맞을 때마다 줄어드는 남은 시간(초). 출혈 같은 지속 피해의 틱은 세지 않는다.
    /// 클라 판정이며 서버는 경과 시간을 보지 않으므로 서버 값은 없다.
    /// </summary>
    public const float HitTimePenaltySeconds = 1f;

    /// <summary>처치 수 표기. 처치 목표는 단계 구성(HeroesDatabase 구간의 마릿수 합계)이 정한다. 예: "12/50".</summary>
    public static string KillCountText(int kills, int target) => $"{kills}/{target}";

    /// <summary>보스 체력 표기. 살아 있는 동안 0% 로 보이지 않게 올림한다. 예: "37%".</summary>
    public static int BossHpPercent(float ratio) => UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Clamp01(ratio) * 100f);
    public static string BossHpText(int percent) => $"{percent}%";

    /// <summary>결과 화면의 한 줄 문구.</summary>
    public static string ResultMessage(HeroesResult result) => result switch {
        HeroesResult.Cleared => "클리어에 성공했습니다.",
        HeroesResult.TimeOver => "제한 시간 안에 처치하지 못했습니다.",
        _ => "소녀가 쓰러졌습니다."
    };

    /// <summary>단계 보상 = 단계 x 이 값.</summary>
    public const int DiamondPerStage = 100;
    public const int GoldPerStage = 100000;

    /// <summary>그 단계를 깼을 때의 보상(표시용). 서버 HeroesCatalog.RewardsFor 와 같아야 한다.</summary>
    public static List<RewardItem> RewardsFor(int stage) {
        int s = System.Math.Clamp(stage, 1, MaxStage);
        return new List<RewardItem> {
            RewardItem.Diamond(DiamondPerStage * s),
            RewardItem.Gold(GoldPerStage * s),
        };
    }

    public static string StageText(int stage) => $"{stage}단계";

    public static string RewardTitleText(int stage) => $"{stage}단계 클리어 보상";

    /// <summary>오늘 남은 도전 횟수 표기(라벨은 프리팹에서 따로 둔다). 예: "2 / 3".</summary>
    public static string EntryCountText(int remain) => $"{remain} / {DailyEntries}";

    public const string MaxStageMessage = "최대 단계를 달성했습니다.\n다음 업데이트를 기다려주세요.";

    public static string EnterFailMessage(int fail) => fail switch {
        1 => "오늘 도전 횟수를 모두 사용했습니다.",
        3 => "단계 정보가 바뀌었습니다. 다시 시도해주세요.",
        4 => "최대 단계를 달성했습니다.",
        _ => "영웅전에 입장하지 못했습니다."
    };

    public static string ClearFailMessage(int fail) => fail switch {
        3 => "단계 정보가 바뀌었습니다.",
        4 => "전투 기록을 확인하지 못했습니다.",
        _ => "클리어 결과를 반영하지 못했습니다."
    };
}
