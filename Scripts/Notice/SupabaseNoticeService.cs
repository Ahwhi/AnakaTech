using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Supabase REST(PostgREST) 기반 공지/이벤트 조회 구현.
/// notices 테이블을 HTTP GET 으로 읽으며, RLS 정책상 게시된 글만 anon 키로 접근 가능하다.
///
/// select 절에서 DB 의 snake_case 컬럼을 camelCase 별칭으로 받아(NoticeData 필드명과 일치),
/// 별도 JSON 매핑 라이브러리 없이 Unity JsonUtility 로 역직렬화한다.
/// </summary>
public sealed class SupabaseNoticeService : INoticeService {
    // DB 컬럼 -> 클라이언트 필드명(camelCase) 별칭. 컬럼 추가 시 여기와 NoticeData 만 맞추면 된다.
    private const string SelectClause =
        "id,category,title,content,author,imageUrl:image_url,isPinned:is_pinned," +
        "createdAt:created_at,startAt:start_at,endAt:end_at,rewardKey:reward_key,eventKey:event_key";

    private readonly SupabaseConfig _config;

    public SupabaseNoticeService(SupabaseConfig config) {
        _config = config;
    }

    public async UniTask<IReadOnlyList<NoticeData>> GetNoticesAsync(NoticeCategory? category = null, CancellationToken ct = default) {
        if (_config == null || string.IsNullOrEmpty(_config.ProjectUrl) || string.IsNullOrEmpty(_config.AnonKey))
            throw new InvalidOperationException("SupabaseConfig 가 비어 있습니다. ProjectUrl/AnonKey 를 인스펙터에서 설정하세요.");

        var url = BuildUrl(category);

        using var request = UnityWebRequest.Get(url);
        request.SetRequestHeader("apikey", _config.AnonKey);
        request.SetRequestHeader("Authorization", "Bearer " + _config.AnonKey);
        request.SetRequestHeader("Accept", "application/json");

        await request.SendWebRequest().WithCancellation(ct);

        if (request.result != UnityWebRequest.Result.Success)
            throw new Exception($"공지 요청 실패({request.responseCode}): {request.error}");

        return NoticeData.ParseList(request.downloadHandler.text);
    }

    private string BuildUrl(NoticeCategory? category) {
        // 게시된 글만, 상단 고정 우선 + 최신순. is_published 필터는 RLS 와 중복이지만 의도를 명시해 둔다.
        var url = $"{_config.ProjectUrl}/rest/v1/notices" +
                  $"?select={SelectClause}" +
                  "&is_published=eq.true" +
                  "&order=is_pinned.desc,created_at.desc";

        if (category.HasValue)
            url += "&category=eq." + (category.Value == NoticeCategory.Event ? "event" : "notice");

        return url;
    }
}
