using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 게시글 종류. DB notices 테이블의 category 컬럼('notice'/'event')과 매핑된다.
/// 화면이 데이터를 요청할 때 필터로도 쓰며(null=전체), 나중에 종류가 늘어나면 여기만 확장한다.
/// </summary>
public enum NoticeCategory {
    Notice,
    Event
}

/// <summary>
/// 공지/이벤트 게시글 1건의 데이터 모델(Supabase REST 응답 1행).
/// 서버(PostgREST)에서 select 별칭으로 camelCase 키를 받아 Unity JsonUtility 로 역직렬화한다.
/// 직렬화 대상 필드는 public 이어야 하므로 그대로 두고, 가공값은 읽기 전용 프로퍼티로 노출한다.
/// 날짜는 JsonUtility 가 DateTime 을 못 다루므로 문자열(ISO 8601)로 받고 프로퍼티에서 파싱한다.
/// </summary>
[Serializable]
public sealed class NoticeData {
    public long id;
    public string category;
    public string title;
    public string content;
    public string author;
    public string imageUrl;
    public string createdAt;
    public string startAt;
    public string endAt;
    public bool isPinned;
    public string rewardKey;
    public string eventKey;

    /// <summary>문자열 category 를 enum 으로. 알 수 없는 값은 공지로 간주한다.</summary>
    public NoticeCategory Category => category == "event" ? NoticeCategory.Event : NoticeCategory.Notice;

    /// <summary>표시할 이미지가 있는지.</summary>
    public bool HasImage => !string.IsNullOrEmpty(imageUrl);

    /// <summary>이벤트 진행 기간이 설정돼 있는지(시작/종료 모두 존재).</summary>
    public bool HasPeriod => !string.IsNullOrEmpty(startAt) && !string.IsNullOrEmpty(endAt);

    /// <summary>서버 보상과 연결된 이벤트인지(reward_key 보유). 없으면 충족(초록) 배지 대상이 아님.</summary>
    public bool HasReward => !string.IsNullOrEmpty(rewardKey);

    /// <summary>전용 페이지 프리팹과 연결된 이벤트인지(event_key 보유). 카탈로그에서 이 키로 프리팹을 찾는다.</summary>
    public bool HasEventPage => !string.IsNullOrEmpty(eventKey);

    /// <summary>작성일(한국 시간, KST). 기기 타임존과 무관하게 항상 KST 로 표시한다.</summary>
    public DateTime CreatedAtKst => ParseUtcToKst(createdAt);
    public DateTime StartAtKst => ParseUtcToKst(startAt);
    public DateTime EndAtKst => ParseUtcToKst(endAt);

    // 서버 시간(UTC)을 한국 시간(KST = UTC+9, 서머타임 없음)으로 고정 변환한다.
    // 기기 타임존에 의존하지 않도록 UTC 로 정규화한 뒤 9시간을 더한다.
    // (오프셋이 없는 문자열은 UTC 로 간주: Supabase timestamptz 는 UTC 저장)
    private static DateTime ParseUtcToKst(string iso) {
        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            return dt.AddHours(9);
        return DateTime.MinValue;
    }

    /// <summary>
    /// REST 가 돌려주는 JSON 배열("[{...},{...}]")을 모델 목록으로 변환한다.
    /// JsonUtility 는 최상위 배열을 직접 못 읽으므로 객체로 감싸서 파싱한다.
    /// </summary>
    public static IReadOnlyList<NoticeData> ParseList(string json) {
        if (string.IsNullOrEmpty(json)) return Array.Empty<NoticeData>();
        var wrapped = "{\"items\":" + json + "}";
        var parsed = JsonUtility.FromJson<Wrapper>(wrapped);
        return parsed?.items ?? Array.Empty<NoticeData>();
    }

    [Serializable]
    private sealed class Wrapper {
        public NoticeData[] items;
    }
}
