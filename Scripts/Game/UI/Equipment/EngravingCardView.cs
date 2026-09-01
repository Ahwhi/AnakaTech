using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 각인 목록의 카드 1장(각인 선택 팝업에서 사용). 보유 여부/레벨/효과 요약을 보여준다.
/// 미보유 각인도 회색으로 함께 보여줘서 "무엇을 모으면 되는지"가 목록에서 바로 보이게 한다.
/// </summary>
public sealed class EngravingCardView : MonoBehaviour {
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("계열 표시(공격/생존/기교). 선택.")]
    [SerializeField] private TMP_Text categoryText;
    [Tooltip("각인 레벨(예: Lv.4). 미보유면 숨김.")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("현재 레벨 기준 효과 요약. 미보유면 레벨 1 기준으로 보여준다.")]
    [SerializeField] private TMP_Text effectText;
    [Tooltip("계열 색을 입힐 테두리(선택).")]
    [SerializeField] private Image frame;
    [Tooltip("미보유일 때 켜는 오브젝트(자물쇠/미획득 표시 등). 선택.")]
    [SerializeField] private GameObject lockedGroup;
    [Tooltip("이 캐릭터의 어느 슬롯에든 장착 중일 때 켜는 표식(지금 보고 있는 슬롯 포함). 선택.")]
    [SerializeField] private GameObject equippedBadge;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;

    public EngravingData Data { get; private set; }

    /// <param name="level">보유 레벨(0=미보유).</param>
    /// <param name="equipped">이 캐릭터의 어느 슬롯에든 장착 중인지(표식 전용).</param>
    /// <param name="blocked">다른 슬롯에 장착 중이라 이 슬롯에는 고를 수 없는지(중복 장착 불가).
    /// 표식과 선택 가능 여부를 나누는 이유: 지금 슬롯에 낀 각인도 표식은 보여야 하지만 해제하려면 고를 수 있어야 한다.</param>
    public void Bind(EngravingData data, int level, bool equipped, bool blocked, Action<EngravingCardView> onClick) {
        Data = data;
        bool owned = level > 0;

        if (icon != null) {
            icon.enabled = data != null && data.Icon != null;
            if (data != null && data.Icon != null) icon.sprite = data.Icon;
            // 미보유는 어둡게 눌러 "아직 못 얻은 것"임을 한눈에 보이게 한다.
            icon.color = owned ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }
        if (nameText != null) nameText.text = data != null ? data.DisplayName : "";
        if (categoryText != null) categoryText.text = data != null ? EngravingRules.CategoryName(data.Category) : "";
        if (levelText != null) {
            levelText.gameObject.SetActive(owned);
            if (owned) levelText.text = $"Lv.{level}";
        }
        if (effectText != null) effectText.text = data != null ? data.EffectSummary(owned ? level : 1) : "";
        if (frame != null && data != null) frame.color = data.ThemeColor;
        if (lockedGroup != null) lockedGroup.SetActive(!owned);
        if (equippedBadge != null) equippedBadge.SetActive(equipped);

        if (button != null) {
            button.onClick.RemoveAllListeners();
            // 미보유/중복 장착은 고를 수 없다(서버도 같은 기준으로 거부한다).
            button.interactable = owned && !blocked;
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    public void SetSelected(bool selected) {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }
}
