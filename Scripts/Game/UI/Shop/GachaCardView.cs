using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>가챠 연출 카드 1장. 캐릭터 일러스트(마스킹 프리팹) + 등급/이름 + 신규 배지(PLANNING 8).</summary>
public sealed class GachaCardView : MonoBehaviour {
    [FormerlySerializedAs("portraitView")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [Tooltip("등급 표시(GradeText). 색 적용 여부는 컴포넌트의 applyColor 체크로 결정.")]
    [SerializeField] private GradeText gradeText;
    [Tooltip("등급별로 그림/색이 바뀌는 이미지들(프레임/배경/글로우 등). 각 오브젝트에 GradeGraphicStyle 을 붙여 지정. 비우면 없음.")]
    [FormerlySerializedAs("gradeTints")]
    [SerializeField] private GradeGraphicStyle[] gradeStyles;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("무기 타입 아이콘. 비우면 표시 안 함.")]
    [SerializeField] private Image weaponTypeImage;
    [Tooltip("무기 타입별 아이콘 공용 테이블(SO). 비우면 이미지 숨김.")]
    [SerializeField] private WeaponIconTable weaponIconTable;
    [Tooltip("신규 획득 시 표시.")]
    [SerializeField] private GameObject newBadge;

    [Header("Duplicate Shards (중복 획득)")]
    [Tooltip("중복 획득이라 조각을 받았을 때만 켜는 그룹(조각 일러스트/이름/수량을 담는 루트). 비우면 조각 표시 없음.")]
    [SerializeField] private GameObject shardGroup;
    [Tooltip("조각 일러스트 슬롯. 육성 팝업 별 페이지와 같은 규약으로 캐릭터 일러스트를 조각 이미지로 쓴다. 선택.")]
    [SerializeField] private CharacterIllustrationView shardIllustrationView;
    [Tooltip("조각 이름(예: '시온 조각'). 선택.")]
    [SerializeField] private TMP_Text shardNameText;
    [Tooltip("획득 조각 수량(예: +5).")]
    [SerializeField] private TMP_Text shardCountText;

    /// <summary>카드 1장을 채운다. 신규면 신규 배지를, 중복이면 획득 조각(일러스트 + 수량)을 보여준다.</summary>
    public void Bind(CharacterData data, bool isNew, int shardsGained) {
        if (illustrationView != null) illustrationView.Bind(data);
        if (gradeText != null) gradeText.SetGrade(data.Grade);
        if (gradeStyles != null) {
            foreach (var style in gradeStyles) {
                if (style != null) style.SetGrade(data.Grade);
            }
        }
        if (nameText != null) nameText.text = data.DisplayName;
        if (weaponTypeImage != null) {
            var icon = weaponIconTable != null ? weaponIconTable.Get(data.WeaponType) : null;
            weaponTypeImage.sprite = icon;
            weaponTypeImage.enabled = icon != null;
        }
        if (newBadge != null) newBadge.SetActive(isNew);

        // 중복 획득은 캐릭터 대신 조각을 받는다(등급별 지급량은 서버가 확정해 내려준다).
        // 표현 규약은 육성 팝업 별 페이지와 동일하게 "캐릭터 일러스트 = 그 캐릭터의 조각"으로 맞춘다.
        bool showShards = !isNew && shardsGained > 0;
        if (shardGroup != null) shardGroup.SetActive(showShards);
        if (!showShards) return;

        if (shardIllustrationView != null) shardIllustrationView.Bind(data);
        if (shardNameText != null) shardNameText.text = $"{data.DisplayName} 조각";
        if (shardCountText != null) shardCountText.text = $"+{shardsGained}";
    }
}
