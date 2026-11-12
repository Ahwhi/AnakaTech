using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사용 가능한 소모품 1칸. 아이콘/보유 개수만 보여주는 순수 뷰이며, 어떤 아이템이 목록에 오르는지는 상위 패널이 정한다.
/// 아이템 종류를 전혀 모르기 때문에 새 아이템이 추가돼도 이 스크립트는 그대로 쓴다.
/// </summary>
public sealed class ItemSlotView : MonoBehaviour {
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("보유 개수(예: x3).")]
    [SerializeField] private TMP_Text countText;
    [Tooltip("아이콘 위 짧은 문구(예: 10%). ItemData.BadgeText 가 비어 있으면 숨긴다.")]
    [SerializeField] private TMP_Text badgeText;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;

    public ItemData Data { get; private set; }

    public void Bind(ItemData data, int count, Action<ItemSlotView> onClick) {
        Data = data;

        if (icon != null) {
            icon.enabled = data != null && data.Icon != null;
            if (data != null && data.Icon != null) icon.sprite = data.Icon;
        }
        if (nameText != null) nameText.text = data != null ? data.DisplayName : "";
        if (countText != null) countText.text = $"x{count:N0}";
        if (badgeText != null) {
            string badge = data != null ? data.BadgeText : "";
            badgeText.gameObject.SetActive(!string.IsNullOrEmpty(badge));
            badgeText.text = badge;
        }

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    public void SetSelected(bool selected) {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }
}
