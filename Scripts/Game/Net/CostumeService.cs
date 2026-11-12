using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 코스튬 서버 통신 구현. 보유는 계정 공용이고 장착만 캐릭터별이라, 각인과 같이 조회 1회로 두 정보를 함께 받는다.
/// </summary>
public sealed class CostumeService : ICostumeService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public CostumeService(INetworkService net) => _net = net;

    public async UniTask<CostumeInfoDto> RequestInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_CostumeInfo>(cts.Token);
        _net.Send(new C_CostumeInfo().Write());
        var res = await wait;

        var dto = new CostumeInfoDto();
        foreach (var o in res.ownedCostumes) dto.Owned.Add(o.costumeId);
        foreach (var e in res.equippedCostumes)
            dto.Equipped.Add(new EquippedCostumeDto { CharDataId = e.charDataId, CostumeId = e.costumeId });
        foreach (var o in res.ownedUnitSkins) dto.OwnedUnitSkins[o.unitSkinId] = o.count;
        foreach (var e in res.equippedUnitSkins) dto.EquippedUnitSkins[e.charDataId] = e.unitSkinId;
        return dto;
    }

    public async UniTask<UnitSkinEquipResult> EquipUnitSkinAsync(int charDataId, int unitSkinId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EquipUnitSkinResult>(cts.Token);
        _net.Send(new C_EquipUnitSkin { charDataId = charDataId, unitSkinId = unitSkinId }.Write());
        var res = await wait;

        return new UnitSkinEquipResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            CharDataId = res.charDataId,
            UnitSkinId = res.unitSkinId
        };
    }

    public async UniTask<CostumeEquipResult> EquipAsync(int charDataId, int costumeId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EquipCostumeResult>(cts.Token);
        _net.Send(new C_EquipCostume { charDataId = charDataId, costumeId = costumeId }.Write());
        var res = await wait;

        return new CostumeEquipResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            CharDataId = res.charDataId,
            CostumeId = res.costumeId
        };
    }
}
