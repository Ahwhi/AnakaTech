using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

/// <summary>
/// 메인 퀘스트 통신 구현. 조회(요청-응답)와 갱신(서버 푸시)을 한 서비스가 함께 다룬다.
///
/// 현황(S_MainQuest)을 상시 구독하는 것이 핵심이다. 메인 퀘스트는 유저가 다른 화면에서 한 행동
/// (모집/강화/의뢰 클리어 등) 때문에 서버에서 완료되므로, 화면이 물어봐서 아는 값이 아니라
/// 서버가 알려 줘야 아는 값이다. 수령 직후의 다음 퀘스트도 같은 경로로 들어온다.
/// </summary>
public sealed class MainQuestService : IMainQuestService, IDisposable {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly ReactiveProperty<MainQuestDto> _current = new(null);
    private readonly Subject<int> _completed = new();

    public IReadOnlyReactiveProperty<MainQuestDto> Current => _current;
    public IObservable<int> Completed => _completed;

    public MainQuestService(INetworkService net) {
        _net = net;
        _net.PacketReceived += OnPacketReceived;
    }

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ResponseTimeout);

            var wait = _net.WaitPacketAsync<S_MainQuest>(cts.Token);
            _net.Send(new C_MainQuest().Write());
            await wait;   // 반영은 푸시 처리(OnPacketReceived)가 한다. 경로를 하나로 두어 두 번 반영되지 않게 한다.
        } catch (OperationCanceledException) {
            // 화면 전환/타임아웃. 다음 갱신이나 서버 푸시에서 맞춰지므로 조용히 넘어간다.
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // 응답이 없는 단방향 보고다. 서버가 진행을 인정하면 현황(S_MainQuest)이 저절로 따라오므로
    // 여기서 기다릴 것이 없고, 인정하지 않아도 화면이 달라질 것이 없다.
    public void ReportAction(MainQuestObjective objective) =>
        _net.Send(new C_MainQuestAction { objective = (int)objective }.Write());

    public async UniTask<MainQuestClaimResultDto> ClaimAsync(int level, CancellationToken ct = default) {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ResponseTimeout);

        var wait = _net.WaitPacketAsync<S_ClaimMainQuestResult>(cts.Token);
        _net.Send(new C_ClaimMainQuest { level = level }.Write());
        var res = await wait;

        var dto = new MainQuestClaimResultDto {
            IsSuccess = res.isSuccess,
            FailReason = res.failReason,
            ClaimedLevel = res.claimedLevel,
            NewGold = res.newGold,
            NewDiamond = res.newDiamond,
        };
        foreach (var g in res.grantedRewards)
            dto.Granted.Add(RewardItem.FromGrant(g.kind, g.value, g.qty, g.currencyKey));
        foreach (var e in res.charExps)
            dto.CharExp.Add(new CharExpGain {
                CharDataId = e.charDataId,
                GainedExp = e.gainedExp,
                NewLevel = e.newLevel,
                NewExp = e.newExp,
                MaxExp = e.maxExp,
                GainedStatPoints = e.gainedStatPoints,
            });
        return dto;
    }

    // 현황/완료 알림은 요청 없이도 도착한다(다른 화면에서 한 행동이 퀘스트를 완료시키는 것이 보통이다).
    private void OnPacketReceived(IPacket packet) {
        switch (packet) {
            case S_MainQuest quest:
                _current.Value = ToDto(quest);
                break;
            case S_MainQuestNotify notify:
                _completed.OnNext(notify.level);
                break;
        }
    }

    private static MainQuestDto ToDto(S_MainQuest p) {
        var dto = new MainQuestDto {
            Level = p.level,
            Objective = (MainQuestObjective)p.objective,
            Param = p.param,
            Progress = p.progress,
            Threshold = p.threshold,
            IsCompleted = p.isCompleted,
        };
        foreach (var r in p.rewards)
            dto.Rewards.Add(RewardItem.FromGrant(r.kind, r.value, r.qty, r.currencyKey));
        return dto;
    }

    public void Dispose() {
        _net.PacketReceived -= OnPacketReceived;
        _completed.Dispose();
        _current.Dispose();
    }
}
