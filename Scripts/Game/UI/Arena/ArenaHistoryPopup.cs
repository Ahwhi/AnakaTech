using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 쟁탈전 역대 전적 팝업(V1.12). 맨 위가 진행 중 시즌, 그 아래로 지난 시즌이 최신순으로 이어진다.
/// 총 전적은 받은 시즌들의 승/패를 합산한 것이다(서버는 시즌별 기록만 준다).
/// 시즌 기록은 V1.12 이후(2026년 11월 시즌)부터 쌓인다.
/// </summary>
public sealed class ArenaHistoryPopup : UIView {
    [Tooltip("시즌 줄이 생성될 부모(스크롤뷰 Content).")]
    [SerializeField] private Transform content;
    [SerializeField] private ArenaHistorySlot slotPrefab;
    [Header("Totals")]
    [Tooltip("통산 시즌(한 판이라도 한 시즌 수, 예: 2시즌).")]
    [SerializeField] private TMP_Text totalSeasonText;
    [Tooltip("통산 기록(모든 시즌 승/패 합산, 예: 120전 80승 40패 (66.7%)).")]
    [SerializeField] private TMP_Text totalRecordText;
    [Tooltip("평균 점수(한 판이라도 한 시즌의 점수 평균). 없으면 -.")]
    [SerializeField] private TMP_Text averageScoreText;
    [Tooltip("평균 순위(순위가 있는 시즌만, 진행 중 시즌은 현재 순위로 셈). 없으면 -.")]
    [SerializeField] private TMP_Text averageRankText;
    [Tooltip("선택. 기록이 하나도 없을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyLabel;
    [Tooltip("선택. 불러오는 동안 켜는 그룹.")]
    [SerializeField] private GameObject loadingGroup;
    [SerializeField] private Button closeButton;

    private readonly List<ArenaHistorySlot> _slots = new();

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    public async UniTask ShowAsync(MainHudContext ctx) {
        Clear();
        if (emptyLabel != null) emptyLabel.SetActive(false);
        if (loadingGroup != null) loadingGroup.SetActive(true);
        SetTotals(0, 0, 0, 0, 0, 0);

        await base.ShowAsync();

        var ct = this.GetCancellationTokenOnDestroy();
        try {
            Build(await ctx.Arena.RequestHistoryAsync(ct));
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            ctx.Ui.ShowToast("역대 전적을 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
    }

    private void Build(List<ArenaSeasonRecordDto> seasons) {
        Clear();
        int wins = 0, losses = 0, shown = 0, played = 0, rankedSeasons = 0;
        long score = 0, rankSum = 0;
        if (seasons != null) {
            foreach (var r in seasons) {
                bool hasGames = r.Wins + r.Losses > 0;
                if (hasGames) {
                    played++;
                    wins += r.Wins;
                    losses += r.Losses;
                    score += r.Score;
                    if (r.Rank > 0) { rankSum += r.Rank; rankedSeasons++; }
                }
                // 진행 중 시즌은 아직 한 판도 안 했어도 보여 준다(이번 달 기록이 여기 쌓인다는 안내).
                if (!r.IsCurrent && !hasGames) continue;
                if (content == null || slotPrefab == null) continue;
                var slot = Instantiate(slotPrefab, content);
                slot.Bind(r);
                _slots.Add(slot);
                shown++;
            }
        }
        SetTotals(played, wins, losses, score, rankSum, rankedSeasons);
        if (emptyLabel != null) emptyLabel.SetActive(shown == 0);
    }

    private void SetTotals(int seasons, int wins, int losses, long score, long rankSum, int rankedSeasons) {
        if (totalSeasonText != null) totalSeasonText.text = $"{seasons}시즌";
        if (totalRecordText != null) totalRecordText.text = ArenaRules.RecordText(wins, losses, ArenaRules.RateColorLight);
        if (averageScoreText != null)
            averageScoreText.text = seasons > 0 ? $"{(double)score / seasons:N0}" : "-";
        if (averageRankText != null)
            averageRankText.text = rankedSeasons > 0 ? $"{(double)rankSum / rankedSeasons:0.#}위" : "-";
    }

    private void Clear() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
    }

    protected override void OnClosed() => Clear();
}
