using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 각성 패널(별레벨 +1). EquipmentPopup 내부에서 켜고 끄는 패널이다.
/// 가방 패널과 달리 "대상과 같은 장비"만 재료 후보로 보여주며, 카드 클릭으로 선택/해제하고 자동 선택도 지원한다.
/// 각성은 확정 성공(실패/파괴 없음)이며, 필요 재료 수 = 현재 별레벨, 필요 다이아 = 현재 별레벨 x 100 이다.
///
/// 이미 각성된(별레벨 2 이상) 장비가 재료에 섞이면 진행 전에 경고하고 진행/제외/취소를 고르게 한다.
/// </summary>
public sealed class EquipmentAwakenPanel : MonoBehaviour {
    [Header("Target (각성 대상)")]
    [SerializeField] private Image targetIcon;
    [SerializeField] private TMP_Text targetNameText;
    [SerializeField] private GradeText targetGradeText;
    [SerializeField] private StarLevelView targetStarView;
    [Tooltip("각성 결과 안내(예: 2성 -> 3성).")]
    [SerializeField] private TMP_Text targetStarStepText;
    [Tooltip("대상 레벨(예: Lv.24 / 40). 상세 패널과 같은 형식으로 최대 레벨과 함께 표시한다. 선택.")]
    [SerializeField] private TMP_Text targetLevelText;
    [Tooltip("대상의 주스탯/옵션 목록. 상세 패널과 같은 문구(EquipmentRules.DescribeStats)를 쓴다. 선택.")]
    [SerializeField] private TMP_Text targetStatsText;
    [Tooltip("각성 성공 VFX 를 띄울 기준 위치. 비우면 targetIcon 의 RectTransform 을 쓴다.")]
    [SerializeField] private RectTransform vfxAnchor;

    [Header("Materials (재료)")]
    [SerializeField] private Transform materialGrid;
    [SerializeField] private EquipmentCard materialCardPrefab;
    [Tooltip("재료로 쓸 같은 장비가 하나도 없을 때 켜는 오브젝트. 선택.")]
    [SerializeField] private GameObject materialEmptyGroup;
    [Tooltip("재료 아이콘(대상 장비와 동일). 선택.")]
    [SerializeField] private Image materialIcon;
    [Tooltip("선택 수량 표시(예: 2/3).")]
    [SerializeField] private TMP_Text materialCountText;
    [SerializeField] private Button autoSelectButton;
    [SerializeField] private Color countEnoughColor = new Color(0.29f, 0.78f, 0.35f);
    [SerializeField] private Color countLackColor = new Color(0.85f, 0.27f, 0.27f);

    [Header("Cost (다이아)")]
    [Tooltip("다이아 아이콘. 재화 카탈로그에서 자동 배선된다.")]
    [SerializeField] private Image diaIcon;
    [SerializeField] private TMP_Text diaCostText;

    [Header("Actions")]
    [SerializeField] private Button awakenButton;
    [SerializeField] private Button closeButton;
    [Tooltip("배경 탭으로 닫기(선택). 패널 뒤를 덮는 배경 이미지(보통 Dim)를 그대로 끌어다 놓으면 그 바깥을 눌러 닫을 수 있다. " +
             "클릭 처리는 코드가 붙이므로 Button 컴포넌트를 따로 달 필요가 없다. 비워 두면 배경 탭으로 닫히지 않는다(할당 = 사용).")]
    [SerializeField] private Graphic tapToCloseArea;

    /// <summary>각성이 성공해 인벤토리가 바뀌었음(상위가 재동기화한다).</summary>
    public event Action Awakened;
    /// <summary>패널 닫기 요청.</summary>
    public event Action CloseRequested;

    private const int BuildPerFrame = 8;

    private MainHudContext _hud;
    private int _charId;      // 전용무기 표식 판정용(재료 카드에 전달).
    private long _targetId;
    private CancellationToken _ct;
    private CancellationTokenSource _buildCts;

    private readonly List<EquipmentCard> _cards = new();
    private readonly List<long> _selected = new();
    private bool _busy;
    private bool _wired;

    public void Initialize(MainHudContext hud, int charDataId, long targetInstanceId) {
        _hud = hud;
        _charId = charDataId;
        _targetId = targetInstanceId;
        _ct = this.GetCancellationTokenOnDestroy();

        if (!_wired) {
            if (autoSelectButton != null)
                autoSelectButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); AutoSelect(); });
            if (awakenButton != null)
                awakenButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); OnAwaken().Forget(); });
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseRequested?.Invoke(); });
            // 배경 탭도 닫기 버튼과 같은 결과를 낸다(팝업들과 조작이 어긋나지 않게).
            TapToClose.Bind(tapToCloseArea, () => {
                _hud.Sound.PlaySfx(SfxKey.ButtonClick);
                CloseRequested?.Invoke();
            });
            CurrencyIcon.Apply(diaIcon, _hud.Currencies, CurrencyType.Diamond);

            // 다이아 보유량이 밖에서 바뀌어도(상점 구매/우편 수령 등) 비용 색과 버튼 활성이 즉시 따라오게 한다.
            // 재료 목록은 다시 만들지 않으므로 선택 상태와 스크롤이 유지된다.
            _hud.Store.Diamond.Subscribe(_ => OnDiamondChanged()).AddTo(this);
            _wired = true;
        }

        _selected.Clear();
        RefreshTarget();
        RebuildMaterials(AutoSelect);   // 열릴 때 한 번 자동 선택해두면 대부분의 경우 바로 각성할 수 있다.
    }

    // ── 표시 ──

    private EquipmentInstanceDto Target =>
        _hud.Store.Equipment.TryGetValue(_targetId, out var t) ? t : null;

    private void RefreshTarget() {
        var inst = Target;
        var data = inst != null ? _hud.EquipDb.Get(inst.EquipDataId) : null;
        if (inst == null || data == null) return;

        if (targetIcon != null) {
            targetIcon.sprite = data.Icon;
            targetIcon.enabled = data.Icon != null;
        }
        if (materialIcon != null) {
            materialIcon.sprite = data.Icon;
            materialIcon.enabled = data.Icon != null;
        }
        if (targetNameText != null) targetNameText.text = data.DisplayName;
        if (targetGradeText != null) targetGradeText.SetGrade(data.Grade);
        if (targetStarView != null) targetStarView.SetStarLevel(inst.StarLevel);
        // 레벨/옵션은 상세 패널과 같은 형식/문구를 쓴다(같은 장비를 두 화면에서 다르게 읽지 않도록).
        if (targetLevelText != null)
            targetLevelText.text = $"Lv.{inst.Level} / {EquipmentRules.MaxLevel(inst.StarLevel)}";
        if (targetStatsText != null)
            targetStatsText.text = EquipmentRules.DescribeStats(inst, data);

        bool canAwaken = EquipmentRules.CanAwaken(inst.StarLevel);
        if (targetStarStepText != null)
            targetStarStepText.text = canAwaken
                ? $"{inst.StarLevel}성 -> {inst.StarLevel + 1}성"
                : "최대 각성";

        if (diaCostText != null) {
            int dia = EquipmentRules.AwakenDiaCost(inst.StarLevel);
            diaCostText.text = canAwaken ? dia.ToString("N0") : "-";
            diaCostText.color = !canAwaken || _hud.Store.Diamond.Value >= dia ? countEnoughColor : countLackColor;
        }
        RefreshCountAndButton();
    }

    private void RefreshCountAndButton() {
        var inst = Target;
        if (inst == null) return;

        int need = EquipmentRules.AwakenMaterialCount(inst.StarLevel);
        bool canAwaken = EquipmentRules.CanAwaken(inst.StarLevel);
        bool enoughMat = _selected.Count >= need;
        bool enoughDia = _hud.Store.Diamond.Value >= EquipmentRules.AwakenDiaCost(inst.StarLevel);

        if (materialCountText != null) {
            materialCountText.text = $"{_selected.Count}/{need}";
            materialCountText.color = enoughMat ? countEnoughColor : countLackColor;
        }
        if (awakenButton != null)
            awakenButton.interactable = canAwaken && enoughMat && enoughDia && !_busy;
    }

    // ── 재료 목록 ──

    private void RebuildMaterials(Action onBuilt) => RebuildMaterialsAsync(onBuilt).Forget();

    private async UniTaskVoid RebuildMaterialsAsync(Action onBuilt) {
        _buildCts?.Cancel();
        _buildCts?.Dispose();
        _buildCts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var ct = _buildCts.Token;

        ClearCards();

        var list = CollectMaterials();
        if (materialEmptyGroup != null) materialEmptyGroup.SetActive(false);

        try {
            await AsyncBuild.SpawnChunkedAsync(list, BuildPerFrame, inst => {
                var card = Instantiate(materialCardPrefab, materialGrid);
                card.Bind(inst, _hud.EquipDb.Get(inst.EquipDataId), _charId, OnMaterialClicked);
                card.SetSelected(_selected.Contains(inst.InstanceId));
                _cards.Add(card);
            }, ct);
        } catch (OperationCanceledException) {
            return;
        }

        if (materialEmptyGroup != null) materialEmptyGroup.SetActive(_cards.Count == 0);
        onBuilt?.Invoke();
    }

    // 재료 후보 판정은 스토어(PlayerStateStore.IsAwakenMaterial)에 있다. 장착 슬롯의 각성 가능 표시도
    // 같은 판정으로 개수를 세므로, 규칙이 갈라져 "표시는 되는데 재료가 없다"가 되지 않는다.
    // 정렬: 별레벨 낮은 것 -> 레벨 낮은 것 -> 미착용 먼저. 아까운 장비가 자동 선택에 덜 걸리게 한다.
    private List<EquipmentInstanceDto> CollectMaterials() {
        var target = Target;
        var list = new List<EquipmentInstanceDto>();
        if (target == null) return list;

        foreach (var kv in _hud.Store.Equipment) {
            var inst = kv.Value;
            if (!PlayerStateStore.IsAwakenMaterial(target, inst, _charId)) continue;
            list.Add(inst);
        }
        list.Sort(CompareMaterial);
        return list;
    }

    private static int CompareMaterial(EquipmentInstanceDto a, EquipmentInstanceDto b) {
        int cmp = a.StarLevel.CompareTo(b.StarLevel);
        if (cmp != 0) return cmp;
        cmp = a.Level.CompareTo(b.Level);
        if (cmp != 0) return cmp;
        // 착용 중인 장비는 뒤로(자동 선택에서 마지막 후보가 되게).
        cmp = a.IsEquipped.CompareTo(b.IsEquipped);
        if (cmp != 0) return cmp;
        return a.InstanceId.CompareTo(b.InstanceId);
    }

    private void OnMaterialClicked(EquipmentCard card) {
        if (_busy || card == null || card.Inst == null) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);

        long id = card.Inst.InstanceId;
        var inst = Target;
        int need = inst != null ? EquipmentRules.AwakenMaterialCount(inst.StarLevel) : 0;

        if (_selected.Contains(id)) {
            _selected.Remove(id);
        } else {
            // 필요 개수를 넘겨 고르지 못하게 막는다(초과 소모 방지).
            if (_selected.Count >= need) {
                _hud.Ui.ShowToast($"재료는 {need}개까지 선택할 수 있습니다.");
                return;
            }
            _selected.Add(id);
        }
        RefreshSelectionMarks();
        RefreshCountAndButton();
    }

    /// <summary>필요 개수만큼 앞에서부터(가장 아깝지 않은 순으로) 자동 선택한다.</summary>
    private void AutoSelect() {
        var inst = Target;
        if (inst == null) return;
        int need = EquipmentRules.AwakenMaterialCount(inst.StarLevel);

        _selected.Clear();
        foreach (var c in _cards) {
            if (_selected.Count >= need) break;
            if (c != null && c.Inst != null) _selected.Add(c.Inst.InstanceId);
        }
        RefreshSelectionMarks();
        RefreshCountAndButton();
    }

    private void RefreshSelectionMarks() {
        foreach (var c in _cards)
            if (c != null && c.Inst != null) c.SetSelected(_selected.Contains(c.Inst.InstanceId));
    }

    // ── 각성 실행 ──

    private async UniTaskVoid OnAwaken() {
        if (_busy) return;
        var inst = Target;
        if (inst == null) return;

        int need = EquipmentRules.AwakenMaterialCount(inst.StarLevel);
        if (_selected.Count < need) { _hud.Ui.ShowToast("재료가 부족합니다."); return; }

        // 이미 각성된 장비가 재료에 섞였으면 진행 여부를 묻는다(되돌릴 수 없는 소모라 반드시 확인).
        if (HasAwakenedMaterial()) {
            int choice = await AskAwakenedMaterialAsync();
            if (choice == ThreeChoicePopup.Cancelled || choice == 2) return;   // 취소
            if (choice == 1) {                                                 // 해당 장비 제외
                ExcludeAwakenedMaterials(need);
                if (_selected.Count < need) {
                    _hud.Ui.ShowToast("각성되지 않은 재료가 부족합니다.");
                    return;
                }
            }
            // choice == 0 이면 그대로 진행.
        }

        _busy = true;
        RefreshCountAndButton();
        try {
            var materials = _selected.GetRange(0, need);
            var res = await _hud.Inventory.AwakenAsync(_targetId, materials, _ct);
            if (!res.IsSuccess) {
                _hud.Ui.ShowToast(AwakenFailMessage(res.FailReason));
                return;
            }

            // 서버 권위 값 즉시 반영 + 연출(각성음은 PlayAwakenVfx 안에서 VFX 와 같이 난다).
            _hud.Store.Diamond.Value = res.NewDiamond;
            PlayAwakenVfx();

            _selected.Clear();
            Awakened?.Invoke();      // 상위가 인벤토리를 재요청한다.
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("각성에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    // 다이아 변동 알림. 패널이 닫혀 있거나 대상이 사라졌으면 할 일이 없다.
    private void OnDiamondChanged() {
        if (!gameObject.activeInHierarchy || Target == null) return;
        RefreshTarget();   // 비용 색 + 버튼 활성만 다시 계산(재료 목록은 그대로).
    }

    /// <summary>인벤토리 재동기화 후 상위가 호출한다(대상 별레벨/재료 목록 갱신).</summary>
    public void Refresh() {
        if (Target == null) { CloseRequested?.Invoke(); return; }
        RefreshTarget();
        RebuildMaterials(null);
    }

    private bool HasAwakenedMaterial() {
        foreach (long id in _selected)
            if (_hud.Store.Equipment.TryGetValue(id, out var m) && m.StarLevel > 1) return true;
        return false;
    }

    private async UniTask<int> AskAwakenedMaterialAsync() {
        var popup = await _hud.Ui.ShowPopupAsync<ThreeChoicePopup>(_hud.Popups.ThreeChoicePopup, _ct);
        return await popup.ShowAsync(
            "경고",
            "이미 각성된 장비가 재료에 포함되어 있습니다.\n소모하면 되돌릴 수 없습니다.",
            "진행", "해당 장비 제외", "취소");
    }

    // 각성된 재료를 빼고, 남은(미각성) 후보로 필요 개수를 다시 채운다.
    private void ExcludeAwakenedMaterials(int need) {
        _selected.RemoveAll(id => _hud.Store.Equipment.TryGetValue(id, out var m) && m.StarLevel > 1);
        foreach (var c in _cards) {
            if (_selected.Count >= need) break;
            if (c?.Inst == null) continue;
            if (c.Inst.StarLevel > 1) continue;
            if (!_selected.Contains(c.Inst.InstanceId)) _selected.Add(c.Inst.InstanceId);
        }
        RefreshSelectionMarks();
        RefreshCountAndButton();
    }

    private void PlayAwakenVfx() {
        // 소리는 VFX 배선 여부와 무관하게 낸다(연출 프리팹이 비어 있어도 각성한 느낌은 남아야 한다).
        _hud.Sound.PlaySfx(SfxKey.EquipAwaken);
        if (_hud.UiVfx == null) return;
        var prefab = _hud.UiVfx.Get("equip_awaken");
        var anchor = vfxAnchor != null ? vfxAnchor : targetIcon != null ? (RectTransform)targetIcon.transform : null;
        if (prefab != null && anchor != null) _hud.Ui.PlayVfx(prefab, anchor);
    }

    private static string AwakenFailMessage(int fail) => fail switch {
        2 => "재료가 부족합니다.",
        3 => "다이아가 부족합니다.",
        4 => "이미 최대 각성 상태입니다.",
        _ => "각성할 수 없습니다."
    };

    private void ClearCards() {
        if (materialGrid != null)
            for (int i = materialGrid.childCount - 1; i >= 0; i--) {
                var child = materialGrid.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        _cards.Clear();
    }

    private void OnDisable() => _buildCts?.Cancel();

    private void OnDestroy() {
        _buildCts?.Cancel();
        _buildCts?.Dispose();
        _buildCts = null;
    }
}
