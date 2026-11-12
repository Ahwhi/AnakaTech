using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마일리지 픽업 그리드의 카드. 도감 카드(EncyclopediaCard)와 같은 부품으로 그리므로 프리팹을 복제해
/// 이 컴포넌트로 바꿔 달면 된다.
///
/// 도감과 달리 미보유도 누를 수 있어야 한다. 마일리지 픽업은 "아직 없는 소녀를 받는" 장치라
/// 미보유가 오히려 고르고 싶은 대상이기 때문이다. 그래서 잠금 처리 대신 보유 여부를 안내로만 알린다
/// (이미 보유한 소녀를 고르면 중복 규칙대로 조각이 들어오므로 그 사실을 미리 보여 준다).
/// </summary>
public sealed class MileagePickCard : MonoBehaviour {
    [SerializeField] private CharacterIllustrationView illustrationView;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("등급 표시(GradeText). 도감 카드와 같은 컴포넌트다.")]
    [SerializeField] private GradeText gradeText;
    [Tooltip("등급별로 그림/색이 바뀌는 이미지들. 각 오브젝트에 GradeGraphicStyle 을 붙여 지정. 비우면 없음.")]
    [SerializeField] private GradeGraphicStyle[] gradeStyles;

    [Header("Owned")]
    [Tooltip("이미 보유한 소녀일 때 켜지는 묶음(예: '보유 중' 딱지). 선택.")]
    [SerializeField] private GameObject ownedGroup;
    [Tooltip("보유 여부 안내 문구. 보유 중이면 중복으로 조각이 들어온다는 것을 알린다. 선택.")]
    [SerializeField] private TMP_Text ownedText;

    [Header("Selection")]
    [Tooltip("선택된 카드에 켜지는 표시(테두리 등). 선택.")]
    [SerializeField] private GameObject selectedMark;
    [SerializeField] private Button button;

    private CharacterData _data;

    public CharacterData Data => _data;

    public void Bind(CharacterData data, bool owned, int dupShards, Action<MileagePickCard> onClick) {
        _data = data;
        if (illustrationView != null) illustrationView.Bind(data);
        if (nameText != null) nameText.text = data.DisplayName;
        if (gradeText != null) gradeText.SetGrade(data.Grade);
        if (gradeStyles != null)
            foreach (var style in gradeStyles)
                if (style != null) style.SetGrade(data.Grade);

        if (ownedGroup != null) ownedGroup.SetActive(owned);
        if (ownedText != null) {
            ownedText.gameObject.SetActive(owned);
            ownedText.text = $"보유 중 (조각 {dupShards}개)";
        }

        if (button != null) {
            button.interactable = true;   // 미보유도 고를 수 있다(도감 카드와 다른 점).
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }

        SetSelected(false);
    }

    public void SetSelected(bool on) {
        if (selectedMark != null) selectedMark.SetActive(on);
    }
}
