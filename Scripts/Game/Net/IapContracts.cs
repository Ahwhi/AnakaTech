using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 인앱결제 통신 계약과 순수 DTO. 화면과 상점 흐름은 이 타입만 참조하며 Unity IAP 타입을 직접 알지 않는다.
// 스토어 SDK 가 바뀌어도(버전 업/교체) 여기와 구현만 손보면 되고 화면은 그대로다.

/// <summary>결제 스토어. 값이 패킷에 실려 가므로 서버 StoreKind 와 반드시 같아야 한다.</summary>
public enum IapStoreKind {
    None = 0,
    GooglePlay = 1,
    AppleAppStore = 2,
}

/// <summary>
/// 스토어에서 결제가 끝났지만 아직 확정(소비)하지 않은 결제 1건.
///
/// 이 상태가 존재하는 이유는 "돈은 냈는데 못 받는" 경우를 없애기 위해서다. 스토어에 확정을 알리기 전까지
/// 그 결제는 살아 있고, 앱이 죽어도 다음 실행에서 다시 전달된다. 그래서 서버가 지급을 끝낸 뒤에만 확정한다.
/// </summary>
public sealed class IapPurchase {
    /// <summary>스토어 상품 id. 우리 OfferId 와 같은 값이다.</summary>
    public string ProductId = "";

    /// <summary>어느 스토어의 결제인지.</summary>
    public IapStoreKind Store;

    /// <summary>서버에 그대로 넘길 영수증(Google=영수증 JSON, Apple=JWS).</summary>
    public string Receipt = "";

    /// <summary>확정에 쓰는 스토어 쪽 핸들. 화면은 내용을 알 필요가 없다(구현이 캐스팅해서 쓴다).</summary>
    public object Handle;

    /// <summary>
    /// 이번에 새로 산 것이 아니라, 지난번에 확정하지 못해 다시 전달된 결제인지.
    /// 복구 흐름은 이 값으로 "조용히 처리할지" 를 정한다(구매 연출을 다시 띄우면 안 된다).
    /// </summary>
    public bool IsRestored;

    public bool IsValid => !string.IsNullOrEmpty(ProductId)
                        && !string.IsNullOrEmpty(Receipt)
                        && Store != IapStoreKind.None
                        && Handle != null;
}

/// <summary>결제 시도 결과. 실패는 예외가 아니라 값으로 돌려준다(사용자 취소가 흔한 정상 경로이기 때문이다).</summary>
public sealed class IapPurchaseResult {
    public bool Ok;
    public IapPurchase Purchase;

    /// <summary>사용자가 스스로 취소했는지. 이 경우 실패 안내를 띄우지 않는다.</summary>
    public bool Cancelled;

    /// <summary>실패 사유(로그/토스트용).</summary>
    public string Error = "";

    public static IapPurchaseResult Success(IapPurchase p) => new() { Ok = true, Purchase = p };
    public static IapPurchaseResult Fail(string error) => new() { Error = error };
    public static IapPurchaseResult UserCancelled() => new() { Cancelled = true, Error = "사용자가 결제를 취소했습니다." };
}

/// <summary>
/// 인앱결제 창구.
///
/// 이 인터페이스는 "스토어에서 결제하고 영수증을 받아 오는 것" 까지만 책임진다. 그 영수증으로 무엇을
/// 받는지는 서버가 정하므로 여기서는 지급도 검증도 하지 않는다.
/// </summary>
public interface IIapService {
    /// <summary>스토어에 연결되어 결제를 받을 수 있는 상태인지.</summary>
    bool IsReady { get; }

    /// <summary>이 플랫폼에서 인앱결제를 지원하는지(에디터/PC 는 지원하지 않는다).</summary>
    bool IsSupported { get; }

    /// <summary>
    /// 스토어에 연결하고 상품 목록을 받아 둔다. 여러 번 불러도 한 번만 실제로 연결하며,
    /// 이미 연결돼 있으면 아직 등록되지 않은 상품만 추가로 받아 둔다.
    /// 상품 id 는 서버 카탈로그에서 온다(클라가 목록을 따로 들고 있으면 서버와 어긋난다).
    /// </summary>
    UniTask<bool> InitializeAsync(IReadOnlyList<string> productIds, CancellationToken ct = default);

    /// <summary>
    /// 아직 스토어에 등록되지 않은 상품만 추가로 받아 둔다. 이미 등록된 것은 다시 묻지 않는다.
    ///
    /// 세션 도중에 새로 열리는 상품(한정 판매 등)을 위해 있다. 상품 목록은 접속할 때 한 번만 받으므로,
    /// 그 뒤에 열린 상품은 이 창구로 등록하지 않으면 결제 시점에 스토어가 상품을 찾지 못한다.
    /// </summary>
    UniTask<bool> EnsureProductsAsync(IReadOnlyList<string> productIds, CancellationToken ct = default);

    /// <summary>
    /// 결제 창을 띄우고 결과를 기다린다. 성공하면 아직 확정되지 않은 결제를 돌려주므로,
    /// 서버 지급이 끝난 뒤 반드시 Confirm 을 불러야 한다.
    /// </summary>
    UniTask<IapPurchaseResult> PurchaseAsync(string productId, CancellationToken ct = default);

    /// <summary>
    /// 결제를 확정(소비)한다. 서버 지급이 끝난 뒤에만 부른다.
    /// 확정하기 전에는 앱이 죽어도 결제가 살아 있어 다음 실행에서 복구된다.
    /// </summary>
    void Confirm(IapPurchase purchase);

    /// <summary>
    /// 확정하지 못한 채 남아 있던 결제가 스토어에서 다시 전달됐을 때 알린다(앱 시작 직후에 주로 온다).
    /// 구독자는 서버에 지급을 요청하고 성공하면 Confirm 해야 한다.
    /// </summary>
    event Action<IapPurchase> PendingPurchaseReceived;
}
