using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 총 전투력(덱에 편성된 캐릭터 전투력 합)이 오르면 상승 토스트를 띄운다.
/// "언제 얼마나 올랐는가"만 판단하고 표시는 전역 UI 서비스에 맡기므로, 연출을 바꿔도 이 클래스는 그대로다.
/// 전투력 계산은 HUD 표시와 같은 관문(AccountStats.TotalDeckCombatPower)을 쓰므로 화면 숫자와 항상 일치한다.
///
/// 알리는 변경과 기준값만 갱신하는 변경을 구분한다:
///  - 알림    : 성장/장비/각인/승급(Growth), 보유 목록 변동(Roster), 편성 변경(DeckChanged)
///  - 기준만  : 로그인/메인 진입 스냅샷(Snapshot), 방치 전투 경험치(Exp)
/// 방치 경험치는 몇 초마다 계속 올라 토스트가 화면을 덮는다. 방치분도 알리려면 IsAnnounced 에 Exp 를 더하면 된다.
///
/// 연속 상승은 묶어서 한 번만 띄운다. 스탯 투자 연타(ButtonHoldRepeater)처럼 짧은 시간에 여러 번 오르는 경우
/// 매번 띄우면 화면이 어지럽고 연출도 끊기므로, 상승이 멎을 때까지 기다렸다가 그 구간 전체의 상승분을 합쳐 보여준다.
/// </summary>
public sealed class CombatPowerAnnouncer : IDisposable {
    // 마지막 상승 이후 이만큼 조용하면 묶어서 띄운다(연타가 멎기를 기다리는 시간).
    private const float QuietSeconds = 0.5f;
    // 연타가 계속 이어져도 이 시간이 지나면 일단 한 번 띄운다(버튼을 오래 누르고 있어도 반응이 보이도록).
    private const float MaxWaitSeconds = 2f;

    private readonly MainHudContext _ctx;
    private readonly CancellationToken _ct;

    private long _last;           // 마지막으로 확인한 총 전투력(기준값). 21억을 넘을 수 있어 long
    private bool _hasBaseline;    // 기준값이 실제 데이터로 채워졌는지(최초 로드의 0 -> N 을 상승으로 보지 않기 위함)

    private long _pendingFrom;    // 이번 상승 구간의 시작 전투력
    private bool _hasPending;
    private float _lastGainAt;    // 마지막 상승 시각(unscaled)
    private bool _flushing;

    public CombatPowerAnnouncer(MainHudContext ctx, CancellationToken ct) {
        _ctx = ctx;
        _ct = ct;

        // 이미 데이터가 들어와 있는 상태에서 만들어졌다면 그 값이 곧 기준이다(씬 재진입 등).
        _last = Current;
        _hasBaseline = _last > 0;

        _ctx.Store.CharactersChanged += OnCharactersChanged;
        _ctx.Store.DeckChanged += OnDeckChanged;
    }

    private long Current => (long)Math.Round(AccountStats.TotalDeckCombatPower(_ctx));

    private static bool IsAnnounced(CharacterChangeReason reason) =>
        reason == CharacterChangeReason.Growth || reason == CharacterChangeReason.Roster;

    private void OnCharactersChanged(CharacterChange change) => Evaluate(IsAnnounced(change.Reason));

    private void OnDeckChanged() => Evaluate(true);

    // 알리지 않는 변경에서도 기준값은 반드시 따라간다. 안 그러면 다음 상승분에 그 변화까지 얹혀 나온다.
    private void Evaluate(bool announce) {
        long before = _last;
        long now = Current;
        _last = now;

        // 씬 진입 초기 로딩 구간: 계정 스냅샷 -> 인벤토리 -> 각인 -> 본부가 순차로 도착하며 전투력이 계단식으로 채워진다.
        // 통지 원인만으로는(전부 Growth 다) 구분할 수 없으므로 로딩 상태로 걸러야 한다. 기준만 따라가고 알리지 않는다.
        if (_ctx.Store.IsInitialLoading) {
            _hasBaseline = now > 0;
            _hasPending = false;   // 로딩 직전에 잡아 둔 상승분이 있으면 버린다(로딩 결과와 섞이면 안 된다).
            return;
        }

        // 최초 로드(0 에서 현재값으로 채워지는 계산)는 상승이 아니다. 로딩 상태를 알려주지 않는 경로를 위한 이중 방어.
        if (!_hasBaseline) {
            _hasBaseline = now > 0;
            return;
        }

        if (!announce || now <= before) return;

        if (!_hasPending) {
            _pendingFrom = before;
            _hasPending = true;
        }
        _lastGainAt = Time.unscaledTime;

        if (!_flushing) FlushWhenSettledAsync().Forget();
    }

    // 상승이 멎거나(QuietSeconds) 너무 오래 이어지면(MaxWaitSeconds) 그때까지의 상승분을 한 번에 띄운다.
    private async UniTaskVoid FlushWhenSettledAsync() {
        _flushing = true;
        try {
            float start = Time.unscaledTime;
            while (true) {
                await UniTask.Yield(PlayerLoopTiming.Update, _ct);

                float now = Time.unscaledTime;
                if (now - _lastGainAt >= QuietSeconds || now - start >= MaxWaitSeconds) break;
            }
            Flush();
        } catch (OperationCanceledException) {
            // 씬 종료. 남은 상승분은 버린다.
        } finally {
            _flushing = false;
        }
    }

    private void Flush() {
        if (!_hasPending) return;
        _hasPending = false;

        long from = _pendingFrom;
        long now = Current;
        if (now > from) _ctx.Ui.ShowCombatPowerGain(from, now);
    }

    public void Dispose() {
        if (_ctx == null || _ctx.Store == null) return;
        _ctx.Store.CharactersChanged -= OnCharactersChanged;
        _ctx.Store.DeckChanged -= OnDeckChanged;
    }
}
