using TMPro;
using UnityEngine;

/// <summary>
/// 편성원 1칸 미리보기(대표 캐릭터 일러 + 별레벨 + 레벨). 편성을 "보여 주기만" 하는 자리에서 공용으로 쓴다.
///
/// 일러와 별/레벨을 한 컴포넌트로 묶은 이유는 세 값이 항상 같은 캐릭터를 가리켜야 하기 때문이다.
/// 프리팹에서 배열 여러 개(일러 5칸 / 별 5칸 / 레벨 5칸)로 두면 인덱스가 어긋났을 때 조용히 틀린 값이 붙는다.
/// </summary>
public sealed class DeckMemberPreview : MonoBehaviour {
    [Tooltip("캐릭터 일러 슬롯. 원본이 선언한 관심점(UiFocus)으로 프레이밍되므로 raw Image 가 아니라 이 컴포넌트를 쓴다.")]
    [SerializeField] private CharacterIllustrationView portraitView;
    [Tooltip("별레벨 표시(선택). 덱 카드/상세 패널과 같은 공용 위젯이라 별 스타일이 저절로 통일된다.")]
    [SerializeField] private StarLevelView starView;
    [Tooltip("캐릭터 레벨 표시(선택, 예: Lv.30). 레벨을 넘기지 않는 화면에서는 비워 둔다.")]
    [SerializeField] private TMP_Text levelText;

    /// <summary>
    /// 이 칸에 캐릭터를 표시한다. 편성 인원이 5명보다 적을 수 있으므로 빈 칸은 Clear 로 끈다.
    /// </summary>
    /// <param name="appearance">표시할 외형(기본 일러스트 또는 장착 코스튬). 내 편성이든 남의 편성이든
    /// 부르는 쪽이 CharacterAppearanceService 로 만들어 넘기므로, 이 위젯은 코스튬을 알 필요가 없다.</param>
    /// <param name="level">캐릭터 레벨. 0 이하면 레벨 표시를 끈다(레벨을 모르는 화면에서 "Lv.0" 이 뜨지 않게).</param>
    public void Bind(CharacterAppearance appearance, int starLevel, int level = 0) {
        gameObject.SetActive(true);

        if (portraitView != null) {
            // 외형을 그대로 넘겨야 관심점/프레이밍 방식이 함께 적용된다(코스튬은 자기 관심점을 갖는다).
            if (appearance.Character != null) portraitView.Bind(appearance);
            else portraitView.Clear();
        }
        if (starView != null) {
            starView.gameObject.SetActive(true);
            starView.SetStarLevel(starLevel);
        }
        if (levelText != null) {
            levelText.gameObject.SetActive(level > 0);
            if (level > 0) levelText.text = $"{level}";
        }
    }

    /// <summary>빈 칸으로 만든다(편성 인원이 칸 수보다 적을 때).</summary>
    public void Clear() {
        if (portraitView != null) portraitView.Clear();
        if (starView != null) starView.gameObject.SetActive(false);
        if (levelText != null) levelText.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
}
