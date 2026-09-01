using UnityEngine;

/// <summary>
/// 전투 수식 공용 유틸. 데미지/치명타/공속/스킬 발동주기 계산을 한 곳에 모아 캐릭터/몬스터/스킬이 동일 규칙을 쓴다.
/// 튜닝값(방어 계수/지터/감쇠 등)은 호출측(BattleSettings)에서 주입한다(여기서는 순수 계산만).
/// </summary>
public static class CombatMath {
    /// <summary>
    /// 공격자/방어자 스탯으로 최종 데미지를 한 번에 계산한다(체력 차감/사망 처리는 대상 유닛이 수행).
    /// 파이프라인(기획 확정):
    ///  1) 기본딜   = 공격력 x 배수
    ///  2) 방어감산 = 유효방어(방어자 방어 x (1-관통)) x 방어계수 를 뺀다
    ///  3) 지터     = 최소/최대데미지비율로 재조형한 [하한, 상한]에서 랜덤
    ///  4) 치명타   = 확률 판정 후 치명배수(방어자 치명타저항으로 추가 배율만 감쇄)
    ///  5) 최종딜   = 최종데미지% 곱 x 방어자 피격데미지감소
    /// 하한/상한: 하한 = base x (1 - 지터 x (1 - 최소비율)), 상한 = base x (1 + 지터 x (1 + 최대비율)). 최종 하한 1 보장.
    ///
    /// 방어 스탯 규약(중요): 공격 스탯에서 뺄셈하지 않고 각각 독립 곱연산으로 적용한다.
    /// 뺄셈 방식은 한쪽이 다른 쪽을 완전 무효화(공격자의 최종데미지 옵션이 0이 되는 등)하고 음수가 날 수 있어
    /// 장기 운영에서 상향 경쟁이 데드락된다. 독립 곱연산 + 점근 감쇄(CombatStats.Resist01)로 이를 원천 차단한다.
    /// </summary>
    public static float ResolveDamage(in CombatStats attacker, in CombatStats defender, float multiplier,
                                      float defenseFactor, float jitterPercent, float resistK, out bool isCritical) {
        // 1~2) 기본 공격딜 + 방어 감산
        float mitigated = Mitigated(attacker, defender, multiplier, defenseFactor);

        // 3) 지터 굴림
        JitterBounds(mitigated, attacker, jitterPercent, out float lo, out float hi);
        float rolled = hi > lo ? Random.Range(lo, hi) : lo;

        // 4) 치명타 판정
        isCritical = Random.value < attacker.CritChance01;
        if (isCritical) rolled *= CritMultiplierAgainst(attacker, defender, resistK);

        // 5) 최종 데미지 곱(공격) x 피격 데미지 감소(방어)
        rolled *= FinalMultiplier(attacker, defender, resistK);

        return Mathf.Max(1f, rolled);
    }

    /// <summary>
    /// ResolveDamage 의 기대값(평균) 버전. 난수를 굴리지 않고 같은 파이프라인의 수학적 기댓값을 돌려준다.
    /// 전투력 산출(CombatPowerFormula)처럼 "이 스탯이면 평균 얼마나 때리는가"가 필요한 표시 계산에 쓴다.
    ///
    /// ResolveDamage 와 감산/지터/치명타/최종곱 헬퍼를 그대로 공유하므로 두 식이 갈라질 수 없다.
    /// 전투 공식을 바꾸면 헬퍼 한 곳만 고치면 실제 전투와 전투력 표시가 함께 따라온다.
    ///  - 지터: 균등분포의 평균이라 정확히 (하한+상한)/2 다.
    ///  - 치명타: 확률 가중 기대배율 1 + 확률 x (치명배수 - 1) 로 대체한다.
    ///  - 최소 1 데미지 바닥은 적용하지 않는다. 그것은 전투 규칙이지 화력 측정값이 아니며,
    ///    저스펙 구간에서 스탯 차이를 뭉개 전투력 비교를 왜곡하기 때문이다.
    /// </summary>
    public static float ExpectedDamage(in CombatStats attacker, in CombatStats defender, float multiplier,
                                       float defenseFactor, float jitterPercent, float resistK) {
        float mitigated = Mitigated(attacker, defender, multiplier, defenseFactor);

        JitterBounds(mitigated, attacker, jitterPercent, out float lo, out float hi);
        float expected = (lo + hi) * 0.5f;

        expected *= 1f + attacker.CritChance01 * (CritMultiplierAgainst(attacker, defender, resistK) - 1f);
        expected *= FinalMultiplier(attacker, defender, resistK);

        return Mathf.Max(0f, expected);
    }

    // 기본 공격딜에서 방어 감산까지(음수 차단). 유효방어 = 방어 x (1 - 관통), 감산 = 유효방어 x 방어계수.
    private static float Mitigated(in CombatStats attacker, in CombatStats defender, float multiplier, float defenseFactor) {
        float baseAtk = Mathf.Max(0f, attacker.Attack) * Mathf.Max(0f, multiplier);
        float effDef = Mathf.Max(0f, defender.Defense) * (1f - attacker.ArmorPen01);
        float mitigated = baseAtk - effDef * Mathf.Max(0f, defenseFactor);
        return mitigated < 0f ? 0f : mitigated;
    }

    // 전역 지터 폭을 공격자의 최소/최대데미지비율로 재조형한 [하한, 상한].
    private static void JitterBounds(float mitigated, in CombatStats attacker, float jitterPercent,
                                     out float lo, out float hi) {
        float jf = Mathf.Max(0f, jitterPercent) / 100f;
        float minRatio = Mathf.Max(0f, attacker.MinDamageRatio) / 100f;   // 하한 상승
        float maxRatio = Mathf.Max(0f, attacker.MaxDamageRatio) / 100f;   // 상한 상승
        lo = mitigated * (1f - jf * (1f - minRatio));
        hi = mitigated * (1f + jf * (1f + maxRatio));
        if (lo > hi) lo = hi;   // 최소비율 과다(하한이 상한 추월) 방지
    }

    // 치명타 적중 시 곱해지는 배수. 방어자의 치명타저항은 "추가 배율"만 점근 감쇄하므로
    // 저항을 아무리 쌓아도 치명타가 일반타보다 약해지지 않는다(배수 하한 1배).
    private static float CritMultiplierAgainst(in CombatStats attacker, in CombatStats defender, float resistK) {
        float critExtra = Mathf.Max(0f, attacker.CritMultiplier - 1f);
        return 1f + critExtra * (1f - defender.CritResist01(resistK));
    }

    // 최종 데미지 곱(공격) x 피격 데미지 감소(방어). 서로 독립이라 어느 쪽도 상대를 0으로 만들지 않는다.
    private static float FinalMultiplier(in CombatStats attacker, in CombatStats defender, float resistK) =>
        attacker.FinalDamageMultiplier * (1f - defender.DamageTakenReduction01(resistK));

    /// <summary>
    /// 상태이상 실제 지속시간(초). 방어자의 상태이상저항으로 점근 감쇄한다.
    /// 점근식이라 100% 면역(지속시간 0)이 원천 불가하므로 상태이상 설계가 무의미해지지 않는다.
    /// (상태이상 자체는 미구현이며, 구현 시 이 함수를 통과시킨다.)
    /// </summary>
    public static float StatusDuration(float baseDuration, in CombatStats defender, float resistK) =>
        Mathf.Max(0f, baseDuration) * (1f - defender.StatusResist01(resistK));

    /// <summary>
    /// 기준 대비 공격속도 배수. softCap 이 켜져 있으면 softCapStart(%) 초과분을 점근 감쇠시킨다(개발 중엔 꺼둔다).
    /// 유효초과 = softK x 초과 / (softK + 초과). softK 가 클수록 감쇠가 약하다.
    /// </summary>
    public static float AttackSpeedMultiplier(float rawPercent, bool softCap, float softCapStart, float softK) {
        float p = rawPercent;
        if (softCap && p > softCapStart) {
            float over = p - softCapStart;
            float k = Mathf.Max(0.0001f, softK);
            p = softCapStart + k * over / (k + over);
        }
        return 1f + p / 100f;
    }

    /// <summary>
    /// 스킬 발동 주기(초). 기획 확정 2단 공식:
    ///  ① 조정쿨    = max(floor, 기본쿨 - 별성장 쿨감(초))   ← "스킬쿨타임" 표시값
    ///  ② 발동주기  = floor + (조정쿨 - floor) x K/(K + 스킬가속)  ← 점근 감소(무제한 투자 안전, floor 로 수렴)
    /// </summary>
    public static float SkillInterval(float baseCooldown, float cooldownReductionSeconds,
                                      float floorSeconds, float skillAccelPercent, float softK) {
        float floor = Mathf.Max(0.05f, floorSeconds);
        float adjusted = Mathf.Max(floor, baseCooldown - Mathf.Max(0f, cooldownReductionSeconds));
        float accel = Mathf.Max(0f, skillAccelPercent);
        float k = Mathf.Max(0.0001f, softK);
        return floor + (adjusted - floor) * (k / (k + accel));
    }

    /// <summary>"스킬쿨타임" 표시값(초): max(floor, 기본쿨 - 쿨감). 스킬가속(점근)은 반영 전 값이며 UI 노출용.</summary>
    public static float DisplaySkillCooldown(float baseCooldown, float cooldownReductionSeconds, float floorSeconds) {
        float floor = Mathf.Max(0.05f, floorSeconds);
        return Mathf.Max(floor, baseCooldown - Mathf.Max(0f, cooldownReductionSeconds));
    }
}
