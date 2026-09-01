using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 각인 페이지(캐릭터 상세 팝업 내부 패널). 선택 캐릭터의 각인 슬롯들과 합계 효과를 보여주고,
/// 슬롯을 누르면 각인 선택 팝업을 연다.
///
/// 슬롯 해금은 별레벨(EngravingRules.SlotUnlockStar)로 정해지므로, 별 성장과 각인이 자연스럽게 이어진다.
/// 각인 레벨은 계정 공용이라 이 화면에서 올린 레벨은 그 각인을 낀 모든 캐릭터에 함께 적용된다.
/// </summary>
public sealed class EngravingPanel : MonoBehaviour {
    [Header("Slots")]
    [Tooltip("각인 슬롯 뷰들. 순서가 아니라 각 뷰의 Slot Index 값으로 판정하므로 배치 순서는 자유다.")]
    [SerializeField] private EngravingSlotView[] slots;

    [Header("Summary")]
    [Tooltip("장착 각인의 '항상 적용' 효과 합계(전투력에 반영되는 부분).")]
    [SerializeField] private TMP_Text totalEffectText;
    [Tooltip("조건부 효과 안내(전투 중에만 붙는 효과 목록). 선택.")]
    [SerializeField] private TMP_Text conditionalEffectText;
    [Tooltip("보유 각인석 수량. 선택.")]
    [SerializeField] private TMP_Text stoneText;
    [Tooltip("각인석 아이콘. 재화 카탈로그에서 자동으로 채운다(선택).")]
    [SerializeField] private Image stoneIcon;

    // 효과가 하나도 없을 때 두 요약 칸이 공통으로 쓰는 문구.
    private const string NoEffect = "효과 없음";

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private int _charId;
    private bool _busy;

    /// <summary>각인이 바뀌어 캐릭터 능력치가 달라졌을 때 발생(상위가 전투력 표시를 갱신한다).</summary>
    public event Action Changed;

    public void Initialize(MainHudContext ctx, int charDataId) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();
        _charId = charDataId;

        CurrencyIcon.Apply(stoneIcon, ctx.Currencies, CurrencyType.EngravingStone);
        Refresh();
    }

    /// <summary>스토어의 최신 값으로 슬롯/요약을 다시 그린다.</summary>
    public void Refresh() {
        if (_ctx == null) return;

        var progress = _ctx.Store.GetProgress(_charId);
        int star = progress != null ? progress.StarLevel : 0;
        var equipped = _ctx.Store.EngravingSlotsOf(_charId);

        if (slots != null) {
            for (int i = 0; i < slots.Length; i++) {
                var view = slots[i];
                if (view == null) continue;

                int index = view.SlotIndex;
                bool unlocked = EngravingRules.IsSlotUnlocked(index, star);

                int id = index >= 0 && index < equipped.Length ? equipped[index] : 0;
                var data = id != 0 && _ctx.EngravingDb != null ? _ctx.EngravingDb.Get(id) : null;
                _ctx.Store.EngravingLevels.TryGetValue(id, out int level);

                view.Bind(data, level, unlocked, OnSlotClicked);
                view.SetSelected(false);
            }
        }

        if (stoneText != null) stoneText.text = _ctx.Store.EngravingStone.Value.ToString("N0");

        RefreshSummary();
    }

    // 항상 적용 효과는 합산 값으로, 조건부 효과는 문구로 보여준다(조건부는 수치를 합쳐 봐야 오해만 생긴다).
    // 둘 다 비어 있어도 칸을 비우지 않고 같은 문구를 채운다(빈 줄이 남으면 레이아웃이 캐릭터마다 달라진다).
    private void RefreshSummary() {
        var loadout = _ctx.Store.EngravingLoadoutFor(_charId);

        if (totalEffectText != null) {
            string summary = StatFormat.DeltaSummary(loadout.Always);
            totalEffectText.text = string.IsNullOrEmpty(summary) ? NoEffect : summary;
        }

        if (conditionalEffectText != null) {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < loadout.Conditionals.Count; i++) {
                var c = loadout.Conditionals[i];
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(EngravingRules.TriggerText(c.Trigger, c.Param))
                  .Append(' ')
                  .Append(GrowthRules.StatName(c.Stat))
                  .Append(" +")
                  .Append(StatFormat.FormatValue(c.Stat, c.Value));
            }
            conditionalEffectText.text = sb.Length > 0 ? sb.ToString() : NoEffect;
        }
    }

    private void OnSlotClicked(EngravingSlotView view) {
        if (_busy || view == null || !view.Unlocked) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenSelectAsync(view).Forget();
    }

    private async UniTaskVoid OpenSelectAsync(EngravingSlotView view) {
        _busy = true;
        view.SetSelected(true);
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<EngravingSelectPopup>(_ctx.Popups.EngravingSelectPopup, _ct);
            popup.Initialize(_ctx, _charId, view.SlotIndex);
            await popup.ShowAsync();
            await popup.WaitUntilClosedAsync();

            Refresh();
            if (popup.Changed) Changed?.Invoke();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("각인 화면을 열지 못했습니다.");
        } finally {
            view.SetSelected(false);
            _busy = false;
        }
    }
}
