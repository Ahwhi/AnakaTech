using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 길드 통신 구현. 패킷을 주고받아 DTO 로 변환한다(다른 서비스와 동일한 WaitPacketAsync + Send 패턴).
/// 판정은 전부 서버가 하므로 여기서는 값을 옮기기만 하고 규칙을 재현하지 않는다.
/// </summary>
public sealed class GuildService : IGuildService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    public GuildService(INetworkService net) {
        _net = net;
    }

    public async UniTask<GuildHomeDto> RequestHomeAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GuildHome>(cts.Token);
        _net.Send(new C_GuildHome().Write());
        var res = await wait;

        var dto = new GuildHomeDto {
            GuildId = res.guildId,
            MyRole = (GuildRole)res.myRole,
            PendingApplications = res.pendingApplications,
            MyCombatPower = res.myCombatPower,
            Notice = res.notice,
            MasterNickname = res.masterNickname,
            CreatedUnix = res.createdUnix,
            ExpInLevel = res.expInLevel,
            ExpPerLevel = res.expPerLevel,
            RaidFreeEntries = res.raidFreeEntries,
        };

        foreach (var a in res.appliedGuilds) dto.AppliedGuildIds.Add(a.guildId);
        foreach (var r in res.recommends) {
            dto.Recommends.Add(new GuildSummaryDto {
                GuildId = r.guildId, Name = r.guildName, MarkId = r.markId, Level = r.level,
                CombatPower = r.combatPower, MemberCount = r.memberCount,
                JoinType = (GuildJoinType)r.joinType, Intro = r.intro, MasterNickname = r.masterNickname,
            });
        }

        if (dto.HasGuild) {
            dto.Summary = new GuildSummaryDto {
                GuildId = res.guildId, Name = res.guildName, MarkId = res.markId, Level = res.level,
                CombatPower = res.combatPower, MemberCount = res.memberCount,
                JoinType = (GuildJoinType)res.joinType, Intro = res.intro, MasterNickname = res.masterNickname,
            };
            foreach (var m in res.unlockedMarks) dto.UnlockedMarkIds.Add(m.markId);
            foreach (var m in res.members) {
                dto.Members.Add(new GuildMemberDto {
                    AccountId = m.accountId, Nickname = m.nickname, Role = (GuildRole)m.role,
                    ProfileFrameId = m.profileFrameId, RepresentativeCharId = m.representativeCharId,
                    RepresentativeCostumeId = m.representativeCostumeId,
                    CombatPower = m.combatPower, JoinedUnix = m.joinedUnix, Contribution = m.contribution,
                    LastLoginUnix = m.lastLoginUnix, IsOnline = m.online,
                });
            }
        }
        return dto;
    }

    // 목록 조회의 세 갈래(추천/검색/단건). 값은 서버 Room.GuildList 의 mode 상수와 일치해야 한다.
    private const int ListModeRecommend = 0;
    private const int ListModeSearch = 1;
    private const int ListModeProfile = 2;

    public async UniTask<List<GuildSummaryDto>> RequestListAsync(bool search, string keyword = "", CancellationToken ct = default) =>
        await RequestGuildsAsync(search ? ListModeSearch : ListModeRecommend, keyword, 0, ct);

    public async UniTask<GuildSummaryDto> RequestGuildAsync(int guildId, CancellationToken ct = default) {
        if (guildId <= 0) return null;
        var list = await RequestGuildsAsync(ListModeProfile, "", guildId, ct);
        // 해체된 길드는 서버가 빈 목록을 준다(찾지 못함).
        return list.Count > 0 ? list[0] : null;
    }

    // 세 갈래가 같은 응답(S_GuildList)을 쓰므로 요청/변환도 한 자리에 둔다.
    private async UniTask<List<GuildSummaryDto>> RequestGuildsAsync(int mode, string keyword, int guildId,
                                                                    CancellationToken ct) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GuildList>(cts.Token);
        _net.Send(new C_GuildList { mode = mode, keyword = keyword ?? "", guildId = guildId }.Write());
        var res = await wait;

        var list = new List<GuildSummaryDto>(res.guilds.Count);
        foreach (var g in res.guilds) {
            list.Add(new GuildSummaryDto {
                GuildId = g.guildId, Name = g.guildName, MarkId = g.markId, Level = g.level,
                CombatPower = g.combatPower, MemberCount = g.memberCount,
                JoinType = (GuildJoinType)g.joinType, Intro = g.intro, MasterNickname = g.masterNickname,
                CreatedUnix = g.createdUnix, Rank = g.rank,
            });
        }
        return list;
    }

    public async UniTask<int> CheckNameAsync(string guildName, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GuildNameCheck>(cts.Token);
        _net.Send(new C_GuildNameCheck { guildName = guildName ?? "" }.Write());
        var res = await wait;
        return res.failReason;
    }

    public async UniTask<GuildActionResultDto> CreateAsync(string guildName, GuildJoinType joinType, long combatPower,
                                                          CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GuildActionResult>(cts.Token);
        _net.Send(new C_CreateGuild {
            guildName = guildName ?? "", joinType = (int)joinType, combatPower = combatPower,
        }.Write());
        return ToResult(await wait);
    }

    public async UniTask<GuildActionResultDto> ActAsync(GuildActionType action, int guildId = 0, int targetAccountId = 0,
                                                       int intValue = 0, string textValue = "", long combatPower = 0,
                                                       CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GuildActionResult>(cts.Token);
        _net.Send(new C_GuildAction {
            action = (int)action, guildId = guildId, targetAccountId = targetAccountId,
            intValue = intValue, textValue = textValue ?? "", combatPower = combatPower,
        }.Write());
        return ToResult(await wait);
    }

    public async UniTask<List<GuildApplicationDto>> RequestApplicationsAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GuildApplications>(cts.Token);
        _net.Send(new C_GuildApplications().Write());
        var res = await wait;

        var list = new List<GuildApplicationDto>(res.applications.Count);
        foreach (var a in res.applications) {
            list.Add(new GuildApplicationDto {
                AccountId = a.accountId, Nickname = a.nickname,
                ProfileFrameId = a.profileFrameId, RepresentativeCharId = a.representativeCharId,
                RepresentativeCostumeId = a.representativeCostumeId,
                CombatPower = a.combatPower, AppliedUnix = a.appliedUnix,
            });
        }
        return list;
    }

    public async UniTask<UserProfileDto> RequestUserProfileAsync(int accountId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_UserProfile>(cts.Token);
        _net.Send(new C_UserProfile { accountId = accountId }.Write());
        var res = await wait;

        var dto = new UserProfileDto {
            AccountId = res.accountId,
            Nickname = res.nickname,
            ProfileFrameId = res.profileFrameId,
            RepresentativeCharId = res.representativeCharId,
            RepresentativeCostumeId = res.representativeCostumeId,
            CombatPower = res.combatPower,
            HighestClearedDungeonIndex = res.highestClearedDungeonIndex,
            CreatedUnix = res.createdUnix,
            GuildName = res.guildName,
            GuildMarkId = res.guildMarkId,
            GuildRole = (GuildRole)res.guildRole,
            Relation = (FriendRelation)res.relation,
            Blocked = res.blocked,
            CanWhisper = res.canWhisper,
            AchievementScore = res.achievementScore,
            CombatPowerRank = res.combatPowerRank,
            AchievementRank = res.achievementRank,
        };
        // 편성은 서버가 보낸 순서를 그대로 유지한다(첫 칸이 대표라 순서 자체가 정보다).
        foreach (var m in res.deckMembers)
            dto.DeckMembers.Add(new UserProfileDeckMemberDto {
                CharDataId = m.charDataId, StarLevel = m.starLevel, Level = m.level,
                CostumeId = m.costumeId,
            });
        return dto;
    }

    // 응답을 기다리지 않는 단방향 통지. 실패해도 다음 보고가 덮으므로 재시도하지 않는다.
    public void ReportCombatPower(long combatPower) {
        if (combatPower < 0) return;
        _net.Send(new C_ReportCombatPower { combatPower = combatPower }.Write());
    }

    // ── 길드 레이드 ──
    public async UniTask<GuildRaidStateDto> RequestRaidStateAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GuildRaidState>(cts.Token);
        _net.Send(new C_GuildRaidState().Write());
        var res = await wait;

        var dto = new GuildRaidStateDto {
            HasGuild = res.hasGuild,
            BossLevel = res.bossLevel,
            BossHp = res.bossHp,
            BossMaxHp = res.bossMaxHp,
            MyEntries = res.myEntries,
            MaxEntries = res.maxEntries,
            NextEntryCost = res.nextEntryCost,
            MyDamageToday = res.myDamageToday,
            ResetUnix = res.resetUnix,
            OccupantNickname = res.occupantNickname ?? "",
        };
        foreach (var c in res.contributions) {
            dto.Contributions.Add(new GuildRaidContributionDto {
                AccountId = c.accountId, Nickname = c.nickname,
                ProfileFrameId = c.profileFrameId, RepresentativeCharId = c.representativeCharId,
                RepresentativeCostumeId = c.representativeCostumeId,
                Damage = c.damage, Entries = c.entries,
            });
        }
        return dto;
    }

    public async UniTask<GuildRaidEnterDto> EnterRaidAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EnterGuildRaidResult>(cts.Token);
        _net.Send(new C_EnterGuildRaid().Write());
        var res = await wait;

        return new GuildRaidEnterDto {
            IsSuccess = res.isSuccess, FailReason = res.failReason,
            BossLevel = res.bossLevel, BossHp = res.bossHp, BossMaxHp = res.bossMaxHp,
            NewDiamond = res.newDiamond,
            OccupantNickname = res.occupantNickname ?? "",
        };
    }

    // 단방향 보고라 기다릴 응답이 없다(의뢰 도전 종료 보고와 같은 방식).
    public void ReportRaidFinished() =>
        _net.Send(new C_BattleFinished { content = BattleFinishContent.GuildRaid }.Write());

    public async UniTask<GuildRaidDamageDto> ReportRaidDamageAsync(long damage, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GuildRaidDamageResult>(cts.Token);
        _net.Send(new C_ReportGuildRaidDamage { damage = damage }.Write());
        var res = await wait;

        var dto = new GuildRaidDamageDto {
            IsSuccess = res.isSuccess, FailReason = res.failReason,
            BossLevel = res.bossLevel, BossHp = res.bossHp, BossMaxHp = res.bossMaxHp,
            BossDefeated = res.bossDefeated,
            DefeatedCount = res.defeatedCount,
        };
        foreach (var g in res.grantedRewards)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        return dto;
    }

    // ── 길드 대항전 ──
    public async UniTask<List<GuildWarLogDto>> RequestWarLogsAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GuildWarLog>(cts.Token);
        _net.Send(new C_GuildWarLog().Write());
        var res = await wait;

        var list = new List<GuildWarLogDto>(res.entrys.Count);
        foreach (var e in res.entrys) {
            list.Add(new GuildWarLogDto {
                AttackerAccountId = e.attackerAccountId, AttackerNickname = e.attackerNickname,
                AttackerGuildId = e.attackerGuildId,
                TargetAccountId = e.targetAccountId, TargetNickname = e.targetNickname,
                TargetGuildId = e.targetGuildId,
                AttackerWon = e.attackerWon, CreatedUnix = e.createdUnix,
            });
        }
        return list;
    }

    public async UniTask<GuildWarStateDto> RequestWarStateAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GuildWarState>(cts.Token);
        _net.Send(new C_GuildWarState().Write());
        var res = await wait;

        var dto = new GuildWarStateDto {
            Phase = (GuildWarPhase)res.phase,
            MatchId = res.matchId,
            BattleStartUnix = res.battleStartUnix,
            BattleEndUnix = res.battleEndUnix,
            NextMatchUnix = res.nextMatchUnix,
            NoMatchReason = (GuildWarNoMatchReason)res.noMatchReason,
            MyAccountId = res.myAccountId,
            MyAttacksLeft = res.myAttacksLeft,
            MyDead = res.myDead,
            WinnerGuildId = res.winnerGuildId,
            Mine = new GuildWarSideDto {
                GuildId = res.myGuildId, GuildName = res.myGuildName, MarkId = res.myMarkId,
                AliveCount = res.myAliveCount, TotalCount = res.myTotalCount,
            },
            Enemy = new GuildWarSideDto {
                GuildId = res.enemyGuildId, GuildName = res.enemyGuildName, MarkId = res.enemyMarkId,
                AliveCount = res.enemyAliveCount, TotalCount = res.enemyTotalCount,
            },
        };

        // 참가자는 한 리스트로 오므로 guildId 로 양쪽에 나눠 담는다.
        foreach (var p in res.participants) {
            var member = new GuildWarParticipantDto {
                GuildId = p.guildId, AccountId = p.accountId, Nickname = p.nickname,
                ProfileFrameId = p.profileFrameId, RepresentativeCharId = p.representativeCharId,
                RepresentativeCostumeId = p.representativeCostumeId,
                CombatPower = p.combatPower, Dead = p.dead, AttacksUsed = p.attacksUsed,
            };
            if (p.guildId == dto.Mine.GuildId) dto.Mine.Members.Add(member);
            else dto.Enemy.Members.Add(member);
        }
        return dto;
    }

    public async UniTask<GuildWarAttackBeginDto> BeginWarAttackAsync(int targetAccountId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_BeginGuildWarAttackResult>(cts.Token);
        _net.Send(new C_BeginGuildWarAttack { targetAccountId = targetAccountId }.Write());
        var res = await wait;

        return new GuildWarAttackBeginDto {
            IsSuccess = res.isSuccess, FailReason = res.failReason,
            TargetAccountId = res.targetAccountId, AttacksLeft = res.attacksLeft,
        };
    }

    public async UniTask<GuildWarAttackResultDto> ReportWarAttackAsync(int targetAccountId, bool win, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_GuildWarAttackResult>(cts.Token);
        _net.Send(new C_ReportGuildWarAttack { targetAccountId = targetAccountId, win = win }.Write());
        var res = await wait;

        var dto = new GuildWarAttackResultDto {
            IsSuccess = res.isSuccess, FailReason = res.failReason, TargetAccountId = res.targetAccountId,
            TargetDead = res.targetDead, MyAliveCount = res.myAliveCount, EnemyAliveCount = res.enemyAliveCount,
        };
        foreach (var g in res.grantedRewards)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        return dto;
    }

    public async UniTask<UserDeckDto> RequestUserDeckAsync(int accountId, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_UserDeck>(cts.Token);
        _net.Send(new C_UserDeck { accountId = accountId }.Write());
        return UserDeckDto.From(await wait);
    }

    private static GuildActionResultDto ToResult(S_GuildActionResult res) => new() {
        IsSuccess = res.isSuccess,
        FailReason = res.failReason,
        Action = (GuildActionType)res.action,
        GuildId = res.guildId,
        TargetAccountId = res.targetAccountId,
        NewDiamond = res.newDiamond,
    };
}
