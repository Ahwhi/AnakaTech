/// <summary>
/// 이벤트 전용 재화(RewardItemKind.EventToken)의 표시 규칙. 재화는 이벤트 키로 구분되며,
/// 이름은 여기서, 아이콘은 ShopIconCatalog 의 IconKey 로 찾는다. 새 이벤트가 전용 재화를 가지면 이름 한 줄만 더한다.
/// </summary>
public static class EventTokens {
    public static string Name(string eventKey) => eventKey switch {
        EventMissionKeys.DailyRoulette => "다트",
        _ => "이벤트 재화"
    };

    /// <summary>ShopIconCatalog 에 등록할 아이콘 키(예: event_token_daily_roulette).</summary>
    public static string IconKey(string eventKey) => $"event_token_{eventKey}";
}
