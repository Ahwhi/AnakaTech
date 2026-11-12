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

    [Header("Equipment Stat Rules")]
    [Tooltip("장비 옵션 규칙 안내를 여는 버튼. 장비 확률로 열렸을 때만 보인다.")]
    [SerializeField] private Button equipmentStatButton;
    [Tooltip("장비 옵션 규칙 안내 묶음. 처음에는 닫혀 있고 위 버튼으로 연다.")]
    [SerializeField] private GameObject equipmentStatGroup;
    [Tooltip("장비 옵션 규칙 본문. 리치텍스트로 등급별 수치 구간과 부위별 옵션 구성을 채운다.")]
    [SerializeField] private TMP_Text equipmentStatText;
    [Tooltip("장비 옵션 규칙 안내 묶음의 닫기 버튼.")]
    [SerializeField] private Button equipmentStatCloseButton;

    // 안내 본문 색. 등급 이름은 GradeColorCatalog 를 따르고, 나머지는 어두운 배경에서 읽기 쉬운 톤으로 맞춘다.
    private const string HeaderColor = "#FFD27A";
    private const int HeaderFontSize = 28;   // 단락 제목(등급별 옵션 수치 / 부위별 옵션 구성) 글자 크기
    private const string LabelColor = "#9AA3B5";
    private const string ValueColor = "#F2F4F8";
    private static readonly string[] SlotColors = { "#FF9B8A", "#8CC4FF", "#9BE39B", "#D9A6FF" };

    private readonly List<GameObject> _rows = new();

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
        if (equipmentStatButton != null) equipmentStatButton.onClick.AddListener(() => SetEquipmentStatOpen(true));
        if (equipmentStatCloseButton != null) equipmentStatCloseButton.onClick.AddListener(() => SetEquipmentStatOpen(false));
    }

    // 안내가 열려 있으면 뒤로가기는 안내만 닫는다(확률 팝업까지 한 번에 닫히지 않게).
    public override void OnBackKey() {
        if (IsEquipmentStatOpen) { SetEquipmentStatOpen(false); return; }
        base.OnBackKey();
    }

    private bool IsEquipmentStatOpen => equipmentStatGroup != null && equipmentStatGroup.activeSelf;

    private void SetEquipmentStatOpen(bool open) {
        if (equipmentStatGroup != null) equipmentStatGroup.SetActive(open);
    }

    public void Show(GachaRatesDto rates, CharacterDatabase charDb, EquipmentDatabase equipDb) {
        Clear();
        if (titleText != null && string.IsNullOrEmpty(titleText.text)) titleText.text = "획득 확률";

        BuildGradeSummary(rates);
        BuildEquipmentStatRules(rates);

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

    // 장비 옵션 규칙 안내. 장비가 하나라도 걸린 확률표일 때만 여는 버튼을 보여 준다.
    // 등급은 이 확률표에 실제로 등장하는 장비 등급만 보여 주고, 옵션 칸도 그 등급들이 가진 만큼만 그린다.
    // 그래서 뽑기 풀에 없는 등급(XR 등)은 나오지 않고, 풀에 추가되면 코드 수정 없이 함께 표시된다.
    private void BuildEquipmentStatRules(GachaRatesDto rates) {
        var grades = EquipGradesOf(rates);
        bool show = grades.Count > 0;
        // 버튼만 장비 확률에서 보이고, 안내 묶음은 버튼을 누를 때까지 닫아 둔다(팝업을 다시 열 때도 닫힌 채 시작).
        if (equipmentStatButton != null) equipmentStatButton.gameObject.SetActive(show);
        SetEquipmentStatOpen(false);
        if (equipmentStatText == null) return;
        if (!show) { equipmentStatText.text = ""; return; }

        int optionCount = 0;
        foreach (var g in grades) optionCount = Mathf.Max(optionCount, EquipmentStatLayout.OptionCount(g));

        var sb = new System.Text.StringBuilder();
        AppendGradeRanges(sb, grades, optionCount);
        sb.Append("\n\n");
        AppendSlotOptions(sb, optionCount);
        AppendScaleNotes(sb, optionCount);
        equipmentStatText.text = sb.ToString();
    }

    private static List<Grade> EquipGradesOf(GachaRatesDto rates) {
        var grades = new List<Grade>();
        if (rates == null) return grades;
        foreach (var e in rates.Entries) {
            if (!e.IsEquip || grades.Contains((Grade)e.Grade)) continue;
            grades.Add((Grade)e.Grade);
        }
        grades.Sort((a, b) => b - a);   // 높은 등급 먼저(확률 목록과 같은 순서)
        return grades;
    }

    // 등급별 획득 시 기본 수치 구간. 열을 pos 로 고정해 등급끼리 같은 옵션이 세로로 맞도록 한다.
    private static void AppendGradeRanges(System.Text.StringBuilder sb, List<Grade> grades, int optionCount) {
        sb.Append(Header("등급별 옵션 수치")).Append('\n');

        sb.Append("<color=").Append(LabelColor).Append('>');
        AppendCell(sb, 0, optionCount, "등급");
        AppendCell(sb, 1, optionCount, "주옵션");
        for (int i = 0; i < optionCount; i++) AppendCell(sb, i + 2, optionCount, $"{i + 1}옵션");
        sb.Append("</color>");

        foreach (var g in grades) {
            sb.Append('\n');
            AppendCell(sb, 0, optionCount, "<b>" + GradeText.Colorize(GradeName((int)g), g) + "</b>");
            var (mLo, mHi) = EquipmentRules.MainRollRange(g);
            AppendCell(sb, 1, optionCount, Value($"{mLo}~{mHi}"));
            for (int i = 0; i < optionCount; i++) {
                var (lo, hi) = EquipmentRules.OptionRollRange(g, i);
                AppendCell(sb, i + 2, optionCount, hi > 0 ? Value($"{lo}~{hi}") : Label("-"));
            }
        }
    }

    // 부위별로 어떤 스탯이 몇 번째 옵션에 붙는지. 부위 이름은 부위 색으로 구분하고 스탯은 순서대로 나열한다.
    private static void AppendSlotOptions(System.Text.StringBuilder sb, int optionCount) {
        sb.Append(Header("부위별 옵션 구성"));
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot))) {
            int i = (int)slot;
            string color = SlotColors[Mathf.Clamp(i, 0, SlotColors.Length - 1)];
            sb.Append("\n<b><color=").Append(color).Append('>').Append(EquipmentStatLayout.SlotName(slot)).Append("</color></b>\n");
            sb.Append(Label("주옵션 ")).Append(Value(EquipmentStatLayout.MainLabel(slot)));
            for (int o = 0; o < optionCount; o++)
                sb.Append(Label($"  |  {o + 1}옵션 ")).Append(Value(EquipmentStatLayout.OptionLabel(slot, o)));
        }
    }

    // 포인트 1당 실제 증가량이 1이 아닌 스탯은 따로 알린다(표의 수치를 그대로 읽으면 체력이 작게 보인다).
    private static void AppendScaleNotes(System.Text.StringBuilder sb, int optionCount) {
        var noted = new List<StatType>();
        var notes = new List<string>();
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot))) {
            for (int o = -1; o < optionCount; o++) {
                var s = o < 0 ? EquipmentStatLayout.Main(slot) : EquipmentStatLayout.Option(slot, o);
                if (Mathf.Approximately(s.PerPoint, 1f) || noted.Contains(s.Stat)) continue;
                noted.Add(s.Stat);
                notes.Add($"{GrowthRules.StatName(s.Stat)}은(는) 수치 1당 {StatFormat.FormatValue(s.Stat, s.PerPoint)}");
            }
        }

        sb.Append("\n\n<size=85%><color=").Append(LabelColor).Append('>');
        sb.Append("- 표의 수치는 획득 시(1레벨) 기본값이며, 레벨업으로 함께 성장합니다.");
        if (notes.Count > 0) sb.Append("\n- ").Append(string.Join(", ", notes)).Append(" 증가합니다.");
        // 서버는 구간 안의 정수를 균등 추첨한다(EquipGrowth.Roll). 확률형 고지 성격이라 함께 밝혀 둔다.
        sb.Append("\n- 각 옵션의 수치는 표시된 구간 안에서 모든 값이 동일한 확률로 결정됩니다.");
        sb.Append("</color></size>");
    }

    // 표의 한 칸. 첫 열(등급)은 좁게, 나머지 열은 남은 폭을 균등하게 나눈다.
    private static void AppendCell(System.Text.StringBuilder sb, int column, int optionCount, string text) {
        const float firstWidth = 16f;
        float rest = (100f - firstWidth) / (optionCount + 1);
        float pos = column == 0 ? 0f : firstWidth + rest * (column - 1);
        sb.Append("<pos=").Append(pos.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)).Append("%>").Append(text);
    }

    private static string Header(string text) => $"<size={HeaderFontSize}><b><color={HeaderColor}>{text}</color></b></size>";
    private static string Label(string text) => $"<color={LabelColor}>{text}</color>";
    private static string Value(string text) => $"<color={ValueColor}>{text}</color>";

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
