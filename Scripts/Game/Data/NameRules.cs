/// <summary>이름 검사 결과. 서버 NameFail 과 값이 일치해야 한다.</summary>
public enum NameFail {
    Ok = 0,
    Empty = 1,          // 비었거나 공백뿐
    TooShort = 2,
    TooLong = 3,
    InvalidChar = 4,    // 한글/영문/숫자 외의 문자
    Banned = 5,         // 금지어 포함(서버만 판정한다)
}

/// <summary>
/// 유저가 정하는 "이름"의 공통 규칙(서버 NameRules 미러). 닉네임과 길드 이름이 같은 규칙을 쓴다.
///
/// 여기에는 문자 종류와 길이만 있다. 이 둘은 절대 바뀌지 않는 규칙이라 클라가 들고 있어도 서버와 어긋나지 않고,
/// 입력하는 즉시 안내할 수 있어 왕복이 줄어든다.
///
/// 금지어는 여기 없다. 목록이 계속 늘어나서 클라에 넣으면 단어 하나 추가할 때마다 앱 배포가 필요하고,
/// 클라 코드는 유저가 고칠 수 있어 애초에 판정 주체가 될 수 없다. 금지어는 서버가 판정해 실패 사유로 알려 준다.
/// 이 화면의 검사는 어디까지나 미리 알려 주기 위한 것이고, 최종 판정은 항상 서버다.
/// </summary>
public static class NameRules {
    // 한글 6자 / 영어 12자를 한 규칙으로 표현하기 위해 "표시 폭"으로 센다.
    // ASCII 는 1, 그 외(한글 등)는 2로 세고 상한을 12로 둔다.
    public const int MaxWidth = 12;

    // 하한도 같은 폭으로 잰다(한글 2자 = 영문 4자). 서버 NameRules 와 값이 같아야 한다.
    public const int MinWidth = 4;

    /// <summary>표시 폭. ASCII 1, 그 외 2.</summary>
    public static int Width(string name) {
        if (string.IsNullOrEmpty(name)) return 0;
        int width = 0;
        foreach (char c in name) width += c < 128 ? 1 : 2;
        return width;
    }

    /// <summary>이름에 쓸 수 있는 문자인지. 한글 완성형 / 영문 / 숫자만 허용한다(서버와 같은 범위).</summary>
    public static bool IsAllowedChar(char c) {
        if (c >= '0' && c <= '9') return true;
        if (c >= 'a' && c <= 'z') return true;
        if (c >= 'A' && c <= 'Z') return true;
        return c >= '가' && c <= '힣';   // 한글 완성형
    }

    /// <summary>문자 종류와 길이 검사. 금지어는 서버가 본다.</summary>
    public static NameFail CheckFormat(string name) {
        if (string.IsNullOrWhiteSpace(name)) return NameFail.Empty;

        // 공백은 허용 문자가 아니므로 앞뒤 공백도 여기서 InvalidChar 로 걸린다(따로 볼 필요가 없다).
        foreach (char c in name)
            if (!IsAllowedChar(c)) return NameFail.InvalidChar;

        int width = Width(name);
        if (width < MinWidth) return NameFail.TooShort;
        if (width > MaxWidth) return NameFail.TooLong;
        return NameFail.Ok;
    }

    /// <summary>형식만 맞으면 서버에 물어볼 가치가 있다(중복/금지어는 서버가 판정).</summary>
    public static bool IsValidFormat(string name) => CheckFormat(name) == NameFail.Ok;

    /// <summary>입력칸 안내 문구(규칙을 유저 말로 옮긴 것).</summary>
    public const string Hint = "한글 2~6자 / 영문 4~12자, 특수문자 불가";

    /// <summary>형식이 어긋났을 때의 안내. 무엇이 틀렸는지까지 알려 준다.</summary>
    public static string FormatMessage(NameFail fail) => fail switch {
        NameFail.Empty => "이름을 입력해 주세요.",
        NameFail.TooShort => "너무 짧습니다. " + Hint,
        NameFail.TooLong => "너무 깁니다. " + Hint,
        NameFail.InvalidChar => "한글, 영문, 숫자만 쓸 수 있습니다.",
        NameFail.Banned => "사용할 수 없는 단어가 들어 있습니다.",
        _ => Hint
    };

    /// <summary>입력 문자열의 형식 안내 문구. 문제가 없으면 빈 문자열.</summary>
    public static string FormatMessage(string name) {
        var fail = CheckFormat(name);
        return fail == NameFail.Ok ? "" : FormatMessage(fail);
    }
}
