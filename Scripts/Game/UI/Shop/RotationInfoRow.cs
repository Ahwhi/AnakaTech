using TMPro;
using UnityEngine;

/// <summary>로테이션 안내 팝업의 한 줄(진행상태/구분/일러스트/등급/이름/기간). RotationInfoPopup 이 픽업마다 생성한다.</summary>
public sealed class RotationInfoRow : MonoBehaviour {
    [Tooltip("진행 상태 텍스트. 표시할 문구는 RotationInfoPopup 이 정한다('진행 중' / '다음' / '진행 중 (교체 예정 없음)').")]
    [SerializeField] private TMP_Text statusText;
    [Tooltip("진행 중일 때 statusText 색.")]
    [SerializeField] private Color currentColor = new Color(1f, 0.78f, 0.25f);
    [Tooltip("예정일 때 statusText 색.")]
    [SerializeField] private Color upcomingColor = new Color(0.65f, 0.65f, 0.65f);

    [Tooltip("픽업 구분 텍스트. '캐릭터' 또는 '무기'.")]
    [SerializeField] private TMP_Text kindText;
    [Tooltip("픽업 대상 일러스트 슬롯. 캐릭터 일러스트와 무기 아이콘이 섞여 들어오며, 각자 맞는 프레이밍은 원본 데이터가 결정한다.")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [Tooltip("등급 표시(GradeColorCatalog 색 적용). TMP 대신 GradeText 컴포넌트 사용.")]
    [SerializeField] private GradeText gradeText;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("픽업 기간(KST).")]
    [SerializeField] private TMP_Text periodText;

    /// <summary>한 줄을 채운다. art 는 캐릭터/장비 데이터 그대로 넘기며, 슬롯이 알아서 프레이밍한다.</summary>
    public void Bind(string status, bool isCurrent, string kind, string itemName, Grade grade, string period,
                     IIllustrationSource art) {
        if (statusText != null) {
            statusText.text = status;
            statusText.color = isCurrent ? currentColor : upcomingColor;
        }
        if (kindText != null) kindText.text = kind;
        if (nameText != null) nameText.text = itemName;
        if (gradeText != null) gradeText.SetGrade(grade);
        if (periodText != null) periodText.text = period;
        if (illustrationView != null) illustrationView.Bind(art);
    }
}
