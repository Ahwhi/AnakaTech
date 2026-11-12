using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장비 일괄 분해 팝업. 등급(N/R/SR/SSR)을 여러 개 골라 한 번에 분해한다.
/// 확인하면 고른 등급 목록을, 취소하면 빈 목록을 await 로 돌려준다(실제 통신은 EquipmentPopup 이 한다).
///
/// 고른 등급의 "미장착 + 각성/레벨업하지 않은" 장비만 대상이다(EquipmentRules.IsBulkDisassembleTarget).
/// 키운 장비를 빼는 이유는 한 번에 수백 개가 사라지는 조작이라 투자한 재화를 되돌릴 방법이 없기 때문이며,
/// 그런 장비는 상세 화면의 개별 분해로만 처분한다.
///
/// 미리보기 개수는 스토어에 있는 장비로 세므로, 가방이 전송 상한을 넘겨 목록이 잘린 계정에서는 실제보다
/// 적게 나올 수 있다. 분해 자체는 서버가 보이지 않는 장비까지 포함해 처리하므로 결과 개수는 서버 값을 쓴다.
/// </summary>
public sealed class EquipmentDisassemblePopup : UIView {
    [Header("Grade Toggles")]
    [Tooltip("등급 토글. 순서 고정: 0=N, 1=R, 2=SR, 3=SSR (EquipmentRules.DisassembleGrades 와 같은 순서). "
           + "여러 개를 동시에 켤 수 있어야 하므로 ToggleGroup 을 붙이지 말 것.")]
    [SerializeField] private Toggle[] gradeToggles;
    [Tooltip("각 토글 옆의 대상 개수 표시(예: 174). gradeToggles 와 같은 순서/길이. 선택.")]
    [SerializeField] private TMP_Text[] gradeCountTexts;

    [Header("Summary")]
    [Tooltip("분해 대상 개수 문구.")]
    [SerializeField] private TMP_Text targetCountText;
    [Tooltip("분해 보상 골드.")]
    [SerializeField] private TMP_Text rewardGoldText;
    [Tooltip("보상 골드 아이콘. 재화 카탈로그에서 자동 배선된다. 선택.")]
    [SerializeField] private Image rewardGoldIcon;

    [Header("Buttons")]
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    private static readonly IReadOnlyList<Grade> Nothing = new List<Grade>();

    private MainHudContext _hud;
    private UniTaskCompletionSource<IReadOnlyList<Grade>> _result;

    // 등급별 대상 개수/보상 골드. 팝업이 열려 있는 동안 장비가 바뀌지 않으므로 열 때 한 번만 센다.
    // 길이는 규칙 표에서 그대로 가져온다(분해 가능 등급이 늘어도 여기는 고칠 것이 없다).
    private readonly int[] _countByGrade = new int[EquipmentRules.DisassembleGrades.Length];
    private readonly long[] _goldByGrade = new long[EquipmentRules.DisassembleGrades.Length];

    protected override void Awake() {
        base.Awake();

        if (okButton != null) okButton.onClick.AddListener(OnOk);
        if (cancelButton != null) cancelButton.onClick.AddListener(() => Resolve(Nothing));

        if (gradeToggles == null) return;
        for (int i = 0; i < gradeToggles.Length; i++) {
            if (gradeToggles[i] == null) continue;
            gradeToggles[i].onValueChanged.AddListener(_ => {
                if (_hud != null) _hud.Sound.PlaySfx(Enums.SfxKey.ButtonClick);
                RefreshSummary();
            });
        }
    }

    /// <returns>확인 시 고른 등급들(1개 이상), 취소/외부 닫힘 시 빈 목록.</returns>
    public async UniTask<IReadOnlyList<Grade>> ShowAsync(MainHudContext hud) {
        _hud = hud;
        CurrencyIcon.Apply(rewardGoldIcon, hud.Currencies, CurrencyType.Gold);

        CountTargets();

        // 대상이 없는 등급은 고를 수 없게 한다(눌러 봐야 0개라 아무 일도 일어나지 않는다).
        var grades = EquipmentRules.DisassembleGrades;
        if (gradeToggles != null) {
            for (int i = 0; i < gradeToggles.Length; i++) {
                if (gradeToggles[i] == null) continue;
                bool usable = i < grades.Length && _countByGrade[i] > 0;
                gradeToggles[i].SetIsOnWithoutNotify(false);
                gradeToggles[i].interactable = usable;
            }
        }
        if (gradeCountTexts != null) {
            for (int i = 0; i < gradeCountTexts.Length; i++) {
                if (gradeCountTexts[i] == null) continue;
                gradeCountTexts[i].text = i < grades.Length ? _countByGrade[i].ToString("N0") : "0";
            }
        }

        RefreshSummary();

        _result = new UniTaskCompletionSource<IReadOnlyList<Grade>>();
        await base.ShowAsync();
        return await _result.Task;
    }

    // 스토어를 한 번 훑어 등급별 대상 개수와 보상 골드를 센다.
    private void CountTargets() {
        var grades = EquipmentRules.DisassembleGrades;
        for (int i = 0; i < _countByGrade.Length; i++) { _countByGrade[i] = 0; _goldByGrade[i] = 0; }

        foreach (var kv in _hud.Store.Equipment) {
            var inst = kv.Value;
            var data = _hud.EquipDb != null ? _hud.EquipDb.Get(inst.EquipDataId) : null;
            if (!EquipmentRules.IsBulkDisassembleTarget(inst, data)) continue;

            int index = System.Array.IndexOf(grades, data.Grade);
            if (index < 0 || index >= _countByGrade.Length) continue;   // 분해 대상이 아닌 등급(XR)

            _countByGrade[index]++;
            _goldByGrade[index] += EquipmentRules.DisassembleGold(data.Grade);
        }
    }

    // 고른 등급들의 합계를 표시하고, 하나도 고르지 않았으면 확인을 막는다.
    private void RefreshSummary() {
        int count = 0;
        long gold = 0;
        foreach (int index in SelectedIndices()) {
            count += _countByGrade[index];
            gold += _goldByGrade[index];
        }

        if (targetCountText != null) targetCountText.text = $"{count:N0}개";
        // 골드는 int 상한을 넘길 수 있으므로(SSR 이 1개 10만) 표시 단계에서 상한을 넘지 않게 잘라 둔다.
        // 실제 지급도 서버가 같은 상한에서 멈춘다.
        if (rewardGoldText != null)
            rewardGoldText.text = (gold > int.MaxValue ? int.MaxValue : gold).ToString("N0");
        if (okButton != null) okButton.interactable = count > 0;
    }

    // 켜져 있고 실제로 대상이 있는 토글의 인덱스.
    private IEnumerable<int> SelectedIndices() {
        if (gradeToggles == null) yield break;
        int limit = Mathf.Min(gradeToggles.Length, _countByGrade.Length);
        for (int i = 0; i < limit; i++) {
            if (gradeToggles[i] == null || !gradeToggles[i].isOn) continue;
            if (_countByGrade[i] <= 0) continue;
            yield return i;
        }
    }

    private void OnOk() => OnOkAsync().Forget();

    private async UniTaskVoid OnOkAsync() {
        var grades = EquipmentRules.DisassembleGrades;
        var selected = new List<Grade>();
        bool includesTopGrade = false;

        foreach (int index in SelectedIndices()) {
            selected.Add(grades[index]);
            // 최상위 분해 등급(현재 SSR)은 한 개당 값이 커서 실수로 날리면 타격이 크다.
            // 등급을 고정값으로 비교하지 않고 표의 마지막 항목으로 판단해, 나중에 등급이 늘어도 경고가 따라온다.
            if (index == grades.Length - 1) includesTopGrade = true;
        }
        if (selected.Count == 0) return;

        if (includesTopGrade) {
            var confirm = await _hud.Ui.ShowPopupAsync<ConfirmCancelPopup>(_hud.Popups.ConfirmCancelPopup);
            bool ok = await confirm.ShowAsync("장비 분해",
                $"{grades[grades.Length - 1]} 장비가 포함되어 있습니다.\n정말 분해하시겠습니까?");
            if (!ok) return;
        }

        Resolve(selected);
    }

    private void Resolve(IReadOnlyList<Grade> grades) {
        _result?.TrySetResult(grades);
        CloseAsync().Forget();
    }

    // 버튼 외의 경로(배경 탭, 일괄 닫힘)로 닫혀도 반드시 결과를 확정한다(취소=빈 목록).
    protected override void OnClosed() {
        _result?.TrySetResult(Nothing);
    }
}
