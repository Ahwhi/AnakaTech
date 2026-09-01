/// <summary>
/// 이벤트 열람 여부 저장소. "한 번도 안 읽은 이벤트"에 미열람(Unread=NEW) 배지를 띄우기 위한 읽음 상태를 보관한다.
/// 현재는 로컬 저장(기기별)이지만, 계정 단위로 옮기려면 이 인터페이스의 다른 구현으로 교체하면 된다.
/// </summary>
public interface IEventReadStore {
    bool IsRead(long eventId);
    void MarkRead(long eventId);
}
