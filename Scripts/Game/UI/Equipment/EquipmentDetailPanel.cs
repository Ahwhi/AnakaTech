using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장비 상세 패널. 착용 슬롯을 눌렀을 때와 가방 카드를 눌렀을 때 모두 이 패널 하나를 재사용한다.
/// 표시: 아이콘/이름/등급/별레벨/레벨 + 스탯 목록 + 전용무기 보너스 그룹 + (미착용 시) 전투력 변화량.
/// 동작(장착/해제/각성/레벨업)은 이벤트로 상위(EquipmentPopup)에 위임한다. 패널은 표시만 책임진다.
/// </summary>
public sealed class EquipmentDetailPanel : MonoBehaviour {
    [Header("Identity")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GradeText gradeText;
    [Tooltip("각성 단계(별) 표시. 캐릭터와 동일한 공용 컴포넌트를 재사용한다.")]
    [SerializeField] private StarLevelView starView;
    [Tooltip("레벨 텍스트(예: Lv.24 / 40). 최대 레벨(별레벨 x 10)과 함께 표시한다.")]
    [SerializeField] private TMP_Text levelText;

    [Header("Stats")]
    [Tooltip("주스탯 + 옵션 목록. EquipmentRules.DescribeStats 결과를 그대로 표시한다.")]
    [SerializeField] private TMP_Text statsText;

    [Header("Signature Weapon (전용무기)")]
    [Tooltip("전용무기 보너스 그룹 전체. 무기가 아닌 장비를 선택하면 숨긴다.")]
    [SerializeField] private GameObject signatureGroup;
    [Tooltip("전용무기 장착 여부를 색으로 보여주는 이미지. 본인 전용무기 장착 중이면 초록, 아니면 회색.")]
    [SerializeField] private Image signatureStateImage;
    [Tooltip("전용무기 효과 문구(예: '최종데미지 +20%'). 미장착이면 안내 문구를 함께 표시한다.")]
    [SerializeField] private TMP_Text signatureEffectText;
    [SerializeField] private Color signatureActiveColor = new Color(0.29f, 0.78f, 0.35f);
    [SerializeField] private Color signatureInactiveColor = new Color(0.55f, 0.55f, 0.55f);

    [Header("Combat Power Delta")]
    [Tooltip("장착 시 전투력 변화량(예: +324). 이 캐릭터가 착용 중인 장비면 '착용 중'을 표시한다.")]
    [SerializeField] private TMP_Text powerDeltaText;
    [SerializeField] private Color powerUpColor = new Color(0.29f, 0.78f, 0.35f);
    [SerializeField] private Color powerDownColor = new Color(0.85f, 0.27f, 0.27f);
    [Tooltip("'착용 중' 표시 색(증감이 아니므로 중립색).")]
    [SerializeField] private Color equippedColor = Color.white;

    [Header("VFX")]
    [Tooltip("강화(레벨업) 성공 VFX 를 띄울 기준 위치. 비우면 아이콘, 그것도 없으면 이 패널 루트에 재생한다. 선택.")]
    [SerializeField] private RectTransform vfxAnchor;

    [Header("Actions")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button awakenButton;
    [SerializeField] private Button levelUpButton;
    [Tooltip("레벨업 버튼에 표시할 필요 골드(예: 300). 선택.")]
    [SerializeField] private TMP_Text levelUpCostText;
    [Tooltip("레벨업 필요 골드 아이콘. 재화 카탈로그에서 자동 배선된다. 선택.")]
    [SerializeField] private Image levelUpCostIcon;
    [Tooltip("다음 레벨업으로 오르는 스탯 미리보기(예: '공격력 +1'). 상한에 닿으면 각성 안내로 바뀐다. 선택.")]
    [SerializeField] private TMP_Text levelUpGainText;
    [SerializeField] private Button closeButton;
    [Tooltip("배경 탭으로 닫기(선택). 패널 뒤를 덮는 배경 이미지(보통 Dim)를 그대로 끌어다 놓으면 그 바깥을 눌러 닫을 수 있다. " +
             "클릭 처리는 코드가 붙이므로 Button 컴포넌트를 따로 달 필요가 없다. 비워 두면 배경 탭으로 닫히지 않는다(할당 = 사용).")]
    [SerializeField] private Graphic tapToCloseArea;

    /// <summary>장착 요청.</summary>
    public event Action EquipRequested;
    /// <summary>해제 요청.</summary>
    public event Action UnequipRequested;
    /// <summary>각성 패널 열기 요청.</summary>
    public event Action AwakenRequested;
    /// <summary>레벨업 요청.</summary>
    public event Action LevelUpRequested;
    /// <summary>패널 닫기 요청.</summary>
    public event Action CloseRequested;

    // 강화 성공 연출 프리팹 조회 키(UIVfxCatalog 의 Named 항목). 각성의 "equip_awaken" 과 같은 규약.
    private const string LevelUpVfxKey = "equip_levelup";

    private MainHudContext _hud;
    private int _charId;
    private bool _wired;

    public EquipmentInstanceDto Inst { get; private set; }

    public void Initialize(MainHudContext hud, int charDataId) {
        _hud = hud;
        _charId = charDataId;

        // 리스너는 최초 1회만(패널 재사용이라 중복 등록 방지).
        if (_wired) return;
        Wire(equipButton, () => EquipRequested?.Invoke());
        Wire(unequipButton, () => UnequipRequested?.Invoke());
        Wire(awakenButton, () => AwakenRequested?.Invoke());
        Wire(levelUpButton, () => LevelUpRequested?.Invoke());
        Wire(closeButton, () => CloseRequested?.Invoke());
        // 배경 탭도 닫기 버튼과 같은 결과를 낸다(팝업들과 조작이 어긋나지 않게).
        TapToClose.Bind(tapToCloseArea, () => {
            _hud.Sound.PlaySfx(Enums.SfxKey.ButtonClick);
            CloseRequested?.Invoke();
        });
        CurrencyIcon.Apply(levelUpCostIcon, _hud.Currencies, CurrencyType.Gold);

        // 골드 보유량이 밖에서 바뀌어도(상점 구매/우편 수령 등) 비용 색과 버튼 활성이 즉시 따라오게 한다.
        _hud.Store.Gold.Subscribe(_ => OnGoldChanged()).AddTo(this);
        _wired = true;
    }

    // 골드 변동 알림. 표시 중인 장비 기준으로 다시 그린다(닫혀 있으면 할 일 없음).
    private void OnGoldChanged() {
        if (!gameObject.activeInHierarchy || Inst == null) return;
        Bind(Inst);
    }

    private void Wire(Button b, Action action) {
        if (b == null) return;
        b.onClick.AddListener(() => {
            _hud.Sound.PlaySfx(Enums.SfxKey.ButtonClick);
            action();
        });
    }

    /// <summary>선택 장비를 표시한다. null 이면 아무것도 하지 않는다(상위가 패널을 닫는다).</summary>
    public void Bind(EquipmentInstanceDto inst) {
        Inst = inst;
        if (inst == null) return;

        var data = _hud.EquipDb.Get(inst.EquipDataId);
        if (data == null) return;

        bool equipped = inst.IsEquipped;
        bool equippedHere = inst.EquippedCharDataId == _charId;

        // ── 기본 표시 ──
        if (icon != null) {
            icon.sprite = data.Icon;
            icon.enabled = data.Icon != null;
        }
        if (gradeText != null) gradeText.SetGrade(data.Grade);
        if (starView != null) starView.SetStarLevel(inst.StarLevel);
        if (levelText != null) levelText.text = $"Lv.{inst.Level} / {EquipmentRules.MaxLevel(inst.StarLevel)}";
        if (statsText != null) statsText.text = EquipmentRules.DescribeStats(inst, data);

        // 이름: 장착 불가 무기면 사유를 덧붙인다.
        bool canEquip = CanEquipOnCurrentChar(data);
        if (nameText != null)
            nameText.text = data.DisplayName + (!equipped && !canEquip ? " (장착 불가)" : "");

        RefreshSignature(data, equippedHere);
        RefreshPowerDelta(inst, equippedHere);
        RefreshActions(inst, data, equipped, equippedHere, canEquip);
    }

    // 전용/공용 그룹: 무기는 전용무기 효과를, 무기가 아닌 부위(방어구/신발/악세서리)는
    // 캐릭터 제한이 없으므로 "공용"으로 표시한다(예전에는 그룹을 통째로 숨겨 아무 정보도 안 보였다).
    // 본인 전용무기를 "이 캐릭터가 장착 중"일 때만 초록(효과 발동 중).
    private void RefreshSignature(EquipmentData data, bool equippedHere) {
        bool isWeapon = data.Slot == EquipmentSlot.Weapon;
        bool show = !isWeapon || data.IsSignature;
        if (signatureGroup != null) signatureGroup.SetActive(show);
        if (!show) return;

        if (!isWeapon) {
            if (signatureStateImage != null) signatureStateImage.color = signatureInactiveColor;
            if (signatureEffectText != null) signatureEffectText.text = "공용";
            return;
        }

        bool own = data.IsOwnSignatureOf(_charId);
        bool active = own && equippedHere;

        if (signatureStateImage != null)
            signatureStateImage.color = active ? signatureActiveColor : signatureInactiveColor;

        if (signatureEffectText == null) return;
        string effect = EquipmentRules.SignatureBonusDescription(data.Grade, data.WeaponType, data.Id);
        if (active) signatureEffectText.text = $"적용 중\n{effect}";
        else if (own) signatureEffectText.text = $"장착 시 적용\n{effect}";
        else signatureEffectText.text = $"효과 없음";
    }

    // 이 캐릭터가 착용 중인 장비면 "착용 중"을, 아니면 장착 시 전투력 변화량을 표시한다.
    // 다른 캐릭터가 착용 중인 장비는 빼앗아 장착할 수 있으므로 변화량 쪽으로 보여준다.
    private void RefreshPowerDelta(EquipmentInstanceDto inst, bool equippedHere) {
        if (powerDeltaText == null) return;
        powerDeltaText.gameObject.SetActive(true);

        if (equippedHere) {
            powerDeltaText.text = "착용 중";
            powerDeltaText.color = equippedColor;
            return;
        }

        // 전투력이 곱연산이라 변화량은 캐릭터의 현재 빌드에 따라 달라진다(캐릭터 정적 데이터/튜닝값이 필요).
        float delta = _hud.Store.CombatPowerDeltaIfEquipped(
            _hud.CharDb.Get(_charId), inst, _hud.EquipDb, _hud.Settings);
        int rounded = Mathf.RoundToInt(delta);
        powerDeltaText.text = rounded >= 0 ? $"+{rounded}" : rounded.ToString();
        powerDeltaText.color = rounded >= 0 ? powerUpColor : powerDownColor;
    }

    private void RefreshActions(EquipmentInstanceDto inst, EquipmentData data,
                               bool equipped, bool equippedHere, bool canEquip) {
        // 장착: 미착용이거나 다른 캐릭터가 낀 장비(빼앗아 장착)일 때. 해제: 이 캐릭터가 낀 장비일 때.
        if (equipButton != null) {
            equipButton.gameObject.SetActive(!equippedHere);
            equipButton.interactable = canEquip;
        }
        if (unequipButton != null) unequipButton.gameObject.SetActive(equippedHere);

        // 각성: 최대 별레벨이면 막는다.
        if (awakenButton != null)
            awakenButton.interactable = EquipmentRules.CanAwaken(inst.StarLevel);

        // 레벨업: 별레벨이 열어준 상한에 닿으면 막고, 비용은 항상 현재 레벨 기준으로 표시한다.
        // 골드가 모자라도 버튼을 막는다(각성 패널과 같은 규칙. 눌러서 토스트로 거절당하지 않게).
        bool canLevel = EquipmentRules.CanLevelUp(inst.StarLevel, inst.Level);
        int levelUpCost = canLevel ? EquipmentRules.LevelUpGold(inst.Level) : 0;
        bool enoughGold = _hud.Store.Gold.Value >= levelUpCost;

        if (levelUpButton != null) levelUpButton.interactable = canLevel && enoughGold;
        if (levelUpCostText != null) {
            if (canLevel) {
                levelUpCostText.text = levelUpCost.ToString("N0");
                // 골드가 부족하면 빨갛게(구매 확인 팝업과 같은 관례).
                levelUpCostText.color = enoughGold ? powerUpColor : powerDownColor;
            } else {
                levelUpCostText.text = "-";
                levelUpCostText.color = Color.white;
            }
        }

        // 다음 레벨업 상승치 미리보기. 상한에 닿았으면 무엇을 해야 더 크는지(각성) 알려준다.
        if (levelUpGainText != null) {
            levelUpGainText.text = canLevel
                ? EquipmentRules.DescribeLevelUpGain(inst, data)
                : (EquipmentRules.CanAwaken(inst.StarLevel)
                    ? "각성하면 레벨을 더 올릴 수 있습니다."
                    : "최대 각성 / 최대 레벨");
        }
    }

    /// <summary>강화(레벨업) 성공 연출. 서버 성공을 확인한 상위(EquipmentPopup)가 호출한다.
    /// 패널은 "어떻게 보여줄지"만 알고, "성공했는지"는 통신을 담당하는 상위가 판단한다(각성 패널과 같은 분담).</summary>
    public void PlayLevelUpVfx() {
        if (_hud == null) return;

        // 소리는 VFX 배선 여부와 무관하게 낸다(연출 프리팹이 비어 있어도 강화된 느낌은 남아야 한다).
        _hud.Sound.PlaySfx(Enums.SfxKey.EquipLevelUp);
        if (_hud.UiVfx == null) return;

        var prefab = _hud.UiVfx.Get(LevelUpVfxKey);
        if (prefab == null) return;

        var anchor = vfxAnchor != null ? vfxAnchor
                   : (icon != null ? (RectTransform)icon.transform : (RectTransform)transform);
        if (anchor != null) _hud.Ui.PlayVfx(prefab, anchor);
    }

    private bool CanEquipOnCurrentChar(EquipmentData data) {
        var ch = _hud.CharDb != null ? _hud.CharDb.Get(_charId) : null;
        return ch == null || data.CanBeEquippedBy(ch.Type);
    }
}
