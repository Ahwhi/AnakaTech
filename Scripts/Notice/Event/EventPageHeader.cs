using TMPro;
using UnityEngine;

/// <summary>
/// 이벤트 페이지 공통 헤더. 이벤트 이름과 진행 기간(한국 시간)을 출력한다.
/// 이벤트 페이지 프리팹(EventPage_Attendance, EventPage_Missions 등)에 이 컴포넌트를 얹어 두면
/// EventDetailView 가 선택된 이벤트 데이터로 자동 바인딩하므로, 각 페이지 스크립트는 기간 표시를 신경 쓰지 않는다.
///
/// 표시 내용의 출처는 공지 DB(notices)의 title/start_at/end_at 한 곳이다.
/// 문구 규칙(종료일 9999년 = 별도 공지 시까지 등)은 EventPeriod 가 단독으로 정한다.
/// 텍스트 필드는 모두 선택이며, 비워 두면 해당 항목만 표시하지 않는다.
/// </summary>
public sealed class EventPageHeader : MonoBehaviour {
    [Tooltip("이벤트 이름(공지 DB 의 title). 비우면 표시하지 않는다.")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("진행 기간(한국 시간). 기간이 없으면 자동으로 숨긴다.")]
    [SerializeField] private TMP_Text periodText;
    [Tooltip("남은 기간 안내(선택). 종료일이 미정이면 자동으로 숨긴다.")]
    [SerializeField] private TMP_Text remainText;

    /// <summary>선택된 이벤트 데이터를 헤더에 반영한다.</summary>
    public void Bind(NoticeData data) {
        if (data == null) return;

        if (titleText != null) titleText.text = data.title;
        SetOptional(periodText, EventPeriod.Format(data));
        SetOptional(remainText, EventPeriod.FormatRemaining(data));
    }

    // 내용이 없으면 빈 줄이 레이아웃을 차지하지 않도록 오브젝트째 숨긴다.
    private static void SetOptional(TMP_Text text, string value) {
        if (text == null) return;
        bool has = !string.IsNullOrEmpty(value);
        text.gameObject.SetActive(has);
        if (has) text.text = value;
    }
}
