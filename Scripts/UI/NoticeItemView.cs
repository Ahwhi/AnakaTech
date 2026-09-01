using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 목록의 게시글 1줄(요약). 제목/종류/작성일과 상단 고정 표시를 보여주고, 클릭하면 콜백으로 알린다.
/// 본문/이미지 같은 상세는 보여주지 않으며, 선택 시 프레젠터가 상세 팝업을 띄운다.
/// </summary>
public sealed class NoticeItemView : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI dateText;
    [Tooltip("상단 고정 글에만 켜지는 표식(선택).")]
    [SerializeField] private GameObject pinnedBadge;
    [SerializeField] private Button button;

    /// <summary>데이터를 바인딩하고 클릭 콜백을 연결한다.</summary>
    public void Bind(NoticeData data, Action<NoticeData> onClick) {
        if (categoryText != null)
            categoryText.text = data.Category == NoticeCategory.Event ? "이벤트" : "공지";
        if (titleText != null) titleText.text = data.title;
        if (dateText != null) dateText.text = data.CreatedAtKst.ToString("yyyy.MM.dd");
        if (pinnedBadge != null) pinnedBadge.SetActive(data.isPinned);

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(data));
        }
    }
}
