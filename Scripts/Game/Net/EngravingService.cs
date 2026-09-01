using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 각인 서버 통신 구현. 보유/레벨은 계정 공용이고 장착만 캐릭터별이라, 조회 1회로 두 정보를 함께 받는다.
/// </summary>
public sealed class EngravingService : IEngravingService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public EngravingService(INetworkService net) => _net = net;

    public async UniTask<EngravingInfoDto> RequestInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EngravingInfo>(cts.Token);
        _net.Send(new C_EngravingInfo().Write());
        var res = await wait;

        var dto = new EngravingInfoDto { EngravingStone = res.engravingStone };
        foreach (var e in res.engravings)
            dto.Owned.Add(new OwnedEngravingDto { EngravingId = e.engravingId, Level = e.level });
        foreach (var s in res.slots)
            dto.Equipped.Add(new EquippedEngravingDto {
                CharDataId = s.charDataId, SlotIndex = s.slotIndex, EngravingId = s.engravingId
            });
        return dto;
    }

    public async UniTask<EngravingUpgradeResult> UpgradeAsync(int engravingId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_UpgradeEngravingResult>(cts.Token);
        _net.Send(new C_UpgradeEngraving { engravingId = engravingId }.Write());
        var res = await wait;

        return new EngravingUpgradeResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            EngravingId = res.engravingId,
            Level = res.level,
            NewEngravingStone = res.newEngravingStone
        };
    }

    public async UniTask<EngravingEquipResult> EquipAsync(int charDataId, int slotIndex, int engravingId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EquipEngravingResult>(cts.Token);
        _net.Send(new C_EquipEngraving {
            charDataId = charDataId, slotIndex = slotIndex, engravingId = engravingId
        }.Write());
        var res = await wait;

        return new EngravingEquipResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            CharDataId = res.charDataId,
            SlotIndex = res.slotIndex,
            EngravingId = res.engravingId
        };
    }
}

/// <summary>용병단 본부 서버 통신 구현.</summary>
public sealed class HeadquartersService : IHeadquartersService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public HeadquartersService(INetworkService net) => _net = net;

    public async UniTask<HeadquartersDto> RequestInfoAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_HeadquartersInfo>(cts.Token);
        _net.Send(new C_HeadquartersInfo().Write());
        var res = await wait;

        var dto = new HeadquartersDto { Renown = res.renown };
        foreach (var n in res.nodes)
            dto.Nodes.Add(new HqNodeDto { NodeId = n.nodeId, Level = n.level });
        return dto;
    }

    public async UniTask<HqUpgradeResult> UpgradeNodeAsync(int nodeId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_UpgradeHqNodeResult>(cts.Token);
        _net.Send(new C_UpgradeHqNode { nodeId = nodeId }.Write());
        var res = await wait;

        return new HqUpgradeResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            NodeId = res.nodeId,
            Level = res.level,
            NewRenown = res.newRenown
        };
    }
}
