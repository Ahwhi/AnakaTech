using System;
using UnityEngine;

/// <summary>
/// 마크다운을 TMP 리치텍스트로 바꿀 때 쓰는 표현 설정(크기/색/들여쓰기/여백).
///
/// 변환 규칙은 MarkdownRichText 가 갖고, "어떻게 보이는가"는 전부 이 클래스로 뺐다.
/// 화면마다 강조 색이나 여백이 달라도 코드를 고치지 않고 인스펙터에서 맞출 수 있게 하기 위함이다.
/// </summary>
[Serializable]
public sealed class MarkdownRichTextStyle {
    /// <summary>제목(#) 한 단계의 표현.</summary>
    [Serializable]
    public sealed class HeadingStyle {
        [Tooltip("본문 대비 글자 크기(%). 100 이면 본문과 같다.")]
        public float sizePercent = 100f;

        [Tooltip("굵게 표시할지 여부.")]
        public bool bold = true;

        [Tooltip("색을 따로 줄지 여부. 끄면 텍스트 컴포넌트의 색을 그대로 쓴다.")]
        public bool useColor;

        [Tooltip("useColor 가 켜져 있을 때 적용할 색.")]
        public Color color = Color.white;
    }

    [Tooltip("제목 단계별 표현. 0번이 '#', 1번이 '##' 이다. 배열보다 깊은 단계는 마지막 항목을 쓴다.")]
    public HeadingStyle[] headings = {
        new() { sizePercent = 140f },
        new() { sizePercent = 122f },
        new() { sizePercent = 110f },
        new() { sizePercent = 100f },
    };

    [Header("목록")]
    [Tooltip("글머리 기호 목록('- ')에 사용할 문자.")]
    public string bulletSymbol = "•";

    [Tooltip("글머리 기호 목록의 들여쓰기(em). 줄이 넘어가도 이 위치에 맞춰 정렬된다.")]
    public float bulletIndentEm = 1.2f;

    [Tooltip("번호 목록('1. ')의 들여쓰기(em). 두 자리 번호까지 감안해 조금 넓게 둔다.")]
    public float orderedIndentEm = 2f;

    [Tooltip("중첩 목록 한 단계당 추가 들여쓰기(em).")]
    public float nestedIndentEm = 1.2f;

    [Header("인용과 구분선")]
    [Tooltip("인용문('> ')의 들여쓰기(em).")]
    public float quoteIndentEm = 1.2f;

    [Tooltip("인용문을 기울임으로 표시할지 여부.")]
    public bool quoteItalic = true;

    [Tooltip("'---' 구분선을 그릴 때 쓸 문자열. 가운데 정렬로 출력한다.")]
    public string ruleSymbol = "────────────";

    [Header("여백")]
    [Tooltip("빈 줄 한 칸의 높이(%). 문단 사이 간격이다.")]
    public float paragraphSpacingPercent = 55f;

    [Tooltip("제목 바로 앞 빈 줄의 높이(%). 문단 간격보다 넓게 두면 장/조 구분이 잘 보인다.")]
    public float headingSpacingPercent = 100f;

    [Header("색")]
    [Tooltip("본문에서 {이름}...{/} 로 부를 수 있는 색 목록. 등록하지 않은 이름은 기호만 지우고 글자는 그대로 둔다.")]
    public NamedColor[] namedColors = {
        new() { name = "강조", color = new Color32(0xFF, 0xD2, 0x4A, 0xFF) },
        new() { name = "주의", color = new Color32(0xFF, 0x5C, 0x5C, 0xFF) },
        new() { name = "안내", color = new Color32(0x7F, 0xB2, 0xFF, 0xFF) },
    };

    [Header("기타")]
    [Tooltip("링크([문구](주소))의 문구에 밑줄을 그을지 여부. 주소 자체는 출력하지 않는다.")]
    public bool underlineLinks = true;

    /// <summary>
    /// 본문에서 이름으로 부를 수 있는 색 하나.
    ///
    /// 본문에 색값(#FFD24A)을 직접 적게 하지 않는 이유: 문서는 원격에 오래 남는데 화면 테마는 바뀐다.
    /// 색값이 문서에 박혀 있으면 테마를 바꿀 때 지난 공지를 전부 고쳐야 한다. 이름으로 부르면 여기만 고치면 된다.
    /// </summary>
    [Serializable]
    public sealed class NamedColor {
        [Tooltip("본문에서 쓸 이름. '강조' 로 두면 본문에 {강조}글자{/} 로 적는다.")]
        public string name = string.Empty;
        public Color color = Color.white;
    }

    /// <summary>이름에 해당하는 색. 앞뒤 공백은 무시하고 대소문자는 가리지 않는다.</summary>
    public bool TryGetColor(string name, out Color color) {
        color = Color.white;
        if (namedColors == null || string.IsNullOrWhiteSpace(name)) return false;

        string key = name.Trim();
        foreach (var entry in namedColors) {
            if (entry == null || string.IsNullOrWhiteSpace(entry.name)) continue;
            if (!string.Equals(entry.name.Trim(), key, StringComparison.OrdinalIgnoreCase)) continue;
            color = entry.color;
            return true;
        }
        return false;
    }

    private static readonly HeadingStyle Fallback = new();

    /// <summary>제목 깊이(1 = '#')에 해당하는 표현을 돌려준다. 범위를 벗어나면 가장 가까운 항목을 쓴다.</summary>
    public HeadingStyle HeadingAt(int level) {
        if (headings == null || headings.Length == 0) return Fallback;
        int index = Mathf.Clamp(level - 1, 0, headings.Length - 1);
        return headings[index] ?? Fallback;
    }

    /// <summary>인스펙터에 설정을 두지 않은 호출부가 쓰는 기본 표현.</summary>
    public static MarkdownRichTextStyle Default { get; } = new();
}
