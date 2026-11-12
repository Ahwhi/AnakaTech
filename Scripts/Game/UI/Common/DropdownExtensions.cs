using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TMP_Dropdown 공용 확장. 펼친 옵션 목록이 지금 선택된 옵션 자리에서 보이도록 한다.
/// 기본 TMP_Dropdown 은 목록을 펼칠 때마다 맨 위부터 보여 주므로, 옵션이 많으면 선택된 항목을 다시 찾아 내려가야 한다.
/// </summary>
public static class DropdownExtensions {
    /// <summary>
    /// 목록이 펼쳐질 때마다 선택된 옵션으로 스크롤한다. 반환값을 해제하면 멈춘다(보통 AddTo 로 화면 수명에 묶는다).
    /// TMP_Dropdown 에는 펼침 이벤트가 없어 IsExpanded 가 꺼짐 -> 켜짐으로 바뀌는 순간을 본다.
    /// </summary>
    public static IDisposable ScrollToSelectedOnExpand(this TMP_Dropdown dropdown, float viewportAnchor = 0.5f) {
        if (dropdown == null) return Disposable.Empty;
        return Observable.EveryUpdate()
            .Select(_ => dropdown != null && dropdown.IsExpanded)
            .DistinctUntilChanged()
            .Where(expanded => expanded)
            .Subscribe(_ => ScrollListToSelected(dropdown, viewportAnchor));
    }

    /// <summary>
    /// 펼쳐진 옵션 목록을 선택된 옵션으로 스크롤한다. 목록이 닫혀 있으면 아무것도 하지 않는다.
    /// 펼친 목록은 템플릿을 복제해 드롭다운 아래에 만들어지고 템플릿 자신은 꺼져 있으므로,
    /// 켜져 있는 ScrollRect 와 그 안의 Toggle 순서가 곧 옵션 순서다.
    /// </summary>
    public static void ScrollListToSelected(this TMP_Dropdown dropdown, float viewportAnchor = 0.5f) {
        if (dropdown == null || !dropdown.IsExpanded) return;

        var list = dropdown.GetComponentInChildren<ScrollRect>();
        if (list == null || list.content == null) return;

        var items = list.content.GetComponentsInChildren<Toggle>();
        int index = dropdown.value;
        if (index < 0 || index >= items.Length) return;

        list.ScrollTo((RectTransform)items[index].transform, viewportAnchor);
    }
}
