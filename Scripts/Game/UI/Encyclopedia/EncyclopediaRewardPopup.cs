using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 도감 보상 팝업. 수집(5명당) / 별레벨(총합 10당) 두 트랙을 각각 스크롤뷰로 보여준다.
/// 각 단계 칸은 요구 조건/보상/상태(수령완료/수령가능/잠금)를 표시하고, 수령 가능 칸의 버튼으로 서버에 수령을 요청한다.
/// 열릴 때 각 트랙에서 아직 수령하지 않은 가장 낮은 단계로 자동 스크롤한다(현재 보상 단계).
/// </summary>
public sealed class EncyclopediaRewardPopup : UIView, IHudPopup {
    [Header("Collection (수집)")]
    [SerializeField] private ScrollRect collectionScroll;
    [SerializeField] private Transform collectionContent;
    [Tooltip("수집 진행도 공통 텍스트(예: '12 / 50'). 각 셀에는 목표만 표기.")]
    [SerializeField] private TMP_Text collectionProgressText;
    [Header("Star (별레벨)")]
    [SerializeField] private ScrollRect starScroll;
    [SerializeField] private Transform starContent;
    [Tooltip("별레벨 진행도 공통 텍스트(예: '34 / 600'). 각 셀에는 목표만 표기.")]
    [SerializeField] private TMP_Text starProgressText;

    [SerializeField] private RewardStepCell cellPrefab;
    [SerializeField] private Button closeButton;

    private MainHudContext _hud;
    private CancellationToken _ct;
    private readonly List<RewardStepCell> _cells = new();
    private bool _busy;

    protected override void Awake() {
        base.Awake();
        _ct = this.GetCancellationTokenOnDestroy();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    public void Initialize(MainHudContext ctx) {
        _hud = ctx;
        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        EncyclopediaRewardStatusDto status;
        try {
            using (_hud.Ui.ShowLoading()) {
                status = await _hud.Encyclopedia.RequestRewardsAsync(_ct);
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("도감 보상 정보를 불러오지 못했습니다.");
            return;
        }

        int collectionTarget = BuildCollection(status);
        int starTarget = BuildStar(status);

        // 레이아웃 확정 후 현재 보상 단계로 자동 스크롤.
        await UniTask.NextFrame(_ct);
        Canvas.ForceUpdateCanvases();
        ScrollTo(collectionScroll, collectionTarget, EncyclopediaRewardRules.CollectionSteps);
        ScrollTo(starScroll, starTarget, EncyclopediaRewardRules.StarSteps);
    }

    // 반환: 자동 스크롤 목표 단계(안 받은 것 중 가장 낮은 단계, 없으면 마지막).
    private int BuildCollection(EncyclopediaRewardStatusDto status) {
        // 진행도는 공통 텍스트 하나로만 표기(각 셀에는 목표만).
        if (collectionProgressText != null) {
            int max = EncyclopediaRewardRules.CollectionThreshold(EncyclopediaRewardRules.CollectionSteps);
            collectionProgressText.text = $"{status.CollectionCount} / {max}";
        }

        int target = EncyclopediaRewardRules.CollectionSteps;
        bool targetFound = false;
        for (int step = 1; step <= EncyclopediaRewardRules.CollectionSteps; step++) {
            int threshold = EncyclopediaRewardRules.CollectionThreshold(step);
            bool claimed = status.ClaimedCollectionSteps.Contains(step);
            bool unlocked = status.CollectionCount >= threshold;
            var state = claimed ? RewardStepCell.CellState.Claimed
                : unlocked ? RewardStepCell.CellState.Claimable
                : RewardStepCell.CellState.Locked;

            string req = $"{threshold}명 수집";
            var rewards = new[] {
                RewardVisualResolver.Resolve(_hud, RewardItem.Diamond(EncyclopediaRewardRules.CollectionDiamond(step)))
            };
            var cell = Instantiate(cellPrefab, collectionContent);
            cell.Bind(EncyclopediaRewardRules.RewardTypeCollection, step, req, rewards, state, OnClaim);
            _cells.Add(cell);

            if (!claimed && !targetFound) { target = step; targetFound = true; }
        }
        return target;
    }

    private int BuildStar(EncyclopediaRewardStatusDto status) {
        if (starProgressText != null) {
            int max = EncyclopediaRewardRules.StarThreshold(EncyclopediaRewardRules.StarSteps);
            starProgressText.text = $"{status.TotalStar} / {max}";
        }

        int target = EncyclopediaRewardRules.StarSteps;
        bool targetFound = false;
        for (int step = 1; step <= EncyclopediaRewardRules.StarSteps; step++) {
            int threshold = EncyclopediaRewardRules.StarThreshold(step);
            bool claimed = status.ClaimedStarSteps.Contains(step);
            bool unlocked = status.TotalStar >= threshold;
            var state = claimed ? RewardStepCell.CellState.Claimed
                : unlocked ? RewardStepCell.CellState.Claimable
                : RewardStepCell.CellState.Locked;

            int grade = EncyclopediaRewardRules.StarShardGrade(step);
            int shardCount = EncyclopediaRewardRules.StarShardCount(step);
            string req = $"총 별레벨 {threshold}";
            var rewards = new[] { RewardVisualResolver.Resolve(_hud, RewardItem.Shard(grade, shardCount)) };
            var cell = Instantiate(cellPrefab, starContent);
            cell.Bind(EncyclopediaRewardRules.RewardTypeStar, step, req, rewards, state, OnClaim);
            _cells.Add(cell);

            if (!claimed && !targetFound) { target = step; targetFound = true; }
        }
        return target;
    }

    // 균등 셀 가정하에 목표 단계(1-based)를 스크롤뷰 상단 부근으로 이동(위=1, 아래=0).
    private static void ScrollTo(ScrollRect scroll, int step, int totalSteps) {
        if (scroll == null || totalSteps <= 1) return;
        float t = Mathf.Clamp01((float)(step - 1) / (totalSteps - 1));
        scroll.verticalNormalizedPosition = 1f - t;
    }

    private void OnClaim(RewardStepCell cell) {
        if (_busy) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        ClaimAsync(cell).Forget();
    }

    private async UniTaskVoid ClaimAsync(RewardStepCell cell) {
        _busy = true;
        try {
            var res = await _hud.Encyclopedia.ClaimAsync(cell.RewardType, cell.Step, _ct);
            if (!res.IsSuccess) {
                _hud.Ui.ShowToast(FailMessage(res.FailReason));
                return;
            }
            if (cell.RewardType == EncyclopediaRewardRules.RewardTypeCollection) {
                _hud.Store.Diamond.Value = res.NewDiamond;
            } else {
                // 만능조각은 인벤토리 재동기화로 즉시 반영.
                try {
                    var inv = await _hud.Inventory.RequestInventoryAsync(_ct);
                    _hud.Store.ApplyInventory(inv);
                } catch (OperationCanceledException) { }
            }
            cell.SetClaimed();
            _hud.Sound.PlaySfx(SfxKey.Claim);
            _hud.Ui.ShowToast("보상을 수령했습니다.");
            if (_hud.Badges != null) _hud.Badges.RefreshAsync(_ct).Forget();   // 도감 보상 배지 갱신
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("보상을 수령할 수 없습니다.");
        } finally {
            _busy = false;
        }
    }

    private static string FailMessage(int fail) => fail switch {
        1 => "잘못된 보상 단계입니다.",
        2 => "아직 조건을 달성하지 못했습니다.",
        3 => "이미 수령한 보상입니다.",
        _ => "보상을 수령할 수 없습니다."
    };

    protected override void OnClosed() {
        foreach (var c in _cells) if (c != null) Destroy(c.gameObject);
        _cells.Clear();
    }
}
