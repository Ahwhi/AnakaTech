/// <summary>
/// 닉네임의 클라 표시 규칙(서버 NicknameCatalog 미러).
/// 길이/문자 종류는 길드 이름과 같은 규칙이라 NameRules 한 곳에 두고 여기서는 이름만 빌려 준다.
/// 중복과 금지어는 서버가 판정하므로 여기 없다.
/// </summary>
public static class NicknameRules {
    public const int MaxWidth = NameRules.MaxWidth;
    public const int MinWidth = NameRules.MinWidth;

    public static int Width(string nickname) => NameRules.Width(nickname);

    /// <summary>서버에 물어볼 만한 형식인지. 최종 판정(중복/금지어)은 서버가 한다.</summary>
    public static bool IsValidFormat(string nickname) => NameRules.IsValidFormat(nickname);

    /// <summary>변경권이 없을 때 결제하는 재화와 수량. 서버 NicknameCatalog.ChangeCurrencyKey / ChangeCertificate 와 같아야 한다.</summary>
    public const CurrencyType ChangeCurrency = CurrencyType.DispatchCertificate;
    public const int ChangeCost = 500;

    /// <summary>입력칸 안내 문구.</summary>
    public const string Hint = NameRules.Hint;

    /// <summary>형식이 어긋났을 때의 안내 문구.</summary>
    public static string FormatMessage(string nickname) => NameRules.FormatMessage(nickname);

    /// <summary>
    /// 서버 NicknameFail -> 안내 문구. 값이 서버와 일치해야 한다.
    /// 중복확인과 변경 결과가 같은 코드 체계를 쓰므로 문구도 한 곳에서 만든다.
    /// </summary>
    public static string FailMessage(int fail) => fail switch {
        0 => "사용 가능한 닉네임입니다.",
        1 => NameRules.Hint + " 입력해 주세요.",
        2 => "이미 사용 중인 닉네임입니다.",
        3 => "재화가 부족합니다.",
        6 => "사용할 수 없는 단어가 들어 있습니다.",
        _ => "닉네임을 변경할 수 없습니다."
    };
}
