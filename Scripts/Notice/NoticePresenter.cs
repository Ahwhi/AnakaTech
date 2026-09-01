using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using VContainer;

/// <summary>
/// 공지/이벤트 표시 흐름 오케스트레이터(INoticePresenter 구현). 루트 DI 에 등록돼 전역에서 호출된다.
/// 뷰는 의존성 주입이 필요 없는 순수 표시 컴포넌트로 두고, 서비스 의존은 모두 이 프레젠터가 들고 있어 역할이 분리된다.
///
/// 공지: NoticePopup 으로 공지 글만. 이벤트: EventPopup 으로 이벤트만(상태 배지 포함).
/// 두 목록은 겹치지 않는다. 같은 글이 두 곳에 나오면 어느 쪽에서 봤는지에 따라 읽음/배지 상태가 달라 보인다.
/// 이벤트 글을 열면 읽음 처리 후 해당 항목의 배지를 갱신하고 상세 팝업을 띄운다.
/// </summary>
public sealed class NoticePresenter : INoticePresenter {
    private readonly INoticeService _notice;
    private readonly IUIService _ui;
    private readonly IEventBadgeResolver _badgeResolver;
    private readonly IEventReadStore _readStore;
    private readonly IEventStatusSync _eventStatusSync;
    private readonly IEventBadgeService _badges;
    private readonly IAssetService _asset;
    private readonly IObjectResolver _resolver;
    private readonly UIPopupCatalog _popups;

    // 이벤트 팝업을 다시 열 때 직전에 보던 이벤트를 우선 선택하기 위한 기억(앱 실행 중 유지).
    private long _lastSelectedEventId = -1;

    public NoticePresenter(INoticeService notice, IUIService ui, IEventBadgeResolver badgeResolver,
                           IEventReadStore readStore, IEventStatusSync eventStatusSync, IEventBadgeService badges,
                           IAssetService asset, IObjectResolver resolver, UIPopupCatalog popups) {
        _notice = notice;
        _ui = ui;
        _badgeResolver = badgeResolver;
        _readStore = readStore;
        _eventStatusSync = eventStatusSync;
        _badges = badges;
        _asset = asset;
        _resolver = resolver;
        _popups = popups;
    }

    public async UniTask OpenNoticesAsync(CancellationToken ct = default) {
        NoticePopup popup;
        try {
            popup = await _ui.ShowPopupAsync<NoticePopup>(_popups.NoticePopup, ct);
        } catch (OperationCanceledException) {
            return;
        }

        popup.Initialize(data => OpenDetailAsync(data).Forget());
        await popup.ShowAsync("공지사항");

        try {
            // 공지사항 목록에는 공지만 싣는다. 이벤트는 전용 팝업(EventPopup)이 배지/기간/보상까지 갖춰
            // 보여 주므로, 여기에 함께 나오면 같은 글을 두 곳에서 보게 되고 이 목록에서는 이벤트를
            // 이벤트답게 다루지도 못한다. Supabase 의 category 는 그대로 두고 조회할 때만 걸러 낸다.
            var items = await _notice.GetNoticesAsync(NoticeCategory.Notice, ct);
            popup.SetItems(items);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogError($"[Notice] 공지 조회 실패: {e.Message}");
            popup.ShowError();
        }
    }

    public async UniTask OpenEventsAsync(CancellationToken ct = default) {
        EventPopup popup;
        try {
            popup = await _ui.ShowPopupAsync<EventPopup>(_popups.EventPopup, ct);
        } catch (OperationCanceledException) {
            return;
        }

        popup.Initialize(_badgeResolver.Resolve, data => OnEventSelected(data, popup), _asset, _resolver);

        // 페이지 안에서 보상을 수령하면(충족 상태 변경) 배지 서비스가 Changed 를 발행한다.
        // 페이지는 팝업을 모르므로, 이 신호를 받아 열려 있는 목록의 배지를 즉시 다시 계산한다.
        // 구독 수명은 팝업 GameObject 에 묶어(AddTo) 닫힘/강제 종료 시 자동 해지한다.
        _badges.Changed.Subscribe(_ => popup.RefreshAllBadges()).AddTo(popup);

        await popup.ShowAsync("이벤트");

        try {
            var items = await _notice.GetNoticesAsync(NoticeCategory.Event, ct);
            popup.SetItems(items, _lastSelectedEventId);

            // 목록은 바로 보여주고, 서버 충족 상태는 받아온 뒤 배지만 갱신한다(목록 표시를 막지 않음).
            RefreshStatusThenBadgesAsync(popup, ct).Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogError($"[Event] 이벤트 조회 실패: {e.Message}");
            popup.ShowError();
        }
    }

    private async UniTaskVoid RefreshStatusThenBadgesAsync(EventPopup popup, CancellationToken ct) {
        await _eventStatusSync.RefreshAsync(ct);
        popup.RefreshAllBadges();
    }

    // 이벤트 선택(같은 팝업 안에서 상세가 교체됨): 읽음 처리 -> 아이템 배지 갱신(미열람 Unread 제거) -> 선택 기억.
    private void OnEventSelected(NoticeData data, EventPopup popup) {
        _readStore.MarkRead(data.id);
        popup.RefreshBadge(data);
        // 읽음은 로컬 변경이라 종합 배지가 자동 갱신되지 않는다. 네트워크 없이 재계산해
        // 이벤트 버튼(BadgeView Id=Events, Aggregate)의 미열람(Unread) 배지도 즉시 사라지게 한다.
        _badges.Recompute();
        _lastSelectedEventId = data.id;
    }

    private async UniTask OpenDetailAsync(NoticeData data) {
        try {
            var detail = await _ui.ShowPopupAsync<NoticeDetailPopup>(_popups.NoticeDetailPopup);
            await detail.ShowAsync(data);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogError($"[Notice] 상세 표시 실패: {e.Message}");
        }
    }
}
