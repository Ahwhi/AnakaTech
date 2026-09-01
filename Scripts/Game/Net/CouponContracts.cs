using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 쿠폰 통신 계약과 순수 DTO(생성 패킷 비의존). 쿠폰 팝업은 이 타입들만 참조한다.

/// <summary>
/// 쿠폰 사용 결과. 보상은 우편으로 나가므로 Granted 는 "무엇을 받았는지" 보여 주기 위한 목록이며
/// 재화 잔액은 담기지 않는다(우편을 수령할 때 기존 경로로 반영된다).
/// </summary>
public sealed class CouponRedeemResultDto {
    public bool IsSuccess;
    public int FailReason;
    /// <summary>서버가 표준형으로 다듬은 코드.</summary>
    public string Code = "";
    public List<RewardItem> Granted = new();
}

/// <summary>쿠폰 통신 추상화. UI 는 이 계약에만 의존한다.</summary>
public interface ICouponService {
    UniTask<CouponRedeemResultDto> RedeemAsync(string code, CancellationToken ct = default);
}
