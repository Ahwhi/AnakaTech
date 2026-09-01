/// <summary>
/// 상점 구매 제한(한정 상품)의 해석/표기 규칙을 한곳에 모은 정책.
/// 잔여 구매 횟수를 보여주는 모든 화면(상품 카드, 구매 확인 팝업 등)이 같은 문구를 쓰도록 한다.
///
/// 제한 창이 있는 종류(일간/주간/월간/계정)는 상태와 무관하게 "기간 잔여/한도"로 표기한다.
/// 소진 시에도 0/1 로 남겨 한도 자체를 계속 보여주고, 구매 완료 사실은 카드의 구매완료 오버레이가 알린다.
/// 구독 기준(매일 지급 패키지)만 예외로, 기간 이름이 없어 잔여 수량 대신 만료 안내를 보여준다.
/// </summary>
public static class ShopLimitRules {
    /// <summary>구매 제한 종류. 값은 서버 ShopLimitType 과 일치해야 한다(패킷에 int 로 실려 온다).</summary>
    public enum LimitType {
        None = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Account = 4,
        Subscription = 5   // 매일 지급 패키지 등: 지급이 끝나야 재구매 가능(달력과 무관)
    }

    private const string ExhaustedText = "만료 후 구매가능";

    /// <summary>제한 창의 표시 이름. 창이 없는 종류(구독 등)면 빈 문자열.</summary>
    public static string PeriodName(LimitType type) => type switch {
        LimitType.Daily => "일간",
        LimitType.Weekly => "주간",
        LimitType.Monthly => "월간",
        LimitType.Account => "계정당",
        _ => ""
    };

    /// <summary>
    /// 한정 상품의 잔여 구매 안내 문구. 예) "일간 1/2", "월간 0/1", "계정당 1/1".
    /// 구독형은 진행 중일 때만 "만료 후 구매가능"을 보여준다.
    /// 비한정 상품이거나 서버가 잔여를 내려주지 않았으면 빈 문자열(호출부에서 숨김 처리).
    /// </summary>
    public static string Format(int limitType, int remaining, int limitCount) =>
        Format((LimitType)limitType, remaining, limitCount);

    public static string Format(LimitType type, int remaining, int limitCount) {
        if (type == LimitType.None || limitCount <= 0 || remaining < 0) return "";
        // 구독형은 남은 횟수를 셀 기간이 없다. 진행 중이면 만료 안내, 살 수 있는 상태면 안내 자체가 불필요.
        if (type == LimitType.Subscription) return remaining == 0 ? ExhaustedText : "";
        return $"{PeriodName(type)} {remaining}/{limitCount}";
    }
}
