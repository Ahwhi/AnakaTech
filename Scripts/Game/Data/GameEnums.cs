using System;

// 게임 도메인 공용 열거형. 서버와는 "인덱스(id)"만 공유하며, 이 열거형 자체는 클라 표현/분기용이다.
// 프로젝트 관례에 따라 전역 네임스페이스에 둔다(플러그인 외 스크립트는 네임스페이스를 쓰지 않음).

/// <summary>캐릭터 직업 타입. 장착 가능한 무기 종류를 결정한다.</summary>
public enum CharacterType {
    Warrior,   // 전사 = 검
    Archer,    // 궁수 = 활
    Gunner,    // 총잡이 = 총
    Mage,      // 마법사 = 스태프
    Fighter    // 격투가 = 너클
}

/// <summary>등급. 오른쪽으로 갈수록 강력하다(N &lt; R &lt; SR &lt; SSR &lt; XR).</summary>
public enum Grade {
    N,
    R,
    SR,
    SSR,
    XR
}

/// <summary>장비 슬롯.</summary>
public enum EquipmentSlot {
    Weapon,
    Armor,
    Shoes,
    Accessory
}

/// <summary>무기 세부 종류. 캐릭터 타입별로 장착 가능한 무기가 정해진다.</summary>
public enum WeaponType {
    None,
    Sword,     // 검  (전사)
    Bow,       // 활  (궁수)
    Gun,       // 총  (총잡이)
    Knuckle,   // 너클(격투가)
    Staff      // 스태프(마법사)
}

/// <summary>
/// 스탯 종류. 값(정수 인덱스)은 서버 패킷(C_AllocateStat.statType 등)과 직렬화에 쓰이므로
/// 순서를 바꾸지 말고 새 스탯은 항상 아래에 추가한다.
/// 투자 가능: Attack/Defense/Health/AttackSpeed/MoveSpeed/SkillAccel(GameEnumRules.InvestableStats).
/// 그 외(Range/치명타/최소최대데미지비율/방어관통/최종데미지/스킬쿨감)는 투자 불가이며 별 성장 또는 무기타입 고정이다.
/// </summary>
public enum StatType {
    Attack,
    Defense,
    Health,
    Range,
    AttackSpeed,
    MoveSpeed,
    CritRate,
    CritDamage,
    // 아래는 후속 추가분(인덱스 8~). 순서 고정.
    // 계정 투자(InvestmentRules.StatOf)로도 오르는 스탯: CritRate/MinDamageRatio/MaxDamageRatio/FinalDamage/
    // DamageTakenReduction/DoubleAttackChance/EvasionChance.
    MinDamageRatio,          // 8  최소 데미지 비율(%) - 지터 하한 상승(별 성장, 투자)
    MaxDamageRatio,          // 9  최대 데미지 비율(%) - 지터 상한 상승(별 성장, 투자)
    ArmorPen,                // 10 방어구 관통력(방어력을 깎는 절대 수치, V1.12 부터) - 별 성장 전용
    FinalDamage,             // 11 최종 데미지(%)     - 별 성장, 수련, 투자
    SkillAccel,              // 12 스킬 가속(%)       - 투자 가능
    SkillCooldownReduction,  // 13 스킬 쿨타임 감소(초) - 별 성장 전용
    // 방어 스탯(인덱스 14~). 장비 옵션 전용이며 전부 점근 감쇄(CombatStats.Resist01)를 통과한다.
    CritResist,              // 14 치명타 저항(%)      - 받는 치명타의 추가 배율만 감쇄
    StatusResist,            // 15 상태이상 저항(%)    - 상태이상 지속시간 감쇄(상태이상 구현 시 소비)
    DamageTakenReduction,    // 16 피격 데미지 감소(%) - 최종 데미지에 독립 곱연산
    // 발동형 스탯(인덱스 17~). 수련과 투자로 오르며 값이 곧 발동 확률(%)이라 점근 감쇄를 쓰지 않는다.
    DoubleAttackChance,      // 17 2회 공격 확률(%)   - 기본공격이 짧은 간격을 두고 한 번 더 나간다
    EvasionChance            // 18 회피 확률(%)       - 피격을 무효로 하고 MISS 를 표시한다
}

/// <summary>
/// 메인 화면 자동 전투 모드. 클리어한 뒤 어디로 갈지를 정한다(서버와 무관한 클라 진행 규칙).
/// 파티가 전멸하면 어느 모드든 한 단계 내려가고 Hunt 로 돌아온다(BattleDirector).
/// </summary>
public enum AutoBattleMode {
    Hunt = 0,        // 사냥: 같은 던전을 계속 반복한다(기본).
    Challenge = 1    // 도전: 클리어할 때마다 다음 던전으로 올라간다.
}

/// <summary>
/// 기력 부스트 모드가 저절로 꺼진 이유. 부스트는 기력과 부스트권을 함께 쓰므로 어느 쪽이 바닥났는지에 따라
/// 유저가 할 일이 다르다(기다리기 / 상점에서 사기). 안내 문구를 고르는 쪽이 판단하지 않도록 전투 루프가 사유를 실어 보낸다.
/// </summary>
public enum BoostStopReason {
    TicketDepleted = 0,   // 기력 부스트권이 모자람.
    StaminaDepleted = 1   // 기력이 모자람(부스트 1회분에 못 미침).
}

/// <summary>
/// 재화 종류. 실제 결제/경제는 서버 권위. 값(정수 인덱스)은 CurrencyCatalog 에셋 등에 직렬화되므로
/// 기존 값을 재배치하지 말고 새 재화는 항상 아래에 새 값으로 추가한다(값을 명시해 실수로 밀리지 않게 한다).
/// </summary>
public enum CurrencyType {
    Gold = 0,
    Diamond = 1,
    Stamina = 2,
    NormalTicket = 3,     // 일반 소환권(일반 캐릭터/장비 뽑기 공용)
    // 4 = 구 GoldTicket. 소환권을 일반/특수 2종으로 정리하면서 폐지했다.
    // 뒤 값들이 밀리면 직렬화된 에셋의 매핑이 어긋나므로 값 자체를 비워 둔다(재사용 금지).
    SpecialTicket = 5,    // 특수 소환권(특수 캐릭터/장비 뽑기 공용)
    NicknameTicket = 6,   // 닉네임 변경권(아이콘 표시는 CurrencyCatalog 공용, 보유 수량은 프로필 소스)
    // 의뢰 컨텐츠 재화(서버 PlayerCurrency 키: commission_ticket / renown / engraving_stone).
    CommissionTicket = 7, // 의뢰해결권. 매일 자동 충전되며 의뢰 도전/즉시해결에 1장씩 쓰인다.
    Renown = 8,           // 명성. 용병단 본부 노드를 여는 계정 단위 성장 재화.
    EngravingStone = 9,   // 각인석. 각인 레벨(계정 공용)을 올리는 재료.
    StageSweepTicket = 10, // 스테이지 소탕권. 진행 중인 던전을 장수만큼 즉시 클리어한다.
    StaminaBoostTicket = 11, // 기력 부스트권. 부스트 모드 클리어 1회에 1장씩 쓰이며 매일 자정 자동 충전된다.
    GuildPoint = 12,         // 길드 포인트. 길드 레이드/대항전 보상으로 쌓이며 길드 상점의 결제 재화다.
    TrainingTicket = 13,     // 수련 완료권. 진행 중인 수련의 남은 시간을 즉시 당긴다(상점에서 주 1개 판매).
    ArenaCoin = 14,          // 쟁탈전 코인. 쟁탈전 승패/일일/주간 보상으로 쌓이며 쟁탈전 상점의 결제 재화다.
    DispatchCertificate = 15, // 파견 증명서. 파견 완료 보상으로만 쌓이며 코스튬 구매의 결제 재화다.
    GachaMileage = 16,       // 모집 마일리지. 모집을 돌리면 소녀/장비를 가리지 않고 쌓이며 SSR 소녀 픽업에 쓴다.
    LimitBreakTicket = 17    // 한계돌파권. 조건을 채운 캐릭터를 한계돌파할 때 1장 쓰인다.
}

/// <summary>
/// 의뢰 종류. 값(정수 인덱스)은 서버/DB 와 공유하므로 순서를 바꾸지 말고 새 종류는 항상 아래에 추가한다.
/// 서버 Server.Data.CommissionType 과 값이 일치해야 한다.
/// </summary>
public enum CommissionType {
    Raid,          // 레이드 의뢰: 보스 1기, 제한시간 내 처치
    Subjugation,   // 토벌 의뢰: 목표 수만큼 처치
    Escort         // 호위 의뢰: 의뢰인을 지키며 제한시간 생존
}

/// <summary>열거형 관련 공용 규칙(투자 가능 여부, 타입-무기 매핑 등)을 모은 헬퍼.</summary>
public static class GameEnumRules {
    // 스탯포인트로 투자 가능한 스탯(6종). 사거리 제외(무기타입 고정), 스킬가속 추가.
    // 그 외 스탯(치명타/최소최대데미지비율/방어관통/최종데미지/스킬쿨감)은 별 성장 전용.
    public static readonly StatType[] InvestableStats = {
        StatType.Attack,
        StatType.Defense,
        StatType.Health,
        StatType.AttackSpeed,
        StatType.MoveSpeed,
        StatType.SkillAccel
    };

    public static bool IsInvestable(StatType stat) {
        for (int i = 0; i < InvestableStats.Length; i++)
            if (InvestableStats[i] == stat) return true;
        return false;
    }

    /// <summary>캐릭터 타입이 장착할 수 있는 무기 종류(PLANNING 5-5).</summary>
    public static WeaponType WeaponFor(CharacterType type) => type switch {
        CharacterType.Warrior => WeaponType.Sword,
        CharacterType.Archer  => WeaponType.Bow,
        CharacterType.Gunner  => WeaponType.Gun,
        CharacterType.Mage    => WeaponType.Staff,
        CharacterType.Fighter => WeaponType.Knuckle,
        _ => WeaponType.None
    };

    /// <summary>해당 캐릭터 타입이 이 무기를 장착할 수 있는지.</summary>
    public static bool CanEquip(CharacterType type, WeaponType weapon) =>
        WeaponFor(type) == weapon;

    /// <summary>무기 종류 표시명(UI 공용).</summary>
    public static string WeaponDisplayName(WeaponType type) => type switch {
        WeaponType.Sword => "검",
        WeaponType.Bow => "활",
        WeaponType.Gun => "총",
        WeaponType.Knuckle => "너클",
        WeaponType.Staff => "스태프",
        _ => "-"
    };

    /// <summary>
    /// 등급별 중복 획득 시 지급되는 조각 수.
    /// 서버 GameRoster.DupShardsForGrade 와 값이 반드시 같아야 한다(실제 지급은 서버가 확정한다).
    /// </summary>
    public static int ShardsOnDuplicate(Grade grade) => grade switch {
        Grade.N   => 5,
        Grade.R   => 5,
        Grade.SR  => 10,
        Grade.SSR => 50,
        Grade.XR  => 50,
        _ => 0
    };
}
