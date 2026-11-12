using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 논타겟 직선 발사체. 대상을 쫓지 않고 정해진 방향으로 정해진 거리만큼 곧게 날아가며,
/// 지나간 길에 몸이 걸친 적에게 맞는다(피하면 맞지 않는다). 대상 추적형 Projectile 과 형제다.
///
/// 맞았을 때 무슨 일이 일어날지는 발사 주체가 onHit 으로 주입한다(데미지 규칙을 모른다).
/// 한 프레임에 지나간 선분 전체로 판정하므로 빠르게 날아도 사이에 낀 유닛을 건너뛰지 않는다.
/// 시전자가 사라지면(사망/전장 정리) 함께 사라진다. 판이 끝나 전장이 정리될 때 남은 발사체를 따로 치울 필요가 없다.
/// </summary>
public sealed class LineProjectile : MonoBehaviour {
    private ICombatUnit _caster;
    private IBattleRuntime _runtime;
    private Action<ICombatUnit> _onHit;
    private Vector2 _direction;
    private float _speed;
    private float _remainingDistance;
    private float _hitRadius;
    private bool _pierce;
    private readonly HashSet<ICombatUnit> _hit = new();

    /// <param name="hitRadius">발사체의 판정 반경. 적의 몸통 반경과 더해 맞았는지를 본다.</param>
    /// <param name="pierce">켜면 길 위의 적을 모두 한 번씩 맞히고, 끄면 처음 맞은 적에서 멈춘다.</param>
    public void Launch(Vector2 origin, Vector2 direction, float length, float speed, float hitRadius, bool pierce,
                       ICombatUnit caster, IBattleRuntime runtime, Action<ICombatUnit> onHit) {
        _caster = caster;
        _runtime = runtime;
        _onHit = onHit;
        _direction = direction.normalized;
        _speed = Mathf.Max(0.01f, speed);
        _remainingDistance = Mathf.Max(0f, length);
        _hitRadius = Mathf.Max(0f, hitRadius);
        _pierce = pierce;
        _hit.Clear();

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.SetPositionAndRotation(new Vector3(origin.x, origin.y, transform.position.z), Quaternion.Euler(0f, 0f, angle));
    }

    private void Update() {
        if (!_caster.Exists() || _remainingDistance <= 0f) { Destroy(gameObject); return; }

        float step = Mathf.Min(_speed * Time.deltaTime, _remainingDistance);
        Vector2 from = transform.position;
        Vector2 to = from + _direction * step;
        transform.position = new Vector3(to.x, to.y, transform.position.z);
        _remainingDistance -= step;

        if (HitAlong(from, to) && !_pierce) Destroy(gameObject);
    }

    // 이번 프레임에 지나간 선분에 걸친 적을 맞힌다. 관통하지 않으면 첫 적중에서 멈춘다.
    private bool HitAlong(Vector2 from, Vector2 to) {
        var enemies = _runtime.Enemies(_caster);
        bool any = false;
        for (int i = 0; i < enemies.Count; i++) {
            var e = enemies[i];
            if (!e.IsTargetable() || _hit.Contains(e)) continue;
            if (DistanceToSegment(e.Position, from, to) > _hitRadius + e.BodyRadius) continue;

            _hit.Add(e);
            _onHit?.Invoke(e);
            any = true;
            if (!_pierce) break;
        }
        return any;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b) {
        Vector2 ab = b - a;
        float len = ab.sqrMagnitude;
        if (len <= 0.000001f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len);
        return Vector2.Distance(p, a + ab * t);
    }

    private void OnDestroy() => _onHit = null;
}
