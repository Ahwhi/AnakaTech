using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 전선 배치 팝업(V1.12). 위쪽 탭(UITabButton)으로 전선을 고르고, 보유 소녀 목록에서 스쿼드 5칸으로 끌어다 놓아 배치한다.
/// 목록/슬롯/카드는 덱, 파견, 무한 타워 화면과 같은 부품(DeckCharacterCard + CompositionSlotView)이다.
///
/// 배치 규칙:
///   한 소녀는 한 전선에만 둘 수 있다. 다른 전선에 배치된 소녀는 목록에서 아예 숨긴다(파견과 같은 방식).
///   클리어 기록이 있는 전선(잠김)의 소녀는 뺄 수도, 다른 소녀로 교체할 수도 없다. 빈 칸 채우기와 자리 바꾸기만 된다.
///   잠긴 소녀를 풀려면 전선 팝업에서 그 전선을 초기화해야 한다.
///
/// 저장: 편집은 작업본(DeckEditSession)에 쌓이고, 다른 전선 탭으로 넘어가거나 팝업을 닫을 때 서버에 저장한다.
/// 탭을 넘길 때는 저장이 끝난 뒤에 넘어가므로, 방금 뺀 소녀가 다른 전선 목록에 바로 나타난다.
///
/// 조작 규칙(편성 화면과 같은 규약):
///   목록 카드 -> 빈 자리  : 배치
///   목록 카드 -> 찬 자리  : 그 자리의 소녀와 교체(잠긴 소녀 자리는 불가)
///   자리     -> 다른 자리 : 자리 맞바꿈
///   자리     -> 자리 밖   : 배치 해제(잠긴 소녀는 불가)
/// </summary>
public sealed class FrontlineDeployPopup : UIView, IHudPopup, IHudPopupTab, CompositionDragHandle.IHost {
    [Header("Tabs")]
    [Tooltip("전선 탭 6개. 배열 순서가 곧 전선 id 다. 탭 이름은 FrontlineDatabase 의 이름으로 채운다.")]
    [SerializeField] private UITabButton[] tabs;

    [Header("Frontline")]
    [Tooltip("선택. 고른 전선의 이름.")]
    [SerializeField] private TMP_Text frontlineNameText;
    [Tooltip("선택. 고른 전선의 레벨(예: Lv.12).")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("선택. 스쿼드가 고정된 전선일 때 켜는 안내(초기화로만 뺄 수 있음 등).")]
    [SerializeField] private GameObject lockedNotice;

    [Header("Squad")]
    [Tooltip("스쿼드 5칸. 배열 순서가 곧 배치 순서다.")]
    [SerializeField] private CompositionSlotView[] slots;
    [Tooltip("슬롯에 붙일 덱 카드 프리팹. 목록 카드와 같은 프리팹을 넣어야 집은 카드가 슬롯에서도 같은 모습으로 보인다.")]
    [SerializeField] private DeckCharacterCard slotCardPrefab;
    [Tooltip("선택. 배치 인원(예: 3 / 5).")]
    [SerializeField] private TMP_Text memberCountText;
    [Tooltip("이 스쿼드의 전투력 합(시너지 포함).")]
    [SerializeField] private TMP_Text totalPowerText;

    [Header("Synergy")]
    [Tooltip("활성 시너지 줄이 생성될 부모. 비우면 시너지 목록을 표시하지 않는다.")]
    [SerializeField] private Transform synergyContent;
    [SerializeField] private SynergyRowView synergyRowPrefab;
    [Tooltip("선택. 활성 시너지가 하나도 없을 때 켜는 안내.")]
    [SerializeField] private GameObject synergyEmptyGroup;

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
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private readonly CompositeDisposable _subscriptions = new();
    private bool _wired;
    private bool _busy;

    // 전선 스쿼드는 비워 둘 수 있으므로 최소 인원이 0 이다.
    private readonly DeckEditSession _session = new(0);
    private int _frontlineId;
    // 지금 탭이 서버에서 받은 배치의 잠긴 소녀들(빼거나 교체할 수 없다).
    private readonly HashSet<int> _lockedMembers = new();

    private readonly List<DeckCharacterCard> _rosterCards = new();
    private DeckCharacterCard[] _slotCards;
    private readonly List<SynergyRowView> _synergyRows = new();
    private readonly List<SynergyData> _activeSynergies = new();
    private readonly HashSet<int> _announcedSynergyIds = new();

    private CompositionDragGhost _drag;
    private CompositionDragHandle _dragging;

    private FrontlineInfoDto Info => _ctx?.Frontline.Info.Value;

    /// <summary>열 전선(탭). Initialize 전에 부르면 그 전선으로 열린다.</summary>
    public void SelectTab(int index) {
        if (!FrontlineRules.IsValidId(index)) return;
        if (_ctx == null) { _frontlineId = index; return; }
        SwitchTabAsync(index).Forget();
    }

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (!_wired) {
            _wired = true;
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            if (tabs != null)
                for (int i = 0; i < tabs.Length; i++) {
                    if (tabs[i] == null) continue;
                    int index = i;
                    tabs[i].Bind(NameOf(i), () => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); SelectTab(index); });
                }
        }

        if (slots != null) {
            _slotCards ??= new DeckCharacterCard[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) slots[i].Initialize(i);
        }
        _drag = new CompositionDragGhost(dragLayer != null ? dragLayer : (RectTransform)transform, slots,
                                         dragGhostAlpha, dragGhostScale);

        BuildRoster();
        LoadSquad();

        _subscriptions.Clear();
        _ctx.Frontline.Info.Subscribe(_ => OnInfoChanged()).AddTo(_subscriptions);
        if (Info == null) RequestInfoAsync().Forget();
    }

    private async UniTaskVoid RequestInfoAsync() {
        try {
            await _ctx.Frontline.RequestInfoAsync(_ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("전선 정보를 불러오지 못했습니다.");
        }
    }

    // 서버 현황이 바뀌었을 때(이 화면의 저장 응답 포함). 편집 중이 아니면 서버 배치로 다시 맞춘다.
    private void OnInfoChanged() {
        if (!_session.IsDirty) LoadSquad();
        else RefreshAll();
    }

    private string NameOf(int frontlineId) =>
        _ctx != null && _ctx.FrontlineDb != null ? _ctx.FrontlineDb.NameOf(frontlineId) : FrontlineRules.DefaultName(frontlineId);

    // ── 탭 ────────────────────────────────────────────
    // 지금 전선의 편집을 저장한 뒤 넘어간다. 저장이 끝나기 전에 넘어가면 방금 뺀 소녀가 다음 목록에 보이지 않는다.
    private async UniTaskVoid SwitchTabAsync(int index) {
        if (_busy || index == _frontlineId) return;
        if (!await CommitAsync()) return;
        _frontlineId = index;
        LoadSquad();
    }

    // 서버 배치를 작업본으로 옮긴다. 보유하지 않은 소녀는 빠진다.
    private void LoadSquad() {
        var squad = Info?.Squad(_frontlineId);
        var owned = new List<int>();
        _lockedMembers.Clear();
        if (squad != null)
            foreach (int id in squad.Members) {
                if (_ctx.Store.GetProgress(id) == null) continue;
                owned.Add(id);
                if (squad.Locked) _lockedMembers.Add(id);
            }
        _session.Reset(owned);

        // 열 때 이미 켜져 있던 시너지는 "새로 켜진 것"이 아니므로 알림 대상에서 뺀다.
        RecomputeSynergies();
        _announcedSynergyIds.Clear();
        foreach (var s in _activeSynergies) if (s != null) _announcedSynergyIds.Add(s.Id);

        RefreshAll();
    }

    /// <summary>편집한 배치를 저장한다. 바뀐 것이 없으면 바로 성공. 실패하면 서버 배치로 되돌리고 false.</summary>
    private async UniTask<bool> CommitAsync() {
        if (!_session.IsDirty) return true;
        _busy = true;
        try {
            var res = await _ctx.Frontline.SetMembersAsync(_frontlineId, _session.Members, _ct);
            _session.MarkCommitted();
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(FrontlineRules.SetMembersFailMessage(res.FailReason));
                LoadSquad();
                return false;
            }
            return true;
        } catch (OperationCanceledException) {
            return false;
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast(FrontlineRules.SetMembersFailMessage(0));
            return false;
        } finally {
            _busy = false;
        }
    }

    // ── 표시 갱신 ────────────────────────────────────
    private void RefreshAll() {
        RefreshHeader();
        RefreshSlots();
        RefreshSummary();
        RefreshSynergyList();
        RefreshRosterState();
    }

    private void RefreshHeader() {
        if (tabs != null)
            for (int i = 0; i < tabs.Length; i++)
                if (tabs[i] != null) tabs[i].SetSelected(i == _frontlineId);

        var squad = Info?.Squad(_frontlineId);
        if (frontlineNameText != null) frontlineNameText.text = NameOf(_frontlineId);
        if (levelText != null) levelText.text = FrontlineRules.StageText(squad?.Stage ?? FrontlineRules.StartStage);
        if (lockedNotice != null) lockedNotice.SetActive(squad != null && squad.Locked);
    }

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
                slot.SetLocked(false);
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
            // 슬롯에 있다는 것 자체가 배치됐다는 뜻이라 편성/대표/육성 배지는 쓰지 않는다.
            card.SetState(inDeck: false, isRepresentative: false, isSelected: false);
            card.SetUpgradeBadge(false);
            card.SetGuideTarget(false);
            card.SetTowerBonus(false);
            card.SetDragSource(this, i);
            slot.SetFilled(true);
            slot.SetLocked(_lockedMembers.Contains(charId));
        }
    }

    private void RefreshSummary() {
        if (memberCountText != null) memberCountText.text = $"{_session.Count} / {DeckEditSession.Capacity}";
        if (totalPowerText != null)
            totalPowerText.text = AccountStats.SquadCombatPower(_ctx, _session.Members).ToString("N0");
    }

    private void RecomputeSynergies() {
        if (_ctx.SynergyDb != null) _ctx.SynergyDb.CollectActive(_session.Members, _activeSynergies);
        else _activeSynergies.Clear();
    }

    private void RefreshSynergyList() {
        foreach (var r in _synergyRows) if (r != null) Destroy(r.gameObject);
        _synergyRows.Clear();
        if (synergyEmptyGroup != null) synergyEmptyGroup.SetActive(_activeSynergies.Count == 0);
        if (synergyContent == null || synergyRowPrefab == null) return;

        foreach (var synergy in _activeSynergies) {
            if (synergy == null) continue;
            var row = Instantiate(synergyRowPrefab, synergyContent);
            row.Bind(synergy);
            _synergyRows.Add(row);
        }
    }

    // 편집으로 새로 켜진 시너지만 알린다. 껐다 다시 켜면 다시 알리도록 꺼진 것은 기록에서 지운다.
    private void AnnounceNewSynergies() {
        _announcedSynergyIds.RemoveWhere(id => !_activeSynergies.Exists(s => s != null && s.Id == id));
        foreach (var synergy in _activeSynergies) {
            if (synergy == null || !_announcedSynergyIds.Add(synergy.Id)) continue;
            _ctx.Ui.ShowSynergyActivated(synergy.DisplayName, synergy.EffectSummary());
        }
    }

    // 보유 소녀 목록은 열 때 한 번만 만든다(편성을 만질 때마다 다시 만들면 스크롤이 튀고 집던 카드가 파괴된다).
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

    // 덱/파견/타워 화면과 같은 정렬 기준(등급 높은 순 -> 레벨 높은 순 -> id).
    private int Compare(CharacterData a, CharacterData b) {
        int gradeCmp = b.Grade.CompareTo(a.Grade);
        if (gradeCmp != 0) return gradeCmp;

        int la = _ctx.Store.GetProgress(a.Id)?.Level ?? 0;
        int lb = _ctx.Store.GetProgress(b.Id)?.Level ?? 0;
        int levelCmp = lb.CompareTo(la);
        if (levelCmp != 0) return levelCmp;

        return a.Id.CompareTo(b.Id);
    }

    // 다른 전선에 배치된 소녀는 숨긴다(파견과 같은 방식). 이 전선의 소녀는 편성 표식만 켠다.
    private void RefreshRosterState() {
        var info = Info;
        foreach (var card in _rosterCards) {
            if (card == null || card.Data == null) continue;
            int id = card.Data.Id;
            bool elsewhere = info != null && info.IsDeployedElsewhere(id, _frontlineId) && !_session.Contains(id);
            card.gameObject.SetActive(!elsewhere);
            card.SetState(inDeck: _session.Contains(id), isRepresentative: false, isSelected: false);
            card.SetDragSource(this, CompositionDragHandle.RosterSlot);
        }
    }

    private void ApplyEdit() {
        _ctx.Sound.PlaySfx(SfxKey.CardPlace);
        RecomputeSynergies();
        RefreshAll();
        AnnounceNewSynergies();
    }

    // ── 드래그(CompositionDragHandle.IHost) ──────────
    // 고정된 소녀(클리어 기록이 있는 전선의 스쿼드)는 슬롯에서도 목록에서도 집을 수 없다.
    // 집지 못한 입력은 스크롤로 넘어가므로 목록 스크롤은 그대로 된다.
    public bool CanDrag(CompositionDragHandle handle) =>
        !_busy && Info != null && _dragging == null && handle != null && handle.CharDataId != 0 &&
        !_lockedMembers.Contains(handle.CharDataId);

    public void OnDragBegan(CompositionDragHandle handle, PointerEventData eventData) {
        _dragging = handle;
        _ctx.Sound.PlaySfx(SfxKey.CardPick);
        handle.SetDimmed(true);
        _drag.Begin(handle, eventData);
    }

    public void OnDragMoved(CompositionDragHandle handle, PointerEventData eventData) => _drag.Move(eventData);

    public void OnDragEnded(CompositionDragHandle handle, PointerEventData eventData) {
        var target = _drag.HoveredSlot;
        if (handle != null) handle.SetDimmed(false);
        _drag.Clear();
        _dragging = null;

        // eventData 가 없으면 카드가 파괴되어 드래그가 끊긴 경우다(편집하지 않고 조용히 정리).
        if (eventData == null || handle == null) return;

        bool changed;
        if (target != null) {
            if (_lockedMembers.Contains(_session.At(target.SlotIndex))) {
                // 고정된 소녀가 있는 자리에는 놓을 수 없다(교체도 자리 바꾸기도 고정된 소녀를 움직인다).
                _ctx.Ui.ShowToast(FrontlineRules.SetMembersFailMessage(5));
                RefreshAll();
                return;
            }
            if (handle.IsFromSlot) {
                changed = _session.MoveSlot(handle.SlotIndex, target.SlotIndex);
            } else {
                changed = _session.Place(target.SlotIndex, handle.CharDataId);
                if (!changed && _session.IsFull && !_session.Contains(handle.CharDataId))
                    _ctx.Ui.ShowToast("배치가 가득 찼습니다. 빈 자리가 없으면 소녀 위에 놓아 교체하세요.");
            }
        } else if (handle.IsFromSlot) {
            changed = _session.RemoveAt(handle.SlotIndex);   // 자리 밖에 놓으면 배치 해제(고정된 소녀는 집을 수 없다)
        } else {
            return;   // 목록에서 집어 밖에 놓은 것은 취소
        }

        if (changed) ApplyEdit();
        else RefreshAll();   // 흐리게 남은 카드 등 표시를 원래대로 되돌린다
    }

    // ── 정리 ─────────────────────────────────────────
    // 닫히는 경로(닫기 버튼/배경 탭/뒤로가기)와 상관없이 남은 편집을 저장한다. 팝업은 곧 파괴되므로 기다리지 않는다.
    protected override void OnClosed() {
        if (_session.IsDirty && _ctx != null) {
            var members = new List<int>(_session.Members);
            _session.MarkCommitted();
            SaveDetachedAsync(_ctx.Frontline, _ctx.Ui, _frontlineId, members).Forget();
        }

        _subscriptions.Clear();
        _drag?.Clear();
        _dragging = null;

        foreach (var c in _rosterCards) if (c != null) Destroy(c.gameObject);
        _rosterCards.Clear();
        if (_slotCards != null)
            for (int i = 0; i < _slotCards.Length; i++) {
                if (_slotCards[i] != null) Destroy(_slotCards[i].gameObject);
                _slotCards[i] = null;
            }
        foreach (var r in _synergyRows) if (r != null) Destroy(r.gameObject);
        _synergyRows.Clear();
    }

    // 팝업 수명과 무관하게 끝까지 저장한다(서비스가 응답으로 현황을 갱신하므로 전선 팝업도 따라 바뀐다).
    private static async UniTaskVoid SaveDetachedAsync(IFrontlineService service, IUIService ui, int frontlineId,
                                                       IReadOnlyList<int> members) {
        try {
            var res = await service.SetMembersAsync(frontlineId, members);
            if (!res.IsSuccess) ui.ShowToast(FrontlineRules.SetMembersFailMessage(res.FailReason));
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void OnDestroy() => _subscriptions.Dispose();
}
