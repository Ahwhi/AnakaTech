using System;
using UnityEngine;

/// <summary>전용무기 보너스 규칙의 적용 범위. 좁은 범위가 우선한다(Equip &gt; WeaponType &gt; Grade &gt; Global).</summary>
public enum SignatureBonusScope {
    Global,      // 전체 전용무기 공통
    Grade,       // 특정 등급(key = Grade 정수값 0=N..4=XR)
    WeaponType,  // 특정 무기 종류(key = WeaponType 정수값)
    Equip        // 특정 장비 1개(key = EquipmentData.id)
}

/// <summary>전용무기 보너스 규칙 1개.</summary>
[Serializable]
public struct SignatureBonusRule {
    [Tooltip("적용 범위. 좁은 범위가 우선한다(Equip > WeaponType > Grade > Global).")]
    public SignatureBonusScope scope;
    [Tooltip("범위 대상 값. Grade=0~4(N~XR), WeaponType=열거형 정수값, Equip=장비 id. Global 이면 무시된다.")]
    public int key;
    [Tooltip("본인 전용무기 장착 시 더해지는 스탯.")]
    public CombatStats bonus;
    [Tooltip("UI 에 출력할 효과 문구. 비우면 bonus 로부터 자동 생성한다(예: '최종데미지 +10%').")]
    public string description;
}

/// <summary>
/// 전용무기 장착 보너스 카탈로그. 캐릭터가 본인 전용무기를 장착했을 때 받는 보너스를 데이터로 관리한다.
/// GradeColorCatalog 와 동일 패턴이며, 앱 시작 시 EquipmentRules 의 기본값으로 1회 등록되므로
/// 개별 프리팹마다 지정할 필요가 없다.
///
/// 확장 방법(코드 수정 없이 규칙만 추가):
///   - 전체 공통 조정        -> scope=Global 규칙 1줄
///   - "XR 전용무기만 강화"  -> scope=Grade,      key=4
///   - "활 전용무기만 강화"  -> scope=WeaponType, key=(int)WeaponType.Bow
///   - "특정 캐릭 무기 특별" -> scope=Equip,      key=전용무기 id
/// 여러 규칙이 걸리면 좁은 범위 하나만 적용한다(합산하지 않음 - 어떤 규칙이 먹었는지 예측 가능하게).
///
/// 이 에셋을 비워두거나 할당하지 않으면 EquipmentRules 의 코드 폴백
/// (등급별 최종데미지 N 10 / R 15 / SR 20 / SSR 25 / XR 30)이 적용된다.
/// </summary>
[CreateAssetMenu(menuName = "Anaka/Signature Bonus Catalog", fileName = "SignatureBonusCatalog")]
public sealed class SignatureBonusCatalog : ScriptableObject {
    [Tooltip("전용무기 보너스 규칙 목록. 순서는 무관하며 범위 우선순위로 선택된다.")]
    [SerializeField] private SignatureBonusRule[] rules;

    /// <summary>
    /// 해당 전용무기에 적용할 규칙을 찾는다. 좁은 범위(Equip)부터 넓은 범위(Global) 순으로 첫 매치를 반환한다.
    /// 매치가 없으면 false(호출측이 폴백을 쓴다).
    /// </summary>
    public bool TryResolve(int equipDataId, Grade grade, WeaponType weaponType, out SignatureBonusRule rule) {
        rule = default;
        if (rules == null || rules.Length == 0) return false;

        return TryFind(SignatureBonusScope.Equip, equipDataId, out rule)
            || TryFind(SignatureBonusScope.WeaponType, (int)weaponType, out rule)
            || TryFind(SignatureBonusScope.Grade, (int)grade, out rule)
            || TryFind(SignatureBonusScope.Global, 0, out rule);
    }

    private bool TryFind(SignatureBonusScope scope, int key, out SignatureBonusRule found) {
        for (int i = 0; i < rules.Length; i++) {
            if (rules[i].scope != scope) continue;
            // Global 은 대상 값이 없으므로 key 를 비교하지 않는다.
            if (scope != SignatureBonusScope.Global && rules[i].key != key) continue;
            found = rules[i];
            return true;
        }
        found = default;
        return false;
    }
}
