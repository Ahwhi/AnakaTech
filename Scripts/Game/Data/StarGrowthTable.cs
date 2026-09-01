using UnityEngine;

/// <summary>
/// 별레벨 성장표(승급별 스탯 델타). 붕괴 원인이던 "전 스탯 균등 증가"를 폐기하고 승급마다 오르는 스탯을 명시한다.
/// 인덱스 i = 별 (i+1)성 -> (i+2)성 으로 승급할 때 더해지는 스탯 델타(1성 기준값 위에 누적).
/// 단위: 공/방/체 = 절대값, 그 외 = 퍼센트 포인트(%p), SkillCooldownReduction = 감소 초.
/// 값은 인스펙터에서 자유롭게 튜닝한다(에셋 생성 시 Reset 으로 확정 기본값이 채워진다).
/// </summary>
[CreateAssetMenu(fileName = "StarGrowthTable", menuName = "Anaka/Data/Star Growth Table")]
public sealed class StarGrowthTable : ScriptableObject {
    // 1성 시작이므로 승급은 최대 (MaxStarLevel-1)번. 각 원소가 그 승급의 델타.
    [Tooltip("승급별 스탯 델타. 인덱스 0 = 1->2성. 길이는 (최대별레벨-1)=14.")]
    [SerializeField] private CombatStats[] steps = new CombatStats[GrowthRules.MaxStarLevel - 1];

    /// <summary>starLevel(1~Max)에 도달하기까지 누적된 성장 델타. 1성이면 0.</summary>
    public CombatStats CumulativeTo(int starLevel) {
        var sum = default(CombatStats);
        if (steps == null) return sum;
        int upgrades = Mathf.Clamp(starLevel - 1, 0, steps.Length);
        for (int i = 0; i < upgrades; i++) sum += steps[i];
        return sum;
    }

    /// <summary>fromStar 성에서 다음 성으로 승급할 때 상승하는 델타(UI 미리보기용). 범위를 벗어나면 0.</summary>
    public CombatStats StepDelta(int fromStar) {
        int idx = fromStar - 1;
        if (steps == null || idx < 0 || idx >= steps.Length) return default;
        return steps[idx];
    }

    // 에셋 생성 시 확정 기본값을 채운다(디자이너가 이후 인스펙터에서 조정 가능).
    private void Reset() {
        steps = new CombatStats[GrowthRules.MaxStarLevel - 1];
        // 1->2
        steps[0]  = new CombatStats { Attack = 10, Defense = 10, Health = 100 };
        // 2->3
        steps[1]  = new CombatStats { AttackSpeed = 10, MoveSpeed = 10, SkillAccel = 10 };
        // 3->4
        steps[2]  = new CombatStats { ArmorPen = 10 };
        // 4->5 (스킬쿨타임 -0.5초)
        steps[3]  = new CombatStats { SkillCooldownReduction = 0.5f };
        // 5->6
        steps[4]  = new CombatStats { CritRate = 10 };
        // 6->7
        steps[5]  = new CombatStats { CritDamage = 10 };
        // 7->8
        steps[6]  = new CombatStats { MinDamageRatio = 10 };
        // 8->9
        steps[7]  = new CombatStats { Attack = 20, Defense = 20, Health = 200 };
        // 9->10
        steps[8]  = new CombatStats { CritRate = 10, CritDamage = 10 };
        // 10->11 (스킬쿨타임 -0.5초)
        steps[9]  = new CombatStats { SkillCooldownReduction = 0.5f };
        // 11->12
        steps[10] = new CombatStats { MaxDamageRatio = 10 };
        // 12->13
        steps[11] = new CombatStats { ArmorPen = 10 };
        // 13->14
        steps[12] = new CombatStats { AttackSpeed = 10, MoveSpeed = 10, SkillAccel = 10 };
        // 14->15
        steps[13] = new CombatStats { FinalDamage = 10 };
    }
}
