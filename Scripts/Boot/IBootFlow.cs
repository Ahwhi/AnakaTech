using Cysharp.Threading.Tasks;

/// <summary>
/// 부트 시퀀스 실행 계약. 앱 시작 시(타이틀 진입)와 재접속 후 타이틀 복귀 시 모두 이 메서드로 부트를 (재)실행한다.
/// 구현체(Bootstrapper)는 재진입 가드를 두어 중복 실행을 막고, 시작 시 진행 상태를 초기화한다.
/// 호출 예) 타이틀 화면 진입 시 _bootFlow.RunAsync().Forget(); / 연결 실패 재시도 시 동일 호출.
/// </summary>
public interface IBootFlow {
    UniTask RunAsync();
}
