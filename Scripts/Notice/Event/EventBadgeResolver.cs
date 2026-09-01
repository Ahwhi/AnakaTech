/// <summary>
/// 배지 결정 구현. 충족 여부(서버 권위, IEventStatusProvider)와 열람 여부(로컬, IEventReadStore)를 조합한다.
/// 우선순위: 충족(Claimable=빨간 동그라미) > 미열람(Unread=NEW) > 없음. 우선순위를 바꾸려면 아래 if 순서만 바꾸면 된다.
/// 충족 여부는 서버가 정한 값을 그대로 읽기만 하며 클라이언트가 계산하지 않는다.
/// </summary>
public sealed class EventBadgeResolver : IEventBadgeResolver {
    private readonly IEventStatusProvider _status;
    private readonly IEventReadStore _read;

    public EventBadgeResolver(IEventStatusProvider status, IEventReadStore read) {
        _status = status;
        _read = read;
    }

    public EventBadge Resolve(NoticeData ev) {
        if (ev == null) return EventBadge.None;
        if (_status.GetClaimState(ev) == EventClaimState.Claimable) return EventBadge.Claimable;
        if (!_read.IsRead(ev.id)) return EventBadge.Unread;
        return EventBadge.None;
    }
}
