/// <summary>
/// 길드 규칙(클라 표시용 미러). 서버 GuildCatalog 와 값이 일치해야 한다(판정은 서버 권위).
/// 입력 제한/버튼 잠금/가격 표시처럼 서버 응답을 기다리지 않고 바로 알아야 하는 값만 둔다.
/// </summary>
public static class GuildRules {
    public const int MaxMembers = 30;
    public const int CreateCostDiamond = 500;
    public const int RenameCostDiamond = 3000;
    public const int MarkUnlockCostDiamond = 1000;

    public const int MaxIntroLength = 80;
    public const int MaxNoticeLength = 200;

    public const int OfficerMax = 5;
    public const int ViceMasterMax = 1;

    public const int MaxLevel = 100;
    public const int ExpPerLevel = 10;

    public const int DefaultMarkId = 1;
    /// <summary>1~8 은 무료. 9 이상은 전부 유료다(마크를 늘려도 무료 구간은 그대로).</summary>
    public const int FreeMarkMaxId = 8;
    /// <summary>지금까지 만든 마크의 상한. 서버 GuildCatalog.PaidMarkMaxId 와 같아야 한다.</summary>
    public const int PaidMarkMaxId = 16;

    public static bool IsFreeMark(int markId) => markId >= 1 && markId <= FreeMarkMaxId;

    /// <summary>추천 목록 새로고침 최소 간격(초). 버튼 잠금 시간.</summary>
    public const float RecommendRefreshCooldown = 5f;

    /// <summary>
    /// 길드 상점 상품의 대분류 키(서버 ShopCatalog 의 Major 와 일치해야 한다).
    /// 상점 카탈로그 전체에서 길드 상품만 걸러 내는 기준이며, 메인 상점은 이 대분류를 노출하지 않는다.
    /// </summary>
    public const string ShopMajor = "guild";

    // ── 길드 이름 ─────────────────────────────────────
    // 길이/문자 종류는 닉네임과 같은 규칙이라 NameRules 한 곳에 두고 여기서는 이름만 빌려 준다.
    // 중복과 금지어는 서버가 판정한다.
    public const int NameMaxWidth = NameRules.MaxWidth;
    public const int NameMinWidth = NameRules.MinWidth;

    public static int NameWidth(string name) => NameRules.Width(name);

    /// <summary>서버에 물어볼 만한 형식인지. 최종 판정(중복/금지어)은 서버가 한다.</summary>
    public static bool IsValidName(string name) => NameRules.IsValidFormat(name);

    /// <summary>이름 입력칸 안내 문구(규칙을 유저 말로 옮긴 것).</summary>
    public const string NameHint = NameRules.Hint;

    /// <summary>형식이 어긋났을 때의 안내 문구.</summary>
    public static string NameFormatMessage(string name) => NameRules.FormatMessage(name);

    // ── 역할 ─────────────────────────────────────────
    public static string RoleName(GuildRole role) => role switch {
        GuildRole.Master => "길드장",
        GuildRole.ViceMaster => "부길드장",
        GuildRole.Officer => "운영진",
        _ => "길드원"
    };

    public static string JoinTypeName(GuildJoinType type) =>
        type == GuildJoinType.Application ? "가입 신청" : "즉시 가입";

    // ── 가입 버튼 ────────────────────────────────────
    // 문구/활성/실제 작업이 전부 같은 세 가지(정원, 가입 방식, 내 신청 여부)에서 나온다.
    // 목록 카드와 길드 프로필이 같은 버튼을 내므로 규칙을 여기 한 곳에 둔다
    // (화면마다 따로 적으면 한쪽만 고쳐져 "가입"이라 써 놓고 신청이 나가는 어긋남이 생긴다).

    /// <summary>가입 버튼 문구. applied 는 내가 이 길드에 신청을 넣어 둔 상태인지.</summary>
    public static string JoinActionLabel(GuildSummaryDto guild, bool applied) {
        if (guild == null) return "";
        if (guild.IsFull && !applied) return "정원 초과";
        if (applied) return "신청 취소";
        return guild.JoinType == GuildJoinType.Instant ? "가입" : "가입 신청";
    }

    /// <summary>가입 버튼을 누를 수 있는지. 정원이 찼어도 이미 넣은 신청은 취소할 수 있다.</summary>
    public static bool CanPressJoin(GuildSummaryDto guild, bool applied) =>
        guild != null && (!guild.IsFull || applied);

    /// <summary>지금 버튼을 누르면 나갈 작업(가입 / 신청 / 신청 취소).</summary>
    public static GuildActionType JoinActionOf(GuildSummaryDto guild, bool applied) =>
        applied ? GuildActionType.CancelApply
        : guild != null && guild.JoinType == GuildJoinType.Instant ? GuildActionType.Join
        : GuildActionType.Apply;

    // ── 실패 사유 ────────────────────────────────────
    /// <summary>서버 GuildRepository.GuildFail 값 -> 안내 문구. 값이 서버와 일치해야 한다.</summary>
    public static string FailMessage(int failReason) => failReason switch {
        1 => "로그인이 필요합니다.",
        2 => "이미 길드에 가입되어 있습니다.",
        3 => "길드에 가입되어 있지 않습니다.",
        4 => "존재하지 않는 길드입니다.",
        5 => "이미 사용 중인 길드 이름입니다.",
        6 => $"사용할 수 없는 이름입니다. ({NameHint})",
        7 => "다이아가 부족합니다.",
        8 => "길드 정원이 가득 찼습니다.",
        9 => "권한이 없습니다.",
        10 => "대상을 찾을 수 없습니다.",
        11 => "해당 직책의 정원이 가득 찼습니다.",
        12 => "이미 가입 신청한 길드입니다.",
        13 => "가입 신청은 최대 5개까지 가능합니다.",
        14 => "지금은 이 방식으로 가입할 수 없습니다.",
        15 => "대항전이 진행 중이라 인원을 변경할 수 없습니다.",
        16 => "길드원이 남아 있어 해체할 수 없습니다.",
        17 => "사용할 수 없는 길드 마크입니다.",
        18 => "해금하지 않은 길드 마크입니다.",
        19 => "이미 해금한 길드 마크입니다.",
        20 => "입력이 너무 깁니다.",
        21 => "사용할 수 없는 단어가 들어 있습니다.",
        _ => "요청을 처리하지 못했습니다."
    };

    /// <summary>
    /// 최근 접속 표기("접속 중" / "오늘" / "n일 전"). 길드원이 활동 중인지 한눈에 보는 것이 목적이라
    /// 정확한 시각 대신 날짜 차이로 보여 준다. 날짜 경계는 다른 일정과 같은 KST 기준이다.
    /// </summary>
    public static string LastLoginText(long lastLoginUnix) {
        if (lastLoginUnix <= 0) return "";

        int days = (int)(KstTime.NowKst.Date - KstTime.From(lastLoginUnix).Date).TotalDays;
        if (days <= 0) return "오늘";
        return $"{days}일 전";
    }
}
