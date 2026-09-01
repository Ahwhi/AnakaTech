using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>
/// 이벤트 배지 집계 구현. 충족 상태를 서버에서 최신화한 뒤 이벤트 목록을 가져와 각 배지를 계산하고,
/// 종합값과 reward_key별 값을 캐시한다. 동시 호출은 진행 중 작업에 합류시켜 중복 요청을 막는다.
/// </summary>
public sealed class EventBadgeService : IEventBadgeService {
    private readonly INoticeService _notice;
    private readonly IEventStatusSync _statusSync;
    private readonly IEventBadgeResolver _resolver;
    private readonly IEventStatusProvider _statusProvider;

    private readonly Subject<Unit> _changed = new();
    private readonly Dictionary<string, EventBadge> _byRewardKey = new();
    private IReadOnlyList<NoticeData> _lastEvents;   // 마지막으로 받아온 이벤트 목록(로컬 재계산용 캐시)
    private bool _refreshing;

    public EventBadgeService(INoticeService notice, IEventStatusSync statusSync,
                             IEventBadgeResolver resolver, IEventStatusProvider statusProvider) {
        _notice = notice;
        _statusSync = statusSync;
        _resolver = resolver;
        _statusProvider = statusProvider;
    }

    public EventBadge Aggregate { get; private set; } = EventBadge.None;
    public IObservable<Unit> Changed => _changed;

    public EventBadge GetRewardBadge(string rewardKey) {
        if (string.IsNullOrEmpty(rewardKey)) return EventBadge.None;
        if (_byRewardKey.TryGetValue(rewardKey, out var badge)) return badge;
        // 콘텐츠(Supabase) 이벤트가 없어도 서버 충족 상태만으로 Claimable(빨간 동그라미)을 켤 수 있게 폴백.
        return _statusProvider.GetClaimState(rewardKey) == EventClaimState.Claimable
            ? EventBadge.Claimable
            : EventBadge.None;
    }

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        // 이미 진행 중이면 즉시 리턴한다. 완료되면 Changed 로 모든 구독 버튼이 갱신되므로,
        // 호출자가 굳이 진행 중 작업을 함께 기다릴 필요가 없다(여러 버튼이 동시에 호출해도 1회만 수행).
        if (_refreshing) return;

        _refreshing = true;
        try {
            await RefreshInternalAsync(ct);
        } finally {
            _refreshing = false;
        }
    }

    public async UniTask RefreshClaimStatesAsync(CancellationToken ct = default) {
        if (_refreshing) return;

        // 공지 목록을 아직 못 받았으면 가벼운 갱신만으로는 배지를 만들 수 없다(전체 갱신으로 돌린다).
        if (_lastEvents == null) {
            await RefreshAsync(ct);
            return;
        }

        _refreshing = true;
        try {
            await _statusSync.RefreshAsync(ct);
            Recompute();
        } catch (OperationCanceledException) {
        } finally {
            _refreshing = false;
        }
    }

    // 로컬 변경(예: 이벤트 읽음)으로 종합/보상 배지를 다시 계산한다. 네트워크 호출 없이 마지막 목록으로만 계산한다.
    // 충족 상태(서버)는 그대로 두고 열람 등 로컬 상태만 바뀐 경우 즉시 버튼 배지를 갱신할 때 쓴다.
    public void Recompute() {
        _byRewardKey.Clear();
        var aggregate = EventBadge.None;

        if (_lastEvents != null)
            foreach (var ev in _lastEvents) {
                var badge = _resolver.Resolve(ev);
                if (ev.HasReward) _byRewardKey[ev.rewardKey] = badge;
                aggregate = Higher(aggregate, badge);
            }

        // 공지(Supabase) 게시글이 없는 이벤트도 받을 것이 있으면 종합 배지를 켠다.
        // 이벤트 목록만 훑으면 게시글을 올리지 않은 이벤트는 메인 버튼에 영영 표시되지 않는다
        // (GetRewardBadge 에는 같은 폴백이 이미 있어, 이벤트 팝업에서만 배지가 보이는 어긋남이 생겼다).
        foreach (var key in _statusProvider.ClaimableKeys) {
            if (!_byRewardKey.ContainsKey(key)) _byRewardKey[key] = EventBadge.Claimable;
            aggregate = Higher(aggregate, EventBadge.Claimable);
        }

        Aggregate = aggregate;
        _changed.OnNext(Unit.Default);
    }

    private async UniTask RefreshInternalAsync(CancellationToken ct) {
        // 1) 서버 충족 상태 최신화.
        await _statusSync.RefreshAsync(ct);

        // 2) 이벤트 목록을 가져와 캐시한 뒤 배지를 계산. 실패 시 기존 값을 유지한다.
        try {
            _lastEvents = await _notice.GetNoticesAsync(NoticeCategory.Event, ct);
        } catch (OperationCanceledException) {
            return;
        } catch {
            return;
        }

        Recompute();
    }

    // 우선순위: Claimable(빨간 동그라미) > Unread(NEW) > None. enum 값 순서(None<Unread<Claimable)와 일치하므로 큰 값 우선.
    private static EventBadge Higher(EventBadge a, EventBadge b)
        => (EventBadge)Math.Max((int)a, (int)b);
}
