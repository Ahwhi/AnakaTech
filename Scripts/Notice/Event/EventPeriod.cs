using System;

/// <summary>
/// 이벤트 진행 기간의 해석/표기 규칙을 한곳에 모은 정책. 기간을 보여주는 모든 화면이 같은 문구를 쓰도록 한다.
/// (이벤트 페이지 공통 헤더, 공지 상세 팝업 등)
///
/// 기준 시각은 항상 한국 시간(KST)이다. NoticeData 가 서버 UTC 를 KST 로 고정 변환해 주므로
/// 여기서는 기기 타임존을 쓰지 않고 KST 끼리만 비교/표기한다.
/// 종료일을 9999년으로 넣으면 종료가 정해지지 않은 상시 이벤트로 보고 별도 공지 문구를 출력한다.
/// </summary>
public static class EventPeriod {
    /// <summary>이 연도 이상이면 종료일 미정(상시)으로 간주한다.</summary>
    public const int OpenEndedYear = 9999;

    private const string DateFormat = "yyyy.MM.dd";
    private const string OpenEndedText = "별도 공지 시까지";

    /// <summary>이벤트가 지금 어느 단계인지.</summary>
    public enum Phase {
        Upcoming,
        Ongoing,
        Ended
    }

    /// <summary>현재 한국 시간. 기기 타임존과 무관하게 UTC 기준으로 구하며, 개발용 시간 이동도 함께 반영한다.</summary>
    public static DateTime NowKst => KstTime.NowKst;

    /// <summary>종료일이 미정(상시)인지.</summary>
    public static bool IsOpenEnded(DateTime endKst) => endKst.Year >= OpenEndedYear;

    /// <summary>기간이 표시 가능한 값으로 채워져 있는지(파싱 실패/미설정 제외).</summary>
    public static bool HasValidPeriod(NoticeData data) =>
        data != null && data.HasPeriod &&
        data.StartAtKst != DateTime.MinValue && data.EndAtKst != DateTime.MinValue;

    /// <summary>
    /// 기간 문구. 예) "2026.07.01 ~ 2026.07.31", 종료일 미정이면 "2026.07.01 ~ 별도 공지 시까지".
    /// 기간이 없거나 읽을 수 없으면 빈 문자열(호출부에서 숨김 처리).
    /// </summary>
    public static string Format(NoticeData data) {
        if (!HasValidPeriod(data)) return string.Empty;

        string end = IsOpenEnded(data.EndAtKst) ? OpenEndedText : data.EndAtKst.ToString(DateFormat);
        return $"{data.StartAtKst.ToString(DateFormat)} ~ {end}";
    }

    /// <summary>현재 시각(KST) 기준 진행 단계. 기간이 없으면 진행 중으로 본다(상시 노출 페이지).</summary>
    public static Phase Resolve(NoticeData data) {
        if (!HasValidPeriod(data)) return Phase.Ongoing;

        var now = NowKst;
        if (now < data.StartAtKst) return Phase.Upcoming;
        if (!IsOpenEnded(data.EndAtKst) && now >= data.EndAtKst) return Phase.Ended;
        return Phase.Ongoing;
    }

    /// <summary>
    /// 남은 기간 안내 문구. 예) "3일 남음", "오늘 종료", "종료된 이벤트", "3일 후 시작".
    /// 종료일이 미정이거나 기간이 없으면 빈 문자열(호출부에서 숨김 처리).
    /// </summary>
    public static string FormatRemaining(NoticeData data) {
        if (!HasValidPeriod(data)) return string.Empty;

        var now = NowKst;
        switch (Resolve(data)) {
            case Phase.Upcoming:
                return $"{DaysBetween(now, data.StartAtKst)}일 후 시작";
            case Phase.Ended:
                return "종료된 이벤트";
            default:
                return FormatRemainingUntil(data.EndAtKst);
        }
    }

    /// <summary>
    /// 종료 시각(KST)만 아는 곳에서 쓰는 남은 기간 문구. 예) "3일 남음", "11:59:03".
    /// 종료일이 미정(상시)이거나 이미 종료됐으면 빈 문자열(호출부에서 숨김 처리).
    /// 상점의 기간제 상품처럼 NoticeData 가 없는 화면도 같은 문구를 쓰게 한다.
    ///
    /// 24시간 이하로 남으면 시:분:초 카운트다운으로 바꾼다. 날짜 경계로만 세면 12시간짜리 상품도
    /// 종료가 내일 새벽이라는 이유로 "1일 남음"이 되어, 실제보다 훨씬 여유가 있는 것처럼 읽힌다.
    /// 24시간을 넘으면 초 단위가 의미 없으므로 지금처럼 일 단위로 보여 준다.
    ///
    /// 카운트다운은 부르는 시점의 값이라, 표시하는 쪽이 주기적으로 다시 불러야 숫자가 흐른다.
    /// </summary>
    public static string FormatRemainingUntil(DateTime endKst) {
        if (endKst == DateTime.MinValue || IsOpenEnded(endKst)) return string.Empty;

        var now = NowKst;
        if (now >= endKst) return string.Empty;

        var left = endKst - now;
        if (left < TimeSpan.FromDays(1))
            return $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";

        return $"{DaysBetween(now, endKst)}일 남음";
    }

    /// <summary>
    /// 남은 시간이 초 단위로 흐르는 구간(24시간 이하)인지. 표시하는 쪽이 "지금 갱신이 필요한가"를 판단한다.
    /// 일 단위 표기는 하루에 한 번만 바뀌므로 굳이 매 초 다시 그릴 필요가 없다.
    /// </summary>
    public static bool IsCountingDown(DateTime endKst) {
        if (endKst == DateTime.MinValue || IsOpenEnded(endKst)) return false;

        var now = NowKst;
        return now < endKst && endKst - now < TimeSpan.FromDays(1);
    }

    // 날짜 경계 기준 일수(시:분 절삭). 종료 시각이 자정 경계면 그 전날까지가 마지막 날이다.
    private static int DaysBetween(DateTime from, DateTime to) =>
        (int)(to.Date - from.Date).TotalDays;
}
