using DG.Tweening;
using UnityEngine;

/// <summary>
/// UIView 의 표시/숨김에 맞춰 지정 패널을 슬라이드 인/아웃 시키는 선택 컴포넌트.
/// 슬라이드가 필요한 뷰(예: 덱 팝업 우측에 도킹되는 육성/장비 패널)에만 부착한다.
/// 부착만 하면 UIView 가 자동으로 트랜지션에 포함시키므로 별도 코드 연결이 필요 없다.
///
/// 숨김 위치는 고정 오프셋(px) 대신 루트 캔버스의 실제 경계에서 매번 계산하는 것을 기본으로 한다.
/// 고정 오프셋은 해상도/화면비가 바뀌면 덜 나가거나 너무 멀리 나가지만, 경계 기준 계산은
/// 어떤 해상도에서도 "화면 밖으로 딱 벗어난 위치"를 보장한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class UISlideTransition : MonoBehaviour, IViewTransitionElement {
    public enum SlideEdge {
        Right,          // 화면 오른쪽 밖에서 들어온다.
        Left,
        Top,
        Bottom,
        CustomOffset,   // 아래 fromOffset 을 그대로 사용(예전 방식).
    }

    [Tooltip("슬라이드시킬 패널. 비우면 이 컴포넌트가 붙은 오브젝트의 RectTransform 을 사용.")]
    [SerializeField] private RectTransform panel;
    [Tooltip("어느 화면 밖에서 들어올지. 숨김 위치를 루트 캔버스 경계로부터 매번 계산한다.")]
    [SerializeField] private SlideEdge from = SlideEdge.Right;
    [Tooltip("화면 밖으로 더 밀어낼 여유(기준 해상도 px). 그림자/외곽선이 걸쳐 보이면 늘린다.")]
    [SerializeField] private float edgeMargin = 32f;
    [Tooltip("from 이 CustomOffset 일 때만 사용하는 수동 오프셋.")]
    [SerializeField] private Vector2 fromOffset = new Vector2(1200f, 0f);
    [Tooltip("등장 시 이징.")]
    [SerializeField] private Ease showEase = Ease.OutCubic;
    [Tooltip("퇴장 시 이징.")]
    [SerializeField] private Ease hideEase = Ease.InCubic;

    private Vector2 _home;
    private bool _homeCaptured;
    private readonly Vector3[] _corners = new Vector3[4];   // GetWorldCorners 재사용 버퍼(할당 방지).

    public void OnViewPrepare() {
        if (!CaptureHome()) return;
        panel.anchoredPosition = HiddenAnchoredPos();   // 숨김 시작 위치로 밀어둔다.
    }

    public void AppendTransition(Sequence seq, bool show, float duration) {
        if (!CaptureHome()) return;
        // 숨김 위치는 재생 시점의 해상도로 매번 다시 계산한다(회전/해상도 변경 후에도 정확).
        Vector2 target = show ? _home : HiddenAnchoredPos();
        seq.Join(panel.DOAnchorPos(target, duration).SetEase(show ? showEase : hideEase));
    }

    // 홈(원위치)은 최초 1회만 캡처한다. 숨김 상태에서 다시 캡처하면 열 때마다 위치가 누적으로 밀린다.
    private bool CaptureHome() {
        if (panel == null) panel = transform as RectTransform;
        if (panel == null) return false;
        if (!_homeCaptured) {
            _home = panel.anchoredPosition;
            _homeCaptured = true;
        }
        return true;
    }

    /// <summary>
    /// 패널이 지정한 화면 변 바깥으로 완전히 벗어나는 anchoredPosition.
    /// 결과는 절대 좌표라 패널이 현재 어디에 있든(트윈 중이어도) 항상 같은 값이 나온다.
    /// </summary>
    private Vector2 HiddenAnchoredPos() {
        if (from == SlideEdge.CustomOffset) return _home + fromOffset;

        var parent = panel.parent as RectTransform;
        var canvas = panel.GetComponentInParent<Canvas>();
        if (parent == null || canvas == null) return _home + fromOffset;

        var canvasRect = (RectTransform)canvas.rootCanvas.transform;
        canvasRect.GetWorldCorners(_corners);
        float canvasLeft = _corners[0].x, canvasBottom = _corners[0].y;
        float canvasRight = _corners[2].x, canvasTop = _corners[2].y;

        panel.GetWorldCorners(_corners);
        float panelLeft = _corners[0].x, panelBottom = _corners[0].y;
        float panelRight = _corners[2].x, panelTop = _corners[2].y;

        // 패널의 안쪽 경계를 화면 경계에 맞추면 = 화면 밖으로 딱 벗어난 상태.
        Vector3 worldDelta = Vector3.zero;
        switch (from) {
            case SlideEdge.Right:  worldDelta.x = canvasRight - panelLeft;   break;
            case SlideEdge.Left:   worldDelta.x = canvasLeft - panelRight;   break;
            case SlideEdge.Top:    worldDelta.y = canvasTop - panelBottom;   break;
            case SlideEdge.Bottom: worldDelta.y = canvasBottom - panelTop;   break;
        }

        Vector2 localDelta = WorldToLocalDelta(parent, worldDelta);
        switch (from) {
            case SlideEdge.Right:  localDelta.x += edgeMargin; break;
            case SlideEdge.Left:   localDelta.x -= edgeMargin; break;
            case SlideEdge.Top:    localDelta.y += edgeMargin; break;
            case SlideEdge.Bottom: localDelta.y -= edgeMargin; break;
        }
        return panel.anchoredPosition + localDelta;
    }

    // 월드 공간의 이동량을 부모 로컬(=anchoredPosition) 단위로 환산. 캔버스 스케일에 무관하게 정확하다.
    private static Vector2 WorldToLocalDelta(RectTransform parent, Vector3 worldDelta) {
        Vector3 origin = parent.InverseTransformPoint(Vector3.zero);
        Vector3 moved = parent.InverseTransformPoint(worldDelta);
        return moved - origin;
    }
}
