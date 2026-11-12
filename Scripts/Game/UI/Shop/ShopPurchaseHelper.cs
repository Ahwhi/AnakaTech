using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using static Enums;

/// <summary>
/// 상점 구매 실행 + 결과 반영(재화/가챠롤/장비롤 + 재화 동기화)의 공용 경로. 상점 페이지와 가챠 전용 팝업이 함께 쓴다.
/// 성공/실패 판정은 서버 권위이며, 실패 메시지/후속 유도(재화 부족 등)는 호출부가 Result.FailReason 으로 처리한다.
/// </summary>
public static class ShopPurchaseHelper {
    public struct BuyApplyResult {
        public BuyResultDto Result;     // 서버 구매 결과(실패 포함)
        public ShopCatalogDto Catalog;  // 성공 시 재동기화된 최신 카탈로그(없으면 null)
    }

    public static async UniTask<BuyApplyResult> BuyAndApplyAsync(MainHudContext hud, string offerId, int qty, CancellationToken ct,
                                                                bool free = false, string currency = null,
                                                                bool nonConsumable = false) {
        // 현금 상품은 스토어에서 먼저 결제하고, 그 영수증으로 서버에 지급을 요청한다.
        // 결제 수단은 호출부가 들고 있는 카탈로그 값을 그대로 받는다(여기서 다시 조회하지 않는다).
        if (!free && currency == CashCurrency && hud.Iap != null && hud.Iap.IsSupported)
            return await BuyCashAndApplyAsync(hud, new IapProduct(offerId, nonConsumable), qty, ct);

        string idempotencyKey = Guid.NewGuid().ToString("N");
        var res = await hud.Shop.BuyOfferAsync(offerId, idempotencyKey, qty, free, ct: ct);
        if (!res.IsSuccess) return new BuyApplyResult { Result = res, Catalog = null };

        return await ApplyAsync(hud, res, ct);
    }

    /// <summary>
    /// 현금 상품 구매. 순서가 중요하다.
    ///   스토어 결제 -> 서버 검증/지급 -> 결제 확정(소비)
    ///
    /// 확정을 마지막에 두는 것이 핵심이다. 확정하기 전의 결제는 스토어에 살아 있어 앱이 죽어도
    /// 다음 실행에서 다시 전달되므로, 중간에 무슨 일이 나도 "돈은 냈는데 못 받는" 상태로 끝나지 않는다.
    /// 서버가 실패를 돌려주면 확정하지 않고 그대로 둔다(다음 접속에서 복구가 다시 시도한다).
    /// </summary>
    private static async UniTask<BuyApplyResult> BuyCashAndApplyAsync(MainHudContext hud, IapProduct product, int qty, CancellationToken ct) {
        string offerId = product.Id;
        // 결제 창을 열기 전에 우리 규칙(결제 한도/구매 제한/판매 기간)을 먼저 확인한다.
        //
        // 이 확인이 없으면 스토어가 돈을 받은 뒤에야 서버가 거절하게 되고, 유저는 돈만 내고 아무것도
        // 받지 못한다. 게다가 그 결제는 소비되지 않은 채 스토어에 남아 같은 상품을 다시 살 수도 없다.
        //
        // 이 확인도 서버 왕복이라 누른 뒤 잠깐 멈춘 것처럼 보인다. 결제 창이 뜨기 전까지 로딩으로 덮어
        // "눌리긴 했다"를 알린다. 결과를 안내(토스트)하기 전에 닫아야 안내가 오버레이 뒤로 가리지 않는다.
        int fail;
        using (hud.Ui.ShowLoading(PurchasingMessage)) {
            fail = await hud.Shop.CanBuyOfferAsync(offerId, qty, ct);

            // 스토어 상품 등록은 접속 시점의 상점 목록으로 한 번 한다. 접속 중에 판매가 시작된 상품
            // (기간제 스텝업 등)은 그 목록에 없었으므로 결제 직전에 등록을 보장한다. 이미 등록된 상품은
            // IapService 가 걸러 내므로 평소에는 왕복이 늘지 않는다.
            if (fail == 0) await hud.Iap.EnsureProductsAsync(new[] { product }, ct);
        }

        if (fail != 0) {
            hud.Ui.ShowToast(BuyFailMessage(fail));
            return new BuyApplyResult { Result = new BuyResultDto { IsSuccess = false, FailReason = CancelledFail }, Catalog = null };
        }

        var purchase = await hud.Iap.PurchaseAsync(offerId, ct);
        if (!purchase.Ok) {
            // 사용자가 스스로 취소한 것은 실패로 알리지 않는다(정상 경로다).
            if (!purchase.Cancelled) hud.Ui.ShowToast(purchase.Error);
            return new BuyApplyResult { Result = new BuyResultDto { IsSuccess = false, FailReason = CancelledFail }, Catalog = null };
        }

        // 결제 창이 닫힌 뒤가 진짜 공백 구간이다. 여기서 영수증 검증 -> 지급 -> 확정 -> 재화 재조회까지
        // 서버 왕복이 이어지는데, 그동안 화면이 그대로라 "결제는 됐는데 아무 일도 안 일어난다"로 보인다.
        // 취소하고 돌아온 경우는 위에서 이미 빠져나가므로 이 로딩을 지나가지 않는다(취소에 로딩을 띄우면
        // 화면만 한 번 깜빡이고 알려 주는 것이 없다).
        //
        // 결제 창 자체는 덮지 않는다. 그 구간은 스토어의 네이티브 화면이 앱 위에 올라와 있어 무엇을 그리든
        // 보이지 않고, 미리 켜 두면 취소하고 돌아오는 순간에만 로딩이 번쩍인다.
        using (hud.Ui.ShowLoading(PurchasingMessage)) {
            return await RedeemAsync(hud, purchase.Purchase, qty, ct);
        }
    }

    /// <summary>
    /// 스토어 결제를 서버에 태워 지급받고, 성공하면 결제를 확정한다.
    /// 새로 산 결제와 복구된 결제가 같은 경로를 쓰므로 처리 규칙이 갈라지지 않는다.
    /// </summary>
    public static async UniTask<BuyApplyResult> RedeemAsync(MainHudContext hud, IapPurchase purchase, int qty, CancellationToken ct) {
        // 멱등키는 서버가 영수증에서 뽑은 스토어 거래 id 로 덮어쓴다. 여기서 보내는 값은 쓰이지 않는다.
        var res = await hud.Shop.BuyOfferAsync(purchase.ProductId, "", qty, free: false,
                                               store: purchase.Store, receipt: purchase.Receipt, ct: ct);

        if (!res.IsSuccess) {
            // 확정하지 않는다. 결제가 스토어에 남아 다음 실행에서 다시 올라오고, 그때 복구가 재시도한다.
            return new BuyApplyResult { Result = res, Catalog = null };
        }

        // 지급이 끝났으므로 이제 확정한다. 이미 처리된 결제를 다시 태워도 서버가 멱등키로 막으므로,
        // 중복 지급 없이 확정만 다시 일어난다.
        hud.Iap.Confirm(purchase);
        return await ApplyAsync(hud, res, ct);
    }

    // 서버가 돌려준 결과를 화면 상태에 반영한다. 재화 구매와 현금 구매가 같은 반영 경로를 쓴다.
    private static async UniTask<BuyApplyResult> ApplyAsync(MainHudContext hud, BuyResultDto res, CancellationToken ct) {
        hud.Store.Gold.Value = res.NewGold;
        hud.Store.Diamond.Value = res.NewDiamond;
        hud.Store.Stamina.Value = res.NewStamina;

        // 범용 재화(티켓 등) 잔량은 S_BuyResult 에 없으므로 카탈로그 재조회로 동기화.
        ShopCatalogDto catalog = null;
        try {
            catalog = await hud.Shop.RequestShopAsync(ct);
            hud.Store.Gold.Value = catalog.Gold;
            hud.Store.Diamond.Value = catalog.Diamond;
            hud.Store.ApplyShopCurrencies(catalog.Currencies);
        } catch (OperationCanceledException) { }

        if (res.Rolls.Count > 0) hud.Store.ApplyGachaRolls(res.Rolls);
        if (res.EquipRolls.Count > 0) hud.Store.ApplyEquipRolls(res.EquipRolls);
        hud.Sound.PlaySfx(SfxKey.Purchase);
        // 구매(특히 현금 충전)로 충전 보상 등이 수령 가능해질 수 있어 배지 상태를 즉시 갱신한다.
        if (hud.Badges != null) hud.Badges.RefreshAsync(ct).Forget();
        return new BuyApplyResult { Result = res, Catalog = catalog };
    }

    /// <summary>현금(인앱) 결제를 뜻하는 결제 수단 키. 서버 ShopCatalog.CashCurrency 와 같은 값이다.</summary>
    public const string CashCurrency = "cash";

    /// <summary>
    /// 현금 결제 진행 중에 띄우는 로딩 문구. 스토어 결제와 서버 지급이 한 번의 구매로 보여야 하므로
    /// 두 구간이 같은 문구를 쓴다(구간이 바뀔 때 문구가 달라지면 두 번 산 것처럼 읽힌다).
    /// </summary>
    private const string PurchasingMessage = "구매 중입니다...";

    /// <summary>사용자가 결제를 취소했을 때의 사유 값. 안내를 띄우지 않는다는 뜻으로만 쓴다.</summary>
    public const int CancelledFail = -1;

    /// <summary>구매 실패 사유(BuyResultDto.FailReason) -> 사용자 메시지.</summary>
    public static string BuyFailMessage(int fail) => fail switch {
        2 => "재화가 부족합니다.",
        3 => "이미 처리된 요청입니다.",
        4 => "인앱결제는 준비 중입니다.",
        5 => "구매할 수 없습니다.",
        6 => "구매 한도를 초과했습니다.",
        7 => "이전 단계를 먼저 구매해야 합니다.",
        8 => "판매 기간이 아닙니다.",
        9 => "수량이 올바르지 않습니다.",
        10 => "무료 뽑기를 아직 사용할 수 없습니다.",
        11 => "결제 준비 중입니다. 잠시 후 다시 시도해 주세요.",
        12 => "일일 결제 한도에 도달했습니다.\n자정 이후에 다시 시도해 주세요.",
        13 => "월 결제 한도에 도달했습니다.\n다음 달에 다시 시도해 주세요.",
        // 장비 모집 전용. 구매 한도(6)와 뜻이 전혀 다르므로 값을 따로 쓴다(서버 GameDataRepository.BagFullFail).
        14 => "가방이 가득 찼습니다.\n장비를 분해한 뒤 다시 시도해 주세요.",
        // 길드 레벨 조건 상품(서버 GameDataRepository.GuildLevelFail). 필요 레벨은 상품 정보로 따로 안내한다.
        15 => "길드 레벨이 부족합니다.",
        _ => "구매할 수 없습니다."
    };

    /// <summary>잠긴 상품 카드에 붙는 짧은 사유. 길드 레벨 조건 상품만 레벨을 밝히고, 나머지 잠금은 공통 문구다.</summary>
    public static string LockedLabel(ShopOfferDto offer) =>
        offer != null && offer.RequiredGuildLevel > 0 ? $"길드 레벨 {offer.RequiredGuildLevel}이상" : "잠금";

    /// <summary>길드 레벨 조건 상품이 잠겨 있을 때의 안내 문구.</summary>
    public static string GuildLevelRequiredMessage(int requiredGuildLevel) =>
        $"길드 레벨 {requiredGuildLevel} 이상부터 구매할 수 있습니다.";
}
