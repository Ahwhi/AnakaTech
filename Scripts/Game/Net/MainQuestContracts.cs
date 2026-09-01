using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

// 메인 퀘스트 통신 계약과 순수 DTO(생성 패킷 비의존). 메인 퀘스트 UI 는 이 타입들만 참조한다.

/// <summary>
/// 메인 퀘스트 목표. 값은 서버 MainQuestObjective 와 일치해야 한다(패킷에 int 로 실려 온다).
/// 정의(순서/인자/보상)는 전부 서버가 갖고, 클라는 이 값으로 문구만 만든다(MainQuestRules).
/// </summary>
public enum MainQuestObjective {
    StageClear = 0,
    CharacterGacha = 1,
    DeckChange = 2,
    StatAllocate = 3,
    AutoStatSet = 4,
    EquipGacha = 5,
    EquipEquip = 6,
    EquipEnhance = 7,
    /// <summary>기력 부스트 켜 보기. 화면 안 조작이라 클라가 보고한다(ReportActionAsync).</summary>
    StaminaBoost = 8,
    Training = 9,
    CommissionRaid = 10,
    CommissionSubjugation = 11,
    EngravingEquip = 12,
    CommissionEscort = 13,
    HeadquartersUpgrade = 14,
    FriendMade = 15,
    ArenaEntry = 16,
    GuildJoin = 17,
    CharacterStarUp = 18,
    EquipAwaken = 19,
    CharacterItemUse = 20,
    GuildRaidEntry = 21,
    /// <summary>자동 전투 모드 바꿔 보기. 화면 안 조작이라 클라가 보고한다(ReportAction).</summary>
    AutoBattleToggle = 22,
    NicknameChange = 23,
    StageSweep = 24,
    Dispatch = 25
}

/// <summary>
/// 지금 진행 중인 메인 퀘스트 1건. 한 번에 하나만 진행되므로 목록이 아니라 단건이다.
/// Level 이 0 이면 준비된 퀘스트를 전부 끝낸 상태다(다음 업데이트까지 표시할 것이 없다).
/// </summary>
public sealed class MainQuestDto {
    public int Level;
    public MainQuestObjective Objective;
    /// <summary>목표가 가리키는 대상(스테이지=던전 인덱스). 인자가 없는 목표는 0.</summary>
    public int Param;
    public int Progress;
    public int Threshold;
    public bool IsCompleted;
    public List<RewardItem> Rewards = new();

    /// <summary>표시할 퀘스트가 있는지. 전부 끝냈으면 false 라 화면이 통째로 꺼진다.</summary>
    public bool HasQuest => Level > 0;
}

/// <summary>메인 퀘스트 보상 수령 결과. 다음 퀘스트 현황은 서버가 이어서 보내는 S_MainQuest 로 갱신된다.</summary>
public sealed class MainQuestClaimResultDto {
    public bool IsSuccess;
    public int FailReason;
    public int ClaimedLevel;
    public int NewGold;
    public int NewDiamond;
    public List<RewardItem> Granted = new();
    public List<CharExpGain> CharExp = new();
}

/// <summary>
/// 메인 퀘스트 통신 추상화. 현황은 ReactiveProperty 로 노출한다. 퀘스트는 유저의 행동(모집/강화/의뢰 등) 때문에
/// 서버에서 먼저 완료되고 그 사실이 푸시로 내려오므로, 화면이 물어보는 값이 아니라 흘러 들어오는 값이다.
/// </summary>
public interface IMainQuestService {
    /// <summary>지금 진행 중인 퀘스트. 아직 한 번도 받지 못했으면 null.</summary>
    IReadOnlyReactiveProperty<MainQuestDto> Current { get; }

    /// <summary>퀘스트가 방금 완료됐다는 서버 알림(레벨). 어느 화면에 있든 토스트로 알리기 위한 신호다.</summary>
    IObservable<int> Completed { get; }

    /// <summary>현황을 받아 온다(메인 씬 진입 시 1회). 이후 갱신은 서버 푸시가 맡는다.</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// 화면 안에서만 일어나는 조작을 알린다(기력 부스트 켜기 / 자동 전투 모드 바꾸기).
    /// 서버에 아무 요청도 남기지 않는 조작이라 알려 주지 않으면 서버가 알 방법이 없다.
    /// 응답을 기다리지 않으며, 진행 결과는 평소처럼 서버 푸시(Current/Completed)로 돌아온다.
    /// 서버가 허용 목록에 없는 목표는 무시하므로 아무 목표나 넣어도 진도가 오르지 않는다.
    /// </summary>
    void ReportAction(MainQuestObjective objective);

    /// <summary>완료한 퀘스트의 보상을 받는다. 성공하면 서버가 이어서 보내는 현황으로 Current 가 갱신된다.</summary>
    UniTask<MainQuestClaimResultDto> ClaimAsync(int level, CancellationToken ct = default);
}
