using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터의 각인 슬롯 1칸. 장착된 각인을 보여주거나, 비었으면 +를, 별레벨이 모자라면 잠금을 표시한다.
/// 클릭 시 상위 패널(EngravingPanel)에 통지한다(잠긴 슬롯은 클릭되지 않는다).
/// </summary>
public sealed class EngravingSlotView : MonoBehaviour {
    [Tooltip("이 슬롯의 인덱스(0 부터 시작). 별레벨 해금 조건이 인덱스로 정해지며 EngravingRules.SlotUnlockStar 와 대응한다.")]
    [SerializeField, Min(0)] private int slotIndex;

    [Header("Filled")]
    [SerializeField] private Image icon;
    [Tooltip("각인 이름. 비었으면 숨김.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("각인 레벨(예: Lv.4). 비었으면 숨김.")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("각인 계열 색을 입힐 테두리(선택). 장착된 각인의 테마 색으로 칠한다.")]
    [SerializeField] private Image frame;

    [Header("Empty / Locked")]
    [Tooltip("비었을 때 표시하는 + 오브젝트.")]
    [SerializeField] private GameObject emptyPlus;
    [Tooltip("별레벨 부족으로 잠겼을 때 켜는 오브젝트.")]
    [SerializeField] private GameObject lockedGroup;
    [Tooltip("잠금 조건 안내(예: 별 5 필요). 선택.")]
    [SerializeField] private TMP_Text lockedText;

    [Header("Interaction")]
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;

    public int SlotIndex => slotIndex;

    /// <summary>장착된 각인 id(0=빈 슬롯).</summary>
    public int EngravingId { get; private set; }

    /// <summary>이 슬롯이 열려 있는지(별레벨 충족).</summary>
    public bool Unlocked { get; private set; }

    /// <param name="data">장착된 각인(없으면 null).</param>
    /// <param name="level">각인 레벨(계정 공용).</param>
    /// <param name="unlocked">별레벨 조건을 충족했는지.</param>
    public void Bind(EngravingData data, int level, bool unlocked, Action<EngravingSlotView> onClick) {
        EngravingId = data != null ? data.Id : 0;
        Unlocked = unlocked;

        bool filled = unlocked && data != null;

        if (lockedGroup != null) lockedGroup.SetActive(!unlocked);
        if (lockedText != null && !unlocked)
            lockedText.text = $"별 {EngravingRules.RequiredStarForSlot(slotIndex)} 필요";

        if (emptyPlus != null) emptyPlus.SetActive(unlocked && data == null);

        if (icon != null) {
            icon.enabled = filled && data.Icon != null;
            if (filled && data.Icon != null) icon.sprite = data.Icon;
        }
        if (nameText != null) {
            nameText.gameObject.SetActive(filled);
            if (filled) nameText.text = data.DisplayName;
        }
        if (levelText != null) {
            levelText.gameObject.SetActive(filled);
            if (filled) levelText.text = $"Lv.{level}";
        }
        if (frame != null && filled) frame.color = data.ThemeColor;

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.interactable = unlocked;
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    public void SetSelected(bool selected) {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }
}
