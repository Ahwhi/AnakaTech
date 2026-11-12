using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UniRx;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 투자 팝업. 골드(100 레벨까지)와 다이아(101~200 레벨)를 넣어 계정 전체에 걸리는 효과를 올린다.
///
/// 슬롯은 캐릭터 별레벨 총합으로 열린다. 열린 칸과 잠긴 칸을 모두 그려, 다음에 무엇이 열리는지를
/// 화면에서 바로 알 수 있게 한다(잠긴 칸을 감추면 "더 있는지조차" 알 수 없다).
///
/// 판정(해금/최대레벨/골드)은 전부 서버가 하고 여기서는 표시와 요청만 한다. 업그레이드 성공 시
/// 서버가 확정한 레벨/잔액을 그대로 반영하므로 화면이 다시 계산하지 않는다.
/// </summary>
public sealed class InvestmentPopup : UIView, IHudPopup {
    [Header("Header")]
    [Tooltip("캐릭터 별레벨 총합 표시(예: 107 / 600). 라벨 문구는 옆에 따로 배치한다.")]
    [SerializeField] private TMP_Text starTotalText;

    [Header("Slots")]
    [Tooltip("슬롯이 생성될 부모.")]
    [SerializeField] private Transform slotRoot;
    [SerializeField] private InvestmentSlotView slotPrefab;

    [Header("Common")]
    [Tooltip("보유 재화 표시(골드). 선택.")]
    [SerializeField] private CurrencyPanel currencyPanel;
    [SerializeField] private GameObject loadingGroup;
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private InvestmentDto _data;
    private bool _busy;

    private readonly List<InvestmentSlotView> _slots = new();

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (currencyPanel != null) currencyPanel.Initialize(ctx);
        // 열었으면 새로 열린 슬롯을 본 것이다(투자 버튼 New 해제).
        if (_ctx.Badges != null) _ctx.Badges.MarkInvestmentSeen();
        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { Click(); CloseAsync().Forget(); });
        }

        // 골드/다이아가 밖에서 바뀌어도(상점 구매/보상 수령 등) 버튼 활성이 따라오게 한다.
        _ctx.Store.Gold.Subscribe((int _) => { if (_data != null) Build(); }).AddTo(this);
        _ctx.Store.Diamond.Subscribe((int _) => { if (_data != null) Build(); }).AddTo(this);

        ReloadAsync().Forget();
    }

    private async UniTaskVoid ReloadAsync() {
        SetLoading(true);
        try {
            _data = await _ctx.Investment.RequestAsync(_ct);
            // 전투 효과(선제 스킬 게이지/스쿼드 재생)는 이 레벨을 근거로 클라가 적용하므로 스토어에도 넣는다.
            _ctx.Store.ApplyInvestment(_data);
            Build();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("투자 정보를 불러오지 못했습니다.");
        } finally {
            SetLoading(false);
        }
    }

    private void Build() {
        if (_data == null || slotPrefab == null || slotRoot == null) return;

        if (starTotalText != null)
            starTotalText.text = $"{_data.StarTotal} / {AccountStats.MaxTotalStarLevel(_ctx)}";

        // 슬롯 수는 정의(InvestmentRules)가 정한다. 라이브에서 슬롯이 늘어도 이 화면은 그대로다.
        int count = InvestmentRules.SlotCount;
        while (_slots.Count < count) _slots.Add(Instantiate(slotPrefab, slotRoot));

        int gold = _ctx.Store.Gold.Value;
        int diamond = _ctx.Store.Diamond.Value;
        for (int i = 0; i < _slots.Count; i++) {
            bool used = i < count;
            _slots[i].gameObject.SetActive(used);
            if (!used) continue;

            _slots[i].Bind(i, _data.LevelOf(i), InvestmentRules.IsUnlocked(i, _data.StarTotal), gold, diamond, _ctx, Upgrade);
        }
    }

    // ── 업그레이드 ────────────────────────────────────
    private void Upgrade(int slotIndex) {
        if (_busy || _data == null) return;
        Click();
        UpgradeAsync(slotIndex).Forget();
    }

    private async UniTaskVoid UpgradeAsync(int slotIndex) {
        _busy = true;
        bool reload = false;
        try {
            // 지금 화면에 보이는 레벨을 함께 보낸다. 서버 레벨과 다르면 서버가 거부하므로,
            // 화면이 골드 비용을 보여 주는 동안 보낸 요청이 다이아 구간에서 처리되는 일이 없다.
            var res = await _ctx.Investment.UpgradeAsync(slotIndex, _data.LevelOf(slotIndex), _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(InvestmentRules.FailMessage(res.FailReason));
                reload = res.FailReason == 7;   // 화면이 서버와 어긋났다. 현황을 다시 받는다.
                return;
            }

            // 서버가 확정한 값을 그대로 반영한다(화면이 다시 계산하지 않는다).
            _data.Levels[res.SlotIndex] = res.NewLevel;
            _data.StarTotal = res.StarTotal;
            _ctx.Store.Gold.Value = res.NewGold;
            _ctx.Store.Diamond.Value = res.NewDiamond;
            _ctx.Store.ApplyInvestmentLevel(res.SlotIndex, res.NewLevel);

            // 투자는 이벤트 일일 미션(투자 n회)의 진행 지표다. 달성되는 순간 메인 이벤트 버튼에도 불이 들어오도록
            // 충족 상태만 가볍게 다시 받아온다(장비 강화와 같은 방식).
            if (_ctx.Badges != null) _ctx.Badges.RefreshEventClaimStatesAsync(_ct).Forget();

            _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
            Build();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("투자에 실패했습니다.");
        } finally {
            _busy = false;
            if (reload) ReloadAsync().Forget();
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
    }
}
