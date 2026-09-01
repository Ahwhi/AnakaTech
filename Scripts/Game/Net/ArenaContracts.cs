using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

// 쟁탈전 통신 계약과 순수 DTO(생성 패킷 비의존). 쟁탈전 UI/씬은 이 타입들만 참조한다.

/// <summary>쟁탈전 팝업 진입 스냅샷.</summary>
public sealed class ArenaHomeDto {
    public int Coin;
    public int Score;
    public int Wins;
    public int Losses;
    /// <summary>랭킹 스냅샷 기준 내 순위(0 = 순위권 밖 또는 아직 미집계).</summary>
    public int Rank;
    /// <summary>오늘 사용한 매칭 횟수(무료 + 유료).</summary>
    public int UsedEntries;
    public int FreeEntries;
    public int MaxEntries;
    /// <summary>다음 매칭 비용(다이아). 0=무료, -1=더 못 함.</summary>
    public int NextEntryCost;
    /// <summary>시즌 종료 시각(월말 23:59:59 KST). 카운트다운 기준.</summary>
    public long SeasonEndUnix;
    /// <summary>내가 방어자인 가장 최근 기록의 id(0 = 없음). 내역 버튼 알림의 근거다.</summary>
    public long LastDefenseLogId;

    /// <summary>지금 매칭할 수 있는지(횟수가 남아 있는지).</summary>
    public bool CanMatch => NextEntryCost >= 0;
}

/// <summary>매칭 결과. 성공하면 매칭 횟수가 이미 소모된 상태다.</summary>
public sealed class ArenaMatchResultDto {
    public bool IsSuccess;
    public int FailReason;
    public int OpponentAccountId;
    public string OpponentNickname = "";
    public long OpponentCombatPower;
    public int OpponentScore;
    public int OpponentProfileFrameId;
    public int OpponentRepresentativeCharId;
    /// <summary>상대 대표 캐릭터가 장착한 코스튬(0=기본 일러스트).</summary>
    public int OpponentRepresentativeCostumeId;
    public int NewDiamond;
    public int UsedEntries;
    public int NextEntryCost;
}

/// <summary>전투 결과 보고의 반영 결과.</summary>
public sealed class ArenaReportResultDto {
    public bool IsSuccess;
    public int FailReason;
    public bool Win;
    public int ScoreDelta;
    public int NewScore;
    public int NewCoin;
    public int Wins;
    public int Losses;
    public List<RewardItem> Granted = new();
}

/// <summary>쟁탈전 내역 1줄(공격자 기준 기록). 내가 공격자일 수도, 방어자일 수도 있다.</summary>
public sealed class ArenaLogEntryDto {
    public int AttackerAccountId;
    public string AttackerNickname = "";
    public int DefenderAccountId;
    public string DefenderNickname = "";
    public bool AttackerWon;
    /// <summary>공격자의 점수 변동(승리 +, 패배 -). 방어자는 정확히 그 반대만큼 움직인다.</summary>
    public int ScoreDelta;
    public long CreatedUnix;

    /// <summary>내 기준 점수 변동. 기록이 공격자 기준이라 내가 방어자면 부호를 뒤집는다.</summary>
    public int MyScoreDelta(int myAccountId) => IsAttacker(myAccountId) ? ScoreDelta : -ScoreDelta;

    /// <summary>내가 공격한 기록인지(아니면 내가 방어자인 기록).</summary>
    public bool IsAttacker(int myAccountId) => AttackerAccountId == myAccountId;

    /// <summary>내 기준 승패. 공격자면 공격자 승패 그대로, 방어자면 뒤집는다.</summary>
    public bool DidIWin(int myAccountId) => IsAttacker(myAccountId) ? AttackerWon : !AttackerWon;

    /// <summary>내 기준 상대 이름.</summary>
    public string OpponentName(int myAccountId) => IsAttacker(myAccountId) ? DefenderNickname : AttackerNickname;
}

/// <summary>
/// 쟁탈전 내역 응답. 기록이 공격자 기준으로 저장되므로 "내가 친 것"인지 가르려면 내 계정 id 가 필요하다.
/// 클라가 자기 계정 id 를 따로 들고 있지 않아도 되도록 서버가 함께 보낸다(대항전 현황과 같은 방식).
/// </summary>
public sealed class ArenaLogDto {
    public int MyAccountId;
    public List<ArenaLogEntryDto> Entries = new();
}

/// <summary>쟁탈전 통신 추상화. UI 는 이 계약에만 의존한다.</summary>
public interface IArenaService {
    /// <summary>
    /// 오늘 무료 매칭이 남아 있는지. 메인 쟁탈전 버튼 배지가 구독한다.
    /// 홈 조회/매칭 응답이 이 값을 갱신하므로 배지가 따로 서버를 두드리지 않는다.
    /// </summary>
    IReadOnlyReactiveProperty<bool> HasFreeEntry { get; }

    UniTask<ArenaHomeDto> RequestHomeAsync(CancellationToken ct = default);

    /// <summary>매칭. 성공하면 횟수가 이미 소모됐으므로 반드시 전투로 넘어가야 한다.</summary>
    UniTask<ArenaMatchResultDto> MatchAsync(CancellationToken ct = default);

    /// <summary>전투 결과 보고. hpPercentTenth = 점수 기준 체력(백분율 x 10, ArenaRules.ToHpPercentTenth).</summary>
    UniTask<ArenaReportResultDto> ReportAsync(int targetAccountId, bool win, int hpPercentTenth,
                                              CancellationToken ct = default);

    UniTask<ArenaLogDto> RequestLogAsync(CancellationToken ct = default);

    /// <summary>무료 매칭 잔여 여부만 최신화한다(배지 초기 로드용). 실패해도 조용히 넘어간다.</summary>
    UniTask RefreshAsync(CancellationToken ct = default);
}
