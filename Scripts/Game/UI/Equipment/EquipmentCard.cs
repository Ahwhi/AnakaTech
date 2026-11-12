using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장비 카드 1칸(가방 목록 / 각성 재료 선택 / 상세 미리보기에서 공용). 아이콘/이름/등급/별레벨/레벨 + 선택 프레임.
/// 각성 재료 선택에서도 그대로 재사용하므로 선택 상태 표시(SetSelected)를 외부에서 제어한다.
/// </summary>
public sealed class EquipmentCard : MonoBehaviour {
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("등급 표시(GradeText). 색 적용 여부는 컴포넌트의 applyColor 체크로 결정. 선택.")]
    [SerializeField] private GradeText gradeText;
    [Tooltip("각성 단계(별) 표시. 캐릭터와 동일한 공용 컴포넌트를 재사용한다. 선택.")]
    [SerializeField] private StarLevelView starView;
    [Tooltip("레벨 텍스트(예: Lv.24). 선택.")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("주스탯/옵션 목록(상세 패널과 같은 문구). 각성 재료처럼 카드에서 바로 옵션을 비교해야 하는 " +
             "프리팹에서만 할당한다. 가방처럼 카드가 많이 깔리는 목록에서는 비워둘 것(카드마다 문자열 조립 비용). 선택.")]
    [SerializeField] private TMP_Text statsText;
    [Tooltip("지금 보고 있는 캐릭터 '본인의' 전용무기일 때만 켜지는 표식. 선택.\n"
           + "모든 무기가 전용무기이므로 '무기인가'로 판정하면 전부 켜진다 - 주인 일치로 판정해야 한다.")]
    [SerializeField] private GameObject signatureBadge;
    [Tooltip("지금 보고 있는 캐릭터에게 전투력 상승이 가장 큰 장비일 때만 켜지는 표식(추천 표시). 선택.\n"
           + "판정은 목록을 만드는 쪽(가방 패널)이 하고 SetBestUpgrade 로 켜 준다 - 카드 혼자서는 알 수 없다.")]
    [SerializeField] private GameObject bestUpgradeBadge;
    [Tooltip("한계돌파한 장비일 때만 켜지는 그룹(마크/테두리 등). 선택.")]
    [SerializeField] private GameObject limitBreakGroup;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;

    public EquipmentInstanceDto Inst { get; private set; }
    public EquipmentData Data { get; private set; }

    /// <param name="viewerCharDataId">지금 장비 화면을 보고 있는 캐릭터 id. 전용무기 표식 판정에 쓴다(0=판정 안 함).</param>
    public void Bind(EquipmentInstanceDto inst, EquipmentData data, int viewerCharDataId, Action<EquipmentCard> onClick) {
        Inst = inst;
        Data = data;

        if (icon != null) {
            icon.sprite = data != null ? data.Icon : null;
            icon.enabled = data != null && data.Icon != null;
        }
        if (gradeText != null) {
            bool showGrade = data != null;
            gradeText.gameObject.SetActive(showGrade);
            if (showGrade) gradeText.SetGrade(data.Grade, inst != null && inst.LimitBroken);
        }
        if (nameText != null) {
            // AutoSize 는 줄바꿈이 켜져 있으면 폰트를 줄이기 전에 먼저 줄을 넘긴다. 줄바꿈을 꺼서
            // 공간이 부족하면 줄을 늘리지 않고 폰트만 작아지도록(항상 한 줄) 고정한다.
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.text = data != null ? data.DisplayName : $"#{(inst != null ? inst.EquipDataId : 0)}";
        }
        if (starView != null) starView.SetStarLevel(inst != null ? inst.StarLevel : 1);
        if (levelText != null) levelText.text = inst != null ? $"Lv.{inst.Level}" : "";
        // 할당된 프리팹에서만 옵션을 그린다(미할당이면 널 체크 한 번으로 끝나 목록 비용이 늘지 않는다).
        if (statsText != null) statsText.text = EquipmentRules.DescribeStats(inst, data);
        if (signatureBadge != null)
            signatureBadge.SetActive(data != null && data.IsOwnSignatureOf(viewerCharDataId));
        // 풀에서 재사용한 카드에 옛 상태가 남지 않도록 매번 확정한다.
        if (limitBreakGroup != null) limitBreakGroup.SetActive(inst != null && inst.LimitBroken);
        // 추천 표식은 목록 쪽이 켜 준다. 풀에서 재사용한 카드에 옛 표식이 남지 않도록 여기서 먼저 끈다.
        if (bestUpgradeBadge != null) bestUpgradeBadge.SetActive(false);

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    public void SetSelected(bool selected) {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }

    /// <summary>전투력 상승이 가장 큰 장비 표식. Bind 로 꺼진 뒤 목록 쪽에서 한 장에만 켜 준다.</summary>
    public void SetBestUpgrade(bool best) {
        if (bestUpgradeBadge != null) bestUpgradeBadge.SetActive(best);
    }
}
