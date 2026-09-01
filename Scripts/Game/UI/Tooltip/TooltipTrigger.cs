using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 이 UI 요소를 누르거나(모바일 터치/클릭) 마우스를 올리면 TooltipView 에 제목/설명을 띄우고,
/// 손을 떼거나 벗어나면 숨긴다. 표시 내용은 SetContent 로 주입하며, 내용이 비면 뜨지 않는다.
/// (이 컴포넌트는 Raycast 대상인 Graphic 이 있는 오브젝트에 붙여야 포인터 이벤트를 받는다. 예: 스킬 아이콘 Image.)
///
/// tooltip 을 인스펙터에서 지정하면 그 패널을 쓰고, 비워두면 전역 공유 툴팁(TooltipView.Active)을 자동으로 쓴다.
/// 따라서 보상 아이콘 등 어느 프리팹이든 이 컴포넌트만 붙이고 SetContent 로 이름을 넣으면 툴팁이 동작한다(패널 배선 불필요).
/// </summary>
public sealed class TooltipTrigger : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler {
    [Tooltip("설명을 띄울 툴팁 패널. 비우면 전역 공용 툴팁(TooltipView.Active)을 사용한다.")]
    [SerializeField] private TooltipView tooltip;

    private string _title;
    private string _body;
    private bool _enabled = true;

    // 지정 패널이 있으면 그것을, 없으면 전역 툴팁을 사용한다.
    private TooltipView Tip => tooltip != null ? tooltip : TooltipView.Active;

    private bool HasContent => !string.IsNullOrEmpty(_title) || !string.IsNullOrEmpty(_body);

    /// <summary>툴팁에 표시할 내용을 설정한다. 비우면 표시되지 않는다.</summary>
    public void SetContent(string title, string body) {
        _title = title;
        _body = body;
        if (!HasContent) Tip?.Hide();
    }

    /// <summary>일시적으로 툴팁 표시를 켜고 끈다(끄면 즉시 숨김).</summary>
    public void SetEnabled(bool on) {
        _enabled = on;
        if (!on) Tip?.Hide();
    }

    public void OnPointerDown(PointerEventData e) => ShowTip();
    public void OnPointerEnter(PointerEventData e) => ShowTip();   // 마우스(에디터) 대응
    public void OnPointerUp(PointerEventData e) => Tip?.Hide();
    public void OnPointerExit(PointerEventData e) => Tip?.Hide();

    private void OnDisable() => Tip?.Hide();

    private void ShowTip() {
        if (!_enabled || !HasContent) return;
        Tip?.Show(_title, _body, (RectTransform)transform);
    }
}
