using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>영웅전 계정 현황. 지금 도전할 단계와 오늘 남은 도전 횟수.</summary>
public sealed class HeroesInfoDto {
    /// <summary>지금 도전할 단계(1부터). MaxStage 를 넘으면 끝까지 깬 것이다.</summary>
    public int Stage;
    public int MaxStage;
    public int RemainEntries;
    public int DailyEntries;

    public bool IsMaxReached => Stage > MaxStage;
    public bool CanEnter => !IsMaxReached && RemainEntries > 0;
}

/// <summary>한 판 입장 응답. 성공하면 오늘 도전 횟수를 1회 썼다.</summary>
public sealed class HeroesEnterDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=오늘 도전 횟수 소진,2=계정없음,3=단계 불일치,4=최대 단계 도달</summary>
    public int FailReason;
    public int RemainEntries;
}

/// <summary>클리어 보고에 대한 응답. 받은 보상과 다음 단계의 현황을 함께 담는다.</summary>
public sealed class HeroesClearDto {
    public bool IsSuccess;
    /// <summary>0=성공,2=계정없음,3=단계 불일치,4=입장 기록 없음 또는 전투 시간 부족</summary>
    public int FailReason;
    public int ClearedStage;
    public HeroesInfoDto Next;
    /// <summary>수령 후 잔액. 서버가 확정한 값이므로 화면이 다시 더하지 않는다.</summary>
    public int NewGold;
    public int NewDiamond;
    public List<RewardItem> Granted = new();
}

public interface IHeroesService {
    /// <summary>오늘 도전할 수 있는지(남은 횟수가 있고 끝까지 깨지 않았다). 배지가 이 값을 구독한다.</summary>
    IReadOnlyReactiveProperty<bool> CanEnterToday { get; }

    UniTask<HeroesInfoDto> RequestInfoAsync(CancellationToken ct = default);

    /// <summary>배지 갱신용 가벼운 조회(실패해도 예외를 밖으로 내지 않는다).</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>한 판 입장. 성공하면 도전 횟수를 1회 쓴다(클리어 보고의 전투 시간 판정 기준이 된다).</summary>
    UniTask<HeroesEnterDto> EnterAsync(int stage, CancellationToken ct = default);

    /// <summary>클리어 보고. 보상을 받고 다음 단계로 오른다.</summary>
    UniTask<HeroesClearDto> ClearAsync(int stage, CancellationToken ct = default);
}
