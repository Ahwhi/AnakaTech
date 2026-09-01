using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 업적 팝업. 메인 화면의 업적 버튼(HudMenuButton)에서 연다.
///
/// 탭은 요약 + 분류 넷(게임/길드/성장/모집) + 보상이다. 서버 응답 하나가 항목과 보상 티어를 모두 담고 있어
/// 진입 시 왕복이 한 번이고, 요약의 분류별 점수도 받아 온 항목을 더해 만든다(같은 값을 두 번 묻지 않는다).
///
/// 업적 목록과 배점, 보상 구성은 서버 카탈로그가 정하므로 이 화면은 어떤 업적/보상이 있는지 알지 못한다.
/// 항목이 늘어도 여기는 그대로이고 표시 문구(AchievementRules)만 추가하면 된다.
/// </summary>
public sealed class AchievementPopup : UIView, IHudPopup, IHudPopupTab {
    // 탭 순서 규약: 요약(0) / 게임(1) / 길드(2) / 성장(3) / 모집(4) / 보상(5).
    // 분류 탭은 요약 다음부터 AchievementCategory 순서 그대로이고, 보상 탭은 맨 뒤에 붙인다
    // (중간에 끼우면 아래 상수와 분류 매핑이 어긋난다).

    /// <summary>요약 탭 인덱스.</summary>
    public const int SummaryTabIndex = 0;

    /// <summary>보상 탭 인덱스. 배지를 보고 들어온 유저를 이 탭으로 바로 보낼 때 쓴다.</summary>
    public const int RewardTabIndex = 5;

    // 분류 탭이 화면에 놓이는 순서. 서버 카탈로그의 분류 순서와 같게 두어 탭과 페이지가 어긋나지 않게 한다.
    private static readonly AchievementCategory[] TabCategories = {
        AchievementCategory.Game,
        AchievementCategory.Guild,
        AchievementCategory.Growth,
        AchievementCategory.Recruit,
    };

    [Header("Tabs")]
    [Tooltip("탭 버튼(요약 / 게임 / 길드 / 성장 / 모집 순).")]
    [SerializeField] private UITabButton[] tabButtons;
    [Tooltip("탭 페이지(탭 버튼과 같은 순서).")]
    [SerializeField] private GameObject[] tabPages;

    [Header("Summary")]
    [Tooltip("분류별 점수 줄이 생성될 부모(요약 탭).")]
    [SerializeField] private Transform summaryRoot;
    [Tooltip("요약 줄 프리팹. 비우면 목록 줄 프리팹을 쓴다(요약에서는 누적/배점 칸이 자동으로 꺼진다).")]
    [SerializeField] private AchievementRowView summaryRowPrefab;
    [Tooltip("업적 총점 표시.")]
    [SerializeField] private TMP_Text totalScoreText;

    [Header("Category Pages")]
    [Tooltip("분류별 업적 줄이 생성될 부모. 배열 순서는 게임 / 길드 / 성장 / 모집 이다.")]
    [SerializeField] private Transform[] categoryRoots;
    [Tooltip("업적 한 줄 프리팹(이름 + 누적 + 배점 + 점수).")]
    [SerializeField] private AchievementRowView rowPrefab;

    [Header("Reward Tab")]
    [Tooltip("점수 보상 칸이 생성될 부모(보상 탭). 스크롤뷰의 Content 를 넣는다.")]
    [SerializeField] private Transform rewardRoot;
    [Tooltip("보상 1티어 칸 프리팹. 도감/충전 보상과 같은 공용 칸(RewardStepCell)이라 보상 슬롯 표시가 통일된다.")]
    [SerializeField] private RewardStepCell rewardCellPrefab;
    [Tooltip("선택. 보상 탭의 스크롤. 열릴 때 아직 수령하지 않은 가장 낮은 티어로 맞춘다.")]
    [SerializeField] private ScrollRect rewardScroll;

    [Header("Loading")]
    [SerializeField] private GameObject loadingGroup;

    [SerializeField] private Button closeButton;

    /// <summary>
    /// 보상 칸(RewardStepCell)의 종류 구분값. 도감/충전 보상과 같은 프리팹을 공유하므로,
    /// 칸이 자기가 어느 보상의 것인지 구분할 수 있도록 다른 값을 쓴다.
    /// </summary>
    private const int RewardCellType = 3;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private AchievementHomeDto _home;
    private bool _claiming;

    private readonly List<AchievementRowView> _rows = new();
    private readonly List<RewardStepCell> _rewardCells = new();

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { Click(); CloseAsync().Forget(); });
        }

        BindTabs();
        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        SetLoading(true);
        try {
            _home = await _ctx.Achievement.RequestAchievementsAsync(_ct);
            Build();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("업적 정보를 불러오지 못했습니다.");
        } finally {
            SetLoading(false);
        }
    }

    private void Build() {
        if (_home == null) return;
        ClearRows();

        BuildSummary();
        BuildCategories();
        BuildRewards();
    }

    // ── 보상 탭 ──────────────────────────────────────
    // 티어마다 "임계치 + 보상 슬롯 + 수령 버튼"을 보여 준다. 달성/수령 판정은 서버가 내려준 값으로만 하고
    // (임계치와 수령 여부), 화면은 그 둘과 현재 총점을 비교해 상태를 고른다.
    private void BuildRewards() {
        ClearRewardCells();
        if (rewardRoot == null || rewardCellPrefab == null) return;

        int firstUnclaimed = 0;
        foreach (var tier in _home.RewardTiers) {
            var cell = Instantiate(rewardCellPrefab, rewardRoot);
            cell.Bind(RewardCellType, tier.Tier, AchievementRules.RewardRequirementText(tier.Threshold),
                      ResolveRewards(tier), StateOf(tier), OnClaim);
            _rewardCells.Add(cell);
            if (firstUnclaimed == 0 && !tier.Claimed) firstUnclaimed = tier.Tier;
        }

        ScrollToAsync(firstUnclaimed).Forget();
    }

    // 아직 수령하지 않은 가장 낮은 티어가 보이도록 맞춘다(전부 수령했으면 맨 아래).
    // 칸이 실제로 배치된 뒤라야 위치를 계산할 수 있어 한 프레임 뒤에 적용한다.
    private async UniTaskVoid ScrollToAsync(int tier) {
        if (rewardScroll == null || _rewardCells.Count <= 1) return;
        try {
            await UniTask.NextFrame(_ct);
        } catch (OperationCanceledException) {
            return;
        }
        Canvas.ForceUpdateCanvases();

        int index = tier > 0 ? tier : _rewardCells.Count;
        float t = Mathf.Clamp01((float)(index - 1) / (_rewardCells.Count - 1));
        rewardScroll.verticalNormalizedPosition = 1f - t;
    }

    // 보상 구성 해석은 RewardVisualResolver 가 카탈로그로 처리하므로 품목이 늘어도 이 화면은 수정할 필요가 없다.
    private List<RewardSlotView.RewardVisual> ResolveRewards(AchievementRewardTierDto tier) {
        var list = new List<RewardSlotView.RewardVisual>(tier.Rewards.Count);
        foreach (var r in tier.Rewards) list.Add(RewardVisualResolver.Resolve(_ctx, r));
        return list;
    }

    private RewardStepCell.CellState StateOf(AchievementRewardTierDto tier) =>
        tier.Claimed ? RewardStepCell.CellState.Claimed
        : tier.IsClaimable(_home.TotalScore) ? RewardStepCell.CellState.Claimable
        : RewardStepCell.CellState.Locked;

    private void OnClaim(RewardStepCell cell) {
        Click();
        ClaimAsync(cell).Forget();
    }

    private async UniTaskVoid ClaimAsync(RewardStepCell cell) {
        // 앞선 수령이 아직 안 끝났다. 조용히 무시하면 눌렀는데 아무 일도 안 일어난 것처럼 보이므로 이유를 알린다.
        if (_claiming) { _ctx.Ui.ShowToast("처리 중입니다. 잠시 후 다시 시도해 주세요."); return; }
        _claiming = true;
        try {
            var res = await _ctx.Achievement.ClaimRewardAsync(cell.Step, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(AchievementRules.ClaimFailMessage(res.FailReason));
                return;
            }

            _ctx.Store.Diamond.Value = res.NewDiamond;
            // 다이아 외에 프로필 테두리 등도 지급되므로 인벤토리/재화를 재동기화한다(다른 보상 수령과 같은 처리).
            await ResyncAsync();

            cell.SetClaimed();
            MarkClaimed(cell.Step);
            _ctx.Sound.PlaySfx(SfxKey.Claim);
            _ctx.Ui.ShowToast("보상을 수령했습니다.");
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("보상을 수령하지 못했습니다.");
        } finally {
            _claiming = false;
        }
    }

    // 받은 티어를 내가 들고 있는 목록에도 반영하고, 남은 수령 가능 여부를 배지에 알린다
    // (팝업이 이미 아는 값이라 배지를 맞추려고 서버를 다시 두드리지 않는다).
    private void MarkClaimed(int tier) {
        foreach (var t in _home.RewardTiers) {
            if (t.Tier != tier) continue;
            t.Claimed = true;
            break;
        }
        _ctx.Achievement.SetClaimable(_home.HasClaimableReward);
    }

    // 지급 품목이 다이아에 그치지 않으므로(프로필 테두리 등) 보유 상태를 서버 기준으로 다시 맞춘다.
    private async UniTask ResyncAsync() {
        try {
            var inv = await _ctx.Inventory.RequestInventoryAsync(_ct);
            _ctx.Store.ApplyInventory(inv);
            var catalog = await _ctx.Shop.RequestShopAsync(_ct);
            _ctx.Store.Diamond.Value = catalog.Diamond;
            _ctx.Store.ApplyShopCurrencies(catalog.Currencies);
        } catch (OperationCanceledException) {
        }
    }

    private void BuildSummary() {
        if (totalScoreText != null) totalScoreText.text = AchievementRules.ScoreText(_home.TotalScore);

        var prefab = summaryRowPrefab != null ? summaryRowPrefab : rowPrefab;
        if (summaryRoot == null || prefab == null) return;

        foreach (var category in TabCategories) {
            var row = Instantiate(prefab, summaryRoot);
            row.BindCategory(category, _home.ScoreOf(category));
            _rows.Add(row);
        }
    }

    private void BuildCategories() {
        if (categoryRoots == null || rowPrefab == null) return;

        for (int i = 0; i < TabCategories.Length && i < categoryRoots.Length; i++) {
            var root = categoryRoots[i];
            if (root == null) continue;

            var category = TabCategories[i];
            foreach (var entry in _home.Entries) {
                if (entry.Category != category) continue;
                var row = Instantiate(rowPrefab, root);
                row.Bind(entry);
                _rows.Add(row);
            }
        }
    }

    // ── 탭 ───────────────────────────────────────────
    private void BindTabs() {
        if (tabButtons == null) return;
        for (int i = 0; i < tabButtons.Length; i++) {
            if (tabButtons[i] == null) continue;
            int index = i;
            tabButtons[i].Bind(null, () => { Click(); SelectTab(index); });
        }
        SelectTab(SummaryTabIndex);
    }

    /// <summary>지정한 탭을 연다(IHudPopupTab).</summary>
    public void SelectTab(int index) {
        if (tabButtons == null || index < 0 || index >= tabButtons.Length) return;
        for (int i = 0; i < tabButtons.Length; i++)
            if (tabButtons[i] != null) tabButtons[i].SetSelected(i == index);
        if (tabPages != null)
            for (int i = 0; i < tabPages.Length; i++)
                if (tabPages[i] != null) tabPages[i].SetActive(i == index);
    }

    // ── 표시 헬퍼 ────────────────────────────────────
    private void SetLoading(bool on) {
        if (loadingGroup != null) loadingGroup.SetActive(on);
    }

    private void ClearRows() {
        foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
        _rows.Clear();
    }

    private void ClearRewardCells() {
        foreach (var c in _rewardCells) if (c != null) Destroy(c.gameObject);
        _rewardCells.Clear();
    }

    private void Click() => _ctx?.Sound.PlaySfx(SfxKey.ButtonClick);

    protected override void OnClosed() {
        ClearRows();
        ClearRewardCells();
    }
}
