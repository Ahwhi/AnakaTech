using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>무한 타워 계정 현황. 지금 오를 층과 그 층의 보너스, 오늘 오를 수 있는지.</summary>
public sealed class TowerInfoDto {
    /// <summary>지금 오를 층(1부터). MaxFloor 를 넘으면 끝까지 오른 것이다.</summary>
    public int Floor;
    public int MaxFloor;
    public bool CanClimb;
    public TowerBonus Bonus;

    public bool IsTopReached => Floor > MaxFloor;
}

/// <summary>한 판 입장 응답(횟수를 쓰지 않는다).</summary>
public sealed class TowerEnterDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=오늘 이미 등반,2=계정없음,3=층 불일치,4=최고층 도달</summary>
    public int FailReason;
}

/// <summary>등반 성공 보고에 대한 응답. 받은 보상과 다음 층의 현황을 함께 담는다.</summary>
public sealed class TowerClearDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=오늘 이미 등반,2=계정없음,3=층 불일치,4=입장 기록 없음 또는 전투 시간 부족</summary>
    public int FailReason;
    public int ClearedFloor;
    public TowerInfoDto Next;
    /// <summary>수령 후 다이아 잔액. 서버가 확정한 값이므로 화면이 다시 더하지 않는다.</summary>
    public int NewDiamond;
    public List<RewardItem> Granted = new();
}

public interface ITowerService {
    /// <summary>오늘 오를 수 있는지. 배지가 이 값을 구독하므로 등반하는 순간 불이 꺼진다.</summary>
    IReadOnlyReactiveProperty<bool> CanClimbToday { get; }

    UniTask<TowerInfoDto> RequestInfoAsync(CancellationToken ct = default);

    /// <summary>배지 갱신용 가벼운 조회(실패해도 예외를 밖으로 내지 않는다).</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>한 판 입장. 실패해도 다시 칠 때마다 부른다(등반 보고의 전투 시간 판정 기준이 된다).</summary>
    UniTask<TowerEnterDto> EnterAsync(int floor, CancellationToken ct = default);

    /// <summary>등반 성공 보고. 보상을 받고 다음 층으로 오른다.</summary>
    UniTask<TowerClearDto> ClearAsync(int floor, CancellationToken ct = default);

    /// <summary>한 판을 끝까지 치렀다는 단방향 보고(성공/실패 무관). 나가기/재도전 경로에서는 부르지 않는다.</summary>
    void ReportFinished();
}
