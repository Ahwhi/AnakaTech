using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

/// <summary>
/// 한정 판매 통신 구현. 조회(요청-응답)와 알림(서버 푸시)을 한 서비스가 함께 다룬다.
///
/// 서버 푸시(S_FlashSaleNotify)를 상시 구독하므로, 어느 컨텐츠가 한정 판매를 열었든 클라는 같은 자리에서 받는다
/// (우편 알림 S_MailNotify 와 같은 방식). 그래서 새 발동 조건이 생겨도 이 서비스는 손댈 것이 없다.
///
/// 만료 처리는 목록을 읽는 쪽이 아니라 여기서 한다. 12시간짜리 상품이라 앱을 켜 둔 채로 끝나는 일이 흔한데,
/// 화면마다 "끝났는지" 를 각자 판정하면 배지 숫자와 팝업 내용이 서로 다른 순간이 생긴다.
/// </summary>
public sealed class FlashSaleService : IFlashSaleService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    // 만료를 걷어 내는 주기. 카운트다운 표시는 화면이 매 프레임 하고, 이 주기는 "목록에서 빠지는" 시점만 정한다.
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(10);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<IReadOnlyList<FlashSaleDto>> _active = new(Array.Empty<FlashSaleDto>());
    private readonly ReactiveProperty<int> _purchasable = new(0);
    private readonly Subject<IReadOnlyList<FlashSaleDto>> _opened = new();
    private readonly CompositeDisposable _subs = new();

    public IReadOnlyReactiveProperty<IReadOnlyList<FlashSaleDto>> Active => _active;
    public IReadOnlyReactiveProperty<int> PurchasableCount => _purchasable;
    public IObservable<IReadOnlyList<FlashSaleDto>> Opened => _opened;

    public FlashSaleService(INetworkService net) {
        _net = net;
        _net.PacketReceived += OnPacketReceived;

        // 열려 있는 것이 있을 때만 훑는다(대부분의 시간에는 목록이 비어 있어 아무 일도 하지 않는다).
        Observable.Interval(SweepInterval).Subscribe(_ => DropExpired()).AddTo(_subs);
    }

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ResponseTimeout);

            var wait = _net.WaitPacketAsync<S_FlashSaleInfo>(cts.Token);
            _net.Send(new C_FlashSaleInfo().Write());
            var res = await wait;

            var list = new List<FlashSaleDto>(res.flashSales.Count);
            foreach (var f in res.flashSales) list.Add(ToDto(f.offerId, f.startUnix, f.endUnix, f.soldOut));
            Publish(Filtered(list));
        } catch (OperationCanceledException) {
            // 화면 전환/타임아웃. 배지는 다음 갱신에서 맞춰지므로 조용히 넘어간다.
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // 서버가 새로 연 한정 판매를 밀어 준다(요청 없음). 목록에 합치고 "새로 열림"을 알린다.
    private void OnPacketReceived(IPacket packet) {
        if (packet is not S_FlashSaleNotify notify || notify.flashSales.Count == 0) return;

        var merged = new List<FlashSaleDto>(_active.Value);
        var opened = new List<FlashSaleDto>(notify.flashSales.Count);

        foreach (var f in notify.flashSales) {
            var dto = ToDto(f.offerId, f.startUnix, f.endUnix, f.soldOut);
            if (!dto.IsOpen) continue;
            // 같은 상품이 이미 목록에 있으면 새로 연 것이 아니다(중복 알림 방어).
            if (merged.Exists(x => x.OfferId == dto.OfferId)) continue;

            merged.Add(dto);
            opened.Add(dto);
        }
        if (opened.Count == 0) return;

        Publish(Filtered(merged));
        _opened.OnNext(opened);
    }

    // 끝난 것을 목록에서 뺀다. 실제로 빠진 것이 있을 때만 값을 밀어 불필요한 갱신을 만들지 않는다.
    private void DropExpired() {
        var current = _active.Value;
        if (current.Count == 0) return;

        var alive = Filtered(current);
        if (alive.Count != current.Count) Publish(alive);
    }

    // 목록과 "살 수 있는 개수"를 함께 민다. 두 값이 어긋나면 버튼은 켜져 있는데 살 것이 없는 상태가 된다.
    private void Publish(List<FlashSaleDto> list) {
        _active.Value = list;

        int purchasable = 0;
        foreach (var f in list) if (f.IsPurchasable) purchasable++;
        _purchasable.Value = purchasable;
    }

    // 열려 있는 것만, 끝나는 순서로. 화면은 이 순서를 그대로 그리므로 곧 끝나는 상품이 위에 온다.
    private static List<FlashSaleDto> Filtered(IReadOnlyList<FlashSaleDto> source) {
        var list = new List<FlashSaleDto>(source.Count);
        foreach (var f in source) if (f != null && f.IsOpen) list.Add(f);
        list.Sort((a, b) => a.EndUnix.CompareTo(b.EndUnix));
        return list;
    }

    private static FlashSaleDto ToDto(string offerId, long startUnix, long endUnix, bool soldOut) =>
        new() { OfferId = offerId, StartUnix = startUnix, EndUnix = endUnix, SoldOut = soldOut };

    public void Dispose() {
        _net.PacketReceived -= OnPacketReceived;
        _subs.Dispose();
        _opened.Dispose();
        _active.Dispose();
        _purchasable.Dispose();
    }
}
