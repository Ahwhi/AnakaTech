using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미션 목록 탭 버튼(일일/누적 등). 위치 고정 + 선택 표시 + 수령가능 배지(ShopSubTab 과 같은 표시 규칙).
/// 순수 표시 컴포넌트로, 클릭은 콜백으로 페이지에 위임하고 선택/배지 상태는 페이지가 넣어준다(디커플링).
/// selectedIndicator / badge / label 은 모두 선택이며, 비워 두면 해당 표시만 생략한다.
/// </summary>
public sealed class MissionTabButton : MonoBehaviour {
    [SerializeField] private Button button;
    [Tooltip("탭 이름(선택).")]
    [SerializeField] private TMP_Text label;
    [Tooltip("선택된 탭에만 켜지는 강조 표시(선택).")]
    [SerializeField] private GameObject selectedIndicator;
    [Tooltip("수령 가능한 미션이 있을 때 켜지는 배지(BadgeView, Source=Manual). 표시는 BadgeCatalog(SO) 공용값을 쓴다.")]
    [SerializeField] private BadgeView badge;

    /// <summary>클릭 콜백을 연결한다(중복 연결 방지).</summary>
    public void Init(Action onClick) {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());
    }

    /// <summary>선택 상태 표시를 켜고 끈다.</summary>
    public void SetSelected(bool selected) {
        if (selectedIndicator != null) selectedIndicator.SetActive(selected);
    }

    /// <summary>수령 가능 배지를 켜고 끈다. 표시는 공용 카탈로그(빨간 동그라미)로 통일한다.</summary>
    public void SetBadge(bool on) {
        if (badge != null) badge.SetOn(on);
    }
}
