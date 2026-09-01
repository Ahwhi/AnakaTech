using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 패키지/세트 상품 전용 카드(스텝업, 한정 최적화 세트 등 구성품이 여러 개인 상품 공용).
/// 공용 ShopItemCard(이미지/이름/가격/재화/오버레이/선택프레임)를 그대로 물려받고,
/// 이 상품에 포함된 구성품(RewardItem)을 공용 RewardSlotView 슬롯으로 추가 표시한다.
/// 즉시 지급분은 grantRoot 에, 반복 지급분(예: 매일 접속 보상)은 선택적 recurringGroup 에 헤더와 함께 표시한다.
/// 구성품 데이터는 클라 미러(ShopPackageRewards)에서 OfferId 로 조회해 상위(ShopOfferPage)가 넘겨준다.
/// 표시는 RewardVisualResolver 가 카탈로그로 해석하므로, 품목이 늘어도 이 카드는 수정할 필요가 없다.
/// </summary>
public sealed class PackageItemCard : ShopItemCard {
    [Header("Package Grants")]
    [Tooltip("즉시 지급 구성품 슬롯을 생성할 부모(스폰 전용 오브젝트).")]
    [SerializeField] private Transform grantRoot;
    [Tooltip("구성품 1개를 표시할 공용 슬롯 프리팹(RewardSlotView).")]
    [SerializeField] private RewardSlotView grantSlotPrefab;

    [Header("Recurring (선택: 매일 지급 등)")]
    [Tooltip("반복 지급 구성품 그룹 루트. 반복 지급이 있는 오퍼일 때만 켠다. 없으면 비워 둔다.")]
    [SerializeField] private GameObject recurringGroup;
    [Tooltip("반복 지급 그룹 헤더 텍스트(예: '매일 지급'). 문구는 미러(RecurringLabel)가 정한다.")]
    [SerializeField] private TMP_Text recurringLabel;
    [Tooltip("반복 지급 구성품 슬롯을 생성할 부모(recurringGroup 자식).")]
    [SerializeField] private Transform recurringGrantRoot;

    private readonly List<RewardSlotView> _slots = new();

    /// <summary>이 카드에 상품 구성품을 채운다. 공용 필드는 Bind 로, 구성품은 이 메서드로 나눠 채운다.</summary>
    public void BindPackage(MainHudContext hud, ShopPackageRewards.PackageInfo info) {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();

        if (info == null) {
            if (recurringGroup != null) recurringGroup.SetActive(false);
            return;
        }

        SpawnSlots(hud, info.Grants, grantRoot);

        bool hasRecurring = info.Recurring != null && info.Recurring.Count > 0;
        if (recurringGroup != null) recurringGroup.SetActive(hasRecurring);
        if (hasRecurring) {
            if (recurringLabel != null && !string.IsNullOrEmpty(info.RecurringLabel))
                recurringLabel.text = info.RecurringLabel;
            SpawnSlots(hud, info.Recurring, recurringGrantRoot);
        }
    }

    private void SpawnSlots(MainHudContext hud, IReadOnlyList<RewardItem> items, Transform parent) {
        if (parent == null || grantSlotPrefab == null || items == null) return;
        foreach (var item in items) {
            var slot = Instantiate(grantSlotPrefab, parent);
            slot.Bind(RewardVisualResolver.Resolve(hud, item));
            _slots.Add(slot);
        }
    }
}
