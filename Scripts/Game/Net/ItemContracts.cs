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
    public int FailReason;       // ItemRules.UseFailMessage 참고
    public int ItemId;
    public int NewCount;
    public int CharDataId;
    /// <summary>경험치를 주는 아이템만 값이 채워진다(아니면 전부 0).</summary>
    public CharExpGain CharExp;
    /// <summary>대상 캐릭터의 사용 후 상태 전체(대상이 있는 아이템만, 아니면 null). 레벨을 되돌리는 아이템도 이것으로 반영된다.</summary>
    public CharacterProgress Updated;
    /// <summary>실제로 들어간 경험치(경험치를 주지 않는 아이템은 0). 큰 단위 캡슐은 int 를 넘을 수 있다.</summary>
    public long GainedExp;
    /// <summary>경험치 추출/성장 초기화로 뽑아낸 양(아니면 0). 고레벨은 int 를 넘는다.</summary>
    public long ExtractedExp;
    /// <summary>사용 결과로 새로 받은 아이템과 "받은 뒤 보유 개수"(경험치 캡슐 큰 단위/일반 등).</summary>
    public List<ItemCountDto> GrantedItems = new();
    /// <summary>성장 초기화권으로 돌려받은 것(다른 아이템은 전부 0). 경험치는 ExtractedExp(캡슐)로 온다.</summary>
    public GrowthRefund Refund;
    /// <summary>돌려받은 뒤 보유량. 해당 환급이 있을 때만 의미가 있다.</summary>
    public int NewDiamond;
    public int NewLimitBreakTicket;
}

/// <summary>성장 초기화로 돌려받는(받을) 조각/다이아/한계돌파권. 경험치는 따로 다룬다.</summary>
public struct GrowthRefund {
    public int Shards;
    public int Diamond;
    public int LimitBreakTicket;
}

/// <summary>아이템 1개를 쓰면 나올 수치의 서버 미리보기.</summary>
public sealed class ItemUsePreview {
    public int FailReason;
    /// <summary>계열별 수치(경험치 추출권/성장 초기화권 = 추출될 경험치, 경험치 캡슐 = 들어갈 경험치). 미리 볼 수치가 없는 계열은 0.</summary>
    public long Value;
    /// <summary>사용하면 받게 될 아이템과 개수(경험치 캡슐 큰 단위/일반 등). 보유량이 아니라 받는 양이다.</summary>
    public List<ItemCountDto> GrantItems = new();
    /// <summary>대상 캐릭터의 지금 레벨과 사용 후 레벨.</summary>
    public int CurrentLevel;
    public int ResultLevel;
    /// <summary>대상 캐릭터의 지금 별레벨.</summary>
    public int CurrentStar;
    /// <summary>성장 초기화권 = 돌려받을 것(다른 계열은 0).</summary>
    public GrowthRefund Refund;
}

/// <summary>소모품 아이템 서버 통신 추상화. 지급은 다른 보상 경로로 들어오고 여기서는 조회/사용만 한다.</summary>
public interface IItemService {
    UniTask<ItemInfoDto> RequestInfoAsync(CancellationToken ct = default);

    /// <param name="charDataId">대상 캐릭터(대상이 필요 없는 아이템은 0).</param>
    UniTask<ItemUseResult> UseAsync(int itemId, int charDataId, int count, CancellationToken ct = default);

    UniTask<ItemUsePreview> PreviewAsync(int itemId, int charDataId, CancellationToken ct = default);
}
