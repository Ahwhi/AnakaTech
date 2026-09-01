using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전역 UI 파티클 VFX 재생 레이어. UIParticle 프리팹을 대상 UI 요소 위치에 풀링 재생하고, 파티클이 끝나면 반환한다.
/// 팝업보다 위의 전용 Canvas 아래에 두고 UIRoot 가 참조한다. 어느 UI 든 IUIService.PlayVfx 로 이 레이어에 재생을 요청한다.
/// UIParticle 이 파티클을 CanvasRenderer 로 그리므로 Overlay 캔버스에서도 UI 위에 정상 합성된다.
///
/// 코루틴으로 반환을 관리하므로 이 오브젝트(및 상위 캔버스)는 항상 활성이어야 한다(유휴 시엔 아무것도 렌더되지 않는다).
/// </summary>
public sealed class UIVfxLayer : MonoBehaviour {
    [Tooltip("생성된 VFX 를 담을 부모(RectTransform). 비우면 이 오브젝트.")]
    [SerializeField] private RectTransform container;
    [Tooltip("파티클 길이를 못 구할 때 쓰는 기본 수명(초).")]
    [SerializeField] private float defaultLifetime = 2f;

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
    private Canvas _canvas;
    private readonly Vector3[] _corners = new Vector3[4];

    private void Awake() {
        if (container == null) container = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>(true);
    }

    /// <summary>대상 UI 요소 중앙에 VFX 프리팹을 재생한다.</summary>
    public void Play(GameObject prefab, RectTransform target) {
        if (target == null) return;
        Play(prefab, ScreenPointOf(target));
    }

    /// <summary>지정 화면 좌표에 VFX 프리팹을 재생한다(움직이는 대상의 특정 지점 등에 사용).</summary>
    public void Play(GameObject prefab, Vector2 screenPoint) {
        if (prefab == null) return;

        var instance = Rent(prefab);
        instance.transform.SetParent(container, false);
        PositionAtScreen(instance, screenPoint);
        instance.SetActive(true);
        instance.transform.SetAsLastSibling();

        float life = PlayParticles(instance);
        StartCoroutine(ReturnAfter(prefab, instance, life));
    }

    // 대상의 화면 중앙 위치를 구한다(오버레이/카메라 캔버스 무관).
    private Vector2 ScreenPointOf(RectTransform target) {
        var targetCanvas = target.GetComponentInParent<Canvas>();
        Camera srcCam = (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? targetCanvas.worldCamera : null;
        target.GetWorldCorners(_corners);   // 0:BL 2:TR
        Vector3 center = (_corners[0] + _corners[2]) * 0.5f;
        return RectTransformUtility.WorldToScreenPoint(srcCam, center);
    }

    private GameObject Rent(GameObject prefab) {
        if (!_pools.TryGetValue(prefab, out var pool)) {
            pool = new Queue<GameObject>();
            _pools[prefab] = pool;
        }
        while (pool.Count > 0) {
            var g = pool.Dequeue();
            if (g != null) return g;   // 파괴된 잔여는 건너뛴다.
        }
        return Instantiate(prefab, container);
    }

    // 화면 좌표를 이 레이어(container)의 로컬 좌표로 변환해 배치(오버레이/카메라 캔버스 무관).
    private void PositionAtScreen(GameObject instance, Vector2 screen) {
        Camera dstCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, dstCam, out var local))
            ((RectTransform)instance.transform).anchoredPosition = local;
    }

    // 자식 ParticleSystem 을 각각 1회 재시작하고, 가장 긴 수명을 추정해 반환한다.
    private float PlayParticles(GameObject instance) {
        float life = 0f;
        var systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems) {
            ps.Clear(false);
            ps.Play(false);
            var main = ps.main;
            float d = main.duration + main.startLifetime.constantMax;
            if (d > life) life = d;
        }
        return life > 0f ? life : defaultLifetime;
    }

    private IEnumerator ReturnAfter(GameObject prefab, GameObject instance, float delay) {
        yield return new WaitForSecondsRealtime(delay);   // 타임스케일 0(연출/일시정지) 에서도 정상 반환.
        if (instance == null) yield break;
        instance.SetActive(false);
        if (_pools.TryGetValue(prefab, out var pool)) pool.Enqueue(instance);
    }
}
