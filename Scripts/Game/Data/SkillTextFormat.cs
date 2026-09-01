using System.Text.RegularExpressions;

/// <summary>
/// 스킬 설명 표시 색상 포맷(덱/도감 등에서 재사용). 설명 원문(SO의 description)은 색 없이 숫자만 두고,
/// UI에 표시할 때만 이 함수를 통과시켜 TMP 리치텍스트 색을 입힌다. 배수/명수를 바꿔도 원문만 고치면 되고 색 규칙은 여기 한 곳에서 관리한다.
/// 규칙: 'N명' = 타격 대상 수(cyan), '총 N%' = 총 피해량(yellow), 그 외 'N%' = 개별 피해량(green),
///       '[이름]' = 키워드. 상태이상 이름이면 그 상태이상 색(StatusEffectRules), 아니면 공용 키워드 색.
/// </summary>
public static class SkillTextFormat {
    // 색상(원하면 여기서 조정). TMP color 태그용 16진 코드.
    private const string CountColor  = "#008A9A"; // 타격 대상 수(명) - cyan
    private const string DamageColor = "#1FB91F"; // 개별 피해량(%) - green
    private const string TotalColor  = "#A98C2E"; // 총 피해량(총 N%) - yellow
    // 대괄호 키워드 중 상태이상이 아닌 것(고유 용어 등)에 쓰는 공용 색.
    private const string KeywordColor = "#B8752E";

    // 대괄호 키워드와 숫자 표기를 한 번에 매칭한다. 하나의 패턴으로 훑는 이유는 대괄호 안의 숫자가
    // 두 번 칠해져 색 태그가 겹치는 것을 막기 위해서다(먼저 잡힌 대괄호는 통째로 한 색이 된다).
    private static readonly Regex Pattern =
        new Regex(@"\[([^\[\]]+)\]|(총\s*)?(\d+(?:\.\d+)?)(%|명)", RegexOptions.Compiled);

    /// <summary>스킬 설명 원문에 색을 입혀 반환한다(원문이 비어 있으면 그대로). TMP 컴포넌트의 Rich Text가 켜져 있어야 색이 적용된다.</summary>
    public static string Colorize(string description) {
        if (string.IsNullOrEmpty(description)) return description;
        return Pattern.Replace(description, m => {
            // 대괄호 키워드([취약] 등). 대괄호까지 통째로 물들여 한 덩어리로 읽히게 한다.
            if (m.Groups[1].Success) {
                string keyword = m.Groups[1].Value;
                string keywordColor = StatusEffectRules.TryFind(keyword, out var status)
                    ? StatusEffectRules.ColorHex(status)
                    : KeywordColor;
                return "<color=" + keywordColor + ">[" + keyword + "]</color>";
            }

            string prefix = m.Groups[2].Value;   // "총 " 또는 ""
            string number = m.Groups[3].Value;
            string unit = m.Groups[4].Value;     // "%" 또는 "명"
            string color = unit == "명" ? CountColor
                         : (prefix.Length > 0 ? TotalColor : DamageColor);
            return prefix + "<color=" + color + ">" + number + unit + "</color>";
        });
    }
}
