using System.Collections.Generic;

/// <summary>
/// 이벤트 보상 충족/수령 상태. 보상과 직결되므로 클라이언트가 판단하지 않고 서버가 정한 값만 담는다.
///   None      : 충족 대상 아님 또는 미충족
///   Claimable : 충족(받을 수 있음) - 빨간 동그라미 배지
///   Claimed   : 이미 수령 - 배지 없음
/// </summary>
public enum EventClaimState {
    None,
    Claimable,
    Claimed
}

/// <summary>
/// 이벤트별 보상 상태 제공자. 서버가 내려준 상태(reward_key 단위)를 캐시해 두고 조회만 제공한다.
/// 클라이언트는 이 값을 표시에만 쓰고 절대 스스로 충족 여부를 계산하지 않는다(조작 방지).
/// 서버 수신부(패킷 핸들러)가 Set/Clear 로 갱신하고, 배지 결정부가 GetClaimState 로 읽는다.
/// </summary>
public interface IEventStatusProvider {
    /// <summary>이벤트의 현재 보상 상태. reward_key 가 없거나 서버 정보가 없으면 None.</summary>
    EventClaimState GetClaimState(NoticeData ev);

    /// <summary>보상 키로 직접 조회(콘텐츠 이벤트 없이 전용 버튼만 있는 경우 등). 정보 없으면 None.</summary>
    EventClaimState GetClaimState(string rewardKey);

    /// <summary>서버 수신 값 반영(보상 키 단위).</summary>
    void Set(string rewardKey, EventClaimState state);

    /// <summary>
    /// 지금 수령 가능한 보상 키 전부. 콘텐츠(공지) 이벤트가 없는 이벤트까지 종합 배지에 반영하기 위해 필요하다
    /// (공지글을 올리지 않은 이벤트도 받을 게 있으면 메인 버튼에 표시되어야 한다).
    /// </summary>
    IEnumerable<string> ClaimableKeys { get; }

    /// <summary>캐시 비우기(재로그인/갱신 전 등).</summary>
    void Clear();
}
