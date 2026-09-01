/// <summary>
/// 던전 해금 규칙(표시용 미러). "앞 던전을 클리어해야 다음 던전이 열린다" = 클리어한 최고 인덱스 + 1 까지 해금.
/// 신규 계정(클리어 없음)은 첫 던전만 열린다.
///
/// GrowthRules 와 같은 성격의 미러다: 실제 시작 가능 여부는 서버가 최종 판정하며(StartDungeon 이 잠긴 던전을 거부),
/// 여기는 잠금 오버레이 표시용이다. 서버 Server.Data.DungeonUnlockRule 과 규칙/상수가 일치해야 한다.
/// 던전 인덱스가 FirstDungeonIndex 부터 1씩 연속이라고 가정한다.
/// </summary>
public static class DungeonUnlockRule {
    public const int FirstDungeonIndex = 1;   // 서버 GameConstants.FirstDungeonIndex 와 일치해야 한다.

    /// <summary>해금된 마지막 던전 인덱스(이 값 이하가 모두 해금).</summary>
    public static int UnlockedThrough(int highestClearedDungeonIndex) =>
        highestClearedDungeonIndex < FirstDungeonIndex
            ? FirstDungeonIndex
            : highestClearedDungeonIndex + 1;

    public static bool IsUnlocked(int dungeonIndex, int highestClearedDungeonIndex) =>
        dungeonIndex >= FirstDungeonIndex &&
        dungeonIndex <= UnlockedThrough(highestClearedDungeonIndex);
}
