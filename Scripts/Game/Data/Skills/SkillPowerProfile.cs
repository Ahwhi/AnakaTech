using UnityEngine;

/// <summary>
/// 스킬이 전투력 산출을 위해 스스로 신고하는 화력 요약. 스킬 1회 발동이 내는 데미지의 "모양"만 담는다.
///
/// 이 구조체가 있는 이유: 전투력 계산기가 스킬 종류(단일/광역/관통/연쇄...)를 전혀 몰라도 되게 하는 경계다.
/// 새 스킬 아키타입을 추가할 때 UnitSkill.PowerProfile 만 구현하면 전투력이 자동으로 따라오며,
/// 전투력 계산기(CombatPowerFormula)는 수정할 필요가 없다.
///
/// 필드 규약(중요):
///  - TotalMultiplier / HitCount 는 "대상 1세트에 들어가는 배율/타격"이며 대상 수를 곱하지 않은 값이다.
///  - MaxTargets 가 그 세트를 몇 명에게 반복하는지를 나타낸다. 0 이하는 "적 전체"를 뜻한다.
/// 예) 적 전체에게 80% x 10회  -> (8.0, 10, 0)
///     최대 5명에게 120%+180% -> (3.0,  2, 5)
///     주 대상 100% + 연쇄 2명 각 60%(서로 다른 대상, 1세트로 끝) -> (2.2, 3, 1)
///
/// HitCount 를 따로 두는 이유: 데미지 파이프라인의 방어 감산이 타격 1회마다 걸리므로,
/// 같은 총배율이라도 다단일수록 방어에 더 많이 깎인다. 계산기가 이를 반영하려면 타격 수를 알아야 한다.
/// </summary>
public readonly struct SkillPowerProfile {
    private readonly float _totalMultiplier;
    private readonly int _hitCount;

    /// <summary>대상 1명 기준 1회 발동 총 공격력 배수 합(1.0 = 100%). 음수는 0으로 취급된다.</summary>
    public float TotalMultiplier => Mathf.Max(0f, _totalMultiplier);
    /// <summary>대상 1명 기준 타격 횟수(방어 감산이 타격마다 걸리므로 필요). 항상 1 이상이며 default 에서도 안전하다.</summary>
    public int HitCount => Mathf.Max(1, _hitCount);
    /// <summary>배율 세트가 적용되는 최대 대상 수. 0 이하면 "적 전체"(계산기가 기준 적 수로 환산).</summary>
    public readonly int MaxTargets;

    public SkillPowerProfile(float totalMultiplier, int hitCount, int maxTargets) {
        _totalMultiplier = totalMultiplier;
        _hitCount = hitCount;
        MaxTargets = maxTargets;
    }

    /// <summary>데미지를 주지 않는 스킬(버프/변신 등)의 프로필. 전투력에 화력으로 기여하지 않는다.</summary>
    public static SkillPowerProfile None => default;

    /// <summary>화력이 없어 전투력 계산에서 건너뛸 수 있는지.</summary>
    public bool IsEmpty => _totalMultiplier <= 0f;
}
