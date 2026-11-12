/// <summary>
/// 전선 규칙의 클라 미러(V1.12). 실제 판정(기력/하루 한도/단계/경험치)은 서버 Server.Data.FrontlineCatalog 가 하며,
/// 여기 값은 화면 표시에 쓴다. 기력 수치는 서버가 S_FrontlineInfo 로 함께 내려주므로 응답을 받은 뒤에는 그 값을 쓴다.
/// </summary>
public static class FrontlineRules {
    /// <summary>전선 수. id 는 0 부터 Count - 1 이다(FrontlineDatabase 의 순서와 같다).</summary>
    public const int Count = 6;

    public const int StartStage = 1;
    public const int MaxStage = 50;

    /// <summary>스쿼드 정원(덱과 같은 5명).</summary>
    public const int SquadSize = DeckEditSession.Capacity;

    public const int StaminaPerClear = 100;
    public const int DailyStaminaLimit = 1000;

    /// <summary>클리어 1회에 스쿼드 1명이 받는 경험치 = 단계 x 이 값.</summary>
    public const int ExpPerStage = 10000;

    /// <summary>
    /// 서버가 클리어 보고를 받아들이는 입장 후 최소 경과 시간(초). 서버 FrontlineCatalog.MinClearSeconds 와 같아야 한다.
    /// 강한 스쿼드는 이보다 빨리 깨므로, 클라는 이 시간이 지날 때까지 보고를 미룬다.
    /// </summary>
    public const int MinClearSeconds = 3;

    // 데이터베이스에 이름을 적지 않았을 때 쓰는 기본 이름(id 순서).
    private static readonly string[] DefaultNames = { "감시 초소", "고지", "최전방", "차단선", "보급로", "해안선" };

    public static bool IsValidId(int frontlineId) => frontlineId >= 0 && frontlineId < Count;

    public static int ClampStage(int stage) => UnityEngine.Mathf.Clamp(stage, StartStage, MaxStage);

    /// <summary>그 단계를 깼을 때 스쿼드 1명이 받는 경험치. 서버 FrontlineCatalog.ExpFor 와 같아야 한다.</summary>
    public static int ExpFor(int stage) => ClampStage(stage) * ExpPerStage;

    /// <summary>
    /// 투자(전선 경험치)까지 반영한 경험치. 서버가 같은 규칙(InvestmentCatalog.ApplyPermille)으로 지급하므로
    /// 화면에 보이는 값과 실제로 받는 값이 같다.
    /// </summary>
    public static int ExpFor(int stage, int investmentPermille) =>
        InvestmentRules.ApplyPermille(ExpFor(stage), investmentPermille);

    /// <summary>내 계정의 전선 경험치 투자 증가분(1000 분율).</summary>
    public static int InvestmentPermille(PlayerStateStore store) =>
        store != null ? store.InvestmentPermille(InvestmentEffect.FrontlineExp) : 0;

    public static string DefaultName(int frontlineId) => IsValidId(frontlineId) ? DefaultNames[frontlineId] : "";

    public static string StageText(int stage) => $"Lv.{stage}";

    public static string ProgressText(int stage) => $"{stage} / {MaxStage}";

    /// <summary>오늘 전선에 더 쓸 수 있는 기력 표기(숫자만, 라벨은 프리팹에서 따로 둔다). 예: "700 / 1,000".</summary>
    public static string DailyRemainText(int remaining, int limit) => $"{remaining:N0} / {limit:N0}";

    public static string SetMembersFailMessage(int fail) => fail switch {
        3 => "편성을 확인해주세요.",
        4 => "다른 전선에 배치된 소녀입니다.",
        5 => "클리어 기록이 있는 전선의 소녀는 초기화로만 뺄 수 있습니다.",
        _ => "배치를 저장하지 못했습니다."
    };

    public static string EnterFailMessage(int fail) => fail switch {
        3 => "전선 정보가 바뀌었습니다. 다시 시도해주세요.",
        4 => "배치된 소녀가 없습니다.",
        5 => "기력이 부족합니다.",
        6 => "오늘 전선에 사용할 수 있는 기력을 모두 사용했습니다.",
        _ => "전선에 참여하지 못했습니다."
    };

    public static string FinishFailMessage(int fail) => fail switch {
        3 => "전선 정보가 바뀌었습니다.",
        4 => "전투 기록을 확인하지 못했습니다.",
        5 => "기력이 부족합니다.",
        6 => "오늘 전선에 사용할 수 있는 기력을 모두 사용했습니다.",
        _ => "전선 결과를 반영하지 못했습니다."
    };
}
