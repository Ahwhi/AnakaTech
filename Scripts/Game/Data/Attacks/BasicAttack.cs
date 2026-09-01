using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 유닛 기본공격의 베이스(ScriptableObject). 무기/연출별로 상속 SO 를 만들어 데이터에 할당한다
/// (CharacterData.BasicAttack / MonsterData.BasicAttack: 캐릭터와 몬스터가 같은 SO 를 공유할 수 있다).
/// 스킬(UnitSkill)과 형제 개념이다(공속 쿨타임마다 발동 vs 스킬 주기 발동). 전투 코드는 Execute 만 호출하므로
/// 새 공격을 추가해도 BattleDirector/Unit 수정이 필요 없다.
///
/// 대부분의 유닛은 재사용 SO(MeleeAttack 등)에 VFX/사운드/발사체를 데이터로 지정해 고유성을 낸다.
/// 진짜 특이한 공격(다단히트/광역 등)만 이 클래스를 상속한 새 SO 를 만든다.
/// </summary>
public abstract class BasicAttack : ScriptableObject {
    [Header("Status Effect (선택)")]
    [Tooltip("이 공격이 적중했을 때 대상에게 거는 상태이상. 비워 두면 걸지 않는다. " +
             "여러 개를 달면 전부 판정한다(각자 확률/지속시간을 갖는다).")]
    [SerializeField] private StatusEffectSpec[] statusEffects = System.Array.Empty<StatusEffectSpec>();

    /// <summary>적중 시 거는 상태이상. 데미지를 넣는 자리(AttackContext.DealDamage)가 함께 넘긴다.</summary>
    public IReadOnlyList<StatusEffectSpec> StatusEffects => statusEffects;

    /// <summary>기본공격 1회 실행(데미지 적용 + 연출). ctx 로 시전자/대상/기준점/연출에 접근한다.</summary>
    public abstract void Execute(AttackContext ctx);
}
