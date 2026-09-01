using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 스탯포인트 일괄 투자 팝업. 투자할 스탯 하나와 수량을 고르고 확인을 누르면 그 내용을 돌려준다.
/// 투자 자체는 하지 않는다(서버 요청은 이 팝업을 연 육성 팝업이 한다).
///
/// 투자 버튼을 여러 번 누르는 방식은 포인트가 쌓일수록 감당이 안 된다. 누를 때마다 서버 왕복이 한 번씩
/// 일어나므로 실기기에서는 연타를 해도 왕복 시간만큼만 들어가고, 수십 포인트를 넣는 데 한참이 걸린다.
/// 그래서 "얼마를 어디에" 한 번에 정해 한 번의 요청으로 끝낸다.
///
/// 스탯을 토글이 아니라 드롭다운으로 고르는 이유: 여러 스탯을 동시에 고를 수 있게 하면 "고른 수량을
/// 어떻게 나눌 것인가"라는 규칙이 하나 더 생긴다(균등 분배인지, 스탯마다 따로 적는지). 화면이 복잡해지는
/// 만큼의 이득이 없고, 육성 팝업의 자동 투자 설정도 이미 드롭다운으로 스탯 하나를 고르는 방식이라
/// 같은 조작을 두 가지 모양으로 배우게 하지 않는다.
/// </summary>
public sealed class GrowthStatBulkPopup : UIView {
    /// <summary>확인을 눌렀을 때 돌려주는 내용.</summary>
    public readonly struct Request {
        public readonly StatType Stat;
        public readonly int Count;

        public Request(StatType stat, int count) {
            Stat = stat;
            Count = count;
        }
    }

    [Header("Stat")]
    [Tooltip("투자할 스탯 드롭다운. 항목은 코드가 채운다(투자 가능 스탯 6종).")]
    [SerializeField] private TMP_Dropdown statDropdown;

    [Header("Quantity")]
    [Tooltip("투자 수량 슬라이더(WholeNumbers=on). 최소 1, 최대는 보유 포인트로 코드가 설정한다.")]
    [SerializeField] private Slider quantitySlider;
    [Tooltip("고른 수량 표시(숫자만).")]
    [SerializeField] private TMP_Text quantityValueText;
    [Tooltip("수량 1 감소(선택). ButtonHoldRepeater 를 붙이면 꾹 눌러 빠르게 줄일 수 있다.")]
    [SerializeField] private Button minusButton;
    [Tooltip("수량 1 증가(선택). ButtonHoldRepeater 를 붙이면 꾹 눌러 빠르게 늘릴 수 있다.")]
    [SerializeField] private Button plusButton;
    [Tooltip("보유 포인트 전부를 고르는 버튼(선택).")]
    [SerializeField] private Button maxButton;

    [Header("Preview")]
    [Tooltip("투자 전/후 능력치 미리보기(선택). 고른 스탯과 수량에 맞춰 두 줄로 채운다. " +
             "TMP 의 Rich Text 를 켜 두어야 아래 색이 적용되고, 두 줄이 들어갈 높이여야 한다.")]
    [SerializeField] private TMP_Text previewText;
    [Tooltip("투자 전 줄의 색.")]
    [SerializeField] private Color beforeColor = Color.black;
    [Tooltip("투자 후 줄의 색.")]
    [SerializeField] private Color afterColor = Color.red;

    [Header("Buttons")]
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    private const string BeforeLabel = "투자전";
    private const string AfterLabel = "투자후";

    private UniTaskCompletionSource<Request?> _result;
    private CharacterProgress _progress;
    private int _available;
    private int _count = 1;
    private bool _wired;

    protected override void Awake() {
        base.Awake();
        if (_wired) return;

        BuildStatDropdown();
        if (statDropdown != null) statDropdown.onValueChanged.AddListener(_ => RefreshPreview());
        if (quantitySlider != null) quantitySlider.onValueChanged.AddListener(OnSliderChanged);
        if (minusButton != null) minusButton.onClick.AddListener(() => SetCount(_count - 1));
        if (plusButton != null) plusButton.onClick.AddListener(() => SetCount(_count + 1));
        if (maxButton != null) maxButton.onClick.AddListener(() => SetCount(_available));
        if (okButton != null) okButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(() => Resolve(null));
        _wired = true;
    }

    // 항목을 코드가 채우므로 투자 가능 스탯이 늘어도 프리팹을 손댈 필요가 없다(육성 팝업의 자동 투자 드롭다운과 같은 방식).
    private void BuildStatDropdown() {
        if (statDropdown == null) return;

        var options = new List<string>(GameEnumRules.InvestableStats.Length);
        foreach (var stat in GameEnumRules.InvestableStats) options.Add(GrowthRules.StatName(stat));
        statDropdown.ClearOptions();
        statDropdown.AddOptions(options);
        statDropdown.SetValueWithoutNotify(0);
    }

    /// <summary>
    /// 팝업을 열고 결과를 기다린다. 확인이면 고른 내용, 취소하거나 그냥 닫으면 null 이다.
    /// </summary>
    /// <param name="progress">
    /// 대상 소녀의 현재 성장치. 남은 포인트로 수량 상한을 잡고, 스탯별 투자값으로 투자 전/후를 보여 준다.
    /// </param>
    public async UniTask<Request?> ShowAsync(CharacterProgress progress) {
        _progress = progress;
        _available = Mathf.Max(1, progress != null ? progress.StatPoints : 1);

        if (quantitySlider != null) {
            quantitySlider.wholeNumbers = true;
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = _available;
        }

        // 팝업을 다시 열면 첫 스탯부터 고르게 한다. 이 팝업은 재사용될 수 있는데, 지난번 선택이 남아 있으면
        // 열 때마다 무엇이 골라져 있는지가 달라져 미리보기를 매번 확인해야 한다.
        if (statDropdown != null) statDropdown.SetValueWithoutNotify(0);

        // 기본값은 전부 투자다. 이 팝업을 여는 이유가 "쌓인 포인트를 한 번에 넣는 것"이라,
        // 대부분의 경우 그대로 확인만 누르면 끝나게 둔다.
        SetCount(_available);

        _result = new UniTaskCompletionSource<Request?>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void OnSliderChanged(float value) => SetCount(Mathf.RoundToInt(value));

    private void SetCount(int value) {
        _count = Mathf.Clamp(value, 1, _available);

        if (quantitySlider != null) quantitySlider.SetValueWithoutNotify(_count);
        if (quantityValueText != null) quantityValueText.text = _count.ToString("N0");
        if (minusButton != null) minusButton.interactable = _count > 1;
        if (plusButton != null) plusButton.interactable = _count < _available;
        if (maxButton != null) maxButton.interactable = _count < _available;

        RefreshPreview();
    }

    /// <summary>
    /// 고른 스탯의 투자값이 지금 얼마이고 확인을 누르면 얼마가 되는지 두 줄로 보여 준다.
    ///
    /// 수량 숫자만 보여 주면 "이걸 넣으면 결국 얼마가 되는지"를 유저가 머릿속으로 더해야 한다.
    /// 되돌리려면 다이아가 드는 조작이라, 누르기 전에 결과가 눈에 보여야 한다.
    /// </summary>
    private void RefreshPreview() {
        if (previewText == null) return;

        var stat = SelectedStat();
        string name = GrowthRules.StatName(stat);
        int before = _progress != null ? _progress.Invested(stat) : 0;

        previewText.text =
            $"<color=#{ColorUtility.ToHtmlStringRGBA(beforeColor)}>{BeforeLabel}: {name} {before:N0}</color>\n" +
            $"<color=#{ColorUtility.ToHtmlStringRGBA(afterColor)}>{AfterLabel}: {name} {before + _count:N0}</color>";
    }

    private StatType SelectedStat() {
        var stats = GameEnumRules.InvestableStats;
        int index = statDropdown != null ? Mathf.Clamp(statDropdown.value, 0, stats.Length - 1) : 0;
        return stats[index];
    }

    private void Confirm() => Resolve(new Request(SelectedStat(), _count));

    private void Resolve(Request? value) {
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }

    // 확인/취소 외의 경로(배경 탭, 외부 CloseAllPopups 등)로 닫혀도 결과를 확정해,
    // await 중인 호출부가 영원히 멈추는 것을 막는다(취소로 간주).
    protected override void OnClosed() {
        _result?.TrySetResult(null);
    }
}
