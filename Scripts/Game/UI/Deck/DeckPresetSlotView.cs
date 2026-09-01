using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 편성 프리셋 1칸. 저장된 편성의 캐릭터 초상들을 미리보기로 보여주고, 비어 있으면 "비어 있음"을 표시한다.
/// 클릭은 상위(DeckPresetPopup)에 통지하고, 실제 저장/적용 판단은 그쪽이 한다(역할 분리).
/// </summary>
public sealed class DeckPresetSlotView : MonoBehaviour {
    [Header("Info")]
    [Tooltip("칸 이름. 유저가 지정한 이름이 있으면 그것을, 없으면 '프리셋 N' 을 표시한다.")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("이름 변경 버튼. 누르면 이름 입력 팝업이 열린다(편성이 없는 빈 칸도 이름을 붙일 수 있다).")]
    [SerializeField] private Button renameButton;
    [Tooltip("비어 있을 때 켜는 안내 오브젝트.")]
    [SerializeField] private GameObject emptyGroup;
    [Tooltip("저장된 편성이 있을 때 켜는 루트.")]
    [SerializeField] private GameObject filledGroup;
    [Tooltip("편성 전투력 합계 표시(선택).")]
    [SerializeField] private TMP_Text combatPowerText;

    [Header("Members")]
    [Tooltip("편성원 초상 슬롯(최대 5개 배치). 저장된 인원 수만큼만 켜진다. 첫 칸이 대표다. " +
             "덱 카드/전투 슬롯과 동일하게 CharacterIllustrationView 를 쓴다(캐릭터별 UiFocus 프레이밍이 적용된다).")]
    [SerializeField] private CharacterIllustrationView[] memberIllustrations;
    [Tooltip("대표 캐릭터 표식(선택). 대표는 항상 첫 칸에 오므로 첫 초상 슬롯 위에 겹쳐 배치한다. " +
             "초상 슬롯에 마스크가 걸려 있으면 그 자식으로 넣지 말 것(마스크에 잘린다). 같은 부모의 형제로 두고 위에 올린다.")]
    [SerializeField] private GameObject representativeMark;

    [Header("Interaction")]
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;

    public int Slot { get; private set; }
    public bool IsEmpty { get; private set; }

    /// <summary>현재 지정된 이름(없으면 빈 문자열). 이름 변경 팝업의 초기값으로 넘긴다.</summary>
    public string PresetName { get; private set; } = "";

    /// <param name="preset">저장된 편성(없거나 비어 있으면 빈 칸으로 표시).</param>
    /// <param name="onClick">칸 선택 통지.</param>
    /// <param name="onRename">이름 변경 버튼 통지.</param>
    public void Bind(MainHudContext ctx, int slot, DeckPresetDto preset,
                     Action<DeckPresetSlotView> onClick, Action<DeckPresetSlotView> onRename) {
        Slot = slot;
        IsEmpty = preset == null || preset.IsEmpty;
        PresetName = preset != null ? preset.Name : "";

        // 이름은 매번 데이터에서 다시 채운다(변경 직후에도 즉시 반영되도록).
        if (titleText != null) titleText.text = DeckPresetRules.DisplayName(slot, PresetName);
        if (emptyGroup != null) emptyGroup.SetActive(IsEmpty);
        if (filledGroup != null) filledGroup.SetActive(!IsEmpty);

        BindMembers(ctx, preset);

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
        if (renameButton != null) {
            renameButton.onClick.RemoveAllListeners();
            renameButton.onClick.AddListener(() => onRename?.Invoke(this));
        }
    }

    // 대표를 첫 칸에 오도록 정렬해 보여준다(전투 캐릭터 패널과 같은 규칙이라 눈에 익다).
    private void BindMembers(MainHudContext ctx, DeckPresetDto preset) {
        var ordered = OrderedMembers(preset);
        float totalPower = 0f;

        if (memberIllustrations != null) {
            for (int i = 0; i < memberIllustrations.Length; i++) {
                var view = memberIllustrations[i];
                if (view == null) continue;

                bool used = i < ordered.Count;
                view.gameObject.SetActive(used);
                // 외형(기본 일러스트 또는 장착 코스튬)을 그대로 넘겨야 그 그림의 관심점으로 프레이밍된다.
                if (used) view.Bind(ctx.Appearance.Mine(ordered[i]));
                else view.Clear();
            }
        }

        // 대표는 OrderedMembers 가 항상 첫 칸으로 끌어올리므로, 표식은 첫 초상이 실제로 채워졌을 때만 켠다.
        if (representativeMark != null) representativeMark.SetActive(ordered.Count > 0);

        for (int i = 0; i < ordered.Count; i++) {
            var data = ctx.CharDb.Get(ordered[i]);
            var progress = ctx.Store.GetProgress(ordered[i]);
            if (data == null || progress == null) continue;
            totalPower += new CharacterRuntime(data, progress, ctx.Settings,
                                               ctx.Store.TotalBonusFor(ordered[i], ctx.EquipDb)).CombatPower;
        }

        if (combatPowerText != null) {
            combatPowerText.gameObject.SetActive(!IsEmpty);
            if (!IsEmpty) combatPowerText.text = Mathf.RoundToInt(totalPower).ToString("N0");
        }
    }

    // 대표를 0번으로 끌어올린 편성원 목록.
    private static List<int> OrderedMembers(DeckPresetDto preset) {
        var list = new List<int>();
        if (preset == null || preset.IsEmpty) return list;

        if (preset.CharIds.Contains(preset.RepresentativeCharId)) list.Add(preset.RepresentativeCharId);
        foreach (int id in preset.CharIds)
            if (id != preset.RepresentativeCharId) list.Add(id);
        return list;
    }

    public void SetSelected(bool selected) {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }
}
