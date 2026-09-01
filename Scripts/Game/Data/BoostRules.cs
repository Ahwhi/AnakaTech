/// <summary>
/// 기력 부스트(부스트 모드) 규칙의 클라 표시용 미러. 서버 GameConstants 와 값이 일치해야 한다(판정은 서버 권위).
/// 값이 어긋나면 클라는 켤 수 있는데 서버가 거부하는 상태가 되므로, 서버 상수를 바꿀 때 여기도 함께 고친다.
///
/// 부스트 모드는 던전 1회 클리어를 ClearMultiplier 회분으로 정산한다.
/// 기력을 StaminaPerBoostClear 만큼 한 번에 쓰고 부스트권 1장을 함께 소모하며, 보상도 그만큼 받는다.
/// 해금 진행도는 실제로 싸운 1단계만 오른다(서버 ClearDungeon 규약).
/// </summary>
public static class BoostRules {
    /// <summary>부스트 클리어 1회를 몇 회분으로 치는지. 서버 GameConstants.BoostClearMultiplier 와 일치.</summary>
    public const int ClearMultiplier = 10;

    /// <summary>일반 클리어 1회에 드는 기력. 서버 GameConstants.StaminaPerClear 와 일치.</summary>
    public const int StaminaPerClear = 1;

    /// <summary>부스트 클리어 1회에 드는 기력. 서버 GameConstants.BoostStaminaPerClear 와 일치.</summary>
    public const int StaminaPerBoostClear = StaminaPerClear * ClearMultiplier;

    /// <summary>부스트 클리어 1회에 드는 부스트권.</summary>
    public const int TicketPerBoostClear = 1;

    /// <summary>지금 부스트로 클리어를 정산할 수 있는지(기력과 부스트권을 모두 갖췄는지).</summary>
    public static bool CanBoost(int stamina, int boostTickets) =>
        stamina >= StaminaPerBoostClear && boostTickets >= TicketPerBoostClear;
}
