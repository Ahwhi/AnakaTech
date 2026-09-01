using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 클리어 획득 표시(PLANNING 7). 다른 팝업을 가리지 않도록 Light 레이어(SortOrder 50)에 뜬다.
/// 페이드인 -> 잠깐 유지 -> 페이드아웃으로 자동으로 닫히며, 입력을 가로채지 않는 비차단 연출이라
/// 위에 떠 있는 일반 팝업(SortOrder 100)을 가리지도, 그 조작을 막지도 않는다.
///
/// 획득량은 던전 선택 팝업과 같은 공용 경로(RewardVisualResolver + RewardSlotView)로 "아이콘 + 수량" 표시한다.
/// 기력 부스트로 여러 회분이 한 번에 정산된 클리어면 배수 그룹(boostGroup)을 함께 켠다.
/// </summary>
public sealed class DungeonClearRewardPopup : UIView {
    [Header("Reward")]
    [Tooltip("경험치 획득 슬롯(아이콘 + 수량). 아이콘은 ShopIconCatalog 의 reward_exp 키에서 온다.")]
    [SerializeField] private RewardSlotView expSlot;
    [Tooltip("골드 획득 슬롯(아이콘 + 수량). 아이콘은 CurrencyCatalog 의 Gold 에서 온다.")]
    [SerializeField] private RewardSlotView goldSlot;

    [Header("Boost (기력 부스트로 정산된 클리어에서만 노출)")]
    [Tooltip("부스트 정산일 때만 켤 오브젝트(배지/테두리/문구 묶음). 비우면 부스트 표시가 없다. " +
             "위 슬롯의 수량에는 배수가 이미 적용되어 있으므로, 이 그룹은 '왜 많이 들어왔는지'를 알려 주는 역할이다.")]
    [SerializeField] private GameObject boostGroup;
    [Tooltip("선택. 적용된 배수 표시(예: x10). boostGroup 하위에 둔다. 배수는 서버가 정산한 값을 그대로 쓴다.")]
    [SerializeField] private TMP_Text boostMultiplierText;
    [Tooltip("선택. 부스트권 아이콘. 카탈로그에서 자동으로 채운다. boostGroup 하위에 둔다.")]
    [SerializeField] private Image boostIcon;

    [Header("Stamina (기력이 모자란 채로 클리어했을 때만 노출)")]
    [Tooltip("기력 부족으로 보상이 깎였을 때만 켤 안내. 비우면 표시하지 않는다. " +
             "전투는 멈추지 않으므로, 보상이 왜 적은지는 이 안내로만 알 수 있다.")]
    [SerializeField] private TMP_Text staminaShortText;
    [Tooltip("위 안내에 넣을 문구.")]
    [SerializeField] private string staminaShortMessage = "기력이 부족하여 보상을 적게 받았습니다.";

    [Tooltip("페이드인 후 유지 시간(초). 이후 자동으로 페이드아웃되어 닫힌다.")]
    [SerializeField] private float holdSeconds = 1.2f;

    public void Setup(MainHudContext hud, DungeonRewardDto dto) {
        if (dto == null) return;

        // 덱 참여 캐릭터는 서버가 모두 같은 경험치를 주므로 대표로 첫 항목을 표시한다.
        int exp = dto.CharExp.Count > 0 ? dto.CharExp[0].GainedExp : 0;

        BindGain(expSlot, hud, RewardItem.Exp(exp), exp);
        BindGain(goldSlot, hud, RewardItem.Gold(dto.GainedGold), dto.GainedGold);

        BindBoost(hud, dto.ClearCount);

        // 기력이 모자라도 전투는 계속되므로, 보상이 적은 이유를 여기서 알려 주지 않으면 유저가 알 길이 없다.
        if (staminaShortText != null) {
            staminaShortText.gameObject.SetActive(dto.StaminaShort);
            if (dto.StaminaShort) staminaShortText.text = staminaShortMessage;
        }
    }

    // 부스트 정산이면 배수 표시를 켠다. 판정 기준은 서버가 내려준 정산 회수뿐이고 클라의 부스트 모드 상태는
    // 보지 않는다. 보상이 도착하기 전에 모드가 꺼질 수 있으므로(자원 소진/유저 조작), 이 보상이 실제로
    // 몇 회분으로 정산됐는지는 서버 값만이 알고 있다.
    private void BindBoost(MainHudContext hud, int clearCount) {
        bool boosted = clearCount > 1;

        if (boostGroup != null) boostGroup.SetActive(boosted);
        if (!boosted) return;

        if (boostMultiplierText != null) boostMultiplierText.text = $"x{clearCount}";
        CurrencyIcon.Apply(boostIcon, hud != null ? hud.Currencies : null, CurrencyType.StaminaBoostTicket);
    }

    // 아이콘은 공용 해석기 결과를 그대로 쓰고, 수량만 획득 표시(+)로 바꿔 넘긴다.
    private static void BindGain(RewardSlotView slot, MainHudContext hud, RewardItem item, int amount) {
        if (slot == null) return;
        var v = RewardVisualResolver.Resolve(hud, item);
        slot.Bind(new RewardSlotView.RewardVisual(v.Icon, $"+{amount:N0}", v.Name));
    }

    /// <summary>페이드인 -> 유지 -> 페이드아웃 자동 닫힘. Setup 이후 호출한다.</summary>
    public async UniTask PlayAsync(CancellationToken ct = default) {
        await ShowAsync();

        // 다른 팝업/HUD 입력을 가리지 않도록 표시 후 비차단으로 되돌린다(ShowAsync 가 켜므로).
        CanvasGroup.blocksRaycasts = false;
        CanvasGroup.interactable = false;

        try {
            await UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), ignoreTimeScale: true, cancellationToken: ct);
        } catch (OperationCanceledException) {
            return;   // 씬 종료 등으로 취소되면 정리는 상위(파괴/일괄 닫힘)에 맡긴다.
        }

        if (this == null) return;   // 새 보상으로 교체되어 이미 닫혀 파괴된 경우.
        await CloseAsync();
    }
}
