using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 분류 탭 버튼 공용 컴포넌트. 클릭은 콜백으로 넘기고 선택 표시만 스스로 바꾸는 순수 표시 컴포넌트라,
/// 탭이 필요한 어느 화면에서든 그대로 재사용한다(탭의 의미/개수/전환 규칙은 쓰는 쪽이 정한다).
///
/// label / selectedIndicator 는 선택 항목이며, 비워 두면 해당 표시만 생략한다.
/// button 을 비우면 같은 오브젝트의 Button 을 자동으로 쓴다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class UITabButton : MonoBehaviour {
    [Tooltip("클릭을 받을 버튼(비우면 같은 오브젝트의 Button).")]
    [SerializeField] private Button button;
    [Tooltip("탭 이름 텍스트(선택). 이름을 코드에서 넣지 않으면 여기 적어 둔 문구가 그대로 남는다.")]
    [SerializeField] private TMP_Text label;
    [Tooltip("선택된 탭에만 켜지는 강조 표시(선택).")]
    [SerializeField] private GameObject selectedIndicator;
    [Tooltip("선택 상태의 탭 이름 색(label 이 있을 때만 적용).")]
    [SerializeField] private Color selectedLabelColor = Color.white;
    [Tooltip("비선택 상태의 탭 이름 색(label 이 있을 때만 적용).")]
    [SerializeField] private Color deselectedLabelColor = new Color(0.65f, 0.65f, 0.65f, 1f);

    private Button Target => button != null ? button : (button = GetComponent<Button>());

    /// <summary>클릭 콜백을 연결한다(중복 연결 방지). display 가 비어 있으면 인스펙터에 적어 둔 이름을 유지한다.</summary>
    public void Bind(string display, Action onClick) {
        if (label != null && !string.IsNullOrEmpty(display)) label.text = display;

        var target = Target;
        if (target == null) return;
        target.onClick.RemoveAllListeners();
        target.onClick.AddListener(() => onClick?.Invoke());
    }

    /// <summary>선택 상태 표시를 갱신한다.</summary>
    public void SetSelected(bool selected) {
        if (selectedIndicator != null) selectedIndicator.SetActive(selected);
        if (label != null) label.color = selected ? selectedLabelColor : deselectedLabelColor;
    }
}
