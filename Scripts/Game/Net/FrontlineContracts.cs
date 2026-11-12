using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>전선 1곳의 스쿼드와 진행.</summary>
public sealed class FrontlineSquadDto {
    public int FrontlineId;
    /// <summary>지금 칠 단계(1 ~ MaxStage).</summary>
    public int Stage = FrontlineRules.StartStage;
    /// <summary>배치된 소녀(순서 = 슬롯 순서).</summary>
    public readonly List<int> Members = new();
    /// <summary>클리어 기록이 있어 소녀를 뺄 수 없는지(초기화로만 풀린다).</summary>
    public bool Locked;
}

/// <summary>전선 계정 현황. 전선 6곳과 하루 기력 한도 사용량.</summary>
public sealed class FrontlineInfoDto {
    public int MaxStage = FrontlineRules.MaxStage;
    public int StaminaPerClear = FrontlineRules.StaminaPerClear;
    public int DailyStaminaLimit = FrontlineRules.DailyStaminaLimit;
    public int DailyStaminaUsed;
    /// <summary>id 순서대로 전선 6곳(서버가 빠짐없이 내려준다).</summary>
    public readonly List<FrontlineSquadDto> Squads = new();

    public int DailyStaminaRemaining => System.Math.Max(0, DailyStaminaLimit - DailyStaminaUsed);

    public FrontlineSquadDto Squad(int frontlineId) {
        foreach (var s in Squads) if (s != null && s.FrontlineId == frontlineId) return s;
        return null;
    }

    /// <summary>다른 전선에 배치된 소녀인지(파견처럼 목록에서 숨기는 기준).</summary>
    public bool IsDeployedElsewhere(int charDataId, int frontlineId) {
        foreach (var s in Squads)
            if (s != null && s.FrontlineId != frontlineId && s.Members.Contains(charDataId)) return true;
        return false;
    }
}

/// <summary>배치 저장/초기화/입장 등 쓰기 작업의 공통 응답.</summary>
public sealed class FrontlineActionDto {
    public bool IsSuccess;
    public int FailReason;
    /// <summary>응답 시점의 기력(입장 응답만 채운다. 그 외는 -1).</summary>
    public int NewStamina = -1;
}

/// <summary>한 판 종료 보고에 대한 응답.</summary>
public sealed class FrontlineFinishDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=없는 전선,2=계정없음,3=단계 불일치,4=입장 기록 없음/시간 부족,5=기력 부족,6=오늘 한도 소진</summary>
    public int FailReason;
    public bool Cleared;
    /// <summary>다음에 칠 단계.</summary>
    public int Stage;
    /// <summary>소녀 1명이 받은 경험치(실패면 0).</summary>
    public int GainedExp;
    /// <summary>기력 잔량(서버 확정값).</summary>
    public int NewStamina;
    public readonly List<CharExpGain> CharExp = new();
}

public interface IFrontlineService {
    /// <summary>
    /// 마지막으로 받은 계정 현황(받기 전이면 null). 조회/배치/초기화/입장/종료 응답이 올 때마다 갱신되므로
    /// 전선 팝업과 배치 팝업이 같은 값을 구독해 서로를 몰라도 함께 맞춰진다.
    /// </summary>
    IReadOnlyReactiveProperty<FrontlineInfoDto> Info { get; }

    UniTask<FrontlineInfoDto> RequestInfoAsync(CancellationToken ct = default);

    /// <summary>현황만 다시 받아 둔다(배지 갱신용). 실패해도 예외를 던지지 않는다.</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>스쿼드 배치 저장. 실패하면 서버가 가진 배치로 되돌린다.</summary>
    UniTask<FrontlineActionDto> SetMembersAsync(int frontlineId, IReadOnlyList<int> members, CancellationToken ct = default);

    /// <summary>전선 초기화(단계 1, 소녀 해제).</summary>
    UniTask<FrontlineActionDto> ResetAsync(int frontlineId, CancellationToken ct = default);

    /// <summary>한 판 입장. 기력은 쓰지 않으며 서버가 기력/하루 한도를 미리 확인한다.</summary>
    UniTask<FrontlineActionDto> EnterAsync(int frontlineId, int stage, CancellationToken ct = default);

    /// <summary>한 판 종료 보고(클리어면 기력 소모 + 경험치 + 다음 단계, 실패면 이전 단계).</summary>
    UniTask<FrontlineFinishDto> FinishAsync(int frontlineId, int stage, bool cleared, CancellationToken ct = default);
}
