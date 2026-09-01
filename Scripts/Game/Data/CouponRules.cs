using System.Text;

/// <summary>
/// 쿠폰의 클라 표시 규칙(서버 CouponCatalog 미러). 유효/무효 판정과 지급은 전부 서버가 하며,
/// 여기에는 입력을 다듬는 규칙과 화면 문구만 둔다.
/// </summary>
public static class CouponRules {
    public const int MinCodeLength = 4;
    public const int MaxCodeLength = 24;

    /// <summary>
    /// 입력을 표준형(대문자 영숫자)으로 다듬는다. 서버 CouponCatalog.Normalize 와 같은 규칙이어야 한다.
    ///
    /// 유저는 코드를 보고 옮겨 적으므로 대소문자나 하이픈/공백이 섞인다. 그대로 보내면 서버가 어차피
    /// 같게 다듬지만, 입력칸에서 미리 정리해 주면 "내가 친 것이 곧 보낼 값"이라 결과를 예측할 수 있다.
    /// </summary>
    public static string Normalize(string code) {
        if (string.IsNullOrEmpty(code)) return "";
        var sb = new StringBuilder(code.Length);
        foreach (char c in code)
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
        return sb.ToString();
    }

    /// <summary>표준형 코드가 길이 규칙에 맞는지. 버튼 활성 판정에 쓴다(서버가 최종 판정한다).</summary>
    public static bool IsValidCode(string normalized) =>
        normalized.Length >= MinCodeLength && normalized.Length <= MaxCodeLength;

    /// <summary>서버 CouponRepository.Fail 값 -> 안내 문구. 값이 서버와 일치해야 한다.</summary>
    public static string FailMessage(int failReason) => failReason switch {
        1 => "로그인이 필요합니다.",
        2 => $"쿠폰 번호는 영문/숫자 {MinCodeLength}~{MaxCodeLength}자입니다.",
        3 => "존재하지 않는 쿠폰 번호입니다.",
        4 => "사용할 수 없는 쿠폰입니다.",
        5 => "아직 사용 기간이 아닙니다.",
        6 => "사용 기간이 지난 쿠폰입니다.",
        7 => "쿠폰 수량이 모두 소진되었습니다.",
        8 => "이미 사용한 쿠폰입니다.",
        9 => "잘못된 입력이 많습니다. 잠시 후 다시 시도해 주세요.",
        _ => "쿠폰을 사용하지 못했습니다."
    };

    /// <summary>사용 성공 안내. 보상은 우편으로 나가므로 어디서 받는지까지 알려 준다.</summary>
    public const string SuccessMessage = "우편함으로 지급되었습니다.";

    /// <summary>받은 목록을 보여주는 공용 결과 팝업의 제목.</summary>
    public const string RewardTitle = "쿠폰 보상";

    /// <summary>입력칸 안내 문구.</summary>
    public const string InputPlaceholder = "쿠폰 번호를 입력하세요";
}
