using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 코스튬 화면(DeckPopup 내부 패널, 육성/장비 패널과 같은 방식으로 도킹해 열린다).
///
/// 선택 캐릭터가 가질 수 있는 코스튬을 전부 목록으로 보여 주고(미보유도 잠금 상태로 노출),
/// 보유한 것 중 하나를 골라 장착 버튼으로 바꾼다. 첫 칸은 항상 "기본"이라 언제든 원래 일러스트로 되돌릴 수 있다.
///
/// 장착은 서버 권위다. 응답이 성공일 때만 저장소에 반영하며, 저장소가 알리면 외형을 쓰는 모든 화면
/// (덱/전투/프로필/길드/친구/랭킹/아레나)이 같은 값을 보게 된다. 코스튬은 스탯도 바꾸므로
/// 저장소가 성장 변경 통로로도 알려 전투력/능력치 표시가 함께 따라온다.
///
/// 코스튬이 하나도 없는 캐릭터도 이 패널을 연다(기본 칸만 보인다). 아직 없는 것과 버튼이 고장난 것을
/// 유저가 구분할 수 없으므로 버튼을 잠그지 않으며, 나중에 그 캐릭터의 코스튬이 추가되어도
/// 데이터만 넣으면 목록에 그대로 나타난다(이 화면은 손댈 것이 없다).
/// </summary>
public sealed class CostumePopup : UIView {
    [Header("List")]
    [Tooltip("코스튬 칸이 생성될 부모(Grid/Vertical Layout).")]
    [SerializeField] private Transform content;
    [Tooltip("코스튬 칸 프리팹(CostumeSlotView).")]
    [SerializeField] private CostumeSlotView slotPrefab;

    [Header("Detail")]
    [Tooltip("선택한 코스튬의 큰 일러스트. 비우면 표시 안 함.")]
    [SerializeField] private CharacterIllustrationView illustrationView;
    [Tooltip("선택한 코스튬 이름.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("선택한 코스튬 등급 표시. 기본 칸에서는 숨긴다. 선택.")]
    [SerializeField] private GradeText gradeText;
    [Tooltip("코스튬 소개 문구. 내용이 없으면 통째로 숨긴다. 선택.")]
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("장착 효과 요약(예: 공격력 +15). 효과가 없으면 안내 문구가 들어간다. 선택.")]
    [SerializeField] private TMP_Text effectText;
    [Tooltip("효과가 있을 때의 글자색. 칸(CostumeSlotView)과 같은 색을 넣어야 목록과 상세가 따로 놀지 않는다.")]
    [SerializeField] private Color effectColor = Color.white;
    [Tooltip("'장착 효과 없음'일 때의 글자색.")]
    [SerializeField] private Color noEffectColor = Color.black;
    [Tooltip("미보유 안내 문구. 보유 중이면 숨긴다. 선택.")]
    [SerializeField] private GameObject lockedNotice;

    [Header("Buttons")]
    [Tooltip("장착 버튼. 이미 장착 중이거나 미보유면 비활성이 된다.")]
    [SerializeField] private Button equipButton;
    [Tooltip("장착 버튼 라벨(장착 / 장착중). 선택.")]
    [SerializeField] private TMP_Text equipButtonLabel;
    [SerializeField] private Button closeButton;

    private const string EquipLabel = "장착";
    private const string EquippedLabel = "장착중";

    private MainHudContext _hud;
    private CancellationToken _ct;
    private int _charId;
    private bool _wired;   // 리스너 1회 구성 가드(내장 패널로 재사용되므로 중복 리스너 방지).
    private bool _busy;

    private readonly List<CostumeSlotView> _slots = new();
    private CostumeSlotView _selected;

    /// <summary>코스튬이 바뀌어 외형/능력치가 달라졌을 때 발생(상위 화면이 상세 표시를 갱신한다).</summary>
    public event Action Changed;

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

        Build();
    }

    // 기본 칸 + 이 캐릭터의 코스튬 전부. 칸 수는 캐릭터마다 달라지므로 열 때마다 다시 만든다
    // (한 캐릭터의 코스튬은 많아야 몇 개라 재사용 풀을 둘 만큼의 비용이 아니다).
    private void Build() {
        ClearSlots();
        if (content == null || slotPrefab == null) return;

        var character = _hud.CharDb != null ? _hud.CharDb.Get(_charId) : null;
        if (character == null) return;

        int equippedId = _hud.Store.EquippedCostumeId(_charId);

        // 첫 칸은 항상 기본 외형(되돌리기). 서버 보유 목록과 무관하게 언제나 고를 수 있다.
        AddSlot(character, null, owned: true, equippedId == 0);

        var costumes = _hud.CostumeDb != null ? _hud.CostumeDb.ForCharacter(_charId) : Array.Empty<CostumeData>();
        for (int i = 0; i < costumes.Count; i++) {
            var costume = costumes[i];
            if (costume == null) continue;
            AddSlot(character, costume, _hud.Store.OwnsCostume(costume.Id), equippedId == costume.Id);
        }

        // 처음에는 지금 장착 중인 것을 골라 둔다(어떤 외형을 쓰고 있는지 바로 보이도록).
        Select(FindSlot(equippedId) ?? (_slots.Count > 0 ? _slots[0] : null));
    }

    private void AddSlot(CharacterData character, CostumeData costume, bool owned, bool equipped) {
        var view = Instantiate(slotPrefab, content);
        view.Bind(new CharacterAppearance(character, costume), costume, owned, equipped, OnSlotClicked);
        _slots.Add(view);
    }

    private void OnSlotClicked(CostumeSlotView slot) {
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        Select(slot);
    }

    private void Select(CostumeSlotView slot) {
        _selected = slot;
        foreach (var s in _slots) if (s != null) s.SetSelected(s == slot);
        RefreshDetail();
    }

    private void RefreshDetail() {
        var character = _hud.CharDb != null ? _hud.CharDb.Get(_charId) : null;
        var costume = _selected != null ? _selected.Data : null;
        bool owned = _selected == null || _selected.Owned;
        bool equipped = _selected != null && _selected.CostumeId == _hud.Store.EquippedCostumeId(_charId);

        if (illustrationView != null) {
            if (character != null) illustrationView.Bind(new CharacterAppearance(character, costume));
            else illustrationView.Clear();
        }
        if (nameText != null)
            nameText.text = costume != null ? costume.DisplayName : CostumeSlotView.DefaultLabel;

        if (gradeText != null) {
            gradeText.gameObject.SetActive(costume != null);
            if (costume != null) gradeText.SetGrade(costume.Grade);
        }

        if (descriptionText != null) {
            string desc = costume != null ? costume.Description : "";
            descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(desc));
            descriptionText.text = desc;
        }

        if (effectText != null) {
            // 색 규칙은 목록 칸과 같다(효과 있으면 강조색, 없으면 죽은 색).
            string summary = costume != null ? costume.EffectSummary() : "";
            bool hasEffect = !string.IsNullOrEmpty(summary);
            effectText.text = hasEffect ? summary : CostumeSlotView.NoEffectText;
            effectText.color = hasEffect ? effectColor : noEffectColor;
        }

        if (lockedNotice != null) lockedNotice.SetActive(!owned);

        if (equipButton != null) equipButton.interactable = !_busy && owned && !equipped;
        if (equipButtonLabel != null) equipButtonLabel.text = equipped ? EquippedLabel : EquipLabel;
    }

    private async UniTaskVoid EquipSelectedAsync() {
        if (_busy || _selected == null) return;
        if (!_selected.Owned) { _hud.Ui.ShowToast("보유하지 않은 코스튬입니다."); return; }

        int costumeId = _selected.CostumeId;
        if (costumeId == _hud.Store.EquippedCostumeId(_charId)) return;

        _busy = true;
        RefreshDetail();
        try {
            var res = await _hud.Costume.EquipAsync(_charId, costumeId, _ct);
            if (!res.IsSuccess) {
                _hud.Ui.ShowToast(CostumeFailMessage.Of(res.FailReason));
                return;
            }

            // 서버가 확정한 값으로 반영한다. 저장소가 알리면 외형을 쓰는 모든 화면이 함께 바뀐다.
            _hud.Store.ApplyCostumeEquip(_charId, res.CostumeId);
            RefreshEquippedMarks();
            Changed?.Invoke();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("코스튬을 변경하지 못했습니다.");
        } finally {
            _busy = false;
            RefreshDetail();
        }
    }

    // 장착 표식만 제자리 갱신한다(목록을 다시 만들지 않아 스크롤/선택이 튀지 않는다).
    private void RefreshEquippedMarks() {
        int equippedId = _hud.Store.EquippedCostumeId(_charId);
        foreach (var s in _slots) if (s != null) s.SetEquipped(s.CostumeId == equippedId);
    }

    private CostumeSlotView FindSlot(int costumeId) {
        foreach (var s in _slots) if (s != null && s.CostumeId == costumeId) return s;
        return null;
    }

    private void ClearSlots() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
        _selected = null;
    }

    protected override void OnClosed() => ClearSlots();
}
