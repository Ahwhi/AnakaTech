using UnityEngine;
using static Enums;

/// <summary>
/// 전투 캐릭터 패널(PLANNING 2-4). 현재 파티를 슬롯에 바인딩한다. 대표가 제일 좌측(슬롯 0),
/// 나머지가 가로로 이어진다(BattleDirector 가 대표를 파티 목록 순서에 반영). 파티 재구성 시 다시 바인딩한다.
/// 접기/펴기는 공용 FoldablePanel 이 맡는다(접힘 상태는 로컬 JSON 에 저장된다).
///
/// 슬롯을 누르면 그 캐릭터로 전투 카메라 포커스를 옮긴다. 판정과 저장은 무대(BattleStage)가 하고
/// 패널은 요청을 넘긴 뒤 CameraFocusChanged 로 돌아온 결과만 슬롯 표시에 반영한다(단일 진실 원천).
/// </summary>
public sealed class BattleCharacterPanel : MonoBehaviour {
    [Tooltip("좌측(대표)부터 순서대로. 최대 5개 배치.")]
    [SerializeField] private BattleCharacterSlot[] slots;
    [Tooltip("레벨업 VFX 를 재생할 레이어. 메인 HUD 캔버스 아래(팝업보다 낮은 정렬)에 두어 팝업이 열리면 VFX 가 가려지게 한다. 전역 VFX 레이어(팝업 위)를 쓰면 안 됨.")]
    [SerializeField] private UIVfxLayer vfxLayer;
    [Tooltip("접기/펴기를 담당하는 공용 컴포넌트. 접힘 상태 저장 키도 거기서 지정한다. 비우면 항상 펼친 상태.")]
    [SerializeField] private FoldablePanel foldable;

    private BattleHudContext _ctx;

    public void Initialize(BattleHudContext ctx) {
        _ctx = ctx;
        if (foldable != null) foldable.Initialize(ctx.Sound);
        // 쓰러짐 문구(부활 대기 / 전투불능)는 슬롯이 BattleSettings 에서 읽으므로 먼저 넣어 준다.
        if (slots != null) {
            foreach (var slot in slots) {
                if (slot == null) continue;
                slot.Initialize(ctx.Settings);
                slot.Clicked += OnSlotClicked;
            }
        }
        ctx.Stage.PartyChanged += Rebind;
        ctx.Stage.CameraFocusChanged += ApplyCameraFocus;
        ctx.Store.CharactersChanged += OnCharactersChanged;   // 방치 전투 경험치/레벨 변동을 슬롯에 반영(+레벨업 VFX)
        Rebind();
    }

    // 슬롯 클릭 = 카메라 포커스 요청. 대상이 실제로 바뀌면 CameraFocusChanged 로 표시가 따라온다.
    private void OnSlotClicked(int charId) {
        _ctx.Sound?.PlaySfx(SfxKey.ButtonClick);
        _ctx.Stage.SetCameraFocus(charId);
    }

    // 무대가 비추고 있는 캐릭터의 슬롯에만 포커스 표시를 켠다.
    private void ApplyCameraFocus() {
        if (slots == null) return;
        int focusId = _ctx.Stage.CameraFocusCharId;

        foreach (var slot in slots) {
            if (slot == null) continue;
            slot.SetFocused(slot.CharId >= 0 && slot.CharId == focusId);
        }
    }

    // 경험치/성장 변동 시 각 슬롯의 레벨/경험치를 갱신하고, 레벨이 오른 슬롯에 레벨업 VFX 를 재생한다.
    // (로스터/스냅샷 변동은 PartyChanged 로 재바인딩되므로 여기서는 다루지 않는다.)
    private void OnCharactersChanged(CharacterChange change) {
        if (change.Reason != CharacterChangeReason.Exp && change.Reason != CharacterChangeReason.Growth) return;
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++) {
            var slot = slots[i];
            if (slot == null || slot.CharId < 0) continue;
            if (change.Ids != null && !Contains(change.Ids, slot.CharId)) continue;

            var progress = _ctx.Store.GetProgress(slot.CharId);
            if (slot.ApplyProgress(progress)) PlayLevelUpVfx(slot);
        }
    }

    // 레벨업 VFX 는 전역 레이어(팝업 위)가 아니라 HUD 로컬 레이어에 재생해 팝업이 열리면 가려지게 한다.
    // 패널이 접혀 있으면 슬롯이 화면에 없으므로 빈자리에 이펙트만 터지지 않도록 건너뛴다.
    private void PlayLevelUpVfx(BattleCharacterSlot slot) {
        if (foldable != null && !foldable.IsExpanded) return;
        if (_ctx.UiVfx == null || vfxLayer == null) return;
        var prefab = _ctx.UiVfx.Get("levelup");
        if (prefab != null) vfxLayer.Play(prefab, slot.VfxAnchor);
    }

    private static bool Contains(System.Collections.Generic.IReadOnlyList<int> ids, int id) {
        for (int i = 0; i < ids.Count; i++) if (ids[i] == id) return true;
        return false;
    }

    private void Rebind() {
        if (slots == null) return;
        var party = _ctx.Stage.Party;

        // 대표를 좌측(슬롯 0)에 오도록 정렬한다(파티 목록 순서는 덱 순서라 대표가 0이 아닐 수 있음).
        for (int i = 0; i < slots.Length; i++) {
            var slot = slots[i];
            if (slot == null) continue;

            if (party != null && i < party.Count) {
                var unit = OrderedAt(party, i);
                var data = unit.Data;
                var progress = _ctx.Store.GetProgress(data.Id);
                slot.Bind(unit, data, progress);
            } else {
                slot.Clear();
            }
        }

        ApplyCameraFocus();   // 슬롯 구성이 바뀌었으니 포커스 표시도 새 배치에 맞춘다.
    }

    // 대표 캐릭터를 인덱스 0 으로 끌어올린 순서로 접근한다.
    private static CharacterUnit OrderedAt(System.Collections.Generic.IReadOnlyList<CharacterUnit> party, int index) {
        int repIndex = -1;
        for (int i = 0; i < party.Count; i++) {
            if (party[i].IsRepresentative) { repIndex = i; break; }
        }
        if (repIndex <= 0) return party[index];      // 대표가 없거나 이미 0번

        if (index == 0) return party[repIndex];
        // 0..repIndex-1 은 한 칸씩 뒤로 밀림, 나머지는 그대로.
        if (index <= repIndex) return party[index - 1];
        return party[index];
    }

    private void OnDestroy() {
        if (_ctx != null) {
            _ctx.Stage.PartyChanged -= Rebind;
            _ctx.Stage.CameraFocusChanged -= ApplyCameraFocus;
            _ctx.Store.CharactersChanged -= OnCharactersChanged;
        }
        if (slots != null)
            foreach (var slot in slots) if (slot != null) slot.Clicked -= OnSlotClicked;
    }
}
