using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 누적 충전 보상 팝업. 누적 충전액(cash 구매 원화 합) 티어별 보상을 스크롤뷰로 보여주고, 달성한 티어의 보상을 수령한다.
/// 각 티어는 이하를 각각 수령 가능(단계별). 열릴 때 아직 수령하지 않은 가장 낮은 티어로 자동 스크롤한다.
/// 재화>다이아 페이지의 버튼에서 연다.
/// </summary>
public sealed class ChargeRewardPopup : UIView, IHudPopup {
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private Transform content;
    [SerializeField] private RewardStepCell cellPrefab;
    [SerializeField] private Button closeButton;
    [Tooltip("현재 누적 충전 금액을 표시하는 단일 텍스트(예: '누적 충전: 12,345원').")]
    [SerializeField] private TMP_Text cumulativeText;
    [Tooltip("누적 충전 금액에서 '숫자+원' 부분에 강조할 색.")]
    [SerializeField] private Color cumulativeAmountColor = new Color(1f, 0.78f, 0.25f);

    private const int RewardTypeCharge = 2;   // RewardStepCell 재사용용 구분값

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
        ChargeRewardStatusDto status;
        try {
            using (_hud.Ui.ShowLoading()) {
                status = await _hud.ChargeReward.RequestStatusAsync(_ct);
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("충전 보상 정보를 불러오지 못했습니다.");
            return;
        }

        int target = Build(status);
        await UniTask.NextFrame(_ct);
        Canvas.ForceUpdateCanvases();
        ScrollTo(target, ChargeRewardRules.Tiers.Count);
    }

    private int Build(ChargeRewardStatusDto status) {
        // 누적 충전액은 팝업 상단 단일 텍스트로만 표시(각 티어 셀에는 목표 금액만 노출).
        // 금액 부분("숫자+원")만 강조색으로 물들인다.
        if (cumulativeText != null)
            cumulativeText.text = $"누적 충전 금액: {Colorize($"{status.CumulativeWon:N0}원", cumulativeAmountColor)}";

        var tiers = ChargeRewardRules.Tiers;
        int target = tiers.Count;
        bool targetFound = false;
        for (int i = 0; i < tiers.Count; i++) {
            int tierIndex = i + 1;
            var info = tiers[i];
            bool claimed = status.ClaimedTiers.Contains(tierIndex);
            bool unlocked = status.CumulativeWon >= info.Threshold;
            var state = claimed ? RewardStepCell.CellState.Claimed
                : unlocked ? RewardStepCell.CellState.Claimable
                : RewardStepCell.CellState.Locked;

            string req = $"{info.Threshold:N0}원";
            var rewards = new RewardSlotView.RewardVisual[info.Rewards.Length];
            for (int r = 0; r < info.Rewards.Length; r++) rewards[r] = RewardVisualResolver.Resolve(_hud, info.Rewards[r]);
            var cell = Instantiate(cellPrefab, content);
            cell.Bind(RewardTypeCharge, tierIndex, req, rewards, state, OnClaim);
            _cells.Add(cell);

            if (!claimed && !targetFound) { target = tierIndex; targetFound = true; }
        }
        return target;
    }

    // TMP 리치텍스트로 문자열에 색을 입힌다(대상 텍스트는 Rich Text 활성 필요).
    private static string Colorize(string text, Color color) =>
        $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";

    private static int Clamp01Index(int step, int total) => Mathf.Clamp(step, 1, Mathf.Max(1, total));

    private void ScrollTo(int tierIndex, int total) {
        if (scroll == null || total <= 1) return;
        float t = Mathf.Clamp01((float)(Clamp01Index(tierIndex, total) - 1) / (total - 1));
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
            var res = await _hud.ChargeReward.ClaimAsync(cell.Step, _ct);
            if (!res.IsSuccess) {
                _hud.Ui.ShowToast(FailMessage(res.FailReason));
                return;
            }
            _hud.Store.Diamond.Value = res.NewDiamond;
            // 조각/티켓/장비/테두리 등도 지급될 수 있어 인벤토리/재화를 재동기화(즉시 반영).
            await ResyncAsync();
            cell.SetClaimed();
            _hud.Sound.PlaySfx(SfxKey.Claim);
            _hud.Ui.ShowToast("보상을 수령했습니다.");
            if (_hud.Badges != null) _hud.Badges.RefreshAsync(_ct).Forget();   // 충전 보상 배지 갱신
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("보상을 수령할 수 없습니다.");
        } finally {
            _busy = false;
        }
    }

    private async UniTask ResyncAsync() {
        try {
            var inv = await _hud.Inventory.RequestInventoryAsync(_ct);
            _hud.Store.ApplyInventory(inv);
            var catalog = await _hud.Shop.RequestShopAsync(_ct);
            _hud.Store.Diamond.Value = catalog.Diamond;
            _hud.Store.ApplyShopCurrencies(catalog.Currencies);
        } catch (OperationCanceledException) {
        }
    }

    private static string FailMessage(int fail) => fail switch {
        1 => "잘못된 보상 단계입니다.",
        2 => "아직 충전 금액이 부족합니다.",
        3 => "이미 수령한 보상입니다.",
        _ => "보상을 수령할 수 없습니다."
    };

    protected override void OnClosed() {
        foreach (var c in _cells) if (c != null) Destroy(c.gameObject);
        _cells.Clear();
    }
}
