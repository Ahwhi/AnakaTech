using System;
using UnityEngine;

/// <summary>
/// 이동하는 발사체(화살/총알/오브). 발사 지점에서 대상까지 이동하고, 도달하면 도달 효과(OnHit) + 임팩트 VFX/사운드를
/// 낸 뒤 풀로 반환된다. 발사체는 "전달 수단"이라 데미지 규칙을 알지 않고, 도달 시 무슨 일이 일어날지는 발사 주체가
/// ProjectileSpec.OnHit 로 주입한다(원거리 기본 데미지/스킬 다단/연쇄 등). 궤적은 프리팹의 TrailRenderer 로 표현한다.
/// 효과는 발사가 아니라 도달 시 적용되어 원거리 타격이 자연스럽다. 비행 중 대상이 죽으면 OnHit 없이 임팩트만 내는 것이 기본이며,
/// 도착 자체가 효과인 발사체(도착 지점 폭발 등)는 ProjectileSpec.ArriveEvenIfTargetLost 로 그때도 OnHit 을 받는다.
/// </summary>
public sealed class Projectile : MonoBehaviour {
    [Tooltip("도달 판정 거리(월드). 이 안에 들어오면 명중 처리.")]
    [SerializeField] private float arriveThreshold = 0.15f;
    [Tooltip("대상 소멸 등 안전을 위한 최대 비행 시간(초).")]
    [SerializeField] private float maxLifetime = 3f;

    private ICombatUnit _source;
    private ICombatUnit _target;
    private Vector2 _fallbackPoint;   // 대상이 죽으면 향할 마지막 좌표(비행 중 최신 위치로 갱신).
    private float _speed;
    private Action<ProjectileHit> _onHit;   // 도달 시 실행할 효과(발사 주체가 주입). null 이면 임팩트 연출만.
    private GameObject _impactVfx;
    private AudioClip _impactClip;
    private bool _faceDir;
    private bool _arriveEvenIfTargetLost;   // 대상이 죽어도 도달 지점에서 OnHit 을 실행할지(도착 지점 폭발 등).
    private IBattleRuntime _runtime;
    private Action<Projectile> _onDone;
    private float _age;
    private bool _active;

    private TrailRenderer _trail;
    private bool _trailResolved;

    /// <summary>발사체를 초기화하고 비행을 시작한다. 도달 시 onDone 으로 풀에 반환된다.</summary>
    public void Launch(ProjectileSpec spec, Vector2 from, ICombatUnit source, ICombatUnit target,
                       IBattleRuntime runtime, Action<Projectile> onDone) {
        _source = source;
        _target = target;
        _fallbackPoint = target.Exists() ? target.Position : from;
        _speed = spec.Speed;
        _onHit = spec.OnHit;
        _impactVfx = spec.ImpactVfxPrefab;
        _impactClip = spec.ImpactClip;
        _faceDir = spec.FaceDirection;
        _arriveEvenIfTargetLost = spec.ArriveEvenIfTargetLost;
        _runtime = runtime;
        _onDone = onDone;
        _age = 0f;
        _active = true;

        transform.position = new Vector3(from.x, from.y, transform.position.z);

        // 풀 재사용 시 이전 위치에서 이어지는 궤적 잔상을 제거한다.
        if (!_trailResolved) { _trail = GetComponentInChildren<TrailRenderer>(); _trailResolved = true; }
        if (_trail != null) _trail.Clear();

        // hitscan(속도 0 이하): 즉시 도달 처리.
        if (_speed <= 0f) Impact(TargetPoint());
    }

    // 대상이 파괴됐는지(파티 재구성/전장 정리 등)까지 확인한다. 판정은 계약 쪽 공용 확장(IsTargetable/Exists)에 맡긴다.
    private Vector2 TargetPoint() => _target.IsTargetable() ? _target.Position : _fallbackPoint;

    private void Update() {
        if (!_active) return;

        _age += Time.deltaTime;
        if (_age >= maxLifetime) { Impact(TargetPoint()); return; }

        Vector2 cur = transform.position;
        Vector2 dest = TargetPoint();
        _fallbackPoint = dest;   // 살아있는 동안 최신 위치 추적(죽으면 마지막 위치 유지).

        Vector2 delta = dest - cur;
        float dist = delta.magnitude;
        if (dist <= arriveThreshold) { Impact(dest); return; }

        Vector2 dir = delta / dist;
        float step = _speed * Time.deltaTime;
        Vector2 next = step >= dist ? dest : cur + dir * step;
        transform.position = new Vector3(next.x, next.y, transform.position.z);

        if (_faceDir) {
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
    }

    private void Impact(Vector2 at) {
        if (!_active) return;
        _active = false;

        // 도달 효과: 발사 주체가 주입한 효과를 실행한다(데미지/연쇄/광역 등).
        // 기본은 대상이 살아있을 때만이다. 죽은 적을 다시 때리지 않기 위해서다.
        // 도착 자체가 효과인 발사체(ArriveEvenIfTargetLost)는 대상이 죽어도 도달 지점에서 실행한다.
        // 그 지점 주변을 다시 훑는 것은 효과 쪽 몫이라, 여기서는 "도착했다"는 사실만 전달한다.
        if (_onHit != null && _source.Exists() && (_target.IsTargetable() || _arriveEvenIfTargetLost))
            _onHit.Invoke(new ProjectileHit(_source, _target, at));
        _onHit = null;   // 재사용(풀) 전에 캡처된 참조를 즉시 해제.

        // 임팩트 연출은 대상 생존 여부와 무관하게 도달 지점에 낸다(발사체 자체 연출).
        if (_runtime != null) {
            if (_impactVfx != null) _runtime.PlayVfx(_impactVfx, at);
            if (_impactClip != null) _runtime.PlaySfx(_impactClip);
        }

        _onDone?.Invoke(this);
    }
}
