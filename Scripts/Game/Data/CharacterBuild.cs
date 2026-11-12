using System.Collections.Generic;

/// <summary>
/// 캐릭터 1명이 성장(레벨/별/투자) 밖에서 받는 보정을 원본 상태로부터 조립하는 단일 관문.
///
/// 왜 따로 두는가: 같은 조립을 두 곳이 쓴다.
///   - 내 파티   : PlayerStateStore 가 자기 보유 데이터로 부른다.
///   - 상대 파티 : 대전(PvP)에서 서버가 보내 준 상대의 원본 상태로 부른다.
/// 규칙을 여기 한 곳에만 두어야 "내 덱 100% vs 상대 덱 100%" 가 성립한다.
/// 어느 한쪽만 장비/각인이 빠지면 그건 대전이 아니라 일방적인 사냥이 된다.
///
/// 최종 스탯 계산(CharacterRuntime)은 여전히 그 클래스가 하고, 여기서는 "더해질 보정"만 만든다.
/// </summary>
public static class CharacterBuild {
    /// <summary>
    /// 장착 장비가 주는 보정 합계. 슬롯당 1개만 센다(같은 슬롯에 두 개가 장착된 것으로 내려와도 중복 합산하지 않는다).
    /// 전용 무기 보너스도 여기서 함께 더한다.
    /// </summary>
    /// <param name="equipped">이 캐릭터가 장착한 장비들(이미 캐릭터별로 걸러진 목록).</param>
    public static CombatStats EquipmentBonus(IEnumerable<EquipmentInstanceDto> equipped, int charDataId,
                                             EquipmentDatabase db) {
        var sum = new CombatStats();
        if (db == null || charDataId == 0 || equipped == null) return sum;

        var countedSlots = new HashSet<EquipmentSlot>();
        foreach (var e in equipped) {
            if (e == null) continue;
            var data = db.Get(e.EquipDataId);
            if (data == null) continue;
            if (!countedSlots.Add(data.Slot)) continue;

            // 한계돌파 배수는 스탯과 전용무기 효과 양쪽에 붙는다(EquipmentRules 가 함께 처리한다).
            sum += EquipmentRules.ResolveStats(e, data);
            if (data.IsOwnSignatureOf(charDataId))
                sum += EquipmentRules.SignatureBonus(data.Grade, data.WeaponType, data.Id, e.LimitBroken);
        }
        return sum;
    }

    /// <summary>
    /// 장착 코스튬이 주는 보정. 코스튬은 외형만이 아니라 스탯(사거리/공격력 등 모든 스탯)도 건드리므로
    /// 장비/각인과 같은 축으로 합산된다. 그 캐릭터의 것이 아닌 코스튬은 무시한다(변조 방어).
    ///
    /// 내 파티와 대전 상대가 이 함수 하나를 공유하기 때문에, 상대가 끼운 코스튬의 효과도 내 화면의 전투에
    /// 100% 그대로 살아난다("내 성장 100% 대 상대 성장 100%").
    /// </summary>
    /// <param name="costumeId">장착 코스튬 id(0 = 기본 일러스트, 보정 없음).</param>
    public static CombatStats CostumeBonus(int costumeId, int charDataId, CostumeDatabase db) {
        if (costumeId == 0 || db == null) return new CombatStats();
        var data = db.Get(costumeId);
        if (data == null || data.CharacterId != charDataId) return new CombatStats();
        return data.BonusStats;
    }

    /// <summary>
    /// 장착 아웃핏이 주는 보정(V1.12). 코스튬과 같은 축으로 더하며, 내 파티와 대전 상대가 이 함수를 공유한다.
    /// </summary>
    /// <param name="unitSkinId">장착 아웃핏 id(0 = 기본 아웃핏, 보정 없음).</param>
    public static CombatStats UnitSkinBonus(int unitSkinId, CostumeDatabase db) =>
        unitSkinId != 0 && db != null && db.UnitSkins != null ? db.UnitSkins.BonusFor(unitSkinId) : new CombatStats();

    /// <summary>
    /// 편성 시너지가 주는 보정. 시너지는 "정해진 캐릭터들이 한 덱에 모두 모였을 때 편성 전원이" 효과를 받으므로,
    /// 캐릭터 1명이 아니라 덱 구성 전체를 봐야 판정할 수 있다(그래서 인자에 덱이 들어간다).
    ///
    /// 여기 있는 덕분에 대전 상대의 시너지도 그대로 살아난다. 서버는 상대 덱 구성(UserDeckDto.Members)을
    /// 이미 보내 주므로, 상대 편성으로 이 함수를 부르면 내 파티와 완전히 같은 규칙으로 계산된다.
    /// 시너지만 서버가 따로 내려줄 필요가 없는 이유다.
    /// </summary>
    /// <param name="deckCharIds">이 캐릭터가 속한 덱의 편성 전원(자기 자신 포함).</param>
    public static CombatStats SynergyBonus(int charDataId, IReadOnlyList<int> deckCharIds, SynergyDatabase db) {
        if (db == null) return new CombatStats();
        return db.BonusFor(charDataId, deckCharIds);
    }

    /// <summary>
    /// 각인 로드아웃(항상 적용분 + 조건부 목록). 조건부는 전투 유닛이 받아 스스로 켜고 끈다
    /// (전투 시작 N초간 같은 효과가 여기서 만들어져 그대로 전달된다).
    ///
    /// 효과 수치는 계정 공용 각인 레벨로 정해지고, 별레벨로 열리지 않은 슬롯은 장착 정보가 있어도 무시한다.
    /// </summary>
    /// <param name="slots">슬롯 배열(인덱스가 곧 슬롯 번호). 0 은 빈 슬롯.</param>
    /// <param name="starLevel">이 캐릭터의 별레벨(슬롯 개방 판정).</param>
    /// <param name="levelsByEngravingId">계정의 각인 레벨 표.</param>
    public static EngravingLoadout Engravings(IReadOnlyList<int> slots, int starLevel,
                                              IReadOnlyDictionary<int, int> levelsByEngravingId,
                                              EngravingDatabase db) {
        if (db == null || slots == null) return EngravingLoadout.Empty;

        var always = new CombatStats();
        List<EngravingLoadout.Conditional> conditionals = null;

        for (int i = 0; i < slots.Count; i++) {
            int engravingId = slots[i];
            if (engravingId == 0) continue;
            if (!EngravingRules.IsSlotUnlocked(i, starLevel)) continue;

            var data = db.Get(engravingId);
            if (data == null || data.Effects == null) continue;

            int level = 0;
            levelsByEngravingId?.TryGetValue(engravingId, out level);
            if (level < 1) continue;

            foreach (var effect in data.Effects) {
                float value = effect.ValueAt(level);
                if (effect.Trigger == EngravingTrigger.Always) {
                    always = always.WithAdded(effect.Stat, value);
                } else {
                    conditionals ??= new List<EngravingLoadout.Conditional>();
                    conditionals.Add(new EngravingLoadout.Conditional(effect.Trigger, effect.TriggerParam, effect.Stat, value));
                }
            }
        }

        if (conditionals == null && always.IsZero) return EngravingLoadout.Empty;
        return new EngravingLoadout(always, conditionals);
    }
}
