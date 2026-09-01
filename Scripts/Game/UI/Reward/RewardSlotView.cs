using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "아이콘 + 수량" 슬롯 1개를 표시하는 공용 뷰. 우편함/도감보상/충전보상 등 보상 표시가 주 용도이며,
/// 같은 형태가 필요한 곳(던전 정보의 몬스터 + 마리 수 등)도 그대로 재사용한다.
/// 표시 데이터(RewardVisual)는 상위가 카탈로그로 해석(RewardVisualResolver)해 넘겨주므로, 이 뷰는 품목이 늘어도 수정할 필요가 없다.
/// </summary>
public sealed class RewardSlotView : MonoBehaviour {
    /// <summary>보상 1개의 표시 데이터(아이콘 + 수량 + 툴팁 이름).</summary>
    public readonly struct RewardVisual {
        public readonly Sprite Icon;
        public readonly string Quantity;
        public readonly string Name;         // 마우스오버 툴팁에 표시할 보상 이름(있을 때만).
        public RewardVisual(Sprite icon, string quantity, string name = null) {
            Icon = icon; Quantity = quantity; Name = name;
        }
    }

    [Tooltip("보상 아이콘 이미지.")]
    [SerializeField] private Image iconImage;
    [Tooltip("수량 텍스트. 빈 문자열이면 숨긴다.")]
    [SerializeField] private TMP_Text quantityText;
    [Tooltip("선택. 마우스오버 시 보상 이름 툴팁을 띄우는 트리거(레이캐스트 대상 Graphic 위에 배치). 비우면 툴팁 없음.")]
    [SerializeField] private TooltipTrigger tooltip;

    public void Bind(RewardVisual v) {
        if (iconImage != null) {
            iconImage.sprite = v.Icon;
            iconImage.enabled = v.Icon != null;
        }
        if (quantityText != null) {
            bool show = !string.IsNullOrEmpty(v.Quantity);
            quantityText.gameObject.SetActive(show);
            if (show) quantityText.text = v.Quantity;
        }
        if (tooltip != null) tooltip.SetContent(v.Name, null);
    }
}
