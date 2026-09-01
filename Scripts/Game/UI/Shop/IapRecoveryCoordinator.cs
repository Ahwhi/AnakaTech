using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

/// <summary>
/// 인앱결제 준비와 미완료 결제 복구.
///
/// 왜 필요한가
///   결제는 "스토어 결제 -> 서버 지급 -> 결제 확정" 순서인데, 그 사이에 앱이 죽거나 통신이 끊기면
///   확정되지 않은 결제가 남는다. 유저는 돈을 냈는데 아무것도 받지 못한 상태다.
///   스토어는 그 결제를 다음 실행에서 다시 전달해 주므로, 그것을 받아 서버에 태우면 지급이 완성된다.
///
/// 상품 목록은 서버 카탈로그에서 온다. 클라가 목록을 따로 들고 있으면 상품을 추가했을 때 한쪽만 바뀐다.
///
/// 접속한 뒤에 열리는 상품(한정 판매)도 여기서 챙긴다. 카탈로그는 그 계정에 지금 열려 있는 상품만
/// 담고 있어, 접속 시점의 목록만 등록해 두면 나중에 열린 상품은 결제할 때 스토어가 찾지 못한다.
///
/// 이 흐름은 게임 진행을 막지 않는다. 스토어 연결이 늦거나 실패해도 전투와 다른 기능은 그대로 돌아가고,
/// 복구는 다음 접속에서 다시 시도된다(결제는 확정 전까지 스토어에 남아 있으므로 사라지지 않는다).
/// </summary>
public sealed class IapRecoveryCoordinator : IDisposable {
    private readonly MainHudContext _hud;
    private readonly CancellationToken _ct;

    // 같은 결제를 두 번 태우지 않기 위한 표시. 서버도 멱등키로 막지만, 불필요한 왕복을 줄인다.
    private readonly HashSet<string> _inFlight = new();

    private readonly CompositeDisposable _subs = new();

    public IapRecoveryCoordinator(MainHudContext hud, CancellationToken ct) {
        _hud = hud;
        _ct = ct;

        if (_hud.Iap == null) return;
        _hud.Iap.PendingPurchaseReceived += OnPendingPurchase;

        // 새로 열린 한정 판매를 스토어에 등록한다. 어느 컨텐츠가 열었는지는 알 필요가 없다.
        if (_hud.FlashSale != null)
            _hud.FlashSale.Opened
                .Subscribe(_ => RegisterOpenedProductsAsync().Forget())
                .AddTo(_subs);
    }

    /// <summary>
    /// 스토어에 연결하고 상품을 받아 둔다. 카탈로그의 현금 상품만 대상으로 한다.
    /// 실패해도 조용히 넘어간다(현금 상품을 누를 때 "결제 준비 중" 안내가 나간다).
    /// </summary>
    public async UniTask InitializeAsync(ShopCatalogDto catalog) {
        if (_hud.Iap == null || !_hud.Iap.IsSupported || catalog == null) return;

        var productIds = CashProductIds(catalog);
        if (productIds.Count == 0) return;

        try {
            bool ok = await _hud.Iap.InitializeAsync(productIds, _ct);
            if (!ok) Debug.LogWarning("[IAP] 스토어 준비에 실패했습니다. 현금 상품 구매가 막힙니다.");
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogWarning($"[IAP] 스토어 준비 중 예외: {e.Message}");
        }
    }

    /// <summary>
    /// 한정 판매가 새로 열렸을 때 그 상품을 스토어에 추가로 등록한다.
    ///
    /// 열린 상품의 id 만으로는 현금 상품인지 알 수 없으므로 카탈로그를 다시 받아 판단한다.
    /// 이미 등록된 상품은 IapService 가 걸러 내므로 목록을 통째로 넘겨도 왕복이 늘지 않는다.
    /// </summary>
    private async UniTaskVoid RegisterOpenedProductsAsync() {
        // 준비가 아직 끝나지 않았어도 막지 않는다. 연결 중이면 EnsureProductsAsync 가 그 연결을 기다리고,
        // 시작 전이면 뒤이어 도는 InitializeAsync 가 갱신된 카탈로그로 이 상품까지 함께 등록한다.
        if (_hud.Iap == null || !_hud.Iap.IsSupported || _hud.Shop == null) return;

        try {
            var catalog = await _hud.Shop.RequestShopAsync(_ct);
            var productIds = CashProductIds(catalog);
            if (productIds.Count == 0) return;

            bool ok = await _hud.Iap.EnsureProductsAsync(productIds, _ct);
            // 준비 전이라 건너뛴 경우는 경고할 일이 아니다(뒤이어 도는 준비 과정이 함께 등록한다).
            if (!ok && _hud.Iap.IsReady)
                Debug.LogWarning("[IAP] 새로 열린 상품을 스토어에 등록하지 못했습니다.");
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogWarning($"[IAP] 새로 열린 상품 등록 중 예외: {e.Message}");
        }
    }

    // 카탈로그에서 현금 결제 상품의 id 만 추린다. 그 외 재화 상품은 스토어를 거치지 않는다.
    private static List<string> CashProductIds(ShopCatalogDto catalog) {
        var ids = new List<string>();
        if (catalog == null) return ids;

        foreach (var offer in catalog.Offers)
            if (offer != null && offer.Currency == ShopPurchaseHelper.CashCurrency)
                ids.Add(offer.OfferId);
        return ids;
    }

    // 확정되지 않은 채 남아 있던 결제. 서버에 태워 지급받고 확정한다.
    private void OnPendingPurchase(IapPurchase purchase) {
        if (purchase == null || !purchase.IsValid) return;
        if (!_inFlight.Add(purchase.ProductId)) return;

        RecoverAsync(purchase).Forget();
    }

    private async UniTaskVoid RecoverAsync(IapPurchase purchase) {
        try {
            var r = await ShopPurchaseHelper.RedeemAsync(_hud, purchase, qty: 1, _ct);
            if (r.Result != null && r.Result.IsSuccess) {
                // 지난번에 받지 못한 것을 이제 받았다는 사실은 알려 줘야 한다(조용히 넣으면 오해한다).
                _hud.Ui.ShowToast("이전에 완료하지 못한 결제가 지급되었습니다.");
                return;
            }

            // 실패하면 확정하지 않는다. 결제는 스토어에 그대로 남아 다음 접속에서 다시 시도된다.
            Debug.LogWarning($"[IAP] 미완료 결제 복구 실패({purchase.ProductId}): fail={r.Result?.FailReason}");
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogWarning($"[IAP] 미완료 결제 복구 중 예외({purchase.ProductId}): {e.Message}");
        } finally {
            _inFlight.Remove(purchase.ProductId);
        }
    }

    public void Dispose() {
        _subs.Dispose();
        if (_hud.Iap == null) return;
        _hud.Iap.PendingPurchaseReceived -= OnPendingPurchase;
    }
}
