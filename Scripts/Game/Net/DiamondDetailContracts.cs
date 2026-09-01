using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>다이아 보유 내역(유상/무상). 합계는 PlayerStateStore.Diamond 가 이미 갖고 있으므로 여기엔 나눔 값만 둔다.</summary>
public sealed class DiamondDetailDto {
    /// <summary>현금 결제로 받아 아직 남아 있는 다이아.</summary>
    public int Paid;
    /// <summary>무료로 받아 아직 남아 있는 다이아.</summary>
    public int Free;

    /// <summary>보유 합계. 화면에 보이는 다이아 수와 같아야 한다.</summary>
    public int Total => Paid + Free;
}

/// <summary>다이아 보유 내역 조회 창구.</summary>
public interface IDiamondDetailService {
    /// <summary>
    /// 유상/무상 보유량을 조회한다. 재화 상세를 열 때만 부른다.
    /// 잔액은 서버가 권위이며, 소진은 언제나 무상부터 이뤄진다.
    /// </summary>
    UniTask<DiamondDetailDto> RequestAsync(CancellationToken ct = default);
}
