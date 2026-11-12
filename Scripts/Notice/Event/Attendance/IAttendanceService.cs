using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>출석 보상 1건(표시/재동기화용). Kind 는 보상표시 규칙과 일치(Gold0/Diamond1/Stamina2/Character3/Currency4/UniversalShard5/Equip6).
/// 표시 수량은 만능조각(UniversalShard)이면 Qty, 그 외는 Value 를 쓴다.</summary>
public readonly struct AttendanceReward {
    public readonly int Day;   // 일자별 보상 목록에서만 유효(수령 결과에선 0)
    public readonly int Kind;
    public readonly int Value;
    public readonly int Qty;
    public readonly string CurrencyKey;

    public AttendanceReward(int day, int kind, int value, int qty, string currencyKey) {
        Day = day; Kind = kind; Value = value; Qty = qty; CurrencyKey = currencyKey ?? "";
    }
}

/// <summary>출석 진행 정보(서버 권위). 그리드 표시 + 일자별 보상 목록. 월간 출석과 출석형 이벤트가 함께 쓴다.</summary>
public readonly struct AttendanceInfo {
    public readonly int ClaimedDays;
    public readonly bool CanClaimToday;
    public readonly int MaxDays;
    public readonly IReadOnlyList<AttendanceReward> Rewards;   // 일자별 보상(칸 표시용)
    /// <summary>참여 조건을 채우지 못해 잠겼는지(출석형 이벤트 전용, 월간 출석은 항상 false).</summary>
    public readonly bool Locked;

    public AttendanceInfo(int claimedDays, bool canClaimToday, int maxDays, IReadOnlyList<AttendanceReward> rewards,
                          bool locked = false) {
        ClaimedDays = claimedDays;
        CanClaimToday = canClaimToday;
        MaxDays = maxDays;
        Rewards = rewards;
        Locked = locked;
    }
}

/// <summary>출석 수령 결과(서버 권위). 지급 보상 + 갱신된 지갑 잔액(클라 재동기화용).</summary>
public readonly struct AttendanceClaimResult {
    public readonly bool Success;
    public readonly int FailReason;     // 0=성공, 1=인증 전, 2=수령 불가
    public readonly int ClaimedDays;
    public readonly bool CanClaimToday;
    public readonly int NewGold;
    public readonly int NewDiamond;
    public readonly int NewStamina;
    public readonly IReadOnlyList<AttendanceReward> Granted;

    public AttendanceClaimResult(bool success, int failReason, int claimedDays, bool canClaimToday,
                                 int newGold, int newDiamond, int newStamina, IReadOnlyList<AttendanceReward> granted) {
        Success = success;
        FailReason = failReason;
        ClaimedDays = claimedDays;
        CanClaimToday = canClaimToday;
        NewGold = newGold;
        NewDiamond = newDiamond;
        NewStamina = newStamina;
        Granted = granted;
    }
}

/// <summary>
/// 출석체크 전용 통신. 진행 정보 조회/수령을 서버 요청-응답으로 처리한다.
/// 충족 여부/적립은 전적으로 서버가 판정하며, 클라는 결과를 표시만 한다(보상 조작 방지).
/// </summary>
public interface IAttendanceService {
    UniTask<AttendanceInfo> GetInfoAsync(CancellationToken ct = default);
    UniTask<AttendanceClaimResult> ClaimAsync(CancellationToken ct = default);
}
