using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 발사체 풀. 프리팹별로 Projectile 인스턴스를 재사용한다(BattleVfxPool 형제).
/// 발사체는 스스로 대상까지 이동하다 도달 시 콜백으로 이 풀에 반환된다.
/// </summary>
public sealed class ProjectilePool : MonoBehaviour {
    [SerializeField] private Transform container;

    private readonly Dictionary<GameObject, Queue<Projectile>> _pools = new();

    public void Configure() {
        if (container == null) container = transform;
    }

    public void Launch(ProjectileSpec spec, Vector2 from, ICombatUnit source, ICombatUnit target, IBattleRuntime runtime) {
        if (spec.Prefab == null || target == null) return;

        var proj = Rent(spec.Prefab);
        if (proj == null) return;

        var prefab = spec.Prefab;   // in 파라미터가 아니라 지역으로 캡처(도달 시 같은 프리팹 큐로 반환).
        proj.gameObject.SetActive(true);
        proj.Launch(spec, from, source, target, runtime, p => Return(prefab, p));
    }

    private Projectile Rent(GameObject prefab) {
        if (!_pools.TryGetValue(prefab, out var pool)) {
            pool = new Queue<Projectile>();
            _pools[prefab] = pool;
        }
        while (pool.Count > 0) {
            var p = pool.Dequeue();
            if (p != null) return p;   // 파괴된 잔여는 건너뛴다.
        }

        var go = Instantiate(prefab, container);
        var proj = go.GetComponent<Projectile>();
        if (proj == null) {
            Debug.LogError($"[ProjectilePool] 발사체 프리팹에 Projectile 컴포넌트가 없습니다: {prefab.name}");
            Destroy(go);
            return null;
        }
        return proj;
    }

    private void Return(GameObject prefab, Projectile proj) {
        if (proj == null) return;
        proj.gameObject.SetActive(false);
        if (_pools.TryGetValue(prefab, out var pool)) pool.Enqueue(proj);
    }
}
