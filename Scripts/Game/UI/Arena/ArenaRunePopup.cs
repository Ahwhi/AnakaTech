using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 쟁탈전 룬 장착 팝업(V1.12). 7계열 x 5레벨 룬을 계열 순서대로 늘어놓고(한 계열이 한 줄), 눌러서 끼고 뺀다.
///   계열마다 하나만 낄 수 있다. 같은 계열의 다른 레벨을 누르면 그것으로 바뀐다. 장착 중인 룬을 다시 누르면 뺀다.
///   총 코스트는 7 까지다(레벨 = 코스트). 넘는 룬은 누를 수 없음을 안내한다.
/// 바꾼 구성은 저장 버튼을 눌러야 서버에 반영된다. 저장하지 않고 닫으면 버린다.
/// ShowAsync 는 저장한 구성을 돌려준다(저장하지 않았으면 null).
/// </summary>
public sealed class ArenaRunePopup : UIView {
    [Header("Slots")]
    [Tooltip("룬 칸이 생성될 부모. 한 줄에 5칸(레벨 1~5)이 오도록 GridLayoutGroup(Constraint Column 5)을 두면 계열이 줄로 정렬된다.")]
    [SerializeField] private Transform slotContent;
    [SerializeField] private ArenaRuneSlot slotPrefab;
    [Tooltip("계열별 룬 아이콘 에셋(쟁탈전 화면과 같은 것을 넣는다). 비우면 아이콘 없이 그린다.")]
    [SerializeField] private ArenaRuneIconTable iconTable;

    [Header("Summary")]
    [Tooltip("사용 코스트(예: 5 / 7).")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("효과 목록 줄이 생성될 부모(VerticalLayoutGroup 권장). 장착한 계열마다 한 줄.")]
    [SerializeField] private Transform effectContent;
    [SerializeField] private ArenaRuneEffectRow effectRowPrefab;
    [Tooltip("선택. 장착한 룬이 하나도 없을 때 켜는 안내(예: 장착한 룬이 없습니다).")]
    [SerializeField] private GameObject emptyEffectNotice;

    [Header("Buttons")]
    [Tooltip("서버에 저장한다. 바뀐 것이 없으면 잠긴다.")]
    [SerializeField] private Button saveButton;
    [Tooltip("선택. 기본 구성(전 계열 1코스트)으로 되돌린다(저장은 따로).")]
    [SerializeField] private Button defaultButton;
    [Tooltip("선택. 전부 뺀다(저장은 따로).")]
    [SerializeField] private Button clearButton;
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private ArenaRuneLoadout _original;
    private ArenaRuneLoadout _draft;
    private ArenaRuneLoadout _saved;
    private UniTaskCompletionSource<ArenaRuneLoadout> _result;
    private readonly List<ArenaRuneSlot> _slots = new();
    private readonly List<ArenaRuneEffectRow> _effectRows = new();
    private bool _busy;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => { Click(); CloseAsync().Forget(); });
        if (saveButton != null) saveButton.onClick.AddListener(() => { Click(); SaveAsync().Forget(); });
        if (defaultButton != null) defaultButton.onClick.AddListener(() => { Click(); _draft = ArenaRuneLoadout.Default(); Refresh(); });
        if (clearButton != null) clearButton.onClick.AddListener(() => { Click(); _draft = new ArenaRuneLoadout(); Refresh(); });
    }

    /// <summary>현재 구성으로 열고, 닫힐 때까지 기다린다. 저장했으면 저장한 구성, 아니면 null.</summary>
    public async UniTask<ArenaRuneLoadout> ShowAsync(MainHudContext ctx, ArenaRuneLoadout current) {
        _ctx = ctx;
        _original = (current ?? ArenaRuneLoadout.Default()).Clone();
        _draft = _original.Clone();
        _saved = null;
        _result = new UniTaskCompletionSource<ArenaRuneLoadout>();

        BuildSlots();
        Refresh();
        await base.ShowAsync();
        return await _result.Task;
    }

    // 룬 칸(계열 x 레벨)과 효과 줄(계열마다 1줄)을 한 번에 만든다. 효과 줄은 장착 여부에 따라 켜고 끈다.
    private void BuildSlots() {
        ClearSlots();
        for (int f = 0; f < ArenaRuneRules.FamilyCount; f++) {
            var family = (ArenaRuneRules.Family)f;
            var icon = iconTable != null ? iconTable.IconOf(family) : null;
            if (slotContent != null && slotPrefab != null) {
                for (int lv = 1; lv <= ArenaRuneRules.MaxLevel; lv++) {
                    var slot = Instantiate(slotPrefab, slotContent);
                    slot.Bind(family, lv, icon, OnSlotClick);
                    _slots.Add(slot);
                }
            }
            if (effectContent != null && effectRowPrefab != null) {
                var row = Instantiate(effectRowPrefab, effectContent);
                row.Bind(family, icon);
                _effectRows.Add(row);
            }
        }
    }

    private void OnSlotClick(ArenaRuneSlot slot) {
        Click();
        int current = _draft.LevelOf(slot.Family);

        // 장착 중인 룬을 다시 누르면 뺀다.
        if (current == slot.Level) {
            _draft.Set(slot.Family, 0);
            Refresh();
            return;
        }

        // 같은 계열은 하나만 끼므로 기존 것을 빼고 센 코스트로 판단한다.
        if (_draft.CostIfSet(slot.Family, slot.Level) > ArenaRuneRules.MaxCost) {
            _ctx.Ui.ShowToast($"코스트가 부족합니다. (최대 {ArenaRuneRules.MaxCost})");
            return;
        }
        _draft.Set(slot.Family, slot.Level);
        Refresh();
    }

    private void Refresh() {
        foreach (var s in _slots) {
            if (s == null) continue;
            bool equipped = _draft.LevelOf(s.Family) == s.Level;
            bool overCost = _draft.CostIfSet(s.Family, s.Level) > ArenaRuneRules.MaxCost;
            s.Refresh(equipped, overCost);
        }
        if (costText != null) costText.text = ArenaRuneRules.CostText(_draft.TotalCost);
        foreach (var row in _effectRows) if (row != null) row.SetLevel(_draft.LevelOf(row.Family));
        if (emptyEffectNotice != null) emptyEffectNotice.SetActive(_draft.TotalCost == 0);
        if (saveButton != null) saveButton.interactable = !_busy && !_draft.SameAs(_original);
    }

    private async UniTaskVoid SaveAsync() {
        if (_busy || _draft.SameAs(_original)) return;
        _busy = true;
        Refresh();
        try {
            var res = await _ctx.Arena.SetRunesAsync(_draft, this.GetCancellationTokenOnDestroy());
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(ArenaRuneRules.FailMessage(res.FailReason));
                return;
            }
            _saved = res.Runes;
            _original = res.Runes.Clone();
            _draft = res.Runes.Clone();
            _ctx.Ui.ShowToast("룬을 저장했습니다.");
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast(ArenaRuneRules.FailMessage(-1));
        } finally {
            _busy = false;
            Refresh();
        }
    }

    private void Click() {
        if (_ctx != null) _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
    }

    private void ClearSlots() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
        foreach (var r in _effectRows) if (r != null) Destroy(r.gameObject);
        _effectRows.Clear();
    }

    // 버튼 외의 경로(배경 탭, 일괄 닫힘)로 닫혀도 결과를 반드시 확정한다(기다리는 쪽이 멈추지 않게).
    protected override void OnClosed() {
        _result?.TrySetResult(_saved);
        ClearSlots();
    }
}
