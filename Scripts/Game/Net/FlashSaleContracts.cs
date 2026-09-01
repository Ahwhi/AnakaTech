using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

// 한정 판매(플래시 세일) 통신 계약과 순수 DTO(생성 패킷 비의존). 화면은 이 타입들만 참조한다.

/// <summary>
/// 지금 이 계정에 열려 있는 한정 판매 1건.
///
/// 상품 내용(이름/가격/구성품)은 담지 않는다. 그것은 상점 카탈로그가 이미 갖고 있고, 두 곳에 두면
/// 한쪽만 바뀌었을 때 화면과 결제가 다른 상품을 말하게 된다. 여기에는 "무엇이 언제까지 열려 있는가"만 있다.
/// </summary>
public sealed class FlashSaleDto {
    /// <summary>상점 오퍼 id. 이 id 로 상점 카탈로그에서 상품 정보를 찾는다.</summary>
    public string OfferId = "";
    public long StartUnix;
    /// <summary>종료 시각(unix). 남은 시간 카운트다운의 기준이다.</summary>
    public long EndUnix;

    /// <summary>
    /// 이미 구매 한도를 채워 더 살 수 없는 상품인지(서버 권위).
    /// 한정 판매는 기간 제한과 구매 제한이 함께 걸려 있어 "열려 있다"와 "살 수 있다"가 다르다.
    /// </summary>
    public bool SoldOut;

    /// <summary>지금 기준 남은 시간. 이미 끝났으면 0.</summary>
    public TimeSpan Remaining {
        get {
            long left = EndUnix - KstTime.NowUnix;
            return left > 0 ? TimeSpan.FromSeconds(left) : TimeSpan.Zero;
        }
    }

    public bool IsOpen => Remaining > TimeSpan.Zero;

    /// <summary>지금 실제로 살 수 있는지(열려 있고 아직 한도가 남음).</summary>
    public bool IsPurchasable => IsOpen && !SoldOut;
}

/// <summary>
/// 한정 판매 통신 창구.
///
/// 발동은 서버가 정한다(스테이지 클리어 등). 클라는 그 결과만 받아 배지 숫자와 안내 팝업에 쓴다.
/// 새 발동 조건이 생겨도 이 계약은 그대로다 - 어떤 컨텐츠가 열었는지는 클라가 알 필요가 없기 때문이다.
/// </summary>
public interface IFlashSaleService {
    /// <summary>
    /// 지금 열려 있는 한정 판매 목록. 이미 구매한 상품도 남는다(안내 팝업이 구매완료 표시와 남은 시간을 보여 준다).
    /// 만료된 것은 목록에서 스스로 빠진다.
    /// </summary>
    IReadOnlyReactiveProperty<IReadOnlyList<FlashSaleDto>> Active { get; }

    /// <summary>
    /// 지금 살 수 있는 상품 수(열려 있고 아직 구매하지 않은 것). 메인 화면 버튼과 그 배지 숫자가 이 값을 구독한다.
    /// 다 사면 0 이 되어 버튼이 사라진다 - 눌러도 살 것이 없는 버튼을 남겨 두지 않기 위함이다.
    /// </summary>
    IReadOnlyReactiveProperty<int> PurchasableCount { get; }

    /// <summary>
    /// 방금 새로 열린 한정 판매(서버 알림). 안내 팝업을 띄우는 신호다.
    /// 이미 열려 있던 것은 흐르지 않으므로, 같은 스테이지를 다시 깨도 팝업이 다시 뜨지 않는다.
    /// </summary>
    IObservable<IReadOnlyList<FlashSaleDto>> Opened { get; }

    /// <summary>현재 목록을 서버에서 다시 받아 온다(메인 진입 시 1회). 실패해도 조용히 넘어간다.</summary>
    UniTask RefreshAsync(CancellationToken ct = default);
}
