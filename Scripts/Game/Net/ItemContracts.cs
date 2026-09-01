using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>보유 소모품 1종(아이템 id -> 개수).</summary>
public struct ItemCountDto {
    public int ItemId;
    public int Count;
}

public sealed class ItemInfoDto {
    public List<ItemCountDto> Items = new();
}

public sealed class ItemUseResult {
    public bool IsSuccess;
    public int FailReason;       // 0=성공,1=인증전/잘못된아이템,2=수량부족,3=대상캐릭터없음,4=사용할수없는아이템
    public int ItemId;
    public int NewCount;
    public int CharDataId;
    /// <summary>경험치 쿠폰처럼 캐릭터를 바꾸는 아이템만 값이 채워진다(아니면 전부 0).</summary>
    public CharExpGain CharExp;
}

/// <summary>소모품 아이템 서버 통신 추상화. 지급은 다른 보상 경로로 들어오고 여기서는 조회/사용만 한다.</summary>
public interface IItemService {
    UniTask<ItemInfoDto> RequestInfoAsync(CancellationToken ct = default);

    /// <param name="charDataId">대상 캐릭터(대상이 필요 없는 아이템은 0).</param>
    UniTask<ItemUseResult> UseAsync(int itemId, int charDataId, int count, CancellationToken ct = default);
}
