using System;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using DummyClient;
using ServerCore;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 서버 연결과 패킷 송수신을 담당. 내부 상태는 전부 캡슐화하며,
/// 세션(ServerSession)은 internal 콜백 메서드로만 통신한다.
/// 핑/RTT 측정은 RttMonitor, 특정 패킷 대기는 PacketWaiter 로 분리.
/// </summary>
public class NetworkManager : MonoBehaviour, INetworkService, IServerRouteTarget {
    [Header("Connection")]
    [Tooltip("서버 주소. 운영은 game.ahwhistudio.com 이다. TLS 를 켜면 인증서를 발급받은 도메인과 정확히 같아야 하며, " +
             "IP 를 넣으면 접속이 거절된다(공인 인증서는 IP 로 발급되지 않는다).")]
    [FormerlySerializedAs("ip")]
    [SerializeField] private string host = "game.ahwhistudio.com";
    [Tooltip("운영은 443(TLS). 개발 PC 서버에 직접 붙을 때는 7777.")]
    [SerializeField] private int port = 443;

    [Header("Security")]
    [Tooltip("전송 구간 TLS 암호화. 개인정보와 로그인 토큰이 오가는 구간이므로 운영 빌드에서는 반드시 켠다. " +
             "개발 PC 서버(평문)에 붙을 때만 끈다.")]
    [SerializeField] private bool useTls = true;
    [Tooltip("선택 사항. 비워 두면 기기의 표준 인증서 검증만으로 판정한다(권장). " +
             "값을 넣으면 표준 검증을 통과한 인증서 중에서도 그 지문과 일치해야만 접속한다. " +
             "형식은 인증서(DER) 전체의 SHA-256 을 Base64 로 적은 값이며, 여러 개를 넣으면 그중 하나만 맞으면 된다. " +
             "발급 기관이 바뀌면 접속이 막히므로 운영 절차(Docs/TLS-설정.md)를 확인하고 사용할 것.")]
    [SerializeField] private string[] pinnedCertificateHashes;

    [Header("Server Profiles (에디터 전환용)")]
    [Tooltip("자주 오가는 서버 설정을 미리 적어 두는 목록. 인스펙터의 버튼으로 위 접속 값에 한 번에 적용한다. " +
             "게임 실행 중에는 쓰이지 않고, 실제 접속은 위의 Host/Port/Use Tls 값을 따른다. " +
             "단 원격 경로표(ServerRouteConfig)에 이 앱 버전이 적혀 있으면 그 대상이 우선한다.")]
    [SerializeField] private ServerProfile[] serverProfiles;

    [Header("Heartbeat")]
    [Tooltip("이 시간(초) 동안 Pong 이 한 번도 안 오면 연결이 끊긴 것으로 보고 강제 종료한다(half-open 감지).")]
    [SerializeField] private float heartbeatTimeoutSeconds = 5f;

    private ServerSession _session;
    private ServerRoute _route;      // 원격 경로표가 지정한 접속 대상. null 이면 인스펙터 값으로 접속한다.
    private float _lastPongTime;     // 마지막 Pong 수신 시각(realtimeSinceStartup)
    private bool _heartbeatActive;   // 첫 Pong 수신 후 true. 그 전(로그인 전 등 서버가 Pong 안 주는 구간)엔 워치독을 끈다.
    private UniTaskCompletionSource _connectTcs;
    private CancellationTokenSource _lifeCts;   // 컴포넌트 생명주기
    private CancellationTokenSource _pingCts;   // 연결 단위 핑 루프

    private readonly PacketWaiter _waiter = new();
    private readonly RttMonitor _rtt = new();

    public bool IsConnected { get; private set; }
    public float LastRtt => _rtt.Average;   // ServerToolEditor 참조용

    public event Action Connected;
    public event Action Disconnected;
    public event Action<IPacket> PacketReceived;

    private void Awake() {
        _lifeCts = new CancellationTokenSource();
        _session = new ServerSession(this);
    }

    private void Update() {
        var packets = PacketQueue.Instance.PopAll();
        foreach (var packet in packets) {
            PacketManager.Instance.HandlePacket(_session, packet);

            // 핑/RTT 는 네트워크 내부 관심사이므로 정적 PacketHandler 를 거치지 않고 여기서 직접 처리한다.
            // (전역 역참조 없이 매니저 내부에서 자기 완결적으로 처리해 순환 의존을 제거)
            if (packet is S_Pong pong) {
                _rtt.OnPong(pong);
                _lastPongTime = Time.realtimeSinceStartup;   // 하트비트 갱신
                _heartbeatActive = true;                     // 첫 Pong 부터 워치독 가동(= 서버가 핑에 응답하기 시작)
            }

            _waiter.Resolve(packet);   // 메인 스레드에서 대기 중인 Awaiter 해제

            // 특정 대기자 없이 상시 수신해야 하는 서버 푸시(예: S_MailNotify)를 위해 모든 패킷을 통지한다.
            // 구독자는 관심 타입만 필터링한다. 이미 메인 스레드이므로 Unity API 접근이 안전하다.
            PacketReceived?.Invoke(packet);
        }
    }

    // iOS 는 AppLifecycle 이 먼저 프로세스를 끝내면 이 콜백이 건너뛰어질 수 있다(NativeProcessExit 참고).
    // 프로세스가 사라지면 소켓도 함께 닫히고 서버는 FIN 으로 즉시 끊김을 감지하므로 결과는 같다.
    private void OnApplicationQuit() => _session?.Disconnect();

    private void OnDestroy() {
        StopPing();
        _lifeCts?.Cancel();
        _lifeCts?.Dispose();
        _waiter.CancelAll();
    }

    // ── 연결 ────────────────────────────────────────────────
    public UniTask ConnectAsync(CancellationToken ct = default) {
        if (IsConnected) return UniTask.CompletedTask;
        if (_connectTcs != null) return _connectTcs.Task; // 재진입 보호

        _connectTcs = new UniTaskCompletionSource();

        // 타임아웃/외부 취소 시 대기 상태를 정리해 재시도가 가능하게 한다.
        // 취소는 CancelAfter 의 타이머(스레드풀)에서 올 수 있다. 그대로 TrySetCanceled 하면 await 연속 실행이
        // 비-메인 스레드에서 재개되어 Unity API(로그/게임오브젝트 접근)가 깨진다. UniTask.Post 로 메인에 마샬링한다.
        ct.Register(() => UniTask.Post(() => {
            if (_connectTcs == null) return;
            _connectTcs.TrySetCanceled();
            _connectTcs = null;
        }));

        // 재접속마다 "새 세션"으로 시작한다(cold connect 와 동일).
        // ServerSession/Session 은 한 번 끊기면 _disconnected 가 1로 고정되고 수신 버퍼/이벤트 핸들러가
        // 남아 송수신이 죽거나 직전 연결의 패킷이 섞인다. 이전 연결의 잔여 패킷/대기도 함께 비워,
        // 재접속 클라가 직전 로그인 상태에 기대지 않고 서버와 진짜 핸드셰이크(C_Version 등)를 다시 하게 만든다.
        _waiter.CancelAll();
        PacketQueue.Instance.PopAll();
        _session = new ServerSession(this);

        var endpoint = _route != null
            ? new ServerEndpoint(_route.host, _route.port, _route.useTls, pinnedCertificateHashes)
            : new ServerEndpoint(host, port, useTls, pinnedCertificateHashes);
        // 연결 실패(거절/타임아웃/인증서 불일치)는 콜백으로 즉시 알려 받아, 대기가 타임아웃까지 매달리지 않게 한다.
        new Connector().Connect(endpoint, () => _session, OnConnectFailed);

        return _connectTcs.Task;
    }

    // 이미 맺어진 연결에는 영향을 주지 않는다. 다음 ConnectAsync 부터 적용된다.
    public void ApplyRoute(ServerRoute route) => _route = route != null && route.IsValid ? route : null;

    // ── 송신 ────────────────────────────────────────────────
    public void Send(ArraySegment<byte> sendBuff) => _session.Send(sendBuff);

    // ── 의도적 종료 ─────────────────────────────────────────
    // 로그아웃 등에서 호출. 세션 종료 -> OnSessionDisconnected -> Disconnected 이벤트로 이어진다.
    public void Disconnect() {
        // 상태를 여기서 즉시 내린다. 세션 콜백은 메인 스레드로 마샬링되어 한 프레임 뒤에 도착하는데,
        // 로그아웃은 끊자마자 타이틀에서 재접속을 시도하므로 그 사이 IsConnected 가 true 로 남아 있으면
        // "이미 연결됨"으로 보고 재접속을 건너뛴다. 그러면 죽은 연결로 부트가 진행돼 버전 확인에서 멈춘다.
        IsConnected = false;
        _session?.Disconnect();
    }

    // ── 특정 패킷 대기 ──────────────────────────────────────
    public UniTask<T> WaitPacketAsync<T>(CancellationToken ct = default) where T : IPacket
        => _waiter.WaitAsync<T>(ct);

    // ── ServerSession 콜백 (같은 어셈블리에서만 호출) ───────
    internal void OnSessionConnected(EndPoint endPoint) {
        // 소켓 IO 스레드 → 메인 스레드로 마샬링
        UniTask.Post(() => {
            IsConnected = true;
            _connectTcs?.TrySetResult();
            _connectTcs = null;
            _lastPongTime = Time.realtimeSinceStartup;   // 하트비트 기준점 초기화
            _heartbeatActive = false;                    // 첫 Pong 전까지 워치독 비활성(로그인 전 false-positive 방지)
            StartPing();
            Connected?.Invoke();
        });
    }

    // 연결 자체가 성립하지 못한 경우(세션이 시작되기 전). 소켓 스레드에서 올 수 있으므로 메인으로 마샬링한다.
    private void OnConnectFailed(Exception e) {
        UniTask.Post(() => {
            _connectTcs?.TrySetException(e);
            _connectTcs = null;
        });
    }

    internal void OnSessionDisconnected(EndPoint endPoint) {
        UniTask.Post(() => {
            IsConnected = false;
            StopPing();
            _connectTcs?.TrySetException(new Exception("Disconnected"));
            _connectTcs = null;
            Disconnected?.Invoke();
        });
    }

    /// <summary>
    /// 앱이 백그라운드에 다녀오면 하트비트 기준점을 다시 잡는다.
    ///
    /// 멈춰 있는 동안에는 핑을 보내지도 퐁을 받지도 못한다. 그 공백을 그대로 재면 돌아오는 순간
    /// "응답이 끊겼다"로 읽혀 멀쩡한 연결을 스스로 끊는다. 판정 시간이 몇 초라 앱 위로 다른 화면이
    /// 올라오는 경우는 거의 다 걸린다(스토어 결제 창, 전화, 알림, 앱 전환).
    ///
    /// 판정을 없애는 것이 아니라 "돌아온 시점부터 다시 센다". 실제로 끊긴 연결이면 기준점을 다시
    /// 잡아도 다음 몇 초 안에 퐁이 오지 않아 그때 정상적으로 걸린다.
    /// </summary>
    private void OnApplicationPause(bool paused) {
        if (paused) return;
        _lastPongTime = Time.realtimeSinceStartup;
    }

    // ── 핑 루프 ─────────────────────────────────────────────
    private void StartPing() {
        StopPing();
        _pingCts = CancellationTokenSource.CreateLinkedTokenSource(_lifeCts.Token);
        PingLoop(_pingCts.Token).Forget();
    }

    private void StopPing() {
        if (_pingCts == null) return;
        _pingCts.Cancel();
        _pingCts.Dispose();
        _pingCts = null;
    }

    private async UniTaskVoid PingLoop(CancellationToken ct) {
        try {
            while (!ct.IsCancellationRequested) {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: ct);

                // 하트비트: (첫 Pong 수신 이후) 일정 시간 Pong 이 끊기면 TCP 가 미처 감지 못한 단절(half-open)로 보고 강제 종료.
                // 첫 Pong 전에는 비활성 - 서버가 로그인(룸 입장) 전까지 C_Ping 에 응답하지 않으므로, 로그인 대기 중 오판으로 끊지 않는다.
                // Disconnect -> OnSessionDisconnected -> Disconnected 이벤트로 이어져 ConnectionGuard 가 처리한다.
                if (_heartbeatActive && Time.realtimeSinceStartup - _lastPongTime > heartbeatTimeoutSeconds) {
                    _session.Disconnect();
                    return;
                }

                _session.Send(_rtt.CreatePing().Write());
            }
        } catch (OperationCanceledException) {
            // 정상 종료
        }
    }
}
