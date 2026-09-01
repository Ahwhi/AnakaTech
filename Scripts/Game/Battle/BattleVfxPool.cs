using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 VFX 풀. 프리팹별로 인스턴스를 재사용하며, 재생 후 일정 수명이 지나면 자동으로 풀에 반환한다.
/// 캐릭터/몬스터의 자기/타격 VFX 및 적 공통 피격 VFX 를 모두 이 풀로 재생한다(PLANNING 1-8,9).
/// </summary>
public sealed class BattleVfxPool : MonoBehaviour {
    [SerializeField] private Transform container;

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
    private BattleSettings _settings;

    public void Configure(BattleSettings settings) {
        _settings = settings;
        if (container == null) container = transform;
    }

    /// <summary>방향 없이 재생(프리팹 기본 회전 사용). 풀 재사용 시 이전 방향 회전 잔상을 프리팹 기본값으로 되돌린다.</summary>
    public void Play(GameObject prefab, Vector2 at) => PlayInternal(prefab, at, 0f, false);

    /// <summary>angleDeg(월드 Z 회전) 방향을 바라보도록 회전시켜 재생(근접/광역 등 공격 방향 연출).
    /// 프리팹의 기본 회전 위에 방향 회전을 합성하므로, 프리팹이 기울어져 있어도 상대 각도를 유지한다.</summary>
    public void Play(GameObject prefab, Vector2 at, float angleDeg) => PlayInternal(prefab, at, angleDeg, true);

    private void PlayInternal(GameObject prefab, Vector2 at, float angleDeg, bool directed) {
        if (prefab == null) return;

        if (!_pools.TryGetValue(prefab, out var pool)) {
            pool = new Queue<GameObject>();
            _pools[prefab] = pool;
        }

        GameObject instance = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab, container);
        // 풀에서 꺼낸 인스턴스는 이전 재생의 방향 회전이 남아있을 수 있으므로 매번 회전을 명시적으로 설정한다.
        Quaternion rot = directed
            ? Quaternion.Euler(0f, 0f, angleDeg) * prefab.transform.rotation
            : prefab.transform.rotation;
        instance.transform.SetPositionAndRotation(at, rot);
        instance.SetActive(true);

        // 파티클을 다시 재생(풀에서 꺼낸 경우).
        var ps = instance.GetComponent<ParticleSystem>();
        if (ps != null) { ps.Clear(true); ps.Play(true); }

        StartCoroutine(ReturnAfter(prefab, instance, _settings != null ? _settings.VfxLifetime : 1f));
    }

    private IEnumerator ReturnAfter(GameObject prefab, GameObject instance, float delay) {
        yield return new WaitForSeconds(delay);
        if (instance == null) yield break;
        instance.SetActive(false);
        _pools[prefab].Enqueue(instance);
    }
}
