using System;
using UniRx;
using UnityEngine;
using static Enums;

/// <summary>
/// 부트 진행 상태의 단일 소유자(Single Source of Truth).
/// 역할별로 인터페이스를 분리(인터페이스 분리 원칙)해 "누가 무엇을 할 수 있는지"를 타입으로 표현한다.
///   - IBootStatus    : 읽기(로딩 UI 가 구독)
///   - IProgress&lt;float&gt; : 진행률 보고(AddressableStep 이 부트 도메인을 모른 채 진행률만 통지)
///   - SetState       : 상태 전이(부트 오케스트레이터 Bootstrapper 만 호출)
///
/// Bootstrapper 와 AddressableStep 이 동시에 의존하더라도 BootStatus 는 아무것도 의존하지 않으므로
/// 생성자 순환이 생기지 않는다(이 분리가 DI 상에서 순환을 끊는 핵심).
/// VContainer 에 Singleton 으로 등록하고 위 세 타입으로 노출한다.
/// </summary>
public sealed class BootStatus : IBootStatus, IProgress<DownloadInfo> {
    private readonly ReactiveProperty<BootState> _state = new(BootState.None);
    private readonly ReactiveProperty<DownloadInfo> _download = new(DownloadInfo.Zero);

    public IReadOnlyReactiveProperty<BootState> State => _state;
    public IReadOnlyReactiveProperty<DownloadInfo> Download => _download;

    public BootFailReason FailReason { get; private set; }
    public string FailMessage { get; private set; }

    /// <summary>부트 오케스트레이터가 단계 진입 시 호출한다.</summary>
    public void SetState(BootState state) {
        _state.Value = state;
        Debug.Log($"[BOOT] {state} - {state.ToMessage()}");
    }

    /// <summary>실패 사유를 기록하고 Failed 상태로 전이한다. 재시작 시 ResetForRerun 으로 초기화된다.</summary>
    public void SetFailed(BootFailReason reason, string message = null) {
        FailReason = reason;
        FailMessage = message;
        SetState(BootState.Failed);
    }

    /// <summary>부트 재실행 직전 진행/실패 상태를 초기화한다.</summary>
    public void ResetForRerun() {
        FailReason = BootFailReason.None;
        FailMessage = null;
        _download.Value = DownloadInfo.Zero;
        _state.Value = BootState.None;
    }

    /// <summary>다운로드 진행 정보(받은/전체 바이트 + 퍼센트) 보고. IProgress&lt;DownloadInfo&gt; 구현이라 소비자는 표준 인터페이스에만 의존한다.</summary>
    public void Report(DownloadInfo value) => _download.Value = value;
}
