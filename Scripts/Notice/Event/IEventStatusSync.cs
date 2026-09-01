using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 서버에서 이벤트 보상 상태를 받아와 EventStatusProvider 를 채우는 동기화기.
/// C_EventStatus 요청 -> S_EventStatus 응답을 받아 reward_key 별 상태를 갱신한다.
/// 로그인 직후, 이벤트 팝업을 열 때 등 최신화가 필요한 시점에 호출한다.
/// </summary>
public interface IEventStatusSync {
    UniTask RefreshAsync(CancellationToken ct = default);
}
