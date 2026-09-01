using System;
using UnityEngine;

/// <summary>
/// 발사체 1발의 설정. 발사 주체(RangedAttack SO / 스킬 등)가 채워 AttackContext/IBattleRuntime 를 통해 발사한다.
/// 발사체는 "전달 수단"일 뿐이라 데미지 규칙을 알지 않는다. 도달 시 무슨 일이 일어날지는 OnHit 콜백이 정의한다
/// (기본 데미지, 다단, 연쇄, 도착 지점 광역, 디버프 등 발사 주체가 자유롭게 조립 - 고난이도 스킬 확장 지점).
/// 도달 지점 임팩트 VFX/사운드는 발사체 자체 연출로 여기서 지정한다(대상 생존 여부와 무관하게 재생).
/// </summary>
public struct ProjectileSpec {
    public GameObject Prefab;          // 발사체 프리팹(Projectile 컴포넌트 + TrailRenderer 등 궤적)
    public float Speed;                // 이동 속도(월드 units/초). 0 이하면 즉시 명중(hitscan)
    public bool FaceDirection;         // 진행 방향으로 발사체를 회전시킬지(화살 등)
    public GameObject ImpactVfxPrefab; // 도달 지점 임팩트 VFX(발사체 자체 연출, 선택)
    public AudioClip ImpactClip;       // 도달 시 사운드(선택)

    /// <summary>
    /// 비행 중 대상이 죽어도 도달 지점에서 OnHit 을 실행할지(기본 false = 죽으면 실행하지 않음).
    ///
    /// 대상에게 데미지를 꽂는 발사체는 꺼 둔다(죽은 적을 다시 때릴 이유가 없다). 반대로 "그 자리에 도착하는 것"
    /// 자체가 효과인 발사체 - 도착 지점 광역 폭발, 장판 설치 - 는 켠다. 대상이 먼저 죽었다고 폭발이 통째로
    /// 사라지면 시전자는 스킬을 한 번 허공에 버린 셈이 되고, 주변에 적이 남아 있어도 아무 일도 일어나지 않는다.
    ///
    /// 켠 경우 OnHit 의 ProjectileHit.Target 은 이미 죽은 대상일 수 있다. 도달 지점(Position)만 쓰거나,
    /// 대상을 쓴다면 호출측이 생존을 다시 확인해야 한다.
    /// </summary>
    public bool ArriveEvenIfTargetLost;

    /// <summary>도달 시 적용할 효과. 데미지/연쇄/광역/디버프 등을 발사 주체가 정의한다.
    /// 대상이 비행 중 사망하면 호출되지 않는다(임팩트 VFX/사운드는 그대로 재생).
    /// 단 ArriveEvenIfTargetLost 를 켜면 대상이 죽어도 도달 지점에서 실행된다.</summary>
    public Action<ProjectileHit> OnHit;
}

/// <summary>발사체 도달(명중) 문맥. OnHit 콜백에 전달되어 "누가/누구에게/어디서" 맞았는지 알려준다.</summary>
public readonly struct ProjectileHit {
    public readonly ICombatUnit Source;   // 발사한 개체(공격력/치명타 계산 기준)
    public readonly ICombatUnit Target;   // 명중한 대상(생존 보장. 단 ArriveEvenIfTargetLost 를 켠 발사체는 죽은 대상일 수 있다)
    public readonly Vector2 Position;     // 명중 지점(월드). 도착 지점 광역/임팩트 위치 등에 사용

    public ProjectileHit(ICombatUnit source, ICombatUnit target, Vector2 position) {
        Source = source;
        Target = target;
        Position = position;
    }
}
