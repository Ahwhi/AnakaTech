using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>상점 통신 구현. 카탈로그 요청/구매/확률조회를 패킷으로 주고받아 DTO 로 변환한다.</summary>
public sealed class ShopService : IShopService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public ShopService(INetworkService net) {
        _net = net;
    }

    public async UniTask<ShopCatalogDto> RequestShopAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ShopCatalog>(cts.Token);
        _net.Send(new C_RequestShop().Write());
        var res = await wait;

        var dto = new ShopCatalogDto {
            Gold = res.gold,
            Diamond = res.diamond,
            RotationCharId = res.rotationCharId,
            RotationStartUnix = res.rotationStartUnix,
            RotationEndUnix = res.rotationEndUnix,
            RotationEquipId = res.rotationEquipId,
            RotationEquipStartUnix = res.rotationEquipStartUnix,
            RotationEquipEndUnix = res.rotationEquipEndUnix,
        };
        foreach (var w in res.wallets) dto.Currencies[w.key] = w.qty;
        foreach (var pt in res.pitys) dto.Pities[pt.poolKey] = pt.count;
        foreach (var fg in res.freeGachas) dto.FreeGachaNextUnix[fg.key] = fg.nextUnix;
        foreach (var c in res.categorys) {
            dto.CategoryNames[c.key] = c.displayName;
            dto.CategoryHasAllTab[c.key] = c.hasAllTab;
        }
        foreach (var o in res.offers) {
            dto.Offers.Add(new ShopOfferDto {
                OfferId = o.offerId,
                DisplayName = o.displayName,
                Description = o.description,
                NameColorHex = o.nameColorHex,
                Major = o.major,
                Sub = o.sub,
                ImageKey = o.imageKey,
                Currency = o.currency,
                Price = o.price,
                IsGacha = o.isGacha,
                GachaIsEquip = o.gachaIsEquip,
                TicketKey = o.ticketKey,
                TicketOfferId = o.ticketOfferId,
                PullPrice = o.pullPrice,
                GachaCount = o.gachaCount,
                PoolKey = o.poolKey,
                HasPickup = o.hasPickup,
                LimitType = o.limitType,
                LimitCount = o.limitCount,
                SoldOut = o.soldOut,
                Locked = o.locked,
                Remaining = o.remaining,
                HasRates = o.hasRates,
                AllowQty = o.allowQty,
                MaxQty = o.maxQty,
                SaleStartUnix = o.saleStartUnix,
                SaleEndUnix = o.saleEndUnix,
                Hot = o.hot,
                Flash = o.flash,
            });
        }
        // 구성품은 리스트 중첩을 피해 평탄하게 오므로 offerId 로 되묶는다(서버 Room 의 offerGrants 와 짝).
        var byOfferId = new Dictionary<string, ShopOfferDto>(dto.Offers.Count);
        foreach (var o in dto.Offers) byOfferId[o.OfferId] = o;
        foreach (var g in res.offerGrants) {
            if (byOfferId.TryGetValue(g.offerId, out var target))
                target.Grants.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        }
        return dto;
    }

    public async UniTask<int> CanBuyOfferAsync(string offerId, int qty, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_CanBuyOffer>(cts.Token);
        _net.Send(new C_CanBuyOffer { offerId = offerId, qty = qty }.Write());
        var res = await wait;
        return res.ok ? 0 : res.failReason;
    }

    public async UniTask<BuyResultDto> BuyOfferAsync(string offerId, string idempotencyKey, int qty = 1, bool free = false,
                                                     IapStoreKind store = IapStoreKind.None, string receipt = null,
                                                     CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_BuyResult>(cts.Token);
        _net.Send(new C_BuyOffer {
            offerId = offerId, idempotencyKey = idempotencyKey, qty = qty, free = free,
            // 현금 상품만 채워 보낸다. 서버는 이 영수증을 스토어에 되물어 확인한 뒤에만 지급한다.
            storeKind = (int)store, receipt = receipt ?? "",
        }.Write());
        var res = await wait;

        var dto = new BuyResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            NewGold = res.newGold,
            NewDiamond = res.newDiamond,
            NewStamina = res.newStamina
        };
        foreach (var r in res.gachaRolls) {
            dto.Rolls.Add(new GachaRollDto {
                CharDataId = r.charDataId,
                IsNew = r.isNew,
                ShardsGained = r.shardsGained,
                MaxExp = r.maxExp
            });
        }
        foreach (var er in res.equipRolls) {
            var roll = new EquipRollDto {
                InstanceId = er.instanceId,
                EquipDataId = er.equipDataId,
                StarLevel = er.starLevel,
                Level = er.level,
                MainPoints = er.mainPoints
            };
            roll.OptionPoints[0] = er.opt1Points;
            roll.OptionPoints[1] = er.opt2Points;
            roll.OptionPoints[2] = er.opt3Points;
            roll.OptionPoints[3] = er.opt4Points;
            dto.EquipRolls.Add(roll);
        }
        // 실제로 지급된 항목(랜덤 확정 결과 포함). 수량 배수는 서버가 이미 반영해 보내므로 여기서 다시 곱하지 않는다.
        foreach (var g in res.grantedRewards)
            dto.Grants.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        return dto;
    }

    public async UniTask<GachaRatesDto> RequestGachaRatesAsync(string offerId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GachaRates>(cts.Token);
        _net.Send(new C_GachaRates { offerId = offerId }.Write());
        var res = await wait;

        var dto = new GachaRatesDto { OfferId = res.offerId };
        foreach (var e in res.entrys) {
            dto.Entries.Add(new GachaRateEntryDto {
                Id = e.id,
                IsEquip = e.isEquip,
                Grade = e.grade,
                Rate = e.rate
            });
        }
        return dto;
    }

    public async UniTask<RotationInfoDto> RequestRotationInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_RotationInfo>(cts.Token);
        _net.Send(new C_RotationInfo().Write());
        var res = await wait;

        var dto = new RotationInfoDto();
        foreach (var e in res.entrys) {
            dto.Entries.Add(new RotationEntryDto {
                IsEquip = e.isEquip,
                Order = e.order,
                Id = e.id,
                Grade = e.grade,
                StartUnix = e.startUnix,
                EndUnix = e.endUnix
            });
        }
        return dto;
    }
}
