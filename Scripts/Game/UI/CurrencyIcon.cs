using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// Image 에 재화 아이콘을 표시하는 공용 헬퍼. 재화 카탈로그(CurrencyCatalog SO)에서 타입으로 스프라이트를 조회해
/// 팝업마다 아이콘을 개별 할당하지 않게 한다(같은 재화를 쓰는 UI 간 자동 일관, 변경 시 카탈로그 한 곳만 수정).
/// 스프라이트가 없으면 이미지를 숨긴다.
/// </summary>
public static class CurrencyIcon {
    /// <summary>
    /// 실물 결제(IAP) 가격의 재화 키. 지갑에 쌓이는 재화가 아니라 가격 단위라 CurrencyType 에 넣지 않고
    /// 상점 이미지 카탈로그(ShopIconCatalog)의 키로 아이콘을 찾는다.
    /// </summary>
    public const string CashKey = "cash";
    public const string CashIconKey = "price_cash";

    public static void Apply(Image image, CurrencyCatalog catalog, CurrencyType type) {
        if (image == null) return;
        var sprite = catalog != null ? catalog.Icon(type) : null;
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    /// <summary>
    /// 서버 재화 키로 아이콘을 찾는다(상품 가격 표시용). 지갑 재화는 CurrencyCatalog,
    /// 실물 결제는 ShopIconCatalog 에서 가져오므로 호출측이 키마다 분기하지 않아도 된다.
    /// </summary>
    public static Sprite ForKey(BattleHudContext hud, string key) {
        if (hud == null) return null;
        if (key == CashKey) return hud.ShopIcons != null ? hud.ShopIcons.Get(CashIconKey) : null;

        var type = CurrencyKeys.ToType(key);
        return type.HasValue && hud.Currencies != null ? hud.Currencies.Icon(type.Value) : null;
    }
}
