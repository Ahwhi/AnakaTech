using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 우편 보상 1개를 "아이콘 + 수량 텍스트" 한 쌍으로 표시하는 슬롯. 우편에 보상이 여러 개면 이 슬롯이 여러 개 생성된다.
/// 아이콘/수량/이름은 상위(MailboxPopup)가 카탈로그로 해석해 넘겨주므로, 이 컴포넌트는 표시만 담당한다(품목 종류 무관).
/// 마우스오버 툴팁은 다른 보상 슬롯(RewardSlotView)과 같은 방식으로 TooltipTrigger 에 이름을 넘겨 띄운다.
/// </summary>
public sealed class MailRewardView : MonoBehaviour {
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;
    [Tooltip("선택. 마우스오버 시 보상 이름 툴팁을 띄우는 트리거(레이캐스트 대상 Graphic 위에 배치). 비우면 툴팁 없음.")]
    [SerializeField] private TooltipTrigger tooltip;

    public void Bind(Sprite icon, string quantity, string name = null) {
        if (iconImage != null) {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
        if (quantityText != null) quantityText.text = quantity ?? "";
        if (tooltip != null) tooltip.SetContent(name, null);
    }
}
