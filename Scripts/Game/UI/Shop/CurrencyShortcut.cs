using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 재화 부족 시 구매 유도 공용 헬퍼. "~가 부족합니다. 구매하시겠습니까?" 확인 후 상점의 해당 재화 페이지로 이동한다.
/// 다이아/골드는 재화 페이지로, 그 외(티켓 등)는 호출부가 전용 구매 팝업을 열도록 여기서 처리하지 않는다(false 성격).
/// 상점 밖(가챠/강화 등)에서 부족이 발생하면 이 헬퍼로 상점을 새로 열어 해당 페이지로 진입한다.
/// </summary>
public static class CurrencyShortcut {
    public static string DisplayName(string currency) => currency switch {
        "gold" => "골드",
        "diamond" => "다이아",
        "stamina" => "기력",
        CurrencyKeys.NormalTicket => "일반 소환권",
        CurrencyKeys.SpecialTicket => "특수 소환권",
        CurrencyKeys.NicknameTicket => "닉네임 변경권",
        CurrencyKeys.DispatchCertificate => "파견 증명서",
        _ => "재화"
    };

    /// <summary>다이아/골드 부족 유도: 확인 팝업 후 상점을 열어 해당 재화 페이지로 진입한다. currency 는 "diamond"/"gold".</summary>
    public static async UniTask RouteToShopAsync(MainHudContext hud, string currency, CancellationToken ct) {
        string sub = currency == "diamond" ? "diamond" : currency == "gold" ? "gold" : null;
        if (sub == null) return;

        var shopRef = hud.Popups.ShopPopup;
        var confirmRef = hud.Popups.ConfirmCancelPopup;
        if (shopRef == null || !shopRef.RuntimeKeyIsValid()) return;

        if (confirmRef != null && confirmRef.RuntimeKeyIsValid()) {
            string name = DisplayName(currency);
            var confirm = await hud.Ui.ShowPopupAsync<ConfirmCancelPopup>(confirmRef, ct);
            bool yes = await confirm.ShowAsync($"{name} 부족", $"{name}가 부족합니다. 구매하시겠습니까?");
            if (!yes) return;
        }

        var shop = await hud.Ui.ShowPopupAsync<ShopPopup>(shopRef, ct);
        shop.SetEntry("currency", sub);
        shop.Initialize(hud);
        await shop.ShowAsync();
    }
}
