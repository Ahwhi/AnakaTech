using System.Collections.Generic;
using UnityEngine;
using static Enums;

/// <summary>
/// 전투 유닛이 전장/연출/사운드에 접근하기 위한 내부 계약(BattleDirector 가 구현).
/// 스킬용 최소 계약(IBattleRuntime)과 별개로, 유닛은 더 풍부한 이 계약에 의존한다.
/// 유닛끼리 직접 참조하지 않고 이 파사드를 통해 상호작용해 디커플링을 유지한다.
/// </summary>
public interface IBattleContext {
    IReadOnlyList<BattleUnit> EnemiesOf(BattleUnit unit);
    IReadOnlyList<BattleUnit> AlliesOf(BattleUnit unit);

    /// <summary>피격 대상 위에 데미지 팝업을 띄운다(스타일별 프리팹: 일반/치명타/피격, PLANNING 1-14). 설정(ShowDamageNumber) OFF 면 무시된다.</summary>
    void ShowDamage(Vector2 worldPos, float amount, DamagePopupStyle style);

    /// <summary>
    /// 피격 대상 위에 숫자가 아닌 결과 문구를 띄운다(회피 MISS 등). 데미지 팝업과 같은 풀/연출을 쓰므로
    /// 표시 위치와 겹침 처리가 숫자와 일관된다. 설정(ShowDamageNumber) OFF 면 무시된다.
    /// </summary>
    void ShowDamageText(Vector2 worldPos, string text, DamagePopupStyle style);

    /// <summary>스킬 사용 시 시전자 머리 위에 말풍선(스킬명+!)을 띄운다(color = 텍스트 색, 보통 캐릭터 퍼스널 컬러). 설정(ShowSkillBubble) OFF 면 무시된다.</summary>
    void ShowSkillBubble(Vector2 worldPos, string text, Color color);

    /// <summary>VFX 프리팹을 지정 위치에 재생(풀링).</summary>
    void PlayVfx(GameObject prefab, Vector2 at);

    /// <summary>VFX 프리팹을 지정 위치에, angleDeg(월드 Z) 방향을 바라보도록 회전시켜 재생(공격 방향 연출).</summary>
    void PlayVfx(GameObject prefab, Vector2 at, float angleDeg);

    /// <summary>가해자 기준 딜량 집계 통지(딜량 그래프). 실제 적용된 최종 데미지를 넘긴다.
    /// 캐릭터가 낸 피해만 집계 대상이며, 그 판단은 디렉터가 한다(유닛은 누가 집계하는지 모른다).</summary>
    void ReportDamage(ICombatUnit source, float amount);

    /// <summary>유닛 사망 통지(디렉터가 목록 갱신/부활 스케줄/클리어 판정).</summary>
    void OnUnitDied(BattleUnit unit);

    /// <summary>
    /// 쓰러진 캐릭터가 몇 초 뒤에 부활할지. 음수면 부활하지 않는다(의뢰처럼 부활이 없는 컨텐츠).
    /// 부활 정책을 유닛이 스스로 갖지 않고 전장에 물어보게 해, 같은 캐릭터 프리팹이 컨텐츠마다 다르게 동작한다.
    /// </summary>
    float ReviveDelayFor(BattleUnit unit);

    /// <summary>
    /// 이 유닛이 "가장 가까운 적" 대신 노려야 할 우선 대상(호위 의뢰의 돌격병 등). null 이면 기본 타게팅을 쓴다.
    /// 대상 선택 규칙이 전장(컨텐츠)에 있으므로 몬스터 AI 를 컨텐츠마다 새로 만들 필요가 없다.
    /// </summary>
    BattleUnit PriorityTargetFor(BattleUnit unit);

    /// <summary>대표 캐릭터가 적을 타격했을 때(카메라 흔들림, PLANNING 1-10). 설정(UseCameraShake) OFF 면 무시된다.</summary>
    void OnRepresentativeHitEnemy();

    void PlaySfx(SfxKey key);
}
