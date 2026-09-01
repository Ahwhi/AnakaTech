using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 핑 패킷 생성과 RTT(EMA)/지터 측정을 담당. NetworkManager 에서 분리해
/// 네트워크 매니저는 연결/송수신에만 집중하게 한다.
/// </summary>
public sealed class RttMonitor {
    private const float Alpha = 0.1f;

    private readonly Dictionary<int, int> _sentAt = new();
    private int _seq;
    private float _ema = -1f;   // RTT 지수평활 평균(미측정 시 -1)
    private float _jitter;      // RTT 변화량 EMA

    public float Average => _ema;
    public float Jitter => _jitter;

    public C_Ping CreatePing() {
        var pkt = new C_Ping {
            seq = ++_seq,
            clientSentTick = Environment.TickCount
        };
        _sentAt[pkt.seq] = pkt.clientSentTick;
        return pkt;
    }

    public void OnPong(S_Pong pkt) {
        if (!_sentAt.Remove(pkt.seq, out int sentTick)) return;

        int rtt = Mathf.Max(0, unchecked(Environment.TickCount - sentTick));
        _ema = _ema < 0f ? rtt : Mathf.Lerp(_ema, rtt, Alpha);
        _jitter = Mathf.Lerp(_jitter, Mathf.Abs(rtt - _ema), Alpha);
    }
}
