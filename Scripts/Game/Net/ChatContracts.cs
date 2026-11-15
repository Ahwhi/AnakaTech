using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

// 채팅 통신 계약과 순수 DTO(생성 패킷 비의존). 화면은 이 타입들만 참조한다.

/// <summary>채팅 채널. 값이 패킷에 int 로 실리므로 순서를 바꾸지 말 것(서버 ChatChannel 과 일치).</summary>
public enum ChatChannel {
    World = 0,   // 전체
    Guild = 1,   // 길드(소속이 있어야 보내고 받을 수 있다)
    Whisper = 2, // 귓속말(보낸 사람과 받는 사람 둘에게만 간다)
}

/// <summary>
/// 귓속말 수신 범위(계정 설정). 값은 서버 WhisperPolicy 와 일치해야 한다.
/// 판정은 서버가 하고, 설정 화면은 이 값을 고르기만 한다.
/// </summary>
public enum WhisperPolicy {
    All = 0,          // 전체 허용
    FriendsOnly = 1,  // 친구만
    None = 2,         // 전체 차단
}

/// <summary>채팅 한 줄의 종류. 유저가 친 말과 시스템 알림을 색/표시로 구분하는 축이다.</summary>
public enum ChatMessageType {
    User = 0,
    Notice = 1,
}

/// <summary>
/// 시스템 알림의 종류. 서버는 "무슨 일이 있었는지"만 보내고 문구는 클라가 만든다
/// (캐릭터 이름/수련 효과 이름 같은 표시용 문자열은 클라 데이터가 갖고 있기 때문).
/// </summary>
public enum ChatNoticeKind {
    None = 0,
    GachaHighGrade = 1,        // 고등급 캐릭터 모집. RefId = 캐릭터 id
    TrainingMax = 2,           // 수련 최대치. RefId = TrainingEffect, RefValue = 수치 x 10
    GachaHighGradeEquip = 3,   // 고등급 장비 모집. RefId = 장비 id
    StarMaxChar = 4,           // 캐릭터 최대 별레벨 달성. RefId = 캐릭터 id, RefValue = 별레벨
    StarMaxEquip = 5,          // 장비 최대 별레벨(각성) 달성. RefId = 장비 id, RefValue = 별레벨
    LimitBreakChar = 6,        // 캐릭터 한계돌파. RefId = 캐릭터 id
    LimitBreakEquip = 7,       // 장비 한계돌파. RefId = 장비 id
}

/// <summary>채팅 한 줄.</summary>
public sealed class ChatMessageDto {
    /// <summary>서버가 매긴 순번. 중복 수신을 걸러 내고 순서를 맞추는 근거다.</summary>
    public long MessageId;
    public ChatChannel Channel;
    public ChatMessageType Type;
    public ChatNoticeKind NoticeKind;

    // 발언자 표시 정보(서버가 함께 보내 준다). 줄마다 사람이 바뀌므로 여기 실려 오지 않으면
    // 화면이 뜬 뒤에 아이콘이 뒤늦게 채워진다.
    public int AccountId;
    public string Nickname = "";
    public int ProfileFrameId;
    public int RepresentativeCharId;
    public int RepresentativeCostumeId;

    /// <summary>유저가 친 말(알림은 빈 문자열).</summary>
    public string Text = "";

    /// <summary>알림 인자. 종류에 따라 의미가 다르다(캐릭터 id / 수련 효과).</summary>
    public int RefId;
    /// <summary>알림 보조 인자(수련 수치 x 10 등).</summary>
    public int RefValue;

    public long CreatedUnix;

    /// <summary>귓속말을 받는 사람(0 이면 귓속말이 아니다). 화면이 "받은/보낸"을 이 값으로 가른다.</summary>
    public int TargetAccountId;
    /// <summary>귓속말을 받는 사람의 닉네임.</summary>
    public string TargetNickname = "";

    /// <summary>
    /// 내가 친 말인지. 채팅에서 내 발언을 다른 색으로 보여 주기 위한 값이며, 서비스가 내 계정 id 를
    /// 알게 된 시점에 채운다(내 계정 id 는 서버가 최근 대화와 함께 보내 준다).
    /// </summary>
    public bool IsMine;

    public bool IsNotice => Type == ChatMessageType.Notice;

    /// <summary>귓속말인지. 채널이 아니라 받는 사람이 있는지로 판단해 옛 줄과도 어긋나지 않는다.</summary>
    public bool IsWhisper => TargetAccountId != 0;

    /// <summary>귓속말 줄에 표시할 상대(내가 보냈으면 받는 사람, 받았으면 보낸 사람).</summary>
    public string WhisperPartnerNickname => IsMine ? TargetNickname : Nickname;
}

/// <summary>
/// 귓속말 대화 상대 1명. 귓속말 탭의 상대 목록(칩)이 이 값으로 그려진다.
///
/// 대화방을 따로 만들어 두지 않고 버퍼에서 그때그때 뽑는다. 귓속말은 보관하지 않는 대화라
/// 방을 만들어 관리하면 버퍼가 비워질 때 빈 방만 남는다.
/// </summary>
public sealed class WhisperPartnerDto {
    public int AccountId;
    public string Nickname = "";
    /// <summary>이 상대와 주고받은 마지막 줄의 순번. 최근 대화 순 정렬의 기준이다.</summary>
    public long LastMessageId;
    /// <summary>아직 보지 않은 이 상대의 귓속말 수(내가 보낸 것은 세지 않는다).</summary>
    public int Unread;
}

/// <summary>
/// 채팅 통신 창구.
///
/// 수신은 서버 푸시라 요청이 없다. 서비스가 상시 받아 채널별 버퍼에 쌓아 두므로, 화면은 채팅창을 열지 않은
/// 동안에도 흘러간 대화를 잃지 않는다(메인 화면의 최근 한 줄도 같은 버퍼를 본다).
/// </summary>
public interface IChatService {
    /// <summary>채널별 최근 대화(오래된 것 -> 최신). 채팅창이 이 목록을 그대로 그린다.</summary>
    IReadOnlyList<ChatMessageDto> Messages(ChatChannel channel);

    /// <summary>새 대화가 도착했을 때 흐른다(그 줄이 실려 온다). 메인 화면의 최근 한 줄이 구독한다.</summary>
    IObservable<ChatMessageDto> Received { get; }

    /// <summary>가장 최근 대화(전체 채널 기준, 없으면 null). 메인 화면 한 줄의 초기 표시에 쓴다.</summary>
    IReadOnlyReactiveProperty<ChatMessageDto> Latest { get; }

    /// <summary>
    /// 내 계정 id(아직 모르면 0). 서버가 최근 대화와 함께 보내 준다.
    /// 클라가 자기 계정 id 를 따로 들고 있지 않아서, 내 발언을 가리는 근거를 여기서 얻는다.
    /// </summary>
    int MyAccountId { get; }

    /// <summary>최근 대화를 서버에서 받아 버퍼를 채운다(메인 진입 시 1회). 실패해도 조용히 넘어간다.</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// 발언. 성공 여부만 돌려주며, 실제 줄은 서버 푸시로 되돌아온다(본인 것도 같은 경로로 받는다).
    /// </summary>
    /// <param name="targetAccountId">귓속말에서만 쓴다. 받을 사람의 계정 id.</param>
    UniTask<ChatSendResult> SendAsync(ChatChannel channel, string text, int targetAccountId = 0,
                                      CancellationToken ct = default);

    /// <summary>
    /// 귓속말 대화 상대 목록(최근 대화 순). 귓속말 탭의 상대 칩이 이 목록을 그대로 그린다.
    /// 화면이 닫혀 있는 동안 온 귓속말도 버퍼에 쌓이므로, 다시 열면 안 읽은 수가 그대로 남아 있다.
    /// </summary>
    IReadOnlyList<WhisperPartnerDto> WhisperPartners();

    /// <summary>이 상대와의 대화를 읽은 것으로 표시한다(칩의 안 읽음 표식이 꺼진다).</summary>
    void MarkWhisperRead(int accountId);

    /// <summary>
    /// 안 읽은 귓속말 줄 수(모든 상대 합). 다른 채팅에 묻혀 귓속말이 온 줄 모르는 일이 없도록
    /// 메인 화면 채팅 줄의 배지가 구독한다. 채팅창에서 그 상대의 대화를 보면 줄어든다.
    /// </summary>
    IReadOnlyReactiveProperty<int> UnreadWhisperCount { get; }

    /// <summary>내 소셜 설정을 받아 온다(설정 화면 진입).</summary>
    UniTask<WhisperPolicy> RequestWhisperPolicyAsync(CancellationToken ct = default);

    /// <summary>귓속말 수신 범위를 저장한다. 저장 후의 값을 돌려준다.</summary>
    UniTask<WhisperPolicy> SetWhisperPolicyAsync(WhisperPolicy policy, CancellationToken ct = default);

    /// <summary>
    /// 발언 하나를 신고한다. 내용은 서버가 보관 기록에서 가져오므로 여기서는 무엇을 신고하는지만 알린다.
    /// </summary>
    UniTask<ChatReportResult> ReportAsync(long messageId, ChatReportReason reason, string reasonText,
                                          CancellationToken ct = default);

    /// <summary>그 상대와의 귓속말 대화를 내 목록에서 지운다(상대에게는 그대로 남는다).</summary>
    UniTask<bool> DeleteWhisperAsync(int partnerAccountId, CancellationToken ct = default);
}

/// <summary>신고 사유. 값이 서버 ChatCatalog.ReportReason 과 일치해야 한다.</summary>
public enum ChatReportReason {
    Abuse = 0,        // 욕설 혹은 부적절한 대화
    Spam = 1,         // 홍보 혹은 매크로
    BadNickname = 2,  // 불건전한 닉네임
    Other = 3,        // 기타(이용자가 직접 적는다)
}

/// <summary>신고 결과.</summary>
public sealed class ChatReportResult {
    public bool IsSuccess;
    public int FailReason;

    /// <summary>신고 실패 사유 -> 안내 문구.</summary>
    public string Message => FailReason switch {
        0 => "신고가 접수되었습니다.",
        1 => "로그인이 필요합니다.",
        2 => "신고할 수 없는 대상입니다.",
        3 => "이미 지난 대화라 신고할 수 없습니다.",
        4 => "이미 신고한 대화입니다.",
        _ => "신고를 접수하지 못했습니다.",
    };
}

/// <summary>발언 결과.</summary>
public sealed class ChatSendResult {
    public bool IsSuccess;
    /// <summary>서버 실패 사유(ChatRules.FailMessage 로 문구를 만든다).</summary>
    public int FailReason;
}

/// <summary>채팅 규칙(서버 ChatCatalog 와 값이 일치해야 한다). 입력창 제한과 안내 문구가 이 값을 쓴다.</summary>
public static class ChatRules {
    public const int MaxLength = 60;
    public const float CooldownSeconds = 1.5f;

    /// <summary>클라가 들고 있는 채널별 최대 보관 줄 수. 서버 보관량보다 넉넉히 둔다.</summary>
    public const int BufferSize = 100;

    public static string FailMessage(int failReason) => failReason switch {
        1 => "로그인이 필요합니다.",
        2 => "채널을 선택하세요.",
        3 => $"{MaxLength}자 이내로 입력하세요.",
        4 => "잠시 후 다시 시도해 주세요.",
        5 => "길드에 가입해야 이용할 수 있습니다.",
        6 => "같은 내용을 반복해서 보낼 수 없습니다.",
        12 => "상대를 찾을 수 없습니다.",
        13 => "채팅 이용이 제한된 상태입니다.",
        7 => "귓속말을 보낼 수 없는 상대입니다.",
        // 8(접속 중 아님)은 더 이상 오지 않는다. 접속하지 않은 상대에게도 귓속말을 보낼 수 있고,
        // 상대는 다음에 접속해 채팅창을 열 때 받는다. 옛 서버와 붙는 경우를 대비해 문구만 남긴다.
        8 => "접속 중이 아닌 유저입니다.",
        9 => "차단한 유저에게는 귓속말을 보낼 수 없습니다.",
        10 => "귓속말을 받지 않는 유저입니다.",
        11 => "친구에게만 귓속말을 받는 유저입니다.",
        _ => "메시지를 보내지 못했습니다."
    };

    public static string ChannelName(ChatChannel channel) => channel switch {
        ChatChannel.Guild => "길드",
        ChatChannel.Whisper => "귓속말",
        _ => "전체"
    };

    /// <summary>귓속말 수신 범위 표시명(설정 화면).</summary>
    public static string WhisperPolicyName(WhisperPolicy policy) => policy switch {
        WhisperPolicy.FriendsOnly => "친구만",
        WhisperPolicy.None => "전체 차단",
        _ => "전체 허용"
    };
}
