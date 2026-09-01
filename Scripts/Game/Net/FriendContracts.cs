using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

// 친구 통신 계약과 순수 DTO(생성 패킷 비의존). 친구 UI 는 이 타입들만 참조한다.

/// <summary>
/// 나를 기준으로 본 상대와의 친구 관계. 값은 서버 FriendRelation 과 일치해야 한다.
/// 차단은 이 축과 독립(친구를 차단할 수도 있다)이라 섞지 않고 별도 bool 로 온다.
/// </summary>
public enum FriendRelation {
    Self = 0,              // 나 자신(친구/차단 버튼을 감춘다)
    None = 1,              // 아무 관계 없음
    Friend = 2,
    RequestSent = 3,       // 내가 신청을 보내 둔 상태
    RequestReceived = 4    // 상대가 나에게 신청한 상태
}

/// <summary>
/// 친구 쓰기 작업 종류. 값은 서버 FriendActionType 과 일치해야 한다.
/// 작업마다 패킷을 만들지 않고 이 값으로 갈라 하나의 요청/응답을 공유한다(길드와 같은 방식).
/// </summary>
public enum FriendActionType {
    Request = 0,
    Accept = 1,
    Decline = 2,
    CancelRequest = 3,
    Remove = 4,
    Block = 5,
    Unblock = 6
}

/// <summary>목록 한 줄(친구 / 받은 신청 / 차단 공용). 친구 슬롯이 쓰는 세 값 + 생성 시각뿐이다.</summary>
public sealed class FriendUserDto {
    public int AccountId;
    public string Nickname = "";
    public int ProfileFrameId;
    public int RepresentativeCharId;
    /// <summary>대표 캐릭터가 장착한 코스튬(0=기본 일러스트). 남의 코스튬은 서버만 알고 있어 함께 내려온다.</summary>
    public int RepresentativeCostumeId;
    /// <summary>마지막으로 보고받은 덱 전투력. 상대가 얼마나 성장했는지를 목록에서 바로 가늠하는 값이다.</summary>
    public long CombatPower;
    /// <summary>친구가 된/신청한/차단한 시각(unix). 0 이면 정보 없음.</summary>
    public long CreatedUnix;
    /// <summary>지금 접속 중인지. 접속 표시를 내려주는 목록(친구 탭)에서만 의미가 있다.</summary>
    public bool IsOnline;
    /// <summary>
    /// 접속 여부를 알고 있는 줄인지. 신청/차단 탭은 서버가 접속 여부를 내려주지 않으므로 false 다.
    /// IsOnline 하나만 보면 "모름"과 "접속 안 함"이 구분되지 않아 오프라인으로 단정해 버린다.
    /// </summary>
    public bool HasPresence;
}

/// <summary>친구 팝업 진입 응답(세 탭의 내용을 한 번에).</summary>
public sealed class FriendHomeDto {
    /// <summary>친구 정원. 서버가 함께 내려주므로 "n / 20" 표시가 클라 상수에 의존하지 않는다.</summary>
    public int MaxFriends;
    public List<FriendUserDto> Friends = new();
    /// <summary>내가 받은 친구 신청. 이 개수가 배지 근거다.</summary>
    public List<FriendUserDto> Requests = new();
    public List<FriendUserDto> Blocked = new();

    public bool IsFull => MaxFriends > 0 && Friends.Count >= MaxFriends;
}

/// <summary>친구 쓰기 작업 결과. 처리 후의 관계가 함께 와서 프로필 팝업이 재조회 없이 버튼을 갱신한다.</summary>
public sealed class FriendActionResultDto {
    public bool IsSuccess;
    public int FailReason;
    public FriendActionType Action;
    public int TargetAccountId;
    public FriendRelation Relation;
    public bool Blocked;
    public int PendingRequests;
}

/// <summary>친구 통신 추상화. UI 는 이 계약에만 의존한다.</summary>
/// <summary>
/// 닉네임 검색 결과 1건. 목록 표시에 필요한 프로필과 "지금 이 사람과 어떤 사이인가"를 함께 담는다.
/// 관계를 서버가 함께 내려 주므로 화면이 친구 목록과 대조해 계산하지 않는다(대조는 목록을 받아 둔
/// 시점과 어긋날 수 있다).
/// </summary>
public sealed class FriendSearchEntry {
    public FriendUserDto User = new();
    public FriendRelation Relation;
    public bool Blocked;

    /// <summary>친구 신청을 보낼 수 있는 상대인지(이미 친구/신청 중/차단은 보낼 수 없다).</summary>
    public bool CanRequest => !Blocked && Relation == FriendRelation.None;
}

/// <summary>친구 검색 결과. 실패해도 목록은 비어 있을 뿐이라 화면은 같은 방식으로 그린다.</summary>
public sealed class FriendSearchResultDto {
    public int FailReason;
    public List<FriendSearchEntry> Users = new();
    public bool IsSuccess => FailReason == 0;
}

/// <summary>친구 검색 규칙(서버 Room.SearchFail / FriendRepository 와 값이 같아야 한다).</summary>
public static class FriendSearchRules {
    public const int MinLength = 2;

    public static string FailMessage(int fail) => fail switch {
        1 => $"{MinLength}글자 이상 입력해 주세요.",
        2 => "로그인이 필요합니다.",
        _ => "검색에 실패했습니다."
    };

    /// <summary>관계에 따른 버튼/상태 문구. 신청할 수 없는 상대에게 왜 안 되는지 알려 준다.</summary>
    public static string RelationLabel(FriendSearchEntry entry) {
        if (entry == null) return "";
        if (entry.Blocked) return "차단함";
        return entry.Relation switch {
            FriendRelation.Friend => "이미 친구",
            FriendRelation.RequestSent => "신청함",
            FriendRelation.RequestReceived => "받은 신청",
            _ => ""
        };
    }
}

public interface IFriendService {
    /// <summary>
    /// 내가 받은(대기 중) 친구 신청 수. 배지가 구독한다.
    /// 접속 중 신청이 오거나 취소되면 서버 푸시(S_FriendNotify)로 즉시 갱신된다.
    /// </summary>
    IReadOnlyReactiveProperty<int> PendingRequests { get; }

    UniTask<FriendHomeDto> RequestHomeAsync(CancellationToken ct = default);

    /// <summary>친구 쓰기 작업 공용 창구. 모든 작업이 대상 계정 하나만 필요로 한다.</summary>
    UniTask<FriendActionResultDto> ActAsync(FriendActionType action, int targetAccountId, CancellationToken ct = default);

    /// <summary>받은 신청 수만 최신화한다(배지 초기 로드용). 실패해도 조용히 넘어간다.</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>닉네임(부분 일치)으로 유저를 찾는다. 친구 목록에 없는 상대에게 신청하려면 이 경로뿐이다.</summary>
    UniTask<FriendSearchResultDto> SearchAsync(string nickname, CancellationToken ct = default);
}
