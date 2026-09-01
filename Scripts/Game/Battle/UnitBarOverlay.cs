using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 체력바(+게이지) 공유 오버레이. MainScene 에 미리 배치된 Canvas 하나에 바를 풀링해서 붙이고,
/// 등록된 유닛(IUnitBar)을 따라 위치/수치를 매 프레임 갱신한다.
/// 유닛 프리팹은 UI 를 갖지 않으므로 유닛 수만큼의 Canvas 리빌드/드로우콜이 사라진다(성능).
///
/// 캔버스 렌더 모드에 자동 대응한다.
///  - World Space  : 바 RectTransform 을 유닛 월드좌표(+오프셋)에 직접 배치(줌과 함께 스케일).
///  - Screen Space : worldCamera 로 월드->스크린 투영 후 배치(줌과 무관하게 픽셀 고정 크기).
/// </summary>
public sealed class UnitBarOverlay : MonoBehaviour {
    [Header("Refs")]
    [Tooltip("바가 붙을 캔버스(비우면 이 오브젝트에서 탐색). World Space/Screen Space 모두 지원.")]
    [SerializeField] private Canvas canvas;
    [Tooltip("바들의 부모 RectTransform(비우면 캔버스 자신).")]
    [SerializeField] private RectTransform container;
    [Tooltip("형태만 담은 체력바 위젯 프리팹.")]
    [SerializeField] private UnitHealthBar barPrefab;
    [Tooltip("Screen Space 캔버스일 때 월드->스크린 투영에 쓸 카메라(전투 카메라). World Space 면 불필요.")]
    [SerializeField] private Camera worldCamera;

    [Header("Placement")]
    [Tooltip("기준 앵커(BarAnchor)로부터 바를 띄울 월드 오프셋. 유닛과 함께 줌에 비례해 커지고 작아지므로 " +
             "'유닛 몸통을 넘어서는 거리'를 여기서 잡는다. 유닛별 크기 차이는 프리팹의 hitAnchor 높이로 흡수한다.")]
    [SerializeField] private Vector2 worldOffset = new(0f, 0.9f);
    [Tooltip("Screen Space 전용. 바 아랫변과 위 지점 사이에 항상 확보할 여백(캔버스 단위, 줌과 무관한 고정량). " +
             "바 자신의 높이는 자동으로 더 띄우므로 여기에는 순수 여백만 넣는다.")]
    [SerializeField] private float screenGap = 6f;

    // 유닛 1개당 바 1개. 등록/해제는 프레임 갱신(LateUpdate) 밖에서만 일어나므로 리스트 순회가 안전하다.
    private sealed class Binding {
        public IUnitBar Unit;
        public UnitHealthBar Bar;
    }

    private readonly List<Binding> _bindings = new();
    private readonly Queue<UnitHealthBar> _pool = new();
    private bool _worldSpace;

    private void Awake() {
        if (canvas == null) canvas = GetComponentInChildren<Canvas>();
        if (container == null && canvas != null) container = canvas.transform as RectTransform;
        _worldSpace = canvas != null && canvas.renderMode == RenderMode.WorldSpace;
        if (worldCamera == null) worldCamera = Camera.main;
    }

    /// <summary>유닛에 바를 부여한다(중복 등록 무시).</summary>
    public void Register(IUnitBar unit) {
        if (unit == null || barPrefab == null || IndexOf(unit) >= 0) return;

        var bar = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(barPrefab, container);
        bar.ConfigureGauge(unit.HasGauge);
        bar.SetVisible(true);
        _bindings.Add(new Binding { Unit = unit, Bar = bar });
    }

    /// <summary>유닛의 바를 회수한다(풀로 반환).</summary>
    public void Unregister(IUnitBar unit) {
        if (unit == null) return;
        int i = IndexOf(unit);
        if (i < 0) return;

        var bar = _bindings[i].Bar;
        bar.SetVisible(false);
        _pool.Enqueue(bar);

        int last = _bindings.Count - 1;
        _bindings[i] = _bindings[last];
        _bindings.RemoveAt(last);
    }

    private int IndexOf(IUnitBar unit) {
        for (int i = 0; i < _bindings.Count; i++)
            if (ReferenceEquals(_bindings[i].Unit, unit)) return i;
        return -1;
    }

    private void LateUpdate() {
        for (int i = 0; i < _bindings.Count; i++) {
            var unit = _bindings[i].Unit;
            var bar = _bindings[i].Bar;

            bool active = unit.BarActive && unit.BarAnchor != null;
            bar.SetVisible(active);
            if (!active) continue;

            Vector3 world = unit.BarAnchor.position + (Vector3)worldOffset;
            Place(bar.Rect, world);

            bar.ApplyHp(unit.HpRatio);
            if (unit.HasGauge) bar.ApplyGauge(unit.GaugeRatio);
        }
    }

    private void Place(RectTransform barRect, Vector3 world) {
        if (_worldSpace) {
            // World Space 캔버스는 바도 유닛과 함께 줌 스케일되므로 월드 오프셋만으로 간격 비율이 유지된다.
            barRect.position = world;
            return;
        }

        // Screen Space: 월드->스크린 투영 후 컨테이너 로컬좌표로 변환.
        Vector2 screen = worldCamera != null
            ? (Vector2)worldCamera.WorldToScreenPoint(world)
            : (Vector2)world;
        Camera uiCam = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, uiCam, out var local)) return;

        // 월드 오프셋은 줌에 비례해 줄어들지만 바 자신은 픽셀 고정 크기라, 투영 지점에 그대로 놓으면
        // 줌 아웃할수록 바가 상대적으로 커지며 유닛을 덮는다. 바의 아랫변이 항상 그 지점 위로 오도록
        // 자기 높이(피벗 기준)와 여백만큼 올린다. 이 보정량은 바와 같은 픽셀 고정량이라 줌과 무관하게 일정하다.
        local.y += barRect.rect.height * barRect.pivot.y + screenGap;
        barRect.anchoredPosition = local;
    }
}
