using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가챠 확률 표시 팝업. 상단에 등급별 합산 확률 한 줄을 두고, 그 아래에 획득 가능한 모든 항목의
/// 종류별 실제 공급 확률을 나열한다(확률형 아이템 확률 고지 기준).
///
/// 표시하는 확률은 서버가 실제 추첨과 동일한 후보 풀로 계산해 보낸 값이라 실제 공급 확률과 어긋나지 않는다.
/// 등급 요약도 항목 확률을 등급으로 합산해 만들기 때문에 두 표기가 항상 일치한다.
/// 상점/가챠 팝업의 "확률 보기" 버튼이 오퍼별로 이 팝업을 연다.
/// </summary>
public sealed class GachaRatesPopup : UIView {
    [Tooltip("선택. 상단 제목.")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("선택. 상단 등급별 합산 확률 한 줄(예: 등급별 등장 확률: SSR 1% SR 9% R 40% N 50%). 등급 색은 GradeColorCatalog 를 따른다.")]
    [SerializeField] private TMP_Text gradeSummaryText;
    [Tooltip("확률 행을 생성할 부모(스크롤뷰 Content).")]
    [SerializeField] private Transform content;
    [SerializeField] private GachaRateRow rowPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<GameObject> _rows = new();

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    public void Show(GachaRatesDto rates, CharacterDatabase charDb, EquipmentDatabase equipDb) {
        Clear();
        if (titleText != null && string.IsNullOrEmpty(titleText.text)) titleText.text = "획득 확률";

        BuildGradeSummary(rates);

        if (rates != null && content != null && rowPrefab != null) {
            var entries = new List<GachaRateEntryDto>(rates.Entries);
            // 등급 높은 순(XR->N), 같은 등급이면 확률 높은 순(픽업이 위로 온다).
            entries.Sort((a, b) => b.Grade != a.Grade ? b.Grade - a.Grade : b.Rate.CompareTo(a.Rate));

            foreach (var e in entries) {
                string itemName;
                IIllustrationSource art = null;
                if (e.Id == 0) {
                    // 등급만(랜덤 등급 상품: 랜덤 캐릭터 만능조각 등). 특정 항목이 아니라 등급/확률만 표기.
                    itemName = GradeName(e.Grade) + " 등급";
                } else if (e.IsEquip) {
                    var d = equipDb != null ? equipDb.Get(e.Id) : null;
                    itemName = d != null ? d.DisplayName : $"#{e.Id}";
                    art = d;
                } else {
                    var d = charDb != null ? charDb.Get(e.Id) : null;
                    itemName = d != null ? d.DisplayName : $"#{e.Id}";
                    art = d;
                }
                var row = Instantiate(rowPrefab, content);
                row.Bind(itemName, (Grade)e.Grade, FormatPercent(e.Rate), art);
                _rows.Add(row.gameObject);
            }
        }

        ShowAsync().Forget();
    }

    // 등급별 합산 확률 한 줄. 등급 색은 GradeText 가 쓰는 카탈로그를 그대로 재사용해 리치텍스트로 칠한다
    // (등급 수만큼 오브젝트를 만들지 않고도 색/표기가 다른 UI 와 일치한다).
    private void BuildGradeSummary(GachaRatesDto rates) {
        if (gradeSummaryText == null) return;
        if (rates == null || rates.Entries.Count == 0) { gradeSummaryText.text = ""; return; }

        var byGrade = rates.RateByGrade();
        var grades = new List<int>(byGrade.Keys);
        grades.Sort((a, b) => b - a);   // 높은 등급 먼저

        var sb = new System.Text.StringBuilder("");
        foreach (int g in grades) {
            sb.Append(' ').Append(GradeText.Colorize(GradeName(g), (Grade)g))
              .Append(' ').Append(FormatPercent(byGrade[g]));
        }
        gradeSummaryText.text = sb.ToString();
    }

    private void Clear() {
        foreach (var r in _rows) if (r != null) Destroy(r);
        _rows.Clear();
    }

    protected override void OnClosed() => Clear();

    private static string GradeName(int grade) => grade switch {
        0 => "N", 1 => "R", 2 => "SR", 3 => "SSR", 4 => "XR", _ => "?"
    };

    /// <summary>
    /// 확률(0~1)을 백분율 문자열로 변환한다.
    ///
    /// 게임산업진흥에 관한 법률 시행령 [별표 3의2] 2-가-2): 공급 확률은 백분율로 표시하되, 반올림하는 경우
    /// "최초로 0이 아닌 숫자가 나오는 자리보다 네 자리 이상 낮은 소수점 자리"에서만 반올림할 수 있다.
    /// "이상 낮은" 이므로 더 낮은 자리(=더 많은 소수 자리)로 표기하는 것은 언제나 허용된다. 즉 정밀도가
    /// 모자란 것만 위반이고 남는 것은 안전하다.
    ///
    /// "N번째 자리에서 반올림" 을 "그 자리를 버린다"(유효숫자 4자리)로 읽을 수도, "그 자리까지 남긴다"
    /// (유효숫자 5자리)로 읽을 수도 있어, 둘 다 만족하는 쪽인 유효숫자 5자리로 남긴다.
    /// 즉 최초 유효숫자 자리보다 4자리 낮은 소수점 자리까지 표기한다.
    ///
    /// 딱 떨어지는 값은 뒤의 0 을 붙이지 않는다(값이 정확하므로 자릿수를 채울 이유가 없다).
    /// 예) 0.5 -> "50%", 0.01 -> "1%", 0.009 -> "0.9%",
    ///     0.0538461538.. -> "5.3846%", 0.00692307.. -> "0.69231%", 0.000769230.. -> "0.076923%".
    /// </summary>
    private static string FormatPercent(double rate) {
        double percent = rate * 100d;
        if (percent <= 0d) return "0%";

        // firstSignificant = 최초 유효숫자의 10의 지수(5.38->0, 0.0769->-2, 50->1).
        // 그보다 4자리 낮은 소수점 자리까지 남기므로 소수 자리수는 4 - firstSignificant 다.
        int firstSignificant = (int)System.Math.Floor(System.Math.Log10(percent));
        int decimals = Mathf.Clamp(4 - firstSignificant, 0, 15);
        double rounded = System.Math.Round(percent, decimals, System.MidpointRounding.AwayFromZero);
        return rounded.ToString("0." + new string('#', decimals)) + "%";
    }
}
