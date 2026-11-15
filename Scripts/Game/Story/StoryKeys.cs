/// <summary>
/// 스토리 키 미러(서버 Server.Data.StoryCatalog 와 값이 일치해야 한다).
/// 서버에 등록되지 않은 키로 완료를 보고하면 거부되어 같은 스토리가 계속 다시 재생되므로,
/// 새 스토리를 추가할 때는 양쪽에 같은 문자열을 넣는다.
/// </summary>
public static class StoryKeys {
    /// <summary>인트로. 계정당 1회, 메인 씬에 처음 들어가기 전에 재생한다.</summary>
    public const string Intro = "intro";

    /// <summary>장 스토리(1~StoryRules.ChapterCount). 키 규약은 StoryRules 가 갖는다(서버 StoryCatalog 와 일치).</summary>
    public static string Chapter(int chapter) => StoryRules.ChapterKey(chapter);
}
