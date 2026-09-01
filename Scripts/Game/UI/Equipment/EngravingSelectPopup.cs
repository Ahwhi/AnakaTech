using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 각인 선택 팝업. 보유/미보유 각인을 모두 나열하고, 고른 각인을 지정 슬롯에 장착하거나 강화한다.
///
/// 각인 레벨은 계정 공용이라 여기서 올리면 그 각인을 낀 모든 캐릭터가 함께 강해진다(그 점을 안내 문구로 표시).
/// 장착/강화 모두 서버 권위이며, 성공 응답을 받은 뒤에만 스토어에 반영한다.
/// 결과로 "장착이 바뀌었는지"를 돌려주어 호출측(각인 패널)이 다시 그릴지 정한다.
/// </summary>
public sealed class EngravingSelectPopup : UIView {
    [Header("List")]
    [SerializeField] private Transform cardParent;
    [SerializeField] private EngravingCardView cardPrefab;

    [Header("Detail")]
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailEffectText;
    [Tooltip("다음 레벨 효과 미리보기(현재 -> 다음). 최대 레벨이면 비운다.")]
    [SerializeField] private TMP_Text detailNextEffectText;
    [Tooltip("강화 비용(각인석). 부족하면 빨간색. 각인 강화는 각인석만 소모한다.")]
    [SerializeField] private TMP_Text stoneCostText;
    [Tooltip("각인석 아이콘. 재화 카탈로그에서 자동으로 채운다(선택).")]
    [SerializeField] private Image stoneIcon;
    [Tooltip("각인 레벨이 계정 공용임을 알리는 안내(선택). 상시 표시해도 된다.")]
    [SerializeField] private GameObject accountWideHint;

    [Header("Currency")]
    [Tooltip("상단 재화 패널. 각인석/골드 CurrencyView 를 자식으로 배치한다(강화 비용 재화).")]
    [SerializeField] private CurrencyPanel currencyPanel;

    [Header("Buttons")]
    [Tooltip("장착/해제 겸용 버튼. 고른 각인이 이 슬롯에 끼워져 있으면 해제로, 아니면 장착으로 동작한다.")]
    [SerializeField] private Button equipButton;
    [Tooltip("장착/해제 버튼의 라벨. 상태에 따라 문구가 바뀐다.")]
    [SerializeField] private TMP_Text equipButtonLabel;
    [Tooltip("선택한 각인 레벨 +1(계정 공용).")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;

    private static readonly Color InsufficientColor = new Color(0.90f, 0.20f, 0.20f);
    private const string EquipLabel = "장착";
    private const string UnequipLabel = "해제";

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private int _charId;
    private int _slotIndex;
    private bool _busy;
    private bool _changed;

    private readonly List<EngravingCardView> _cards = new();
    // 카드와 1:1로 대응하는 각인 데이터. 데이터베이스에 빈 항목이 섞여 있어도 인덱스가 어긋나지 않게 따로 들고 간다.
    private readonly List<EngravingData> _cardData = new();
    private EngravingData _selected;
    private Color _costNormalColor = Color.white;
    private bool _costColorCaptured;

    protected override void Awake() {
        base.Awake();
        if (stoneCostText != null && !_costColorCaptured) {
            _costNormalColor = stoneCostText.color;
            _costColorCaptured = true;
        }
    }

    /// <param name="slotIndex">장착 대상 슬롯(0 부터 시작, EngravingRules.SlotCount 미만).</param>
    public void Initialize(MainHudContext ctx, int charDataId, int slotIndex) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();
        _charId = charDataId;
        _slotIndex = slotIndex;
        _changed = false;

        if (currencyPanel != null) currencyPanel.Initialize(ctx);
        CurrencyIcon.Apply(stoneIcon, ctx.Currencies, CurrencyType.EngravingStone);
        if (accountWideHint != null) accountWideHint.SetActive(true);

        WireButtons();
        BuildCards();

        // 현재 슬롯에 끼워져 있는 각인을 기본 선택으로 둔다(없으면 첫 보유 각인).
        int equippedId = ctx.Store.EngravingSlotsOf(charDataId)[slotIndex];
        Select(equippedId != 0 ? ctx.EngravingDb.Get(equippedId) : FirstOwned());
    }

    private void WireButtons() {
        if (equipButton != null) {
            equipButton.onClick.RemoveAllListeners();
            // 장착/해제를 한 버튼에 겹친다(둘을 나란히 두면 자리도 좁고, 지금 어느 쪽이 유효한지도 읽히지 않는다).
            equipButton.onClick.AddListener(() => {
                _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
                EquipAsync(EquippedHere ? 0 : (_selected != null ? _selected.Id : 0)).Forget();
            });
        }
        if (upgradeButton != null) {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); UpgradeAsync().Forget(); });
        }
        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
        }
    }

    // 데이터베이스에 등록된 모든 각인을 카드로 만든다(미보유 포함). 목록이 짧아 분할 생성이 필요 없다.
    private void BuildCards() {
        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();
        _cardData.Clear();

        if (cardPrefab == null || cardParent == null || _ctx.EngravingDb == null) return;

        foreach (var data in _ctx.EngravingDb.All) {
            if (data == null) continue;
            _cardData.Add(data);
        }

        // 계열끼리 묶어 보여준다(같은 계열을 비교하며 고르는 화면이라 데이터베이스 등록 순서보다 읽기 쉽다).
        // 같은 계열 안에서는 id 순이라 각인을 추가해도 기존 항목의 자리가 흔들리지 않는다.
        _cardData.Sort(static (a, b) => {
            int byCategory = ((int)a.Category).CompareTo((int)b.Category);
            return byCategory != 0 ? byCategory : a.Id.CompareTo(b.Id);
        });

        for (int i = 0; i < _cardData.Count; i++)
            _cards.Add(Instantiate(cardPrefab, cardParent));

        RefreshCards();
    }

    private void RefreshCards() {
        var slots = _ctx.Store.EngravingSlotsOf(_charId);

        for (int i = 0; i < _cards.Count; i++) {
            var data = _cardData[i];
            _ctx.Store.EngravingLevels.TryGetValue(data.Id, out int level);

            // 표식은 "어느 슬롯에든 끼워져 있는가", 선택 차단은 "다른 슬롯에 끼워져 있는가"로 나눈다.
            // 지금 슬롯의 각인도 장착 표식을 달아야 목록만 보고 상태를 알 수 있고, 해제하려면 고를 수는 있어야 한다.
            bool equippedElsewhere = IsEquippedInOtherSlot(slots, data.Id);
            bool equippedHere = IsValidSlotIndex(slots) && slots[_slotIndex] == data.Id;

            _cards[i].Bind(data, level, equippedHere || equippedElsewhere, equippedElsewhere, OnCardClicked);
            _cards[i].SetSelected(_selected != null && _selected.Id == data.Id);
        }
    }

    private bool IsValidSlotIndex(int[] slots) => _slotIndex >= 0 && _slotIndex < slots.Length;

    /// <summary>고른 각인이 지금 보고 있는 슬롯에 끼워져 있는지(장착/해제 버튼의 동작을 가른다).</summary>
    private bool EquippedHere {
        get {
            if (_selected == null) return false;
            var slots = _ctx.Store.EngravingSlotsOf(_charId);
            return IsValidSlotIndex(slots) && slots[_slotIndex] == _selected.Id;
        }
    }

    // 같은 캐릭터의 "다른" 슬롯에 이미 끼워져 있는지(현재 슬롯은 교체 대상이므로 제외).
    private bool IsEquippedInOtherSlot(int[] slots, int engravingId) {
        for (int i = 0; i < slots.Length; i++)
            if (i != _slotIndex && slots[i] == engravingId) return true;
        return false;
    }

    // 기본 선택은 "이 슬롯에 지금 낄 수 있는" 각인이어야 한다(다른 슬롯에 낀 것을 골라두면 카드가 비활성이라 조작이 막힌다).
    private EngravingData FirstOwned() {
        var slots = _ctx.Store.EngravingSlotsOf(_charId);
        for (int i = 0; i < _cardData.Count; i++) {
            var data = _cardData[i];
            if (!_ctx.Store.EngravingLevels.TryGetValue(data.Id, out int lv) || lv <= 0) continue;
            if (IsEquippedInOtherSlot(slots, data.Id)) continue;
            return data;
        }
        return null;
    }

    private void OnCardClicked(EngravingCardView card) {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        Select(card.Data);
    }

    private void Select(EngravingData data) {
        _selected = data;
        RefreshCards();
        RefreshDetail();
    }

    private void RefreshDetail() {
        int equippedId = _ctx.Store.EngravingSlotsOf(_charId)[_slotIndex];

        if (_selected == null) {
            if (detailNameText != null) detailNameText.text = "";
            if (detailEffectText != null) detailEffectText.text = "";
            if (detailNextEffectText != null) detailNextEffectText.text = "";
            if (equipButtonLabel != null) equipButtonLabel.text = EquipLabel;
            SetCostText(stoneCostText, false, 0, 0);
            SetInteractable(equipButton, false);
            SetInteractable(upgradeButton, false);
            return;
        }

        _ctx.Store.EngravingLevels.TryGetValue(_selected.Id, out int level);
        bool owned = level > 0;
        bool maxed = level >= EngravingRules.MaxLevel;

        if (detailNameText != null) detailNameText.text = _selected.DisplayName;
        if (detailEffectText != null) detailEffectText.text = _selected.Description(owned ? level : 1);
        if (detailNextEffectText != null)
            detailNextEffectText.text = owned && !maxed ? _selected.EffectSummary(level + 1) : "";

        int stoneCost = owned && !maxed ? EngravingRules.UpgradeStoneCost(level) : 0;
        int ownedStone = _ctx.Store.EngravingStone.Value;

        SetCostText(stoneCostText, owned && !maxed, stoneCost, ownedStone);

        SetInteractable(upgradeButton, owned && !maxed && ownedStone >= stoneCost);

        // 이 슬롯에 낀 각인을 고르면 해제, 아니면 장착. 어느 쪽도 불가능하면 장착 문구로 두고 비활성화한다.
        bool equippedHere = equippedId == _selected.Id;
        if (equipButtonLabel != null) equipButtonLabel.text = equippedHere ? UnequipLabel : EquipLabel;
        SetInteractable(equipButton, equippedHere || owned);
    }

    private void SetCostText(TMP_Text text, bool show, int cost, int owned) {
        if (text == null) return;
        text.text = show ? cost.ToString("N0") : "-";
        text.color = show && owned < cost ? InsufficientColor : _costNormalColor;
    }

    private static void SetInteractable(Button button, bool on) {
        if (button != null) button.interactable = on;
    }

    // ── 서버 액션 ──
    private async UniTaskVoid EquipAsync(int engravingId) {
        if (_busy) return;
        _busy = true;
        try {
            var res = await _ctx.Engraving.EquipAsync(_charId, _slotIndex, engravingId, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(EngravingRules.EquipFailMessage(res.FailReason));
                return;
            }
            _ctx.Store.ApplyEngravingEquip(res.CharDataId, res.SlotIndex, res.EngravingId);
            _changed = true;
            RefreshCards();
            RefreshDetail();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("각인 장착에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    private async UniTaskVoid UpgradeAsync() {
        if (_busy || _selected == null) return;
        _busy = true;
        try {
            var res = await _ctx.Engraving.UpgradeAsync(_selected.Id, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(EngravingRules.UpgradeFailMessage(res.FailReason));
                return;
            }
            _ctx.Store.ApplyEngravingUpgrade(res.EngravingId, res.Level, res.NewEngravingStone);
            _ctx.Sound.PlaySfx(SfxKey.Purchase);
            _changed = true;
            RefreshCards();
            RefreshDetail();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("각인 강화에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    /// <summary>이 팝업에서 장착/강화가 실제로 일어났는지(호출측이 화면을 다시 그릴지 판단).</summary>
    public bool Changed => _changed;
}
