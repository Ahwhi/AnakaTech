using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 공지/이벤트 게시글 조회 계약. 데이터 출처(현재 Supabase REST)를 캡슐화해,
/// 호출부(프레젠터/뷰)는 "어떻게 가져오는지"를 모른 채 목록만 요청한다.
/// 실시간 TCP 게임 서버와는 별개의 HTTP 백엔드를 사용한다.
/// </summary>
public interface INoticeService {
    /// <summary>
    /// 게시된 공지/이벤트 목록을 가져온다. 상단 고정 글이 먼저, 그 다음 최신순으로 정렬된다.
    /// </summary>
    /// <param name="category">필터. null 이면 전체(공지+이벤트), 지정하면 해당 종류만.</param>
    UniTask<IReadOnlyList<NoticeData>> GetNoticesAsync(NoticeCategory? category = null, CancellationToken ct = default);
}
