using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 장비 화면(DeckPopup 내부 패널). 기본 화면에는 선택 캐릭터의 착용 슬롯 4개만 두고,
/// 보유 목록/상세/각성은 각각 별도 패널을 켜고 끄는 구조다.
///
///   착용 슬롯 클릭  -> 장착 장비가 있으면 상세 패널, 없으면 그 부위로 가방 패널
///   가방 버튼       -> 가방 패널 온오프
///   가방 카드 클릭  -> 같은 상세 패널(착용/미착용 공통)
///   상세의 각성     -> 각성 패널
///
/// 서버 권위 액션(장착/해제/각성/레벨업) 성공 시 인벤토리를 다시 받아 전체를 동기화한다.
/// 각 패널은 목록/표시만 책임지고, 화면 전환과 통신은 이 클래스가 담당한다(역할 분리).
/// </summary>
public sealed class EquipmentPopup : UIView {
    [Header("Page Switch (장비 / 각인)")]
    [Tooltip("장비 페이지 버튼. 육성 팝업의 스탯/별 전환과 같은 방식이다.")]
    [SerializeField] private Button equipPageButton;
    [Tooltip("각인 페이지 버튼.")]
    [SerializeField] private Button engravingPageButton;
    [Tooltip("장비 페이지 루트. 기존 장비 UI(착용 슬롯/가방/상세/각성)를 통째로 담는다.")]
    [SerializeField] private GameObject equipPage;
    [Tooltip("각인 페이지 루트(EngravingPanel 을 담는다).")]
    [SerializeField] private GameObject engravingPage;
    [Tooltip("각인 페이지 패널. 각인 슬롯과 효과 요약을 담당한다.")]
    [SerializeField] private EngravingPanel engravingPanel;
    [Tooltip("각인 페이지 버튼 배지: 사용 가능한(해금됐는데 비어 있는) 슬롯이 있으면 표시(선택).")]
    [SerializeField] private BadgeView engravingBadge;

    [Header("Equipped Slots")]
    [SerializeField] private EquipmentSlotView[] slots;
    [Tooltip("선택 캐릭터의 총 전투력 표시(장비/각인/본부 반영). 선택.")]
    [SerializeField] private TMP_Text combatPowerText;

    [Header("Panels")]
    [Tooltip("보유 장비 목록 패널.")]
    [SerializeField] private EquipmentBagPanel bagPanel;
    [Tooltip("장비 상세 패널(착용/가방 공통).")]
    [SerializeField] private EquipmentDetailPanel detailPanel;
    [Tooltip("각성 패널.")]
    [SerializeField] private EquipmentAwakenPanel awakenPanel;
    [Tooltip("한계돌파 패널.")]
    [SerializeField] private EquipmentLimitBreakPanel limitBreakPanel;

    [Header("Buttons")]
    [Tooltip("가방 패널을 켜고 끄는 버튼. 각인 페이지에서는 쓸 일이 없어 숨긴다.")]
    [SerializeField] private Button bagButton;
    [Tooltip("가방 버튼 안의 아이콘. 가방이 열렸는지에 따라 아래 두 스프라이트로 바뀐다(선택).")]
    [SerializeField] private Image bagButtonIcon;
    [SerializeField] private Sprite bagClosedSprite;
    [SerializeField] private Sprite bagOpenSprite;
    [SerializeField] private Button closeButton;

    private MainHudContext _hud;
    private int _charId;
    private CancellationToken _ct;

    private EquipmentInstanceDto _selected;
    private bool _busy;
    private bool _wired;   // 리스너 1회 구성 가드(내장 패널로 재사용되므로 중복 리스너 방지).

    // 현재 보고 있는 페이지(0=장비, 1=각인). 캐릭터를 바꿔 재초기화해도 보던 페이지를 유지한다(GrowthPopup 과 동일 규칙).
    private int _currentPage;

    public void Initialize(MainHudContext hud, int charDataId) {
        _hud = hud;
        _charId = charDataId;
        _ct = this.GetCancellationTokenOnDestroy();

        if (!_wired) {
            if (bagButton != null)
                bagButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); ToggleBag(); });
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            if (equipPageButton != null)
                equipPageButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); ShowPage(0); });
            if (engravingPageButton != null)
                engravingPageButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); ShowPage(1); });
            // 각인 변경은 전투력에 영향을 주므로 상단 전투력 표시를 함께 갱신한다.
            if (engravingPanel != null) engravingPanel.Changed += RefreshCombatPower;

            if (detailPanel != null) {
                detailPanel.EquipRequested += OnEquip;
                detailPanel.UnequipRequested += OnUnequip;
                detailPanel.LevelUpRequested += () => OnLevelUp().Forget();
                detailPanel.AwakenRequested += OpenAwaken;
                detailPanel.LimitBreakRequested += OpenLimitBreak;
                detailPanel.DisassembleRequested += () => OnDisassembleOne().Forget();
                detailPanel.CloseRequested += CloseDetail;
            }
            if (bagPanel != null) bagPanel.DisassembleRequested += () => OnDisassembleBulk().Forget();
            if (awakenPanel != null) {
                awakenPanel.Awakened += () => OnAwakened().Forget();
                awakenPanel.CloseRequested += CloseAwaken;
            }
            if (limitBreakPanel != null) {
                limitBreakPanel.LimitBroken += () => OnLimitBroken().Forget();
                limitBreakPanel.CloseRequested += CloseLimitBreak;
            }
            _wired = true;
        }

        // 재사용(재오픈/캐릭터 전환) 시 선택과 패널 상태를 초기화한다.
        _selected = null;
        SetActiveSafe(detailPanel, false);
        SetActiveSafe(awakenPanel, false);
        SetActiveSafe(limitBreakPanel, false);
        SetActiveSafe(bagPanel, false);

        bagPanel?.Initialize(_hud, _charId, OnBagCardClicked);
        detailPanel?.Initialize(_hud, _charId);
        engravingPanel?.Initialize(_hud, _charId);

        ShowPage(_currentPage);   // 캐릭터를 바꿔도 보던 페이지 유지(최초 오픈은 0=장비).
        RefreshSlots();
        RefreshCombatPower();
    }

    // ── 페이지 전환(장비 / 각인) ──
    private void ShowPage(int index) {
        _currentPage = index;
        if (equipPage != null) equipPage.SetActive(index == 0);
        if (engravingPage != null) engravingPage.SetActive(index == 1);
        // 현재 페이지 버튼을 비활성으로 두어 선택 상태를 표시한다(GrowthPopup 과 동일).
        if (equipPageButton != null) equipPageButton.interactable = index != 0;
        if (engravingPageButton != null) engravingPageButton.interactable = index != 1;

        // 각인 페이지로 넘어가면 가방은 볼 일이 없으므로 닫는다(열린 채로 두면 뒤에 남아 페이지가 겹쳐 보인다).
        if (index != 0) SetBagOpen(false);
        RefreshBagButton();

        // 각인 페이지로 들어올 때마다 최신 상태로 다시 그린다(별 승급으로 슬롯이 열렸을 수 있다).
        if (index == 1) engravingPanel?.Refresh();
        RefreshEngravingBadge();
    }

    // 해금됐는데 비어 있는 각인 슬롯이 있으면 배지를 켠다("낄 수 있는데 안 꼈다"는 안내).
    private void RefreshEngravingBadge() {
        if (engravingBadge == null) return;

        var progress = _hud.Store.GetProgress(_charId);
        int star = progress != null ? progress.StarLevel : 0;
        var equipped = _hud.Store.EngravingSlotsOf(_charId);

        bool hasOwnedEngraving = false;
        foreach (var kv in _hud.Store.EngravingLevels)
            if (kv.Value > 0) { hasOwnedEngraving = true; break; }

        bool emptyUnlockedSlot = false;
        for (int i = 0; i < equipped.Length; i++)
            if (EngravingRules.IsSlotUnlocked(i, star) && equipped[i] == 0) { emptyUnlockedSlot = true; break; }

        engravingBadge.SetOn(hasOwnedEngraving && emptyUnlockedSlot);
    }

    // ── 화면 전환 ──

    private void ToggleBag() {
        if (bagPanel == null) return;
        bool open = !bagPanel.gameObject.activeSelf;
        SetBagOpen(open);
        if (open) bagPanel.SetFilter(EquipmentBagPanel.Filter.All);
    }

    private void OpenBagForSlot(EquipmentSlot slot) {
        if (bagPanel == null) return;
        SetBagOpen(true);
        bagPanel.SetFilterBySlot(slot);
    }

    // 가방 열림/닫힘은 반드시 이 한 곳을 통과시킨다. 슬롯 클릭처럼 버튼을 거치지 않는 경로로 열려도
    // 버튼 아이콘이 실제 상태와 어긋나지 않게 하기 위함이다.
    private void SetBagOpen(bool open) {
        if (bagPanel == null) return;
        if (open) {
            CloseAwaken();
            CloseLimitBreak();
        }
        bagPanel.gameObject.SetActive(open);
        RefreshBagButton();
    }

    // 가방 버튼의 표시 상태(각인 페이지에서는 숨김 + 열림/닫힘 아이콘).
    private void RefreshBagButton() {
        bool onEquipPage = _currentPage == 0;
        if (bagButton != null && bagButton.gameObject.activeSelf != onEquipPage)
            bagButton.gameObject.SetActive(onEquipPage);

        if (bagButtonIcon == null) return;
        bool open = bagPanel != null && bagPanel.gameObject.activeSelf;
        var sprite = open ? bagOpenSprite : bagClosedSprite;
        if (sprite != null) bagButtonIcon.sprite = sprite;
    }

    private void OpenDetail(EquipmentInstanceDto inst) {
        if (inst == null || detailPanel == null) return;
        _selected = inst;
        detailPanel.gameObject.SetActive(true);
        detailPanel.Bind(inst);

        // 각성 패널이 열린 채로 다른 장비를 고르면 대상도 함께 바꿔 준다.
        // (안 하면 처음 연 장비의 각성 상태가 그대로 남아 상세 패널과 서로 다른 장비를 가리킨다.)
        if (awakenPanel != null && awakenPanel.gameObject.activeSelf)
            awakenPanel.Initialize(_hud, _charId, inst.InstanceId);
        if (limitBreakPanel != null && limitBreakPanel.gameObject.activeSelf)
            limitBreakPanel.Initialize(_hud, inst.InstanceId);

        RefreshSelectionMarks();
    }

    private void CloseDetail() {
        SetActiveSafe(detailPanel, false);
        _selected = null;
        RefreshSelectionMarks();
    }

    private void OpenAwaken() {
        if (_selected == null || awakenPanel == null) return;
        CloseLimitBreak();   // 각성과 한계돌파는 같은 자리에 뜨는 패널이라 하나만 연다.
        awakenPanel.gameObject.SetActive(true);
        awakenPanel.Initialize(_hud, _charId, _selected.InstanceId);
    }

    private void CloseAwaken() => SetActiveSafe(awakenPanel, false);

    private void OpenLimitBreak() {
        if (_selected == null || limitBreakPanel == null) return;
        CloseAwaken();
        limitBreakPanel.gameObject.SetActive(true);
        limitBreakPanel.Initialize(_hud, _selected.InstanceId);
    }

    private void CloseLimitBreak() => SetActiveSafe(limitBreakPanel, false);

    private static void SetActiveSafe(Component c, bool on) {
        if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
    }

    // ── 표시 갱신 ──

    private void RefreshSlots() {
        if (slots == null) return;
        foreach (var slotView in slots) {
            if (slotView == null) continue;
            var inst = _hud.Store.EquippedItem(_charId, slotView.Slot, _hud.EquipDb);
            var data = inst != null ? _hud.EquipDb.Get(inst.EquipDataId) : null;
            slotView.Bind(inst, data, _charId, OnSlotClicked);
            // 재료/다이아 보유량까지 봐야 하는 판정이라 슬롯이 아니라 여기서 구한다.
            slotView.SetAwakenReady(_hud.Store.CanAwakenNow(inst, _charId));
        }
        RefreshSelectionMarks();
    }

    private void RefreshSelectionMarks() {
        if (slots != null)
            foreach (var s in slots) {
                if (s == null) continue;
                s.SetSelected(_selected != null && s.Inst != null && s.Inst.InstanceId == _selected.InstanceId);
            }
        bagPanel?.SetSelected(_selected != null ? _selected.InstanceId : 0);
    }

    private void RefreshCombatPower() {
        if (combatPowerText == null) return;
        var data = _hud.CharDb != null ? _hud.CharDb.Get(_charId) : null;
        var progress = _hud.Store.GetProgress(_charId);
        if (data == null || progress == null) { combatPowerText.text = "-"; return; }

        float power = _hud.Store.RuntimeFor(data, progress, _hud.Settings, _hud.EquipDb).CombatPower;
        combatPowerText.text = power.ToString("N0");
    }

    // ── 입력 ──

    private void OnSlotClicked(EquipmentSlotView slotView) {
        if (slotView == null) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);

        if (slotView.Inst != null) {
            OpenDetail(slotView.Inst);            // 장착 장비: 상세(해제/각성/레벨업)
        } else {
            CloseDetail();
            OpenBagForSlot(slotView.Slot);         // 빈 슬롯: 그 부위 가방
        }
    }

    private void OnBagCardClicked(EquipmentCard card) {
        if (card?.Inst == null) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenDetail(card.Inst);
    }

    // ── 서버 액션 ──

    private void OnEquip() {
        if (_selected == null || _busy) return;
        long id = _selected.InstanceId;
        RunAsync(async () => {
            var res = await _hud.Inventory.EquipAsync(id, _charId, _ct);
            if (res.IsSuccess) await RefreshInventoryAsync();
            else _hud.Ui.ShowToast(EquipFailMessage(res.FailReason));
        }).Forget();
    }

    private void OnUnequip() {
        if (_selected == null || _busy) return;
        long id = _selected.InstanceId;
        RunAsync(async () => {
            var res = await _hud.Inventory.UnequipAsync(id, _ct);
            if (res.IsSuccess) await RefreshInventoryAsync();
            else _hud.Ui.ShowToast("해제할 수 없습니다.");
        }).Forget();
    }

    private async UniTaskVoid OnLevelUp() {
        if (_selected == null || _busy) return;
        long id = _selected.InstanceId;
        int cost = EquipmentRules.LevelUpGold(_selected.Level);
        if (_hud.Store.Gold.Value < cost) { _hud.Ui.ShowToast("골드가 부족합니다."); return; }

        await RunAsync(async () => {
            var res = await _hud.Inventory.LevelUpAsync(id, 1, _ct);
            if (!res.IsSuccess) { _hud.Ui.ShowToast(LevelUpFailMessage(res.FailReason)); return; }
            _hud.Store.Gold.Value = res.NewGold;   // 서버 권위 잔액 즉시 반영
            // 연출은 인벤토리 재동기화보다 먼저 낸다(전역 VFX 레이어에 이미 떠 있으므로 재바인딩에 영향받지 않는다).
            detailPanel?.PlayLevelUpVfx();
            await RefreshInventoryAsync();
            RefreshEventBadges();
        });
    }

    /// <summary>
    /// 상세 화면의 개별 분해. 되돌릴 수 없는 조작이라 장비 이름과 보상 골드를 보여 주고 한 번 확인받는다.
    /// 각성/레벨업한 장비도 여기서는 분해할 수 있다(유저가 그 하나를 직접 지목한 경우다).
    /// </summary>
    private async UniTaskVoid OnDisassembleOne() {
        if (_selected == null || _busy) return;

        // 장착 중인 장비는 분해하지 않는다. 상세 패널이 이미 버튼을 숨기지만, 그것은 표시 상태일 뿐이라
        // 여기서도 값으로 확인한다(서버도 같은 이유로 한 번 더 막는다).
        if (_selected.IsEquipped) { _hud.Ui.ShowToast("장착 중인 장비는 분해할 수 없습니다."); return; }

        var data = _hud.EquipDb != null ? _hud.EquipDb.Get(_selected.EquipDataId) : null;
        if (data == null || !EquipmentRules.CanDisassemble(_selected, data)) return;

        long id = _selected.InstanceId;
        int gold = EquipmentRules.DisassembleGold(data.Grade);

        // 확인 팝업 대기까지 RunAsync 안에서 한다. 팝업이 떠 있는 동안 장비 화면이 닫히면 취소 예외가 나는데,
        // 바깥에 두면 그것을 받아 줄 곳이 없다(중복 입력도 이 구간부터 막혀야 한다).
        await RunAsync(async () => {
            var confirm = await _hud.Ui.ShowPopupAsync<ConfirmCancelPopup>(_hud.Popups.ConfirmCancelPopup, _ct);
            bool ok = await confirm.ShowAsync("장비 분해",
                $"'{data.DisplayName}' 을(를) 분해할까요?\n골드 {gold:N0} 을(를) 얻습니다.\n분해한 장비는 되돌릴 수 없습니다.");
            if (!ok) return;

            var res = await _hud.Inventory.DisassembleAsync(new[] { id }, _ct);
            if (!res.IsSuccess) { _hud.Ui.ShowToast(DisassembleFailMessage(res.FailReason)); return; }
            await ApplyDisassembledAsync(res);
        });
    }

    /// <summary>
    /// 가방의 일괄 분해. 팝업에서 등급을 고르면 그 등급을 서버에 넘긴다(어떤 장비가 대상인지는 서버가 정한다).
    /// 가방이 전송 상한을 넘겨 목록이 잘린 계정에서도 서버가 보이지 않는 장비까지 정리하므로,
    /// 결과 개수는 팝업의 미리보기가 아니라 서버가 돌려준 값을 쓴다.
    /// </summary>
    private async UniTaskVoid OnDisassembleBulk() {
        if (_busy) return;

        // 팝업 대기까지 RunAsync 안에서 한다(개별 분해와 같은 이유).
        await RunAsync(async () => {
            var popup = await _hud.Ui.ShowPopupAsync<EquipmentDisassemblePopup>(
                _hud.Popups.EquipmentDisassemblePopup, _ct);
            var grades = await popup.ShowAsync(_hud);
            if (grades == null || grades.Count == 0) return;

            var res = await _hud.Inventory.DisassembleByGradeAsync(grades, _ct);
            if (!res.IsSuccess) { _hud.Ui.ShowToast(DisassembleFailMessage(res.FailReason)); return; }
            await ApplyDisassembledAsync(res);
        });
    }

    // 분해 성공 후 공통 처리: 서버 권위 잔액을 먼저 반영하고 인벤토리를 다시 받는다.
    private async UniTask ApplyDisassembledAsync(DisassembleResultDto res) {
        _hud.Store.Gold.Value = res.NewGold;
        _hud.Ui.ShowToast($"장비 {res.Count:N0}개를 분해해 골드 {res.GainedGold:N0} 을(를) 얻었습니다.");
        await RefreshInventoryAsync();
    }

    private async UniTaskVoid OnAwakened() {
        await RefreshInventoryAsync();
        // 각성 패널이 열려 있으면 새 별레벨/재료 목록으로 갱신한다(연속 각성 가능).
        if (awakenPanel != null && awakenPanel.gameObject.activeSelf) awakenPanel.Refresh();
        RefreshEventBadges();
    }

    // 한계돌파 성공: 돌파 상태(스탯 배수)를 인벤토리로 다시 받아 슬롯/전투력/상세를 함께 갱신한다.
    private async UniTaskVoid OnLimitBroken() {
        await RefreshInventoryAsync();
        if (limitBreakPanel != null && limitBreakPanel.gameObject.activeSelf) limitBreakPanel.Refresh();
    }

    // 강화/각성은 장비 이벤트 미션의 진행 지표다. 달성되는 순간 메인 화면 이벤트 버튼에도 불이 들어오도록
    // 충족 상태만 가볍게 다시 받아온다(공지 목록은 캐시를 그대로 쓴다).
    private void RefreshEventBadges() {
        if (_hud != null && _hud.Badges != null) _hud.Badges.RefreshEventClaimStatesAsync(_ct).Forget();
    }

    // 통신 중 중복 입력을 막고 예외를 한 곳에서 처리한다. 결과를 기다려야 하는 호출부는 await,
    // 아니면 Forget() 으로 던진다(UniTask 반환이라 두 방식 모두 가능).
    private async UniTask RunAsync(Func<UniTask> action) {
        _busy = true;
        try { await action(); }
        catch (OperationCanceledException) { }
        catch (Exception e) { Debug.LogException(e); _hud.Ui.ShowToast("처리에 실패했습니다."); }
        finally { _busy = false; }
    }

    private async UniTask RefreshInventoryAsync() {
        var inv = await _hud.Inventory.RequestInventoryAsync(_ct);
        _hud.Store.ApplyInventory(inv);
        RefreshAllFromStore();
    }

    /// <summary>스토어의 최신 값으로 전체를 다시 그린다(외부에서 인벤토리가 바뀐 뒤에도 호출 가능).</summary>
    public void RefreshAllFromStore() {
        // 선택 인스턴스를 최신 스토어 값으로 재바인딩(각성 재료로 소모돼 사라졌을 수 있다).
        if (_selected != null && _hud.Store.Equipment.TryGetValue(_selected.InstanceId, out var latest))
            _selected = latest;
        else
            _selected = null;

        RefreshSlots();
        RefreshCombatPower();
        RefreshEngravingBadge();
        if (bagPanel != null && bagPanel.gameObject.activeSelf) bagPanel.Rebuild();

        if (_selected != null) detailPanel?.Bind(_selected);
        else {
            CloseDetail();
            CloseAwaken();
            CloseLimitBreak();
        }
    }

    private static string EquipFailMessage(int fail) => fail switch {
        7 => "이 캐릭터가 장착할 수 없는 장비입니다.",
        _ => "장착할 수 없습니다."
    };

    private static string DisassembleFailMessage(int fail) => fail switch {
        1 => "분해할 수 있는 장비가 없습니다.",
        _ => "분해할 수 없습니다."
    };

    private static string LevelUpFailMessage(int fail) => fail switch {
        2 => "골드가 부족합니다.",
        3 => "각성을 먼저 해야 레벨을 더 올릴 수 있습니다.",
        _ => "레벨업할 수 없습니다."
    };
}
