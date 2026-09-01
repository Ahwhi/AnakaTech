using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이벤트 목록의 한 줄. 제목/작성일과 함께 상태 배지(Unread=NEW 미열람, Claimable=빨간 동그라미 충족)를 보여준다.
/// 배지는 None 이면 숨긴다. 읽음 처리 후 배지만 갱신할 수 있도록 SetBadge 를 별도로 노출한다.
/// </summary>
public sealed class EventItemView : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI titleText;
    [Tooltip("상단 고정 표식(선택).")]
    [SerializeField] private GameObject pinnedBadge;
    [Tooltip("현재 선택된 항목(탭) 강조 표시(선택).")]
    [SerializeField] private GameObject selectedMark;

    [Header("Status Badge")]
    [Tooltip("상태 배지(BadgeView, Source=Manual). 표시는 BadgeCatalog(SO)가 결정한다. 없으면 배지 미사용.")]
    [SerializeField] private BadgeView badge;

    [SerializeField] private Button button;

    /// <summary>데이터와 배지를 바인딩하고 클릭 콜백을 연결한다.</summary>
    public void Bind(NoticeData data, EventBadge state, Action<NoticeData> onClick) {
        if (titleText != null) titleText.text = data.title;
        if (pinnedBadge != null) pinnedBadge.SetActive(data.isPinned);

        SetBadge(state);
        SetSelected(false);

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(data));
        }
    }

    /// <summary>선택(탭 강조) 표시를 켜고 끈다.</summary>
    public void SetSelected(bool selected) {
        if (selectedMark != null) selectedMark.SetActive(selected);
    }

    /// <summary>배지만 갱신한다(예: 읽은 직후 미열람 Unread 제거).</summary>
    public void SetBadge(EventBadge state) {
        if (badge != null) badge.SetState(state);
    }
}
