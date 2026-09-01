using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 편성 프리셋 팝업(덱 팝업의 프리셋 버튼). 3칸을 보여주고, 칸을 고르면
///  - 저장된 칸  : 확인/취소 팝업으로 물은 뒤 그 편성을 현재 덱으로 적용
///  - 비어 있는 칸: "현재 스쿼드 저장" 버튼이 활성화되어 지금 편성을 저장
/// 저장/적용 모두 서버 권위이며(덱 자체가 서버 값), 성공 응답을 받은 뒤에만 스토어에 반영한다.
///
/// 적용에 성공하면 Applied 이벤트로 알린다. 덱 팝업이 이를 받아 그리드/상세를 다시 그린다.
/// </summary>
public sealed class DeckPresetPopup : UIView {
    [Header("Slots")]
    [Tooltip("프리셋 칸 3개. 배치 순서대로 슬롯 0/1/2 로 쓴다.")]
    [SerializeField] private DeckPresetSlotView[] slots;

    [Header("Buttons")]
    [Tooltip("선택한 칸에 현재 스쿼드를 저장. 빈 칸을 골랐을 때만 활성화된다.")]
    [SerializeField] private Button saveButton;
    [Tooltip("선택한 칸의 편성을 현재 덱으로 적용. 저장된 칸을 골랐을 때만 활성화된다.")]
    [SerializeField] private Button applyButton;
    [Tooltip("선택한 칸을 덮어쓰기 위한 버튼(선택). 저장된 칸을 골랐을 때 활성화된다.")]
    [SerializeField] private Button overwriteButton;
    [SerializeField] private Button closeButton;
    [Tooltip("목록을 불러오는 동안 표시할 로딩 그룹(선택).")]
    [SerializeField] private GameObject loadingGroup;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private bool _busy;

    private readonly Dictionary<int, DeckPresetDto> _presets = new();
    private DeckPresetSlotView _selected;

    /// <summary>프리셋을 현재 덱에 적용해 서버까지 반영됐을 때 발생.</summary>
    public event Action Applied;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        WireButtons();
        LoadAsync().Forget();
    }

    private void WireButtons() {
        if (saveButton != null) {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); SaveSelectedAsync().Forget(); });
        }
        if (overwriteButton != null) {
            overwriteButton.onClick.RemoveAllListeners();
            overwriteButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OverwriteAsync().Forget(); });
        }
        if (applyButton != null) {
            applyButton.onClick.RemoveAllListeners();
            applyButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); ApplyAsync().Forget(); });
        }
        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
        }
    }

    private async UniTaskVoid LoadAsync() {
        if (loadingGroup != null) loadingGroup.SetActive(true);
        try {
            var list = await _ctx.MainData.GetDeckPresetsAsync(_ct);
            _presets.Clear();
            foreach (var p in list) _presets[p.Slot] = p;
            Rebuild();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("프리셋을 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
    }

    private void Rebuild() {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++) {
            var view = slots[i];
            if (view == null) continue;
            _presets.TryGetValue(i, out var preset);
            view.Bind(_ctx, i, preset, OnSlotClicked, OnRenameClicked);
            view.SetSelected(_selected == view);
        }
        RefreshButtons();
    }

    private void OnSlotClicked(DeckPresetSlotView view) {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        _selected = view;
        foreach (var s in slots)
            if (s != null) s.SetSelected(s == view);
        RefreshButtons();
    }

    private void RefreshButtons() {
        bool hasSelection = _selected != null;
        bool empty = hasSelection && _selected.IsEmpty;

        // 빈 칸에는 저장만, 저장된 칸에는 적용/덮어쓰기만 할 수 있게 해 오조작을 줄인다.
        if (saveButton != null) saveButton.interactable = empty;
        if (applyButton != null) applyButton.interactable = hasSelection && !empty;
        if (overwriteButton != null) overwriteButton.interactable = hasSelection && !empty;
    }

    // ── 이름 변경 ──
    private void OnRenameClicked(DeckPresetSlotView view) {
        if (_busy || view == null) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        RenameAsync(view.Slot, view.PresetName).Forget();
    }

    private async UniTaskVoid RenameAsync(int slot, string currentName) {
        if (_busy) return;
        _busy = true;
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<DeckPresetRenamePopup>(_ctx.Popups.DeckPresetRenamePopup, _ct);
            string name = await popup.ShowAsync(slot, currentName);
            if (name == null) return;   // 취소(빈 문자열은 "기본 이름으로 되돌리기"라 유효한 입력이다)

            var res = await _ctx.MainData.RenameDeckPresetAsync(slot, name, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(FailMessage(res.FailReason));
                return;
            }
            // 서버가 다듬어 확정한 이름을 그대로 반영한다(길이 초과분을 자른 결과가 화면과 일치하도록).
            _presets[slot] = res.Preset;
            Rebuild();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("이름을 바꾸지 못했습니다.");
        } finally {
            _busy = false;
        }
    }

    // ── 저장 ──
    // 이미 저장된 칸을 덮어쓸 때는 한 번 물어본다(실수로 기존 편성을 날리지 않게).
    private async UniTaskVoid OverwriteAsync() {
        if (_selected == null) return;

        var confirm = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(_ctx.Popups.ConfirmCancelPopup, _ct);
        bool ok = await confirm.ShowAsync("프리셋 덮어쓰기",
            $"'{NameOf(_selected.Slot)}' 에 현재 스쿼드를 저장할까요?\n기존에 저장된 편성은 사라집니다.");
        if (!ok) return;

        await SaveSelectedAsync();
    }

    // 선택한 칸에 현재 덱을 저장한다. 빈 칸은 바로, 저장된 칸은 확인을 받은 뒤 이 경로로 들어온다.
    private async UniTask SaveSelectedAsync() {
        if (_busy || _selected == null) return;
        int slot = _selected.Slot;

        _busy = true;
        try {
            var res = await _ctx.MainData.SaveDeckPresetAsync(slot, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(FailMessage(res.FailReason));
                return;
            }
            _presets[slot] = res.Preset;
            _ctx.Ui.ShowToast($"'{res.Preset.DisplayName}' 에 저장했습니다.");
            Rebuild();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("프리셋 저장에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    // ── 적용 ──
    private async UniTaskVoid ApplyAsync() {
        if (_busy || _selected == null || _selected.IsEmpty) return;
        int slot = _selected.Slot;

        var confirm = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(_ctx.Popups.ConfirmCancelPopup, _ct);
        bool ok = await confirm.ShowAsync("편성 변경",
            $"'{NameOf(slot)}' 의 편성으로 바꿀까요?");
        if (!ok) return;

        _busy = true;
        try {
            var res = await _ctx.MainData.ApplyDeckPresetAsync(slot, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(FailMessage(res.FailReason));
                return;
            }
            // 서버가 확정한 구성을 그대로 스토어에 반영한다(클라가 따로 조립하지 않는다).
            _ctx.Store.ApplyDeck(res.Preset.CharIds, res.Preset.RepresentativeCharId);
            _ctx.Ui.ShowToast("편성을 변경했습니다.");
            Applied?.Invoke();
            await CloseAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("편성 변경에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    // 안내 문구에 쓸 프리셋 표시 이름(지정 이름이 없으면 "프리셋 N").
    private string NameOf(int slot) =>
        _presets.TryGetValue(slot, out var p) ? p.DisplayName : DeckPresetRules.DisplayName(slot, null);

    // 서버 SaveDeckPreset / ApplyDeckPreset / RenameDeckPreset 의 실패 코드와 짝을 이룬다.
    private static string FailMessage(int fail) => fail switch {
        1 => "잘못된 프리셋 칸입니다.",
        2 => "저장된 편성이 없습니다.",
        3 => "보유하지 않은 캐릭터가 포함되어 있습니다.",
        _ => "처리할 수 없습니다."
    };
}
