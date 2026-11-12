using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>원정대 팝업 현황(이번 주 파티 + 장소 5곳 + 오늘 입장 여부).</summary>
public sealed class ExpeditionHomeDto {
    public bool Matched;
    public long NextMatchUnix;
    public long DayResetUnix;
    public int MyAccountId;
    public bool EnteredToday;
    public List<ExpeditionMemberDto> Members = new();
    public List<ExpeditionLocationDto> Locations = new();

    /// <summary>내 파티원 행(용병 자리 제외). 매칭되지 않았으면 null.</summary>
    public ExpeditionMemberDto Me {
        get {
            foreach (var m in Members) if (m.AccountId == MyAccountId && !m.Mercenary) return m;
            return null;
        }
    }

    public ExpeditionLocationDto LocationOf(ExpeditionLocation location) {
        foreach (var l in Locations) if (l.Location == location) return l;
        return null;
    }

    /// <summary>오늘 아직 칠 수 있는지(매칭됨 + 오늘 미입장 + 클리어하지 않은 장소가 남음). 배지의 근거다.</summary>
    public bool CanEnterToday {
        get {
            if (!Matched || EnteredToday) return false;
            foreach (var l in Locations) if (!l.IsCleared) return true;
            return false;
        }
    }
}

/// <summary>파티원 1명(매칭 시점에 고정된 대표 캐릭터).</summary>
public sealed class ExpeditionMemberDto {
    public int Slot;
    public int AccountId;
    public string Nickname = "";
    public int ProfileFrameId;
    public int CharDataId;
    public int CostumeId;
    public int Level;
    public int StarLevel;
    public bool LimitBroken;
    public long CombatPower;
    /// <summary>남는 자리를 채운 용병. 전투에만 함께 나서며 이 파티에서 입장/주간 보상은 없다.</summary>
    public bool Mercenary;
    public bool EnteredToday;
}

/// <summary>장소 1곳의 진행 상태.</summary>
public sealed class ExpeditionLocationDto {
    public ExpeditionLocation Location;
    public int Level;
    public long Hp;
    public long MaxHp;
    /// <summary>지금 이 장소를 치고 있는 파티원(0=없음).</summary>
    public int BusyAccountId;
    public string BusyNickname = "";

    public bool IsCleared => ExpeditionRules.IsCleared(Level);
    public bool IsBusy => BusyAccountId != 0;
    public float HpRatio => MaxHp > 0 ? UnityEngine.Mathf.Clamp01((float)Hp / MaxHp) : 0f;
}

public sealed class ExpeditionEnterDto {
    public bool IsSuccess;
    public int FailReason;
    public ExpeditionLocation Location;
    public int Level;
    public long Hp;
    public long MaxHp;
    public string BusyNickname = "";
}

public sealed class ExpeditionDamageDto {
    public bool IsSuccess;
    public int FailReason;
    public ExpeditionLocation Location;
    public int Level;
    public long Hp;
    public long MaxHp;
    public bool LevelUp;
    public List<RewardItem> Granted = new();
}

public sealed class ExpeditionLogDto {
    public int MyAccountId;
    public List<ExpeditionLogEntryDto> Entries = new();
}

public sealed class ExpeditionLogEntryDto {
    public int AccountId;
    public string Nickname = "";
    public ExpeditionLocation Location;
    /// <summary>도전 당시 레벨.</summary>
    public int Level;
    public long Damage;
    public bool LevelUp;
    public long CreatedUnix;
}

/// <summary>원정대 서버 통신. 판정은 서버가 하고 여기서는 패킷 <-> DTO 변환과 배지 근거 갱신만 한다.</summary>
public interface IExpeditionService {
    /// <summary>오늘 칠 수 있는지(배지 근거). 현황 조회/입장 응답이 올 때마다 갱신된다.</summary>
    IReadOnlyReactiveProperty<bool> CanEnterToday { get; }

    /// <summary>이번 주 내게 묶인 캐릭터(0=매칭 없음/아직 모름). 대표를 바꿔도 이 캐릭터의 전투력을 계속 보고하는 데 쓴다.</summary>
    int MatchedCharId { get; }

    UniTask<ExpeditionHomeDto> RequestHomeAsync(CancellationToken ct = default);
    UniTask<ExpeditionEnterDto> EnterAsync(ExpeditionLocation location, CancellationToken ct = default);
    /// <summary>파티원 1명의 전투 재현용 편성(서버 스냅샷). 비어 있으면 Members 가 0 개다.</summary>
    UniTask<UserDeckDto> RequestMemberDeckAsync(int slot, CancellationToken ct = default);
    UniTask<ExpeditionDamageDto> ReportDamageAsync(ExpeditionLocation location, long damage, CancellationToken ct = default);
    UniTask<ExpeditionLogDto> RequestLogAsync(CancellationToken ct = default);

    /// <summary>캐릭터 1명의 전투력 보고(응답 없음). 매칭 자격과 파티원 스냅샷 갱신의 근거다.</summary>
    void ReportCharacterPower(int charDataId, long combatPower);

    /// <summary>배지 근거만 최신화한다(BadgeCenter 가 부른다).</summary>
    UniTask RefreshAsync(CancellationToken ct = default);
}
