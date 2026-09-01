using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 랭킹 팝업. 메인 화면의 랭킹 버튼(HudMenuButton)에서 연다.
///
/// 탭은 유저(덱 전투력) / 길드(길드 전투력) / 업적(업적 총점) 셋이다.
/// 순위는 서버가 주기적으로 만들어 둔 스냅샷에서 오므로 이 화면은 정렬하지 않고 받은 순서대로 그린다.
///
/// 탭마다 별도 조회이며 한 번 받은 탭은 팝업이 열려 있는 동안 다시 받지 않는다
/// (순위가 분 단위로 갱신되는 값이라 탭을 오갈 때마다 왕복할 이유가 없다).
/// 내 순위는 서버가 함께 보내 주므로 목록에 내가 없어도 아래 고정 영역에 표시된다.
/// </summary>
public sealed class RankingPopup : UIView, IHudPopup, IHudPopupTab {
    // 탭이 화면에 놓이는 순서. 아래 배열(탭 버튼 / 페이지 / 목록 부모 / 줄 프리팹 / 안내)이 모두 이 순서를 따른다.
    private static readonly RankingCategory[] TabCategories = {
        RankingCategory.UserCombatPower,
        RankingCategory.GuildCombatPower,
        RankingCategory.Achievement,
        RankingCategory.ArenaScore,
    };

    [Header("Tabs")]
    [Tooltip("탭 버튼(유저 / 길드 / 업적 / 쟁탈전 순).")]
    [SerializeField] private UITabButton[] tabButtons;
    [Tooltip("탭 페이지(탭 버튼과 같은 순서).")]
    [SerializeField] private GameObject[] tabPages;
    [Tooltip("순위 줄이 생성될 부모(탭과 같은 순서).")]
    [SerializeField] private Transform[] rankRoots;

    [Header("Slot Prefabs")]
    [Tooltip("탭별 줄 프리팹(탭과 같은 순서: 유저 / 길드 / 업적). 랭킹마다 보여 주는 값이 달라 프리팹을 따로 둔다.")]
    [SerializeField] private RankingSlot[] slotPrefabs;

    [Header("My Rank")]
    [Tooltip("내 순위. 순위권 밖이면 안내 문구가 들어가며, 구간별 색은 RankColorCatalog 가 정한다.")]
    [SerializeField] private RankText myRankText;
    [Tooltip("내 점수(전투력 또는 업적 점수).")]
    [SerializeField] private TMP_Text myScoreText;
    [Tooltip("선택. 집계 시각 안내(예: '10:20 기준').")]
    [SerializeField] private TMP_Text updatedText;
    [Tooltip("선택. 순위가 하나도 없을 때 켜는 안내(탭과 같은 순서). 비워 두면 표시하지 않는다.")]
    [SerializeField] private GameObject[] emptyLabels;

    [Header("Catalog")]
    [Tooltip("길드 마크 카탈로그. 세 탭의 줄(길드 마크 / 소속 길드 마크)과 유저 프로필 팝업이 모두 쓴다.")]
    [SerializeField] private GuildMarkCatalog markCatalog;

    [Header("Loading")]
    [SerializeField] private GameObject loadingGroup;

    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private CancellationToken _ct;

    // 지금 보고 있는 탭의 랭킹. 응답이 늦게 온 탭이 지금 화면의 내 순위를 덮어쓰지 않게 하는 기준이다.
    private RankingCategory _current;

    // 조회 중인 탭. 같은 탭을 두 번 요청하는 것만 막는다.
    //
    // 예전에는 "하나라도 조회 중이면 무시"였는데, 팝업을 열자마자(첫 탭 조회 중에) 다른 탭을 누르면
    // 그 탭의 조회가 통째로 버려져 목록이 비고 내 순위/점수는 앞 탭 값이 그대로 남았다
    // (쟁탈전 탭에 전투력 순위가 남아 있는 것처럼 보인다).
    private readonly HashSet<RankingCategory> _loading = new();

    // 탭별로 받아 둔 순위표. 팝업이 열려 있는 동안 재사용한다.
    private readonly Dictionary<RankingCategory, RankingBoardDto> _boards = new();
    private readonly List<RankingSlot> _slots = new();

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { Click(); CloseAsync().Forget(); });
        }

        BindTabs();
    }

    // ── 탭 ───────────────────────────────────────────
    private void BindTabs() {
        if (tabButtons == null) return;
        for (int i = 0; i < tabButtons.Length; i++) {
            if (tabButtons[i] == null) continue;
            int index = i;
            tabButtons[i].Bind(null, () => { Click(); SelectTab(index); });
        }
        SelectTab(0);
    }

    /// <summary>지정한 탭을 연다(IHudPopupTab). 그 탭을 처음 열면 순위표를 받아 온다.</summary>
    public void SelectTab(int index) {
        if (tabButtons == null || index < 0 || index >= tabButtons.Length) return;
        for (int i = 0; i < tabButtons.Length; i++)
            if (tabButtons[i] != null) tabButtons[i].SetSelected(i == index);
        if (tabPages != null)
            for (int i = 0; i < tabPages.Length; i++)
                if (tabPages[i] != null) tabPages[i].SetActive(i == index);

        // 탭 버튼보다 분류가 적으면 배선이 어긋난 것이다. 조용히 지나가면 앞 탭 내용이 그대로 남아
        // "쟁탈전 탭인데 전투력 순위가 보인다" 처럼 나타나므로 원인을 즉시 드러낸다.
        if (index >= TabCategories.Length) {
            Debug.LogError($"[RankingPopup] {index}번 탭에 대응하는 랭킹 분류가 없습니다. " +
                           $"탭 버튼 {tabButtons.Length}개 / 분류 {TabCategories.Length}개.");
            return;
        }

        var category = TabCategories[index];
        _current = category;
        if (_boards.TryGetValue(category, out var board)) ApplyMyRank(category, board);
        else LoadAsync(category).Forget();
    }

    private async UniTaskVoid LoadAsync(RankingCategory category) {
        if (!_loading.Add(category)) return;   // 같은 탭 중복 요청만 막는다
        SetLoading(true);
        try {
            var board = await _ctx.Ranking.RequestBoardAsync(category, _ct);
            _boards[category] = board;
            Build(category, board);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("랭킹을 불러오지 못했습니다.");
        } finally {
            _loading.Remove(category);
            SetLoading(_loading.Count > 0);
        }
    }

    // ── 표시 ─────────────────────────────────────────
    private void Build(RankingCategory category, RankingBoardDto board) {
        int tab = Array.IndexOf(TabCategories, category);
        var root = rankRoots != null && tab >= 0 && tab < rankRoots.Length ? rankRoots[tab] : null;
        var prefab = slotPrefabs != null && tab >= 0 && tab < slotPrefabs.Length ? slotPrefabs[tab] : null;

        // 배선이 비어 있으면 그 탭은 영영 빈 목록으로 남는다(원인이 화면에 드러나지 않으므로 로그로 알린다).
        if (root == null || prefab == null)
            Debug.LogError($"[RankingPopup] {RankingRules.CategoryName(category)} 탭({tab})의 " +
                           $"{(root == null ? "목록 부모" : "줄 프리팹")}가 배선되지 않았습니다.");

        // 줄을 채우는 방식은 대상이 계정이냐 길드냐로만 갈린다(업적 랭킹도 계정이라 유저 줄과 같은 방식이다).
        bool isGuild = category == RankingCategory.GuildCombatPower;

        if (root != null && prefab != null) {
            foreach (var entry in board.Entries) {
                var slot = Instantiate(prefab, root);
                // 내 줄 강조는 순위로 판단한다. 서버가 내 순위를 알려 주므로 계정 id 를 클라가 들고 있을 필요가 없다.
                bool mine = board.HasMyRank && entry.Rank == board.MyRank;
                if (isGuild) slot.BindGuild(entry, markCatalog, ScoreText(category, entry.Score), mine, OpenGuildProfile);
                else slot.BindUser(entry, _ctx, markCatalog, ScoreText(category, entry.Score), mine, OpenUserProfile);
                _slots.Add(slot);
            }
        }

        if (emptyLabels != null && tab >= 0 && tab < emptyLabels.Length && emptyLabels[tab] != null)
            emptyLabels[tab].SetActive(board.Entries.Count == 0);

        // 내 순위/점수 칸은 탭마다 따로 두지 않고 하나를 공유한다. 응답이 늦게 온 탭이 지금 보고 있는
        // 탭의 값을 덮어쓰지 않도록, 그 사이에 탭을 옮겼으면 목록만 채우고 여기는 건드리지 않는다.
        if (category == _current) ApplyMyRank(category, board);
    }

    private void ApplyMyRank(RankingCategory category, RankingBoardDto board) {
        // 순위 표기와 색은 프로필 팝업과 같은 컴포넌트를 쓴다(한 게임 안에서 표기가 갈리지 않게).
        if (myRankText != null) myRankText.SetRank(board.MyRank);
        if (myScoreText != null) myScoreText.text = ScoreText(category, board.MyScore);
        if (updatedText != null) {
            bool has = board.UpdatedUnix > 0;
            updatedText.gameObject.SetActive(has);
            if (has) updatedText.text = $"{KstTime.From(board.UpdatedUnix):HH:mm} 기준";
        }
    }

    // 전투력은 숫자 그대로, 업적은 점수 단위를 붙인다. 표시 규칙이 한 곳에만 있도록 여기서 정한다.
    private static string ScoreText(RankingCategory category, long score) =>
        category == RankingCategory.Achievement ? AchievementRules.ScoreText(score) : score.ToString("N0");

    /// <summary>유저 줄을 눌렀을 때 그 유저의 프로필 팝업을 연다.</summary>
    private void OpenUserProfile(int accountId) {
        Click();
        OpenUserProfileAsync(accountId).Forget();
    }

    /// <summary>길드 줄을 눌렀을 때 그 길드의 프로필 팝업을 연다(길드 id 만 넘기면 팝업이 스스로 조회한다).</summary>
    private void OpenGuildProfile(int guildId) {
        Click();
        OpenGuildProfileAsync(guildId).Forget();
    }

    private async UniTaskVoid OpenGuildProfileAsync(int guildId) {
        var reference = _ctx.Popups.GuildProfilePopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<GuildProfilePopup>(reference, _ct);
            await popup.ShowAsync(_ctx, guildId, markCatalog);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private async UniTaskVoid OpenUserProfileAsync(int accountId) {
        var reference = _ctx.Popups.UserProfilePopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<UserProfilePopup>(reference, _ct);
            await popup.ShowAsync(_ctx, accountId, markCatalog);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // ── 표시 헬퍼 ────────────────────────────────────
    private void SetLoading(bool on) {
        if (loadingGroup != null) loadingGroup.SetActive(on);
    }

    private void Click() => _ctx?.Sound.PlaySfx(SfxKey.ButtonClick);

    protected override void OnClosed() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
        _boards.Clear();
    }
}
