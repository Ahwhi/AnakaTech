using UnityEngine;
using VContainer;

/// <summary>
/// MainHudContext 를 주입받는 HUD 컴포넌트의 공통 베이스.
///
/// LifetimeScope 의 AutoInject 는 "씬 빌드 시점 1회"만 동작하므로, 씬에서 오브젝트를 비활성으로 두었다가
/// 런타임에 활성화하는 경우 주입을 놓칠 수 있다(활성화 시점엔 이미 스코프 빌드가 끝난 뒤라 재주입되지 않음).
/// 그래서 이 베이스는 활성화(OnEnable) 시 아직 주입 전이면 현재 씬 스코프에서 직접 해석한다.
///
/// 결과: 이 컴포넌트를 쓰는 오브젝트는 AutoInject 목록에 넣지 않아도 되고, 씬에서 비활성으로 두어도
/// 활성화되는 순간 Ctx 가 채워져 정상 동작한다. Ctx 는 클릭 등 실제 사용 시점에만 참조하면 안전하다.
/// </summary>
public abstract class HudInjectedBehaviour : MonoBehaviour {
    protected MainHudContext Ctx { get; private set; }

    [Inject]
    public void Construct(MainHudContext ctx) => Ctx = ctx;

    protected virtual void OnEnable() {
        if (Ctx == null) {
            // 비활성 -> 활성 전환 시(AutoInject 를 놓친 경우) 씬 스코프에서 직접 주입한다.
            var scope = FindAnyObjectByType<MainLifetimeScope>();
            if (scope != null && scope.Container != null)
                scope.Container.Inject(this);   // [Inject] Construct 를 다시 태워 Ctx 를 채운다.
        }
        if (Ctx != null) OnInjected();
    }

    /// <summary>
    /// Ctx 가 준비된 뒤 호출된다(활성화될 때마다). 상태를 구독해야 하는 컴포넌트가 덮어쓴다.
    /// 켜고 끄기를 반복해도 안전하도록, 덮어쓰는 쪽은 이전 구독을 정리하고 다시 걸어야 한다.
    /// </summary>
    protected virtual void OnInjected() { }
}
