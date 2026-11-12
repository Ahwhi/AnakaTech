using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 메인 씬 서버 통신 구현. AttendanceService 와 동일하게 대기자를 먼저 등록하고 요청을 보낸 뒤 응답을 받아
/// 순수 DTO 로 변환한다(짧은 타임아웃으로 무한 대기 방지). 생성 패킷(C_/S_)에 의존하는 유일한 계층이다.
/// </summary>
public sealed class MainDataService : IMainDataService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public MainDataService(INetworkService net) {
        _net = net;
    }

    public async UniTask<MainDataDto> EnterMainAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_MainData>(cts.Token);
        _net.Send(new C_EnterMain().Write());
        var res = await wait;

        var dto = new MainDataDto {
            Nickname = res.nickname,
            Gold = res.gold,
            Diamond = res.diamond,
            Stamina = res.stamina,
            StaminaMax = res.staminaMax,
            StaminaBoostTicket = res.staminaBoostTicket,
            StaminaBoostTicketDaily = res.staminaBoostTicketDaily,
            CurrentDungeonIndex = res.currentDungeonIndex,
            HighestClearedDungeonIndex = res.highestClearedDungeonIndex,
            RepresentativeCharId = res.representativeCharId,
            EquippedProfileFrameId = res.equippedProfileFrameId
        };

        foreach (var c in res.characters) {
            dto.Characters.Add(new CharacterProgress {
                CharDataId = c.charDataId,
                Level = c.level,
                Exp = c.exp,
                MaxExp = c.maxExp,
                StarLevel = c.starLevel,
                StatPoints = c.statPoints,
                Shards = c.shards,
                InvestedAttack = c.invAttack,
                InvestedDefense = c.invDefense,
                InvestedHealth = c.invHealth,
                InvestedAttackSpeed = c.invAttackSpeed,
                InvestedMoveSpeed = c.invMoveSpeed,
                InvestedSkillAccel = c.invSkillAccel,
                AutoStatType = c.autoStatType,
                LimitBroken = c.limitBroken
            });
        }

        foreach (var d in res.decks)
            dto.DeckCharIds.Add(d.charDataId);

        return dto;
    }

    public async UniTask<StartDungeonResult> StartDungeonAsync(int dungeonIndex, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_StartDungeonResult>(cts.Token);
        _net.Send(new C_StartDungeon { dungeonIndex = dungeonIndex }.Write());
        var res = await wait;

        return new StartDungeonResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            RemainingStamina = res.remainingStamina
        };
    }

    public async UniTask<DungeonRewardDto> ClearDungeonAsync(int dungeonIndex, bool boost, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DungeonReward>(cts.Token);
        _net.Send(new C_ClearDungeon { dungeonIndex = dungeonIndex, boost = boost }.Write());
        var res = await wait;

        var dto = new DungeonRewardDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            ClearCount = res.clearCount,
            GainedGold = res.gainedGold,
            NewGold = res.newGold,
            NewStamina = res.newStamina,
            NewBoostTicket = res.newBoostTicket,
            StaminaShort = res.staminaShort,
            HighestClearedDungeonIndex = res.highestClearedDungeonIndex
        };
        foreach (var e in res.charExps) {
            dto.CharExp.Add(new CharExpGain {
                CharDataId = e.charDataId,
                GainedExp = e.gainedExp,
                NewLevel = e.newLevel,
                NewExp = e.newExp,
                MaxExp = e.maxExp,
                GainedStatPoints = e.gainedStatPoints
            });
        }
        foreach (var d in res.drops)
            dto.Drops.Add(new ItemDrop { EquipDataId = d.equipDataId, Qty = d.qty });

        return dto;
    }

    public async UniTask<List<DungeonRewardPreview>> GetDungeonRewardPreviewAsync(IReadOnlyList<int> dungeonIndexes, CancellationToken ct = default) {
        var list = new List<DungeonRewardPreview>();
        if (dungeonIndexes == null || dungeonIndexes.Count == 0) return list;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var req = new C_DungeonRewardPreview();
        for (int i = 0; i < dungeonIndexes.Count; i++)
            req.dungeons.Add(new C_DungeonRewardPreview.Dungeon { dungeonIndex = dungeonIndexes[i] });

        var wait = _net.WaitPacketAsync<S_DungeonRewardPreview>(cts.Token);
        _net.Send(req.Write());
        var res = await wait;

        foreach (var p in res.previews) {
            list.Add(new DungeonRewardPreview {
                DungeonIndex = p.dungeonIndex,
                Gold = p.gold,
                ExpPerCharacter = p.expPerCharacter
            });
        }
        return list;
    }

    public async UniTask<ChangeDeckResult> ChangeDeckAsync(IReadOnlyList<int> charIds, int representativeCharId, bool immediate, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var req = new C_ChangeDeck {
            representativeCharId = representativeCharId,
            immediate = immediate
        };
        if (charIds != null) {
            for (int i = 0; i < charIds.Count; i++)
                req.deckChars.Add(new C_ChangeDeck.DeckChar { charDataId = charIds[i] });
        }

        var wait = _net.WaitPacketAsync<S_ChangeDeckResult>(cts.Token);
        _net.Send(req.Write());
        var res = await wait;

        return new ChangeDeckResult { IsSuccess = res.isSuccess, FailReason = res.failReason };
    }

    public async UniTask<GrowthResult> AllocateStatAsync(int charDataId, StatType statType, int count = 1,
                                                         CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_AllocateStatResult>(cts.Token);
        _net.Send(new C_AllocateStat {
            charDataId = charDataId, statType = (int)statType, count = Math.Max(1, count),
        }.Write());
        var res = await wait;

        return new GrowthResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Updated = res.isSuccess ? new CharacterProgress {
                CharDataId = res.charDataId,
                Level = res.level, Exp = res.exp, MaxExp = res.maxExp,
                StarLevel = res.starLevel, StatPoints = res.statPoints, Shards = res.shards,
                InvestedAttack = res.invAttack, InvestedDefense = res.invDefense, InvestedHealth = res.invHealth,
                InvestedAttackSpeed = res.invAttackSpeed, InvestedMoveSpeed = res.invMoveSpeed, InvestedSkillAccel = res.invSkillAccel,
                AutoStatType = res.autoStatType, LimitBroken = res.limitBroken
            } : null
        };
    }

    public async UniTask<StageSweepResult> SweepStageAsync(int count, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_SweepStageResult>(cts.Token);
        _net.Send(new C_SweepStage { count = count }.Write());
        var res = await wait;

        var dto = new StageSweepResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            DungeonIndex = res.dungeonIndex,
            Count = res.count,
            GainedGold = res.gainedGold,
            NewGold = res.newGold,
            NewTicketQty = res.newTicketQty,
        };
        foreach (var e in res.charExps)
            dto.CharExp.Add(new CharExpGain {
                CharDataId = e.charDataId, GainedExp = e.gainedExp,
                NewLevel = e.newLevel, NewExp = e.newExp, MaxExp = e.maxExp,
                GainedStatPoints = e.gainedStatPoints
            });
        return dto;
    }

    public async UniTask<GrowthResult> ResetStatsAsync(int charDataId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ResetStatsResult>(cts.Token);
        _net.Send(new C_ResetStats { charDataId = charDataId }.Write());
        var res = await wait;

        return new GrowthResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            NewDiamond = res.newDiamond,
            Updated = res.isSuccess ? new CharacterProgress {
                CharDataId = res.charDataId,
                Level = res.level, Exp = res.exp, MaxExp = res.maxExp,
                StarLevel = res.starLevel, StatPoints = res.statPoints, Shards = res.shards,
                InvestedAttack = res.invAttack, InvestedDefense = res.invDefense, InvestedHealth = res.invHealth,
                InvestedAttackSpeed = res.invAttackSpeed, InvestedMoveSpeed = res.invMoveSpeed, InvestedSkillAccel = res.invSkillAccel,
                AutoStatType = res.autoStatType, LimitBroken = res.limitBroken
            } : null
        };
    }

    // 자동 투자 설정은 캐릭터의 다른 성장치를 건드리지 않으므로 응답도 대상 스탯만 담는다.
    // 스토어 반영은 호출측이 기존 진행 상태에 이 값만 얹는다(전체 스냅샷 교체 아님).
    public async UniTask<GrowthResult> SetAutoStatAsync(int charDataId, StatType? statType, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        int value = statType.HasValue ? (int)statType.Value : GrowthRules.AutoStatNone;
        var wait = _net.WaitPacketAsync<S_SetAutoStatResult>(cts.Token);
        _net.Send(new C_SetAutoStat { charDataId = charDataId, statType = value }.Write());
        var res = await wait;

        return new GrowthResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Updated = res.isSuccess ? new CharacterProgress {
                CharDataId = res.charDataId,
                AutoStatType = res.autoStatType
            } : null
        };
    }

    public async UniTask<List<DeckPresetDto>> GetDeckPresetsAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DeckPresets>(cts.Token);
        _net.Send(new C_DeckPresets().Write());
        var res = await wait;

        var list = new List<DeckPresetDto>();
        foreach (var p in res.presets)
            list.Add(ToPreset(p.slot, p.representativeCharId, p.charIdsCsv, p.presetName));
        return list;
    }

    public UniTask<DeckPresetResult> SaveDeckPresetAsync(int slot, CancellationToken ct = default) =>
        DeckPresetAsync(new C_SaveDeckPreset { slot = slot }.Write(), ct);

    public UniTask<DeckPresetResult> ApplyDeckPresetAsync(int slot, CancellationToken ct = default) =>
        DeckPresetAsync(new C_ApplyDeckPreset { slot = slot }.Write(), ct);

    public UniTask<DeckPresetResult> RenameDeckPresetAsync(int slot, string name, CancellationToken ct = default) =>
        DeckPresetAsync(new C_RenameDeckPreset { slot = slot, presetName = name ?? "" }.Write(), ct);

    // 저장/적용/이름변경은 응답 패킷(S_DeckPresetResult)이 같아 요청 바이트만 다르다. 대기-전송-변환 흐름을 공유한다.
    private async UniTask<DeckPresetResult> DeckPresetAsync(ArraySegment<byte> request, CancellationToken ct) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_DeckPresetResult>(cts.Token);
        _net.Send(request);
        var res = await wait;

        return new DeckPresetResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            Preset = ToPreset(res.slot, res.representativeCharId, res.charIdsCsv, res.presetName)
        };
    }

    // 서버는 편성을 CSV 로 저장한다(PlayerDeck 과 같은 형식). 파싱은 이 계층에서만 하고 위로는 목록으로 넘긴다.
    private static DeckPresetDto ToPreset(int slot, int representativeCharId, string csv, string name) {
        var dto = new DeckPresetDto { Slot = slot, RepresentativeCharId = representativeCharId, Name = name ?? "" };
        if (string.IsNullOrEmpty(csv)) return dto;

        foreach (var part in csv.Split(','))
            if (int.TryParse(part.Trim(), out int id)) dto.CharIds.Add(id);
        return dto;
    }

    public async UniTask<GrowthResult> UpgradeStarAsync(int charDataId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_UpgradeStarResult>(cts.Token);
        _net.Send(new C_UpgradeStar { charDataId = charDataId }.Write());
        var res = await wait;

        return new GrowthResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            NewDiamond = res.newDiamond,
            Updated = res.isSuccess ? new CharacterProgress {
                CharDataId = res.charDataId,
                Level = res.level, Exp = res.exp, MaxExp = res.maxExp,
                StarLevel = res.starLevel, StatPoints = res.statPoints, Shards = res.shards,
                InvestedAttack = res.invAttack, InvestedDefense = res.invDefense, InvestedHealth = res.invHealth,
                InvestedAttackSpeed = res.invAttackSpeed, InvestedMoveSpeed = res.invMoveSpeed, InvestedSkillAccel = res.invSkillAccel,
                AutoStatType = res.autoStatType, LimitBroken = res.limitBroken
            } : null
        };
    }

    public async UniTask<GrowthResult> LimitBreakAsync(int charDataId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_LimitBreakResult>(cts.Token);
        _net.Send(new C_LimitBreak { charDataId = charDataId }.Write());
        var res = await wait;

        return new GrowthResult {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            NewLimitBreakTicket = res.newLimitBreakTicket,
            Updated = res.isSuccess ? new CharacterProgress {
                CharDataId = res.charDataId,
                Level = res.level, Exp = res.exp, MaxExp = res.maxExp,
                StarLevel = res.starLevel, StatPoints = res.statPoints, Shards = res.shards,
                InvestedAttack = res.invAttack, InvestedDefense = res.invDefense, InvestedHealth = res.invHealth,
                InvestedAttackSpeed = res.invAttackSpeed, InvestedMoveSpeed = res.invMoveSpeed, InvestedSkillAccel = res.invSkillAccel,
                AutoStatType = res.autoStatType, LimitBroken = res.limitBroken
            } : null
        };
    }
}
