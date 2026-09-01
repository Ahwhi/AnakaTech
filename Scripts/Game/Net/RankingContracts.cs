using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 랭킹 통신 계약과 순수 DTO(생성 패킷 비의존). 랭킹 UI 는 이 타입들만 참조한다.

/// <summary>랭킹 종류. 값은 서버 RankingCategory 와 일치해야 한다.</summary>
public enum RankingCategory {
    UserCombatPower = 0,   // 유저 랭킹(덱 전투력)
    GuildCombatPower = 1,  // 길드 랭킹(길드 전투력)
    Achievement = 2,       // 업적 랭킹(업적 총점)
    ArenaScore = 3         // 쟁탈전 랭킹(시즌 점수). 월말에 점수가 초기화되면 순위표도 함께 비워진다.
}

/// <summary>
/// 순위표 한 줄. 유저 랭킹과 길드 랭킹이 같은 형태를 쓰고, 이름 두 칸과 아이콘 한 칸의 의미만 달라진다
/// (서버가 스냅샷을 만들 때 이미 그 자리에 맞는 값을 넣어 보낸다).
/// </summary>
public sealed class RankingEntryDto {
    public int Rank;
    /// <summary>유저 랭킹은 계정 id, 길드 랭킹은 길드 id.</summary>
    public int TargetId;
    public long Score;
    /// <summary>유저 닉네임 또는 길드 이름.</summary>
    public string Name = "";
    /// <summary>유저 = 소속 길드 이름(무소속이면 빈 값), 길드 = 길드장 닉네임.</summary>
    public string SubName = "";
    /// <summary>유저 = 프로필 테두리 id, 길드 = 길드 마크 id.</summary>
    public int IconId;
    /// <summary>유저 = 소속 길드의 마크 id(무소속이면 0). 보조 이름과 같은 짝이라 길드 랭킹에서는 쓰지 않는다.</summary>
    public int SubIconId;
    /// <summary>유저 랭킹에서만 쓰는 대표 캐릭터.</summary>
    public int RepresentativeCharId;
    /// <summary>대표 캐릭터가 장착한 코스튬(0=기본 일러스트). 남의 코스튬은 서버만 알고 있어 함께 내려온다.</summary>
    public int RepresentativeCostumeId;
}

/// <summary>순위표 1개(상위 목록 + 내 순위).</summary>
public sealed class RankingBoardDto {
    public RankingCategory Category;
    /// <summary>이 순위표를 만든 시각(unix). 0 이면 아직 집계되지 않았다.</summary>
    public long UpdatedUnix;
    /// <summary>내(또는 내 길드) 순위. 0 이면 순위권 밖이다.</summary>
    public int MyRank;
    public long MyScore;
    public List<RankingEntryDto> Entries = new();

    public bool HasMyRank => MyRank > 0;
}

/// <summary>랭킹 통신 추상화. UI 는 이 계약에만 의존한다.</summary>
public interface IRankingService {
    UniTask<RankingBoardDto> RequestBoardAsync(RankingCategory category, CancellationToken ct = default);
}
