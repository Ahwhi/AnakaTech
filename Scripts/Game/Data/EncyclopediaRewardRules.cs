using UnityEngine;

/// <summary>
/// 도감 보상 규칙(클라 표시용 미러). 서버 EncyclopediaRewards 와 값이 일치해야 한다(수령 판정은 서버 권위).
/// 수집: 5명당 1단계(총 10), 보상 다이아 100*단계.
/// 별레벨: 총별레벨 5당 1단계(총 120), 만능조각 등급을 N-R-SR-SSR 로 돌리고 한 바퀴마다 개수 +1
/// (N1, R1, SR1, SSR1, N2, ...). XR 조각은 지급하지 않는다.
/// </summary>
public static class EncyclopediaRewardRules {
    public const int RewardTypeCollection = 0;
    public const int RewardTypeStar = 1;

    public const int CollectionCharsPerStep = 5;
    public const int CollectionSteps = 10;

    public const int StarLevelsPerStep = 5;
    public const int StarSteps = 120;

    // 별레벨 보상이 순환하는 등급 수(N/R/SR/SSR).
    public const int StarShardGradeCycle = 4;

    public static int CollectionThreshold(int step) => CollectionCharsPerStep * step;
    public static int CollectionDiamond(int step) => 100 * step;

    public static int StarThreshold(int step) => StarLevelsPerStep * step;
    public static int StarShardGrade(int step) => (Mathf.Max(1, step) - 1) % StarShardGradeCycle;
    public static int StarShardCount(int step) => (Mathf.Max(1, step) - 1) / StarShardGradeCycle + 1;

    public static string GradeName(int grade) => grade switch {
        0 => "N", 1 => "R", 2 => "SR", 3 => "SSR", 4 => "XR", _ => "?"
    };
}
