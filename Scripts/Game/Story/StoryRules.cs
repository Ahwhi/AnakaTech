/// <summary>
/// 장 스토리의 키 규약과 표시 규칙(클라 표시용 미러). 서버 Server.Data.StoryCatalog 와 값이 일치해야 한다.
///
/// 해금 판정은 여기에 두지 않는다. 진도(클리어한 최고 던전)와 직전 편 완독을 서버가 보고 내려 주므로
/// 클라가 같은 판정을 한 번 더 하면 두 규칙이 어긋날 때 "열려 보이는데 눌리면 거부되는" 카드가 생긴다.
/// 이 클래스는 키에서 장 번호를 읽고, 화면에 쓸 문구를 만드는 일만 한다.
/// </summary>
public static class StoryRules {
    /// <summary>장 스토리 키 접두사. 서버 StoryCatalog.ChapterPrefix 와 같아야 한다.</summary>
    public const string ChapterPrefix = "chapter_";

    /// <summary>장 스토리 편 수. 서버 StoryCatalog.ChapterCount 와 같아야 한다.</summary>
    public const int ChapterCount = 23;

    /// <summary>한 편을 끝까지 읽고 받는 다이아. 서버 StoryCatalog.ChapterDiamondReward 와 같아야 한다.</summary>
    public const int ChapterDiamondReward = 1000;

    /// <summary>장 번호(1-based)의 스토리 키.</summary>
    public static string ChapterKey(int chapter) => ChapterPrefix + chapter;

    /// <summary>장 스토리 키에서 장 번호를 읽는다. 장 스토리가 아니면 0.</summary>
    public static int ChapterOf(string storyKey) {
        if (string.IsNullOrEmpty(storyKey) || !storyKey.StartsWith(ChapterPrefix)) return 0;
        if (!int.TryParse(storyKey.Substring(ChapterPrefix.Length), out int chapter)) return 0;
        return chapter >= 1 && chapter <= ChapterCount ? chapter : 0;
    }

    /// <summary>
    /// 이 편을 열기 위해 깨야 하는 스테이지 표기(예: 2편 -> "1-9"). 1편은 조건이 없어 빈 문자열.
    /// 잠긴 카드에 "무엇을 깨야 열리는지" 를 보여 주기 위한 것이다.
    /// </summary>
    public static string RequirementText(int chapter) =>
        chapter <= 1 ? "" : $"{chapter - 1}-{StagesPerChapter} 클리어 시\n잠금 해제";

    /// <summary>이 편을 열기 위해 깨야 하는 던전 인덱스. 1편은 조건이 없어 0.</summary>
    public static int RequiredDungeonIndex(int chapter) =>
        chapter <= 1 ? 0 : (chapter - 2) * StagesPerChapter + StagesPerChapter;

    /// <summary>
    /// 잠긴 이유를 알려 주는 문구. 두 조건 중 무엇이 막고 있는지를 갈라 준다.
    ///
    /// 진도는 됐는데 직전 편을 안 읽은 경우에 "12-9 클리어" 라고 쓰면, 이미 깬 스테이지를 또 깨라는 말이 되어
    /// 유저가 무엇을 해야 하는지 알 수 없다. 그때는 읽으라고 안내해야 한다.
    ///
    /// 이 판정은 문구를 고르기 위한 것일 뿐이고, 실제 해금 여부는 서버가 내려 준 값만 본다(규칙이 갈라지지 않게).
    /// </summary>
    public static string LockReasonText(int chapter, int clearedDungeonIndex, bool previousCompleted) {
        if (chapter <= 1) return "";
        return clearedDungeonIndex >= RequiredDungeonIndex(chapter) && !previousCompleted
            ? "이전 단계 먼저 읽기"
            : RequirementText(chapter);
    }

    /// <summary>한 장의 스테이지 수. 서버 GameConstants.StagesPerChapter 와 같아야 한다.</summary>
    public const int StagesPerChapter = 9;

    /// <summary>카드에 쓰는 편 번호 문구.</summary>
    public static string ChapterLabel(int chapter) => $"{chapter}장";
}
