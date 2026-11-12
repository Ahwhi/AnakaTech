using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScrollRect 공용 확장. 목록 안의 특정 항목이 보이도록 스크롤 위치를 맞춘다.
/// 콘텐츠 피벗/앵커와 무관하게 콘텐츠 사각형 기준으로 계산하므로 레이아웃 구성이 달라도 그대로 쓸 수 있다.
/// </summary>
public static class ScrollRectExtensions {
    /// <summary>
    /// target 이 뷰포트 안의 정해진 자리에 오도록 스크롤한다. 켜져 있는 축(세로/가로)만 움직인다.
    /// viewportAnchor 는 0 = 항목의 시작(위/왼쪽)을 뷰포트 시작에, 0.5 = 가운데에, 1 = 끝(아래/오른쪽)을 끝에 맞춘다.
    /// 방금 생성한 항목이어도 되도록 콘텐츠 레이아웃을 먼저 다시 계산한다. 콘텐츠가 뷰포트보다 작으면 움직이지 않는다.
    /// </summary>
    public static void ScrollTo(this ScrollRect scroll, RectTransform target, float viewportAnchor = 0.5f) {
        if (scroll == null || target == null || scroll.content == null) return;

        var content = scroll.content;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        Rect view = viewport.rect;
        Rect area = content.rect;
        Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(content, target);
        float a = Mathf.Clamp01(viewportAnchor);

        if (scroll.vertical) {
            float hidden = area.height - view.height;
            if (hidden > 0f) {
                float point = Mathf.Lerp(b.max.y, b.min.y, a);           // 항목에서 맞출 지점(위 -> 아래)
                float offset = (area.yMax - point) - view.height * a;    // 콘텐츠 위에서 뷰포트 위까지의 거리
                scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(offset / hidden);   // 1 = 맨 위
            }
        }

        if (scroll.horizontal) {
            float hidden = area.width - view.width;
            if (hidden > 0f) {
                float point = Mathf.Lerp(b.min.x, b.max.x, a);           // 항목에서 맞출 지점(왼쪽 -> 오른쪽)
                float offset = (point - area.xMin) - view.width * a;     // 콘텐츠 왼쪽에서 뷰포트 왼쪽까지의 거리
                scroll.horizontalNormalizedPosition = Mathf.Clamp01(offset / hidden);      // 0 = 맨 왼쪽
            }
        }

        scroll.velocity = Vector2.zero;
    }

    /// <summary>맨 처음(위/왼쪽)으로 되돌린다. 목록을 다른 내용으로 갈아 끼웠을 때 이전 위치가 남지 않게 한다.</summary>
    public static void ScrollToStart(this ScrollRect scroll) {
        if (scroll == null) return;
        if (scroll.vertical) scroll.verticalNormalizedPosition = 1f;
        if (scroll.horizontal) scroll.horizontalNormalizedPosition = 0f;
        scroll.velocity = Vector2.zero;
    }
}
