using UnityEngine;

/// <summary>의뢰 진행 상태. InProgress 가 아니게 되는 순간 디렉터가 결과 팝업을 띄운다.</summary>
public enum CommissionResult {
    InProgress,
    Success,
    Fail
}

/// <summary>
/// 상단 게이지 표시에 필요한 값 묶음. 세 의뢰 모두 "주 게이지 1개 + 남은 시간 1개" 형태라 하나의 구조체로 덮인다.
///  - 레이드   : 주 게이지 = 보스 체력
///  - 토벌     : 주 게이지 = 처치 진행도(n/목표)
///  - 호위     : 주 게이지 = 의뢰인 체력
/// HUD 패널은 이 값만 그리므로 의뢰 종류를 알 필요가 없다.
/// </summary>
public readonly struct CommissionHudState {
    /// <summary>주 게이지 비율(0~1).</summary>
    public readonly float PrimaryRatio;
    /// <summary>주 게이지 옆 텍스트(예: "72.4%", "37 / 100").</summary>
    public readonly string PrimaryText;
    /// <summary>주 게이지 위 제목(예: 보스 이름, "토벌 진행").</summary>
    public readonly string PrimaryLabel;
    /// <summary>남은 시간 비율(0~1). 제한 시간이 없으면 1.</summary>
    public readonly float TimeRatio;
    /// <summary>남은 시간 텍스트(예: "23.4").</summary>
    public readonly string TimeText;

    public CommissionHudState(float primaryRatio, string primaryText, string primaryLabel, float timeRatio, string timeText) {
        PrimaryRatio = primaryRatio;
        PrimaryText = primaryText;
        PrimaryLabel = primaryLabel;
        TimeRatio = timeRatio;
        TimeText = timeText;
    }
}

/// <summary>
/// 의뢰 종류별 진행 규칙. "무엇을 스폰하고, 언제 성공/실패하며, 상단 게이지에 무엇을 그리는가"만 담당한다.
/// 전투 자체(유닛 생성/공격/연출)는 BattleStage 가 하므로, 여기에는 컨텐츠 규칙만 남는다.
///
/// 새 의뢰 종류를 추가할 때는 이 클래스를 상속한 모드를 하나 만들고 CommissionDirector 의 팩토리에 한 줄 더하면 된다.
/// </summary>
public abstract class CommissionMode {
    protected CommissionDirector Director { get; private set; }
    protected CommissionData Data { get; private set; }

    /// <summary>남은 제한 시간(초). 제한 시간이 없으면 항상 양수로 유지된다.</summary>
    protected float TimeRemaining { get; private set; }

    /// <summary>현재 진행 상태. 디렉터가 매 프레임 확인한다.</summary>
    public CommissionResult Result { get; protected set; } = CommissionResult.InProgress;

    /// <summary>전투 시작. 파티는 이미 만들어져 있고, 여기서 몬스터/호위 대상 등 컨텐츠 유닛을 세운다.</summary>
    public void Begin(CommissionDirector director, CommissionData data) {
        Director = director;
        Data = data;
        TimeRemaining = data.TimeLimitSeconds > 0f ? data.TimeLimitSeconds : float.PositiveInfinity;
        Result = CommissionResult.InProgress;
        OnBegin();
    }

    /// <summary>매 프레임 진행. 제한 시간과 전멸은 공통 처리하고, 종류별 판정만 파생 클래스가 한다.</summary>
    public void Tick(float dt) {
        if (Result != CommissionResult.InProgress) return;

        // 전멸은 어느 의뢰에서나 즉시 실패다(의뢰에는 부활이 없다).
        if (Director.IsPartyWiped) {
            Result = CommissionResult.Fail;
            return;
        }

        if (!float.IsPositiveInfinity(TimeRemaining)) {
            TimeRemaining -= dt;
            if (TimeRemaining <= 0f) {
                TimeRemaining = 0f;
                // 시간 종료의 의미는 종류마다 다르다(레이드/토벌=실패, 호위=성공).
                Result = OnTimeExpired();
                return;
            }
        }

        OnTick(dt);
    }

    /// <summary>상단 게이지에 그릴 값.</summary>
    public abstract CommissionHudState BuildHudState();

    /// <summary>몬스터가 쓰러졌을 때(처치 수 집계 등). 기본 동작 없음.</summary>
    public virtual void OnMonsterDied(BattleUnit unit) { }

    /// <summary>아군(파티/의뢰인)이 쓰러졌을 때. 기본 동작 없음(전멸 판정은 Tick 이 공통 처리).</summary>
    public virtual void OnPlayerUnitDied(BattleUnit unit) { }

    /// <summary>몬스터가 노려야 할 우선 대상(호위 의뢰만 사용). null 이면 기본 타게팅.</summary>
    public virtual BattleUnit PriorityTarget => null;

    protected abstract void OnBegin();
    protected abstract void OnTick(float dt);

    /// <summary>제한 시간이 다 됐을 때의 결과. 기본은 실패(제한 시간 안에 목표를 못 이뤘다는 뜻).</summary>
    protected virtual CommissionResult OnTimeExpired() => CommissionResult.Fail;

    /// <summary>남은 시간 비율(0~1). 제한 시간이 없으면 1.</summary>
    protected float TimeRatio =>
        Data.TimeLimitSeconds > 0f ? Mathf.Clamp01(TimeRemaining / Data.TimeLimitSeconds) : 1f;

    /// <summary>남은 시간 표시 문자열(소수 1자리). 제한 시간이 없으면 빈 문자열.</summary>
    protected string TimeText =>
        Data.TimeLimitSeconds > 0f ? TimeRemaining.ToString("0.0") : "";
}
