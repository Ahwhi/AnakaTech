using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 누적 충전 보상 통신 계약과 순수 DTO.

public sealed class ChargeRewardStatusDto {
    public int CumulativeWon;
    public HashSet<int> ClaimedTiers = new();
}

public sealed class ClaimChargeResult {
    public bool IsSuccess;
    public int FailReason;   // 0성공,1잘못된티어,2미달성,3이미수령,5지갑없음
    public int Tier;
    public int NewDiamond;
}

/// <summary>누적 충전 보상 통신 추상화.</summary>
public interface IChargeRewardService {
    UniTask<ChargeRewardStatusDto> RequestStatusAsync(CancellationToken ct = default);
    UniTask<ClaimChargeResult> ClaimAsync(int tier, CancellationToken ct = default);
}
