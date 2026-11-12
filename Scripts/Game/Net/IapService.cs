using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Unity IAP(5.x) 구현. 스토어 연결 / 상품 조회 / 결제 / 확정만 다루고, 지급은 서버가 한다.
///
/// 결제는 두 단계다(IAP 5 의 규약).
///   1. PurchaseProduct -> OnPurchasePending 으로 "아직 확정되지 않은 결제" 가 온다
///   2. 내용을 지급한 뒤 ConfirmPurchase 를 불러야 결제가 끝난다
/// 2번을 부르기 전에 앱이 죽으면 그 결제는 살아 있고 다음 실행에서 다시 전달된다. 이 성질 덕분에
/// "돈은 나갔는데 아무것도 못 받는" 상태를 없앨 수 있으므로, 확정은 반드시 서버 지급이 끝난 뒤에 한다.
///
/// 조건부 컴파일(UNITY_PURCHASING 등)로 코드를 감싸지 않는다. 그 심볼이 정의되지 않으면 결제 코드가
/// 통째로 사라져 "빌드는 되는데 결제만 안 되는" 상태가 되고, 그 사실을 스토어 심사나 라이브에서야 알게 된다.
/// 패키지가 빠지면 컴파일이 깨지는 편이 훨씬 안전하다(manifest.json 에 선언된 의존성이다).
/// </summary>
public sealed class IapService : IIapService, IDisposable {
    // 결제 구간 알림을 보내는 통로. 결제 자체와는 무관하며, 서버가 이 구간의 침묵으로 접속을 끊지 않게 한다.
    private readonly INetworkService _net;

    public IapService(INetworkService net) {
        _net = net;
    }

    /// <summary>스토어 응답을 기다리는 최대 시간. 결제 창은 사용자가 붙잡고 있을 수 있어 따로 길게 둔다.</summary>
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan PurchaseTimeout = TimeSpan.FromMinutes(5);

    public bool IsReady { get; private set; }

    /// <summary>
    /// 실기기(안드로이드/iOS)에서만 결제를 지원한다. 에디터와 PC 는 스토어가 없어 영수증을 만들 수 없고,
    /// 만들어 봐야 서버 검증을 통과하지 못한다. 개발 중 상점 흐름은 서버의 개발용 설정으로 확인한다
    /// (Payment.AllowUnverifiedCashPurchase).
    /// </summary>
    public bool IsSupported =>
        Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;

    public event Action<IapPurchase> PendingPurchaseReceived;

    private StoreController _store;

    // 지금 기다리고 있는 결제. 결제 창이 하나뿐이라 한 번에 하나만 진행한다.
    private string _awaitingProductId;
    private UniTaskCompletionSource<IapPurchaseResult> _awaiting;

    private bool _connecting;
    private UniTaskCompletionSource<bool> _connected;
    private UniTaskCompletionSource<bool> _fetched;

    // 스토어에 등록을 마친 상품 id. 같은 상품을 두 번 조회하지 않기 위한 것이며,
    // 세션 도중 새로 열리는 상품을 추가 등록할 때 무엇이 빠졌는지 판단하는 기준이 된다.
    private readonly HashSet<string> _fetchedProductIds = new();

    // 상품 조회를 한 번에 하나만 돌린다. 조회 결과는 이벤트(_fetched)로 오는데, 두 조회가 겹치면
    // 먼저 온 결과가 나중 조회의 대기를 끝내 버려 어느 쪽도 제대로 끝나지 않는다.
    private readonly SemaphoreSlim _fetchGate = new(1, 1);

    public async UniTask<bool> InitializeAsync(IReadOnlyList<IapProduct> products, CancellationToken ct = default) {
        if (!IsSupported) return false;
        if (products == null || products.Count == 0) return false;

        // 이미 연결돼 있으면 연결은 건너뛰고 빠진 상품만 채운다. 접속 중에 상품이 늘어난 경우
        // (한정 판매가 열린 뒤 메인으로 다시 들어온 경우 등) 여기서 그대로 메워진다.
        if (IsReady) return await EnsureProductsAsync(products, ct);
        if (_connecting) {
            await _connected.Task;
            return IsReady && await EnsureProductsAsync(products, ct);
        }

        _connecting = true;
        _connected = new UniTaskCompletionSource<bool>();

        try {
            _store = UnityIAPServices.StoreController();

            // 이벤트는 반드시 Connect 이전에 건다. 지난번에 확정하지 못한 결제가 연결 직후 바로 올라오는데,
            // 그때 구독이 없으면 그 결제를 놓친다(유저는 이미 낸 돈이다).
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;
            _store.OnStoreDisconnected += OnStoreDisconnected;
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += OnProductsFetchFailed;
            _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

            await _store.Connect().AsUniTask().Timeout(ConnectTimeout);

            bool ok = await FetchMissingAsync(products, ct);

            IsReady = ok;
            _connected.TrySetResult(ok);

            // 확정하지 못한 채 남아 있는 결제를 스토어에 다시 물어본다. 결과는 OnPurchasePending 으로 온다.
            if (ok) _store.FetchPurchases();
            return ok;
        } catch (Exception e) {
            Debug.LogWarning($"[IAP] 스토어 연결 실패: {e.Message}");
            IsReady = false;
            _connected.TrySetResult(false);
            return false;
        } finally {
            _connecting = false;
        }
    }

    public async UniTask<bool> EnsureProductsAsync(IReadOnlyList<IapProduct> products, CancellationToken ct = default) {
        if (!IsSupported) return false;
        if (products == null || products.Count == 0) return false;

        // 연결이 진행 중이면 끝나기를 기다린다. 그 연결이 필요한 상품을 이미 등록했을 수도 있으므로
        // 판단은 그 뒤에 한다(같은 상품을 두 번 조회하지 않는다).
        if (_connecting) await _connected.Task;
        if (!IsReady) return false;

        try {
            return await FetchMissingAsync(products, ct);
        } catch (OperationCanceledException) {
            return false;
        } catch (Exception e) {
            Debug.LogWarning($"[IAP] 상품 추가 등록 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 아직 등록되지 않은 상품만 스토어에 조회해 둔다. 등록에 성공한 것만 기록하므로,
    /// 스토어에 없는 상품(콘솔 등록 누락 등)은 다음에 다시 시도된다.
    /// </summary>
    private async UniTask<bool> FetchMissingAsync(IReadOnlyList<IapProduct> products, CancellationToken ct) {
        await _fetchGate.WaitAsync(ct);
        try {
            // 현금 상품은 대부분 소모성이다(30일 패키지도 30일이 끝나면 다시 산다).
            // 평생 패스처럼 계정에 영구히 남는 상품만 서버 카탈로그가 비소모성으로 표시해 내려 준다.
            var definitions = new List<ProductDefinition>(products.Count);
            foreach (var p in products)
                if (!string.IsNullOrEmpty(p.Id) && !_fetchedProductIds.Contains(p.Id))
                    definitions.Add(new ProductDefinition(p.Id, p.NonConsumable ? ProductType.NonConsumable : ProductType.Consumable));

            if (definitions.Count == 0) return true;   // 이미 다 등록돼 있다

            _fetched = new UniTaskCompletionSource<bool>();
            _store.FetchProducts(definitions);
            bool ok = await _fetched.Task.Timeout(ConnectTimeout);
            if (!ok) return false;

            // 조회가 성공해도 스토어에 없는 상품은 돌아오지 않는다. 실제로 잡힌 것만 등록으로 친다.
            foreach (var d in definitions) {
                if (_store.GetProductById(d.id) != null) _fetchedProductIds.Add(d.id);
                else Debug.LogWarning($"[IAP] 스토어에 없는 상품입니다: {d.id}");
            }
            return true;
        } finally {
            _fetchGate.Release();
        }
    }

    public async UniTask<IapPurchaseResult> PurchaseAsync(string productId, CancellationToken ct = default) {
        if (!IsSupported) return IapPurchaseResult.Fail("이 기기에서는 결제를 지원하지 않습니다.");
        if (!IsReady) return IapPurchaseResult.Fail("결제 준비 중입니다. 잠시 후 다시 시도해 주세요.");
        if (_awaiting != null) return IapPurchaseResult.Fail("이미 결제가 진행 중입니다.");

        var product = _store.GetProductById(productId);
        if (product == null) return IapPurchaseResult.Fail("스토어에서 상품을 찾을 수 없습니다.");

        _awaitingProductId = productId;
        _awaiting = new UniTaskCompletionSource<IapPurchaseResult>();

        // 결제 창이 떠 있는 동안 앱은 멈춰 핑을 보내지 못한다. 그 침묵으로 서버가 접속을 끊지 않도록
        // 미리 알린다. 끝나면 반드시 해제해, 정말로 끊긴 접속이 오래 남지 않게 한다.
        SetPurchaseGate(true);

        try {
            _store.PurchaseProduct(product);
            return await _awaiting.Task.AttachExternalCancellation(ct).Timeout(PurchaseTimeout);
        } catch (TimeoutException) {
            return IapPurchaseResult.Fail("결제 응답이 오지 않았습니다.");
        } catch (OperationCanceledException) {
            return IapPurchaseResult.UserCancelled();
        } finally {
            SetPurchaseGate(false);
            _awaiting = null;
            _awaitingProductId = null;
        }
    }

    private void SetPurchaseGate(bool open) {
        // 통신이 이미 끊겨 있으면 보낼 곳이 없다. 실패해도 결제 자체에는 영향이 없으므로 조용히 넘어간다.
        try {
            _net?.Send(new C_PurchaseGate { open = open }.Write());
        } catch (Exception e) {
            Debug.LogWarning($"[IAP] 결제 구간 알림 실패: {e.Message}");
        }
    }

    public void Confirm(IapPurchase purchase) {
        if (purchase?.Handle is not PendingOrder order) return;
        try {
            _store.ConfirmPurchase(order);
        } catch (Exception e) {
            // 확정에 실패해도 지급은 이미 끝났다. 그 결제는 다음 실행에서 다시 올라오지만,
            // 서버가 같은 거래 id 를 멱등키로 막으므로 두 번 지급되지 않는다.
            Debug.LogWarning($"[IAP] 결제 확정 실패({purchase.ProductId}): {e.Message}");
        }
    }

    public async UniTask<bool> RestoreAsync(CancellationToken ct = default) {
        if (!IsSupported) return false;
        if (_connecting) await _connected.Task;
        if (!IsReady || _store == null) return false;

        var done = new UniTaskCompletionSource<bool>();
        try {
            // 복원된 거래 중 아직 확정하지 않은 것은 OnPurchasePending 으로 다시 와 평소 복구 흐름을 탄다.
            _store.RestoreTransactions((ok, error) => {
                if (!ok) Debug.LogWarning($"[IAP] 구매 복원 실패: {error}");
                done.TrySetResult(ok);
            });
            bool restored = await done.Task.AttachExternalCancellation(ct).Timeout(ConnectTimeout);

            // 안드로이드는 복원 대신 보유 거래를 다시 읽는 것이 같은 효과다. 두 스토어 모두 한 번 더 물어 둔다.
            if (restored) _store.FetchPurchases();
            return restored;
        } catch (TimeoutException) {
            Debug.LogWarning("[IAP] 구매 복원 응답이 오지 않았습니다.");
            return false;
        }
    }

    // ── 스토어 이벤트 ────────────────────────────────
    private void OnProductsFetched(List<Product> products) => _fetched?.TrySetResult(true);

    private void OnProductsFetchFailed(ProductFetchFailed failure) {
        Debug.LogWarning($"[IAP] 상품 조회 실패: {failure.FailureReason}");
        _fetched?.TrySetResult(false);
    }

    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure) {
        // 미확정 결제를 못 읽은 것뿐이라 진행은 계속한다. 다음 실행에서 다시 시도된다.
        Debug.LogWarning($"[IAP] 미확정 결제 조회 실패: {failure.FailureReason}");
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription failure) {
        IsReady = false;
        // 상품 등록은 이 연결에 딸린 것이다. 기록을 비워 두지 않으면 다시 연결됐을 때
        // "이미 등록됨"으로 판단해 아무 상품도 받아 오지 않는다.
        _fetchedProductIds.Clear();
        Debug.LogWarning($"[IAP] 스토어 연결이 끊겼습니다: {failure.Message}");
    }

    /// <summary>
    /// 결제가 성사됐지만 아직 확정되지 않은 상태. 지금 산 것일 수도 있고, 지난번에 확정하지 못해
    /// 다시 전달된 것일 수도 있다. 기다리는 쪽이 있으면 그쪽에 넘기고, 없으면 복구 흐름으로 보낸다.
    /// </summary>
    private void OnPurchasePending(PendingOrder order) {
        var purchase = ToPurchase(order);
        if (purchase == null) return;

        // 지금 기다리는 결제면 그쪽으로 넘긴다. TrySetResult 가 false 면 이미 결론이 난 대기라는 뜻이므로
        // (실패로 끝난 뒤 늦게 도착한 경우 등) 그냥 두지 않고 복구로 보낸다. 그러지 않으면 이 결제가
        // 이번 실행에서는 아무에게도 전달되지 않는다.
        if (_awaiting != null && purchase.ProductId == _awaitingProductId
            && _awaiting.TrySetResult(IapPurchaseResult.Success(purchase))) {
            return;
        }

        purchase.IsRestored = true;
        PendingPurchaseReceived?.Invoke(purchase);
    }

    private void OnPurchaseConfirmed(Order order) {
        if (order is FailedOrder failed)
            Debug.LogWarning($"[IAP] 결제 확정 실패: {failed.FailureReason} - {failed.Details}");
    }

    private void OnPurchaseFailed(FailedOrder failed) {
        // "이미 가진 상품" 은 실패가 아니라 미처리 결제가 남아 있다는 뜻이다.
        //
        // 앞선 결제가 확정되지 못하면(서버 지급 실패 등) 그 결제는 스토어에 소비되지 않은 채 남고,
        // 같은 상품을 다시 사려 하면 스토어가 이 사유로 거절한다. 그대로 두면 유저는 돈을 낸 상품을
        // 영영 받지 못하므로, 남아 있는 결제를 다시 끌어와 지급 흐름에 태운다(결과는 OnPurchasePending).
        if (failed.FailureReason == PurchaseFailureReason.DuplicateTransaction
            || failed.FailureReason == PurchaseFailureReason.ExistingPurchasePending) {
            Debug.LogWarning($"[IAP] 미처리 결제가 남아 있어 복구를 시도합니다({failed.FailureReason}).");
            try {
                _store.FetchPurchases();
            } catch (Exception e) {
                Debug.LogWarning($"[IAP] 미처리 결제 조회 실패: {e.Message}");
            }
            _awaiting?.TrySetResult(IapPurchaseResult.Fail(
                "이전 결제가 아직 처리되지 않았습니다.\n잠시 후 자동으로 지급됩니다."));
            return;
        }

        if (_awaiting == null) return;

        bool cancelled = failed.FailureReason == PurchaseFailureReason.UserCancelled;
        _awaiting.TrySetResult(cancelled
            ? IapPurchaseResult.UserCancelled()
            : IapPurchaseResult.Fail($"결제에 실패했습니다. ({failed.FailureReason})"));
    }

    /// <summary>
    /// 승인 대기(가족 승인 등). 아직 결제된 것이 아니므로 아무것도 지급하지 않는다.
    /// 승인되면 OnPurchasePending 으로 다시 오고, 그때 복구 흐름이 지급을 받아 온다.
    /// </summary>
    private void OnPurchaseDeferred(DeferredOrder order) {
        _awaiting?.TrySetResult(IapPurchaseResult.Fail("결제 승인을 기다리는 중입니다.\n승인되면 자동으로 지급됩니다."));
    }

    // 스토어 주문을 순수 DTO 로 옮긴다. 영수증은 서버가 스토어에 되물을 때 쓴다.
    private static IapPurchase ToPurchase(PendingOrder order) {
        var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
        if (product == null) return null;

        var store = CurrentStore();
        // Apple 은 JWS 서명 거래를, Google 은 영수증 JSON 을 그대로 보낸다(서버가 스토어별로 나눠 읽는다).
        string receipt = store == IapStoreKind.AppleAppStore
            ? order.Info.Apple?.jwsRepresentation ?? ""
            : order.Info.Receipt ?? "";

        if (string.IsNullOrEmpty(receipt)) {
            Debug.LogWarning($"[IAP] 영수증이 비어 있습니다({product.definition.id}). 서버 검증을 할 수 없습니다.");
            return null;
        }

        return new IapPurchase {
            ProductId = product.definition.id,
            Store = store,
            Receipt = receipt,
            Handle = order,
        };
    }

    private static IapStoreKind CurrentStore() => Application.platform switch {
        RuntimePlatform.IPhonePlayer => IapStoreKind.AppleAppStore,
        RuntimePlatform.Android => IapStoreKind.GooglePlay,
        _ => IapStoreKind.None,
    };

    public void Dispose() {
        _fetchGate.Dispose();
        if (_store == null) return;

        _store.OnPurchasePending -= OnPurchasePending;
        _store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
        _store.OnPurchaseFailed -= OnPurchaseFailed;
        _store.OnPurchaseDeferred -= OnPurchaseDeferred;
        _store.OnStoreDisconnected -= OnStoreDisconnected;
        _store.OnProductsFetched -= OnProductsFetched;
        _store.OnProductsFetchFailed -= OnProductsFetchFailed;
        _store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
    }
}
