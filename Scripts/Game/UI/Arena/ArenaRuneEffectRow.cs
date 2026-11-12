using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 룬 장착 팝업의 효과 목록 1줄(장착한 룬 1개 = 계열 1개). 아이콘 + 계열 이름 + 효과 값 + 코스트.
/// 팝업이 계열마다 한 줄씩 만들어 두고, 장착 여부에 따라 켜고 끈다.
/// </summary>
public sealed class ArenaRuneEffectRow : MonoBehaviour {
    [Tooltip("선택. 계열 아이콘(팝업의 계열별 아이콘이 들어간다).")]
    [SerializeField] private Image icon;
    [Tooltip("계열 이름(예: 공격력). 계열 색으로 칠한다.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("효과 값(예: x1.5 / +10%).")]
    [SerializeField] private TMP_Text effectText;
    [Tooltip("선택. 코스트(예: 3코스트).")]
    [SerializeField] private TMP_Text costText;

    public ArenaRuneRules.Family Family { get; private set; }

    /// <summary>계열을 정하고 이름/아이콘처럼 바뀌지 않는 부분을 그린다.</summary>
    public void Bind(ArenaRuneRules.Family family, Sprite sprite) {
        Family = family;
        if (icon != null) {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
        if (nameText != null) {
            nameText.text = ArenaRuneRules.FamilyName(family);
            nameText.color = ArenaRuneRules.ColorOf(family);
        }
    }

    /// <summary>장착 레벨을 그린다. 0 이면 줄을 끈다.</summary>
    public void SetLevel(int level) {
        gameObject.SetActive(level > 0);
        if (level <= 0) return;
        if (effectText != null) effectText.text = ArenaRuneRules.ValueText(Family, level);
        if (costText != null) costText.text = $"{ArenaRuneRules.CostOf(level)}코스트";
    }
}
