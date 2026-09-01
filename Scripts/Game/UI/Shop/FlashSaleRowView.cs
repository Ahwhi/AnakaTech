using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 한정 판매 안내 팝업의 상품 1줄. 이름/가격/구성품/남은 시간을 보여 주고, 누르면 상점의 해당 자리로 보낸다.
///
/// 여기서는 결제하지 않는다. 상품을 파는 자리는 상점 하나로 유지해야 결제 검증/한도/영수증 처리가
/// 한 경로에만 있게 되기 때문이다. 이 줄은 "이런 것이 열렸다"는 안내이고, 구매는 상점에서 한다.
/// </summary>
public sealed class FlashSaleRowView : MonoBehaviour {
    [Tooltip("상품 이름. 서버 카탈로그의 이름 색(NameColorHex)이 함께 적용된다.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("가격 표시(예: 4,900원 / 500). 선택.")]
    [SerializeField] private TMP_Text priceText;
    [Tooltip("결제 재화 아이콘. 현금 상품에서는 꺼진다. 선택.")]
    [SerializeField] private Image currencyIcon;
    [Tooltip("상품 아이콘. 상점 카드와 같은 그림(ShopIconCatalog 의 ImageKey)을 쓴다. 선택.")]
    [SerializeField] private Image itemIcon;
    [Tooltip("남은 시간(예: 11:59:03). 매 프레임 갱신된다. 선택.")]
    [SerializeField] private TMP_Text remainingText;
    [Tooltip("이미 구매한 상품에 덮는 표시. 선택.")]
    [SerializeField] private GameObject soldOutOverlay;
    [Tooltip("구성품 슬롯이 생성될 부모. 비우면 구성품을 그리지 않는다.")]
    [SerializeField] private Transform grantRoot;
    [Tooltip("구성품 슬롯 프리팹(보상 표시 공용).")]
    [SerializeField] private RewardSlotView grantSlotPrefab;
    [Tooltip("이 줄을 눌렀을 때 상점으로 이동하는 버튼.")]
    [SerializeField] private Button moveToShopButton;

    private readonly List<RewardSlotView> _slots = new();
    private long _endUnix;

    /// <param name="offer">상점 카탈로그의 상품(이름/가격/구성품의 단일 출처).</param>
    /// <param name="sale">이 계정에 열린 기간.</param>
    public void Bind(BattleHudContext hud, ShopOfferDto offer, FlashSaleDto sale, Action onMoveToShop) {
        _endUnix = sale != null ? sale.EndUnix : 0;

        if (nameText != null) {
            nameText.text = offer.DisplayName;
            if (ColorUtility.TryParseHtmlString(offer.NameColorHex, out var color)) nameText.color = color;
        }
        if (itemIcon != null) {
            var sprite = ItemSpriteFor(hud, offer);
            itemIcon.sprite = sprite;
            itemIcon.enabled = sprite != null;
        }
        // 가격 표기는 상점 카드(ShopItemCard)와 같은 규칙을 쓴다. 같은 상품이 두 화면에서 다르게 보이면 안 된다.
        if (priceText != null) priceText.text = offer.Price.ToString("N0");
        if (currencyIcon != null) {
            var sprite = CurrencyIcon.ForKey(hud, offer.Currency);
            currencyIcon.sprite = sprite;
            currencyIcon.enabled = sprite != null;
        }
        if (soldOutOverlay != null) soldOutOverlay.SetActive(offer.SoldOut);

        BuildGrants(hud, offer.Grants);

        if (moveToShopButton != null) {
            moveToShopButton.onClick.RemoveAllListeners();
            moveToShopButton.onClick.AddListener(() => onMoveToShop?.Invoke());
        }
        RefreshRemaining();
    }

    // 상품 그림. 전용 아이콘이 없으면 구성품이 하나뿐인 상품에 한해 그 품목의 그림을 쓴다
    // (상점 카드 ShopOfferPage.ItemSpriteFor 와 같은 규칙이라 두 화면의 그림이 어긋나지 않는다).
    private static Sprite ItemSpriteFor(BattleHudContext hud, ShopOfferDto offer) {
        var sprite = hud != null && hud.ShopIcons != null ? hud.ShopIcons.Get(offer.ImageKey) : null;
        if (sprite != null) return sprite;

        if (offer.Grants != null && offer.Grants.Count == 1)
            return RewardVisualResolver.Resolve(hud, offer.Grants[0]).Icon;
        return null;
    }

    private void BuildGrants(BattleHudContext hud, List<RewardItem> grants) {
        ClearGrants();
        if (grantRoot == null || grantSlotPrefab == null || grants == null) return;

        foreach (var g in grants) {
            var slot = Instantiate(grantSlotPrefab, grantRoot);
            slot.Bind(RewardVisualResolver.Resolve(hud, g));
            _slots.Add(slot);
        }
    }

    // 남은 시간은 초 단위로 줄어드는 값이라 매 프레임 갱신한다(팝업이 떠 있는 동안만 도는 비용이다).
    private void Update() => RefreshRemaining();

    private void RefreshRemaining() {
        if (remainingText == null || _endUnix <= 0) return;

        long left = _endUnix - KstTime.NowUnix;
        if (left < 0) left = 0;
        var span = TimeSpan.FromSeconds(left);
        remainingText.text = $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
    }

    private void ClearGrants() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();
    }

    private void OnDestroy() => ClearGrants();
}
