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
                detailPanel.CloseRequested += CloseDetail;
            }
            if (awakenPanel != null) {
                awakenPanel.Awakened += () => OnAwakened().Forget();
                awakenPanel.CloseRequested += CloseAwaken;
            }
            _wired = true;
        }

        // 재사용(재오픈/캐릭터 전환) 시 선택과 패널 상태를 초기화한다.
        _selected = null;
        SetActiveSafe(detailPanel, false);
        SetActiveSafe(awakenPanel, false);
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
        if (open) CloseAwaken();
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

        RefreshSelectionMarks();
    }

    private void CloseDetail() {
        SetActiveSafe(detailPanel, false);
        _selected = null;
        RefreshSelectionMarks();
    }

    private void OpenAwaken() {
        if (_selected == null || awakenPanel == null) return;
        awakenPanel.gameObject.SetActive(true);
        awakenPanel.Initialize(_hud, _charId, _selected.InstanceId);
    }

    private void CloseAwaken() => SetActiveSafe(awakenPanel, false);

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

        var bonus = _hud.Store.TotalBonusFor(_charId, _hud.EquipDb);
        float power = new CharacterRuntime(data, progress, _hud.Settings, bonus).CombatPower;
        combatPowerText.text = Mathf.RoundToInt(power).ToString("N0");
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

    private async UniTaskVoid OnAwakened() {
        await RefreshInventoryAsync();
        // 각성 패널이 열려 있으면 새 별레벨/재료 목록으로 갱신한다(연속 각성 가능).
        if (awakenPanel != null && awakenPanel.gameObject.activeSelf) awakenPanel.Refresh();
        RefreshEventBadges();
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
        }
    }

    private static string EquipFailMessage(int fail) => fail switch {
        7 => "이 캐릭터가 장착할 수 없는 장비입니다.",
        _ => "장착할 수 없습니다."
    };

    private static string LevelUpFailMessage(int fail) => fail switch {
        2 => "골드가 부족합니다.",
        3 => "각성을 먼저 해야 레벨을 더 올릴 수 있습니다.",
        _ => "레벨업할 수 없습니다."
    };
}
