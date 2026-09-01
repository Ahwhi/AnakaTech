/// <summary>
/// 편성 프리셋 규칙의 클라 미러(서버 GameConstants 와 값이 일치해야 한다).
/// 이름 표시/입력 제한이 여러 화면(슬롯 뷰, 이름 변경 팝업)에서 쓰이므로 한 곳에 모은다.
/// </summary>
public static class DeckPresetRules {
    /// <summary>보유 가능한 프리셋 칸 수.</summary>
    public const int SlotCount = 3;

    /// <summary>프리셋 이름 최대 길이(서버가 이 길이로 잘라 저장한다).</summary>
    public const int NameMaxLength = 12;

    public static bool IsValidSlot(int slot) => slot >= 0 && slot < SlotCount;

    /// <summary>
    /// 화면에 쓸 프리셋 이름. 유저가 지정한 이름이 없으면 슬롯 번호로 만든 기본 이름을 쓴다.
    /// 기본 이름을 서버에 저장하지 않는 이유: 나중에 표기를 바꾸면 기존 데이터가 옛 문구로 남는다.
    /// </summary>
    public static string DisplayName(int slot, string name) =>
        string.IsNullOrWhiteSpace(name) ? $"프리셋 {slot + 1}" : name;

    /// <summary>입력값을 서버와 같은 기준으로 다듬는다(앞뒤 공백 제거 + 길이 제한). 빈 이름은 기본 표시명으로 되돌린다는 뜻.</summary>
    public static string Sanitize(string name) {
        if (string.IsNullOrWhiteSpace(name)) return "";

        string trimmed = name.Trim();
        return trimmed.Length > NameMaxLength ? trimmed.Substring(0, NameMaxLength) : trimmed;
    }
}
