using TMPro;
using UnityEngine;

/// <summary>
/// 수련 확률표의 한 줄(효과 / 등장 확률 / 획득 가능 수치 / 수치 추첨 확률 / 최종 확률).
/// 값은 전부 TrainingRatesPopup 이 서버가 준 확률표에서 계산해 넘긴다(이 줄은 표시만 한다).
/// </summary>
public sealed class TrainingRateRow : MonoBehaviour {
    [Tooltip("수련 효과 이름(예: 사거리).")]
    [SerializeField] private TMP_Text effectText;
    [Tooltip("효과 등장 확률(예: 20% (1/5)).")]
    [SerializeField] private TMP_Text effectRateText;
    [Tooltip("획득 가능한 수치 범위(예: +0.5 ~ +5.0 / 0.1 단위).")]
    [SerializeField] private TMP_Text valueRangeText;
    [Tooltip("수치 1칸의 추첨 확률(예: 2.1739% (1/46)).")]
    [SerializeField] private TMP_Text valueRateText;
    [Tooltip("개별 최종 결과 확률(예: 0.43478% (1/230)).")]
    [SerializeField] private TMP_Text finalRateText;

    public void Bind(string effect, string effectRate, string valueRange, string valueRate, string finalRate) {
        if (effectText != null) effectText.text = effect;
        if (effectRateText != null) effectRateText.text = effectRate;
        if (valueRangeText != null) valueRangeText.text = valueRange;
        if (valueRateText != null) valueRateText.text = valueRate;
        if (finalRateText != null) finalRateText.text = finalRate;
    }
}
