using TMPro;
using UnityEngine;

/// <summary>역대 전적 한 줄(시즌 1개). 진행 중 시즌은 표시를 따로 켠다.</summary>
public sealed class ArenaHistorySlot : MonoBehaviour {
    [Tooltip("시즌 이름(예: 2026년 11월).")]
    [SerializeField] private TMP_Text seasonText;
    [Tooltip("전적(예: 42전 32승 10패 (76.2%)).")]
    [SerializeField] private TMP_Text recordText;
    [Tooltip("최종 점수.")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("최종 순위(진행 중이면 현재 순위).")]
    [SerializeField] private TMP_Text rankText;
    [Tooltip("선택. 진행 중 시즌에만 켜는 표시(예: '진행 중' 라벨).")]
    [SerializeField] private GameObject currentMark;

    public void Bind(ArenaSeasonRecordDto r) {
        if (seasonText != null) seasonText.text = ArenaRules.SeasonName(r.SeasonKey);
        if (recordText != null) recordText.text = ArenaRules.RecordText(r.Wins, r.Losses, ArenaRules.RateColorLight);
        if (scoreText != null) scoreText.text = ArenaRules.ScoreText(r.Score);
        if (rankText != null) rankText.text = RankingRules.RankText(r.Rank);
        if (currentMark != null) currentMark.SetActive(r.IsCurrent);
    }
}
