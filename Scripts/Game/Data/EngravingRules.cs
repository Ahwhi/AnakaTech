using System.Collections.Generic;

/// <summary>
/// 각인 규칙의 클라 미러(서버 EngravingCatalog 와 값이 일치해야 한다). 실제 판정/차감은 서버가 하며
/// 여기 값은 UI 표시(비용/해금 조건/최대치)와 버튼 활성 조건에만 쓰인다.
/// </summary>
public static class EngravingRules {
    /// <summary>
    /// 슬롯 해금에 필요한 별레벨(슬롯 인덱스 순). 0 은 처음부터 열려 있는 슬롯이고,
    /// 그 뒤로는 별 성장에 목적지를 주도록 단계별 조건을 둔다.
    /// 이 배열의 길이가 곧 슬롯 수다(슬롯을 늘리려면 여기에 원소 하나만 추가한다).
    /// 값은 GrowthRules.MaxStarLevel 이하여야 하고 서버 EngravingCatalog 와 반드시 일치해야 한다.
    /// </summary>
    public static readonly int[] SlotUnlockStar = { 0, 5, 10, 15 };

    /// <summary>캐릭터 1명의 각인 슬롯 수(해금 조건 표의 길이).</summary>
    public static readonly int SlotCount = SlotUnlockStar.Length;

    /// <summary>각인 최대 레벨(계정 공용).</summary>
    public const int MaxLevel = 99;

    public static bool IsValidSlot(int slotIndex) => slotIndex >= 0 && slotIndex < SlotCount;

    public static int RequiredStarForSlot(int slotIndex) =>
        IsValidSlot(slotIndex) ? SlotUnlockStar[slotIndex] : int.MaxValue;

    /// <summary>해당 캐릭터의 별레벨로 이 슬롯을 쓸 수 있는지.</summary>
    public static bool IsSlotUnlocked(int slotIndex, int starLevel) =>
        starLevel >= RequiredStarForSlot(slotIndex);

    /// <summary>현재 레벨에서 다음 레벨로 올리는 데 드는 각인석(강화 비용은 각인석 단일 재화다).</summary>
    public static int UpgradeStoneCost(int currentLevel) => 20 * (currentLevel < 1 ? 1 : currentLevel);

    /// <summary>발동 조건의 표시 문구(효과 요약 앞에 붙는다). Always 는 조건 문구가 없다.</summary>
    public static string TriggerText(EngravingTrigger trigger, float param) => trigger switch {
        EngravingTrigger.BattleStart => $"전투 시작 {param:0.#}초간",
        EngravingTrigger.SelfHpBelow => $"체력 {param:0.#}% 이하일 때",
        _ => ""
    };

    /// <summary>계열 표시명(각인 목록 분류 표시용).</summary>
    public static string CategoryName(EngravingCategory category) => category switch {
        EngravingCategory.Offense => "공격",
        EngravingCategory.Defense => "생존",
        EngravingCategory.Tempo => "기교",
        _ => ""
    };

    /// <summary>각인 장착/강화 실패 사유(서버 코드) -> 안내 문구. 서버 EquipEngraving/UpgradeEngraving 과 짝을 이룬다.</summary>
    public static string EquipFailMessage(int fail) => fail switch {
        1 => "캐릭터 정보를 찾을 수 없습니다.",
        2 => "잘못된 각인 슬롯입니다.",
        3 => "별레벨이 부족해 아직 열리지 않은 슬롯입니다.",
        4 => "보유하지 않은 각인입니다.",
        5 => "같은 각인을 두 슬롯에 장착할 수 없습니다.",
        _ => "각인을 장착할 수 없습니다."
    };

    public static string UpgradeFailMessage(int fail) => fail switch {
        1 => "보유하지 않은 각인입니다.",
        2 => "이미 최대 레벨입니다.",
        3 => "각인석이 부족합니다.",
        _ => "각인을 강화할 수 없습니다."
    };
}

/// <summary>
/// 한 캐릭터에 장착된 각인들이 만들어내는 전투 보정. 항상 적용분과 조건부 적용분을 나눠 담는다.
///  - Always      : 성장 스탯처럼 합산되어 전투력/상세 스탯 표시에도 반영된다.
///  - Conditionals: 전투 중 조건이 맞을 때만 붙는다. 상세 스탯에는 나오지 않고,
///                  전투력에는 가동률만큼 환산되어 반영된다(CombatPowerFormula).
/// 전투(CharacterUnit)와 표시(전투력 계산)가 같은 소스를 쓰도록 PlayerStateStore 가 이 객체를 만든다.
/// </summary>
public sealed class EngravingLoadout {
    /// <summary>조건부 보정 1줄.</summary>
    public readonly struct Conditional {
        public readonly EngravingTrigger Trigger;
        public readonly float Param;
        public readonly StatType Stat;
        public readonly float Value;

        public Conditional(EngravingTrigger trigger, float param, StatType stat, float value) {
            Trigger = trigger; Param = param; Stat = stat; Value = value;
        }
    }

    /// <summary>각인이 없는 캐릭터가 공유하는 빈 로드아웃(매번 할당하지 않기 위한 공용 인스턴스).</summary>
    public static readonly EngravingLoadout Empty = new EngravingLoadout(default, null);

    public readonly CombatStats Always;
    public readonly IReadOnlyList<Conditional> Conditionals;

    public EngravingLoadout(CombatStats always, IReadOnlyList<Conditional> conditionals) {
        Always = always;
        Conditionals = conditionals ?? System.Array.Empty<Conditional>();
    }

    /// <summary>조건부 효과가 하나라도 있는지(없으면 전투 중 조건 판정을 아예 건너뛴다).</summary>
    public bool HasConditionals => Conditionals.Count > 0;
}
