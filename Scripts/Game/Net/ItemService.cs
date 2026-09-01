using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 소모품 아이템 통신. 각인/본부 서비스와 같은 패턴(요청 -> 응답 대기 -> DTO 변환)이며,
/// 응답을 스토어에 반영하는 일은 호출측(UI)이 한다.
/// </summary>
public sealed class ItemService : IItemService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public ItemService(INetworkService net) => _net = net;

    public async UniTask<ItemInfoDto> RequestInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ItemInfo>(cts.Token);
        _net.Send(new C_ItemInfo().Write());
        var res = await wait;

        var dto = new ItemInfoDto();
        foreach (var i in res.items)
            dto.Items.Add(new ItemCountDto { ItemId = i.itemId, Count = i.count });
        return dto;
    }

    public async UniTask<ItemUseResult> UseAsync(int itemId, int charDataId, int count, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_UseItemResult>(cts.Token);
        _net.Send(new C_UseItem { itemId = itemId, charDataId = charDataId, count = count }.Write());
        var res = await wait;

        return new ItemUseResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            ItemId = res.itemId,
            NewCount = res.newCount,
            CharDataId = res.charDataId,
            CharExp = new CharExpGain {
                CharDataId = res.charDataId,
                GainedExp = res.gainedExp,
                NewLevel = res.newLevel,
                NewExp = res.newExp,
                MaxExp = res.maxExp,
                GainedStatPoints = res.gainedStatPoints
            }
        };
    }
}
