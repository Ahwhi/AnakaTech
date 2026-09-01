using VContainer;
using VContainer.Unity;

public class TitleLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 타이틀 씬의 뷰를 등록 = 주입 트리거. 이게 비어 있으면 TitleSceneManager/LoginPanel 의
        // [Inject] Construct 가 호출되지 않아 _boot 등이 null 인 채 Start 가 돌아 NullReferenceException 이 난다.
        builder.RegisterComponentInHierarchy<TitleSceneManager>();
        builder.RegisterComponentInHierarchy<LoginPanel>();

        // 로그인 완료 시 버튼 패널 노출 + 그 패널 안의 로그아웃 버튼.
        builder.RegisterComponentInHierarchy<LoginCompletePanel>();
        builder.RegisterComponentInHierarchy<LogoutButton>();
    }
}
