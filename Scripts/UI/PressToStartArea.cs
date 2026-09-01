using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// "터치하여 게임시작"용 전체 화면 클릭 영역. EventSystem 레이캐스트로 클릭을 받으므로,
/// 위에 있는 버튼/패널이 자기 클릭을 먼저 소비하고 "빈 화면"을 눌렀을 때만 Clicked 를 발생시킨다.
/// 전역 InputAction + IsPointerOverGameObject 방식의 프레임 타이밍 경고를 피한 표준 방법이다.
///
/// 동작 조건: 같은 오브젝트에 Raycast Target 인 Graphic(예: Image, alpha 0 도 가능)이 있어야 하고,
/// 시작 버튼/패널보다 하이어라키상 "아래(뒤)"에 두어 버튼이 위에서 클릭을 먼저 받게 한다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class PressToStartArea : MonoBehaviour, IPointerClickHandler {
    public event Action Clicked;

    public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
}
