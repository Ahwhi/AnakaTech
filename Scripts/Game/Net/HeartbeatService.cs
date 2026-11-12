using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

/// <summary>
/// 접속 유지 시간 적립용 하트비트 송신기. 로그인 이후 주기적으로 서버에 "아직 접속 중"을 알린다.
///
/// 경과 시간을 클라가 계산해 보내지 않고 신호만 보낸다. 누적 시간에 보상이 걸려 있으므로 얼마나 놀았는지는
/// 서버가 신호 간격으로 판정해야 하며(GameDataRepository.TrackHeartbeat), 클라가 숫자를 보내면 조작에 그대로 뚫린다.
///
/// 앱이 백그라운드로 가면 송신을 멈춘다. 그 사이 간격이 서버의 허용 상한을 넘으면 그 구간은 접속으로 세지 않으므로,
/// 켜 두고 방치한 시간이 아니라 실제로 보고 있던 시간이 쌓인다.
/// </summary>
public sealed class HeartbeatService : IHeartbeatService, IDisposable {
    /// <summary>송신 주기(초). 서버의 허용 간격(MaxHeartbeatGapSeconds)보다 넉넉히 짧아야 한 번 놓쳐도 구간이 끊기지 않는다.</summary>
    private const float IntervalSeconds = 60f;

    private readonly INetworkService _net;
    private readonly IEventBadgeService _badges;
    private readonly IDisposable _recoveredSubscription;
    private CancellationTokenSource _cts;
    private int _lastNotifiedMinutes = -1;
    private bool _resumeOnRecovery;   // 끊기기 직전에 돌고 있었는지. 같은 계정으로 다시 붙으면 이어서 돈다.

    public HeartbeatService(INetworkService net, IEventBadgeService badges, ISessionRecovery recovery) {
        _net = net;
        _badges = badges;
        // 연결이 끊기면 이 계정의 세션은 끝난 것이다. 로그아웃(의도적 끊기)과 접속 장애가 모두 이 신호로
        // 모이므로, 여기서 멈추면 다음 로그인은 항상 빈 상태에서 시작한다.
        _net.Disconnected += OnDisconnected;
        // 화면 전환 없이 같은 계정으로 다시 붙은 경우에는 메인 진입이 다시 일어나지 않으므로 여기서 이어 간다.
        // 전투 씬처럼 메인 밖에 있을 때 붙어도 접속 시간이 끊기지 않는다.
        _recoveredSubscription = recovery.Recovered.Subscribe(_ => OnRecovered());
    }

    private void OnDisconnected() {
        // 복구 재시도 중의 끊김이 이 값을 지우지 않도록 켜는 쪽으로만 기록한다. 다음 Start 에서 내려간다.
        if (_cts != null) _resumeOnRecovery = true;
        Stop();
    }

    private void OnRecovered() {
        if (_resumeOnRecovery) Start();
    }

    /// <summary>가장 최근에 서버가 알려준 오늘 누적 접속 시간(초). 표시용이며 판정은 서버가 한다.</summary>
    public int SessionSecondsToday { get; private set; }

    // 서버가 알려준 기력 부스트권 보유량. 이 서비스는 씬보다 오래 살아 플레이어 상태 저장소를 알지 못하므로
    // 값만 흘려보내고, 저장소 반영은 그 씬의 주인이 구독해서 한다.
    //
    // 마지막 값을 품는 ReactiveProperty 가 아니라 그때그때 흘려보내는 Subject 다. 이 서비스는 계정보다
    // 오래 살기 때문에, 마지막 값을 품고 있으면 다른 계정으로 다시 로그인했을 때 새 구독자가 구독하는
    // 순간 이전 계정의 값을 받아, 방금 서버에서 받아 채운 값을 덮어써 버린다.
    private readonly Subject<int> _staminaBoostTicket = new();

    public IObservable<int> StaminaBoostTicket => _staminaBoostTicket;

    public void Start() {
        _resumeOnRecovery = false;
        if (_cts != null) return;
        _cts = new CancellationTokenSource();
        RunAsync(_cts.Token).Forget();
    }

    // 멈춤은 곧 "이 계정의 세션이 끝났다"는 뜻이라 누적치도 함께 버린다. 남겨 두면 다음 로그인에서
    // 이전 계정의 접속 시간이 잠시 보이고, 분이 바뀌는 시점을 잘못 잡아 배지 갱신도 한 박자 늦는다.
    public void Stop() {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        SessionSecondsToday = 0;
        _lastNotifiedMinutes = -1;
    }

    private async UniTaskVoid RunAsync(CancellationToken ct) {
        // 첫 신호를 바로 보내 기준점을 잡는다(서버는 첫 신호로는 시간을 더하지 않고 기준만 세운다).
        while (!ct.IsCancellationRequested) {
            await SendOnceAsync(ct);
            try {
                await UniTask.Delay(TimeSpan.FromSeconds(IntervalSeconds), ignoreTimeScale: true, cancellationToken: ct);
            } catch (OperationCanceledException) {
                return;
            }
        }
    }

    private async UniTask SendOnceAsync(CancellationToken ct) {
        // 백그라운드에서는 보내지 않는다. 화면을 보고 있지 않은 시간을 접속으로 쳐 주지 않기 위한 것이며,
        // 서버도 긴 공백은 스스로 걸러내므로 여기서 굳이 따라잡기(catch-up)를 하지 않는다.
        if (!Application.isFocused && Application.platform != RuntimePlatform.WindowsEditor) return;

        try {
            var wait = _net.WaitPacketAsync<S_HeartbeatResult>(ct);
            _net.Send(new C_Heartbeat().Write());
            var res = await wait;
            SessionSecondsToday = res.sessionSecondsToday;
            // 부스트권 자정 충전은 서버가 접근 시점에 따라잡는 방식이라, 주기 신호인 이 응답이 그 결과를 실어 온다.
            // 앱을 켜 둔 채 자정을 넘겨도 다음 신호(최대 1분)에 화면이 채워진다.
            _staminaBoostTicket.OnNext(res.staminaBoostTicket);

            // 접속 시간이 1분 넘어갈 때마다 이벤트 배지를 다시 받아온다. 접속 보상은 시간이 차면 저절로
            // 달성되므로, 유저 조작이 없어도 메인 버튼에 불이 들어와야 한다(다른 이벤트와 달리 계기가 없다).
            // 임계치를 클라가 알 필요는 없다. 분이 바뀐 순간만 태우고 달성 판정은 서버가 한다.
            int minutes = SessionSecondsToday / 60;
            if (minutes != _lastNotifiedMinutes) {
                _lastNotifiedMinutes = minutes;
                await _badges.RefreshClaimStatesAsync(ct);
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            // 하트비트는 실패해도 게임 진행에 지장이 없다. 다음 주기에 다시 시도한다.
            Debug.LogWarning($"[Heartbeat] 송신 실패: {e.Message}");
        }
    }

    public void Dispose() {
        _net.Disconnected -= OnDisconnected;
        _recoveredSubscription.Dispose();
        Stop();
        _staminaBoostTicket.Dispose();
    }
}
