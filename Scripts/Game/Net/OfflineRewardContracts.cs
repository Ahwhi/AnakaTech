using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 오프라인 보상 통신 계약과 순수 DTO(생성 패킷 비의존). 화면과 상태는 이 타입만 참조한다.

/// <summary>
/// 오프라인 보상 정산 결과.
///
/// 시간과 지급량은 전부 서버가 정한 값이다. 클라이언트는 어떤 시각도 보내지 않고 계산도 하지 않는다.
/// 기기 시계를 바꿔도 결과가 달라지지 않는 이유가 이것이다.
/// </summary>
public sealed class OfflineRewardDto {
    /// <summary>정산 식별자. 0 이면 지급 대상이 없다(팝업을 띄우지 않는다).</summary>
    public long SettlementId;

    /// <summary>실제로 비워 둔 시간(초). 최대 누적 제한을 적용하기 전의 값이다.</summary>
    public long OfflineSeconds;

    /// <summary>보상 계산에 사용한 시간(분). 최대치에 걸리면 720 이다.</summary>
    public int RewardMinutes;

    /// <summary>최대 누적 시간(12시간)에 도달했는지. 화면에 안내 문구를 띄우는 기준이다.</summary>
    public bool Capped;

    public int Gold;
    public int Stamina;
    public int Diamond;
    /// <summary>편성원 1명당 경험치. 지갑 재화가 아니라 캐릭터에 들어간다. 투자 증가분이 이미 반영된 값이다.</summary>
    public int Exp;
    /// <summary>
    /// 위 경험치에 반영된 투자 증가분(1000 분율). 0 이면 투자 효과가 없다.
    /// 지급 시점의 투자 레벨로 서버가 굳힌 값이라, 지금 투자 레벨로 다시 계산하지 않는다.
    /// </summary>
    public int InvestExpPermille;
    /// <summary>경험치가 편성원에게 들어간 결과(레벨업/스탯포인트 포함). 이미 정산된 것을 다시 읽으면 비어 있다.</summary>
    public List<CharExpGain> CharExp = new();

    /// <summary>이번 요청으로 지급한 것이 아니라 이미 처리된 정산을 다시 읽은 것인지.</summary>
    public bool AlreadySettled;

    public int NewGold;
    public int NewStamina;
    public int NewDiamond;

    /// <summary>표시할 것이 있는지. 서버가 이미 걸러 주지만 화면이 스스로 한 번 더 확인한다.</summary>
    public bool HasReward => SettlementId > 0 && (Gold > 0 || Stamina > 0 || Diamond > 0 || Exp > 0);

    /// <summary>실제 오프라인 시간을 TimeSpan 으로(최대 누적 제한 적용 전).</summary>
    public TimeSpan OfflineDuration => TimeSpan.FromSeconds(OfflineSeconds);

    /// <summary>
    /// 보상으로 인정된 시간(최대 누적 제한 적용 후). 표시 문구는 이 값을 써야 한다.
    /// 12시간을 넘겨 비워도 12시간까지만 지급되므로, 실제 시간을 적으면 문구와 보상이 어긋난다.
    /// </summary>
    public TimeSpan RewardDuration => TimeSpan.FromMinutes(RewardMinutes);
}

/// <summary>오프라인 보상 통신 창구.</summary>
public interface IOfflineRewardService {
    /// <summary>
    /// 정산을 요청한다. 지급 대상이 없으면 SettlementId=0 인 결과가 돌아온다.
    /// 같은 오프라인 구간으로 다시 요청해도 서버가 재지급하지 않고 같은 결과를 돌려준다.
    /// </summary>
    UniTask<OfflineRewardDto> RequestAsync(CancellationToken ct = default);
}
