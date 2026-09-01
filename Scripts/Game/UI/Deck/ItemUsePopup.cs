using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 아이템 사용 팝업. 지정한 캐릭터에게 쓸 수 있는 소모품을 나열하고, 고른 아이템을 서버에 사용 요청한다.
///
/// 이 팝업은 아이템의 효과를 전혀 모른다. 목록에 올릴지는 ItemData.RequiresTarget 만 보고 판단하고
/// 실제 효과/지급량은 서버가 정하므로, 경험치 쿠폰 외의 아이템이 늘어도 여기는 수정하지 않는다.
/// </summary>
public sealed class ItemUsePopup : UIView {
    [Header("List")]
    [SerializeField] private Transform slotParent;
    [SerializeField] private ItemSlotView slotPrefab;
    [Tooltip("쓸 수 있는 아이템이 하나도 없을 때 켜는 안내. 선택.")]
    [SerializeField] private GameObject emptyHint;

    [Header("Detail")]
    [Tooltip("대상 캐릭터 이름/레벨 표시. 선택.")]
    [SerializeField] private TMP_Text targetText;
    [Tooltip("선택한 아이템 설명. 선택.")]
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("사용 버튼. 아이템을 고르기 전에는 비활성.")]
    [SerializeField] private Button useButton;
    [Tooltip("일괄 사용 버튼. 고른 아이템을 보유한 개수만큼 한 번에 사용한다(확인을 받는다). 선택.")]
    [SerializeField] private Button useAllButton;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private int _charId;
    private bool _busy;

    private readonly List<ItemSlotView> _slots = new();
    private ItemData _selected;

    /// <summary>이 팝업에서 아이템을 실제로 사용했는지(호출측이 상세 표시를 다시 그릴지 판단).</summary>
    public bool Changed { get; private set; }

    public void Initialize(MainHudContext ctx, int charDataId) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();
        _charId = charDataId;
        Changed = false;

        if (useButton != null) {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); UseAsync(_selected, 1).Forget(); });
        }
        if (useAllButton != null) {
            useAllButton.onClick.RemoveAllListeners();
            useAllButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); UseAllAsync().Forget(); });
        }
        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
        }

        RefreshTarget();
        Refresh();
    }

    private void RefreshTarget() {
        if (targetText == null) return;

        var data = _ctx.CharDb != null ? _ctx.CharDb.Get(_charId) : null;
        var progress = _ctx.Store.GetProgress(_charId);
        targetText.text = data != null && progress != null
            ? $"{data.DisplayName}  Lv.{progress.Level}"
            : data != null ? data.DisplayName : "";
    }

    /// <summary>스토어의 최신 보유량으로 목록을 다시 그린다.</summary>
    public void Refresh() {
        if (_ctx == null) return;

        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();

        if (_ctx.ItemDb != null && slotPrefab != null && slotParent != null) {
            // 데이터베이스 등록 순서로 나열한다(보유 순서로 하면 아이템을 쓸 때마다 자리가 바뀐다).
            foreach (var data in _ctx.ItemDb.All) {
                if (data == null || !data.RequiresTarget) continue;

                int count = _ctx.Store.ItemCount(data.Id);
                if (count <= 0) continue;

                var view = Instantiate(slotPrefab, slotParent);
                view.Bind(data, count, OnSlotClicked);
                view.SetSelected(_selected != null && _selected.Id == data.Id);
                _slots.Add(view);
            }
        }

        // 다 써서 목록에서 빠졌으면 선택을 비운다(없는 아이템을 쓰려는 요청을 막는다).
        if (_selected != null && _ctx.Store.ItemCount(_selected.Id) <= 0) _selected = null;

        if (emptyHint != null) emptyHint.SetActive(_slots.Count == 0);
        RefreshDetail();
    }

    private void OnSlotClicked(ItemSlotView view) {
        if (_busy || view == null) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);

        _selected = view.Data;
        foreach (var s in _slots)
            if (s != null) s.SetSelected(_selected != null && s.Data != null && s.Data.Id == _selected.Id);
        RefreshDetail();
    }

    private void RefreshDetail() {
        if (descriptionText != null)
            descriptionText.text = _selected != null ? _selected.Description : "";

        bool canUse = _selected != null && _charId != 0 && !_busy;
        if (useButton != null) useButton.interactable = canUse;
        if (useAllButton != null) useAllButton.interactable = canUse;
    }

    /// <summary>
    /// 고른 아이템을 보유한 개수만큼 한 번에 사용한다. 한 개씩 쓸 때와 달리 되돌릴 수 없는 양이 한 번에
    /// 나가므로 수량을 밝히고 확인을 받는다(사용 자체는 아래 UseAsync 와 같은 경로다).
    /// </summary>
    private async UniTaskVoid UseAllAsync() {
        if (_busy || _selected == null || _charId == 0) return;

        // 확인을 기다리는 동안 선택이 바뀔 수 있으므로 지금 고른 아이템을 붙잡아 둔다.
        var item = _selected;
        int count = _ctx.Store.ItemCount(item.Id);
        if (count < 1) return;

        try {
            var reference = _ctx.Popups != null ? _ctx.Popups.ConfirmCancelPopup : null;
            if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

            var confirm = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(reference, _ct);
            if (!await confirm.ShowAsync("일괄 사용", $"{item.DisplayName} {count:N0}개를 모두 사용합니다."))
                return;

            // 확인을 받는 사이에 다른 경로로 수량이 줄었을 수 있어 보낼 직전 값으로 다시 센다
            // (많이 보내면 서버가 수량 부족으로 통째로 거절한다).
            await UseAsync(item, _ctx.Store.ItemCount(item.Id));
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("아이템을 사용하지 못했습니다.");
        }
    }

    // 사용 버튼(1개)과 일괄 사용 버튼(보유 전량)이 함께 쓰는 경로. 수량만 다르고 나머지는 같다.
    private async UniTask UseAsync(ItemData item, int count) {
        if (_busy || item == null || _charId == 0 || count < 1) return;
        _busy = true;
        RefreshDetail();

        try {
            var res = await _ctx.Item.UseAsync(item.Id, _charId, count, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(ItemRules.UseFailMessage(res.FailReason));
                return;
            }

            _ctx.Store.ApplyItemUse(res);
            _ctx.Sound.PlaySfx(SfxKey.Claim);
            Changed = true;

            if (res.CharExp != null && res.CharExp.GainedExp > 0)
                _ctx.Ui.ShowToast($"경험치 {res.CharExp.GainedExp:N0} 획득");
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("아이템을 사용하지 못했습니다.");
        } finally {
            _busy = false;
            RefreshTarget();   // 레벨이 올랐을 수 있다
            Refresh();         // 잔여 수량 반영(0 이 되면 목록에서 빠진다)
        }
    }
}
