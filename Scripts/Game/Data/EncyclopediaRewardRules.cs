using UnityEngine;

/// <summary>
/// 도감 보상 규칙(클라 표시용 미러). 서버 EncyclopediaRewards 와 값이 일치해야 한다(수령 판정은 서버 권위).
/// 수집: 5명당 1단계, 보상 다이아 100 ~ 500(25명), 30명 5천, 35명 5만, 40명 10만.
/// 별레벨: 총별레벨 5당 1단계, 만능조각 등급을 N-R-SR-SSR 로 돌리고 한 바퀴마다 개수 +1
/// (N1, R1, SR1, SSR1, N2, ...). XR 조각은 지급하지 않는다.
/// 단계 수는 캐릭터 수에서 정해진다(서버는 GameRoster.AllChars, 클라는 CharacterDatabase 의 캐릭터 수).
/// 두 목록의 캐릭터 수가 같아야 단계 수가 일치한다.
/// </summary>
public static class EncyclopediaRewardRules {
    public const int RewardTypeCollection = 0;
    public const int RewardTypeStar = 1;

    public const int CollectionCharsPerStep = 5;
    public const int StarLevelsPerStep = 5;

    /// <summary>수집 단계 수. 전원을 모았을 때 열리는 마지막 단계까지만 둔다(5명 단위에 못 미치는 나머지는 버린다).</summary>
    public static int CollectionSteps(int characterCount) => Mathf.Max(0, characterCount) / CollectionCharsPerStep;

    /// <summary>별레벨 단계 수. 전원을 최대 별레벨까지 올렸을 때의 총별레벨에서 정해진다.</summary>
    public static int StarSteps(int characterCount) =>
        Mathf.Max(0, characterCount) * GrowthRules.MaxStarLevel / StarLevelsPerStep;

    // 별레벨 보상이 순환하는 등급 수(N/R/SR/SSR).
    public const int StarShardGradeCycle = 4;

    /// <summary>수집 단계별 보상 다이아(서버 EncyclopediaRewards.CollectionDiamondTable 과 일치). 표보다 높은 단계는 마지막 값.</summary>
    private static readonly int[] CollectionDiamondTable = { 100, 200, 300, 400, 500, 5000, 50000, 100000 };

    public static int CollectionThreshold(int step) => CollectionCharsPerStep * step;
    public static int CollectionDiamond(int step) =>
        CollectionDiamondTable[Mathf.Clamp(step, 1, CollectionDiamondTable.Length) - 1];

    public static int StarThreshold(int step) => StarLevelsPerStep * step;
    public static int StarShardGrade(int step) => (Mathf.Max(1, step) - 1) % StarShardGradeCycle;
    public static int StarShardCount(int step) => (Mathf.Max(1, step) - 1) / StarShardGradeCycle + 1;

    public static string GradeName(int grade) => grade switch {
        0 => "N", 1 => "R", 2 => "SR", 3 => "SSR", 4 => "XR", _ => "?"
    };
}
