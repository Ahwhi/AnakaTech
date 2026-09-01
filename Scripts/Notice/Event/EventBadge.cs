/// <summary>
/// 배지 표시 상태(이벤트 목록 항목과 범용 배지 컴포넌트 BadgeView 가 공유하는 시각 상태).
///   None      : 표시 없음
///   Unread    : NEW 뱃지(스프라이트). 이벤트에선 미열람, 다른 버튼에선 "새로 생김" 등으로 재사용 가능.
///   Claimable : 빨간 동그라미 뱃지(스프라이트). 수령/처리할 게 있음.
/// 우선순위는 enum 값 순서(Claimable > Unread > None)를 따른다. 표시 프리팹은 BadgeCatalog(SO)가 결정한다.
/// </summary>
public enum EventBadge {
    None,
    Unread,
    Claimable,
    /// <summary>
    /// 개수를 숫자로 보여 주는 배지(예: 지금 열려 있는 한정 판매 2개). 켜고 끄는 것만으로는
    /// "몇 건인지" 를 전할 수 없는 자리에 쓴다. 숫자는 BadgeView.SetCount 가 넣는다.
    /// </summary>
    Counter
}

/// <summary>
/// 이벤트 1건의 배지를 결정한다. 열람 여부와 충족 여부를 조합해 우선순위에 따라 하나를 고른다.
/// </summary>
public interface IEventBadgeResolver {
    EventBadge Resolve(NoticeData ev);
}
