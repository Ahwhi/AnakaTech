using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 스토리 통신 구현(다른 서비스와 동일한 WaitPacketAsync + Send 패턴).
/// 본 스토리 목록을 캐시해 두는 이유는 타이틀(재생 여부 판단)과 스토리 씬(완료 보고)이 서로 다른 씬이면서
/// 같은 사실을 봐야 하기 때문이다. 서비스는 씬보다 오래 사는 루트 스코프에 등록된다.
///
/// 조회 실패는 "아직 못 봤다"로 떨어진다. 스토리를 한 번 더 보는 쪽이, 봐야 할 스토리를 건너뛰는 쪽보다 낫다.
/// </summary>
public sealed class StoryService : IStoryService {
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(8);

    private readonly INetworkService _net;
    private readonly HashSet<string> _completed = new();

    public StoryService(INetworkService net) {
        _net = net;
    }

    public bool IsCompleted(string storyKey) =>
        !string.IsNullOrEmpty(storyKey) && _completed.Contains(storyKey);

    public async UniTask RefreshAsync(CancellationToken ct = default) {
        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ResponseTimeout);

            var wait = _net.WaitPacketAsync<S_StoryProgress>(cts.Token);
            _net.Send(new C_StoryProgress().Write());
            var res = await wait;

            _completed.Clear();
            foreach (var s in res.storys)
                if (!string.IsNullOrEmpty(s.storyKey)) _completed.Add(s.storyKey);
        } catch (OperationCanceledException) {
            _completed.Clear();
        } catch (Exception e) {
            Debug.LogException(e);
            _completed.Clear();
        }
    }

    public async UniTask<bool> CompleteAsync(string storyKey, CancellationToken ct = default) {
        if (string.IsNullOrEmpty(storyKey)) return false;

        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ResponseTimeout);

            var wait = _net.WaitPacketAsync<S_CompleteStoryResult>(cts.Token);
            _net.Send(new C_CompleteStory { storyKey = storyKey }.Write());
            var res = await wait;

            if (res.isSuccess) _completed.Add(storyKey);
            return res.isSuccess;
        } catch (OperationCanceledException) {
            return false;
        } catch (Exception e) {
            Debug.LogException(e);
            return false;
        }
    }
}
