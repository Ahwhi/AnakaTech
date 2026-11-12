using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 무한 타워 입구(슬라이드 메뉴의 무한 타워 버튼). 지금 오를 층과 그 층의 보너스, 깼을 때의 보상을 보여 주고,
/// 합류 순번 편성을 정해 등반을 시작한다.
///
/// 편성은 드롭다운으로 출처를 고른다: 메인 스쿼드(지금 덱) / 저장한 프리셋 / 커스텀.
/// 고른 출처와 커스텀 편성은 계정별 로컬에 남아(TowerSession) 팝업을 닫았다 열어도 그대로다.
/// 메인 스쿼드나 프리셋으로 바꾸면 커스텀 편성은 버린다.
/// 슬롯은 편성 화면과 같은 부품(CompositionSlotView + DeckCharacterCard)이고, 목록에서 끌어다 놓아 직접 바꿀 수 있다.
/// 메인 스쿼드나 프리셋을 고른 상태에서 직접 바꾸면 드롭다운이 저절로 커스텀으로 넘어간다(그 편성이 곧 커스텀 편성이 된다).
///
/// 슬롯 순서가 곧 합류 순번이다(왼쪽부터 1, 2, 3, 4, 5번이 차례로 합류한다).
/// 이 화면의 편성은 덱을 바꾸지 않는다. 무한 타워 안에서만 쓰는 편성이다.
///
/// 조작 규칙(편성 화면과 같은 규약):
///   목록 카드 -> 빈 자리  : 편성
///   목록 카드 -> 찬 자리  : 그 자리의 소녀와 교체
///   자리     -> 다른 자리 : 자리 맞바꿈(순번 변경)
///   자리     -> 자리 밖   : 편성 해제(마지막 1명은 뺄 수 없다)
/// </summary>
public sealed class TowerPopup : UIView, IHudPopup, CompositionDragHandle.IHost {
    [Header("Floor")]
    [Tooltip("지금 오를 층(예: 12층).")]
    [SerializeField] private TMP_Text floorText;
    [Tooltip("오늘 오를 수 있는 층 수(n/1 층). 라벨은 프리팹에서 따로 둔다.")]
    [SerializeField] private TMP_Text climbCountText;
    [Tooltip("이 층의 보너스. 라벨 없이 내용만 리치 텍스트로 쓰므로 TMP 의 Rich Text 옵션이 켜져 있어야 한다.")]
    [SerializeField] private TMP_Text bonusText;

    [Header("Clear Reward")]
    [Tooltip("이 층을 깼을 때 받는 보상 칸이 생성될 부모.")]
    [SerializeField] private Transform rewardContent;
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    [Header("Deck")]
    [Tooltip("편성 출처 드롭다운(메인 스쿼드 / 프리셋들 / 커스텀). 항목은 코드가 채운다.")]
    [SerializeField] private TMP_Dropdown deckDropdown;
    [Tooltip("합류 순번 슬롯 5칸. 배열 순서가 곧 순번이다(0번 = 가장 먼저 나서는 소녀).")]
    [SerializeField] private CompositionSlotView[] slots;
    [Tooltip("슬롯에 붙일 덱 카드 프리팹. 목록 카드와 같은 프리팹을 넣어야 집은 카드가 슬롯에서도 같은 모습으로 보인다.")]
    [SerializeField] private DeckCharacterCard slotCardPrefab;

    [Header("Roster (보유 소녀 목록)")]
    [SerializeField] private Transform rosterContent;
    [SerializeField] private DeckCharacterCard rosterCardPrefab;

    [Header("Drag")]
    [Tooltip("드래그 중인 카드 복제본이 올라갈 레이어. 하이어라키 마지막의 Raycast Target 이 꺼진 빈 RectTransform. 비우면 팝업 루트.")]
    [SerializeField] private RectTransform dragLayer;
    [Range(0.2f, 1f)]
    [SerializeField] private float dragGhostAlpha = 0.8f;
    [SerializeField] private float dragGhostScale = 1.05f;

    [Header("Buttons")]
    [SerializeField] private Button climbButton;
    [Tooltip("등반 버튼 문구(등반 / 오늘 완료).")]
    [SerializeField] private TMP_Text climbButtonText;
    [Tooltip("등반 버튼의 알림 배지(BadgeView, Source=Manual). 오늘 오를 수 있으면 켜지고 등반을 마치면 꺼진다.")]
    [SerializeField] private BadgeView climbBadge;
    [SerializeField] private Button closeButton;

    [Header("Currency")]
    [Tooltip("선택. 상단 재화 패널.")]
    [SerializeField] private CurrencyPanel currencyPanel;

    // 드롭다운 항목 하나. 프리셋 항목만 PresetSlot 을 쓴다.
    private readonly struct DeckOption {
        public readonly TowerDeckSource Source;
        public readonly int PresetSlot;
        public DeckOption(TowerDeckSource source, int presetSlot) { Source = source; PresetSlot = presetSlot; }
    }

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private TowerInfoDto _info;
    private bool _busy;
    private bool _wired;

    private readonly DeckEditSession _session = new();
    private readonly List<int> _customMembers = new();
    private readonly List<DeckPresetDto> _presets = new();
    private readonly List<DeckOption> _options = new();
    private int _selected;

    private readonly List<DeckCharacterCard> _rosterCards = new();
    private DeckCharacterCard[] _slotCards;
    private readonly List<RewardSlotView> _rewardSlots = new();

    // 드래그 상태(편성 화면과 같은 구조).
    private CompositionDragHandle _dragging;
    private GameObject _ghost;
    private RectTransform _ghostRt;
    private CompositionSlotView _hoveredSlot;
    private readonly List<RaycastResult> _raycastBuffer = new();

    private TowerBonus Bonus => _info != null ? _info.Bonus : default;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (currencyPanel != null) currencyPanel.Initialize(ctx);

        if (!_wired) {
            if (climbButton != null)
                climbButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); Climb(); });
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            if (deckDropdown != null) deckDropdown.onValueChanged.AddListener(OnDeckSelected);
            _wired = true;
        }

        InitializeSlots();
        RestoreRememberedDeck();
        BuildRoster();
        RefreshAll();
        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        _busy = true;
        RefreshButton();
        try {
            _info = await _ctx.Tower.RequestInfoAsync(_ct);

            // 프리셋을 못 받아도 메인 스쿼드/커스텀으로는 오를 수 있어야 하므로 따로 감싼다.
            try {
                var presets = await _ctx.MainData.GetDeckPresetsAsync(_ct);
                _presets.Clear();
                if (presets != null) _presets.AddRange(presets);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception e) {
                Debug.LogWarning($"[TowerPopup] 편성 프리셋을 받지 못했습니다: {e.Message}");
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("무한 타워 정보를 불러오지 못했습니다.");
        } finally {
            _busy = false;
        }

        BuildDropdown();
        RefreshAll();
    }

    private void InitializeSlots() {
        if (slots == null) return;
        _slotCards ??= new DeckCharacterCard[slots.Length];
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null) slots[i].Initialize(i);
    }

    // ── 편성 출처 ─────────────────────────────────────
    // 전투를 다녀와 다시 열렸을 때 방금 쓴 편성이 그대로 보이게 한다. 기록이 없으면 지금 덱으로 시작한다.
    // 프리셋 내용은 아직 받기 전이라, 프리셋이었다면 받은 뒤(BuildDropdown)에 다시 적용한다.
    private void RestoreRememberedDeck() {
        _customMembers.Clear();
        _customMembers.AddRange(TowerSession.CustomMembers);

        if (TowerSession.DeckSource == TowerDeckSource.Custom && _customMembers.Count > 0) LoadMembers(_customMembers, 0);
        else _session.Reset(_ctx.Store.Deck);
    }

    private void BuildDropdown() {
        _options.Clear();
        var labels = new List<string>();

        _options.Add(new DeckOption(TowerDeckSource.Current, -1));
        labels.Add("메인스쿼드");
        foreach (var p in _presets) {
            if (p == null || p.IsEmpty) continue;   // 저장하지 않은 빈 칸은 고를 것이 없다
            _options.Add(new DeckOption(TowerDeckSource.Preset, p.Slot));
            labels.Add(p.DisplayName);
        }
        _options.Add(new DeckOption(TowerDeckSource.Custom, -1));
        labels.Add("커스텀");

        // 기억한 선택을 찾는다. 그 프리셋이 비워졌거나 커스텀 편성이 없으면 메인 스쿼드로 돌아간다.
        int index = 0;
        for (int i = 0; i < _options.Count; i++) {
            var o = _options[i];
            if (o.Source != TowerSession.DeckSource) continue;
            if (o.Source == TowerDeckSource.Preset && o.PresetSlot != TowerSession.PresetSlot) continue;
            if (o.Source == TowerDeckSource.Custom && _customMembers.Count == 0) continue;
            index = i;
            break;
        }

        if (deckDropdown != null) {
            deckDropdown.ClearOptions();
            deckDropdown.AddOptions(labels);
            deckDropdown.SetValueWithoutNotify(index);
            deckDropdown.RefreshShownValue();
        }
        ApplyOption(index);
    }

    private void OnDeckSelected(int index) {
        if (index < 0 || index >= _options.Count) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        ApplyOption(index);
        RefreshAll();
    }

    private void ApplyOption(int index) {
        if (index < 0 || index >= _options.Count) return;
        _selected = index;

        var option = _options[index];
        switch (option.Source) {
            case TowerDeckSource.Current:
                _session.Reset(_ctx.Store.Deck);
                _customMembers.Clear();   // 다른 출처를 고르면 커스텀 편성은 버린다
                break;
            case TowerDeckSource.Preset:
                var preset = FindPreset(option.PresetSlot);
                if (preset != null) LoadMembers(preset.CharIds, preset.RepresentativeCharId);
                else _session.Reset(_ctx.Store.Deck);
                _customMembers.Clear();
                break;
            case TowerDeckSource.Custom:
                // 커스텀 편성이 아직 없으면 지금 보이는 편성을 그대로 커스텀의 시작점으로 삼는다.
                if (_customMembers.Count > 0) LoadMembers(_customMembers, 0);
                else CopyMembers(_session.Members, _customMembers);
                break;
        }
        Remember(option);
    }

    // 고를 때마다 바로 기록한다. 등반하지 않고 닫아도 다음에 열면 같은 편성이 보인다.
    private void Remember(DeckOption option) =>
        TowerSession.RememberDeck(option.Source, option.PresetSlot, _customMembers);

    private DeckPresetDto FindPreset(int slot) {
        foreach (var p in _presets) if (p != null && p.Slot == slot) return p;
        return null;
    }

    // 목록을 편성 작업본에 옮긴다. 보유하지 않은 소녀는 빠진다. 대표(첫 칸)를 따로 주지 않으면 목록의 첫 소녀가 첫 칸이다.
    private void LoadMembers(IReadOnlyList<int> ids, int representativeId) {
        var owned = new List<int>(ids.Count);
        foreach (int id in ids)
            if (id != 0 && _ctx.Store.GetProgress(id) != null) owned.Add(id);

        var deck = new DeckState();
        deck.Set(owned, representativeId != 0 ? representativeId : (owned.Count > 0 ? owned[0] : 0));
        _session.Reset(deck);
    }

    // 직접 바꾼 편성은 곧 커스텀 편성이다. 드롭다운도 커스텀으로 넘긴다(다시 고르지 않아도 된다).
    private void OnEdited() {
        CopyMembers(_session.Members, _customMembers);
        // 드롭다운이 아직 채워지기 전(정보 수신 중)에 바꿔도 커스텀으로 남겨야 하므로 옵션 목록과 무관하게 기록한다.
        Remember(new DeckOption(TowerDeckSource.Custom, -1));

        int custom = _options.FindIndex(o => o.Source == TowerDeckSource.Custom);
        if (custom >= 0 && _selected != custom) {
            _selected = custom;
            if (deckDropdown != null) {
                deckDropdown.SetValueWithoutNotify(custom);
                deckDropdown.RefreshShownValue();
            }
        }
        _ctx.Sound.PlaySfx(SfxKey.CardPlace);
        RefreshAll();
    }

    private static void CopyMembers(IReadOnlyList<int> from, List<int> to) {
        to.Clear();
        for (int i = 0; i < from.Count; i++) to.Add(from[i]);
    }

    // ── 표시 갱신 ────────────────────────────────────
    private void RefreshAll() {
        RefreshHeader();
        RefreshRewards();
        RefreshSlots();
        RefreshRosterState();
        RefreshButton();
    }

    private void RefreshHeader() {
        if (_info == null) {
            if (floorText != null) floorText.text = "";
            if (climbCountText != null) climbCountText.text = "";
            if (bonusText != null) bonusText.text = "";
            return;
        }

        if (floorText != null)
            floorText.text = TowerRules.FloorText(_info.IsTopReached ? _info.MaxFloor : _info.Floor);
        if (climbCountText != null) climbCountText.text = TowerRules.ClimbCountText(_info.CanClimb);
        if (bonusText != null) bonusText.text = _info.Bonus.Describe();
    }

    private void RefreshRewards() {
        foreach (var s in _rewardSlots) if (s != null) Destroy(s.gameObject);
        _rewardSlots.Clear();
        if (_info == null || _info.IsTopReached || rewardContent == null || rewardSlotPrefab == null) return;

        foreach (var item in TowerRules.RewardsFor(_info.Floor)) {
            var slot = Instantiate(rewardSlotPrefab, rewardContent);
            slot.gameObject.SetActive(true);
            slot.Bind(RewardVisualResolver.Resolve(_ctx, item));
            _rewardSlots.Add(slot);
        }
    }

    // 슬롯마다 덱 카드를 붙여 채운다(편성 화면과 같은 방식). 카드는 슬롯당 하나를 재사용하고, 빠지면 파괴한다.
    private void RefreshSlots() {
        if (slots == null || _slotCards == null) return;

        for (int i = 0; i < slots.Length; i++) {
            var slot = slots[i];
            if (slot == null) continue;

            int charId = _session.At(i);
            var data = charId != 0 ? _ctx.CharDb.Get(charId) : null;
            var progress = charId != 0 ? _ctx.Store.GetProgress(charId) : null;

            if (data == null || progress == null) {
                if (_slotCards[i] != null) { Destroy(_slotCards[i].gameObject); _slotCards[i] = null; }
                slot.SetFilled(false);
                continue;
            }

            var card = _slotCards[i];
            if (card == null && slotCardPrefab != null) {
                card = Instantiate(slotCardPrefab);
                slot.Attach((RectTransform)card.transform);
                _slotCards[i] = card;
            }
            if (card == null) continue;

            card.Bind(_ctx.Appearance.Mine(data), progress, null);
            // 슬롯에 있다는 것 자체가 편성됐다는 뜻이라 편성/대표/육성 배지는 쓰지 않는다.
            card.SetState(inDeck: false, isRepresentative: false, isSelected: false);
            card.SetUpgradeBadge(false);
            card.SetGuideTarget(false);
            card.SetTowerBonus(Bonus.AppliesTo(data, progress.LimitBroken));
            card.SetDragSource(this, i);
            slot.SetFilled(true);
        }
    }

    // 보유 소녀 목록은 열 때 한 번만 만든다(편성을 만질 때마다 다시 만들면 스크롤이 튀고 집던 카드가 파괴된다).
    private void BuildRoster() {
        foreach (var c in _rosterCards) if (c != null) Destroy(c.gameObject);
        _rosterCards.Clear();
        if (rosterContent == null || rosterCardPrefab == null || _ctx.CharDb == null) return;

        var owned = new List<CharacterData>();
        foreach (var data in _ctx.CharDb.All) {
            if (data == null || _ctx.Store.GetProgress(data.Id) == null) continue;
            owned.Add(data);
        }
        owned.Sort(Compare);

        foreach (var data in owned) {
            var card = Instantiate(rosterCardPrefab, rosterContent);
            card.Bind(_ctx.Appearance.Mine(data), _ctx.Store.GetProgress(data.Id), null);
            card.SetUpgradeBadge(false);
            card.SetGuideTarget(false);
            _rosterCards.Add(card);
        }
    }

    // 덱/파견 화면과 같은 정렬 기준(등급 높은 순 -> 레벨 높은 순 -> id). 편성 여부는 정렬에 넣지 않는다.
    private int Compare(CharacterData a, CharacterData b) {
        int gradeCmp = b.Grade.CompareTo(a.Grade);
        if (gradeCmp != 0) return gradeCmp;

        int la = _ctx.Store.GetProgress(a.Id)?.Level ?? 0;
        int lb = _ctx.Store.GetProgress(b.Id)?.Level ?? 0;
        int levelCmp = lb.CompareTo(la);
        if (levelCmp != 0) return levelCmp;

        return a.Id.CompareTo(b.Id);
    }

    private void RefreshRosterState() {
        foreach (var card in _rosterCards) {
            if (card == null || card.Data == null) continue;
            var progress = _ctx.Store.GetProgress(card.Data.Id);

            card.SetState(inDeck: _session.Contains(card.Data.Id), isRepresentative: false, isSelected: false);
            card.SetTowerBonus(Bonus.AppliesTo(card.Data, progress != null && progress.LimitBroken));
            card.SetDragSource(this, CompositionDragHandle.RosterSlot);
        }
    }

    private void RefreshButton() {
        bool canClimb = _info != null && _info.CanClimb && !_info.IsTopReached;
        if (climbButton != null) climbButton.interactable = canClimb && !_busy && _session.Count > 0;
        // 통신 중(_busy)에 잠깐 버튼이 잠기는 것으로는 끄지 않는다(배지가 깜빡이지 않게). 오를 수 있는지만 본다.
        if (climbBadge != null) climbBadge.SetOn(canClimb && _session.Count > 0);
        if (climbButtonText != null)
            climbButtonText.text = _info != null && _info.IsTopReached ? "최고층 도달"
                                 : canClimb || _info == null ? "등반" : "오늘 완료";
    }

    // ── 등반 ──────────────────────────────────────────
    private void Climb() {
        if (_busy || _info == null) return;
        if (!_info.CanClimb || _info.IsTopReached) {
            _ctx.Ui.ShowToast(_info.IsTopReached ? "최고층에 도달했습니다." : "오늘은 이미 등반했습니다.");
            return;
        }
        if (_session.Count == 0) {
            _ctx.Ui.ShowToast("편성할 소녀를 골라주세요.");
            return;
        }
        if (_ctx.Scenes == null || _ctx.Scenes.TowerScene == null || !_ctx.Scenes.TowerScene.RuntimeKeyIsValid()) {
            Debug.LogError("[TowerPopup] SceneCatalog 에 무한 타워 씬이 할당되지 않았습니다.");
            _ctx.Ui.ShowToast("무한 타워 씬을 열 수 없습니다.");
            return;
        }

        TowerSession.SetEntry(_info.Floor, _info.Bonus, _session.Members);

        // CloseAllPopups 는 이 팝업 자신도 파괴하므로, 그 뒤에 쓸 것들을 먼저 지역 변수로 확보한다.
        var ui = _ctx.Ui;
        var sceneService = _ctx.SceneService;
        var sceneKey = _ctx.Scenes.TowerScene.RuntimeKey;

        // 돌아왔을 때 이 팝업이 다시 열리도록 예약한 뒤 닫는다.
        MainPopupReturn.Request(_ctx.Popups.TowerPopup);
        ui.CloseAllPopups();
        sceneService.LoadAddressableAsync(sceneKey).Forget();
    }

    // ── 드래그(CompositionDragHandle.IHost) ──────────
    public bool CanDrag(CompositionDragHandle handle) =>
        !_busy && _dragging == null && handle != null && handle.CharDataId != 0;

    public void OnDragBegan(CompositionDragHandle handle, PointerEventData eventData) {
        _dragging = handle;
        _ctx.Sound.PlaySfx(SfxKey.CardPick);
        CreateGhost(handle, eventData);
        handle.SetDimmed(true);
        UpdateHoveredSlot(eventData);
    }

    public void OnDragMoved(CompositionDragHandle handle, PointerEventData eventData) {
        MoveGhost(eventData);
        UpdateHoveredSlot(eventData);
    }

    public void OnDragEnded(CompositionDragHandle handle, PointerEventData eventData) {
        var target = _hoveredSlot;

        if (handle != null) handle.SetDimmed(false);
        SetHoveredSlot(null);
        DestroyGhost();
        _dragging = null;

        // eventData 가 없으면 카드가 파괴되어 드래그가 끊긴 경우다(편집하지 않고 조용히 정리).
        if (eventData == null || handle == null) return;

        bool changed;
        if (target != null) {
            changed = handle.IsFromSlot
                ? _session.MoveSlot(handle.SlotIndex, target.SlotIndex)
                : _session.Place(target.SlotIndex, handle.CharDataId);
            if (!changed && !handle.IsFromSlot && _session.IsFull && !_session.Contains(handle.CharDataId))
                _ctx.Ui.ShowToast("편성이 가득 찼습니다. 빈 자리가 없으면 소녀 위에 놓아 교체하세요.");
        } else if (handle.IsFromSlot) {
            changed = _session.RemoveAt(handle.SlotIndex);   // 자리 밖에 놓으면 편성 해제
            if (!changed) _ctx.Ui.ShowToast("최소 1명은 편성해야 합니다.");
        } else {
            return;   // 목록에서 집어 밖에 놓은 것은 취소
        }

        if (changed) OnEdited();
        else RefreshAll();   // 흐리게 남은 카드 등 표시를 원래대로 되돌린다
    }

    // ── 드래그 복제본(고스트) ────────────────────────
    private void CreateGhost(CompositionDragHandle handle, PointerEventData eventData) {
        DestroyGhost();

        var layer = dragLayer != null ? dragLayer : (RectTransform)transform;
        var source = handle.GhostSource;

        _ghost = Instantiate(source.gameObject, layer);
        _ghost.name = "DragGhost";
        _ghostRt = (RectTransform)_ghost.transform;

        // 복제본은 표시 전용이다. 입력을 받으면 아래의 자리가 드롭 대상으로 잡히지 않는다.
        foreach (var h in _ghost.GetComponentsInChildren<CompositionDragHandle>(true)) Destroy(h);
        foreach (var a in _ghost.GetComponentsInChildren<TutorialAnchor>(true)) Destroy(a);
        var group = _ghost.GetComponent<CanvasGroup>();
        if (group == null) group = _ghost.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = dragGhostAlpha;

        _ghostRt.anchorMin = _ghostRt.anchorMax = _ghostRt.pivot = new Vector2(0.5f, 0.5f);
        _ghostRt.sizeDelta = source.rect.size;
        float layerScale = layer.lossyScale.x;
        float ratio = layerScale > 0f ? source.lossyScale.x / layerScale : 1f;
        _ghostRt.localScale = Vector3.one * (ratio * dragGhostScale);
        MoveGhost(eventData);
    }

    private void MoveGhost(PointerEventData eventData) {
        if (_ghostRt == null || eventData == null) return;

        var layer = dragLayer != null ? dragLayer : (RectTransform)transform;
        var canvas = layer.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, eventData.position, camera, out var local)) return;

        // 레이어를 어떻게 잡아 두든 손끝에 정확히 붙도록 pivot 차이를 보정한다.
        _ghostRt.anchoredPosition = local + (layer.pivot - new Vector2(0.5f, 0.5f)) * layer.rect.size;
    }

    private void DestroyGhost() {
        if (_ghost != null) Destroy(_ghost);
        _ghost = null;
        _ghostRt = null;
    }

    // ── 드롭 대상 판정 ───────────────────────────────
    private void UpdateHoveredSlot(PointerEventData eventData) => SetHoveredSlot(RaycastSlot(eventData));

    private CompositionSlotView RaycastSlot(PointerEventData eventData) {
        if (eventData == null || EventSystem.current == null || slots == null) return null;

        _raycastBuffer.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastBuffer);
        for (int i = 0; i < _raycastBuffer.Count; i++) {
            var slot = _raycastBuffer[i].gameObject.GetComponentInParent<CompositionSlotView>();
            // 이 팝업의 슬롯만 드롭 대상이다(뒤에 깔린 다른 화면의 슬롯을 잡지 않게).
            if (slot != null && Array.IndexOf(slots, slot) >= 0) return slot;
        }
        return null;
    }

    private void SetHoveredSlot(CompositionSlotView slot) {
        if (_hoveredSlot == slot) return;
        if (_hoveredSlot != null) _hoveredSlot.SetDropHighlight(false);
        _hoveredSlot = slot;
        if (_hoveredSlot != null) _hoveredSlot.SetDropHighlight(true);
    }

    // ── 정리 ─────────────────────────────────────────
    protected override void OnClosed() {
        SetHoveredSlot(null);
        DestroyGhost();
        _dragging = null;

        foreach (var c in _rosterCards) if (c != null) Destroy(c.gameObject);
        _rosterCards.Clear();
        if (_slotCards != null)
            for (int i = 0; i < _slotCards.Length; i++) {
                if (_slotCards[i] != null) Destroy(_slotCards[i].gameObject);
                _slotCards[i] = null;
            }
        foreach (var s in _rewardSlots) if (s != null) Destroy(s.gameObject);
        _rewardSlots.Clear();
    }
}
