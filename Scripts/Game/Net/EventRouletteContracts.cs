using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>이벤트 돌림판 정보. 칸 목록은 서버가 정한 순서 그대로이며, 돌림판 그림도 이 순서로 그린다.</summary>
public sealed class EventRouletteInfoDto {
    public string EventKey = "";
    /// <summary>그 이벤트의 전용 재화 보유량(돌림판 다트 등).</summary>
    public int Tokens;
    public List<RewardItem> Slots = new();
}

/// <summary>돌림판 1회 결과. SlotIndex 는 정보의 Slots 순서와 같은 칸 번호다(그 칸에 돌림판을 멈춘다).</summary>
public sealed class EventRouletteSpinResultDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=돌림판없음/이벤트종료,2=재화부족,3=계정없음</summary>
    public int FailReason;
    public int SlotIndex = -1;
    public int Tokens;
    /// <summary>골드/다이아/기력 잔액. 서버가 확정한 값이므로 화면이 다시 더하지 않는다.</summary>
    public int NewGold;
    public int NewDiamond;
    public int NewStamina;
    public List<RewardItem> Granted = new();
}

/// <summary>
/// 이벤트 돌림판 통신. 전용 재화 보유량은 이벤트 키별 ReactiveProperty 로 노출해,
/// 이벤트 페이지(보유량 표시)와 돌림판 팝업이 같은 값을 구독한다(미션 수령으로 늘어도 양쪽이 함께 바뀐다).
/// </summary>
public interface IEventRouletteService {
    IReadOnlyReactiveProperty<int> TokensOf(string eventKey);

    UniTask<EventRouletteInfoDto> RequestInfoAsync(string eventKey, CancellationToken ct = default);

    /// <summary>보유량만 다시 받아 온다(미션 수령 직후 등). 실패해도 예외를 밖으로 내지 않는다.</summary>
    UniTask RefreshAsync(string eventKey, CancellationToken ct = default);

    UniTask<EventRouletteSpinResultDto> SpinAsync(string eventKey, CancellationToken ct = default);
}
