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
/// 영웅전 입구(슬라이드 메뉴의 영웅전 버튼). 오늘 남은 도전 횟수, 도전할 단계와 그 단계의 클리어 보상을 보여 주고,
/// 나설 소녀 1명을 골라 도전을 시작한다.
///
/// 소녀 칸은 1칸이며 처음에는 대표 소녀가 들어 있다. 보유 소녀 목록에서 끌어다 칸에 놓으면 그 소녀로 바뀐다.
/// 고른 소녀는 계정별 로컬에 남아(HeroesSession) 팝업을 닫았다 열어도, 전투를 다녀와도 그대로다.
/// 칸과 카드는 편성 화면과 같은 부품(CompositionSlotView + DeckCharacterCard)이다.
/// 칸은 비울 수 없다(언제나 1명이 나선다). 칸에서 끌어내는 조작은 받지 않는다.
///
/// 50단계까지 모두 깨면 보상 대신 다음 업데이트를 기다려 달라는 안내가 나오고 도전 버튼이 잠긴다.
/// </summary>
public sealed class HeroesPopup : UIView, IHudPopup, CompositionDragHandle.IHost {
    [Header("Info")]
    [Tooltip("오늘 남은 도전 횟수(n / 3). 라벨은 프리팹에서 따로 둔다.")]
    [SerializeField] private TMP_Text entryCountText;
    [Tooltip("도전할 단계(예: 3단계).")]
    [SerializeField] private TMP_Text stageText;

    [Header("Clear Reward")]
    [Tooltip("보상 제목(예: 3단계 클리어 보상).")]
    [SerializeField] private TMP_Text rewardTitleText;
    [Tooltip("이 단계를 깼을 때 받는 보상 칸이 생성될 부모.")]
    [SerializeField] private Transform rewardContent;
    [SerializeField] private RewardSlotView rewardSlotPrefab;
    [Tooltip("선택. 보상 영역 묶음(제목 + 칸). 끝까지 깼으면 꺼진다.")]
    [SerializeField] private GameObject rewardGroup;
    [Tooltip("끝까지 깼을 때만 켜지는 안내 문구(최대 단계 달성, 다음 업데이트 대기). 내용은 코드가 쓴다.")]
    [SerializeField] private TMP_Text maxStageText;

    [Header("Hero Slot")]
    [Tooltip("나설 소녀 칸(1칸).")]
    [SerializeField] private CompositionSlotView slot;
    [Tooltip("칸에 붙일 덱 카드 프리팹. 목록 카드와 같은 프리팹을 넣어야 집은 카드가 칸에서도 같은 모습으로 보인다.")]
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
    [SerializeField] private Button challengeButton;
    [Tooltip("도전 버튼의 알림 배지(BadgeView, Source=Manual). 오늘 도전 횟수가 남아 있으면 켜진다.")]
    [SerializeField] private BadgeView challengeBadge;
    [SerializeField] private Button closeButton;

    [Header("Currency")]
    [Tooltip("선택. 상단 재화 패널.")]
    [SerializeField] private CurrencyPanel currencyPanel;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private HeroesInfoDto _info;
    private bool _busy;
    private bool _wired;
    private int _charId;

    private readonly List<DeckCharacterCard> _rosterCards = new();
    private DeckCharacterCard _slotCard;
    private readonly List<RewardSlotView> _rewardSlots = new();

    // 드래그 상태(편성 화면과 같은 구조).
    private CompositionDragHandle _dragging;
    private GameObject _ghost;
    private RectTransform _ghostRt;
    private bool _hoveringSlot;
    private readonly List<RaycastResult> _raycastBuffer = new();

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (currencyPanel != null) currencyPanel.Initialize(ctx);

        if (!_wired) {
            if (challengeButton != null)
                challengeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); Challenge(); });
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            _wired = true;
        }

        if (slot != null) slot.Initialize(0);
        _charId = ResolveInitialChar();
        BuildRoster();
        RefreshAll();
        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        _busy = true;
        RefreshButton();
        try {
            _info = await _ctx.Heroes.RequestInfoAsync(_ct);
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("영웅전 정보를 불러오지 못했습니다.");
        } finally {
            _busy = false;
        }
        RefreshAll();
    }

    // 기억한 소녀가 아직 내 소녀면 그 소녀, 아니면 대표, 대표도 없으면 보유 소녀 중 첫 번째.
    private int ResolveInitialChar() {
        int remembered = HeroesSession.RememberedCharId;
        if (IsOwned(remembered)) return remembered;

        int rep = _ctx.Store.Deck.RepresentativeCharId;
        if (IsOwned(rep)) return rep;

        foreach (var data in _ctx.CharDb.All)
            if (data != null && IsOwned(data.Id)) return data.Id;
        return 0;
    }

    private bool IsOwned(int charId) =>
        charId != 0 && _ctx.CharDb.Get(charId) != null && _ctx.Store.GetProgress(charId) != null;

    // ── 표시 갱신 ────────────────────────────────────
    private void RefreshAll() {
        RefreshHeader();
        RefreshRewards();
        RefreshSlot();
        RefreshRosterState();
        RefreshButton();
    }

    private void RefreshHeader() {
        bool max = _info != null && _info.IsMaxReached;

        if (entryCountText != null) entryCountText.text = _info != null ? HeroesRules.EntryCountText(_info.RemainEntries) : "";
        if (stageText != null)
            stageText.text = _info == null ? "" : HeroesRules.StageText(max ? _info.MaxStage : _info.Stage);
        if (rewardTitleText != null) rewardTitleText.text = _info != null && !max ? HeroesRules.RewardTitleText(_info.Stage) : "";
        if (rewardGroup != null) rewardGroup.SetActive(!max);
        if (maxStageText != null) {
            maxStageText.gameObject.SetActive(max);
            maxStageText.text = HeroesRules.MaxStageMessage;
        }
    }

    private void RefreshRewards() {
        foreach (var s in _rewardSlots) if (s != null) Destroy(s.gameObject);
        _rewardSlots.Clear();
        if (_info == null || _info.IsMaxReached || rewardContent == null || rewardSlotPrefab == null) return;

        foreach (var item in HeroesRules.RewardsFor(_info.Stage)) {
            var view = Instantiate(rewardSlotPrefab, rewardContent);
            view.gameObject.SetActive(true);
            view.Bind(RewardVisualResolver.Resolve(_ctx, item));
            _rewardSlots.Add(view);
        }
    }

    // 칸에 덱 카드를 붙여 채운다(편성 화면과 같은 방식). 카드는 하나를 재사용한다.
    private void RefreshSlot() {
        if (slot == null) return;

        var data = _charId != 0 ? _ctx.CharDb.Get(_charId) : null;
        var progress = _charId != 0 ? _ctx.Store.GetProgress(_charId) : null;
        if (data == null || progress == null) {
            if (_slotCard != null) { Destroy(_slotCard.gameObject); _slotCard = null; }
            slot.SetFilled(false);
            return;
        }

        if (_slotCard == null && slotCardPrefab != null) {
            _slotCard = Instantiate(slotCardPrefab);
            slot.Attach((RectTransform)_slotCard.transform);
        }
        if (_slotCard == null) return;

        _slotCard.Bind(_ctx.Appearance.Mine(data), progress, null);
        // 칸에 있다는 것 자체가 골랐다는 뜻이라 편성/대표/육성 배지는 쓰지 않는다.
        _slotCard.SetState(inDeck: false, isRepresentative: false, isSelected: false);
        _slotCard.SetUpgradeBadge(false);
        _slotCard.SetGuideTarget(false);
        _slotCard.SetTowerBonus(false);
        _slotCard.SetDragSource(this, 0);
        slot.SetFilled(true);
    }

    // 보유 소녀 목록은 열 때 한 번만 만든다(고를 때마다 다시 만들면 스크롤이 튀고 집던 카드가 파괴된다).
    private void BuildRoster() {
        foreach (var c in _rosterCards) if (c != null) Destroy(c.gameObject);
        _rosterCards.Clear();
        if (rosterContent == null || rosterCardPrefab == null || _ctx.CharDb == null) return;

        var owned = new List<CharacterData>();
        foreach (var data in _ctx.CharDb.All)
            if (data != null && _ctx.Store.GetProgress(data.Id) != null) owned.Add(data);
        owned.Sort(Compare);

        foreach (var data in owned) {
            var card = Instantiate(rosterCardPrefab, rosterContent);
            card.Bind(_ctx.Appearance.Mine(data), _ctx.Store.GetProgress(data.Id), null);
            card.SetUpgradeBadge(false);
            card.SetGuideTarget(false);
            card.SetTowerBonus(false);
            _rosterCards.Add(card);
        }
    }

    // 덱/무한 타워 화면과 같은 정렬 기준(등급 높은 순 -> 레벨 높은 순 -> id).
    private int Compare(CharacterData a, CharacterData b) {
        int gradeCmp = b.Grade.CompareTo(a.Grade);
        if (gradeCmp != 0) return gradeCmp;

        int la = _ctx.Store.GetProgress(a.Id)?.Level ?? 0;
        int lb = _ctx.Store.GetProgress(b.Id)?.Level ?? 0;
        int levelCmp = lb.CompareTo(la);
        if (levelCmp != 0) return levelCmp;

        return a.Id.CompareTo(b.Id);
    }

    // 지금 칸에 든 소녀를 목록에서 "편성됨"으로 표시한다.
    private void RefreshRosterState() {
        foreach (var card in _rosterCards) {
            if (card == null || card.Data == null) continue;
            card.SetState(inDeck: card.Data.Id == _charId, isRepresentative: false, isSelected: false);
            card.SetDragSource(this, CompositionDragHandle.RosterSlot);
        }
    }

    private void RefreshButton() {
        bool canEnter = _info != null && _info.CanEnter;
        if (challengeButton != null) challengeButton.interactable = canEnter && !_busy && _charId != 0;
        // 통신 중(_busy)에 잠깐 버튼이 잠기는 것으로는 끄지 않는다(배지가 깜빡이지 않게). 도전할 수 있는지만 본다.
        if (challengeBadge != null) challengeBadge.SetOn(canEnter);
    }

    // ── 도전 ─────────────────────────────────────────
    private void Challenge() {
        if (_busy || _info == null) return;
        if (_info.IsMaxReached) { _ctx.Ui.ShowToast("최대 단계를 달성했습니다."); return; }
        if (_info.RemainEntries <= 0) { _ctx.Ui.ShowToast("오늘 도전 횟수를 모두 사용했습니다."); return; }
        if (_charId == 0) { _ctx.Ui.ShowToast("도전할 소녀를 골라주세요."); return; }
        if (_ctx.Scenes == null || _ctx.Scenes.HeroesScene == null || !_ctx.Scenes.HeroesScene.RuntimeKeyIsValid()) {
            Debug.LogError("[HeroesPopup] SceneCatalog 에 영웅전 씬이 할당되지 않았습니다.");
            _ctx.Ui.ShowToast("영웅전 씬을 열 수 없습니다.");
            return;
        }

        HeroesSession.SetEntry(_info.Stage, _charId);

        // CloseAllPopups 는 이 팝업 자신도 파괴하므로, 그 뒤에 쓸 것들을 먼저 지역 변수로 확보한다.
        var ui = _ctx.Ui;
        var sceneService = _ctx.SceneService;
        var sceneKey = _ctx.Scenes.HeroesScene.RuntimeKey;

        // 돌아왔을 때 이 팝업이 다시 열리도록 예약한 뒤 닫는다.
        MainPopupReturn.Request(_ctx.Popups.HeroesPopup);
        ui.CloseAllPopups();
        sceneService.LoadAddressableAsync(sceneKey).Forget();
    }

    // ── 드래그(CompositionDragHandle.IHost) ──────────
    // 목록 카드만 집을 수 있다. 칸은 비울 수 없으므로 칸의 카드는 집히지 않는다.
    public bool CanDrag(CompositionDragHandle handle) =>
        !_busy && _dragging == null && handle != null && handle.CharDataId != 0 && !handle.IsFromSlot;

    public void OnDragBegan(CompositionDragHandle handle, PointerEventData eventData) {
        _dragging = handle;
        _ctx.Sound.PlaySfx(SfxKey.CardPick);
        CreateGhost(handle, eventData);
        handle.SetDimmed(true);
        UpdateHover(eventData);
    }

    public void OnDragMoved(CompositionDragHandle handle, PointerEventData eventData) {
        MoveGhost(eventData);
        UpdateHover(eventData);
    }

    public void OnDragEnded(CompositionDragHandle handle, PointerEventData eventData) {
        bool dropped = _hoveringSlot;

        if (handle != null) handle.SetDimmed(false);
        SetHover(false);
        DestroyGhost();
        _dragging = null;

        // eventData 가 없으면 카드가 파괴되어 드래그가 끊긴 경우다(바꾸지 않고 조용히 정리).
        if (eventData == null || handle == null || !dropped) return;
        if (handle.CharDataId == _charId) return;

        _charId = handle.CharDataId;
        // 고를 때마다 바로 기록한다. 도전하지 않고 닫아도 다음에 열면 같은 소녀가 보인다.
        HeroesSession.RememberChar(_charId);
        _ctx.Sound.PlaySfx(SfxKey.CardPlace);
        RefreshAll();
    }

    // ── 드래그 복제본(고스트) ────────────────────────
    private void CreateGhost(CompositionDragHandle handle, PointerEventData eventData) {
        DestroyGhost();

        var layer = dragLayer != null ? dragLayer : (RectTransform)transform;
        var source = handle.GhostSource;

        _ghost = Instantiate(source.gameObject, layer);
        _ghost.name = "DragGhost";
        _ghostRt = (RectTransform)_ghost.transform;

        // 복제본은 표시 전용이다. 입력을 받으면 아래의 칸이 드롭 대상으로 잡히지 않는다.
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
    private void UpdateHover(PointerEventData eventData) => SetHover(IsOverSlot(eventData));

    private bool IsOverSlot(PointerEventData eventData) {
        if (eventData == null || EventSystem.current == null || slot == null) return false;

        _raycastBuffer.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastBuffer);
        for (int i = 0; i < _raycastBuffer.Count; i++) {
            // 이 팝업의 칸만 드롭 대상이다(뒤에 깔린 다른 화면의 칸을 잡지 않게).
            if (_raycastBuffer[i].gameObject.GetComponentInParent<CompositionSlotView>() == slot) return true;
        }
        return false;
    }

    private void SetHover(bool on) {
        if (_hoveringSlot == on) return;
        _hoveringSlot = on;
        if (slot != null) slot.SetDropHighlight(on);
    }

    // ── 정리 ─────────────────────────────────────────
    protected override void OnClosed() {
        SetHover(false);
        DestroyGhost();
        _dragging = null;

        foreach (var c in _rosterCards) if (c != null) Destroy(c.gameObject);
        _rosterCards.Clear();
        if (_slotCard != null) Destroy(_slotCard.gameObject);
        _slotCard = null;
        foreach (var s in _rewardSlots) if (s != null) Destroy(s.gameObject);
        _rewardSlots.Clear();
    }
}
