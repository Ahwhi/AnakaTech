using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 출석 그리드의 하루 칸. 일차 번호 + 상태(잠김/오늘 수령 가능/수령 완료) + 보상 아이콘/수량을 표시한다.
/// 보상 아이콘/수량은 페이지가 서버 보상 데이터로 해석해 넣어준다(칸은 표시만 담당, 디커플링).
/// </summary>
public sealed class AttendanceDayCell : MonoBehaviour {
    public enum CellState {
        Locked,     // 아직 도달하지 않은 날
        Claimable,  // 오늘 받을 수 있는 날
        Claimed     // 이미 받은 날
    }

    [SerializeField] private TextMeshProUGUI dayText;
    [Tooltip("수령 완료 표시(체크 등).")]
    [SerializeField] private GameObject claimedMark;
    [Tooltip("오늘 받을 수 있는 날 강조.")]
    [SerializeField] private GameObject todayHighlight;
    [Header("Reward")]
    [Tooltip("보상 아이콘. 없으면 표시 안 함.")]
    [SerializeField] private Image rewardIcon;
    [Tooltip("보상 수량 텍스트. 없으면 표시 안 함.")]
    [SerializeField] private TextMeshProUGUI rewardQtyText;
    [Tooltip("선택. 보상 아이콘 위 마우스오버 시 이름 툴팁을 띄우는 트리거. 비우면 툴팁 없음.")]
    [SerializeField] private TooltipTrigger rewardTooltip;

    public void Set(int day, CellState state, Sprite rewardSprite, string rewardQty, string rewardName = null) {
        if (dayText != null) dayText.text = $"{day.ToString()}일차";
        if (claimedMark != null) claimedMark.SetActive(state == CellState.Claimed);
        if (todayHighlight != null) todayHighlight.SetActive(state == CellState.Claimable);
        if (rewardIcon != null) {
            rewardIcon.sprite = rewardSprite;
            rewardIcon.enabled = rewardSprite != null;
        }
        if (rewardQtyText != null) rewardQtyText.text = rewardQty ?? "";
        if (rewardTooltip != null) rewardTooltip.SetContent(rewardName, null);
    }
}
