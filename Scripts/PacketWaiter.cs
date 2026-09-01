using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ServerCore;

/// <summary>
/// 특정 패킷 타입의 1회 수신을 await 로 기다리게 해주는 보조 클래스.
/// Resolve 는 NetworkManager.Update(메인 스레드)에서 호출되므로 await 후속 코드도
/// 메인 스레드에서 재개된다. dynamic 미사용으로 IL2CPP(AOT) 안전.
///
/// 같은 타입을 여러 곳이 동시에 기다릴 수 있다(요청한 순서대로 응답이 짝지어진다).
/// 예전에는 타입당 하나만 대기할 수 있어서, 뒤에 온 대기가 앞선 대기를 취소해 버렸다.
/// 그 결과 서로 아무 관계도 없는 두 기능이 같은 조회를 동시에 하면 한쪽이 조용히 취소됐다
/// (예: 메인 진입의 순차 로딩과 배지 갱신이 각자 수련 정보를 조회하는 경우).
/// 요청을 보낸 쪽과 응답을 받는 쪽이 어긋나지 않도록, 먼저 기다린 쪽이 먼저 받는다(선입선출).
/// TCP 라 응답도 요청 순서대로 도착하므로 이 규칙이면 짝이 맞는다.
/// </summary>
public sealed class PacketWaiter {
    private sealed class Waiter {
        public Action<IPacket> Resolve;
        public Action Cancel;
    }

    // 타입 -> 대기 줄. 중간에서 취소된 대기를 O(1) 로 빼내려고 LinkedList 를 쓴다.
    private readonly Dictionary<Type, LinkedList<Waiter>> _waiters = new();

    public UniTask<T> WaitAsync<T>(CancellationToken ct = default) where T : IPacket {
        var key = typeof(T);
        if (!_waiters.TryGetValue(key, out var line)) {
            line = new LinkedList<Waiter>();
            _waiters[key] = line;
        }

        var tcs = new UniTaskCompletionSource<T>();
        var node = line.AddLast(new Waiter {
            Resolve = p => tcs.TrySetResult((T)p),
            Cancel = () => tcs.TrySetCanceled(),
        });

        if (ct.CanBeCanceled) {
            // 취소가 CancelAfter 타이머(스레드풀)에서 올 수 있다. Resolve 는 메인 스레드(NetworkManager.Update)에서
            // 돌므로, 취소도 UniTask.Post 로 메인에 마샬링해 목록 접근과 await 재개를 메인 스레드로 통일한다.
            ct.Register(() => UniTask.Post(() => {
                // 이미 응답을 받았거나 정리된 대기면 List 가 null 이다(이 취소는 늦게 도착한 것이라 무시한다).
                if (node.List != null) {
                    node.List.Remove(node);
                    if (line.Count == 0) _waiters.Remove(key);
                }
                tcs.TrySetCanceled();
            }));
        }
        return tcs.Task;
    }

    /// <summary>도착한 패킷을 가장 먼저 기다린 대기자에게 넘긴다. 기다리는 곳이 없으면 흘려보낸다.</summary>
    public void Resolve(IPacket packet) {
        var key = packet.GetType();
        if (!_waiters.TryGetValue(key, out var line) || line.Count == 0) return;

        var node = line.First;
        line.Remove(node);
        if (line.Count == 0) _waiters.Remove(key);

        node.Value.Resolve(packet);
    }

    /// <summary>대기 중인 전부를 취소한다(연결 끊김 등). 취소 콜백이 다시 대기를 걸어도 안전하도록 먼저 비운다.</summary>
    public void CancelAll() {
        if (_waiters.Count == 0) return;

        var pending = new List<Waiter>();
        foreach (var line in _waiters.Values) pending.AddRange(line);
        _waiters.Clear();

        foreach (var waiter in pending) waiter.Cancel();
    }
}
