using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 길드 마크 선택 슬롯 1칸(관리 탭의 마크 변경 화면).
/// 해금된 마크는 누르면 고른 상태가 되고(적용은 저장 버튼이 한다), 잠긴 마크는 자물쇠 + 가격을 보여 준다(누르면 해금 확인).
/// 어느 쪽인지의 판단과 실제 요청은 콜백을 받은 쪽이 하므로 이 슬롯은 표시와 입력 전달만 한다.
/// </summary>
public sealed class GuildMarkSlot : MonoBehaviour {
    [SerializeField] private Image markImage;
    [Tooltip("선택. 마크 이름.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("지금 고른 마크에 켜지는 표시(아직 저장 전일 수 있다).")]
    [SerializeField] private GameObject selectedIndicator;
    [Tooltip("선택. 지금 길드에 실제로 적용되어 있는 마크에만 켜지는 표시(예: '적용 중' 라벨). 저장 전후를 구분해 준다.")]
    [SerializeField] private GameObject appliedIndicator;
    [Tooltip("잠긴(유료 미해금) 마크에 켜지는 자물쇠 그룹.")]
    [SerializeField] private GameObject lockGroup;
    [Tooltip("잠긴 마크의 해금 비용 표시(선택).")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("비용 재화 아이콘(선택). 스프라이트는 재화 카탈로그(CurrencyCatalog)에서 타입으로 가져오므로 직접 할당하지 않는다.")]
    [SerializeField] private Image costIcon;
    [Tooltip("다이아가 부족할 때 비용 텍스트에 적용할 색.")]
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.20f, 0.20f, 1f);
    [SerializeField] private Button button;

    private int _markId;
    private bool _unlocked;
    // 비용 텍스트의 평상시 색. 프리팹에서 정한 색을 그대로 쓰려고 첫 표시 때 한 번만 기억한다.
    private Color _costDefaultColor = Color.white;
    private bool _costColorCaptured;

    /// <param name="unlocked">지금 고를 수 있는 마크인지(무료이거나 이미 해금).</param>
    /// <param name="currencies">해금 비용 아이콘을 조회할 재화 카탈로그.</param>
    /// <param name="affordable">해금 비용을 지금 낼 수 있는지. 보유량 판단은 컨텍스트를 가진 쪽이 하고 슬롯은 표시만 한다.</param>
    /// <param name="selected">지금 고른 마크인지(저장 전일 수 있다).</param>
    /// <param name="applied">지금 길드에 실제로 적용되어 있는 마크인지.</param>
    /// <param name="onClick">markId 와 해금 여부를 넘긴다. 해금이면 선택, 아니면 해금 확인.</param>
    public void Bind(GuildMarkCatalog.Entry entry, GuildMarkCatalog catalog, CurrencyCatalog currencies,
                     bool unlocked, bool affordable, bool selected, bool applied, Action<int, bool> onClick) {
        if (entry == null) return;
        _markId = entry.markId;
        _unlocked = unlocked;

        if (markImage != null) {
            markImage.sprite = entry.icon != null ? entry.icon : (catalog != null ? catalog.Icon(entry.markId) : null);
            markImage.enabled = markImage.sprite != null;
        }
        if (nameText != null) nameText.text = catalog != null ? catalog.DisplayName(entry.markId) : "";
        if (selectedIndicator != null) selectedIndicator.SetActive(selected);
        if (appliedIndicator != null) appliedIndicator.SetActive(applied);
        if (lockGroup != null) lockGroup.SetActive(!unlocked);
        if (costText != null) {
            if (!_costColorCaptured) { _costDefaultColor = costText.color; _costColorCaptured = true; }
            costText.gameObject.SetActive(!unlocked);
            if (!unlocked) {
                costText.text = GuildRules.MarkUnlockCostDiamond.ToString("N0");
                costText.color = affordable ? _costDefaultColor : insufficientColor;
            }
        }
        if (costIcon != null) {
            CurrencyIcon.Apply(costIcon, currencies, CurrencyType.Diamond);
            costIcon.gameObject.SetActive(!unlocked);
        }

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(_markId, _unlocked));
            // 이미 고른 마크는 다시 누를 이유가 없고, 살 수 없는 잠긴 마크도 누를 이유가 없다(가격이 빨갛게 알려 준다).
            button.interactable = !selected && (unlocked || affordable);
        }
    }
}
