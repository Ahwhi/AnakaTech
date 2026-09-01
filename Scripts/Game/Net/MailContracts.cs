using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

// 우편함 통신 계약과 순수 DTO(생성 패킷 비의존). 우편함 UI/배지는 이 타입들만 참조한다.

/// <summary>우편 보상 종류(서버 MailRewardKind 와 값 일치).</summary>
public enum MailRewardKind {
    Gold = 0,
    Diamond = 1,
    Stamina = 2,
    Character = 3,
    Currency = 4,
    UniversalShard = 5,
    Equip = 6,
    ProfileFrame = 7,     // Value=프로필 테두리 id
    CharacterShard = 8,   // Value=캐릭터 id, Qty=조각 개수
    Item = 9,             // Value=아이템 id, Qty=개수
    Engraving = 10,       // Value=각인 id(도면)
    Costume = 11,         // Value=코스튬 id(단품이라 Qty 는 쓰지 않는다)
}

public sealed class MailRewardDto {
    public MailRewardKind Kind;
    public int Value;
    /// <summary>개수. Value 가 대상 id 인 종류(캐릭터/조각/장비/아이템)에서만 의미가 있다.</summary>
    public int Qty = 1;
    public string CurrencyKey;
}

public sealed class MailDto {
    public long MailId;
    public string Title;
    public string Body;
    public DateTime CreatedAt;   // 로컬 시간(서버 UTC unix 를 변환)
    public DateTime ExpiresAt;
    public List<MailRewardDto> Rewards = new();
}

public sealed class MailClaimResultDto {
    public bool IsSuccess;
    public int FailReason;   // 0=성공,1=우편없음/이미수령/만료,5=지갑없음
    public int NewGold;
    public int NewDiamond;
    public int NewStamina;
    public List<MailRewardDto> Granted = new();
    public List<long> ClaimedMailIds = new();
}

/// <summary>우편함 통신 추상화(ShopService 와 동일한 WaitPacketAsync + Send 패턴). 미수령 수를 배지가 구독한다.</summary>
public interface IMailService {
    IReadOnlyReactiveProperty<int> UnreadCount { get; }
    UniTask<List<MailDto>> RequestMailAsync(CancellationToken ct = default);
    UniTask<MailClaimResultDto> ClaimMailAsync(long mailId, CancellationToken ct = default);
    UniTask<MailClaimResultDto> ClaimAllAsync(CancellationToken ct = default);
    /// <summary>배지 갱신용: 목록을 받아 미수령 수만 최신화한다(표시는 하지 않음).</summary>
    UniTask RefreshAsync(CancellationToken ct = default);
}
