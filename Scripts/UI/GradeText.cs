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

    /// <summary>등급 문자열을 표시하고, applyColor 시 등급 색을 적용한다.</summary>
    public void SetGrade(Grade grade) {
        var t = Label;
        if (t == null) return;
        t.text = grade.ToString();
        if (!applyColor) return;

        var cat = catalog != null ? catalog : _default;
        if (cat != null && cat.TryGetColor(grade, out var col)) t.color = col;
    }
}
