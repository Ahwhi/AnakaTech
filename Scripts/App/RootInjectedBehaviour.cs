using UnityEngine;

/// <summary>
/// 루트(GameLifetimeScope)에 등록된 전역 서비스만 필요한 컴포넌트의 공통 베이스.
///
/// 이런 컴포넌트(종료 버튼, 사운드 토글 등)는 어느 씬에나 붙을 수 있는데, 씬마다 스코프에 등록해 주어야 한다면
/// 붙일 때마다 인스펙터 작업이 따라오고 등록을 잊으면 주입이 비어 조용히 동작하지 않는다. 그래서 활성화 시점에
/// 루트 스코프를 찾아 스스로 주입받는다. 루트 스코프는 DontDestroyOnLoad 로 앱에 하나뿐이라 대상이 모호하지 않다.
///
/// 결과: 이 베이스를 쓰는 컴포넌트는 아무 오브젝트에 붙이기만 하면 어느 씬에서든 동작하고,
/// 씬 스코프에는 등록하지 않는다(등록하면 주입이 두 번 일어난다).
/// 씬 전용 의존성(MainHudContext 등)이 필요한 HUD 컴포넌트는 HudInjectedBehaviour 를 쓴다.
/// </summary>
public abstract class RootInjectedBehaviour : MonoBehaviour {
    private bool _injected;

    /// <summary>주입이 끝났는지. 주입 전에는 서비스 참조가 비어 있으므로 사용 시점에 확인한다.</summary>
    protected bool IsInjected => _injected;

    protected virtual void OnEnable() {
        if (_injected) return;

        var scope = FindAnyObjectByType<GameLifetimeScope>();
        if (scope == null || scope.Container == null) return;   // 아직 컨테이너 빌드 전이면 다음 활성화 때 다시 시도한다

        scope.Container.Inject(this);   // [Inject] Construct 를 태워 의존성을 채운다
        _injected = true;
        OnInjected();
    }

    /// <summary>주입이 끝난 직후 1회 호출된다. 구독/초기 표시가 필요한 컴포넌트가 덮어쓴다.</summary>
    protected virtual void OnInjected() { }
}
