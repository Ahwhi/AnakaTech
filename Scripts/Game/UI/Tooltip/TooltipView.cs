using TMPro;
using UnityEngine;

/// <summary>
/// 대상 UI 요소 근처에 제목/설명을 띄우는 공용 툴팁 패널(상용 게임식 간단 툴팁).
/// Show/Hide 만 노출하며, 대상의 상단 중앙을 기준으로 배치한다(오버레이/카메라 캔버스 무관).
///
/// 사용 방식 2가지:
///  1) 팝업 내장: 팝업 프리팹에 1개 배치하고 각 TooltipTrigger 의 tooltip 에 할당(기존 방식, 그 팝업 안에서만 공유).
///  2) 전역 공유(권장): registerAsGlobal 을 켠 인스턴스를 팝업보다 위 캔버스에 1개 배치하면, tooltip 을 비워둔
///     모든 TooltipTrigger 가 이 전역 툴팁으로 자동 연결된다(프리팹마다 패널을 심을 필요가 없다).
///
/// 전역 인스턴스는 항상 활성 상태여야 등록되므로, 보이기/숨기기는 루트가 아니라 panel(내용) 을 토글한다.
/// </summary>
public sealed class TooltipView : MonoBehaviour {
    [Tooltip("툴팁 본체(내용) RectTransform. 비우면 이 오브젝트를 사용. 전역 툴팁은 반드시 자식 패널을 지정한다(루트는 항상 켜져 있어야 함).")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [Tooltip("대상(상단 중앙) 기준 추가 오프셋(px).")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 12f);
    [Tooltip("켜면 이 인스턴스를 전역 공유 툴팁으로 등록한다(오브젝트가 활성일 때만). 비활성으로 둘 거면 대신 UIRoot 의 tooltip 필드에 할당하라(그쪽이 확실).")]
    [SerializeField] private bool registerAsGlobal;

    /// <summary>전역 공유 툴팁(없으면 null). TooltipTrigger 가 자기 tooltip 미할당 시 폴백으로 사용한다.</summary>
    public static TooltipView Active { get; private set; }

    private RectTransform _panel;
    private RectTransform _parent;
    private Canvas _canvas;
    private bool _initialized;
    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>이 인스턴스를 전역 공유 툴팁으로 등록한다. 오브젝트가 비활성이어도 되므로(항상 활성인 UIRoot 가 호출),
    /// Awake 에 의존하지 않아 툴팁을 꺼둔 채로도 안전하게 등록된다.</summary>
    public void RegisterAsGlobal() => Active = this;

    private void Awake() {
        if (registerAsGlobal) RegisterAsGlobal();   // 오브젝트가 활성일 때만 실행됨(비활성이면 UIRoot 경로 사용).
    }

    private void Start() => Hide();   // 시작 시 내용은 숨김.

    private void OnDestroy() {
        if (Active == this) Active = null;
    }

    private void EnsureInit() {
        if (_initialized) return;
        _panel = panel != null ? panel : (RectTransform)transform;
        _parent = _panel.parent as RectTransform;
        _canvas = GetComponentInParent<Canvas>(true);   // 비활성 상태에서 초기화돼도 캔버스를 찾도록 includeInactive.
        _initialized = true;
    }

    /// <summary>제목/설명을 채우고 대상 근처에 표시한다.</summary>
    public void Show(string title, string body, RectTransform target) {
        if (target == null) return;
        EnsureInit();

        // 내용이 없는 줄은 텍스트를 비우는 데 그치지 않고 오브젝트 자체를 끈다.
        // 빈 텍스트도 레이아웃에서는 자리를 차지해(줄 높이/여백) 패널이 필요 이상으로 늘어나기 때문.
        if (titleText != null) {
            titleText.text = title ?? "";
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }
        if (bodyText != null) {
            bodyText.text = body ?? "";
            bodyText.gameObject.SetActive(!string.IsNullOrEmpty(body));
        }

        // 꺼둔 채 시작한 전역 툴팁도 뜨도록, 상위 캔버스와 이 오브젝트를 함께 켠다.
        // (등록은 UIRoot 가 하므로 툴팁/캔버스를 꺼두고 시작해도 안전하다.)
        if (_canvas != null && !_canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(true);
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!_panel.gameObject.activeSelf) _panel.gameObject.SetActive(true);   // 내용이 별도 자식이면 그것도 보장.

        _panel.SetAsLastSibling();   // 같은 부모 내 다른 UI 위로 올린다.
        PositionNear(target);
    }

    public void Hide() {
        // 이 오브젝트만 끈다(상위 캔버스는 건드리지 않아 공유 캔버스여도 안전). 꺼진 툴팁은 렌더되지 않는다.
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    // 대상의 상단 중앙 월드 좌표를 스크린 -> 부모 로컬로 변환해 배치(오버레이/카메라 캔버스 모두 대응).
    private void PositionNear(RectTransform target) {
        if (_parent == null) return;
        Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;

        target.GetWorldCorners(_corners);   // 0:BL 1:TL 2:TR 3:BR
        Vector3 topCenterWorld = (_corners[1] + _corners[2]) * 0.5f;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, topCenterWorld);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, screen, cam, out var local))
            _panel.anchoredPosition = local + offset;
    }
}
