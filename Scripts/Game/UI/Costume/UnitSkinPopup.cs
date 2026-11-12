using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 아웃핏 화면(V1.12, DeckPopup 내부 패널. 코스튬 패널과 같은 방식으로 도킹해 열린다).
///
/// 첫 칸은 항상 "기본"(아웃핏 해제)이고, 그 뒤로 아웃핏 전체를 보여 준다. 칸마다 남은 개수 / 보유 개수가 보이며,
/// 남은 개수가 없으면(전부 다른 소녀가 끼고 있거나 미보유) 이 소녀는 낄 수 없다.
/// 한 개는 한 소녀만 끼므로, 다른 소녀가 끼고 있는 것을 쓰려면 그 소녀에게서 먼저 해제해야 한다.
///
/// 장착은 서버 권위다. 응답이 성공일 때만 저장소에 반영하며, 저장소가 성장 변경 통로로 알려
/// 전투력/능력치 표시가 함께 따라온다. 아웃핏은 다음 전투부터 유닛 모습에 반영된다.
/// </summary>
public sealed class UnitSkinPopup : UIView {
    [Header("List")]
    [Tooltip("아웃핏 칸이 생성될 부모(Grid/Vertical Layout).")]
    [SerializeField] private Transform content;
    [Tooltip("아웃핏 칸 프리팹(UnitSkinSlotView).")]
    [SerializeField] private UnitSkinSlotView slotPrefab;

    [Header("Detail")]
    [Tooltip("선택. 고른 아웃핏을 이 소녀의 초상으로 실제 유닛 프리팹 그대로 보여 주는 미리보기(RawImage + UnitPreviewView).")]
    [SerializeField] private UnitPreviewView preview;
    [Tooltip("선택. 고른 아웃핏의 아이콘.")]
    [SerializeField] private Image iconImage;
    [Tooltip("고른 아웃핏 이름.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("선택. 장착 효과 요약.")]
    [SerializeField] private TMP_Text effectText;
    [Tooltip("선택. 남은 개수 / 보유 개수.")]
    [SerializeField] private TMP_Text countText;
    [Tooltip("선택. 낄 수 없을 때(남은 개수 없음) 켜는 안내.")]
    [SerializeField] private GameObject lockedNotice;

    [Header("Buttons")]
    [Tooltip("장착 버튼. 이미 장착 중이거나 남은 개수가 없으면 비활성이 된다.")]
    [SerializeField] private Button equipButton;
    [Tooltip("선택. 장착 버튼 라벨(장착 / 장착중).")]
    [SerializeField] private TMP_Text equipButtonLabel;
    [SerializeField] private Button closeButton;

    private const string EquipLabel = "장착";
    private const string EquippedLabel = "장착중";

    private MainHudContext _hud;
    private CancellationToken _ct;
    private int _charId;
    private bool _wired;
    private bool _busy;

    private readonly List<UnitSkinSlotView> _slots = new();
    private UnitSkinSlotView _selected;

    /// <summary>아웃핏이 바뀌어 능력치가 달라졌을 때 발생(상위 화면이 상세 표시를 갱신한다).</summary>
    public event Action Changed;

    private UnitSkinCatalog Catalog => _hud != null && _hud.CostumeDb != null ? _hud.CostumeDb.UnitSkins : null;

    public void Initialize(MainHudContext hud, int charDataId) {
        _hud = hud;
        _charId = charDataId;
        _ct = this.GetCancellationTokenOnDestroy();

        if (!_wired) {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            if (equipButton != null)
                equipButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); EquipSelectedAsync().Forget(); });
            _wired = true;
        }

        // 소녀를 새로 열거나 바꿨을 때는 그 소녀가 지금 낀 아웃핏을 고른 상태로 시작한다.
        Build(keepSelection: false);
    }

    // 기본 칸 + 아웃핏 전체. 남은 개수가 장착할 때마다 바뀌므로 장착 후에도 다시 만든다(5칸이라 비용이 없다).
    // keepSelection: 장착 직후처럼 같은 소녀를 다시 그릴 때만 고르던 칸을 유지한다.
    private void Build(bool keepSelection) {
        int keep = keepSelection && _selected != null ? _selected.UnitSkinId : -1;
        ClearSlots();
        if (content == null || slotPrefab == null) return;

        int equippedId = _hud.Store.EquippedUnitSkinId(_charId);
        AddSlot(null, equippedId);

        var catalog = Catalog;
        if (catalog != null)
            foreach (var entry in catalog.All)
                if (entry != null) AddSlot(entry, equippedId);

        // 같은 소녀를 다시 그릴 때는 고르던 칸을 유지하고, 새로 열거나 소녀를 바꾸면 지금 장착 중인 것을 골라 둔다.
        Select(FindSlot(keep >= 0 ? keep : equippedId) ?? (_slots.Count > 0 ? _slots[0] : null));
    }

    private void AddSlot(UnitSkinCatalog.Entry entry, int equippedId) {
        int id = entry != null ? entry.id : 0;
        int owned = entry != null ? _hud.Store.UnitSkinCount(id) : 0;
        int left = entry != null ? Mathf.Max(0, owned - _hud.Store.UnitSkinInUse(id)) : 0;
        bool available = _hud.Store.CanEquipUnitSkin(_charId, id);

        var view = Instantiate(slotPrefab, content);
        view.Bind(entry, Catalog != null ? Catalog.IconFor(id) : null, left, owned, available, equippedId == id, OnSlotClicked);
        _slots.Add(view);
    }

    private void OnSlotClicked(UnitSkinSlotView slot) {
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        Select(slot);
    }

    private void Select(UnitSkinSlotView slot) {
        _selected = slot;
        foreach (var s in _slots) if (s != null) s.SetSelected(s == slot);
        RefreshDetail();
    }

    private void RefreshDetail() {
        var entry = _selected != null ? _selected.Entry : null;
        int id = entry != null ? entry.id : 0;
        bool available = _selected == null || _selected.Available;
        bool equipped = _selected != null && id == _hud.Store.EquippedUnitSkinId(_charId);

        RefreshPreview(entry);
        if (iconImage != null) {
            var sprite = Catalog != null ? Catalog.IconFor(entry != null ? entry.id : 0) : null;
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }
        if (nameText != null) nameText.text = entry != null ? entry.displayName : UnitSkinSlotView.DefaultLabel;
        if (effectText != null) {
            string summary = entry != null ? entry.EffectSummary() : "";
            effectText.text = string.IsNullOrEmpty(summary) ? CostumeSlotView.NoEffectText : summary;
        }
        if (countText != null) {
            countText.gameObject.SetActive(entry != null);
            if (entry != null) {
                int owned = _hud.Store.UnitSkinCount(id);
                int left = Mathf.Max(0, owned - _hud.Store.UnitSkinInUse(id));
                countText.text = $"{left} / {owned}";
            }
        }
        if (lockedNotice != null) lockedNotice.SetActive(!available);

        if (equipButton != null) equipButton.interactable = !_busy && available && !equipped;
        if (equipButtonLabel != null) equipButtonLabel.text = equipped ? EquippedLabel : EquipLabel;
    }

    // 고른 아웃핏을 이 소녀가 낀 모습으로 그린다. 초상은 지금 장착 중인 코스튬을 그대로 반영한다.
    private void RefreshPreview(UnitSkinCatalog.Entry entry) {
        if (preview == null) return;
        var catalog = Catalog;
        var character = _hud.CharDb != null ? _hud.CharDb.Get(_charId) : null;
        if (catalog == null || character == null) { preview.Clear(); return; }

        var mine = _hud.Appearance != null ? _hud.Appearance.Mine(character) : CharacterAppearance.Default(character);
        var look = new CharacterAppearance(character, mine.Costume, entry);
        preview.Show(catalog.PreviewPrefabFor(entry != null ? entry.id : 0), look);
    }

    private async UniTaskVoid EquipSelectedAsync() {
        if (_busy || _selected == null) return;
        int id = _selected.UnitSkinId;
        if (id == _hud.Store.EquippedUnitSkinId(_charId)) return;
        if (!_selected.Available) { _hud.Ui.ShowToast(UnitSkinRules.FailMessage(3)); return; }

        _busy = true;
        RefreshDetail();
        try {
            var res = await _hud.Costume.EquipUnitSkinAsync(_charId, id, _ct);
            if (!res.IsSuccess) {
                _hud.Ui.ShowToast(UnitSkinRules.FailMessage(res.FailReason));
                return;
            }
            _hud.Store.ApplyUnitSkinEquip(_charId, res.UnitSkinId);
            Changed?.Invoke();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast(UnitSkinRules.FailMessage(-1));
        } finally {
            _busy = false;
            Build(keepSelection: true);   // 남은 개수가 바뀌었으므로 칸을 다시 그린다(고르던 칸은 유지).
        }
    }

    private UnitSkinSlotView FindSlot(int unitSkinId) {
        foreach (var s in _slots) if (s != null && s.UnitSkinId == unitSkinId) return s;
        return null;
    }

    private void ClearSlots() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
        _selected = null;
    }

    protected override void OnClosed() => ClearSlots();
}
