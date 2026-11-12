using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>장비 인벤토리 통신 구현(요청/장착/해제/각성/레벨업).</summary>
public sealed class InventoryService : IInventoryService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public InventoryService(INetworkService net) {
        _net = net;
    }

    public async UniTask<InventoryDto> RequestInventoryAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_Inventory>(cts.Token);
        _net.Send(new C_RequestInventory().Write());
        var res = await wait;

        var dto = new InventoryDto();
        foreach (var e in res.equips) {
            var inst = new EquipmentInstanceDto {
                InstanceId = e.instanceId,
                EquipDataId = e.equipDataId,
                StarLevel = e.starLevel,
                Level = e.level,
                MainPoints = e.mainPoints,
                EquippedCharDataId = e.equippedCharDataId,
                LimitBroken = e.limitBroken,
            };
            inst.OptionPoints[0] = e.opt1Points;
            inst.OptionPoints[1] = e.opt2Points;
            inst.OptionPoints[2] = e.opt3Points;
            inst.OptionPoints[3] = e.opt4Points;
            dto.Equipment.Add(inst);
        }
        foreach (var u in res.universalShards) {
            dto.UniversalShards[u.grade] = u.qty;
        }
        return dto;
    }

    public async UniTask<ConvertShardResult> ConvertUniversalShardAsync(int charDataId, int amount, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ConvertUniversalShardResult>(cts.Token);
        _net.Send(new C_ConvertUniversalShard { charDataId = charDataId, amount = amount }.Write());
        var res = await wait;

        return new ConvertShardResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            CharDataId = res.charDataId,
            NewCharShards = res.newCharShards,
            Grade = res.grade,
            NewUniversalQty = res.newUniversalQty
        };
    }

    public async UniTask<EquipActionResult> EquipAsync(long instanceId, int charDataId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EquipResult>(cts.Token);
        _net.Send(new C_EquipItem { instanceId = instanceId, charDataId = charDataId }.Write());
        var res = await wait;
        return new EquipActionResult { IsSuccess = res.isSuccess, FailReason = res.failReason };
    }

    public async UniTask<EquipActionResult> UnequipAsync(long instanceId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_UnequipResult>(cts.Token);
        _net.Send(new C_UnequipItem { instanceId = instanceId }.Write());
        var res = await wait;
        return new EquipActionResult { IsSuccess = res.isSuccess, FailReason = res.failReason };
    }

    public async UniTask<AwakenResultDto> AwakenAsync(long instanceId, IReadOnlyList<long> materialInstanceIds,
                                                      CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var pkt = new C_AwakenEquip { instanceId = instanceId };
        if (materialInstanceIds != null)
            for (int i = 0; i < materialInstanceIds.Count; i++)
                pkt.materials.Add(new C_AwakenEquip.Material { instanceId = materialInstanceIds[i] });

        var wait = _net.WaitPacketAsync<S_AwakenResult>(cts.Token);
        _net.Send(pkt.Write());
        var res = await wait;
        return new AwakenResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            NewStarLevel = res.newStarLevel,
            NewDiamond = res.newDiamond
        };
    }

    public async UniTask<LevelUpResultDto> LevelUpAsync(long instanceId, int count, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_LevelUpEquipResult>(cts.Token);
        _net.Send(new C_LevelUpEquip { instanceId = instanceId, count = count }.Write());
        var res = await wait;
        return new LevelUpResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            NewLevel = res.newLevel,
            NewGold = res.newGold,
            GainedLevels = res.gainedLevels
        };
    }

    public async UniTask<EquipLimitBreakResultDto> LimitBreakAsync(long instanceId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EquipLimitBreakResult>(cts.Token);
        _net.Send(new C_EquipLimitBreak { instanceId = instanceId }.Write());
        var res = await wait;
        return new EquipLimitBreakResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            InstanceId = res.instanceId,
            NewLimitBreakTicket = res.newLimitBreakTicket
        };
    }

    public async UniTask<DisassembleResultDto> DisassembleAsync(IReadOnlyList<long> instanceIds, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var pkt = new C_DisassembleEquip();
        if (instanceIds != null)
            for (int i = 0; i < instanceIds.Count; i++)
                pkt.targets.Add(new C_DisassembleEquip.Target { instanceId = instanceIds[i] });

        var wait = _net.WaitPacketAsync<S_DisassembleEquipResult>(cts.Token);
        _net.Send(pkt.Write());
        return ToDisassembleDto(await wait);
    }

    public async UniTask<DisassembleResultDto> DisassembleByGradeAsync(IReadOnlyList<Grade> grades, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var pkt = new C_DisassembleEquipByGrade();
        if (grades != null)
            for (int i = 0; i < grades.Count; i++)
                pkt.targets.Add(new C_DisassembleEquipByGrade.Target { grade = (int)grades[i] });

        var wait = _net.WaitPacketAsync<S_DisassembleEquipResult>(cts.Token);
        _net.Send(pkt.Write());
        return ToDisassembleDto(await wait);
    }

    // 두 분해 경로가 같은 응답 패킷을 쓰므로 변환도 한 곳에 둔다.
    private static DisassembleResultDto ToDisassembleDto(S_DisassembleEquipResult res) => new() {
        IsSuccess = res.isSuccess,
        FailReason = res.failReason,
        Count = res.count,
        GainedGold = res.gainedGold,
        NewGold = res.newGold
    };
}
