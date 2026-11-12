using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>
/// 원정대 서버 통신 구현. 판정은 서버가 하고 여기서는 패킷 <-> DTO 변환만 한다(쟁탈전 서비스와 같은 구조).
///
/// 오늘 칠 수 있는지(배지 근거)는 현황/입장 응답이 올 때마다 갱신한다. 하루 한 번 자정에만 바뀌는 값이라
/// 시간을 재는 감시가 필요 없고, 응답이 곧 최신 상태다.
/// </summary>
public sealed class ExpeditionService : IExpeditionService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<bool> _canEnterToday = new(false);

    public ExpeditionService(INetworkService net) => _net = net;

    public IReadOnlyReactiveProperty<bool> CanEnterToday => _canEnterToday;
    public int MatchedCharId { get; private set; }

    public async UniTask<ExpeditionHomeDto> RequestHomeAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ExpeditionHome>(cts.Token);
        _net.Send(new C_ExpeditionHome().Write());
        var res = await wait;

        var dto = new ExpeditionHomeDto {
            Matched = res.matched,
            NextMatchUnix = res.nextMatchUnix,
            DayResetUnix = res.dayResetUnix,
            MyAccountId = res.myAccountId,
            EnteredToday = res.enteredToday,
        };
        foreach (var m in res.members) {
            dto.Members.Add(new ExpeditionMemberDto {
                Slot = m.slot, AccountId = m.accountId, Nickname = m.nickname ?? "",
                ProfileFrameId = m.profileFrameId, CharDataId = m.charDataId, CostumeId = m.costumeId,
                Level = m.level, StarLevel = m.starLevel, LimitBroken = m.limitBroken,
                CombatPower = m.combatPower, Mercenary = m.mercenary, EnteredToday = m.enteredToday,
            });
        }
        foreach (var l in res.locations) {
            dto.Locations.Add(new ExpeditionLocationDto {
                Location = (ExpeditionLocation)l.location, Level = l.level, Hp = l.hp, MaxHp = l.maxHp,
                BusyAccountId = l.busyAccountId, BusyNickname = l.busyNickname ?? "",
            });
        }

        MatchedCharId = dto.Me?.CharDataId ?? 0;
        _canEnterToday.Value = dto.CanEnterToday;
        return dto;
    }

    public async UniTask<ExpeditionEnterDto> EnterAsync(ExpeditionLocation location, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_EnterExpeditionResult>(cts.Token);
        _net.Send(new C_EnterExpedition { location = (int)location }.Write());
        var res = await wait;

        // 입장에 성공했으면 오늘 횟수를 쓴 것이므로 배지도 그 자리에서 끈다.
        if (res.isSuccess) _canEnterToday.Value = false;
        return new ExpeditionEnterDto {
            IsSuccess = res.isSuccess, FailReason = res.failReason, Location = (ExpeditionLocation)res.location,
            Level = res.level, Hp = res.hp, MaxHp = res.maxHp, BusyNickname = res.busyNickname ?? "",
        };
    }

    public async UniTask<UserDeckDto> RequestMemberDeckAsync(int slot, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        // 응답은 상대 덱 조회와 같은 S_UserDeck 이다(재현 경로를 하나로 유지하기 위함).
        var wait = _net.WaitPacketAsync<S_UserDeck>(cts.Token);
        _net.Send(new C_ExpeditionMemberDeck { slot = slot }.Write());
        return UserDeckDto.From(await wait);
    }

    public async UniTask<ExpeditionDamageDto> ReportDamageAsync(ExpeditionLocation location, long damage,
                                                                 CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ExpeditionDamageResult>(cts.Token);
        _net.Send(new C_ReportExpeditionDamage { location = (int)location, damage = damage }.Write());
        var res = await wait;

        var dto = new ExpeditionDamageDto {
            IsSuccess = res.isSuccess, FailReason = res.failReason, Location = (ExpeditionLocation)res.location,
            Level = res.level, Hp = res.hp, MaxHp = res.maxHp, LevelUp = res.levelUp,
        };
        foreach (var g in res.grantedRewards)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        return dto;
    }

    public async UniTask<ExpeditionLogDto> RequestLogAsync(CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ExpeditionLog>(cts.Token);
        _net.Send(new C_ExpeditionLog().Write());
        var res = await wait;

        var dto = new ExpeditionLogDto { MyAccountId = res.myAccountId };
        foreach (var e in res.entrys) {
            dto.Entries.Add(new ExpeditionLogEntryDto {
                AccountId = e.accountId, Nickname = e.nickname ?? "", Location = (ExpeditionLocation)e.location,
                Level = e.level, Damage = e.damage, LevelUp = e.levelUp, CreatedUnix = e.createdUnix,
            });
        }
        return dto;
    }

    public void ReportCharacterPower(int charDataId, long combatPower) {
        if (charDataId <= 0 || combatPower <= 0) return;
        _net.Send(new C_ReportCharacterPower { charDataId = charDataId, combatPower = combatPower }.Write());
    }

    public async UniTask RefreshAsync(CancellationToken ct = default) => await RequestHomeAsync(ct);
}
