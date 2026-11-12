using System.Collections.Generic;

/// <summary>
/// 다른 유저의 캐릭터를 전투에 세울 때의 최종 스탯/각인 조립기. 서버가 준 원본 상태(UserDeckDto: 성장 + 장비 + 각인 +
/// 본부 + 투자 + 수련 + 코스튬)를 내 파티와 완전히 같은 조립 경로(CharacterBuild -> CharacterRuntime)에 태운다.
///
/// 쟁탈전/대항전의 상대 파티와 원정대의 파티원이 같은 규칙으로 서야 하므로 조립을 이 한 곳에 둔다.
/// 서버가 최종 스탯을 계산해 보내지 않는 이유는 그러면 조립 규칙이 서버/클라 두 곳에 생기기 때문이고,
/// 보정을 빼고 성장분만 쓰면 "내 덱 100% 대 약해진 상대(파티원)"가 된다.
/// </summary>
public sealed class RemoteCharacterBuilder {
    private readonly BattleSettings _settings;
    private readonly EquipmentDatabase _equipDb;
    private readonly EngravingDatabase _engravingDb;
    private readonly HqNodeDatabase _hqNodeDb;
    private readonly CostumeDatabase _costumeDb;
    private readonly SynergyDatabase _synergyDb;

    public RemoteCharacterBuilder(BattleSettings settings, EquipmentDatabase equipDb, EngravingDatabase engravingDb,
                                  HqNodeDatabase hqNodeDb, CostumeDatabase costumeDb, SynergyDatabase synergyDb) {
        _settings = settings;
        _equipDb = equipDb;
        _engravingDb = engravingDb;
        _hqNodeDb = hqNodeDb;
        _costumeDb = costumeDb;
        _synergyDb = synergyDb;
    }

    /// <summary>
    /// 각인 로드아웃. 항상 적용분(스탯)과 조건부(전투 중 발동)로 나뉘므로 한 번 만들어 스탯 조립과 유닛 양쪽에 쓴다.
    /// </summary>
    public EngravingLoadout Engravings(UserDeckMemberDto member, UserDeckDto deck) {
        deck.EngravingSlots.TryGetValue(member.CharDataId, out var slots);
        return CharacterBuild.Engravings(slots, member.StarLevel, deck.EngravingLevels, _engravingDb);
    }

    /// <summary>캐릭터 1명의 최종 스탯. deckCharIds 는 편성 시너지 판정 기준(그 유저의 편성원 id 목록)이다.</summary>
    public CombatStats Stats(CharacterData data, UserDeckMemberDto member, UserDeckDto deck,
                             EngravingLoadout engravings, IReadOnlyList<int> deckCharIds) {
        var bonus = CharacterBuild.EquipmentBonus(EquippedItemsOf(deck, member.CharDataId), member.CharDataId, _equipDb)
                  + engravings.Always
                  + (_hqNodeDb != null ? _hqNodeDb.TotalBonus(deck.HqNodeLevels) : new CombatStats())
                  + InvestmentRules.StatBonus(deck.InvestmentLevels)
                  + TrainingOf(deck, member.CharDataId)
                  + CharacterBuild.CostumeBonus(member.CostumeId, member.CharDataId, _costumeDb)
                  + CharacterBuild.UnitSkinBonus(member.UnitSkinId, _costumeDb)
                  + CharacterBuild.SynergyBonus(member.CharDataId, deckCharIds, _synergyDb);

        return new CharacterRuntime(data, ToProgress(member), _settings, bonus).FinalStats;
    }

    // 수련 효과. 내 파티와 같은 해석 규칙(TrainingRules)을 통과시키므로 사거리/2회공격/회피가 그대로 동작한다.
    private static CombatStats TrainingOf(UserDeckDto deck, int charDataId) =>
        deck.Trainings.TryGetValue(charDataId, out var t)
            ? TrainingRules.BonusOf(t.Effect, t.ValueTenth)
            : new CombatStats();

    private static CharacterProgress ToProgress(UserDeckMemberDto member) => new() {
        CharDataId = member.CharDataId,
        Level = member.Level,
        StarLevel = member.StarLevel,
        InvestedAttack = member.InvAttack,
        InvestedDefense = member.InvDefense,
        InvestedHealth = member.InvHealth,
        InvestedAttackSpeed = member.InvAttackSpeed,
        InvestedMoveSpeed = member.InvMoveSpeed,
        InvestedSkillAccel = member.InvSkillAccel,
        LimitBroken = member.LimitBroken,
        Shards = member.Shards,   // 한계돌파 조각 보너스가 이 값으로 정해진다
    };

    private static IEnumerable<EquipmentInstanceDto> EquippedItemsOf(UserDeckDto deck, int charDataId) {
        foreach (var e in deck.Equips)
            if (e != null && e.EquippedCharDataId == charDataId) yield return e;
    }
}
