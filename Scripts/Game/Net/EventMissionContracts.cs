using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

// 미션형 이벤트 통신 계약과 순수 DTO(생성 패킷 비의존). 이벤트 페이지/서비스는 이 타입들만 참조한다.
// 이벤트는 키로 구분되며(EventMissionKeys), 어떤 이벤트든 "미션 목록 + 진행/수령"이라는 같은 형태를 쓴다.

/// <summary>이벤트 진행 지표(서버 EventMissionMetric 과 값 일치. 새 지표는 항상 끝에 추가).</summary>
public enum EventMissionMetric {
    CharGacha = 0,          // 캐릭터 뽑기 횟수
    EquipGacha = 1,         // 장비 뽑기 횟수
    StaminaSpent = 2,       // 기력 소모량
    CharLevelUp = 3,        // 캐릭터 레벨 상승 횟수
    GachaTotal = 4,         // 누적 뽑기 횟수
    RaidClear = 5,          // 레이드 의뢰 해결(도전 + 즉시해결) 횟수
    SubjugationClear = 6,   // 토벌 의뢰 해결(도전 + 즉시해결) 횟수
    EscortClear = 7,        // 호위 의뢰 해결(도전 + 즉시해결) 횟수
    CharacterCollected = 8, // 보유 캐릭터 종류 수(Threshold=목표 인원)
    StageCleared = 9,       // 특정 스테이지 클리어(Param=던전 인덱스)
    DiaPackPurchased = 10,  // 특정 다이아 상품 구매(Param=다이아 수량)
    EquipEnhanceTotal = 11, // 장비 강화 누적 횟수
    EquipAwakenTotal = 12,  // 장비 각성 누적 횟수
    EquipGachaTotal = 13,   // 장비 모집 누적 횟수
    SessionMinutesToday = 14, // 오늘 누적 접속 시간(분)
    /// <summary>특정 의뢰 종류의 특정 단계 클리어(Param=CommissionMissionParam 인코딩).</summary>
    CommissionStageCleared = 15,
    /// <summary>같은 묶음(일차 등)의 미션을 모두 달성(Param=그룹 번호).</summary>
    MissionGroupCleared = 16,
    /// <summary>이벤트의 모든 미션을 달성(전체 완주 보상).</summary>
    MissionSetCleared = 17,
    CharStarUpTotal = 18,   // 캐릭터 성급업(별레벨 승급) 누적 횟수
    /// <summary>오늘 핫타임(정해진 시간대)에 접속했는지. Param=HotTimeMissionParam 인코딩, Threshold=1.</summary>
    HotTimeAttended = 19,
}

/// <summary>
/// 의뢰 미션의 Param 인코딩. 서버 CommissionMissionParam 과 규칙이 같아야 한다.
/// 종류와 단계를 한 정수에 담아 지표 하나로 "무슨 의뢰 몇 단계"를 표현한다.
/// </summary>
public static class CommissionMissionParam {
    private const int TypeScale = 1000;

    public static int Encode(int commissionType, int stage) => commissionType * TypeScale + stage;
    public static int TypeOf(int param) => param / TypeScale;
    public static int StageOf(int param) => param % TypeScale;
}

/// <summary>
/// 핫타임 미션의 Param 인코딩. 서버 HotTimeMissionParam 과 규칙이 같아야 한다.
/// 시작/종료 시각(24시간제)을 한 정수에 담아, 시간대가 몇 시간짜리든 Param 하나로 문구를 만들 수 있게 한다.
/// </summary>
public static class HotTimeMissionParam {
    private const int HourScale = 100;

    public static int Encode(int startHour, int endHour) => startHour * HourScale + endHour;
    public static int StartOf(int param) => param / HourScale;
    public static int EndOf(int param) => param % HourScale;
}

/// <summary>
/// 서버 EventKeys 와 약속된 이벤트 키. 이벤트 페이지 프리팹이 이 값 중 하나를 들고 자기 미션 목록을 받아온다
/// (공지 DB 의 reward_key 와도 같은 문자열이라 배지가 함께 연결된다).
/// </summary>
public static class EventMissionKeys {
    public const string OpenEvent = "open_event";
    public const string CollectGirls = "collect_girls";
    public const string KotohaMotivation = "kotoha_motivation";
    public const string DiaRefund = "dia_refund";
    public const string EquipChallenge = "equip_challenge";
    public const string PlaytimeStamina = "playtime_stamina";
    public const string SevenDayMission = "seven_day_mission";
    public const string HotTime = "hot_time";
}

/// <summary>이벤트 보상 1건(표시/재동기화용). Kind 는 보상표시 규칙(Gold0/Diamond1/Stamina2/Currency4/UniversalShard5/ProfileFrame7).</summary>
public readonly struct EventMissionReward {
    public readonly int Kind;
    public readonly int Value;
    public readonly int Qty;
    public readonly string CurrencyKey;

    public EventMissionReward(int kind, int value, int qty, string currencyKey) {
        Kind = kind; Value = value; Qty = qty; CurrencyKey = currencyKey ?? "";
    }
}

/// <summary>이벤트 미션 1개(서버 권위). 진행/수령 + 보상 표시.</summary>
public readonly struct EventMission {
    public readonly int Id;
    public readonly bool Daily;
    public readonly int Metric;      // EventMissionMetric
    /// <summary>지표가 가리키는 대상(스테이지 인덱스/다이아 수량 등). 대상이 없는 지표는 0.</summary>
    public readonly int Param;
    public readonly int Threshold;
    public readonly int Progress;
    public readonly bool Claimed;
    /// <summary>아직 열리지 않은 일차의 미션인지(7일 미션). 잠겨 있으면 진행/수령이 막힌다.</summary>
    public readonly bool Locked;
    /// <summary>묶음 번호(7일 미션의 일차 등). 0이면 묶음 없음.</summary>
    public readonly int Group;
    /// <summary>계정 생성일로부터 며칠째에 열리는지(1=첫날). 0이면 처음부터 열려 있다.</summary>
    public readonly int UnlockDay;
    public readonly EventMissionReward Reward;

    public EventMission(int id, bool daily, int metric, int param, int threshold, int progress, bool claimed,
                        bool locked, int group, int unlockDay, EventMissionReward reward) {
        Id = id; Daily = daily; Metric = metric; Param = param; Threshold = threshold; Progress = progress;
        Claimed = claimed; Locked = locked; Group = group; UnlockDay = unlockDay; Reward = reward;
    }

    public bool Unlocked => !Locked && Progress >= Threshold;
    public bool Claimable => Unlocked && !Claimed;
}

/// <summary>이벤트 상태(종료 시각 + 미션 목록 + 진행 현황).</summary>
public readonly struct EventMissionStatus {
    public readonly long EndUnix;
    public readonly IReadOnlyList<EventMission> Missions;
    /// <summary>진행 현황의 분모(전체 완주 보상은 제외). 예: 7일 미션 35.</summary>
    public readonly int TotalMissions;
    /// <summary>진행 현황의 분자(달성 수). 수령 여부와 무관하다.</summary>
    public readonly int AchievedMissions;
    /// <summary>계정이 지금 열 수 있는 최대 일차(일차 개념이 없는 이벤트는 0).</summary>
    public readonly int UnlockedDay;

    public EventMissionStatus(long endUnix, IReadOnlyList<EventMission> missions,
                              int totalMissions, int achievedMissions, int unlockedDay) {
        EndUnix = endUnix;
        Missions = missions;
        TotalMissions = totalMissions;
        AchievedMissions = achievedMissions;
        UnlockedDay = unlockedDay;
    }
}

/// <summary>미션 수령 결과(서버 권위). 지급 보상 + 갱신된 지갑 잔액(재동기화용).</summary>
public readonly struct EventMissionClaimResult {
    public readonly bool Success;
    public readonly int FailReason;   // 0성공,1잘못된미션,2미달성,3이미수령,4이벤트종료,5지갑없음/인증전
    public readonly int MissionId;
    public readonly int NewGold;
    public readonly int NewDiamond;
    public readonly int NewStamina;
    public readonly IReadOnlyList<EventMissionReward> Granted;

    public EventMissionClaimResult(bool success, int failReason, int missionId,
                                int newGold, int newDiamond, int newStamina, IReadOnlyList<EventMissionReward> granted) {
        Success = success; FailReason = failReason; MissionId = missionId;
        NewGold = newGold; NewDiamond = newDiamond; NewStamina = newStamina; Granted = granted;
    }
}

/// <summary>
/// 접속 유지 시간 적립용 하트비트. 로그인 이후 시작하고 로그아웃/종료 시 멈춘다.
/// 누적 시간은 서버가 신호 간격으로 판정하므로 클라는 신호만 보낸다.
/// </summary>
public interface IHeartbeatService {
    void Start();
    void Stop();
    /// <summary>가장 최근에 서버가 알려준 오늘 누적 접속 시간(초). 표시용.</summary>
    int SessionSecondsToday { get; }

    /// <summary>
    /// 서버가 하트비트 응답으로 알려준 기력 부스트권 보유량(자정 자동 충전이 반영된 값).
    ///
    /// 하트비트는 씬보다 오래 사는 루트 서비스라 플레이어 상태 저장소를 직접 건드리지 않는다.
    /// 값만 흘려보내고, 저장소에 반영하는 것은 그 씬의 주인(MainSceneManager)이 한다.
    ///
    /// 보유량을 들고 있는 상태가 아니라 "방금 서버가 알려줬다"는 알림이다. 이 서비스는 계정보다 오래
    /// 살기 때문에 마지막 값을 품고 있으면, 다른 계정으로 다시 로그인했을 때 새 구독자가 구독하는 순간
    /// 이전 계정의 값을 그대로 받아 화면에 써 버린다. 그래서 마지막 값을 재생하지 않는 알림으로 둔다.
    /// </summary>
    IObservable<int> StaminaBoostTicket { get; }
}

/// <summary>미션형 이벤트 통신 추상화(AttendanceService 와 동일한 WaitPacketAsync + Send 패턴).</summary>
public interface IEventMissionService {
    /// <summary>이벤트 키에 해당하는 미션 목록/진행 상태를 받아온다(EventMissionKeys).</summary>
    UniTask<EventMissionStatus> GetStatusAsync(string eventKey, CancellationToken ct = default);

    /// <summary>미션 1개 수령. 미션 id 만으로 서버가 소속 이벤트를 찾으므로 이벤트 키는 보내지 않는다.</summary>
    UniTask<EventMissionClaimResult> ClaimAsync(int missionId, CancellationToken ct = default);
}
