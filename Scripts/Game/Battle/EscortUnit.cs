using UnityEngine;

/// <summary>
/// 호위 의뢰의 의뢰인(호송 대상) 유닛. 아군 팀이지만 움직이지도 공격하지도 않고 오직 "맞고 버티는" 역할만 한다.
///
/// BattleUnit 파생이라 체력/피격/데미지 파이프라인/체력바/사망 통지가 전부 공통 경로를 그대로 탄다.
/// 몬스터 입장에서도 그냥 아군 팀 유닛이라, 별도 AI 없이 자동으로 공격 대상이 된다(전용 코드가 필요 없는 이유).
/// 체력 회복 수단은 두지 않는다. 다만 아군 목록에 들어가므로 캐릭터의 아군 대상 스킬(치유/버프)은 닿는다
/// - 이는 의도된 설계로, 호위 의뢰에서 지원형 캐릭터의 가치를 만들어 준다.
/// </summary>
public sealed class EscortUnit : BattleUnit {
    [Header("Escort Refs")]
    [Tooltip("마스크 일러스트 표현부(선택). 할당하면 캐릭터/몬스터와 같은 방식으로 초상을 보여준다.")]
    [SerializeField] private BattleUnitIllustration illust;
    [Tooltip("표시할 의뢰인 일러스트(선택). 비우면 프리팹 자체 스프라이트를 그대로 쓴다.")]
    [SerializeField] private Sprite portrait;
    [Tooltip("일러스트 프레이밍(관심점 + 크기 배율). 몬스터의 BattleFocus 와 같은 역할.")]
    [SerializeField] private IllustFocus portraitFocus = IllustFocus.Default;

    public override Team Team => Team.Player;

    /// <summary>의뢰인은 전장 중앙에 고정되어야 하므로 겹침 해소 패스가 밀어내지 않는다.</summary>
    public override bool PushableByOverlap => false;

    /// <summary>
    /// 체력/방어력만 받아 세운다. 의뢰인은 성장/장비/각인과 무관한 컨텐츠 고정 스펙이라 CommissionData 값을 그대로 쓴다.
    /// slot 은 마스크 정렬 격리용(캐릭터 슬롯과 겹치지 않는 값을 넘길 것).
    /// </summary>
    public void Init(float health, float defense, IBattleContext ctx, BattleSettings settings, int slot) {
        var stats = new CombatStats { Health = Mathf.Max(1f, health), Defense = Mathf.Max(0f, defense) };

        if (illust != null) {
            if (portrait != null) illust.Apply(portrait, portraitFocus);
            illust.IsolateMask(slot);
        }

        BaseInit(stats, ctx, settings);
        if (body != null) body.localPosition = Vector3.zero;
    }

    // 의뢰인은 아무 행동도 하지 않는다(이동/공격 없음). 살아 있는 동안 제자리를 지키는 것이 전부다.
    protected override void Tick(float dt) { }

    // 기본공격/스킬 SO 가 없으므로 BattleUnit 의 공격 경로는 애초에 호출되지 않는다(CanAttack 판단 자체를 하지 않는다).

    protected override void OnDamaged(float amount, ICombatUnit source) {
        if (illust != null)
            illust.PlayHitBlink(Settings.HitBlinkAlphaFactor, Settings.HitBlinkDuration, Settings.HitBlinkLoops);
    }

    protected override void Die() {
        base.Die();   // 사망 통지(IBattleContext.OnUnitDied)로 진행 규칙이 호위 실패를 판정한다.
        if (illust != null) illust.StopBlink();
        if (body != null) body.gameObject.SetActive(false);
    }
}
