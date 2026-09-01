/// <summary>
/// 이벤트 열람 여부를 로컬 데이터 매니저(UserDataManager)에 저장하는 구현. 읽은 이벤트 id 를
/// 현재 계정의 로컬 데이터에 누적하고, 저장은 기존 SaveFlag/오토세이버 흐름(RequestSave)에 맡긴다.
/// 읽음은 보상과 무관한 단순 UI 표시(미열람 Unread=NEW 배지)라 로컬 보관으로 충분하다.
/// 다만 "누가 읽었는가"는 계정에 속한 값이라, 같은 기기 다계정에서 섞이지 않도록 계정별 칸에 기록한다.
/// </summary>
public sealed class LocalEventReadStore : IEventReadStore {
    public bool IsRead(long eventId) {
        var account = UserDataManager.Account;
        return account != null && account.readEventIds.Contains(eventId);
    }

    public void MarkRead(long eventId) {
        var account = UserDataManager.Account;
        if (account == null || account.readEventIds.Contains(eventId)) return;

        account.readEventIds.Add(eventId);
        UserDataManager.RequestSave();
    }
}
