/// <summary>
/// PvP 전투 씬으로 넘기는 대전 정보. 씬 전환은 인자를 실어 보낼 수 없으므로 진입 직전에 여기 담아 두고
/// 전투 씬이 시작할 때 읽어 간다(레이드/의뢰 씬과 같은 규약).
///
/// 어떤 컨텐츠에서 들어왔는지(Origin)를 함께 담는다. 전투 자체는 어느 컨텐츠든 같지만,
/// 결과를 누구에게 보고할지가 다르기 때문이다(길드 대항전 -> 대항전 보고, 향후 아레나 -> 아레나 보고).
/// 전투 로직은 이 값을 보지 않으며, 씬 매니저만 결과 처리 분기에 쓴다.
/// </summary>
public static class PvpBattleSession {
    /// <summary>이 대전이 어느 컨텐츠에서 시작됐는지. 결과 보고 대상을 정한다.</summary>
    public enum OriginType {
        None = 0,
        GuildWar = 1,
        Arena = 2
    }

    public static OriginType Origin { get; private set; }
    public static int OpponentAccountId { get; private set; }
    public static UserDeckDto OpponentDeck { get; private set; }

    /// <summary>대전 정보가 준비돼 있는지. 씬을 직접 열어 본 경우(에디터 테스트) false 다.</summary>
    public static bool HasEntry => Origin != OriginType.None && OpponentDeck != null && OpponentDeck.Members.Count > 0;

    /// <summary>길드 대항전 공격으로 진입.</summary>
    public static void Set(int opponentAccountId, UserDeckDto opponentDeck) =>
        Set(OriginType.GuildWar, opponentAccountId, opponentDeck);

    /// <summary>
    /// 컨텐츠를 지정해 진입한다. 전투는 어느 컨텐츠든 같으므로 여기서 갈리는 것은 결과 보고 대상뿐이다.
    /// </summary>
    public static void Set(OriginType origin, int opponentAccountId, UserDeckDto opponentDeck) {
        Origin = origin;
        OpponentAccountId = opponentAccountId;
        OpponentDeck = opponentDeck;
    }

    /// <summary>전투 씬을 나올 때 비운다(다음 진입이 옛 값을 쓰지 않도록).</summary>
    public static void Clear() {
        Origin = OriginType.None;
        OpponentAccountId = 0;
        OpponentDeck = null;
    }
}
