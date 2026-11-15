using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 도감 보상(수집/별레벨) 통신 계약과 순수 DTO. 보상 팝업은 이 타입들만 참조한다.

public sealed class EncyclopediaRewardStatusDto {
    public int CollectionCount;
    public int TotalStar;
    public HashSet<int> ClaimedCollectionSteps = new();
    public HashSet<int> ClaimedStarSteps = new();
}

public sealed class ClaimEncyclopediaResult {
    public bool IsSuccess;
    public int FailReason;   // 0성공,1잘못된단계,2미달성,3이미수령,5지갑없음
    public int RewardType;   // 0=수집,1=별레벨
    public int Step;
    public int NewDiamond;
    public int GrantedGrade; // 별레벨 보상 시 만능조각 등급(수집이면 -1)
}

/// <summary>도감 보상 통신 추상화.</summary>
public interface IEncyclopediaService {
    UniTask<EncyclopediaRewardStatusDto> RequestRewardsAsync(CancellationToken ct = default);
    UniTask<ClaimEncyclopediaResult> ClaimAsync(int rewardType, int step, CancellationToken ct = default);

    /// <summary>수령 현황을 받을 때마다 흐른다(누가 요청했든).</summary>
    IObservable<EncyclopediaRewardStatusDto> StatusReceived { get; }

    /// <summary>
    /// 보상 수령에 성공할 때마다 흐른다. 수령 응답이 곧 최신 상태이므로 배지는 이 값으로 바로 꺼지고,
    /// 현황 전체를 다시 조회할 필요가 없다.
    /// </summary>
    IObservable<ClaimEncyclopediaResult> Claimed { get; }
}
