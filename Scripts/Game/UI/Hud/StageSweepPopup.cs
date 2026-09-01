using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 스테이지 소탕 팝업. 보유 소탕권 범위 안에서 사용 장수를 고르고(슬라이더 / - + / 최대),
/// 그 장수만큼 진행 중인 던전을 즉시 클리어한다. 획득 예정 보상은 RewardSlotView 로 미리 보여준다.
///
/// 미리보기 수치는 서버 던전 보상 미리보기(C_DungeonRewardPreview)에서 받는다. 실제 지급도 같은 서버 공식을
/// 쓰므로 표시와 지급이 어긋나지 않는다(클라에 보상 공식 미러를 두지 않는다).
/// </summary>
public sealed class StageSweepPopup : UIView, IHudPopup {
    [Header("Header")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("소탕할 던전 안내(예: '3층 반복'). 진행 중인 던전 이름이 들어간다.")]
    [SerializeField] private TMP_Text subtitleText;
    [Tooltip("보유 소탕권을 포함한 재화 표시(선택). 팝업에서도 잔량을 바로 보여준다.")]
    [SerializeField] private CurrencyPanel currencyPanel;

    [Header("Quantity")]
    [Tooltip("사용 장수 슬라이더(WholeNumbers=on). 최소 1, 최대는 보유 장수로 코드가 설정한다.")]
    [SerializeField] private Slider quantitySlider;
    [SerializeField] private TMP_Text quantityValueText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [Tooltip("보유 장수 전부를 선택하는 버튼.")]
    [SerializeField] private Button maxButton;

    [Header("Cost (확인 버튼 안)")]
    [Tooltip("확인 버튼 안의 소모 장수 텍스트. 보유가 모자라면 부족 색으로 바뀐다.")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("확인 버튼 안의 소탕권 아이콘. 재화 카탈로그에서 자동으로 채운다(선택).")]
    [SerializeField] private Image ticketIcon;
    [Tooltip("선택. 소탕권 부족 시 노출할 안내 오브젝트.")]
    [SerializeField] private GameObject insufficientHint;
    [Tooltip("소탕권 부족 시 소모 장수 텍스트 색.")]
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.20f, 0.20f, 1f);

    [Header("Reward Preview")]
    [SerializeField] private Transform rewardParent;
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    [Header("Buttons")]
    [SerializeField] private Button okButton;
    [SerializeField] private Button closeButton;

    [Header("Loading")]
    [Tooltip("잔량/미리보기를 받아 오는 동안 켜지는 팝업 안 로딩 표시(선택). 화면 전체를 덮는 전역 로딩 " +
             "대신 이것을 쓴다. 기다리는 대상이 이 팝업의 내용뿐이라 바깥까지 막을 이유가 없다. " +
             "다른 팝업들과 같이 팝업 영역을 덮고 클릭을 막는 오브젝트를 넣는다.")]
    [SerializeField] private GameObject loadingGroup;

    private MainHudContext _hud;
    private CancellationToken _ct;
    private readonly List<RewardSlotView> _slots = new();

    private int _owned;
    private int _count = 1;
    private int _dungeonIndex;
    private DungeonRewardPreview _preview;   // 1회분 예상 보상(서버). 없으면 미리보기를 비운다.
    private bool _busy;

    private Color _costDefaultColor = Color.white;
    private bool _capturedCostColor;
    private bool _wired;

    protected override void Awake() {
        base.Awake();
        if (costText != null && !_capturedCostColor) {
            _costDefaultColor = costText.color;
            _capturedCostColor = true;
        }
    }

    public void Initialize(MainHudContext ctx) {
        _hud = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (!_wired) {
            if (okButton != null) okButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); SweepAsync().Forget(); });
            if (closeButton != null) closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            if (minusButton != null) minusButton.onClick.AddListener(() => SetCount(_count - 1));
            if (plusButton != null) plusButton.onClick.AddListener(() => SetCount(_count + 1));
            if (maxButton != null) maxButton.onClick.AddListener(() => SetCount(SelectableMax));
            if (quantitySlider != null) quantitySlider.onValueChanged.AddListener(v => SetCount(Mathf.RoundToInt(v)));
            _wired = true;
        }

        if (currencyPanel != null) currencyPanel.Initialize(ctx);
        CurrencyIcon.Apply(ticketIcon, ctx.Currencies, CurrencyType.StageSweepTicket);
        if (titleText != null) titleText.text = "스테이지 소탕";

        _dungeonIndex = ctx.Store.CurrentDungeonIndex.Value;
        var dungeon = ctx.DungeonDb != null ? ctx.DungeonDb.Get(_dungeonIndex) : null;
        if (subtitleText != null)
            subtitleText.text = $"{(dungeon != null ? dungeon.DisplayName : $"#{_dungeonIndex}")} 반복";

        ApplyOwned();
        LoadAsync().Forget();
    }

    // 소탕권 잔량은 상점 지갑 경로로 내려오므로 열 때 한 번 최신값을 받는다(메인 스냅샷에는 없다).
    // 던전 1회분 예상 보상도 같이 받아 미리보기를 채운다.
    private async UniTaskVoid LoadAsync() {
        if (loadingGroup != null) loadingGroup.SetActive(true);
        try {
            if (_hud.Shop != null) {
                var catalog = await _hud.Shop.RequestShopAsync(_ct);
                _hud.Store.Gold.Value = catalog.Gold;
                _hud.Store.Diamond.Value = catalog.Diamond;
                _hud.Store.ApplyShopCurrencies(catalog.Currencies);
            }
            var list = await _hud.MainData.GetDungeonRewardPreviewAsync(new[] { _dungeonIndex }, _ct);
            _preview = list != null && list.Count > 0 ? list[0] : null;
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _preview = null;
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
        ApplyOwned();   // 받은 잔량으로 슬라이더 범위/버튼 상태를 다시 잡는다(미리보기도 함께 갱신).
    }

    // 보유 소탕권을 스토어에서 읽어 조작 범위를 잡는다(구매/소탕 후에도 같은 경로로 갱신).
    // 상한은 보유 장수와 1회 요청 상한(StageSweepRules.MaxCount) 중 작은 쪽이다.
    // 서버가 거부할 값을 애초에 고를 수 없게 해야 "장수가 올바르지 않습니다"가 뜨지 않는다.
    private void ApplyOwned() {
        var rp = _hud.Store.Currency(CurrencyType.StageSweepTicket);
        _owned = rp != null ? Mathf.Max(0, rp.Value) : 0;

        bool usable = _owned > 0;
        if (quantitySlider != null) {
            quantitySlider.wholeNumbers = true;
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = SelectableMax;
            quantitySlider.interactable = usable;
        }
        if (minusButton != null) minusButton.interactable = usable;
        if (plusButton != null) plusButton.interactable = usable;
        if (maxButton != null) maxButton.interactable = usable;

        SetCount(_count);
    }

    // 한 번에 고를 수 있는 최대 장수(최소 1). 보유가 상한을 넘어도 상한까지만 고를 수 있다.
    private int SelectableMax => StageSweepRules.SelectableMax(_owned);

    private void SetCount(int value) {
        _count = Mathf.Clamp(value, 1, SelectableMax);
        if (quantitySlider != null) quantitySlider.SetValueWithoutNotify(_count);
        if (quantityValueText != null) quantityValueText.text = _count.ToString();

        // 선택 장수는 항상 보유량 이하로 클램프되므로, 부족해지는 경우는 보유가 0일 때뿐이다.
        bool insufficient = _owned < _count;
        if (costText != null) {
            costText.text = _count.ToString("N0");
            costText.color = insufficient ? insufficientColor : _costDefaultColor;
        }
        if (insufficientHint != null) insufficientHint.SetActive(insufficient);
        if (okButton != null) okButton.interactable = !insufficient && !_busy;

        BuildRewards();
    }

    // 소탕 보상 = 던전 1회 보상 x 장수. 경험치는 덱 캐릭터 각자에게 같은 양이 들어가므로 1인분으로 표시한다.
    private void BuildRewards() {
        if (rewardSlotPrefab == null || rewardParent == null) return;

        var rewards = new List<RewardItem>();
        if (_preview != null) {
            if (_preview.Gold > 0) rewards.Add(RewardItem.Gold(_preview.Gold * _count));
            if (_preview.ExpPerCharacter > 0) rewards.Add(RewardItem.Exp(_preview.ExpPerCharacter * _count));
        }

        while (_slots.Count < rewards.Count) _slots.Add(Instantiate(rewardSlotPrefab, rewardParent));

        for (int i = 0; i < _slots.Count; i++) {
            bool used = i < rewards.Count;
            _slots[i].gameObject.SetActive(used);
            if (used) _slots[i].Bind(RewardVisualResolver.Resolve(_hud, rewards[i]));
        }
    }

    private async UniTaskVoid SweepAsync() {
        if (_busy) return;
        _busy = true;
        if (okButton != null) okButton.interactable = false;
        try {
            var res = await _hud.MainData.SweepStageAsync(_count, _ct);
            if (!res.IsSuccess) {
                _hud.Ui.ShowToast(res.FailReason switch {
                    2 => "소탕 장수가 올바르지 않습니다.",
                    3 => "스테이지 소탕권이 부족합니다.",
                    4 => "아직 진행할 수 없는 던전입니다.",
                    _ => "소탕할 수 없습니다."
                });
                return;
            }

            // 서버 확정값으로 지갑/성장치를 반영한다(재로그인 없이 즉시 확인 가능).
            _hud.Store.Gold.Value = res.NewGold;
            _hud.Store.SetCurrency(CurrencyType.StageSweepTicket, res.NewTicketQty);
            _hud.Store.ApplyCharExp(res.CharExp);

            _hud.Sound.PlaySfx(SfxKey.Claim);
            _hud.Ui.ShowToast($"{res.Count}회 소탕을 완료했습니다.");
            ApplyOwned();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("소탕에 실패했습니다.");
        } finally {
            _busy = false;
            if (okButton != null) okButton.interactable = _owned > 0;
        }
    }
}
