using System.Collections.Generic;

/// <summary>
/// 서버가 내려준 이벤트 보상 상태 캐시(IEventStatusProvider 구현). reward_key 를 키로 상태를 보관한다.
/// 서버 정보가 없는 키는 None 으로 본다(안전한 기본값). 실제 값 채움은 서버 수신부가 담당한다.
/// </summary>
public sealed class EventStatusProvider : IEventStatusProvider {
    private readonly Dictionary<string, EventClaimState> _states = new();

    public EventClaimState GetClaimState(NoticeData ev) {
        if (ev == null || !ev.HasReward) return EventClaimState.None;
        return GetClaimState(ev.rewardKey);
    }

    public EventClaimState GetClaimState(string rewardKey) {
        if (string.IsNullOrEmpty(rewardKey)) return EventClaimState.None;
        return _states.TryGetValue(rewardKey, out var state) ? state : EventClaimState.None;
    }

    public void Set(string rewardKey, EventClaimState state) {
        if (string.IsNullOrEmpty(rewardKey)) return;
        _states[rewardKey] = state;
    }

    public IEnumerable<string> ClaimableKeys {
        get {
            foreach (var kv in _states)
                if (kv.Value == EventClaimState.Claimable) yield return kv.Key;
        }
    }

    public void Clear() => _states.Clear();
}
