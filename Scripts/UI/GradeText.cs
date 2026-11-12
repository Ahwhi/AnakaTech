using TMPro;
using UnityEngine;

/// <summary>
/// 등급을 텍스트로 출력하는 공용 컴포넌트. 등급 문자열을 넣어주고, applyColor 가 체크돼 있으면 GradeColorCatalog 의
/// 등급별 색을 텍스트 색에 적용한다(미체크면 색을 건드리지 않아 프리팹에 지정된 색을 유지).
///
/// 등급 표시가 필요한 곳(덱/도감/가챠 등)에서 TMP_Text 대신 이 컴포넌트를 참조하고 SetGrade 만 호출하면 된다.
/// 카탈로그는 앱 시작 시 SetDefaultCatalog 로 1회 등록되므로, 개별 프리팹마다 SO 를 다시 지정하지 않아도 된다
/// (프리팹별로 다른 팔레트를 쓰고 싶으면 catalog 를 직접 지정하면 그 값이 우선한다).
/// </summary>
public sealed class GradeText : MonoBehaviour {
    [Tooltip("등급을 출력할 텍스트. 비우면 같은 오브젝트의 TMP_Text 를 사용.")]
    [SerializeField] private TMP_Text label;
    [Tooltip("체크 시 등급별 색(GradeColorCatalog)을 적용. 미체크면 색을 건드리지 않는다.")]
    [SerializeField] private bool applyColor = true;
    [Tooltip("색 소스(SO). 비우면 앱이 등록한 기본 카탈로그를 사용.")]
    [SerializeField] private GradeColorCatalog catalog;
    [Tooltip("한계돌파 표시(+) 글자 크기. 등급 글자 대비 비율(1 = 같은 크기).")]
    [SerializeField, Range(0.3f, 1f)] private float limitBreakSuffixScale = 0.7f;

    // 앱 시작 시(LifetimeScope) 1회 등록되는 기본 카탈로그. 개별 프리팹에 SO 를 매번 할당하지 않아도 되게 한다.
    private static GradeColorCatalog _default;
    public static void SetDefaultCatalog(GradeColorCatalog c) => _default = c;

    /// <summary>앱이 등록한 기본 등급 색 카탈로그(등급 색을 텍스트 외 리치텍스트 등에서 재사용할 때). 미등록이면 null.</summary>
    public static GradeColorCatalog DefaultCatalog => _default;

    /// <summary>
    /// 리치텍스트 등급 색 태그로 문자열을 감싼다. 한 텍스트 안에 등급별 색을 섞어야 해서
    /// 컴포넌트로 통째로 색칠할 수 없는 자리(요약 한 줄, 안내 문구)에서 쓴다.
    /// 카탈로그가 없거나 그 등급이 빠져 있으면 색을 입히지 않고 원문을 그대로 돌려준다.
    /// </summary>
    public static string Colorize(string text, Grade grade) {
        if (string.IsNullOrEmpty(text)) return text;
        if (_default == null || !_default.TryGetColor(grade, out var col)) return text;
        return $"<color=#{ColorUtility.ToHtmlStringRGB(col)}>{text}</color>";
    }

    private TMP_Text Label => label != null ? label : (label = GetComponent<TMP_Text>());

    /// <summary>한계돌파한 캐릭터의 등급 뒤에 붙는 표시(예: SSR+).</summary>
    public const string LimitBreakSuffix = "+";

    /// <summary>
    /// 등급 문자열을 표시하고, applyColor 시 등급 색을 적용한다.
    /// limitBroken 이면 등급 뒤에 LimitBreakSuffix 를 붙인다(색은 같은 등급색). 캐릭터의 한계돌파 상태를 아는
    /// 화면만 넘기고, 장비/코스튬/가챠처럼 상관없는 곳은 생략하면 기존과 똑같이 나온다.
    /// </summary>
    public void SetGrade(Grade grade, bool limitBroken = false) {
        var t = Label;
        if (t == null) return;
        // + 만 작게 그린다(TMP 리치 텍스트 size 태그. TMP 는 Rich Text 가 기본으로 켜져 있다).
        t.text = limitBroken
            ? $"{grade}<size={Mathf.RoundToInt(limitBreakSuffixScale * 100f)}%>{LimitBreakSuffix}</size>"
            : grade.ToString();
        if (!applyColor) return;

        var cat = catalog != null ? catalog : _default;
        if (cat != null && cat.TryGetColor(grade, out var col)) t.color = col;
    }
}
