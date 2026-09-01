using Cysharp.Threading.Tasks;

/// <summary>
/// 로그아웃 처리 계약. 어느 씬에서든 주입받아 호출한다(루트에 등록된 전역 서비스).
/// 소켓을 끊고 타이틀로 복귀해 재로그인 흐름으로 돌려보낸다. 의도적 종료이므로 연결 끊김 에러 팝업은 띄우지 않는다.
/// </summary>
public interface ILogoutService {
    /// <summary>로그아웃 진행 중인지. ConnectionGuard 가 의도적 끊김을 에러로 오인하지 않도록 참고한다.</summary>
    bool IsLoggingOut { get; }

    UniTask LogoutAsync();
}
