using System;
using VContainer;
using VContainer.Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Enums;

/// <summary>
/// 앱 전역 컴포지션 루트(VContainer LifetimeScope).
/// 기존 AppDirector 의 "정적 싱글톤 서비스 로케이터" 역할을 대체한다. 더 이상 AppDirector.I 로
/// 전역 접근하지 않고, 여기서 등록한 의존성이 각 소비자에게 생성자/메서드 주입된다.
///
/// 등록 대상:
///   1) 플랫폼 값 (버전 체크 스텝이 주입받음)
///   2) 매니저(MonoBehaviour) 를 서비스 인터페이스로 노출 (구현체 은닉)
///   3) 부트 진행 상태 BootStatus (읽기/진행률쓰기 분리)
///   4) 부트 단계 IBootStep 들 (등록 순서 = 실행 순서)
///   5) 부트 오케스트레이터 Bootstrapper (IBootFlow: 타이틀 진입/재접속 복귀 시 RunAsync 로 (재)실행)
///   6) ConnectionGuard (게임 플레이 중 끊김 -> 타이틀 복귀)
///
/// 타이틀 씬 뷰(TitleSceneManager/LoginPanel)는 이 루트가 아니라 타이틀 자식 스코프(TitleLifetimeScope)가 등록한다.
/// 그래야 재접속으로 타이틀을 다시 로드할 때 새로 생긴 뷰들이 다시 주입된다(루트는 앱 시작 때 한 번만 빌드됨).
///
/// 배치: 이 컴포넌트(+매니저들+UIRoot+AppLifecycle)는 "재로드되지 않는" 전용 Bootstrap 씬에 두고,
/// Awake 에서 DontDestroyOnLoad 로 살린 뒤 AppEntryPoint 가 TitleScene 을 로드한다. 부트 오브젝트가
/// 재로드되는 씬에 있으면 중복 생성/자식 스코프 부모 탐색 경합이 생기므로, 전용 씬으로 분리해 단일 인스턴스를 보장한다.
///
/// 인스펙터에 매니저 4종 + UIRoot 를 할당한다.
/// </summary>
public sealed class GameLifetimeScope : LifetimeScope {
    [Header("Persistent Managers")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private LoginManager loginManager;
    [SerializeField] private SoundManager soundManager;
    [SerializeField] private AddressableManager addressableManager;

    [Header("UI")]
    [SerializeField] private UIRoot uiRoot;
    [Tooltip("공용 팝업 프리팹(AssetReference) 카탈로그. 팝업을 띄우는 소비자에게 주입된다.")]
    [SerializeField] private UIPopupCatalog uiPopupCatalog;

    [Header("Notice")]
    [SerializeField] private SupabaseConfig supabaseConfig;

    [Header("Version")]
    [Tooltip("원격 앱 설정(RemoteAppConfig, Remote_Config 그룹 + preload_boot). 콘텐츠 버전 등 콘텐츠와 함께 배포되는 값. 부트에서 로드해 표시 버전 뒷자리로 쓴다.")]
    [SerializeField] private AssetReference remoteAppConfigRef;

    protected override void Awake() {
        // 컨테이너 빌드(및 빌드 직후 실행될 AppEntryPoint 의 씬 로드)보다 먼저 부트 오브젝트를 영속화한다.
        // 이 순서를 보장해야 AppEntryPoint 가 TitleScene 을 Single 로드할 때 부트 오브젝트가 함께 언로드되지 않는다.
        DontDestroyOnLoad(gameObject);
        base.Awake();
    }

    protected override void Configure(IContainerBuilder builder) {
        // 1) 플랫폼 값 (VersionCheckStep 생성자 주입용)
        builder.RegisterInstance(DetectPlatform());

        // 1-1) 버전 표시 - 앞자리 Application.version(빌드) + 뒷자리 RemoteAppConfig.contentVersion(어드레서블).
        //      ContentVersionStep(부트에서 로드/설정)과 TitleSceneManager(표시)가 같은 인스턴스를 공유한다.
        builder.RegisterInstance(remoteAppConfigRef);
        builder.Register<VersionInfo>(Lifetime.Singleton).As<IVersionInfo>();

        // 2) 매니저를 서비스 인터페이스로 노출 - 소비자는 구현체가 아닌 계약에만 의존
        builder.RegisterComponent(networkManager).As<INetworkService>();
        builder.RegisterComponent(loginManager).As<ILoginService>();
        builder.RegisterComponent(soundManager).As<ISoundService>();
        builder.RegisterComponent(addressableManager).As<IAssetService>();
        builder.RegisterComponent(uiRoot).As<IUIService>();

        // 2-0) 공용 팝업 카탈로그 - 인스펙터에서 팝업 프리팹(AssetReference)을 할당한 에셋.
        //      문자열 주소 키 대신 이 카탈로그를 주입받아 ShowPopupAsync 에 참조를 넘긴다.
        builder.RegisterInstance(uiPopupCatalog);

        // 2-1) 씬 전환 서비스 - 영속 서비스라 루트에 둔다(전환 중 로딩 오버레이 표시).
        builder.Register<SceneService>(Lifetime.Singleton).As<ISceneService>();

        // 2-1-2) 로그아웃 - 어느 씬에서든 호출. 소켓 끊기 + 타이틀 복귀.
        builder.Register<LogoutService>(Lifetime.Singleton).As<ILogoutService>();

        // 2-1-3) 게임 종료 - 확인 팝업 후 종료. 뒤로가기(BackKeyRouter)와 화면의 종료 버튼(QuitButton)이 함께 쓴다.
        builder.Register<QuitService>(Lifetime.Singleton).As<IQuitService>();

        // 2-2) 공지/이벤트 - TCP 게임 서버와 별개의 HTTP(Supabase) 백엔드. 앱 어디서든 열 수 있도록 루트에 등록.
        //      설정 에셋(SupabaseConfig)을 인스펙터에서 할당하고, 조회 서비스와 표시 프레젠터를 노출한다.
        builder.RegisterInstance(supabaseConfig);
        builder.Register<SupabaseNoticeService>(Lifetime.Singleton).As<INoticeService>();
        builder.Register<NoticePresenter>(Lifetime.Singleton).As<INoticePresenter>();

        // 2-2-1) 약관 동의(가입 게이트) - 동의 대상/기록/문서 주소는 전부 게임 서버(패킷)가 내려주고,
        //        본문은 그 주소의 공식 홈페이지를 앱 내부 웹뷰로 보여 준다(TermsDetailPopup).
        //        부트 스텝과 설정 화면이 같은 게이트를 재사용하므로 루트에 둔다.
        builder.Register<TermsService>(Lifetime.Singleton).As<ITermsService>();
        // 원격 푸시 발송 주소. 광고성 수신 동의가 곧 사용 여부이므로 동의 흐름보다 먼저 등록해 주입한다
        // (조정자는 동의 흐름이 아니라 그보다 아래인 약관 통신만 참조해 순환 의존이 생기지 않는다).
        builder.Register<PushTokenService>(Lifetime.Singleton).As<IPushTokenService>();
        builder.Register<PushCoordinator>(Lifetime.Singleton).As<IPushCoordinator>();
        builder.Register<TermsConsentFlow>(Lifetime.Singleton).As<ITermsConsentFlow>();
        // 회원 탈퇴. 약관과 생명주기가 달라 계약을 분리하되, 설정 화면이 함께 쓰므로 같은 자리에 둔다.
        builder.Register<AccountService>(Lifetime.Singleton).As<IAccountService>();

        // 2-3) 이벤트 배지(미열람/충족) - 열람은 로컬 데이터, 충족은 서버가 내려준 상태.
        builder.Register<LocalEventReadStore>(Lifetime.Singleton).As<IEventReadStore>();
        builder.Register<EventStatusProvider>(Lifetime.Singleton).As<IEventStatusProvider>();
        builder.Register<EventBadgeResolver>(Lifetime.Singleton).As<IEventBadgeResolver>();
        builder.Register<EventStatusSync>(Lifetime.Singleton).As<IEventStatusSync>();

        // 버튼 알림등(Unread=NEW / Claimable=빨간 동그라미) 집계 + 출석체크 전용 통신.
        builder.Register<EventBadgeService>(Lifetime.Singleton).As<IEventBadgeService>();
        builder.Register<AttendanceService>(Lifetime.Singleton).As<IAttendanceService>();
        builder.Register<EventMissionService>(Lifetime.Singleton).As<IEventMissionService>();

        // 접속 유지 시간 적립(하트비트). 씬과 무관하게 계속 돌아야 하므로 루트에 둔다.
        builder.Register<HeartbeatService>(Lifetime.Singleton).As<IHeartbeatService>();

        // 2-4) 스토리(인트로 등 연출 대화) - 재생 여부는 타이틀이 묻고 완료 보고는 스토리 씬이 한다.
        //      서로 다른 씬이 같은 사실(본 스토리 목록)을 봐야 하므로 씬보다 오래 사는 루트에 둔다.
        builder.Register<StoryService>(Lifetime.Singleton).As<IStoryService>();

        // 3) 부트 진행 상태 - 하나의 인스턴스를 세 가지 역할로 노출(읽기/진행률/상태쓰기)
        builder.Register<BootStatus>(Lifetime.Singleton)
               .As<IBootStatus>()
               .As<IProgress<DownloadInfo>>()
               .AsSelf();

        // 다운로드 동의 게이트 - 부트 스텝(요청)과 타이틀 UI(응답)를 이어준다(역할별 인터페이스 분리).
        builder.Register<DownloadConsent>(Lifetime.Singleton)
               .As<IDownloadConsent>()
               .As<IDownloadConsentPresenter>();

        // 4) 부트 단계 - "등록 순서"가 곧 "실행 순서". 단계 추가/제거/재배치는 이 블록만 수정하면 된다.
        builder.Register<AppInitializeStep>(Lifetime.Singleton).As<IBootStep>();
        builder.Register<ConnectStep>(Lifetime.Singleton).As<IBootStep>();
        builder.Register<VersionCheckStep>(Lifetime.Singleton).As<IBootStep>();
        builder.Register<AddressableStep>(Lifetime.Singleton).As<IBootStep>();
        builder.Register<ContentVersionStep>(Lifetime.Singleton).As<IBootStep>();   // 다운로드 직후 콘텐츠 버전 로드 -> 표시 반영
        builder.Register<LoginStep>(Lifetime.Singleton).As<IBootStep>();
        builder.Register<TermsStep>(Lifetime.Singleton).As<IBootStep>();             // 로그인으로 계정이 생긴 직후 약관 동의를 받는다

        // 5) 부트 오케스트레이터 - 자동 실행이 아니라 타이틀이 RunAsync 로 (재)호출한다.
        builder.Register<Bootstrapper>(Lifetime.Singleton).As<IBootFlow>();

        // 6) 연결 끊김 가드(엔트리포인트) - 앱 내내 Disconnected 를 감시
        builder.RegisterEntryPoint<ConnectionGuard>();

        // 6-1) 안드로이드 뒤로가기 창구(엔트리포인트) - 팝업 닫기 -> 없으면 게임 종료 확인.
        builder.RegisterEntryPoint<BackKeyRouter>();

        // 6-2) UI 클릭음 창구(엔트리포인트) - 버튼/토글 클릭을 한 곳에서 듣고 클릭음을 낸다.
        //      새 버튼마다 사운드 코드를 붙이지 않아도 되고, 화면마다 소리가 빠지는 일이 없어진다.
        builder.RegisterEntryPoint<UIClickSfxRouter>();

        // 7) 앱 진입점(엔트리포인트) - 컨테이너 빌드 직후 TitleScene 을 로드한다.
        builder.RegisterEntryPoint<AppEntryPoint>();
    }

    private static PlatformType DetectPlatform() {
#if UNITY_ANDROID
        return PlatformType.Android;
#elif UNITY_IOS
        return PlatformType.IOS;
#else
        return PlatformType.Windows;
#endif
    }
}
