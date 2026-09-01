using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장착 슬롯(무기/방어구/신발/악세서리 중 하나). 장착된 장비를 보여주거나, 비었으면 +버튼을 표시한다.
/// 클릭 시 팝업에 통지한다(빈 슬롯이면 그 부위로 가방을 열고, 장착 슬롯이면 상세 패널을 연다).
/// </summary>
public sealed class EquipmentSlotView : MonoBehaviour {
    [Tooltip("이 슬롯이 담당하는 장비 타입.")]
    [SerializeField] private EquipmentSlot slot;
    [SerializeField] private Image icon;
    [Tooltip("이름 표시 묶음(배경/텍스트 등). 빈 슬롯이면 통째로 숨김. 선택.")]
    [SerializeField] private GameObject nameGroup;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("레벨 표시 묶음(배경/텍스트 등). 빈 슬롯이면 통째로 숨김. 선택.")]
    [SerializeField] private GameObject levelGroup;
    [Tooltip("장착된 장비의 등급 표시(GradeText). 빈 슬롯이면 숨김. 선택.")]
    [SerializeField] private GradeText gradeText;
    [Tooltip("각성 단계(별) 표시. 빈 슬롯이면 숨김. 선택.")]
    [SerializeField] private StarLevelView starView;
    [Tooltip("레벨 텍스트(예: Lv.24). 빈 슬롯이면 숨김. 선택.")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("이 캐릭터 '본인의' 전용무기를 착용 중일 때만 켜지는 표식(= 전용무기 보너스가 붙는 상태). 선택.")]
    [SerializeField] private GameObject signatureBadge;
    [Tooltip("지금 바로 각성할 수 있는 상태(별레벨 여유 + 재료 + 다이아 충족)일 때 켜지는 표식. 선택.\n"
           + "판정은 슬롯을 채우는 쪽(EquipmentPopup)이 하고 SetAwakenReady 로 켜 준다.")]
    [SerializeField] private GameObject awakenReadyBadge;
    [Tooltip("비었을 때 표시하는 + 오브젝트.")]
    [SerializeField] private GameObject emptyPlus;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;

    public EquipmentSlot Slot => slot;
    public EquipmentInstanceDto Inst { get; private set; }

    /// <param name="viewerCharDataId">이 장비 화면의 캐릭터 id. 전용무기 표식 판정에 쓴다(0=판정 안 함).</param>
    public void Bind(EquipmentInstanceDto inst, EquipmentData data, int viewerCharDataId, Action<EquipmentSlotView> onClick) {
        Inst = inst;
        bool empty = inst == null || data == null;

        if (emptyPlus != null) emptyPlus.SetActive(empty);
        if (icon != null) {
            icon.enabled = !empty && data.Icon != null;
            if (!empty && data.Icon != null) icon.sprite = data.Icon;
        }
        if (nameGroup != null) nameGroup.SetActive(!empty);
        if (nameText != null) nameText.text = empty ? "" : data.DisplayName;
        if (levelGroup != null) levelGroup.SetActive(!empty);
        if (gradeText != null) {
            gradeText.gameObject.SetActive(!empty);
            if (!empty) gradeText.SetGrade(data.Grade);
        }
        if (starView != null) {
            starView.gameObject.SetActive(!empty);
            if (!empty) starView.SetStarLevel(inst.StarLevel);
        }
        if (levelText != null) {
            levelText.gameObject.SetActive(!empty);
            if (!empty) levelText.text = $"Lv.{inst.Level}";
        }
        if (signatureBadge != null) signatureBadge.SetActive(!empty && data.IsOwnSignatureOf(viewerCharDataId));
        // 각성 가능 표식은 보유 재료와 다이아까지 봐야 하므로 상위가 SetAwakenReady 로 켜 준다.
        // 여기서는 먼저 꺼 두어 빈 슬롯이나 교체 직후에 옛 표식이 남지 않게 한다.
        if (awakenReadyBadge != null) awakenReadyBadge.SetActive(false);

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    public void SetSelected(bool selected) {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }

    /// <summary>각성 가능 표식. Bind 로 꺼진 뒤 상위가 판정 결과로 켜 준다.</summary>
    public void SetAwakenReady(bool ready) {
        if (awakenReadyBadge != null) awakenReadyBadge.SetActive(ready);
    }
}
