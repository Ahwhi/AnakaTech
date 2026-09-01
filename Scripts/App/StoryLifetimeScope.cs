using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 스토리 씬 컴포지션 루트. 루트 스코프(GameLifetimeScope)의 자식으로 동작한다.
///
/// 스토리는 전투도 계정 데이터도 쓰지 않으므로 등록할 것이 스토리 데이터베이스 하나뿐이다
/// (통신/씬 전환/팝업은 전부 루트 스코프의 영속 서비스를 그대로 쓴다). 다른 씬 스코프들과 달리
/// 이 씬이 가벼운 이유이고, 그래서 인트로를 메인 씬 앞에 끼워도 로딩이 눈에 띄게 늘지 않는다.
///
/// 인스펙터 할당: storyDatabase + Parent > Type = GameLifetimeScope.
/// 씬 배치: StorySceneManager 가 이 씬 하이어라키에 있어야 자동 주입된다(StoryPlayerView 는 그 인스펙터 참조로 연결한다).
/// </summary>
public sealed class StoryLifetimeScope : LifetimeScope {
    [Header("Domain Databases (SO)")]
    [Tooltip("스토리 데이터베이스. 재생할 스토리를 키로 찾는다.")]
    [SerializeField] private StoryDatabase storyDatabase;

    protected override void Configure(IContainerBuilder builder) {
        if (storyDatabase == null)
            Debug.LogError("[StoryLifetimeScope] 인스펙터 미할당: storyDatabase");

        // 미할당이어도 씬 전체가 죽지 않도록 빈 데이터베이스로 폴백한다
        // (스토리를 찾지 못하면 StorySceneManager 가 다음 씬으로 넘겨 흐름을 잇는다).
        builder.RegisterInstance(storyDatabase != null ? storyDatabase : ScriptableObject.CreateInstance<StoryDatabase>());

        // StoryPlayerView 는 주입받는 것이 없어 등록하지 않는다(StorySceneManager 가 인스펙터 참조로 구동한다).
        builder.RegisterComponentInHierarchy<StorySceneManager>();
    }
}
