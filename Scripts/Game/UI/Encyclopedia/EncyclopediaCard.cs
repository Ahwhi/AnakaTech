using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 도감 그리드 카드. 미보유 시 검은 반투명 잠금 오버레이 + 미보유 표시를 켜고 클릭을 막는다(PLANNING 9).
/// </summary>
public sealed class EncyclopediaCard : MonoBehaviour {
    [FormerlySerializedAs("portraitView")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("등급 표시(GradeText). 색 적용 여부는 컴포넌트의 applyColor 체크로 결정.")]
    [SerializeField] private GradeText gradeText;
    [Tooltip("등급별로 그림/색이 바뀌는 이미지들(프레임/배경/글로우 등). 각 오브젝트에 GradeGraphicStyle 을 붙여 지정. 비우면 없음.")]
    [SerializeField] private GradeGraphicStyle[] gradeStyles;
    [Tooltip("미보유 시 켜지는 검은 반투명 패널(미보유 텍스트 포함).")]
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Button button;

    private CharacterData _data;

    public CharacterData Data => _data;

    public void Bind(CharacterData data, bool owned, Action<EncyclopediaCard> onClick) {
        _data = data;
        if (illustrationView != null) illustrationView.Bind(data);
        if (nameText != null) nameText.text = data.DisplayName;
        if (gradeText != null) gradeText.SetGrade(data.Grade);
        if (gradeStyles != null) {
            foreach (var style in gradeStyles) {
                if (style != null) style.SetGrade(data.Grade);
            }
        }
        if (lockedOverlay != null) lockedOverlay.SetActive(!owned);

        if (button != null) {
            button.interactable = owned;   // 미보유는 클릭 불가
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }
}
