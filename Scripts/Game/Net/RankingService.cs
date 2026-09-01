using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 랭킹 통신 구현(다른 서비스와 동일한 WaitPacketAsync + Send 패턴).
/// 순위 산정은 서버가 주기적으로 만들어 둔 스냅샷에서 오므로 여기서는 값을 옮기기만 한다.
/// </summary>
public sealed class RankingService : IRankingService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;

    // 조회를 한 번에 하나씩만 내보낸다.
    //
    // 응답 대기(WaitPacketAsync)는 "패킷 종류"로만 짝을 맞추므로, 순위표 두 개를 동시에 요청하면
    // 먼저 도착한 응답이 아무 대기자에게나 붙는다(쟁탈전 탭을 눌렀는데 전투력 순위가 들어오는 식).
    // 탭을 빠르게 옮기면 실제로 겹치므로 여기서 줄을 세운다. 대기 시간은 조회 한 번(최대 8초)뿐이다.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public RankingService(INetworkService net) {
        _net = net;
    }

    public async UniTask<RankingBoardDto> RequestBoardAsync(RankingCategory category, CancellationToken ct = default) {
        await _gate.WaitAsync(ct);
        try {
            return await RequestBoardCoreAsync(category, ct);
        } finally {
            _gate.Release();
        }
    }

    private async UniTask<RankingBoardDto> RequestBoardCoreAsync(RankingCategory category, CancellationToken ct) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_Ranking>(cts.Token);
        _net.Send(new C_Ranking { category = (int)category }.Write());
        var res = await wait;

        var dto = new RankingBoardDto {
            Category = (RankingCategory)res.category,
            UpdatedUnix = res.updatedUnix,
            MyRank = res.myRank,
            MyScore = res.myScore,
        };
        foreach (var e in res.entrys) {
            dto.Entries.Add(new RankingEntryDto {
                Rank = e.rank,
                TargetId = e.targetId,
                Score = e.score,
                Name = e.name,
                SubName = e.subName,
                IconId = e.iconId,
                SubIconId = e.subIconId,
                RepresentativeCharId = e.representativeCharId,
                RepresentativeCostumeId = e.representativeCostumeId,
            });
        }
        return dto;
    }
}
