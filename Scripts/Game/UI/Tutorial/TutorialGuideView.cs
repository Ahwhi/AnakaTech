using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 유도 표시. 가이드 프레임 하나를 대상 UI 바깥 테두리에 맞춰 씌우고, 여백을 늘렸다 줄이며 숨쉬게 한다.
///
/// 크기(sizeDelta)를 움직이고 스케일을 건드리지 않는 이유: 프레임은 보통 9-slice 라 스케일을 키우면 테두리 굵기까지
/// 같이 굵어져 뭉개진다. 여백만 움직이면 굵기는 그대로고 "밖에서 조여 오는" 느낌만 남는다.
///
/// 대상은 매 프레임 따라간다. 스크롤/레이아웃/팝업 등장 연출로 버튼이 움직여도 프레임이 어긋나지 않아야 하기 때문이다.
/// 대상이 어느 캔버스에 있든(팝업은 각자 정렬 캔버스를 갖는다) 화면 좌표를 거쳐 변환하므로 상관하지 않는다.
///
/// 배치 규약: 이 오브젝트(또는 content)는 화면 전체를 덮는 RectTransform 이고, 팝업보다 위 정렬순서의
/// 전용 Canvas 아래에 둔다. 프레임/손/문구/차단판의 앵커는 코드가 중앙으로 맞추므로 인스펙터에서 신경 쓸 필요 없다.
/// </summary>
public sealed class TutorialGuideView : MonoBehaviour {
    [Header("Layout")]
    [Tooltip("화면 전체를 덮는 기준 영역(비우면 이 오브젝트). 프레임/문구/차단판이 모두 이 안에서 배치된다.")]
    [SerializeField] private RectTransform content;

    [Tooltip("가리키는 대상이 있는 캔버스보다 이만큼 위에 그린다. 이 값 덕분에 메인 화면을 가리킬 때는 " +
             "팝업 아래에, 팝업 안을 가리킬 때는 그 팝업 위에 그려진다(캔버스 정렬순서를 고정해 두면 둘 중 " +
             "하나는 반드시 어긋난다).")]
    [SerializeField] private int sortingOffset = 1;

    [Header("Frame")]
    [Tooltip("대상 버튼을 감쌀 가이드 프레임 이미지(9-slice 권장).")]
    [SerializeField] private RectTransform frame;
    [Tooltip("가장 조였을 때 대상 바깥으로 벌어지는 여백(px).")]
    [SerializeField] private float expandFrom = 6f;
    [Tooltip("가장 벌어졌을 때의 여백(px).")]
    [SerializeField] private float expandTo = 26f;
    [Tooltip("한 번 벌어지는 데 걸리는 시간(초). 왕복이므로 주기는 이 값의 두 배다.")]
    [SerializeField] private float pulseDuration = 0.7f;

    [Header("Hand (선택)")]
    [Tooltip("탭을 유도하는 손 아이콘. 비우면 표시하지 않는다.")]
    [SerializeField] private RectTransform hand;
    [Tooltip("대상 중앙 기준 손 아이콘 위치 보정(px).")]
    [SerializeField] private Vector2 handOffset = new(30f, -30f);
    [Tooltip("손 아이콘이 눌리는 느낌을 낼 때 줄어드는 비율.")]
    [SerializeField] private float handPressScale = 0.85f;

    [Header("Hint (선택)")]
    [Tooltip("안내 문구 묶음(말풍선 등). 비우면 문구를 표시하지 않는다.")]
    [SerializeField] private RectTransform hintGroup;
    [SerializeField] private TMP_Text hintText;
    [Tooltip("프레임과 문구 사이 간격(px).")]
    [SerializeField] private float hintGap = 28f;

    [Header("Block (선택)")]
    [Tooltip("가리키는 곳 외를 막는 판 4개(위/아래/왼쪽/오른쪽). 넷 다 비우면 차단 기능을 쓰지 않는다. " +
             "반투명 검정 이미지를 쓰면 딤 효과까지 함께 난다.")]
    [SerializeField] private RectTransform blockTop;
    [SerializeField] private RectTransform blockBottom;
    [SerializeField] private RectTransform blockLeft;
    [SerializeField] private RectTransform blockRight;

    private static readonly Vector3[] Corners = new Vector3[4];
    private static readonly Vector2 Center = new(0.5f, 0.5f);

    private Canvas _canvas;
    private TutorialAnchor _anchor;
    private bool _blocking;
    private float _expand;
    private Tween _pulse;
    private Tween _handPulse;

    private RectTransform Content => content != null ? content : (content = transform as RectTransform);

    private void Awake() {
        _canvas = GetComponentInParent<Canvas>(true);

        var contentRect = Content;
        if (contentRect != null) contentRect.pivot = Center;

        // 코드가 위치/크기를 직접 넣으므로 앵커는 중앙 고정이어야 한다(인스펙터 배선 실수를 원천 차단).
        PrepareRect(frame, false);
        PrepareRect(hand, false);
        PrepareRect(hintGroup, false);
        PrepareRect(blockTop, true);
        PrepareRect(blockBottom, true);
        PrepareRect(blockLeft, true);
        PrepareRect(blockRight, true);

        Hide();
    }

    // raycast: 프레임/손/문구는 대상 버튼의 클릭을 가로채면 안 되고, 차단판은 반대로 반드시 가로채야 한다.
    private static void PrepareRect(RectTransform rect, bool raycast) {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = rect.pivot = Center;
        foreach (var graphic in rect.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = raycast;
    }

    /// <summary>지금 무언가를 가리키고 있는지.</summary>
    public bool IsShowing => _anchor != null;

    /// <summary>
    /// 지정한 지점을 가리킨다. 같은 지점을 다시 요청하면 문구/차단 여부만 갱신하고 연출은 이어 간다
    /// (가이드는 짧은 주기로 다시 판단하므로, 매번 처음부터 재생되면 프레임이 계속 튄다).
    /// </summary>
    public void Show(TutorialAnchor anchor, string hint, bool blockOutside) {
        if (anchor == null) {
            Hide();
            return;
        }

        bool changed = _anchor != anchor;
        _anchor = anchor;
        _blocking = blockOutside;

        SetActive(frame, true);
        SetActive(hand, true);
        SetActive(hintGroup, !string.IsNullOrEmpty(hint));
        if (hintText != null) hintText.text = hint;

        if (changed || _pulse == null || !_pulse.IsActive()) StartPulse();
        Follow();
    }

    /// <summary>표시를 끈다(차단판도 함께 꺼져 입력이 다시 자유로워진다).</summary>
    public void Hide() {
        _anchor = null;
        _blocking = false;

        _pulse?.Kill();
        _pulse = null;
        _handPulse?.Kill();
        _handPulse = null;

        SetActive(frame, false);
        SetActive(hand, false);
        SetActive(hintGroup, false);
        SetActive(blockTop, false);
        SetActive(blockBottom, false);
        SetActive(blockLeft, false);
        SetActive(blockRight, false);
    }

    // 대상이 움직여도 어긋나지 않도록 매 프레임 맞춘다(레이아웃이 확정된 뒤여야 하므로 LateUpdate).
    private void LateUpdate() {
        if (_anchor == null) return;
        if (!_anchor.IsPointable) {
            // 가리키던 곳이 사라졌다(팝업이 닫힘 등). 다음 대상은 가이드(Director)가 정하므로 여기서는 지운다.
            Hide();
            return;
        }
        Follow();
    }

    private void StartPulse() {
        _pulse?.Kill();
        _expand = expandFrom;
        _pulse = DOTween.To(() => _expand, v => _expand = v, expandTo, pulseDuration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetUpdate(true);   // 팝업/연출로 타임스케일이 0 이어도 계속 숨쉰다.

        if (hand == null) return;
        _handPulse?.Kill();
        hand.localScale = Vector3.one;
        _handPulse = hand.DOScale(handPressScale, pulseDuration)
                         .SetEase(Ease.InOutSine)
                         .SetLoops(-1, LoopType.Yoyo)
                         .SetUpdate(true);
    }

    // 대상의 화면상 영역을 이 뷰의 좌표로 옮겨 프레임/손/문구/차단판을 배치한다.
    private void Follow() {
        if (!TryResolveTarget(out var min, out var max)) return;

        ApplySorting(_anchor.Area);

        Vector2 pad = _anchor.Padding + new Vector2(_expand, _expand);
        Vector2 frameMin = min - pad;
        Vector2 frameMax = max + pad;

        SetRect(frame, frameMin, frameMax);

        if (hand != null) hand.anchoredPosition = (min + max) * 0.5f + handOffset;
        PlaceHint(frameMin, frameMax);
        PlaceBlockers(frameMin, frameMax);
    }

    // 대상이 그려지는 캔버스 바로 위로 이 뷰를 올린다.
    //
    // 정렬순서를 고정해 두면 반드시 한쪽이 어긋난다. 낮게 두면 팝업 안의 버튼을 가리킬 때 프레임이 팝업에
    // 가려지고, 높게 두면 메인 화면(퀘스트 수령 버튼 등)을 가리킬 때 팝업들 위에 프레임만 둥둥 뜬다.
    // 대상을 따라가면 두 경우가 저절로 맞는다. 대상이 가려져 있으면 프레임도 같이 가려지는 것이 옳다.
    private void ApplySorting(RectTransform target) {
        if (_canvas == null || target == null) return;

        int order = SortingOrderOf(target) + sortingOffset;
        if (_canvas.sortingOrder != order) _canvas.sortingOrder = order;
    }

    // 대상의 정렬순서를 정하는 캔버스를 찾는다. 중첩 캔버스는 overrideSorting 이 켜져 있을 때만
    // 자기 정렬순서를 쓰므로(그 전에는 부모를 따른다) 그 조건에 맞는 캔버스까지 거슬러 올라간다.
    private static int SortingOrderOf(RectTransform target) {
        var canvas = target.GetComponentInParent<Canvas>();
        while (canvas != null && !canvas.isRootCanvas && !canvas.overrideSorting) {
            var parent = canvas.transform.parent;
            canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
        }
        return canvas != null ? canvas.sortingOrder : 0;
    }

    // 대상 RectTransform 의 네 모서리를 화면 좌표로 바꾼 뒤 이 뷰의 로컬 좌표로 다시 옮긴다.
    // 대상이 팝업 전용 캔버스에 있어도, 카메라 캔버스여도 같은 경로로 처리된다.
    private bool TryResolveTarget(out Vector2 min, out Vector2 max) {
        min = max = Vector2.zero;

        var target = _anchor.Area;
        var contentRect = Content;
        if (target == null || contentRect == null) return false;

        var srcCanvas = target.GetComponentInParent<Canvas>();
        Camera srcCam = (srcCanvas != null && srcCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? srcCanvas.worldCamera : null;
        Camera dstCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;

        target.GetWorldCorners(Corners);
        bool first = true;
        for (int i = 0; i < 4; i++) {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(srcCam, Corners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRect, screen, dstCam, out var local)) continue;

            if (first) {
                min = max = local;
                first = false;
                continue;
            }
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        return !first;
    }

    // 문구는 프레임을 가리지 않도록 반대편에 둔다. 대상이 화면 위쪽이면 아래에, 아래쪽이면 위에 붙인다.
    private void PlaceHint(Vector2 frameMin, Vector2 frameMax) {
        if (hintGroup == null || !hintGroup.gameObject.activeSelf) return;

        float half = hintGroup.rect.height * 0.5f;
        bool upperHalf = (frameMin.y + frameMax.y) * 0.5f > 0f;
        float y = upperHalf ? frameMin.y - hintGap - half : frameMax.y + hintGap + half;

        float limit = Mathf.Max(0f, Content.rect.width * 0.5f - hintGroup.rect.width * 0.5f);
        float x = Mathf.Clamp((frameMin.x + frameMax.x) * 0.5f, -limit, limit);
        hintGroup.anchoredPosition = new Vector2(x, y);
    }

    // 프레임을 뺀 나머지를 판 4개로 덮는다. 대상만 눌리므로 유도한 곳이 아닌 곳을 눌러 길을 잃는 일이 없다.
    private void PlaceBlockers(Vector2 holeMin, Vector2 holeMax) {
        if (!_blocking) {
            SetActive(blockTop, false);
            SetActive(blockBottom, false);
            SetActive(blockLeft, false);
            SetActive(blockRight, false);
            return;
        }

        var area = Content.rect;
        Vector2 areaMin = area.min;
        Vector2 areaMax = area.max;

        // 대상이 영역 밖으로 나가면 구멍이 뒤집혀 판이 어긋나므로 영역 안으로 눌러 담는다.
        holeMin = Vector2.Max(holeMin, areaMin);
        holeMax = Vector2.Min(holeMax, areaMax);

        SetActive(blockTop, true);
        SetActive(blockBottom, true);
        SetActive(blockLeft, true);
        SetActive(blockRight, true);

        SetRect(blockTop, new Vector2(areaMin.x, holeMax.y), areaMax);
        SetRect(blockBottom, areaMin, new Vector2(areaMax.x, holeMin.y));
        SetRect(blockLeft, new Vector2(areaMin.x, holeMin.y), new Vector2(holeMin.x, holeMax.y));
        SetRect(blockRight, new Vector2(holeMax.x, holeMin.y), new Vector2(areaMax.x, holeMax.y));
    }

    // 중앙 앵커 기준으로 두 점이 이루는 사각형에 맞춘다(음수 크기가 나오지 않게 0 으로 자른다).
    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max) {
        if (rect == null) return;
        rect.sizeDelta = new Vector2(Mathf.Max(0f, max.x - min.x), Mathf.Max(0f, max.y - min.y));
        rect.anchoredPosition = (min + max) * 0.5f;
    }

    private static void SetActive(Component target, bool on) {
        if (target != null && target.gameObject.activeSelf != on) target.gameObject.SetActive(on);
    }

    private void OnDestroy() {
        _pulse?.Kill();
        _handPulse?.Kill();
    }
}
