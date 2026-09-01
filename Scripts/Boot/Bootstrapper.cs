using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Enums;

/// <summary>
/// 부트 시퀀스 오케스트레이터(IBootFlow 구현). 더 이상 자동 실행 엔트리포인트가 아니라,
/// 타이틀 화면이 진입할 때(앱 시작/재접속 복귀) RunAsync 로 (재)실행한다. 이로써 "타이틀로 돌아가면
/// 부트를 처음부터 다시" 가 자연스럽게 성립한다.
///
/// 주입: 등록 순서대로 실행할 IBootStep 들 + 진행/실패 상태를 기록할 BootStatus.
/// 실패는 사유(BootFailReason)로 분류해 BootStatus 에 남기고, 타이틀 UI 가 그걸 보고 팝업을 띄운다.
/// </summary>
public sealed class Bootstrapper : IBootFlow, IDisposable {
    private readonly IReadOnlyList<IBootStep> _steps;
    private readonly BootStatus _status;
    private readonly CancellationTokenSource _appCts = new();   // 앱 수명(컨테이너 폐기 시 취소)

    private bool _running;   // 재진입 가드

    public Bootstrapper(IReadOnlyList<IBootStep> steps, BootStatus status) {
        _steps = steps;
        _status = status;
    }

    public async UniTask RunAsync() {
        if (_running) return;   // 중복 트리거(타이틀 재진입과 재시도가 겹치는 등) 무시
        _running = true;

        var ct = _appCts.Token;
        // 어느 단계에서 실패했는지 남기기 위해 진행 중인 단계를 기억한다.
        // 실패 사유(BootFailReason)는 종류가 적어 "서버에 연결할 수 없습니다" 하나로 뭉뚱그려지기 쉬운데,
        // 실제 원인은 자산 로드 실패나 특정 패킷 무응답처럼 전혀 다른 곳일 수 있다.
        IBootStep current = null;
        try {
            _status.ResetForRerun();
            MemoryProbe.Mark("부트 시작");
            // 단계마다 계측 지점을 남긴다. 단계 목록이 바뀌어도 여기 한 줄이 따라가므로 스텝 코드는 진단을 모른다.
            // 빌드에서는 MemoryProbe 호출이 컴파일 단계에서 사라진다.
            foreach (var step in _steps) {
                current = step;
                _status.SetState(step.State);
                await step.ExecuteAsync(ct);
                MemoryProbe.Mark($"부트 단계 {step.State}");
            }
            current = null;
            _status.SetState(BootState.Ready);
        } catch (ForceUpdateException) {
            // 취소/완료가 비-메인 스레드에서 재개됐을 수 있으므로, Unity API(상태 갱신->UI 구독) 전에 메인으로 복귀한다.
            await UniTask.SwitchToMainThread();
            _status.SetFailed(BootFailReason.ForceUpdate);
        } catch (MaintenanceException) {
            await UniTask.SwitchToMainThread();
            _status.SetFailed(BootFailReason.Maintenance);
        } catch (DownloadDeclinedException) {
            // 사용자가 다운로드를 거절해 앱을 종료하는 정상 흐름. 실패 팝업 없이 조용히 끝낸다.
        } catch (TermsDeclinedException) {
            // 사용자가 약관 동의를 거부해 앱을 종료하는 정상 흐름. 위와 같은 이유로 조용히 끝낸다.
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            // 앱 종료 등 외부 취소 - 정상 종료라 실패로 처리하지 않는다.
        } catch (OperationCanceledException) {
            // 단계 내부 타임아웃(연결/응답 시간 초과)은 예상된 실패 시나리오다.
            // 예외 로그(빨간 에러) 없이 연결 실패로 분류한다(Failed 전이는 BootStatus 가 Debug.Log 로 남김).
            await UniTask.SwitchToMainThread();
            Debug.LogWarning($"[BOOT] {current?.State} 단계에서 응답 시간이 초과되었습니다.");
            _status.SetFailed(BootFailReason.CannotConnect, $"{current?.State} 응답 없음");
        } catch (Exception e) {
            // 그 외 진짜 예상치 못한 오류만 예외 로그로 남긴다.
            await UniTask.SwitchToMainThread();
            Debug.LogError($"[BOOT] {current?.State} 단계에서 오류가 발생했습니다.");
            Debug.LogException(e);
            _status.SetFailed(BootFailReason.CannotConnect, $"{current?.State} 오류: {e.GetType().Name}");
        } finally {
            _running = false;
        }
    }

    public void Dispose() {
        _appCts.Cancel();
        _appCts.Dispose();
    }
}
