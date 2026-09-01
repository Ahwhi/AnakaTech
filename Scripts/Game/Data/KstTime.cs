using System;

/// <summary>
/// 한국 표준시(KST = UTC+9) 표시 변환. 서버는 모든 시각을 UTC epoch seconds 로 보내고,
/// 화면에 보여줄 때만 KST 로 바꾼다. 게임 내 일정(로테이션/판매기간/우편 만료 등)은 전부 KST 기준으로 안내하므로
/// 변환과 표기 형식을 여기 한 곳에 모아 화면마다 달라지지 않게 한다.
///
/// 기기 로컬 시간대를 쓰지 않는 이유: 해외 접속이나 기기 설정에 따라 같은 일정이 다른 시각으로 보이면
/// 공지/문의 대응이 어긋난다. 표기는 항상 KST 로 고정한다.
/// </summary>
public static class KstTime {
    public static readonly TimeSpan Offset = TimeSpan.FromHours(9);

    /// <summary>
    /// 개발용 시간 이동(초). DevelopTool 이 서버 시계를 옮겼을 때 같은 값을 여기에도 넣어,
    /// 서버가 보낸 시각과 클라의 "지금"이 어긋나지 않게 한다(카운트다운이 오프셋만큼 틀어지는 것을 막는다).
    /// 평상시에는 0 이므로 실제 동작에 영향이 없다.
    /// </summary>
    public static long DevOffsetSeconds { get; set; }

    /// <summary>
    /// 지금(UTC epoch seconds). 서버가 보낸 시각과 비교하는 계산은 전부 이 값을 쓴다.
    /// 기기 시계를 직접 읽지 않는 이유는 개발용 시간 이동을 한 곳에서 반영하기 위해서다.
    /// </summary>
    public static long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds() + DevOffsetSeconds;

    /// <summary>지금(KST 벽시계). 달력 날짜 판정에 쓴다.</summary>
    public static DateTime NowKst => From(NowUnix).DateTime;

    /// <summary>UTC epoch seconds -> KST DateTimeOffset.</summary>
    public static DateTimeOffset From(long unixSeconds) =>
        DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToOffset(Offset);

    /// <summary>"2026. 08. 01 00:00 KST". unix 가 0 이하면 "-".</summary>
    public static string Format(long unixSeconds) =>
        unixSeconds <= 0 ? "-" : From(unixSeconds).ToString("yyyy. MM. dd HH:mm") + " KST";

    /// <summary>"2026. 08. 01 00:00 ~ 2026. 08. 15 00:00 KST". 기간 표기는 KST 를 끝에 한 번만 붙인다.</summary>
    public static string Period(long startUnix, long endUnix) {
        if (startUnix <= 0 && endUnix <= 0) return "-";
        string start = startUnix > 0 ? From(startUnix).ToString("yyyy. MM. dd HH:mm") : "-";
        string end = endUnix > 0 ? From(endUnix).ToString("yyyy. MM. dd HH:mm") : "-";
        return $"{start} ~ {end} KST";
    }

    public static string Period_Short(long startUnix, long endUnix) {
        if (startUnix <= 0 && endUnix <= 0) return "-";
        string start = startUnix > 0 ? From(startUnix).ToString("MM. dd HH:mm") : "-";
        string end = endUnix > 0 ? From(endUnix).ToString("MM. dd HH:mm") : "-";
        return $"{start} ~ {end} KST";
    }

    /// <summary>
    /// 종료까지 남은 시간을 짧은 문구로("3일 남음" / "5시간 남음" / "12분 남음" / "곧 종료").
    /// 이미 끝났거나 종료 시각이 없으면 빈 문자열(표시측이 해당 줄을 숨기게 한다).
    ///
    /// 단위는 내림이라 하루 미만이면 자동으로 시간 단위로 내려간다. 남은 기간을 실제보다 길게 보이지 않게 하는 쪽이
    /// 안전하므로(종료 임박 안내가 늦으면 안 된다) 올림하지 않는다.
    /// </summary>
    public static string Remaining(long endUnix, long nowUnix) {
        if (endUnix <= 0) return "";
        long sec = endUnix - nowUnix;
        if (sec <= 0) return "";
        if (sec >= 86400) return $"{sec / 86400}일 남음";
        if (sec >= 3600) return $"{sec / 3600}시간 남음";
        if (sec >= 60) return $"{sec / 60}분 남음";
        return "곧 종료";
    }

    /// <summary>지금 기준으로 남은 시간(위 오버로드의 편의 버전).</summary>
    public static string Remaining(long endUnix) => Remaining(endUnix, NowUnix);

    /// <summary>
    /// 초 단위로 줄어드는 카운트다운 표기("3일 04:12:07" / "04:12:07").
    /// 하루가 넘는 카운트다운에 TimeSpan 의 "hh" 를 그대로 쓰면 일수가 통째로 사라지므로(hh 는 0~23 성분이다)
    /// 일수를 앞에 따로 붙인다. 남은 시간이 없으면 0 으로 고정한다.
    /// </summary>
    public static string Countdown(long remainSeconds) {
        if (remainSeconds <= 0) return "00:00:00";
        var span = TimeSpan.FromSeconds(remainSeconds);
        string clock = $"{span.Hours:00}:{span.Minutes:00}:{span.Seconds:00}";
        return span.Days > 0 ? $"{span.Days}일 {clock}" : clock;
    }
}
