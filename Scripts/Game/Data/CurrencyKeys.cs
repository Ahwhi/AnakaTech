/// <summary>
/// 서버 범용 재화 키(PlayerCurrency.CurrencyKey) 와 클라 CurrencyType 의 단일 매핑 지점.
/// 보상 표시/지갑 반영 등 키 문자열이 등장하는 곳이 여러 군데라, 새 재화를 추가할 때 고칠 자리를 여기 하나로 모은다.
/// 서버 키 문자열과 반드시 값이 일치해야 한다(오타는 조용히 아이콘/수량이 비는 형태로 나타난다).
/// </summary>
public static class CurrencyKeys {
    public const string Gold = "gold";
    public const string Diamond = "diamond";
    public const string Stamina = "stamina";
    public const string NormalTicket = "normal_ticket";
    public const string SpecialTicket = "special_ticket";
    public const string NicknameTicket = "nickname_ticket";
    public const string CommissionTicket = "commission_ticket";
    public const string Renown = "renown";
    public const string EngravingStone = "engraving_stone";
    public const string StageSweepTicket = "stage_sweep_ticket";
    public const string StaminaBoostTicket = "stamina_boost_ticket";
    public const string GuildPoint = "guild_point";
    public const string TrainingTicket = "training_ticket";
    public const string ArenaCoin = "arena_coin";
    public const string DispatchCertificate = "dispatch_certificate";

    /// <summary>서버 재화 키 -> 클라 재화 종류. 모르는 키면 null(표시 소비측이 키 문자열을 그대로 쓴다).</summary>
    public static CurrencyType? ToType(string key) => key switch {
        Gold => CurrencyType.Gold,
        Diamond => CurrencyType.Diamond,
        Stamina => CurrencyType.Stamina,
        NormalTicket => CurrencyType.NormalTicket,
        SpecialTicket => CurrencyType.SpecialTicket,
        NicknameTicket => CurrencyType.NicknameTicket,
        CommissionTicket => CurrencyType.CommissionTicket,
        Renown => CurrencyType.Renown,
        EngravingStone => CurrencyType.EngravingStone,
        StageSweepTicket => CurrencyType.StageSweepTicket,
        StaminaBoostTicket => CurrencyType.StaminaBoostTicket,
        GuildPoint => CurrencyType.GuildPoint,
        TrainingTicket => CurrencyType.TrainingTicket,
        ArenaCoin => CurrencyType.ArenaCoin,
        DispatchCertificate => CurrencyType.DispatchCertificate,
        _ => null
    };

    /// <summary>클라 재화 종류 -> 서버 재화 키. 지갑 컬럼으로 관리되는 재화도 키 표현이 필요한 곳이 있어 함께 둔다.</summary>
    public static string ToKey(CurrencyType type) => type switch {
        CurrencyType.Gold => Gold,
        CurrencyType.Diamond => Diamond,
        CurrencyType.Stamina => Stamina,
        CurrencyType.NormalTicket => NormalTicket,
        CurrencyType.SpecialTicket => SpecialTicket,
        CurrencyType.NicknameTicket => NicknameTicket,
        CurrencyType.CommissionTicket => CommissionTicket,
        CurrencyType.Renown => Renown,
        CurrencyType.EngravingStone => EngravingStone,
        CurrencyType.StageSweepTicket => StageSweepTicket,
        CurrencyType.StaminaBoostTicket => StaminaBoostTicket,
        CurrencyType.GuildPoint => GuildPoint,
        CurrencyType.TrainingTicket => TrainingTicket,
        CurrencyType.ArenaCoin => ArenaCoin,
        CurrencyType.DispatchCertificate => DispatchCertificate,
        _ => ""
    };
}
