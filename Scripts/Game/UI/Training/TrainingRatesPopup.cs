using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 수련 확률 표시 팝업. 효과 종류별로 "등장 확률 / 획득 가능한 수치 / 각 수치 추첨 확률 / 개별 최종 확률"을 한 줄씩 보여준다.
///
/// 표시하는 확률은 서버가 실제 추첨에 쓰는 카탈로그를 그대로 내려준 값(TrainingRateDto)에서 계산한다.
/// 화면에 수치를 적어 두지 않으므로 밸런스를 바꿔도 고지가 저절로 따라오고, 고지와 추첨이 어긋날 수 없다
/// (가챠 확률 고지와 같은 원칙).
/// </summary>
public sealed class TrainingRatesPopup : UIView {
    [Tooltip("선택. 상단 제목. 배선하면 코드가 문구를 채운다(프리팹에 적어 둔 글자는 덮어쓴다). " +
             "프리팹에서 직접 문구를 쓰고 싶으면 비워 두면 된다.")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("선택. 상단 안내 문구(추첨 방식 설명). 제목과 같은 규칙으로 코드가 채운다.")]
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("확률 행을 생성할 부모(스크롤뷰 Content).")]
    [SerializeField] private Transform content;
    [SerializeField] private TrainingRateRow rowPrefab;
    [SerializeField] private Button closeButton;

    // 표 강조 색(TMP 리치텍스트). 축마다 색을 달리해 한 줄에서 "무엇이 / 얼마나 자주 / 얼마나 크게" 가 갈리게 한다.
    //   이름 = 사이언, 확률 = 노랑, 획득 수치 = 초록, 간격 = 회색(보조 설명).
    // 이름과 확률을 같은 색으로 두면 종류와 수치가 한 덩어리로 읽히므로 반드시 다른 색을 쓴다.
    // 표시하는 TMP 의 Rich Text 가 켜져 있어야 색이 적용된다.
    private const string NameColor = "#3DB8E0";    // 효과 이름 - 사이언
    private const string RateColor = "#E0B33D";    // 확률 값 - 노랑
    private const string ValueColor = "#3DC46A";   // 획득 가능한 수치 범위 - 초록
    private const string StepColor = "#9AA0A6";    // 수치 간격 (0.1단위) - 회색

    // 화면 문구. 표에 들어가는 값과 달리 데이터에서 나오는 것이 아니라 고정 안내라 여기 둔다.
    private const string TitleMessage = "수련 효과 확률";
    private const string DescriptionMessage =
        "수련이 완료되면 효과 종류를 먼저 뽑고, 그 효과의 수치를 한 칸 균등하게 뽑습니다.";

    private readonly List<GameObject> _rows = new();

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    /// <summary>확률표를 그리고 팝업을 연다. rates 는 수련 조회 응답에 함께 실려 온 값을 그대로 넘긴다.</summary>
    public void Show(IReadOnlyList<TrainingRateDto> rates) {
        Clear();

        // 배선돼 있으면 항상 덮어쓴다(다른 팝업과 같은 방식). 조건부로 채우면 프리팹에 글자를 적어 둔 순간
        // 배선이 아무 일도 하지 않게 되어, 할당해 두고도 왜 안 바뀌는지 알 수 없다.
        if (titleText != null) titleText.text = TitleMessage;
        if (descriptionText != null) descriptionText.text = DescriptionMessage;

        if (rates != null && content != null && rowPrefab != null) {
            foreach (var r in rates) {
                var effect = (TrainingEffect)r.Effect;
                // 획득 수치 칸은 "범위(초록) + 간격(회색)" 두 조각이라 색을 따로 입혀 이어 붙인다.
                string valueRange = Colored(TrainingRules.RangeText(effect, r.MinTenth, r.MaxTenth), ValueColor)
                                    + " " + Colored(TrainingRules.StepText(effect, r.StepTenth), StepColor);

                var row = Instantiate(rowPrefab, content);
                row.Bind(
                    Colored(TrainingRules.EffectName(effect), NameColor),
                    Colored(RateText(r.EffectRate, r.TotalWeight, r.Weight), RateColor),
                    valueRange,
                    Colored(RateText(r.ValueRate, r.ValueCount, 1), RateColor),
                    Colored(RateText(r.FinalRate, r.TotalWeight * r.ValueCount, r.Weight), RateColor));
                _rows.Add(row.gameObject);
            }
        }

        ShowAsync().Forget();
    }

    // TMP 리치텍스트 색 입히기. 색 규칙을 한 곳에 모아 두기 위한 것이며, 문구가 비면 태그도 붙이지 않는다.
    private static string Colored(string text, string hex) =>
        string.IsNullOrEmpty(text) ? text : $"<color={hex}>{text}</color>";

    // "20% (1/5)" 형태. 분수를 함께 보여 주는 이유는 백분율만 적으면 반올림된 값처럼 보이기 때문이다
    // (실제 확률은 가중치의 비율이라 정확한 유리수다).
    private static string RateText(double rate, int denominator, int numerator) {
        string percent = FormatPercent(rate);
        if (denominator <= 0 || numerator <= 0) return percent;
        return numerator == 1 ? $"{percent} (1/{denominator})" : $"{percent} ({numerator}/{denominator})";
    }

    /// <summary>
    /// 확률(0~1)을 백분율 문자열로 변환한다. 표기 정밀도 규칙은 가챠 확률 고지(GachaRatesPopup)와 같다.
    ///
    /// 게임산업진흥에 관한 법률 시행령 [별표 3의2] 2-가-2): 공급 확률은 백분율로 표시하되, 반올림하는 경우
    /// "최초로 0이 아닌 숫자가 나오는 자리보다 네 자리 이상 낮은 소수점 자리"에서만 반올림할 수 있다.
    /// 정밀도가 모자란 것만 위반이고 더 많은 소수 자리로 적는 것은 언제나 허용되므로 유효숫자 5자리로 남긴다.
    /// </summary>
    private static string FormatPercent(double rate) {
        double percent = rate * 100d;
        if (percent <= 0d) return "0%";

        int firstSignificant = (int)System.Math.Floor(System.Math.Log10(percent));
        int decimals = Mathf.Clamp(4 - firstSignificant, 0, 15);
        double rounded = System.Math.Round(percent, decimals, System.MidpointRounding.AwayFromZero);
        return rounded.ToString("0." + new string('#', decimals)) + "%";
    }

    private void Clear() {
        foreach (var r in _rows) if (r != null) Destroy(r);
        _rows.Clear();
    }

    protected override void OnClosed() => Clear();
}
