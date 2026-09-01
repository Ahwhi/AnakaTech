using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장비 가챠 연출 카드 1장. 장비 아이콘 + 등급/이름 + 굴린 능력치 + 신규 배지.
/// 장비는 개체별로 능력치가 다르므로(같은 장비여도 수치가 다름) 뽑은 즉시 능력치를 보여줘야 유저가 결과를 판단할 수 있다.
/// 캐릭터 가챠 카드(GachaCardView)의 장비 버전으로, 동일한 드롭 연출에서 재사용된다.
/// </summary>
public sealed class EquipGachaCardView : MonoBehaviour {
    [SerializeField] private Image icon;
    [Tooltip("등급 표시(GradeText). 색 적용 여부는 컴포넌트의 applyColor 체크로 결정.")]
    [SerializeField] private GradeText gradeText;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("굴린 능력치 목록(주스탯 + 옵션). EquipmentRules.DescribeStats 결과를 표시한다.")]
    [SerializeField] private TMP_Text statsText;
    [Tooltip("전용무기의 주인 캐릭터 표시(예: '시온 전용'). 무기가 아니면 숨긴다. 선택.\n"
           + "모든 무기가 전용무기라 '전용무기인가'는 정보가 되지 않는다 - 누구 것인지를 보여준다.")]
    [SerializeField] private TMP_Text ownerText;
    [Tooltip("신규 획득 시 표시(장비는 항상 새 인스턴스라 기본 표시).")]
    [SerializeField] private GameObject newBadge;

    /// <param name="ownerName">전용무기 주인 캐릭터 이름. 무기가 아니거나 알 수 없으면 빈 문자열.</param>
    public void Bind(EquipmentData data, EquipRollDto roll, string ownerName, bool isNew) {
        if (icon != null) {
            icon.sprite = data != null ? data.Icon : null;
            icon.enabled = data != null && data.Icon != null;
        }
        if (gradeText != null && data != null) gradeText.SetGrade(data.Grade);
        if (nameText != null) nameText.text = data != null ? data.DisplayName : "";
        if (ownerText != null) {
            bool show = !string.IsNullOrEmpty(ownerName);
            ownerText.gameObject.SetActive(show);
            if (show) ownerText.text = $"{ownerName} 전용";
        }
        if (newBadge != null) newBadge.SetActive(isNew);

        if (statsText != null) {
            statsText.text = data != null && roll != null
                ? EquipmentRules.DescribeStats(data.Slot, data.Grade, roll.Level, roll.MainPoints, roll.OptionPoints)
                : "";
        }
    }
}
