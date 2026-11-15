using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 유닛 고유 스킬의 베이스(ScriptableObject). 스킬 쿨타임이 차면 기본 공격 대신 실행된다(PLANNING 1-18).
/// 캐릭터/몬스터 구분 없이 쓰인다(CharacterData.Skill / MonsterData.Skill): 스킬은 ICombatUnit/IBattleRuntime
/// 계약에만 의존하므로 시전자가 어느 팀인지 알 필요가 없다.
/// 새 스킬은 이 클래스를 상속한 SO 를 만들고 OnExecute 만 구현해 데이터에 할당하면 되며,
/// 전투 코드(BattleDirector/Unit)는 수정하지 않는다(PLANNING 5-19,20 확장성).
///
/// Execute 는 시전자쪽 공통 연출(castVfx/castClip)을 먼저 재생한 뒤 파생의 OnExecute 를 호출하는 템플릿이다.
/// 따라서 모든 스킬이 별도 코드 없이 시전자 VFX/사운드 슬롯을 갖는다(기본공격의 selfVfx 와 대칭).
/// 피격쪽 임팩트 연출은 DamageSkill.impactVfxPrefab 가 대상별로 담당한다.
/// </summary>
public abstract class UnitSkill : ScriptableObject {
    [SerializeField] private string skillName;
    [TextArea] [SerializeField] private string description;
    [Tooltip("스킬 아이콘. 덱/상세 UI 에 표시하고, 툴팁 트리거로 설명을 띄운다.")]
    [SerializeField] private Sprite skillIcon;

    [Header("Cooldown")]
    [Tooltip("스킬 기본 쿨타임(초). 실제 발동 주기는 여기에 별 성장(스킬쿨타임감소)과 스킬가속(점근 감소)이 적용된 값이다(CombatMath.SkillInterval).")]
    [SerializeField] private float baseCooldownSeconds = 8f;

    [Header("Status Effect (선택)")]
    [Tooltip("이 스킬이 적중했을 때 대상에게 거는 상태이상. 비워 두면 걸지 않는다. " +
             "광역/다단 스킬이면 맞은 대상마다 각각 판정한다.")]
    [SerializeField] private StatusEffectSpec[] statusEffects = System.Array.Empty<StatusEffectSpec>();

    [Header("Self Heal (선택)")]
    [Tooltip("스킬을 쓸 때 시전자가 최대 체력의 이 비율(%)만큼 회복한다. 10 = 최대 체력의 10%. 0 이면 회복하지 않는다. " +
             "스킬 종류와 무관하게 시전 순간 한 번 적용된다(예: C4402 미스즈).")]
    [SerializeField] [Min(0f)] private float selfHealMaxHpPercent;

    [Header("Ally Heal (선택)")]
    [Tooltip("스킬을 쓸 때 살아 있는 아군 전원(시전자 포함)이 각자 최대 체력의 이 비율(%)만큼 회복한다. " +
             "10 = 각자 최대 체력의 10%. 0 이면 회복하지 않는다. 스킬 종류와 무관하게 시전 순간 한 번 적용된다(예: C3102 히나). " +
             "Self Heal 과 함께 쓰면 시전자는 둘 다 받는다.")]
    [SerializeField] [Min(0f)] private float allyHealMaxHpPercent;

    [Header("Cast (시전자 연출, 선택)")]
    [Tooltip("스킬 발동 순간 시전자 기준점(HitPoint)에 재생하는 VFX(차지/오라/휘두름 등). 비우면 재생 안 함.")]
    [SerializeField] private GameObject castVfxPrefab;
    [Tooltip("시전 VFX 를 주 타겟(유닛이 조준한 적) 방향으로 회전시켜 재생할지. 방향성 있는 시전 연출이면 켠다(기본공격 self VFX 와 동일). 사방 대칭/오라형이면 끈다.")]
    [SerializeField] private bool orientCastToTarget = true;
    [Tooltip("스킬 발동 사운드. 비우면 재생 안 함(임팩트 연출은 별도).")]
    [SerializeField] private AudioClip castClip;

    public string SkillName => string.IsNullOrEmpty(skillName) ? name : skillName;
    public string Description => description;
    public Sprite SkillIcon => skillIcon;
    /// <summary>스킬 기본 쿨타임(초). 유닛이 별 성장/스킬가속을 반영한 실제 발동 주기 계산의 기준값으로 쓴다.</summary>
    public float BaseCooldown => Mathf.Max(0f, baseCooldownSeconds);

    /// <summary>적중 시 거는 상태이상. 데미지를 넣는 자리가 함께 넘긴다.</summary>
    public IReadOnlyList<StatusEffectSpec> StatusEffects => statusEffects;

    /// <summary>
    /// 이 스킬로 인해 대상이 걸릴 수 있는 상태이상 전부(표시용). 기본은 적중 시 상태이상이며,
    /// 다른 경로로 상태이상을 거는 스킬(변신 중 기본공격 등)은 오버라이드해 함께 알린다.
    /// UI 는 이 목록만 보므로 스킬 종류를 알 필요가 없다. 설정이 비어 있는(지속시간 0) 항목은 빠진다.
    /// </summary>
    public virtual IEnumerable<StatusEffectSpec> InflictedStatusEffects {
        get {
            foreach (var s in statusEffects)
                if (s.IsValid) yield return s;
        }
    }

    /// <summary>
    /// 전투력 산출용 화력 신고(SkillPowerProfile). 자기 직렬화 필드만으로 "1회 발동이 내는 배율/타격/대상 수"를 알린다.
    /// 직접 피해가 없는 버프형 스킬도 "버프가 늘려 주는 피해"를 배율로 환산해 반드시 구현한다.
    /// None 으로 두면 그 캐릭터는 스킬 가속/쿨타임 감소 성장이 전투력에 반영되지 않는다(CombatPowerFormula 의 단조성 규칙).
    ///
    /// 이 프로퍼티 덕분에 전투력 계산기가 스킬 종류를 알 필요가 없다. 새 아키타입을 추가할 때
    /// OnExecute 와 함께 이것만 구현하면 전투력에 자동 반영되며, 전투/전투력 코드는 수정하지 않는다.
    /// </summary>
    public virtual SkillPowerProfile PowerProfile => SkillPowerProfile.None;

    /// <summary>스킬 실행 진입점. 시전자 공통 연출을 먼저 재생하고 파생 고유 로직(OnExecute)을 호출한다.</summary>
    public void Execute(SkillContext ctx) {
        PlayCast(ctx);
        if (selfHealMaxHpPercent > 0f) ctx.Runtime.HealByMaxHp(ctx.Caster, selfHealMaxHpPercent / 100f);
        if (allyHealMaxHpPercent > 0f) HealAllies(ctx, allyHealMaxHpPercent / 100f);
        OnExecute(ctx);
    }

    // 살아 있는 아군 전원을 각자의 최대 체력 비율만큼 회복한다. 죽은 유닛은 HealByMaxHp 가 걸러 준다.
    // 아군 목록은 전장이 공유하는 버퍼라 회복 도중 다른 질의로 바뀌지 않도록 먼저 복사해 둔다.
    private static void HealAllies(SkillContext ctx, float ratio) {
        var allies = new List<ICombatUnit>(ctx.Runtime.Allies(ctx.Caster));
        for (int i = 0; i < allies.Count; i++) {
            var ally = allies[i];
            if (ally.IsTargetable()) ctx.Runtime.HealByMaxHp(ally, ratio);
        }
    }

    /// <summary>파생 스킬의 고유 로직. ctx 를 통해 시전자/적/아군 조회 및 데미지/연출을 수행한다.</summary>
    protected abstract void OnExecute(SkillContext ctx);

    // 시전자쪽 VFX/사운드 재생(할당된 것만). 위치는 시전 기준점(CastOrigin), 방향은 주 타겟(PrimaryTarget) 기준으로
    // 기본공격 self VFX 와 동일하게 낸다. 방향 옵션이 꺼져 있거나 타겟이 없으면 회전 없이 프리팹 기본 회전으로 재생한다.
    private void PlayCast(SkillContext ctx) {
        if (castVfxPrefab != null) {
            var target = ctx.PrimaryTarget;
            if (orientCastToTarget && target.IsTargetable()) {
                // 각도는 기본공격의 방향 연출(AttackContext.PlayVfxToward)과 동일하게 시전자 -> 타겟 기준으로 계산한다.
                Vector2 d = (Vector2)target.Position - (Vector2)ctx.Caster.Position;
                float ang = d.sqrMagnitude > 0.000001f ? Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg : 0f;
                ctx.Runtime.PlayVfx(castVfxPrefab, ctx.CastOrigin, ang);
            } else {
                ctx.Runtime.PlayVfx(castVfxPrefab, ctx.CastOrigin);
            }
        }
        if (castClip != null) ctx.Runtime.PlaySfx(castClip);
    }
}

/// <summary>스킬 실행에 필요한 문맥(시전자 + 주 타겟 + 시전 기준점 + 전장 파사드).</summary>
public sealed class SkillContext {
    public readonly ICombatUnit Caster;
    public readonly IBattleRuntime Runtime;
    /// <summary>시전 시점에 유닛이 조준한 주 타겟(없을 수 있음). 시전 연출 방향 등 "유닛이 향한 곳"이 필요할 때 사용한다.</summary>
    public readonly ICombatUnit PrimaryTarget;
    /// <summary>시전자 연출 기준점(머즐/머리 = HitPoint). 시전 VFX 재생 위치.</summary>
    public readonly Vector2 CastOrigin;

    public SkillContext(ICombatUnit caster, ICombatUnit primaryTarget, Vector2 castOrigin, IBattleRuntime runtime) {
        Caster = caster;
        PrimaryTarget = primaryTarget;
        CastOrigin = castOrigin;
        Runtime = runtime;
    }
}
