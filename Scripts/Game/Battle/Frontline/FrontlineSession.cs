/// <summary>
/// 전선 씬으로 넘기는 입장 정보(TowerSession 과 같은 규약). 씬 전환은 인자를 실어 보낼 수 없으므로
/// 진입 직전에 여기 담아 두고 전선 씬이 시작할 때 읽어 간다. 스쿼드와 단계는 씬이 서버 현황에서 다시 읽는다.
/// </summary>
public static class FrontlineSession {
    public const int None = -1;

    /// <summary>참여할 전선 id. None 이면 입장 정보가 없다.</summary>
    public static int FrontlineId { get; private set; } = None;

    /// <summary>입장 정보가 준비돼 있는지. 씬을 직접 열어 본 경우(에디터 테스트) false 다.</summary>
    public static bool HasEntry => FrontlineRules.IsValidId(FrontlineId);

    public static void SetEntry(int frontlineId) => FrontlineId = frontlineId;

    /// <summary>전선 씬을 나올 때 비운다(다음 입장이 옛 값을 쓰지 않도록).</summary>
    public static void ClearEntry() => FrontlineId = None;
}
