using UniRx;
using static Enums;

/// <summary>
/// 부트 진행 상태의 읽기 전용 계약. 로딩 UI(TitleSceneManager, LoginPanel)는 이 인터페이스만 주입받아
/// 현재 단계와 다운로드 진행률을 구독한다. ReactiveProperty 라서 구독 시점에 현재 값이 즉시 재생(replay)되므로,
/// 기존처럼 "구독 직후 현재 상태를 수동으로 1회 반영"하는 보일러플레이트가 필요 없다.
/// 쓰기(상태 전이/진행률 보고)는 BootStatus 구현체가 담당하며 외부에 노출하지 않는다.
/// </summary>
public interface IBootStatus {
    IReadOnlyReactiveProperty<BootState> State { get; }
    IReadOnlyReactiveProperty<DownloadInfo> Download { get; }

    // State 가 Failed 일 때 의미를 가진다. 실패 콜백에서 분기/메시지 표시에 사용.
    BootFailReason FailReason { get; }
    string FailMessage { get; }
}
