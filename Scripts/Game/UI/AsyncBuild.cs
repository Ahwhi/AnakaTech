using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 다수의 UI 아이템을 한 프레임에 몰아 생성하면서 생기는 프레임 끊김(첫 실행 렉)을 막기 위한 공용 유틸.
/// 정해진 개수(perFrame)만큼 생성한 뒤 다음 프레임으로 양보하며 나눠 생성한다.
/// 취소 토큰으로 재구성(정렬/필터/데이터 변경 등)이 겹칠 때 진행 중이던 생성을 안전하게 중단할 수 있다.
/// </summary>
public static class AsyncBuild {
    /// <summary>
    /// source 를 순회하며 각 항목에 spawn 을 호출하되, perFrame 개마다 한 프레임 양보한다.
    /// 취소되면 OperationCanceledException 을 던지므로, 호출부는 try/catch 로 중단을 처리한다.
    /// </summary>
    public static async UniTask SpawnChunkedAsync<T>(IReadOnlyList<T> source, int perFrame,
                                                     Action<T> spawn, CancellationToken ct) {
        if (source == null || spawn == null) return;

        int step = perFrame > 0 ? perFrame : int.MaxValue;
        for (int i = 0; i < source.Count; i++) {
            ct.ThrowIfCancellationRequested();
            spawn(source[i]);
            if ((i + 1) % step == 0 && i + 1 < source.Count)
                await UniTask.Yield(ct);
        }
    }
}
