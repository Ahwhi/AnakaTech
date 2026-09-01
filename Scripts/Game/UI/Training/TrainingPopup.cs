using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 수련(집중 수련) 팝업. 왼쪽에 수련 슬롯 5칸, 오른쪽에 보유 소녀 목록을 두고,
/// 소녀를 고른 뒤 비어 있는 슬롯의 시작 버튼으로 수련을 건다.
///
/// 화면 구성 원칙:
///  - 판단은 전부 여기서 하고 슬롯/카드 뷰는 받은 상태를 그리기만 한다.
///  - 수련 중인 소녀는 목록에서 빼지 않고 오버레이로 덮는다. 목록에서 사라지면 카드 위치가 매번 달라져
///    "내가 누구를 수련 보냈는지" 를 목록에서 확인할 수 없게 되기 때문이다(수련 중임을 그 자리에서 보여준다).
///  - 이미 수련 효과가 있는 소녀를 다시 수련시키면 효과가 대체되므로 시작 전에 반드시 확인을 받는다.
///
/// 서버 응답으로 스토어를 갱신하면 스토어가 변경을 알리고(TrainingChanged) 화면이 다시 그려진다.
/// 수련 효과는 능력치라 확정되는 순간 전투/전투력에도 즉시 반영된다(스토어가 CharactersChanged 를 낸다).
/// </summary>
public sealed class TrainingPopup : UIView, IHudPopup {
    [Header("Slots")]
    [Tooltip("수련 슬롯 뷰. 배치 순서가 곧 슬롯 번호(0-based)다. 개수는 서버 maxSlots 와 맞춘다.")]
    [SerializeField] private TrainingSlotView[] slots;

    [Header("Character List")]
    [Tooltip("소녀 카드가 생성될 부모(스크롤뷰 Content).")]
    [SerializeField] private Transform gridContent;
    [Tooltip("수련용 소녀 카드 프리팹(덱 카드 프리팹을 복제해 오버레이를 추가한 것).")]
    [SerializeField] private TrainingCharacterCard cardPrefab;
    [Tooltip("목록을 채우는 동안 표시할 로딩 그룹(선택).")]
    [SerializeField] private GameObject loadingGroup;

    [Header("Selection")]
    [Tooltip("선택한 소녀의 이름. 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text selectedNameText;
    [Tooltip("선택한 소녀가 보유한 수련 효과. 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text selectedEffectText;
    [Tooltip("선택한 소녀로 수련을 시작하는 버튼. 비어 있는 슬롯 중 가장 앞 칸에 들어간다.")]
    [SerializeField] private Button startButton;
    [Tooltip("수련 소요 시간 안내(예: 24시간). 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text durationText;

    [Header("Info")]
    [Tooltip("확률 보기 버튼. 서버가 내려준 확률표를 그대로 보여주는 팝업을 연다.")]
    [SerializeField] private Button ratesButton;

    [Header("Currency")]
    [Tooltip("상단 재화 패널. 다이아 / 수련 완료권 CurrencyView 를 자식으로 배치한다.")]
    [SerializeField] private CurrencyPanel currencyPanel;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private const int BuildPerFrame = 8;   // 한 프레임에 생성할 카드 수(덱 팝업과 같은 방식으로 분산 생성).

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private CancellationTokenSource _gridCts;

    private readonly List<TrainingCharacterCard> _cards = new();

    // 서버가 내려준 값 중 스토어에 담지 않는 것(화면에서만 쓰는 상수/확률표).
    private int _maxSlots = 1;
    private int _nextUnlockCost;
    private int _durationSeconds;
    private int _instantTicketCost = 1;
    private readonly List<TrainingRateDto> _rates = new();

    private CharacterData _selected;
    private bool _busy;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (currencyPanel != null) currencyPanel.Initialize(ctx);
        WireButtons();

        _ctx.Store.TrainingChanged -= Rebuild;
        _ctx.Store.TrainingChanged += Rebuild;

        LoadAsync().Forget();
    }

    private void WireButtons() {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
        if (startButton != null)
            startButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); StartAsync().Forget(); });
        if (ratesButton != null)
            ratesButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenRatesAsync().Forget(); });

        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++) {
            var view = slots[i];
            if (view == null) continue;
            // 비용 아이콘(다이아 / 수련 완료권)은 재화 카탈로그에서 1회만 배선한다(재화 종류가 고정이다).
            view.Configure(_ctx.Currencies);
            view.UnlockRequested += OnUnlockRequested;
            view.CancelRequested += OnCancelRequested;
            view.InstantRequested += OnInstantRequested;
            view.ClaimRequested += OnClaimRequested;
            // 진행 중이던 수련의 시간이 다 되면 버튼 구성이 바뀌어야 하므로 슬롯이 알려 준다(폴링 없음).
            view.Finished += OnSlotFinished;
        }
    }

    private async UniTaskVoid LoadAsync() {
        if (loadingGroup != null) loadingGroup.SetActive(true);
        try {
            var info = await _ctx.Training.RequestInfoAsync(_ct);
            _maxSlots = Mathf.Max(1, info.MaxSlots);
            _nextUnlockCost = info.NextUnlockCost;
            _durationSeconds = info.DurationSeconds;
            _instantTicketCost = Mathf.Max(1, info.InstantTicketCost);
            _rates.Clear();
            _rates.AddRange(info.Rates);

            // ApplyTrainingInfo 가 TrainingChanged 를 내며 Rebuild 를 부른다.
            _ctx.Store.ApplyTrainingInfo(info);
            RebuildGrid();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("수련 정보를 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
    }

    // ── 목록 ───────────────────────────────────────
    private void RebuildGrid() => RebuildGridAsync().Forget();

    private async UniTaskVoid RebuildGridAsync() {
        _gridCts?.Cancel();
        _gridCts?.Dispose();
        _gridCts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var ct = _gridCts.Token;

        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();

        if (gridContent == null || cardPrefab == null) return;

        var owned = new List<CharacterData>();
        foreach (var kv in _ctx.Store.Characters) {
            var d = _ctx.CharDb.Get(kv.Key);
            if (d != null) owned.Add(d);
        }
        owned.Sort(Compare);

        try {
            await AsyncBuild.SpawnChunkedAsync(owned, BuildPerFrame, d => {
                var card = Instantiate(cardPrefab, gridContent);
                card.Bind(_ctx.Appearance.Mine(d), _ctx.Store.GetProgress(d.Id), OnCardClicked);
                _cards.Add(card);
            }, ct);
        } catch (OperationCanceledException) {
            return;
        }

        // 처음 들어오면 아직 수련하지 않은 소녀 중 첫 번째를 골라 둔다(바로 시작할 수 있게).
        if (_selected == null && _cards.Count > 0) _selected = FirstUntrained() ?? _cards[0].Data;
        Rebuild();
    }

    // 정렬: 대표 -> 편성 -> 등급(높은 순) -> 레벨(높은 순). 덱 팝업과 같은 순서다.
    // 수련 여부는 정렬 기준으로 쓰지 않는다. 수련한 소녀가 뒤로 밀리면 방금 수련시킨 소녀가 목록에서
    // 사라진 것처럼 보이고, 화면을 열 때마다 카드 위치가 달라져 늘 보던 자리에서 찾을 수 없게 된다.
    private int Compare(CharacterData a, CharacterData b) {
        var deck = _ctx.Store.Deck;
        int rep = deck.RepresentativeCharId;

        int ra = a.Id == rep ? 0 : 1, rb = b.Id == rep ? 0 : 1;
        if (ra != rb) return ra - rb;

        int da = deck.SelectedCharIds.Contains(a.Id) ? 0 : 1;
        int db = deck.SelectedCharIds.Contains(b.Id) ? 0 : 1;
        if (da != db) return da - db;

        int gradeCmp = b.Grade.CompareTo(a.Grade);
        if (gradeCmp != 0) return gradeCmp;

        int la = _ctx.Store.GetProgress(a.Id)?.Level ?? 0;
        int lb = _ctx.Store.GetProgress(b.Id)?.Level ?? 0;
        int levelCmp = lb.CompareTo(la);
        if (levelCmp != 0) return levelCmp;

        return a.Id.CompareTo(b.Id);   // 동률 시 안정적 순서 보장
    }

    // 처음 들어왔을 때 고를 기본 대상. 아직 수련하지 않았고 수련 중도 아닌 소녀를 목록 순서대로 찾는다
    // (정렬은 건드리지 않고 "무엇을 미리 골라 둘지"에만 수련 여부를 쓴다).
    private CharacterData FirstUntrained() {
        foreach (var card in _cards) {
            if (card == null || card.Data == null) continue;
            if (_ctx.Store.TryGetTraining(card.Data.Id, out _)) continue;
            if (_ctx.Store.TryGetTrainingSlotOf(card.Data.Id, out _)) continue;
            return card.Data;
        }
        return null;
    }

    private void OnCardClicked(TrainingCharacterCard card) {
        if (card == null || card.Data == null) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        _selected = card.Data;
        Rebuild();
    }

    // ── 다시 그리기(슬롯 + 카드 + 선택 정보) ─────────────
    private void Rebuild() {
        RebuildSlots();
        RefreshCards();
        RefreshSelection();
        // 배지 판정 근거(진행 중인 슬롯의 완료 시각)를 최신 상태로 맞춘다. 서버를 다시 두드리지 않는다.
        _ctx.Training.TrackEndTimes(_ctx.Store.TrainingSlotList());
    }

    private void RebuildSlots() {
        if (slots == null) return;

        int unlocked = _ctx.Store.TrainingUnlockedSlots;
        int diamond = _ctx.Store.Diamond.Value;
        bool canInstant = _ctx.Store.TrainingTicket.Value >= _instantTicketCost;

        for (int i = 0; i < slots.Length; i++) {
            var view = slots[i];
            if (view == null) continue;

            // 서버가 정한 슬롯 수보다 많이 배치된 칸은 아예 감춘다(칸 수를 데이터로 늘려도 화면이 따라온다).
            bool exists = i < _maxSlots;
            if (view.gameObject.activeSelf != exists) view.gameObject.SetActive(exists);
            if (!exists) continue;

            if (i >= unlocked) {
                // 해금은 낮은 번호부터 순서대로만 열리므로, 바로 다음 칸에만 비용을 걸어 준다.
                bool isNext = i == unlocked;
                view.BindLocked(i, isNext ? _nextUnlockCost : 0, diamond >= _nextUnlockCost);
                continue;
            }

            if (_ctx.Store.TrainingSlots.TryGetValue(i, out var slot) && slot != null && slot.CharDataId != 0) {
                view.BindRunning(i, _ctx.Appearance.Mine(slot.CharDataId), slot.StartUnix, slot.EndUnix,
                                 _instantTicketCost, canInstant);
            } else {
                view.BindEmpty(i);
            }
        }
    }

    private void RefreshCards() {
        var deck = _ctx.Store.Deck;
        foreach (var card in _cards) {
            if (card == null || card.Data == null) continue;
            int id = card.Data.Id;
            card.RefreshProgress(_ctx.Store.GetProgress(id));
            TryEffectPartsOf(id, out string effectName, out string effectValue);
            card.SetTrainingState(BusyTextOf(id), effectName, effectValue,
                                  selected: _selected != null && _selected.Id == id,
                                  inDeck: deck.SelectedCharIds.Contains(id),
                                  isRepresentative: deck.RepresentativeCharId == id);
        }
    }

    // 수련 중이면 안내 문구, 아니면 빈 문자열. 남은 시간은 슬롯이 초 단위로 보여주므로 카드에는 상태만 적는다
    // (같은 카운트다운을 두 곳에서 굴리면 갱신 주기가 어긋나 서로 다른 숫자가 보인다).
    private string BusyTextOf(int charDataId) {
        if (!_ctx.Store.TryGetTrainingSlotOf(charDataId, out var slot)) return "";
        return slot.EndUnix > KstTime.NowUnix ? "수련중" : "수련 완료";
    }

    private string EffectTextOf(int charDataId) =>
        _ctx.Store.TryGetTraining(charDataId, out var t) ? TrainingRules.Describe(t.Effect, t.ValueTenth) : "";

    /// <summary>
    /// 카드에 넘길 수련 효과의 능력치 이름과 수치. 카드가 둘을 다른 색으로 다른 줄에 그리므로 합치지 않고 준다
    /// (어떻게 보일지는 화면의 몫이고, 여기서는 무엇을 보여 줄지만 정한다).
    /// </summary>
    private bool TryEffectPartsOf(int charDataId, out string name, out string value) {
        if (_ctx.Store.TryGetTraining(charDataId, out var t)) {
            name = TrainingRules.EffectName(t.Effect);
            value = TrainingRules.ValueText(t.Effect, t.ValueTenth);
            return true;
        }
        name = "";
        value = "";
        return false;
    }

    private void RefreshSelection() {
        if (selectedNameText != null)
            selectedNameText.text = _selected != null ? _selected.DisplayName : "";
        // 상세는 항상 보이는 자리라 효과가 없으면 "없음"으로 채운다(빈 칸이면 아직 안 불러온 것처럼 보인다).
        // 고른 소녀가 아직 없을 때만 비워 둔다.
        if (selectedEffectText != null) {
            string effect = _selected != null ? EffectTextOf(_selected.Id) : "";
            selectedEffectText.text = _selected == null ? ""
                                    : (string.IsNullOrEmpty(effect) ? TrainingRules.NoEffectText : effect);
        }
        if (durationText != null && _durationSeconds > 0)
            durationText.text = $"{_durationSeconds / 3600}시간";

        // 시작 버튼은 "고른 소녀가 있고, 그 소녀가 수련 중이 아니며, 빈 슬롯이 있을 때"만 열린다.
        bool canStart = _selected != null
                        && !_ctx.Store.TryGetTrainingSlotOf(_selected.Id, out _)
                        && FindEmptySlot() >= 0;
        if (startButton != null) startButton.interactable = canStart && !_busy;
    }

    // 비어 있는 해금된 슬롯 중 가장 앞 칸(없으면 -1).
    private int FindEmptySlot() {
        int unlocked = Mathf.Min(_ctx.Store.TrainingUnlockedSlots, _maxSlots);
        for (int i = 0; i < unlocked; i++)
            if (!_ctx.Store.TrainingSlots.ContainsKey(i)) return i;
        return -1;
    }

    // ── 수련 시작 ──────────────────────────────────
    private async UniTaskVoid StartAsync() {
        if (_busy || _selected == null) return;

        int slotIndex = FindEmptySlot();
        if (slotIndex < 0) { _ctx.Ui.ShowToast("비어 있는 수련 슬롯이 없습니다."); return; }
        if (_ctx.Store.TryGetTrainingSlotOf(_selected.Id, out _)) {
            _ctx.Ui.ShowToast("이미 수련 중인 소녀입니다.");
            return;
        }

        // 이미 수련 효과를 가진 소녀는 효과가 대체되므로 반드시 확인을 받는다.
        if (_ctx.Store.TryGetTraining(_selected.Id, out var current)) {
            bool ok = await ConfirmAsync("수련 시작",
                TrainingRules.OverwriteConfirmMessage(_selected.DisplayName,
                                                      (TrainingEffect)current.Effect, current.ValueTenth));
            if (!ok) return;
        }

        await ActAsync(TrainingActionType.Start, slotIndex, _selected.Id, "수련을 시작했습니다.");
    }

    // ── 슬롯 버튼 ──────────────────────────────────
    private void OnUnlockRequested(int slotIndex) {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        UnlockAsync().Forget();
    }

    private async UniTaskVoid UnlockAsync() {
        if (_busy) return;
        if (_nextUnlockCost <= 0) { _ctx.Ui.ShowToast("모든 수련 슬롯을 이미 해금했습니다."); return; }
        if (_ctx.Store.Diamond.Value < _nextUnlockCost) { _ctx.Ui.ShowToast("다이아가 부족합니다."); return; }

        if (!await ConfirmAsync("수련 슬롯 해금", TrainingRules.UnlockConfirmMessage(_nextUnlockCost))) return;
        await ActAsync(TrainingActionType.UnlockSlot, 0, 0, "수련 슬롯을 해금했습니다.");
    }

    private void OnCancelRequested(int slotIndex) {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        CancelAsync(slotIndex).Forget();
    }

    private async UniTaskVoid CancelAsync(int slotIndex) {
        if (_busy) return;
        if (!await ConfirmAsync("수련 중단", TrainingRules.CancelConfirmMessage)) return;
        await ActAsync(TrainingActionType.Cancel, slotIndex, 0, "수련을 중단했습니다.");
    }

    private void OnInstantRequested(int slotIndex) {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        InstantAsync(slotIndex).Forget();
    }

    private async UniTaskVoid InstantAsync(int slotIndex) {
        if (_busy) return;
        if (_ctx.Store.TrainingTicket.Value < _instantTicketCost) {
            _ctx.Ui.ShowToast("수련 완료권이 부족합니다.");
            return;
        }
        if (!await ConfirmAsync("즉시 완료", TrainingRules.InstantConfirmMessage(_instantTicketCost))) return;
        await ActAsync(TrainingActionType.Instant, slotIndex, 0, "수련이 즉시 완료되었습니다.");
    }

    private void OnClaimRequested(int slotIndex) {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        ClaimAsync(slotIndex).Forget();
    }

    private async UniTaskVoid ClaimAsync(int slotIndex) {
        if (_busy) return;

        var result = await ActAsync(TrainingActionType.Claim, slotIndex, 0, null);
        if (result == null || !result.IsSuccess) return;

        // 획득한 효과를 그 자리에서 알린다(전투력 상승 연출은 스토어 통지를 받아 별도로 뜬다).
        _ctx.Sound.PlaySfx(SfxKey.TrainingComplete);
        var data = _ctx.CharDb.Get(result.CharDataId);
        string name = data != null ? data.DisplayName : "";
        _ctx.Ui.ShowToast($"{name} 수련 완료! {TrainingRules.Describe(result.Effect, result.ValueTenth)}");
    }

    // 시간이 다 된 슬롯은 버튼 구성이 바뀌므로 화면 전체를 한 번 다시 맞춘다(목록의 "수련 완료" 표시도 함께 바뀐다).
    private void OnSlotFinished(int slotIndex) {
        RefreshCards();
        RefreshSelection();
    }

    // 모든 쓰기 작업의 공통 경로. 성공하면 스토어에 반영하고(화면은 통지로 다시 그려진다) 안내를 띄운다.
    private async UniTask<TrainingActionResultDto> ActAsync(TrainingActionType action, int slotIndex,
                                                            int charDataId, string successMessage) {
        _busy = true;
        RefreshSelection();
        try {
            var res = await _ctx.Training.ActAsync(action, slotIndex, charDataId, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(TrainingRules.FailMessage(res.FailReason));
                return res;
            }

            _ctx.Store.ApplyTrainingAction(res);
            if (!string.IsNullOrEmpty(successMessage)) _ctx.Ui.ShowToast(successMessage);
            return res;
        } catch (OperationCanceledException) {
            return null;
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("수련 처리에 실패했습니다.");
            return null;
        } finally {
            _busy = false;
            RefreshSelection();
        }
    }

    // ── 확률 보기 ──────────────────────────────────
    private async UniTaskVoid OpenRatesAsync() {
        if (_ctx.Popups.TrainingRatesPopup == null || !_ctx.Popups.TrainingRatesPopup.RuntimeKeyIsValid()) {
            Debug.LogError("[TrainingPopup] UIPopupCatalog 에 수련 확률 팝업이 할당되지 않았습니다.");
            return;
        }
        var popup = await _ctx.Ui.ShowPopupAsync<TrainingRatesPopup>(_ctx.Popups.TrainingRatesPopup, _ct);
        popup.Show(_rates);
    }

    // 공용 확인 팝업. 되돌릴 수 없는 작업(효과 대체/중단/재화 소모) 앞에서만 쓴다.
    private async UniTask<bool> ConfirmAsync(string title, string message) {
        var popup = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(_ctx.Popups.ConfirmCancelPopup, _ct);
        return await popup.ShowAsync(title, message);
    }

    protected override void OnClosed() => Unsubscribe();

    private void OnDestroy() => Unsubscribe();

    private void Unsubscribe() {
        if (_ctx != null && _ctx.Store != null) _ctx.Store.TrainingChanged -= Rebuild;
        if (slots != null) {
            foreach (var view in slots) {
                if (view == null) continue;
                view.UnlockRequested -= OnUnlockRequested;
                view.CancelRequested -= OnCancelRequested;
                view.InstantRequested -= OnInstantRequested;
                view.ClaimRequested -= OnClaimRequested;
                view.Finished -= OnSlotFinished;
            }
        }
        _gridCts?.Cancel();
        _gridCts?.Dispose();
        _gridCts = null;
    }
}
