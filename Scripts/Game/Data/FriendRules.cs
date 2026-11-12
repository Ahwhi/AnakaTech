/// <summary>
/// 친구 규칙(클라 표시용 미러). 서버 FriendCatalog 와 값이 일치해야 한다(판정은 서버 권위).
/// 버튼 문구/잠금처럼 서버 응답을 기다리지 않고 바로 알아야 하는 값만 둔다.
/// </summary>
public static class FriendRules {
    /// <summary>친구 정원. 서버가 S_FriendHome 으로도 내려주므로 표시에는 그 값을 우선 쓰고, 이건 폴백이다.</summary>
    public const int MaxFriends = 20;

    // ── 버튼 문구 ────────────────────────────────────
    /// <summary>
    /// 관계에 따른 친구 버튼 문구. 버튼 하나가 신청/취소/수락/삭제를 겸하므로 문구와 동작이 어긋나지 않도록
    /// 문구(여기)와 동작(FriendAction)을 같은 관계 값에서 뽑는다.
    /// </summary>
    public static string FriendButtonText(FriendRelation relation) => relation switch {
        FriendRelation.Friend => "친구 삭제",
        FriendRelation.RequestSent => "신청 취소",
        FriendRelation.RequestReceived => "친구 수락",
        _ => "친구 신청"
    };

    /// <summary>관계에 따라 친구 버튼이 보낼 작업.</summary>
    public static FriendActionType FriendAction(FriendRelation relation) => relation switch {
        FriendRelation.Friend => FriendActionType.Remove,
        FriendRelation.RequestSent => FriendActionType.CancelRequest,
        FriendRelation.RequestReceived => FriendActionType.Accept,
        _ => FriendActionType.Request
    };

    public static string BlockButtonText(bool blocked) => blocked ? "차단 해제" : "차단";

    // ── 실패 사유 ────────────────────────────────────
    /// <summary>서버 FriendRepository.FriendFail 값 -> 안내 문구. 값이 서버와 일치해야 한다.</summary>
    public static string FailMessage(int failReason) => failReason switch {
        1 => "로그인이 필요합니다.",
        2 => "대상을 찾을 수 없습니다.",
        3 => "자기 자신에게는 할 수 없습니다.",
        4 => "이미 친구입니다.",
        5 => $"친구는 최대 {MaxFriends}명까지 가능합니다.",
        6 => "상대의 친구 목록이 가득 찼습니다.",
        7 => "이미 친구 신청을 보냈습니다.",
        8 => "이미 처리된 신청입니다.",
        9 => "친구가 아닙니다.",
        10 => "차단한 상대입니다. 차단을 해제해 주세요.",
        11 => "상대에게 친구 신청을 보낼 수 없습니다.",
        12 => "이미 차단한 상대입니다.",
        13 => "차단하지 않은 상대입니다.",
        14 => "친구 신청이 너무 많습니다.",
        15 => "차단 목록이 가득 찼습니다.",
        16 => "오늘 기력을 보낼 친구가 없습니다.",
        17 => "받을 기력이 없습니다.",
        _ => "요청을 처리하지 못했습니다."
    };

    // ── 기력 선물 ────────────────────────────────────
    /// <summary>기력 선물 버튼 문구(보낼 친구가 남아 있으면 일괄 전송, 아니면 기력 받기).</summary>
    public static string StaminaButtonLabel(FriendStaminaState state) => state.CanSend ? "일괄 전송" : "기력 받기";

    // ── 확인 문구 ────────────────────────────────────
    /// <summary>되돌리기 번거로운 조작에만 확인을 받는다(친구 삭제 / 차단). 그 외에는 바로 진행한다.</summary>
    public static bool NeedsConfirm(FriendActionType action) =>
        action == FriendActionType.Remove || action == FriendActionType.Block;

    public static string ConfirmTitle(FriendActionType action) =>
        action == FriendActionType.Block ? "유저 차단" : "친구 삭제";

    public static string ConfirmMessage(FriendActionType action, string nickname) =>
        action == FriendActionType.Block
            ? $"{nickname} 님을 차단하시겠습니까?"
            : $"{nickname} 님을 친구 목록에서 삭제하시겠습니까?";
}
