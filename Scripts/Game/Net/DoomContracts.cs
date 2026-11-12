using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>멸망전 계정 현황. 계정이 기억하는 것은 최고 점수와 "어디까지 받았는가" 두 가지뿐이다.</summary>
public sealed class DoomInfoDto {
    public int BestScore;
    public int ClaimedScore;
    /// <summary>지금 받을 수 있는 보상이 있는지(버튼/배지 활성).</summary>
    public bool CanClaim;
}

/// <summary>
/// 오늘 참여 현황(V1.13). 하루 참여 제한은 없고, 하루 무료분 이후로는 다이아로 참여한다.
/// 비용은 서버가 계산해 내려주므로(서버 DoomCatalog.EntryCost) 클라는 비용 표를 들고 있지 않다.
/// </summary>
public sealed class DoomEntryDto {
    /// <summary>오늘 참여한 횟수(무료 포함).</summary>
    public int UsedEntries;
    /// <summary>다음 참여 비용(다이아). 0 이면 무료다.</summary>
    public int NextEntryCost;

    public bool IsFree => NextEntryCost <= 0;
}

/// <summary>입장 실패 사유. 값은 서버 GameDataRepository.DoomEnterFail 과 같다.</summary>
public static class DoomEnterFail {
    public const int None = 0;
    public const int NoAccount = 2;
    public const int NotEnoughDiamond = 3;
}

/// <summary>한 판 입장 응답(무료분 또는 다이아 소모).</summary>
public sealed class DoomEnterDto {
    public bool IsSuccess;
    /// <summary>DoomEnterFail.</summary>
    public int FailReason;
    /// <summary>처리 후 현황(다음 참여 비용 포함).</summary>
    public DoomEntryDto Entry;
    /// <summary>처리 후 다이아 잔액. 서버가 확정한 값이므로 화면이 다시 빼지 않는다.</summary>
    public int NewDiamond;
}

/// <summary>한 판 종료 보고에 대한 응답(기록 갱신 + 보유 소녀 전원 경험치).</summary>
public sealed class DoomFinishDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=점수오류,2=계정없음</summary>
    public int FailReason;
    public int BestScore;
    /// <summary>이번 판이 최고 점수를 갈아치웠는지(연출용).</summary>
    public bool IsBest;
    public bool CanClaim;
    /// <summary>소녀 1명이 받은 경험치(투자 증폭 포함).</summary>
    public int GainedExp;
    public List<CharExpGain> CharExp = new();
}

/// <summary>보상 수령 결과. 도달한 구간 전부가 한 번에 들어온다.</summary>
public sealed class DoomClaimResultDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=받을것없음,2=계정없음</summary>
    public int FailReason;
    public int ClaimedScore;
    public int BestScore;
    /// <summary>수령 후 골드/다이아 잔액. 서버가 확정한 값이므로 화면이 다시 더하지 않는다.</summary>
    public int NewGold;
    public int NewDiamond;
    public List<RewardItem> Granted = new();
}

public interface IDoomService {
    /// <summary>
    /// 지금 받을 수 있는 보상이 있는지. 배지가 이 값을 구독하므로 서버 응답이 올 때마다 실시간으로 켜지고 꺼진다
    /// (재접속해야 불이 들어오는 일이 없다).
    /// </summary>
    IReadOnlyReactiveProperty<bool> HasClaimable { get; }

    /// <summary>
    /// 오늘 무료 참여가 남았는지. 배지가 이 값도 구독한다. 유료 참여는 언제든 가능하므로 배지 근거로 쓰지 않는다
    /// (무료분을 쓴 뒤에도 배지가 켜져 있으면 하고 싶지 않아도 계속 거슬린다).
    /// </summary>
    IReadOnlyReactiveProperty<bool> HasFreeEntry { get; }

    UniTask<DoomInfoDto> RequestInfoAsync(CancellationToken ct = default);

    /// <summary>오늘 참여 현황(다음 참여 비용).</summary>
    UniTask<DoomEntryDto> RequestEntryAsync(CancellationToken ct = default);

    /// <summary>수령 가능 여부와 참여 현황을 가볍게 다시 받아온다(배지 갱신용).</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>한 판 입장. 무료분 또는 다이아를 쓴다. 씬에 들어설 때와 재도전할 때마다 부른다.</summary>
    UniTask<DoomEnterDto> EnterAsync(CancellationToken ct = default);

    /// <summary>
    /// 한 판 종료 보고(처치 수 + 끝난 페이즈). 최고 점수만 갱신되므로 낮은 점수를 보고해도 기록이 내려가지 않고,
    /// 입장한 판이면 보유 소녀 전원이 끝난 페이즈만큼 경험치를 받는다. 나가기/재도전으로 버린 판도 보고한다.
    /// </summary>
    UniTask<DoomFinishDto> FinishAsync(int score, int phase, CancellationToken ct = default);

    /// <summary>
    /// 한 판을 끝까지 치렀다는 보고(응답 없음). 전멸로 판이 끝났을 때만 부르며, 나가기/재도전으로 버린 판에는 부르지 않는다.
    /// 반드시 그 판의 종료 보고(FinishAsync)가 성공한 뒤에 부른다(서버가 종료 보고를 완주 허용의 근거로 쓴다).
    /// </summary>
    void ReportFinished();

    /// <summary>도달한 구간의 보상을 한 번에 받는다.</summary>
    UniTask<DoomClaimResultDto> ClaimAsync(CancellationToken ct = default);
}
