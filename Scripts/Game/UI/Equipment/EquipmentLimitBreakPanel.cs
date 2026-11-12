using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 장비 한계돌파 패널. EquipmentPopup 내부에서 켜고 끄는 패널이다(각성 패널과 같은 자리/분담).
/// 조건 충족 현황(한계돌파권/별레벨/레벨)과 돌파 전후 스탯(전용무기는 전용무기 효과까지)을 보여 주고,
/// 한계돌파 요청까지 이 패널이 맡는다. 상위는 성공 알림을 받아 인벤토리를 다시 받기만 하면 된다.
///
/// 조건 텍스트는 "현재 / 필요" 형태이며 충족하면 초록, 미충족이면 빨강이다(캐릭터 한계돌파 패널과 같은 규칙).
/// 판정은 서버 권위이고, 여기 판정(EquipmentRules.CanLimitBreak)은 버튼/색 표시용 미러다.
/// </summary>
public sealed class EquipmentLimitBreakPanel : MonoBehaviour {
    [Header("Target (대상)")]
    [SerializeField] private Image targetIcon;
    [SerializeField] private TMP_Text targetNameText;
    [SerializeField] private GradeText targetGradeText;
    [SerializeField] private StarLevelView targetStarView;
    [Tooltip("대상 레벨(예: Lv.150 / 150). 상세 패널과 같은 형식. 선택.")]
    [SerializeField] private TMP_Text targetLevelText;
    [Tooltip("한계돌파 성공 VFX 를 띄울 기준 위치. 비우면 targetIcon 을 쓴다. 선택.")]
    [SerializeField] private RectTransform vfxAnchor;

    [Header("Requirements")]
    [Tooltip("한계돌파권 아이콘. 재화 카탈로그(CurrencyCatalog)에서 자동으로 찾아 표시한다.")]
    [SerializeField] private Image ticketIcon;
    [Tooltip("선택: 한계돌파권 재화 위젯(아이콘을 누르면 설명 팝업). CurrencyPanel 밖에 두므로 이 패널이 초기화한다.")]
    [SerializeField] private CurrencyView ticketView;
    [Tooltip("한계돌파권 '보유 / 필요'.")]
    [SerializeField] private TMP_Text ticketCountText;
    [Tooltip("별레벨 '현재 / 필요'.")]
    [SerializeField] private TMP_Text starText;
    [Tooltip("레벨 '현재 / 필요'.")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Color metColor = new(0.25f, 0.8f, 0.35f);
    [SerializeField] private Color unmetColor = Color.red;

    [Header("Stats (돌파 전 / 후)")]
    [Tooltip("지금 스탯 목록(상세 패널과 같은 문구).")]
    [SerializeField] private TMP_Text beforeStatsText;
    [Tooltip("한계돌파 후 스탯 목록(배수 반영).")]
    [SerializeField] private TMP_Text afterStatsText;
    [Tooltip("전용무기일 때 두 스탯 목록 맨 위에 붙는 전용무기 효과 줄의 색. 텍스트에 Rich Text 가 켜져 있어야 한다.")]
    [SerializeField] private Color signatureLineColor = new(1f, 0.85f, 0.2f);

    [Header("Signature Weapon (전용무기만)")]
    [Tooltip("전용무기 효과 비교 그룹. 전용무기가 아니면 숨긴다(방어구 등은 스탯만 오른다).")]
    [SerializeField] private GameObject signatureGroup;
    [Tooltip("지금 전용무기 효과.")]
    [SerializeField] private TMP_Text beforeSignatureText;
    [Tooltip("한계돌파 후 전용무기 효과(배수 반영).")]
    [SerializeField] private TMP_Text afterSignatureText;

    [Header("State Groups")]
    [Tooltip("한계돌파 전에만 켜는 그룹(조건/버튼 등). 돌파하면 꺼진다. 선택.")]
    [SerializeField] private GameObject unlimitGroup;
    [Tooltip("한계돌파한 장비일 때만 켜는 그룹(완료 표시 등). 선택.")]
    [SerializeField] private GameObject completedGroup;

    [Header("Actions")]
    [SerializeField] private Button limitBreakButton;
    [SerializeField] private Button closeButton;
    [Tooltip("배경 탭으로 닫기(선택). 패널 뒤를 덮는 배경 이미지(보통 Dim)를 그대로 끌어다 놓으면 그 바깥을 눌러 닫을 수 있다. " +
             "클릭 처리는 코드가 붙이므로 Button 컴포넌트를 따로 달 필요가 없다. 비워 두면 배경 탭으로 닫히지 않는다(할당 = 사용).")]
    [SerializeField] private Graphic tapToCloseArea;

    /// <summary>한계돌파가 성공해 장비 상태가 바뀌었음(상위가 인벤토리를 다시 받는다).</summary>
    public event Action LimitBroken;
    /// <summary>패널 닫기 요청.</summary>
    public event Action CloseRequested;

    // 한계돌파 성공 연출 프리팹 조회 키(UIVfxCatalog 의 Named 항목). 각성의 "equip_awaken" 과 같은 규약.
    private const string LimitBreakVfxKey = "equip_limitbreak";

    private MainHudContext _hud;
    private long _targetId;
    private CancellationToken _ct;
    private bool _busy;
    private bool _wired;

    public void Initialize(MainHudContext hud, long targetInstanceId) {
        _hud = hud;
        _targetId = targetInstanceId;

        if (!_wired) {
            _ct = this.GetCancellationTokenOnDestroy();
            if (limitBreakButton != null)
                limitBreakButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); LimitBreakAsync().Forget(); });
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseRequested?.Invoke(); });
            // 배경 탭도 닫기 버튼과 같은 결과를 낸다(팝업들과 조작이 어긋나지 않게).
            TapToClose.Bind(tapToCloseArea, () => {
                _hud.Sound.PlaySfx(SfxKey.ButtonClick);
                CloseRequested?.Invoke();
            });
            // 한계돌파권은 이 패널 밖에서도 바뀐다(상점 구매/우편 수령). 보유량과 버튼이 바로 따라오게 한다.
            _hud.Store.LimitBreakTicket.Subscribe(_ => { if (isActiveAndEnabled) Refresh(); }).AddTo(this);
            _wired = true;
        }

        CurrencyIcon.Apply(ticketIcon, _hud.Currencies, CurrencyType.LimitBreakTicket);
        // CurrencyView 는 리스너를 지우고 다시 거므로 대상을 바꿔 다시 불러도 중복되지 않는다.
        if (ticketView != null) ticketView.Initialize(_hud);
        Refresh();
    }

    private EquipmentInstanceDto Target =>
        _hud != null && _hud.Store.Equipment.TryGetValue(_targetId, out var t) ? t : null;

    /// <summary>대상/조건/버튼을 다시 그린다. 인벤토리 재동기화 후 상위가 호출한다(대상이 사라졌으면 닫힌다).</summary>
    public void Refresh() {
        var inst = Target;
        var data = inst != null ? _hud.EquipDb.Get(inst.EquipDataId) : null;
        if (inst == null || data == null) { CloseRequested?.Invoke(); return; }

        if (targetIcon != null) {
            targetIcon.sprite = data.Icon;
            targetIcon.enabled = data.Icon != null;
        }
        if (targetNameText != null) targetNameText.text = data.DisplayName;
        if (targetGradeText != null) targetGradeText.SetGrade(data.Grade, inst.LimitBroken);
        if (targetStarView != null) targetStarView.SetStarLevel(inst.StarLevel);
        if (targetLevelText != null) targetLevelText.text = $"Lv.{inst.Level} / {EquipmentRules.MaxLevel(inst.StarLevel)}";

        int tickets = _hud.Store.LimitBreakTicket.Value;
        SetRequirement(ticketCountText, $"{tickets:N0} / {EquipmentRules.LimitBreakTicketCost:N0}",
                       EquipmentRules.IsLimitBreakTicketMet(tickets));
        SetRequirement(starText, $"{inst.StarLevel} / {EquipmentRules.LimitBreakRequiredStar}",
                       EquipmentRules.IsLimitBreakStarMet(inst));
        SetRequirement(levelText, $"{inst.Level:N0} / {EquipmentRules.LimitBreakRequiredLevel:N0}",
                       EquipmentRules.IsLimitBreakLevelMet(inst));

        // 돌파 전에는 "지금 -> 돌파 후" 를, 돌파 후에는 양쪽 모두 지금(돌파 반영) 값을 보여 준다.
        // 전용무기는 전용무기 효과를 색을 달리해 목록 맨 위에 붙인다(스탯과 같은 배수로 오르는 것을 한눈에 보이게).
        bool signature = data.IsSignature;
        if (beforeStatsText != null)
            beforeStatsText.text = WithSignatureLine(EquipmentRules.DescribeStats(inst, data), data, inst.LimitBroken);
        if (afterStatsText != null)
            afterStatsText.text = WithSignatureLine(EquipmentRules.DescribeLimitBrokenStats(inst, data), data, limitBroken: true);

        if (signatureGroup != null) signatureGroup.SetActive(signature);
        if (signature) {
            if (beforeSignatureText != null)
                beforeSignatureText.text = EquipmentRules.SignatureBonusDescription(data.Grade, data.WeaponType, data.Id, inst.LimitBroken);
            if (afterSignatureText != null)
                afterSignatureText.text = EquipmentRules.SignatureBonusDescription(data.Grade, data.WeaponType, data.Id, limitBroken: true);
        }

        if (unlimitGroup != null) unlimitGroup.SetActive(!inst.LimitBroken);
        if (completedGroup != null) completedGroup.SetActive(inst.LimitBroken);
        if (limitBreakButton != null)
            limitBreakButton.interactable = !_busy && EquipmentRules.CanLimitBreak(inst, tickets);
    }

    private const string SignatureLineTitle = "(전용 무기 효과)";

    // 전용무기면 "(전용 무기 효과)\n효과" 를 지정 색으로 스탯 목록 맨 위에 붙인다. 전용무기가 아니면 목록 그대로.
    private string WithSignatureLine(string stats, EquipmentData data, bool limitBroken) {
        if (!data.IsSignature) return stats;
        string effect = EquipmentRules.SignatureBonusDescription(data.Grade, data.WeaponType, data.Id, limitBroken);
        string hex = ColorUtility.ToHtmlStringRGBA(signatureLineColor);
        return $"<color=#{hex}>{SignatureLineTitle}\n{effect}</color>\n{stats}";
    }

    private void SetRequirement(TMP_Text text, string value, bool met) {
        if (text == null) return;
        text.text = value;
        text.color = met ? metColor : unmetColor;
    }

    private async UniTaskVoid LimitBreakAsync() {
        var inst = Target;
        if (_busy || inst == null || !EquipmentRules.CanLimitBreak(inst, _hud.Store.LimitBreakTicket.Value)) return;

        _busy = true;
        Refresh();   // 응답을 기다리는 동안 버튼을 막는다(연타로 요청이 겹치지 않게).
        try {
            var res = await _hud.Inventory.LimitBreakAsync(_targetId, _ct);
            if (res.IsSuccess) {
                _hud.Store.LimitBreakTicket.Value = res.NewLimitBreakTicket;   // 소모 후 서버 확정 보유량
                PlayLimitBreakVfx();
                LimitBroken?.Invoke();   // 상위가 인벤토리를 다시 받아 돌파 상태를 반영한다.
            } else {
                _hud.Ui.ShowToast(EquipmentRules.LimitBreakFailMessage(res.FailReason));
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("한계돌파에 실패했습니다.");
        } finally {
            _busy = false;
            if (this != null && isActiveAndEnabled) Refresh();
        }
    }

    private void PlayLimitBreakVfx() {
        // 소리는 VFX 배선 여부와 무관하게 낸다(캐릭터 한계돌파와 같은 효과음).
        _hud.Sound.PlaySfx(SfxKey.StarUp);
        if (_hud.UiVfx == null) return;
        var prefab = _hud.UiVfx.Get(LimitBreakVfxKey);
        var anchor = vfxAnchor != null ? vfxAnchor : targetIcon != null ? (RectTransform)targetIcon.transform : null;
        if (prefab != null && anchor != null) _hud.Ui.PlayVfx(prefab, anchor);
    }
}
