using System;
using System.Collections.Generic;
using UnityEngine;
using static Enums;

// 스킬/전투 로직이 의존하는 최소 계약. 전투 유닛(CharacterUnit/MonsterUnit)이 구현하고,
// 스킬(ScriptableObject)은 이 인터페이스에만 의존해 구체 유닛 타입과 디커플링된다.
// 의존 방향: 데이터/스킬(계약) <- 전투(구현). 새 스킬 추가 시 전투 코드 변경이 필요 없다.

/// <summary>전투에 참여하는 개체(캐릭터/몬스터)의 스킬 관점 계약.</summary>
public interface ICombatUnit {
    Vector2 Position { get; }
    CombatStats Stats { get; }
    bool IsAlive { get; }

    /// <summary>몸통 반경(월드 units). 개체를 점이 아니라 이 반경의 원으로 취급한다. 0 이면 중심점 기준.
    /// 대상 앞에서 멈추는 연출(대시 등)이 덩치를 고려하려면 이 값을 더해 쓴다.</summary>
    float BodyRadius { get; }

    /// <summary>발사/자기 연출 기준점(머즐/머리)의 현재 위치. 시전 시점 스냅샷(SkillContext.CastOrigin)과 달리
    /// 유닛이 이동하면 함께 움직인다(대시 중 타격 이펙트를 캐릭터의 현재 위치에서 내는 등).</summary>
    Vector2 MuzzlePoint { get; }

    /// <summary>현재 체력(절대값). 최대 체력은 Stats.Health. "체력이 높은/낮은 적" 타게팅 스킬 등에서 사용.</summary>
    float CurrentHp { get; }

    /// <summary>이 개체가 대상과 적대 관계인지(팀이 다른지).</summary>
    bool IsEnemyOf(ICombatUnit other);

    /// <summary>
    /// 공격자 스탯 + 배수를 받아 데미지를 입힌다. 방어/관통/지터/치명타/최종데미지 계산과 사망 처리는 구현체가 수행한다.
    /// </summary>
    /// <param name="statuses">이번 적중으로 걸 상태이상(없으면 null). 실제로 맞았을 때만 걸리며,
    /// 지속시간은 대상의 상태이상저항으로 줄어든다. 공격/스킬 SO 가 자기 설정을 그대로 넘긴다.</param>
    void TakeDamage(CombatStats attacker, float multiplier, ICombatUnit source,
                    IReadOnlyList<StatusEffectSpec> statuses = null);
}

/// <summary>
/// ICombatUnit 참조의 유효성 판정.
///
/// 구현체는 MonoBehaviour 지만 이 계약은 인터페이스라, 인터페이스 참조로 하는 == null 비교는
/// UnityEngine.Object 의 == 오버로드를 타지 않는다. 즉 "이미 파괴된 유닛"이 null 로 보이지 않아
/// 통과해 버리고, 그 뒤 transform 을 건드리는 순간 MissingReferenceException 이 난다.
///
/// 스킬은 예약 실행(Schedule)/발사체 도달처럼 시간차를 두고 대상을 다시 만지므로, 그 사이에
/// 전장이 정리되면(의뢰 종료/다음 단계/씬 이동) 대상이 파괴돼 있을 수 있다.
/// 따라서 ICombatUnit 의 유효성은 항상 이 확장으로 판단한다.
/// </summary>
public static class CombatUnitRefExtensions {
    /// <summary>참조가 아직 살아 있는지(파괴되지 않았는지). 전투상의 생존(IsAlive)과는 별개다.</summary>
    public static bool Exists(this ICombatUnit unit) =>
        unit != null && !(unit is UnityEngine.Object o && o == null);

    /// <summary>지금 대상으로 삼아도 되는지(참조가 유효하고 전투에서도 살아 있는지).</summary>
    public static bool IsTargetable(this ICombatUnit unit) => unit.Exists() && unit.IsAlive;
}

/// <summary>스킬이 전장을 질의하고 연출을 요청하기 위한 런타임 파사드(BattleDirector 가 구현).</summary>
public interface IBattleRuntime {
    IReadOnlyList<ICombatUnit> Enemies(ICombatUnit of);
    IReadOnlyList<ICombatUnit> Allies(ICombatUnit of);

    /// <summary>지정 위치에 VFX 프리팹을 재생(풀링 관리).</summary>
    void PlayVfx(GameObject prefab, Vector2 at);

    /// <summary>지정 위치에 angleDeg(월드 Z) 방향을 바라보도록 회전시켜 VFX 를 재생(스킬 임팩트 방향 연출).</summary>
    void PlayVfx(GameObject prefab, Vector2 at, float angleDeg);

    /// <summary>공통 사운드 키로 SFX 재생.</summary>
    void PlaySfx(SfxKey key);

    /// <summary>임의 오디오클립으로 SFX 재생(공격 SO 가 캐릭터별 고유 사운드를 낼 때).</summary>
    void PlaySfx(AudioClip clip);

    /// <summary>발사체를 발사한다(풀 관리). 도달 시 대상에게 데미지 + 임팩트 연출을 낸다.</summary>
    void LaunchProjectile(ProjectileSpec spec, Vector2 from, ICombatUnit source, ICombatUnit target);

    /// <summary>delaySeconds 후 action 을 실행한다(delay<=0 이면 즉시). 전투 재시작/씬 종료 시 자동 취소된다.
    /// 다단 스태거(연사 간격), "잠시 후 폭발", 채널링 등 시간차 스킬 연출/로직에 사용한다.</summary>
    void Schedule(float delaySeconds, Action action);

    /// <summary>시전자(유닛 전체)를 worldTarget 근처로 arriveSeconds 동안 빠르게 이동(대시)시킨다. 마스크 일러스트/체력바가
    /// 함께 움직여 초상이 잘리지 않는다. stopShort 만큼 대상 앞에서 멈춘다(겹침 방지). 대시 중에는 실제 위치가 이동하며 종료 시 원위치로 복귀한다.
    /// 대시를 지원하지 않는 시전자면 아무 것도 하지 않는다. 도착 타이밍의 데미지 처리는 호출부가 Schedule 로 별도 관리한다(안전한 취소).
    /// "적 사이를 이동하며 연속 타격" 같은 근접 돌진 스킬의 기반.</summary>
    void DashCaster(ICombatUnit caster, Vector2 worldTarget, float arriveSeconds, float stopShort);

    /// <summary>대시로 이동한 시전자의 시각 표현을 원위치로 seconds 동안 복귀시킨다(대시 스킬 종료 연출). 미지원 시전자는 무시.</summary>
    void ReturnCaster(ICombatUnit caster, float seconds);

    /// <summary>
    /// unit 의 체력을 최대 체력의 ratio(0.1 = 10%) 만큼 회복하고 회복 숫자를 띄운다(최대치를 넘지 않는다).
    /// 실제로 회복된 양을 돌려준다. 죽었거나 회복할 수 없는 유닛이면 0.
    /// </summary>
    float HealByMaxHp(ICombatUnit unit, float ratio);
}

/// <summary>유닛 변신(일러스트 교체 + 지속시간 스탯 강화)에 필요한 데이터 묶음. 변신 스킬 SO 가 조립해 유닛에 전달한다.</summary>
public struct UnitTransformSpec {
    /// <summary>변신 중 표시할 일러스트. null 이면 일러스트는 바꾸지 않는다(스탯 강화만).</summary>
    public Sprite Sprite;
    /// <summary>변신 일러스트의 프레이밍 focus(정규화 0~1, CharacterData.UiFocus 와 동일 규약).</summary>
    public Vector2 SpriteFocus;
    /// <summary>변신 지속시간(초). 변신 중 재시전 시 이 값으로 다시 채워진다.</summary>
    public float Duration;
    /// <summary>강화할 스탯 목록. bonusMultiplier 는 변신 중 그 스탯이 될 최종 배수(3 = 3배). 공속/이속/스킬가속은 실효 속도가 N배가 되도록 자동 변환된다.</summary>
    public IReadOnlyList<TransformStatBuff> Buffs;
    /// <summary>변신 중 기본공격의 타격(임팩트) VFX 를 이걸로 교체한다. null 이면 기존 VFX 유지.</summary>
    public GameObject AttackImpactVfxOverride;
    /// <summary>
    /// 변신 중 기본공격이 추가로 거는 상태이상. 변신 스킬 자체는 피해를 주지 않아 상태이상을 걸 자리가 없으므로,
    /// "변신해 있는 동안 때리면 걸린다" 를 이 경로로 표현한다. 비우면 기본공격 설정만 그대로 쓴다.
    /// </summary>
    public IReadOnlyList<StatusEffectSpec> AttackStatusEffects;
}

/// <summary>변신 스탯 강화 항목(스탯 종류 + 베이스 대비 상승 배수). 인스펙터에서 편집한다.</summary>
[Serializable]
public struct TransformStatBuff {
    [Tooltip("강화할 스탯.")]
    public StatType stat;
    [Tooltip("변신 중 이 스탯이 될 최종 배수. 3 = 3배. 공속/이속/스킬가속은 실효 속도가 정확히 N배가 되도록 자동 변환된다(미성장 캐릭터도 N배).")]
    public float bonusMultiplier;
}

/// <summary>변신(일러스트 교체 + 지속시간 스탯 강화)을 지원하는 유닛의 계약. CharacterUnit 이 구현하고,
/// 변신 스킬은 시전자가 이 계약을 구현하는지 확인 후 호출한다(미구현 유닛이면 무시). 전투 코드 수정 없이 스킬을 추가하기 위한 확장점.</summary>
public interface ITransformableUnit {
    /// <summary>변신을 적용한다. 이미 변신 중이면 지속시간만 spec.Duration 으로 원복하고 중첩하지 않는다.
    /// 지속시간이 끝나거나 사망하면 원래 모습/스탯으로 되돌아간다.</summary>
    void ApplyTransform(in UnitTransformSpec spec);
}

/// <summary>
/// 무적(모든 피해를 회피하는 상태)을 지원하는 유닛의 계약. BattleUnit 이 구현하고, 무적을 거는 스킬은
/// 시전자가 이 계약을 구현하는지 확인 후 호출한다(미구현 유닛이면 무시). 변신(ITransformableUnit)과 같은 확장점이다.
///
/// 무적 중에는 모든 피격이 MISS 로 처리되고(데미지/넉백/상태이상 없음) 지속 피해도 들어가지 않는다.
/// 생존 판정(IsAlive)은 그대로라 적의 타게팅은 바뀌지 않는다.
/// </summary>
public interface IInvulnerableUnit {
    /// <summary>지금 무적인지.</summary>
    bool IsInvulnerable { get; }

    /// <summary>
    /// 무적을 건다. 이미 무적이면 남은 시간을 더 긴 쪽으로 늘린다(중첩하지 않는다).
    /// maxSeconds 는 EndInvulnerability 가 오지 않아도 풀리는 안전 상한이다(예약 실행이 취소되는 경우 대비).
    /// </summary>
    /// <param name="vfxOverride">무적 동안 유닛을 따라다닐 VFX. null 이면 전투 설정의 공용 무적 VFX 를 쓴다.</param>
    void BeginInvulnerability(float maxSeconds, GameObject vfxOverride = null);

    /// <summary>무적을 즉시 해제한다(VFX 도 함께 사라진다). 무적이 아니면 아무 일도 하지 않는다.</summary>
    void EndInvulnerability();
}
