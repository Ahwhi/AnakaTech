using TMPro;
using UnityEngine;

/// <summary>가챠 확률 표시 팝업의 한 줄(아이콘/이름/등급/확률). GachaRatesPopup 이 풀 항목마다 생성한다.</summary>
public sealed class GachaRateRow : MonoBehaviour {
    [Tooltip("항목 아이콘 슬롯. 캐릭터 일러스트와 장비 아이콘이 섞여 들어오며, 각자 맞는 프레이밍은 원본 데이터가 결정한다.")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("등급 표시(GradeColorCatalog 색 적용). TMP 대신 GradeText 컴포넌트 사용.")]
    [SerializeField] private GradeText gradeText;
    [SerializeField] private TMP_Text percentText;

    /// <summary>한 줄을 채운다. art 는 캐릭터/장비 데이터 그대로 넘기며(등급만 표기하는 줄은 null), 슬롯이 알아서 프레이밍한다.</summary>
    public void Bind(string itemName, Grade grade, string percent, IIllustrationSource art) {
        if (nameText != null) nameText.text = itemName;
        if (gradeText != null) gradeText.SetGrade(grade);
        if (percentText != null) percentText.text = percent;
        if (illustrationView != null) illustrationView.Bind(art);
    }
}
