using System;
using UnityEngine.UI;

/// <summary>
/// "배경을 눌러 닫기" 배선 공용 창구.
///
/// 팝업마다 투명 버튼을 만들고 리스너를 다는 반복을 없애기 위한 것이다. 배경으로 쓸 그래픽(보통 Dim)만
/// 넘기면 클릭 수신에 필요한 것(레이캐스트 대상 지정, 전환 없는 버튼)을 여기서 갖춘다.
///
/// UIView 를 상속하는 팝업은 베이스가 알아서 부르므로 신경 쓸 것이 없고, 화면 안에 도킹되는 패널처럼
/// UIView 가 아닌 것들이 같은 조작을 제공할 때 이 창구를 직접 쓴다(같은 조작이 두 벌로 갈라지지 않게).
/// </summary>
public static class TapToClose {
    /// <summary>
    /// 지정한 그래픽을 눌렀을 때 onDismiss 가 불리도록 배선한다. area 가 null 이면 아무 일도 하지 않는다
    /// (할당하지 않은 화면은 배경 탭으로 닫히지 않는다 = 할당이 곧 사용 여부다).
    /// </summary>
    public static void Bind(Graphic area, Action onDismiss) {
        if (area == null || onDismiss == null) return;

        // 눌림을 받으려면 레이캐스트 대상이어야 한다. 프리팹에서 꺼져 있어도 여기서 켠다.
        area.raycastTarget = true;

        // 배경은 눌렸을 때 색이 변하면 안 되므로 전환 없는 버튼을 쓴다(이미 붙어 있으면 그대로 재사용).
        var button = area.GetComponent<Button>();
        if (button == null) {
            button = area.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }
        button.onClick.AddListener(() => onDismiss());
    }
}
