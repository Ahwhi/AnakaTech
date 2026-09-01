using System;

/// <summary>
/// 투자 표시/판정 규칙(클라 미러). 해금 조건과 비용, 효과 수치는 서버 InvestmentCatalog 가 정답이며
/// 여기 값은 그것과 반드시 일치해야 한다(어긋나면 화면에 보이는 값과 실제로 받는 값이 달라진다).
///
/// 슬롯 이름/설명 문구를 서버에서 내려받지 않는 이유는 메인 퀘스트와 같다. 같은 슬롯이 화면마다
/// 다르게 불릴 일이 없고, 문구를 고치는 데 서버 배포가 필요 없어야 한다.
///
/// 슬롯 추가는 아래 Slots 배열에 한 줄 넣으면 끝이다(해금 조건은 인덱스에서 파생된다).
/// </summary>
public static class InvestmentRules {
    public const int BaseSlots = 1;
    public const int StarPerSlot = 30;
    public const int MaxLevel = 100;
    public const int GoldPerLevel = 10000;

    /// <summary>퍼센트를 정수로 다루기 위한 분모(1000 분율). 서버와 같은 단위다.</summary>
    public const int PermilleDenominator = 1000;

    public sealed class Slot {
        public readonly int Index;
        public readonly InvestmentEffect Effect;
        public readonly int PermillePerLevel;
        public readonly string DisplayName;
        public readonly string Description;

        public Slot(int index, InvestmentEffect effect, int permillePerLevel, string displayName, string description) {
            Index = index;
            Effect = effect;
            PermillePerLevel = permillePerLevel;
            DisplayName = displayName;
            Description = description;
        }

        /// <summary>이 슬롯이 열리는 데 필요한 별레벨 총합. 첫 칸은 0(항상 열림).</summary>
        public int RequiredStarTotal => Index < BaseSlots ? 0 : (Index - BaseSlots + 1) * StarPerSlot;
    }

    // 서버 InvestmentCatalog.Slots 와 순서/효과/수치가 같아야 한다.
    private static readonly Slot[] Slots = {
    new(0, InvestmentEffect.StageExp, 10,
        "<color=#FF6B6B>스테이지 경험치</color>",
        "스테이지 클리어로 얻는 경험치가 늘어납니다.\n소탕과 기력 부스트에도 함께 적용됩니다."),

    new(1, InvestmentEffect.OfflineExp, 10,
        "<color=#FFA94D>오프라인 경험치</color>",
        "오프라인 보상으로 얻는 경험치가 늘어납니다."),

    new(2, InvestmentEffect.RaidCommissionExp, 10,
        "<color=#FFE066>레이드 의뢰 경험치</color>",
        "레이드 의뢰로 얻는 경험치가 늘어납니다.\n의뢰해결권으로 해결해도 함께 적용됩니다."),

    new(3, InvestmentEffect.SkillGaugeStart, 1,
        "<color=#69DB7C>선제 스킬 게이지</color>",
        "전투를 시작할 때 스쿼드의 스킬 게이지가 채워진 상태로 시작합니다."),

    new(4, InvestmentEffect.SquadRegen, 1,
        "<color=#74C0FC>스쿼드 재생</color>",
        "전투 중 10초마다 스쿼드 전원이 최대 체력의 일부를 회복합니다."),
};

    public static int SlotCount => Slots.Length;

    public static Slot Get(int index) => index >= 0 && index < Slots.Length ? Slots[index] : null;

    /// <summary>별레벨 총합으로 열려 있는 슬롯 수(정의된 슬롯 수를 넘지 않는다).</summary>
    public static int UnlockedSlots(int starTotal) =>
        Math.Min(BaseSlots + Math.Max(0, starTotal) / StarPerSlot, Slots.Length);

    public static bool IsUnlocked(int index, int starTotal) =>
        index >= 0 && index < UnlockedSlots(starTotal);

    /// <summary>현재 레벨에서 다음 레벨로 올릴 때 드는 골드(최대 레벨이면 0).</summary>
    public static int UpgradeCost(int currentLevel) =>
        currentLevel < 0 || currentLevel >= MaxLevel ? 0 : GoldPerLevel * (currentLevel + 1);

    /// <summary>그 레벨에서의 총 효과(1000 분율).</summary>
    public static int PermilleAt(int index, int level) {
        var slot = Get(index);
        if (slot == null || level <= 0) return 0;
        return slot.PermillePerLevel * Math.Min(level, MaxLevel);
    }

    /// <summary>1000 분율을 퍼센트 실수로(표시 전용).</summary>
    public static float ToPercent(int permille) => permille / (float)PermilleDenominator * 100f;

    /// <summary>1000 분율을 배율로(전투 효과 계산용). 예: 50 -> 0.05</summary>
    public static float ToRatio(int permille) => permille / (float)PermilleDenominator;

    /// <summary>
    /// 1000 분율을 퍼센트 문구로(예: "5%", "0.5%"). 소수점이 필요 없으면 정수로 보여 준다.
    /// 투자 효과를 퍼센트로 적는 화면은 전부 이 규칙을 쓴다(화면마다 자릿수가 달라지지 않게).
    /// </summary>
    public static string PercentLabel(int permille) {
        float percent = ToPercent(permille);
        return percent % 1f == 0f ? $"{percent:0}%" : $"{percent:0.#}%";
    }

    /// <summary>효과 표시 문구(예: "+5%", "+0.5%"). 소수점이 필요 없으면 정수로 보여 준다.</summary>
    public static string EffectLabel(int index, int level) => $"+{PercentLabel(PermilleAt(index, level))}";

    /// <summary>해금 안내 문구. 잠긴 슬롯에 그대로 표시한다.</summary>
    public static string UnlockLabel(int index) {
        var slot = Get(index);
        return slot == null ? "" : $"캐릭터 별레벨 {slot.RequiredStarTotal}에서 슬롯 개방";
    }

    /// <summary>업그레이드 실패 사유(서버 InvestmentRepository.Fail) -> 안내 문구.</summary>
    public static string FailMessage(int fail) => fail switch {
        2 => "아직 열리지 않은 슬롯입니다.",
        3 => "캐릭터 별레벨이 부족합니다.",
        4 => "이미 최대 레벨입니다.",
        5 => "골드가 부족합니다.",
        _ => "투자에 실패했습니다."
    };
}
