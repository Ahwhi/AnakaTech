using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 버튼 알림등(Unread=NEW / Claimable=빨간 동그라미)을 위한 이벤트 배지 집계기. 이벤트 목록과 서버 충족 상태를 가져와
/// 각 이벤트의 배지를 계산하고, 버튼이 구독할 수 있도록 제공한다.
///   - Aggregate     : 모든 이벤트 종합(하나라도 Claimable 이면 Claimable, 아니면 Unread, 둘 다 없으면 없음)
///   - GetRewardBadge: 특정 보상 이벤트(reward_key) 1건의 배지(전용 버튼용, 예: 출석체크)
///   - Changed       : 재계산될 때 통지(버튼이 다시 읽어 갱신)
///   - RefreshAsync  : 서버 충족 상태까지 최신화 후 재계산(네트워크)
///   - Recompute     : 로컬 변경(이벤트 읽음 등)만 반영해 네트워크 없이 즉시 재계산
/// 새 전용 버튼은 reward_key 만 지정하면 같은 시스템으로 불이 켜진다(확장 포인트).
/// </summary>
public interface IEventBadgeService {
    EventBadge Aggregate { get; }
    EventBadge GetRewardBadge(string rewardKey);
    IObservable<UniRx.Unit> Changed { get; }
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// 서버 충족 상태만 다시 받아 배지를 계산한다(공지 목록은 마지막으로 받아둔 것을 그대로 쓴다).
    /// 게임 플레이로 미션이 달성되는 순간마다 부르는 용도라, 매번 공지 HTTP 를 다시 치지 않도록 분리했다.
    /// 공지 목록을 아직 한 번도 받지 못했다면 전체 갱신으로 대체된다.
    /// </summary>
    UniTask RefreshClaimStatesAsync(CancellationToken ct = default);

    void Recompute();
}
