using System;

/// <summary>
/// 서버 권위의 캐릭터 성장 상태(치트 방지 대상). 클라는 이 값을 받아 CharacterData(SO)의 기본 스탯과 합쳐
/// 최종 전투/표시 스탯을 계산한다(CharacterRuntime). 순수 데이터 홀더라 어느 계층에서도 참조 가능하다.
/// </summary>
[Serializable]
public sealed class CharacterProgress {
    public int CharDataId;
    public int Level = 1;
    public int Exp;
    // 서버가 내려주는 값. 받기 전에는 0이며, 표시부는 MaxExp > 0 일 때만 비율을 그린다(0 나눗셈 방지).
    public int MaxExp;
    public int StarLevel = 1;   // 1 ~ 15 (PLANNING 5-9)
    public int StatPoints;      // 미사용 스탯포인트
    public int Shards;          // 별레벨 승급용 조각(PLANNING 5-11)

    // 스탯포인트로 투자한 누적치. 사거리 투자는 폐지(무기타입 고정)되고 스킬가속 투자로 대체됨. 서버 패킷과 필드 일치.
    public int InvestedAttack;
    public int InvestedDefense;
    public int InvestedHealth;
    public int InvestedAttackSpeed;
    public int InvestedMoveSpeed;
    public int InvestedSkillAccel;

    // 스탯포인트 자동 투자 대상(StatType 값). GrowthRules.AutoStatNone(-1) 이면 미설정.
    // 서버가 레벨업 시 이 스탯에 포인트를 바로 넣으므로 클라는 표시만 한다.
    public int AutoStatType = GrowthRules.AutoStatNone;

    // 한계돌파 여부(서버 권위, 한 번 돌파하면 되돌리지 않는다). 돌파한 캐릭터는 CharacterData.SkillFor 로 한계돌파 스킬을 쓴다.
    public bool LimitBroken;

    /// <summary>
    /// 다음 레벨까지의 경험치 진행도(0~1). 표시부(슬라이더/Filled 이미지)가 공통으로 쓴다.
    /// MaxExp 를 아직 받지 못한 상태(0)에서는 0을 돌려 0 나눗셈을 막는다.
    /// </summary>
    public float ExpRatio => MaxExp > 0 ? Math.Clamp((float)Exp / MaxExp, 0f, 1f) : 0f;

    public int Invested(StatType stat) => stat switch {
        StatType.Attack      => InvestedAttack,
        StatType.Defense     => InvestedDefense,
        StatType.Health      => InvestedHealth,
        StatType.AttackSpeed => InvestedAttackSpeed,
        StatType.MoveSpeed   => InvestedMoveSpeed,
        StatType.SkillAccel  => InvestedSkillAccel,
        _ => 0
    };

    /// <summary>
    /// 자동 투자 대상이 설정돼 있으면 그 스탯에 amount 만큼 더하고 true 를 돌려준다.
    /// 미설정이면 아무 것도 하지 않고 false(호출측이 미사용 포인트로 쌓는다).
    /// 서버 ApplyExp 의 자동 투자와 같은 규칙이라 두 쪽 값이 어긋나지 않는다.
    /// </summary>
    public bool AddAutoInvested(int amount) {
        if (amount <= 0 || AutoStatType == GrowthRules.AutoStatNone) return false;
        switch ((StatType)AutoStatType) {
            case StatType.Attack:      InvestedAttack += amount;      return true;
            case StatType.Defense:     InvestedDefense += amount;     return true;
            case StatType.Health:      InvestedHealth += amount;      return true;
            case StatType.AttackSpeed: InvestedAttackSpeed += amount; return true;
            case StatType.MoveSpeed:   InvestedMoveSpeed += amount;   return true;
            case StatType.SkillAccel:  InvestedSkillAccel += amount;  return true;
            default: return false;
        }
    }
}
