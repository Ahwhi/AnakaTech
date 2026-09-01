using TMPro;
using UnityEngine;

/// <summary>
/// 업적 점수 한 줄. 업적 목록(게임/길드/성장/모집 탭)과 요약 탭이 같은 프리팹을 쓴다.
///
/// 두 화면이 보여 주는 것이 "이름 + 점수"로 같고, 목록 쪽만 누적/배점이 더 붙는 형태라
/// 줄 컴포넌트를 나누는 대신 Bind 를 나눴다(요약에서는 누적/배점 칸을 끈다).
/// </summary>
public sealed class AchievementRowView : MonoBehaviour {
    [SerializeField] private TMP_Text nameText;
    [Tooltip("누적 횟수(또는 누적량). 요약 줄에서는 꺼진다.")]
    [SerializeField] private TMP_Text countText;
    [Tooltip("1회당 배점 안내. 요약 줄에서는 꺼진다.")]
    [SerializeField] private TMP_Text pointsText;
    [Tooltip("이 줄의 점수(누적 x 배점).")]
    [SerializeField] private TMP_Text scoreText;

    /// <summary>업적 1건(목록 탭). 누적/배점까지 모두 보여 준다.</summary>
    public void Bind(AchievementEntryDto entry) {
        if (nameText != null) nameText.text = AchievementRules.Name(entry.Metric);
        SetText(countText, AchievementRules.CountText(entry.Metric, entry.Count), true);
        SetText(pointsText, AchievementRules.PointsText(entry.Metric, entry.Points), true);
        if (scoreText != null) scoreText.text = AchievementRules.ScoreText(entry.Score);
    }

    /// <summary>분류 1개의 점수 합(요약 탭). 누적/배점은 항목별 값이라 여기서는 끈다.</summary>
    public void BindCategory(AchievementCategory category, long score) {
        if (nameText != null) nameText.text = AchievementRules.CategoryName(category);
        SetText(countText, "", false);
        SetText(pointsText, "", false);
        if (scoreText != null) scoreText.text = AchievementRules.ScoreText(score);
    }

    private static void SetText(TMP_Text label, string text, bool on) {
        if (label == null) return;
        label.gameObject.SetActive(on);
        if (on) label.text = text;
    }
}
