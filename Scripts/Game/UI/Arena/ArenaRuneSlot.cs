using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 룬 장착 팝업의 룬 한 칸(계열 1 x 레벨 1). 아이콘 + 계열 이름 + 효과 값 + 코스트, 장착 여부만 그린다.
/// 장착 규칙(계열당 1개, 총 코스트)은 팝업이 판단한다. 아이콘은 팝업이 계열별로 넘겨준다.
/// </summary>
public sealed class ArenaRuneSlot : MonoBehaviour {
    [Tooltip("룬 썸네일. 팝업의 계열별 아이콘(본부 노드 아이콘 등)이 들어간다.")]
    [SerializeField] private Image thumbnail;
    [Tooltip("계열 이름(예: 공격력). 계열 색으로 칠한다.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("효과 값(예: x1.5 / +10%).")]
    [SerializeField] private TMP_Text effectText;
    [Tooltip("선택. 코스트 숫자.")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("선택. 계열 색을 입힐 이미지(테두리/배경 등).")]
    [SerializeField] private Graphic familyTint;
    [Tooltip("장착 중일 때 켜는 표시.")]
    [SerializeField] private GameObject equippedMark;
    [Tooltip("선택. 지금 끼면 코스트를 넘는 룬에 켜는 오브젝트들(어둡게 덮는 오버레이, 잠금 아이콘 등).")]
    [SerializeField] private GameObject[] overCostOverlays;
    [SerializeField] private Button button;

    public ArenaRuneRules.Family Family { get; private set; }
    public int Level { get; private set; }

    public void Bind(ArenaRuneRules.Family family, int level, Sprite icon, Action<ArenaRuneSlot> onClick) {
        Family = family;
        Level = level;

        var color = ArenaRuneRules.ColorOf(family);
        if (thumbnail != null) {
            thumbnail.sprite = icon;
            thumbnail.enabled = icon != null;
        }
        if (nameText != null) {
            nameText.text = ArenaRuneRules.FamilyName(family);
            nameText.color = color;
        }
        if (effectText != null) effectText.text = ArenaRuneRules.ValueText(family, level);
        if (costText != null) costText.text = ArenaRuneRules.CostOf(level).ToString();
        if (familyTint != null) familyTint.color = color;

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    /// <summary>장착 상태와 코스트 초과 여부를 다시 그린다(구성이 바뀔 때마다 팝업이 부른다).</summary>
    public void Refresh(bool equipped, bool overCost) {
        if (equippedMark != null) equippedMark.SetActive(equipped);
        if (overCostOverlays == null) return;
        bool show = !equipped && overCost;
        foreach (var o in overCostOverlays) if (o != null) o.SetActive(show);
    }
}
