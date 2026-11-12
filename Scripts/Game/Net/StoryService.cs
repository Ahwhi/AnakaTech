using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
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
    private readonly List<StoryChapterStateDto> _chapters = new();
    private readonly ReactiveProperty<bool> _hasBadge = new(false);

    public StoryService(INetworkService net) {
        _net = net;
    }

    public bool IsCompleted(string storyKey) =>
        !string.IsNullOrEmpty(storyKey) && _completed.Contains(storyKey);

    public IReadOnlyList<StoryChapterStateDto> Chapters => _chapters;

    public int Version { get; private set; }

    public IReadOnlyReactiveProperty<bool> HasBadge => _hasBadge;

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

            _chapters.Clear();
            foreach (var c in res.chapters) {
                if (string.IsNullOrEmpty(c.storyKey)) continue;
                _chapters.Add(new StoryChapterStateDto {
                    StoryKey = c.storyKey,
                    Chapter = StoryRules.ChapterOf(c.storyKey),
                    Completed = c.completed,
                    RewardClaimed = c.rewardClaimed,
                    Unlocked = c.unlocked,
                });
            }
            Bump();
        } catch (OperationCanceledException) {
            ClearAll();
        } catch (Exception e) {
            Debug.LogException(e);
            ClearAll();
        }
    }

    // 조회 실패는 "아직 못 봤다"로 떨어진다. 그래도 배지는 꺼 둬야 한다(근거 없이 불이 켜져 있으면 안 된다).
    private void ClearAll() {
        _completed.Clear();
        _chapters.Clear();
        Bump();
    }

    // 현황이 바뀌면 번호를 올리고 배지를 다시 계산한다. 배지 근거가 현황 하나뿐이라 한곳에 모아 둔다.
    private void Bump() {
        Version++;

        bool badge = false;
        for (int i = 0; i < _chapters.Count; i++) {
            var c = _chapters[i];
            if (c.CanClaim || c.CanRead) { badge = true; break; }
        }
        _hasBadge.Value = badge;
    }

    public async UniTask<bool> CompleteAsync(string storyKey, CancellationToken ct = default) {
        if (string.IsNullOrEmpty(storyKey)) return false;

        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ResponseTimeout);

            var wait = _net.WaitPacketAsync<S_CompleteStoryResult>(cts.Token);
            _net.Send(new C_CompleteStory { storyKey = storyKey }.Write());
            var res = await wait;

            if (res.isSuccess) {
                _completed.Add(storyKey);
                // 방금 읽은 편은 그 자리에서 "보상 받을 수 있음"이 된다. 목록을 다시 받지 않아도
                // 팝업과 배지가 곧바로 따라오도록 캐시를 고쳐 둔다.
                MarkCompleted(storyKey);
            }
            return res.isSuccess;
        } catch (OperationCanceledException) {
            return false;
        } catch (Exception e) {
            Debug.LogException(e);
            return false;
        }
    }

    public async UniTask<StoryClaimResultDto> ClaimRewardAsync(string storyKey, CancellationToken ct = default) {
        var dto = new StoryClaimResultDto { StoryKey = storyKey ?? "", FailReason = 1 };
        if (string.IsNullOrEmpty(storyKey)) return dto;

        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ResponseTimeout);

            var wait = _net.WaitPacketAsync<S_ClaimStoryRewardResult>(cts.Token);
            _net.Send(new C_ClaimStoryReward { storyKey = storyKey }.Write());
            var res = await wait;

            dto.IsSuccess = res.isSuccess;
            dto.FailReason = res.failReason;
            dto.StoryKey = res.storyKey ?? storyKey;
            dto.Diamond = res.diamond;
            dto.NewDiamond = res.newDiamond;

            if (res.isSuccess) MarkClaimed(storyKey);
            return dto;
        } catch (OperationCanceledException) {
            return dto;
        } catch (Exception e) {
            Debug.LogException(e);
            return dto;
        }
    }

    // 한 편이 완독되면 그 다음 편의 해금 조건(직전 편 완독)도 함께 충족된다.
    // 서버와 같은 규칙으로 캐시를 고쳐, 목록을 다시 받기 전에도 화면이 맞는 상태를 보이게 한다.
    private void MarkCompleted(string storyKey) {
        for (int i = 0; i < _chapters.Count; i++) {
            if (_chapters[i].StoryKey != storyKey) continue;
            _chapters[i].Completed = true;

            // 다음 편은 진도 조건이 이미 충족돼 있어야 열린다. 진도는 서버만 아는 값이라
            // 여기서 임의로 열지 않고, 다음 조회에서 서버가 내려 주는 값을 그대로 쓴다.
            break;
        }
        Bump();
    }

    private void MarkClaimed(string storyKey) {
        for (int i = 0; i < _chapters.Count; i++) {
            if (_chapters[i].StoryKey != storyKey) continue;
            _chapters[i].RewardClaimed = true;
            break;
        }
        Bump();
    }
}
