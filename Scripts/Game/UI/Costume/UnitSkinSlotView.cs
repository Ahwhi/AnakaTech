using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아웃핏 목록 1칸(V1.12). 아이콘 + 이름 + 효과 + 남은 개수 + 장착 상태를 보여 준다.
/// "기본" 칸도 같은 프리팹으로 그린다(Entry 가 null 인 칸). 코스튬 칸과 같은 이유로 칸을 나누지 않는다.
/// </summary>
public sealed class UnitSkinSlotView : MonoBehaviour {
    [Tooltip("아웃핏 아이콘. 기본 칸에서는 꺼진다.")]
    [SerializeField] private Image iconImage;
    [Tooltip("아웃핏 이름. 기본 칸에서는 '기본'이 들어간다.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("선택. 장착 효과 요약(예: 최종데미지 +10).")]
    [SerializeField] private TMP_Text effectText;
    [Tooltip("선택. 남은 개수 / 보유 개수(예: 1 / 3). 기본 칸에서는 꺼진다.")]
    [SerializeField] private TMP_Text countText;
    [Tooltip("선택. 이 소녀가 낄 수 없을 때(남은 개수 없음) 켜는 묶음(자물쇠/어둡게 등).")]
    [SerializeField] private GameObject lockedGroup;
    [Tooltip("선택. 이 소녀가 지금 끼고 있는 아웃핏 표식.")]
    [SerializeField] private GameObject equippedMark;
    [Tooltip("선택. 목록에서 지금 고른 칸 표식.")]
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;

    /// <summary>이 칸의 아웃핏. null 이면 기본 아웃핏 칸이다.</summary>
    public UnitSkinCatalog.Entry Entry { get; private set; }

    /// <summary>이 칸이 가리키는 아웃핏 id(기본 칸은 0). 장착 요청에 그대로 쓰인다.</summary>
    public int UnitSkinId => Entry != null ? Entry.id : 0;

    /// <summary>이 소녀가 지금 낄 수 있는지(기본 칸은 항상 가능).</summary>
    public bool Available { get; private set; }

    /// <param name="entry">이 칸의 아웃핏. null 이면 기본 아웃핏 칸이다.</param>
    /// <param name="icon">칸 아이콘(기본 칸은 카탈로그의 기본 아이콘).</param>
    /// <param name="left">남은 개수(다른 소녀가 끼지 않은 개수).</param>
    /// <param name="owned">보유 개수.</param>
    public void Bind(UnitSkinCatalog.Entry entry, Sprite icon, int left, int owned, bool available, bool equipped,
                     Action<UnitSkinSlotView> onClick) {
        Entry = entry;
        Available = available;

        if (iconImage != null) {
            var sprite = icon;
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }
        if (nameText != null) nameText.text = entry != null ? entry.displayName : DefaultLabel;
        if (effectText != null) {
            string summary = entry != null ? entry.EffectSummary() : "";
            effectText.text = string.IsNullOrEmpty(summary) ? CostumeSlotView.NoEffectText : summary;
        }
        if (countText != null) {
            countText.gameObject.SetActive(entry != null);
            countText.text = entry != null ? $"{left} / {owned}" : "";
        }
        if (lockedGroup != null) lockedGroup.SetActive(!available);
        if (equippedMark != null) equippedMark.SetActive(equipped);
        SetSelected(false);

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    public void SetSelected(bool selected) {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }

    /// <summary>기본 아웃핏 칸의 표시 이름.</summary>
    public const string DefaultLabel = "기본";
}
