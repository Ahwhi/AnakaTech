using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 마크다운 문자열을 TextMeshPro 리치텍스트로 바꾸는 변환기.
///
/// 약관이나 공지처럼 원본이 원격(Supabase)에 마크다운으로 보관되는 문서를 TMP 로 그대로 뿌리면
/// '#' 이나 '**' 가 글자로 보인다. 원본을 평문으로 낮추는 대신 표현만 TMP 태그로 옮겨,
/// 문서는 계속 마크다운으로 관리하고 화면에서는 굵기/크기/들여쓰기가 살아 있게 한다.
///
/// 지원 문법
///   - '#' ~ '######' 제목(크기/굵기/색은 MarkdownRichTextStyle 이 정한다)
///   - '**굵게**', '***굵은 기울임***', '*기울임*', '~~취소선~~'
///   - '- ', '* ', '+ ' 글머리 기호 목록과 '1. ' 번호 목록(중첩 들여쓰기 포함, 줄바꿈 시 매달린 들여쓰기)
///   - '> ' 인용, '---' 구분선, '```' 코드 블록(내용은 원문 그대로)
///   - '`인라인 코드`'(기호만 제거), '[문구](주소)'(문구만 표시)
///   - '{이름}글자{/}' 색 지정. 이름과 색은 MarkdownRichTextStyle 이 갖는다(본문에 색값을 적지 않는다)
///   - 역슬래시 이스케이프('\*', '\{' 등)
///
/// 지원하지 않는 문법
///   - 표: TMP 에는 표 개념이 없다. 원본 문서에서 목록이나 문단으로 풀어 쓴다.
///   - '_' 를 이용한 강조: terms_key 같은 식별자가 본문에 자주 나와 오작동한다. '*' 만 강조로 본다.
///
/// 원문에 들어 있던 '&lt;' 는 TMP 가 태그로 오해하지 않도록 noparse 로 감싸 그대로 보여 준다.
/// </summary>
public static class MarkdownRichText {
    // 이스케이프한 문자와 원문 '<' 를 변환 도중 숨겨 두는 표식. 본문에 나올 일이 없는 제어문자를 쓴다.
    private const string EscapeMark = "\u0001";
    private const string LessThanMark = "\u0002";

    // 이스케이프한 문자를 잠시 옮겨 둘 사용자 정의 영역(Private Use Area)의 시작점.
    private const char EscapeShift = '\uE000';

    private static readonly Regex FencePattern = new(@"^\s*(```|~~~)", RegexOptions.Compiled);
    private static readonly Regex RulePattern = new(@"^\s{0,3}(-{3,}|\*{3,}|_{3,})\s*$", RegexOptions.Compiled);
    private static readonly Regex HeadingPattern = new(@"^\s{0,3}(#{1,6})\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex QuotePattern = new(@"^\s*>\s?(.*)$", RegexOptions.Compiled);
    private static readonly Regex OrderedPattern = new(@"^(\s*)(\d{1,3})[.)]\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex BulletPattern = new(@"^(\s*)[-*+]\s+(.*)$", RegexOptions.Compiled);

    private static readonly Regex BoldItalicPattern = new(@"\*\*\*(?=\S)(.+?)(?<=\S)\*\*\*", RegexOptions.Compiled);
    private static readonly Regex BoldPattern = new(@"\*\*(?=\S)(.+?)(?<=\S)\*\*", RegexOptions.Compiled);
    private static readonly Regex ItalicPattern = new(@"\*(?=\S)(.+?)(?<=\S)\*", RegexOptions.Compiled);
    private static readonly Regex StrikePattern = new(@"~~(?=\S)(.+?)(?<=\S)~~", RegexOptions.Compiled);
    private static readonly Regex InlineCodePattern = new(@"`([^`\n]+)`", RegexOptions.Compiled);
    private static readonly Regex LinkPattern = new(@"\[([^\]\n]+)\]\([^)\s]*\)", RegexOptions.Compiled);
    // '{이름}글자{/}'. 여는 이름은 '/' 로 시작할 수 없어 닫는 표시 '{/}' 와 섞이지 않는다.
    private static readonly Regex NamedColorPattern = new(@"\{([^\}/\n][^\}\n]*)\}(.*?)\{/\}", RegexOptions.Compiled);
    private static readonly Regex EscapePattern = new(@"\\([\\`*_{}\[\]()#+\-.!~>|<])", RegexOptions.Compiled);
    private static readonly Regex HiddenPattern = new(EscapeMark + "(.)", RegexOptions.Compiled);

    private enum BlockKind { Blank, Heading, Bullet, Ordered, Quote, Rule, Code, Text }

    private sealed class Block {
        public BlockKind Kind;
        public int Level;    // 제목 깊이 또는 목록 중첩 단계
        public string Text;
        public string Marker; // 번호 목록에서 원문에 적힌 번호
    }

    /// <summary>마크다운 본문을 TMP 리치텍스트로 변환한다. style 을 생략하면 기본 표현을 쓴다.</summary>
    public static string ToRichText(string markdown, MarkdownRichTextStyle style = null) {
        if (string.IsNullOrEmpty(markdown)) return string.Empty;
        return Render(Parse(markdown), style ?? MarkdownRichTextStyle.Default);
    }

    // 줄 단위로 종류를 판정해 블록 목록을 만든다. 여백 계산에 앞뒤 줄 정보가 필요해 변환과 단계를 나눴다.
    private static List<Block> Parse(string markdown) {
        var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var blocks = new List<Block>(lines.Length);
        bool inCode = false;

        foreach (var raw in lines) {
            var line = raw.TrimEnd();

            if (FencePattern.IsMatch(line)) {
                inCode = !inCode;
                continue;
            }
            if (inCode) {
                blocks.Add(new Block { Kind = BlockKind.Code, Text = line });
                continue;
            }
            if (line.Trim().Length == 0) {
                blocks.Add(new Block { Kind = BlockKind.Blank });
                continue;
            }

            // '---' 는 글머리 기호로도 읽히므로 목록보다 먼저 판정한다.
            if (RulePattern.IsMatch(line)) {
                blocks.Add(new Block { Kind = BlockKind.Rule });
                continue;
            }

            var match = HeadingPattern.Match(line);
            if (match.Success) {
                blocks.Add(new Block { Kind = BlockKind.Heading, Level = match.Groups[1].Length, Text = match.Groups[2].Value.TrimEnd('#', ' ') });
                continue;
            }

            match = QuotePattern.Match(line);
            if (match.Success) {
                blocks.Add(new Block { Kind = BlockKind.Quote, Text = match.Groups[1].Value });
                continue;
            }

            match = OrderedPattern.Match(line);
            if (match.Success) {
                blocks.Add(new Block {
                    Kind = BlockKind.Ordered,
                    Level = NestingLevel(match.Groups[1].Value),
                    Marker = match.Groups[2].Value,
                    Text = match.Groups[3].Value,
                });
                continue;
            }

            match = BulletPattern.Match(line);
            if (match.Success) {
                blocks.Add(new Block { Kind = BlockKind.Bullet, Level = NestingLevel(match.Groups[1].Value), Text = match.Groups[2].Value });
                continue;
            }

            blocks.Add(new Block { Kind = BlockKind.Text, Text = line.TrimStart() });
        }

        return blocks;
    }

    // 앞 공백 두 칸을 한 단계로 본다(탭은 두 칸으로 환산). 지나친 중첩은 화면을 좁히므로 세 단계로 제한한다.
    private static int NestingLevel(string leading) {
        int spaces = 0;
        foreach (char c in leading) spaces += c == '\t' ? 2 : 1;
        return Mathf.Clamp(spaces / 2, 0, 3);
    }

    private static string Render(List<Block> blocks, MarkdownRichTextStyle style) {
        var sb = new StringBuilder();
        bool emitted = false;
        bool pendingBlank = false;

        foreach (var block in blocks) {
            if (block.Kind == BlockKind.Blank) {
                // 문서 맨 앞의 빈 줄과 연속된 빈 줄은 한 칸으로 접는다.
                if (emitted) pendingBlank = true;
                continue;
            }

            if (emitted) {
                sb.Append('\n');
                if (pendingBlank) {
                    float percent = block.Kind == BlockKind.Heading ? style.headingSpacingPercent : style.paragraphSpacingPercent;
                    sb.Append(Sized(" ", percent)).Append('\n');
                }
            }
            pendingBlank = false;
            emitted = true;

            sb.Append(RenderBlock(block, style));
        }

        return sb.ToString();
    }

    private static string RenderBlock(Block block, MarkdownRichTextStyle style) {
        switch (block.Kind) {
            case BlockKind.Heading:
                return RenderHeading(Inline(block.Text, style), block.Level, style);

            case BlockKind.Bullet: {
                float indent = style.bulletIndentEm + block.Level * style.nestedIndentEm;
                return Indented($"{style.bulletSymbol} ", Inline(block.Text, style), indent);
            }

            case BlockKind.Ordered: {
                float indent = style.orderedIndentEm + block.Level * style.nestedIndentEm;
                return Indented($"{block.Marker}. ", Inline(block.Text, style), indent);
            }

            case BlockKind.Quote: {
                string body = Inline(block.Text, style);
                if (style.quoteItalic) body = $"<i>{body}</i>";
                return Indented(string.Empty, body, style.quoteIndentEm);
            }

            case BlockKind.Rule:
                return $"<align=center>{Escape(style.ruleSymbol)}</align>";

            case BlockKind.Code:
                return Escape(block.Text);

            default:
                return Inline(block.Text, style);
        }
    }

    // 목록 기호는 왼쪽 끝에 두고 본문만 들여쓴다. TMP 의 indent 는 줄이 넘어가도 유지되므로 매달린 들여쓰기가 된다.
    private static string Indented(string marker, string body, float indentEm) {
        if (indentEm <= 0f) return marker + body;
        return $"{marker}<indent={Number(indentEm)}em>{body}</indent>";
    }

    private static string RenderHeading(string text, int level, MarkdownRichTextStyle style) {
        var heading = style.HeadingAt(level);
        bool resize = !Mathf.Approximately(heading.sizePercent, 100f);

        var sb = new StringBuilder();
        if (heading.useColor) sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(heading.color)).Append('>');
        if (resize) sb.Append("<size=").Append(Number(heading.sizePercent)).Append("%>");
        if (heading.bold) sb.Append("<b>");
        sb.Append(text);
        if (heading.bold) sb.Append("</b>");
        if (resize) sb.Append("</size>");
        if (heading.useColor) sb.Append("</color>");
        return sb.ToString();
    }

    private static string Sized(string text, float percent) => $"<size={Number(percent)}%>{text}</size>";

    // TMP 태그의 수치는 항상 마침표 소수점이어야 한다. 기기 지역 설정에 흔들리지 않게 고정한다.
    private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    // 강조 기호를 TMP 태그로 바꾼다. 이스케이프한 문자와 원문 '<' 는 먼저 숨겨 두었다가 마지막에 되돌린다.
    private static string Inline(string text, MarkdownRichTextStyle style) {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        string s = Hide(text);
        s = BoldItalicPattern.Replace(s, "<b><i>$1</i></b>");
        s = BoldPattern.Replace(s, "<b>$1</b>");
        s = ItalicPattern.Replace(s, "<i>$1</i>");
        s = StrikePattern.Replace(s, "<s>$1</s>");
        s = InlineCodePattern.Replace(s, "$1");
        s = LinkPattern.Replace(s, style.underlineLinks ? "<u>$1</u>" : "$1");
        s = NamedColors(s, style);
        return Reveal(s);
    }

    /// <summary>
    /// '{이름}글자{/}' 를 색 태그로 바꾼다. 강조/목록 처리를 모두 마친 뒤에 돌리므로 색 안에 굵기를 함께 쓸 수 있다.
    ///
    /// 등록되지 않은 이름은 기호만 지우고 글자는 그대로 남긴다. 본문은 운영자가 원격에서 고치는 문서라
    /// 오타를 막을 방법이 없는데, 그때 공지가 통째로 깨지거나 '{강조}' 가 글자로 보이는 것보다
    /// 색만 빠진 채 읽히는 쪽이 낫다.
    /// </summary>
    private static string NamedColors(string text, MarkdownRichTextStyle style) {
        if (text.IndexOf('{') < 0) return text;

        return NamedColorPattern.Replace(text, m => {
            string body = m.Groups[2].Value;
            return style.TryGetColor(m.Groups[1].Value, out Color color)
                ? $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{body}</color>"
                : body;
        });
    }

    /// <summary>강조 처리 없이 원문을 그대로 보여 줄 때 쓰는 변환('&lt;' 만 무력화한다).</summary>
    private static string Escape(string text) => string.IsNullOrEmpty(text) ? string.Empty : Reveal(Hide(text));

    // 이스케이프한 문자는 표식만 붙여서는 안 된다. '\*' 의 '*' 가 그대로 남으면 뒤이은 강조 검사에 다시 걸리기 때문에,
    // 사용자 영역(U+E000~)으로 옮겨 두어 어떤 패턴에도 걸리지 않게 한 뒤 마지막에 제자리로 돌린다.
    private static string Hide(string text) {
        string s = EscapePattern.Replace(text, m => {
            char c = m.Groups[1].Value[0];
            return c == '<' ? LessThanMark : EscapeMark + (char)(EscapeShift + c);
        });
        return s.Replace("<", LessThanMark);
    }

    private static string Reveal(string text) {
        string s = HiddenPattern.Replace(text, m => ((char)(m.Groups[1].Value[0] - EscapeShift)).ToString());
        return s.Replace(LessThanMark, "<noparse><</noparse>");
    }
}
