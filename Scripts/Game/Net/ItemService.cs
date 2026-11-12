using System;
using System.Collections.Generic;
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

        var granted = new List<ItemCountDto>(res.grantedItems.Count);
        foreach (var g in res.grantedItems) granted.Add(new ItemCountDto { ItemId = g.itemId, Count = g.count });

        return new ItemUseResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            ItemId = res.itemId,
            NewCount = res.newCount,
            CharDataId = res.charDataId,
            CharExp = new CharExpGain {
                CharDataId = res.charDataId,
                // 스토어 갱신은 NewLevel/NewExp 로 하고 이 값은 표시용이라 int 로 잘라도 된다(전체 양은 아래 GainedExp).
                GainedExp = (int)Math.Min(res.gainedExp, int.MaxValue),
                NewLevel = res.newLevel,
                NewExp = res.newExp,
                MaxExp = res.maxExp,
                GainedStatPoints = res.gainedStatPoints
            },
            Updated = res.isSuccess && res.hasCharacter ? new CharacterProgress {
                CharDataId = res.charDataId,
                Level = res.newLevel, Exp = res.newExp, MaxExp = res.maxExp,
                StarLevel = res.starLevel, StatPoints = res.statPoints, Shards = res.shards,
                InvestedAttack = res.invAttack, InvestedDefense = res.invDefense, InvestedHealth = res.invHealth,
                InvestedAttackSpeed = res.invAttackSpeed, InvestedMoveSpeed = res.invMoveSpeed, InvestedSkillAccel = res.invSkillAccel,
                AutoStatType = res.autoStatType, LimitBroken = res.limitBroken
            } : null,
            GainedExp = res.gainedExp,
            ExtractedExp = res.extractedExp,
            GrantedItems = granted,
            Refund = new GrowthRefund {
                Shards = res.refundShards, Diamond = res.refundDiamond, LimitBreakTicket = res.refundLimitBreakTicket
            },
            NewDiamond = res.newDiamond,
            NewLimitBreakTicket = res.newLimitBreakTicket
        };
    }

    public async UniTask<ItemUsePreview> PreviewAsync(int itemId, int charDataId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ItemUsePreview>(cts.Token);
        _net.Send(new C_ItemUsePreview { itemId = itemId, charDataId = charDataId }.Write());
        var res = await wait;

        var grants = new List<ItemCountDto>(res.grantItems.Count);
        foreach (var g in res.grantItems) grants.Add(new ItemCountDto { ItemId = g.itemId, Count = g.count });

        return new ItemUsePreview {
            FailReason = res.failReason, Value = res.value, GrantItems = grants,
            CurrentLevel = res.currentLevel, ResultLevel = res.resultLevel,
            CurrentStar = res.currentStar,
            Refund = new GrowthRefund {
                Shards = res.refundShards, Diamond = res.refundDiamond, LimitBreakTicket = res.refundLimitBreakTicket
            }
        };
    }
}
