using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 길드 통신 계약과 순수 DTO(생성 패킷 비의존). 길드 UI 는 이 타입들만 참조한다.

/// <summary>길드 내 역할. 값은 서버 GuildRole 과 일치해야 한다(패킷에 int 로 실려 온다).</summary>
public enum GuildRole {
    Member = 0,
    Officer = 1,      // 운영진
    ViceMaster = 2,   // 부길드장
    Master = 3        // 길드장
}

/// <summary>길드 가입 방식. 값은 서버 GuildJoinType 과 일치.</summary>
public enum GuildJoinType {
    Instant = 0,      // 즉시 가입
    Application = 1   // 가입 신청형
}

/// <summary>
/// 길드 쓰기 작업 종류. 값은 서버 Room.GuildActionType 과 일치해야 한다.
/// 작업마다 패킷을 만들지 않고 이 값으로 갈라 하나의 요청/응답을 공유한다.
/// </summary>
public enum GuildActionType {
    Create = 0,
    Join = 1,
    Apply = 2,
    CancelApply = 3,
    Approve = 4,
    Reject = 5,
    Leave = 6,
    Kick = 7,
    ChangeRole = 8,
    TransferMaster = 9,
    Disband = 10,
    Rename = 11,
    SetIntro = 12,
    SetNotice = 13,
    SetJoinType = 14,
    SetMark = 15,
    UnlockMark = 16
}

/// <summary>목록/검색에 뜨는 길드 요약.</summary>
public sealed class GuildSummaryDto {
    public int GuildId;
    public string Name = "";
    public int MarkId;
    public int Level;
    public long CombatPower;
    public int MemberCount;
    public GuildJoinType JoinType;
    public string Intro = "";
    /// <summary>길드장 닉네임. 목록에서 "누가 이끄는 길드인지" 바로 보이게 함께 내려온다.</summary>
    public string MasterNickname = "";
    /// <summary>길드 생성일(UTC epoch). 0 이면 기록이 없는 옛 길드다.</summary>
    public long CreatedUnix;
    /// <summary>길드 랭킹(길드 전투력) 순위. 0 이면 순위권 밖이거나 아직 집계 전. 단건 조회에서만 채워진다.</summary>
    public int Rank;

    public bool IsFull => MemberCount >= GuildRules.MaxMembers;
    /// <summary>지금 바로 들어갈 수 있는지(즉시 가입 + 정원 여유).</summary>
    public bool CanJoinNow => JoinType == GuildJoinType.Instant && !IsFull;
}

/// <summary>길드원 1명(길드원 탭 / 길드원 관리 탭 공용).</summary>
public sealed class GuildMemberDto {
    public int AccountId;
    public string Nickname = "";
    public GuildRole Role;
    public int ProfileFrameId;
    public int RepresentativeCharId;
    /// <summary>대표 캐릭터가 장착한 코스튬(0=기본 일러스트). 남의 코스튬은 서버만 알고 있어 함께 내려온다.</summary>
    public int RepresentativeCostumeId;
    public long CombatPower;
    public long JoinedUnix;
    /// <summary>이 길드에서 쌓은 공헌도. 재화가 아니라 길드 안에서만 의미가 있고, 탈퇴/추방하면 사라진다.</summary>
    public int Contribution;
    /// <summary>마지막 접속 시각(unix). 0 이면 기록이 없다.</summary>
    public long LastLoginUnix;
    /// <summary>지금 접속 중인지. 목록을 받은 순간의 값이라 화면이 떠 있는 동안 갱신되지는 않는다.</summary>
    public bool IsOnline;
}

/// <summary>대항전 교전 기록 1건(누가 누구를 쳐서 이겼는지).</summary>
public sealed class GuildWarLogDto {
    public int AttackerAccountId;
    public string AttackerNickname = "";
    public int AttackerGuildId;
    public int TargetAccountId;
    public string TargetNickname = "";
    public int TargetGuildId;
    public bool AttackerWon;
    public long CreatedUnix;
}

/// <summary>가입 신청 1건(관리 탭).</summary>
public sealed class GuildApplicationDto {
    public int AccountId;
    public string Nickname = "";
    public int ProfileFrameId;
    public int RepresentativeCharId;
    /// <summary>대표 캐릭터가 장착한 코스튬(0=기본 일러스트). 남의 코스튬은 서버만 알고 있어 함께 내려온다.</summary>
    public int RepresentativeCostumeId;
    public long CombatPower;
    public long AppliedUnix;
}

/// <summary>
/// 길드 팝업 진입 응답. 길드 소속 여부에 따라 채워지는 부분이 다르다.
/// GuildId == 0 이면 "길드 없음" 그룹(Recommends / AppliedGuildIds 사용),
/// 아니면 "길드 있음" 그룹(Summary / Notice / Members 사용).
/// </summary>
public sealed class GuildHomeDto {
    public int GuildId;
    public GuildRole MyRole;
    /// <summary>대기 중 가입 신청 수(운영진 이상에게만 채워진다). 길드 버튼/관리 탭 배지의 근거.</summary>
    public int PendingApplications;
    public long MyCombatPower;

    // 길드 없음
    public List<int> AppliedGuildIds = new();
    public List<GuildSummaryDto> Recommends = new();

    // 길드 있음
    public GuildSummaryDto Summary = new();
    public string Notice = "";
    public string MasterNickname = "";
    public long CreatedUnix;
    public int ExpInLevel;
    public int ExpPerLevel;
    /// <summary>오늘 남은 무료 레이드 참여 횟수. 길드 버튼/컨텐츠 탭 배지의 근거.</summary>
    public int RaidFreeEntries;
    public List<int> UnlockedMarkIds = new();
    public List<GuildMemberDto> Members = new();

    public bool HasGuild => GuildId > 0;
    /// <summary>관리 탭에 들어갈 수 있는 역할인지(길드장/부길드장).</summary>
    public bool CanManage => MyRole >= GuildRole.ViceMaster;
    /// <summary>가입 신청을 처리할 수 있는 역할인지(운영진 이상).</summary>
    public bool CanHandleApplications => MyRole >= GuildRole.Officer;
}

/// <summary>길드 쓰기 작업 결과.</summary>
public sealed class GuildActionResultDto {
    public bool IsSuccess;
    public int FailReason;
    public GuildActionType Action;
    public int GuildId;
    public int TargetAccountId;
    public int NewDiamond;
}

/// <summary>다른 유저의 공개 프로필(길드원 목록에서 연다).</summary>
public sealed class UserProfileDto {
    public int AccountId;
    public string Nickname = "";
    public int ProfileFrameId;
    public int RepresentativeCharId;
    /// <summary>대표 캐릭터가 장착한 코스튬(0=기본 일러스트). 남의 코스튬은 서버만 알고 있어 함께 내려온다.</summary>
    public int RepresentativeCostumeId;
    public long CombatPower;
    public int HighestClearedDungeonIndex;
    public long CreatedUnix;
    public string GuildName = "";
    public int GuildMarkId;
    public GuildRole GuildRole;

    /// <summary>
    /// 나를 기준으로 본 이 유저와의 친구 관계(서버가 계산해 보낸다).
    /// 프로필과 달리 "요청자와 대상 사이"의 값이라, 이걸로 친구/차단 버튼 상태가 정해진다.
    /// </summary>
    public FriendRelation Relation;
    /// <summary>내가 이 유저를 차단해 두었는지. 친구 관계와 독립된 축이다.</summary>
    public bool Blocked;

    /// <summary>
    /// 내가 이 유저에게 귓속말을 보낼 수 있는지(상대의 수신 범위 + 차단 + 친구 여부를 서버가 합쳐 판단).
    /// 버튼을 눌러 보고 거절당하는 대신 미리 알려 주기 위한 값이다.
    /// </summary>
    public bool CanWhisper;

    /// <summary>업적 총점.</summary>
    public long AchievementScore;

    /// <summary>유저 랭킹(전투력) 순위. 0 이면 순위권 밖이거나 아직 집계 전이다.</summary>
    public int CombatPowerRank;
    /// <summary>업적 랭킹 순위. 0 이면 순위권 밖이거나 아직 집계 전이다.</summary>
    public int AchievementRank;

    /// <summary>
    /// 현재 편성(1~5명, 편성 순서 그대로. 첫 칸이 대표다).
    /// 전투 재현용 UserDeckDto 와 달리 표시에 필요한 값만 담긴다.
    /// </summary>
    public List<UserProfileDeckMemberDto> DeckMembers = new();

    public bool HasGuild => !string.IsNullOrEmpty(GuildName);
}

/// <summary>공개 프로필에 실리는 편성원 1명(일러스트 + 별레벨 + 레벨 + 코스튬).</summary>
public sealed class UserProfileDeckMemberDto {
    public int CharDataId;
    public int StarLevel;
    public int Level;
    /// <summary>이 캐릭터가 장착한 코스튬(0=기본 일러스트).</summary>
    public int CostumeId;
}

/// <summary>길드 레이드 현황(패널 표시용).</summary>
public sealed class GuildRaidStateDto {
    public bool HasGuild;
    public int BossLevel;
    public long BossHp;
    public long BossMaxHp;
    public int MyEntries;
    public int MaxEntries;
    /// <summary>다음 참여 비용(다이아). 0 = 무료분이 남음, -1 = 오늘 더 참여할 수 없음.</summary>
    public int NextEntryCost;
    public long MyDamageToday;
    /// <summary>다음 초기화(KST 자정) 시각. 남은 시간 카운트다운에 쓴다.</summary>
    public long ResetUnix;
    public List<GuildRaidContributionDto> Contributions = new();

    public bool CanEnter => NextEntryCost >= 0;
    /// <summary>보스 남은 체력 비율(0~1). 체력바에 그대로 쓴다.</summary>
    public float HpRatio => BossMaxHp > 0 ? Mathf.Clamp01((float)BossHp / BossMaxHp) : 0f;
}

/// <summary>오늘 기여도 1건.</summary>
public sealed class GuildRaidContributionDto {
    public int AccountId;
    public string Nickname = "";
    public int ProfileFrameId;
    public int RepresentativeCharId;
    /// <summary>대표 캐릭터가 장착한 코스튬(0=기본 일러스트). 남의 코스튬은 서버만 알고 있어 함께 내려온다.</summary>
    public int RepresentativeCostumeId;
    public long Damage;
    public int Entries;
}

/// <summary>레이드 입장 결과. 성공했을 때만 레이드 씬으로 넘어간다(참여 횟수는 서버가 이미 소모했다).</summary>
public sealed class GuildRaidEnterDto {
    public bool IsSuccess;
    public int FailReason;
    public int BossLevel;
    public long BossHp;
    public long BossMaxHp;
    public int NewDiamond;
}

/// <summary>도전 결과 보고에 대한 응답.</summary>
public sealed class GuildRaidDamageDto {
    public bool IsSuccess;
    public int FailReason;
    public int BossLevel;
    public long BossHp;
    public long BossMaxHp;
    public bool BossDefeated;
    /// <summary>도전 참여 보상(즉시 지급분). 결과 팝업이 그대로 그린다.</summary>
    public List<RewardItem> Granted = new();
}

/// <summary>대항전 진행 단계. 값은 서버 GuildWarPhase 와 일치.</summary>
public enum GuildWarPhase {
    None = 0,      // 매칭 전
    Waiting = 1,   // 매칭 완료 ~ 경기 시작 전
    Battle = 2,    // 경기 중(공격 가능)
    Settle = 3,    // 정산
    Done = 4       // 보상 지급 완료
}

/// <summary>상대가 없을 때 그 이유. 값은 서버 GuildWarNoMatchReason 과 일치.</summary>
public enum GuildWarNoMatchReason {
    Waiting = 0,    // 아직 이번 회차 매칭 시각 전
    Matching = 1,   // 매칭 시각은 지났고 짝을 짓는 중
    Skipped = 2     // 매칭이 끝났는데 짝이 없었다(참가 길드가 홀수)
}

/// <summary>대항전 참가자 1명.</summary>
public sealed class GuildWarParticipantDto {
    public int GuildId;
    public int AccountId;
    public string Nickname = "";
    public int ProfileFrameId;
    public int RepresentativeCharId;
    /// <summary>대표 캐릭터가 장착한 코스튬(0=기본 일러스트). 남의 코스튬은 서버만 알고 있어 함께 내려온다.</summary>
    public int RepresentativeCostumeId;
    public long CombatPower;
    public bool Dead;
    public int AttacksUsed;
}

/// <summary>대항전 한쪽 길드의 요약과 명단.</summary>
public sealed class GuildWarSideDto {
    public int GuildId;
    public string GuildName = "";
    public int MarkId;
    public int AliveCount;
    public int TotalCount;
    public List<GuildWarParticipantDto> Members = new();
}

/// <summary>대항전 현황(패널 표시용).</summary>
public sealed class GuildWarStateDto {
    public GuildWarPhase Phase;
    public int MatchId;
    public long BattleStartUnix;
    public long BattleEndUnix;
    /// <summary>다음 매칭 시각(토 17:00 KST). 매칭이 없을 때 남은 시간을 보여 준다.</summary>
    public long NextMatchUnix;
    /// <summary>상대가 없는 이유. 매칭 직후 공백기를 "매칭 중"으로 구분해 안내한다.</summary>
    public GuildWarNoMatchReason NoMatchReason;
    public int MyAttacksLeft;
    public bool MyDead;
    /// <summary>내 계정 id. 명단에서 나를 찾아 강조/정렬하는 데 쓴다.</summary>
    public int MyAccountId;
    public int WinnerGuildId;
    public GuildWarSideDto Mine = new();
    public GuildWarSideDto Enemy = new();

    public bool HasMatch => Phase != GuildWarPhase.None;
    /// <summary>지금 상대를 때릴 수 있는지(경기 중 + 내가 살아 있고 + 공격 횟수가 남음).</summary>
    public bool CanAttack => Phase == GuildWarPhase.Battle && !MyDead && MyAttacksLeft > 0;
    /// <summary>승패가 정해졌는지(정산 이후).</summary>
    public bool HasResult => WinnerGuildId != 0;
    public bool IWon => HasResult && WinnerGuildId == Mine.GuildId;
}

/// <summary>공격 시작 결과. 성공했을 때만 전투 씬으로 넘어간다(공격 횟수는 서버가 이미 소모했다).</summary>
public sealed class GuildWarAttackBeginDto {
    public bool IsSuccess;
    public int FailReason;
    public int TargetAccountId;
    public int AttacksLeft;
}

/// <summary>공격 결과 보고에 대한 응답.</summary>
public sealed class GuildWarAttackResultDto {
    public bool IsSuccess;
    public int FailReason;
    public int TargetAccountId;
    public bool TargetDead;
    public int MyAliveCount;
    public int EnemyAliveCount;
    /// <summary>공격 참여 보상(즉시 지급분). 결과 팝업이 그대로 그린다.</summary>
    public List<RewardItem> Granted = new();
}

/// <summary>
/// 다른 유저의 덱 편성(PvP 전투에서 적 파티를 구성할 때 쓴다).
/// 최종 능력치가 아니라 성장 상태만 오며, 조립은 클라가 자기 규칙(CharacterRuntime)으로 한다.
/// </summary>
public sealed class UserDeckDto {
    public int AccountId;
    public string Nickname = "";
    public int RepresentativeCharId;
    /// <summary>대표 캐릭터가 장착한 코스튬(0=기본 일러스트). 남의 코스튬은 서버만 알고 있어 함께 내려온다.</summary>
    public int RepresentativeCostumeId;
    public int ProfileFrameId;
    public List<UserDeckMemberDto> Members = new();

    // 아래 넷은 상대 덱을 100% 재현하기 위한 원본 상태다(최종 스탯이 아니다).
    // 조립은 내 파티와 똑같이 CharacterBuild + CharacterRuntime 이 하므로, 조건부 각인까지 그대로 살아난다.

    /// <summary>장착 장비. 캐릭터별로 나눠 쓰려면 EquippedCharDataId 로 거른다.</summary>
    public List<EquipmentInstanceDto> Equips = new();
    /// <summary>캐릭터별 각인 슬롯 배열(인덱스 = 슬롯 번호, 0 = 빈 슬롯).</summary>
    public Dictionary<int, int[]> EngravingSlots = new();
    /// <summary>계정 각인 레벨(각인 효과 수치를 정한다).</summary>
    public Dictionary<int, int> EngravingLevels = new();
    /// <summary>계정 본부 노드 레벨(모든 캐릭터 공통 보너스).</summary>
    public Dictionary<int, int> HqNodeLevels = new();
    /// <summary>캐릭터별 수련 결과(캐릭터 id -> 효과). 사거리/2회공격/회피 등이 여기서 살아난다.</summary>
    public Dictionary<int, TrainingResultDto> Trainings = new();

    /// <summary>
    /// 상대 계정의 투자 레벨(슬롯 번호 -> 레벨). 선제 스킬 게이지/스쿼드 재생은 스탯이 아니라 전투 중 동작이라
    /// 최종 스탯에 녹지 않으므로 레벨을 그대로 받아 내 파티와 같은 규칙으로 적용한다.
    /// </summary>
    public Dictionary<int, int> InvestmentLevels = new();

    /// <summary>지정 효과의 총 증가분(1000 분율). 내 쪽 PlayerStateStore.InvestmentPermille 과 같은 계산이다.</summary>
    public int InvestmentPermille(InvestmentEffect effect) {
        int permille = 0;
        foreach (var kv in InvestmentLevels) {
            var slot = InvestmentRules.Get(kv.Key);
            if (slot == null || slot.Effect != effect) continue;
            permille += InvestmentRules.PermilleAt(kv.Key, kv.Value);
        }
        return permille;
    }
}

/// <summary>덱에 편성된 캐릭터 1명의 성장 상태.</summary>
public sealed class UserDeckMemberDto {
    public int CharDataId;
    public int Level;
    public int StarLevel;
    public int InvAttack;
    public int InvDefense;
    public int InvHealth;
    public int InvAttackSpeed;
    public int InvMoveSpeed;
    public int InvSkillAccel;
    /// <summary>장착 코스튬(0=기본). 외형뿐 아니라 장착 효과(스탯)까지 정하므로 전투 재현에 반드시 쓰인다.</summary>
    public int CostumeId;
}

/// <summary>길드 통신 추상화. UI 는 이 계약에만 의존한다.</summary>
public interface IGuildService {
    UniTask<GuildHomeDto> RequestHomeAsync(CancellationToken ct = default);
    /// <param name="search">true 면 이름 검색, false 면 즉시 가입 추천 새로고침.</param>
    UniTask<List<GuildSummaryDto>> RequestListAsync(bool search, string keyword = "", CancellationToken ct = default);
    /// <summary>
    /// 길드 하나의 요약(길드 프로필용). 길드 id 만 아는 화면(랭킹/전적 등)이 그 길드를 보여 줄 때 쓴다.
    /// 해체된 길드면 null.
    /// </summary>
    UniTask<GuildSummaryDto> RequestGuildAsync(int guildId, CancellationToken ct = default);
    /// <summary>이름 중복확인. 0 이면 사용 가능, 그 외는 GuildRules.FailMessage 로 옮길 실패 사유(중복/형식/금지어).</summary>
    UniTask<int> CheckNameAsync(string guildName, CancellationToken ct = default);
    UniTask<GuildActionResultDto> CreateAsync(string guildName, GuildJoinType joinType, long combatPower, CancellationToken ct = default);
    UniTask<List<GuildApplicationDto>> RequestApplicationsAsync(CancellationToken ct = default);
    UniTask<UserProfileDto> RequestUserProfileAsync(int accountId, CancellationToken ct = default);

    /// <summary>
    /// 길드 쓰기 작업 공용 창구. 작업에 따라 쓰는 인자가 다르다.
    ///   가입/신청/신청취소 : guildId (+ 가입/신청은 combatPower)
    ///   승인/거절/추방/역할변경/위임 : targetAccountId (+ 역할변경은 intValue = GuildRole)
    ///   이름/소개/공지 변경 : textValue
    ///   가입방식/마크 변경, 마크 해금 : intValue
    /// </summary>
    UniTask<GuildActionResultDto> ActAsync(GuildActionType action, int guildId = 0, int targetAccountId = 0,
                                           int intValue = 0, string textValue = "", long combatPower = 0,
                                           CancellationToken ct = default);

    /// <summary>내 덱 전투력을 알린다(응답 없음). 길드원 목록/길드 전투력/대항전 매칭의 근거가 된다.</summary>
    void ReportCombatPower(long combatPower);

    // ── 길드 레이드 ──
    UniTask<GuildRaidStateDto> RequestRaidStateAsync(CancellationToken ct = default);
    /// <summary>레이드 입장. 서버가 참여 횟수를 먼저 소모하므로, 성공했으면 반드시 씬으로 들어가야 한다.</summary>
    UniTask<GuildRaidEnterDto> EnterRaidAsync(CancellationToken ct = default);
    UniTask<GuildRaidDamageDto> ReportRaidDamageAsync(long damage, CancellationToken ct = default);

    // ── 길드 대항전 ──
    UniTask<GuildWarStateDto> RequestWarStateAsync(CancellationToken ct = default);

    /// <summary>진행 중인 대항전의 교전 기록(최신순). 매치가 없으면 빈 목록이다.</summary>
    UniTask<List<GuildWarLogDto>> RequestWarLogsAsync(CancellationToken ct = default);
    /// <summary>공격 시작. 서버가 공격 횟수를 먼저 소모하므로, 성공했으면 반드시 전투 씬으로 들어가야 한다.</summary>
    UniTask<GuildWarAttackBeginDto> BeginWarAttackAsync(int targetAccountId, CancellationToken ct = default);
    UniTask<GuildWarAttackResultDto> ReportWarAttackAsync(int targetAccountId, bool win, CancellationToken ct = default);
    /// <summary>상대 덱 편성(PvP 전투의 적 파티 구성).</summary>
    UniTask<UserDeckDto> RequestUserDeckAsync(int accountId, CancellationToken ct = default);
}
