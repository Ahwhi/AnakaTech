using System;
using System.Collections.Generic;

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
    /// <summary>슬롯 하나의 최대 레벨(V1.12 에 100 에서 200 으로 확장).</summary>
    public const int MaxLevel = 200;
    /// <summary>골드로 올릴 수 있는 마지막 레벨. 이 레벨부터 다음 레벨로는 다이아로 올린다.</summary>
    public const int GoldMaxLevel = 100;
    public const int GoldPerLevel = 10000;
    public const int DiamondPerLevel = 100;

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

    new(5, InvestmentEffect.DoubleAttackChance, 1,
        "<color=#9775FA>2회 공격 확률</color>",
        "모든 캐릭터의 기본공격이 한 번 더 나갈 확률이 늘어납니다."),

    new(6, InvestmentEffect.EvasionChance, 1,
        "<color=#63E6BE>회피 확률</color>",
        "모든 캐릭터가 공격을 회피할 확률이 늘어납니다."),

    new(7, InvestmentEffect.CritRate, 1,
        "<color=#FF8787>치명타 확률</color>",
        "모든 캐릭터의 치명타 확률이 늘어납니다."),

    new(8, InvestmentEffect.MinDamageRatio, 10,
        "<color=#FFD43B>최소 데미지 비율</color>",
        "모든 캐릭터의 데미지 하한이 올라가 데미지가 안정적으로 들어갑니다."),

    new(9, InvestmentEffect.MaxDamageRatio, 10,
        "<color=#FF922B>최대 데미지 비율</color>",
        "모든 캐릭터의 데미지 상한이 올라갑니다."),

    new(10, InvestmentEffect.DamageTakenReduction, 1,
        "<color=#4DABF7>피격 데미지 감소</color>",
        "모든 캐릭터가 받는 데미지가 줄어듭니다."),

    new(11, InvestmentEffect.FinalDamage, 1,
        "<color=#F783AC>최종 데미지</color>",
        "모든 캐릭터의 최종 데미지가 늘어납니다."),

    new(12, InvestmentEffect.DoomExp, 10,
        "<color=#B197FC>멸망전 경험치</color>",
        "멸망전이 끝날 때 보유한 소녀 전원이 얻는 경험치가 늘어납니다."),

    new(13, InvestmentEffect.FrontlineExp, 10,
        "<color=#38D9A9>전선 경험치</color>",
        "전선을 클리어할 때 스쿼드 소녀가 얻는 경험치가 늘어납니다."),
};

    public static int SlotCount => Slots.Length;

    public static Slot Get(int index) => index >= 0 && index < Slots.Length ? Slots[index] : null;

    /// <summary>
    /// 효과가 올리는 전투 스탯. 스탯이 아닌 효과(경험치 증폭, 선제 게이지, 스쿼드 재생)는 null 이다.
    /// 매핑을 여기 한 곳에만 두어 전투/전투력/대전 상대가 같은 결론을 쓴다.
    /// </summary>
    public static StatType? StatOf(InvestmentEffect effect) => effect switch {
        InvestmentEffect.DoubleAttackChance   => StatType.DoubleAttackChance,
        InvestmentEffect.EvasionChance        => StatType.EvasionChance,
        InvestmentEffect.CritRate             => StatType.CritRate,
        InvestmentEffect.MinDamageRatio       => StatType.MinDamageRatio,
        InvestmentEffect.MaxDamageRatio       => StatType.MaxDamageRatio,
        InvestmentEffect.DamageTakenReduction => StatType.DamageTakenReduction,
        InvestmentEffect.FinalDamage          => StatType.FinalDamage,
        _ => null
    };

    /// <summary>
    /// 슬롯 레벨 묶음(슬롯 번호 -> 레벨)에서 지정 효과의 총 증가분(1000 분율).
    /// 내 계정(PlayerStateStore)과 대전 상대(UserDeckDto)가 같은 계산을 쓰도록 여기 둔다.
    /// </summary>
    public static int PermilleOf(IReadOnlyDictionary<int, int> levels, InvestmentEffect effect) {
        if (levels == null) return 0;
        int permille = 0;
        foreach (var kv in levels) {
            var slot = Get(kv.Key);
            if (slot == null || slot.Effect != effect) continue;
            permille += PermilleAt(kv.Key, kv.Value);
        }
        return permille;
    }

    /// <summary>
    /// 슬롯 레벨 묶음이 모든 캐릭터에게 주는 스탯 보정. CombatStats 의 퍼센트 단위(15 = 15%)로 환산한다.
    /// 스탯이 아닌 효과는 여기서 제외되며 각 전투 로직이 PermilleOf 로 따로 적용한다.
    /// </summary>
    public static CombatStats StatBonus(IReadOnlyDictionary<int, int> levels) {
        var stats = new CombatStats();
        if (levels == null) return stats;
        foreach (var kv in levels) {
            var slot = Get(kv.Key);
            if (slot == null) continue;
            var stat = StatOf(slot.Effect);
            if (stat == null) continue;
            int permille = PermilleAt(kv.Key, kv.Value);
            if (permille > 0) stats = stats.WithAdded(stat.Value, ToPercent(permille));
        }
        return stats;
    }

    /// <summary>별레벨 총합으로 열려 있는 슬롯 수(정의된 슬롯 수를 넘지 않는다).</summary>
    public static int UnlockedSlots(int starTotal) =>
        Math.Min(BaseSlots + Math.Max(0, starTotal) / StarPerSlot, Slots.Length);

    public static bool IsUnlocked(int index, int starTotal) =>
        index >= 0 && index < UnlockedSlots(starTotal);

    /// <summary>
    /// 화면에 보여 줄 레벨 상한. 골드 구간을 다 채우기 전에는 골드 구간 끝(100)을, 채운 뒤에는 실제 최대(200)를 보여 준다.
    /// 처음부터 200 을 보이면 골드만으로 닿을 수 없는 목표가 먼저 보여 부담만 커진다.
    /// </summary>
    public static int DisplayMaxLevel(int level) => level < GoldMaxLevel ? GoldMaxLevel : MaxLevel;

    /// <summary>현재 레벨에서 다음 레벨로 올릴 때 쓰는 재화(100 레벨까지 골드, 이후 다이아).</summary>
    public static CurrencyType UpgradeCurrency(int currentLevel) =>
        currentLevel >= GoldMaxLevel ? CurrencyType.Diamond : CurrencyType.Gold;

    /// <summary>
    /// 현재 레벨에서 다음 레벨로 올릴 때 드는 재화의 양(최대 레벨이면 0). 서버 InvestmentCatalog.UpgradeCost 미러.
    /// 골드 구간 = 다음 레벨 x 10,000, 다이아 구간 = (현재 레벨 - 99) x 100.
    /// </summary>
    public static int UpgradeCost(int currentLevel) {
        if (currentLevel < 0 || currentLevel >= MaxLevel) return 0;
        return UpgradeCurrency(currentLevel) == CurrencyType.Diamond
            ? DiamondPerLevel * (currentLevel - GoldMaxLevel + 1)
            : GoldPerLevel * (currentLevel + 1);
    }

    /// <summary>그 레벨에서의 총 효과(1000 분율).</summary>
    public static int PermilleAt(int index, int level) {
        var slot = Get(index);
        if (slot == null || level <= 0) return 0;
        return slot.PermillePerLevel * Math.Min(level, MaxLevel);
    }

    /// <summary>
    /// 1000 분율 증가분을 정수 값에 적용한다(서버 InvestmentCatalog.ApplyPermille 미러).
    /// 반올림 규칙("0.5 이상이면 올림")까지 같아야 미리보기와 실제 지급이 1 단위도 어긋나지 않는다.
    /// </summary>
    public static int ApplyPermille(int amount, int permille) {
        if (amount <= 0 || permille <= 0) return amount;

        long scaled = (long)amount * (PermilleDenominator + permille);
        return (int)((scaled + PermilleDenominator / 2) / PermilleDenominator);
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

    /// <summary>
    /// 보상에 투자가 반영됐음을 알리는 문구(예: "투자 15% 반영"). 증가분이 없으면 빈 문자열.
    /// 의뢰/전선처럼 투자로 보상이 늘어나는 화면이 같은 문구를 쓴다.
    /// </summary>
    public static string BonusLabel(int permille) =>
        permille > 0 ? $"투자 {PercentLabel(permille)} 반영" : "";

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
        6 => "다이아가 부족합니다.",
        7 => "투자 정보가 바뀌었습니다. 다시 시도해주세요.",
        _ => "투자에 실패했습니다."
    };
}
