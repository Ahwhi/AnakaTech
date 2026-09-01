using System.Threading;
using Cysharp.Threading.Tasks;
using static Enums;

/// <summary>
/// 부트 시퀀스의 한 단계. State 는 진행 중 표시할 부트 상태이며,
/// Bootstrapper 가 ExecuteAsync 전에 BootStatus 에 기록 → 로딩 UI 갱신에 사용된다.
/// </summary>
public interface IBootStep {
    BootState State { get; }
    UniTask ExecuteAsync(CancellationToken ct);
}
