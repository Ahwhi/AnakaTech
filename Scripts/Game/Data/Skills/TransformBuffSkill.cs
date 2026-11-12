using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 변신 + 스탯 강화. 시전자가 지정 일러스트로 변신하고 duration 동안 지정 스탯이 "실효 N배"로 강화된다.
/// 변신 중 재시전하면 지속시간이 duration 으로 원복된다(중첩 없음). 지속시간 종료/사망 시 원래 모습/스탯으로 복귀.
/// 변신 순간 연출은 베이스의 castVfxPrefab/castClip 을 사용한다. 데미지가 없는 버프형이라 DamageSkill 이 아닌 UnitSkill 상속.
/// 변신은 ITransformableUnit 계약으로 전달되므로 전투 코드는 수정하지 않는다.
/// 예) C3402 앨리시아: 5초간 늑대 변신, 공격력/공속/이속 3배.
/// </summary>
[CreateAssetMenu(fileName = "TransformBuffSkill", menuName = "Anaka/Skills/Transform Buff")]
public sealed class TransformBuffSkill : UnitSkill {
    [Header("Transform")]
    [Tooltip("변신 중 표시할 일러스트(예: 늑대). 비우면 일러스트는 안 바꾸고 스탯 강화만 한다.")]
    [SerializeField] private Sprite transformSprite;
    [Tooltip("변신 일러스트의 프레이밍 focus(정규화 0~1). CharacterData.UiFocus 와 같은 규약으로, 이 점이 마스크 창의 기준 위치에 온다.")]
    [SerializeField] private Vector2 transformFocus = new Vector2(0.5f, 0.5f);
    [Tooltip("변신 지속시간(초). 변신 중 재시전하면 이 값으로 다시 채워진다.")]
    [SerializeField] private float duration = 5f;

    [Header("Stat Buffs")]
    [Tooltip("변신 중 강화할 스탯 목록. bonusMultiplier 는 그 스탯이 될 최종 배수(3 = 3배). 공속/이속/스킬가속은 실효 속도가 정확히 N배가 되도록 자동 변환된다(미성장 캐릭터도 N배). " +
             "주의: Health 는 스탯값만 올라가고 현재/최대 HP 는 변하지 않는다.")]
    [SerializeField] private List<TransformStatBuff> buffs = new List<TransformStatBuff> {
        new TransformStatBuff { stat = StatType.Attack, bonusMultiplier = 3f },
        new TransformStatBuff { stat = StatType.MoveSpeed, bonusMultiplier = 3f }
    };

    [Header("Attack Override (선택)")]
    [Tooltip("변신 중 기본공격의 타격(임팩트) VFX 를 이걸로 교체한다(늑대 발톱 등). 비우면 기존 임팩트 그대로.")]
    [SerializeField] private GameObject attackImpactVfxOverridePrefab;

    [Header("Status Effect (변신 중 기본공격)")]
    [Tooltip("변신해 있는 동안 기본공격이 대상에게 추가로 거는 상태이상. " +
             "변신 스킬은 그 자체로 피해를 주지 않아 위의 공용 상태이상 칸(적중 시 발동)이 걸릴 자리가 없으므로, " +
             "변신 중 공격에 실어 보내려면 여기에 넣는다.")]
    [SerializeField] private StatusEffectSpec[] attackStatusEffects = System.Array.Empty<StatusEffectSpec>();

    [Header("Combat Power")]
    [Tooltip("전투력 환산 배율. 변신의 전투력 기여(아래 PowerProfile)에 곱한다. 1 = 그대로, 0.5 = 절반. " +
             "전투에는 영향이 없고 표시 전투력만 바뀐다. 성장에 따른 증가 방향은 그대로 유지된다.")]
    [Min(0f)]
    [SerializeField] private float powerValueScale = 1f;

    /// <summary>적중 시 상태이상에 더해, 변신 중 기본공격이 거는 상태이상도 함께 알린다.</summary>
    public override IEnumerable<StatusEffectSpec> InflictedStatusEffects {
        get {
            foreach (var s in base.InflictedStatusEffects) yield return s;
            foreach (var s in attackStatusEffects)
                if (s.IsValid) yield return s;
        }
    }

    // 전투력 환산 기준 타수(초당). 실제 공속은 캐릭터마다 다르지만 스킬은 시전자 스탯을 모르므로 1타/초로 잡는다.
    private const float PowerReferenceAttacksPerSecond = 1f;

    /// <summary>
    /// 전투력 평가용 화력. 변신은 직접 피해가 없지만 지속시간 동안 기본공격 피해가 (공격력 배수 x 공속 배수) 배가 된다.
    /// 1회 발동의 가치를 "지속시간 동안 늘어나는 기본공격 피해"로 환산해 신고한다(1타당 늘어난 배율 x 타수).
    /// </summary>
    public override SkillPowerProfile PowerProfile {
        get {
            float attackMul = 1f, speedMul = 1f;
            foreach (var b in buffs) {
                if (b.stat == StatType.Attack) attackMul *= Mathf.Max(1f, b.bonusMultiplier);
                else if (b.stat == StatType.AttackSpeed) speedMul *= Mathf.Max(1f, b.bonusMultiplier);
            }
            float extraPerHit = (attackMul * speedMul - 1f) * Mathf.Max(0f, powerValueScale);
            int hits = Mathf.Max(1, Mathf.RoundToInt(duration * PowerReferenceAttacksPerSecond));
            return extraPerHit > 0f ? new SkillPowerProfile(extraPerHit * hits, hits, 1) : SkillPowerProfile.None;
        }
    }

    protected override void OnExecute(SkillContext ctx) {
        if (!(ctx.Caster is ITransformableUnit unit)) return;
        unit.ApplyTransform(new UnitTransformSpec {
            Sprite = transformSprite,
            SpriteFocus = transformFocus,
            Duration = duration,
            Buffs = buffs,
            AttackImpactVfxOverride = attackImpactVfxOverridePrefab,
            AttackStatusEffects = attackStatusEffects
        });
    }
}
