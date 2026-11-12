using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

/// <summary>
/// 버튼/팝업 알림 배지의 상태 허브. 두 종류를 한곳에서 계산해 각 배지(BadgeView)가 구독한다.
///   - Claimable(빨간 동그라미): 수령/처리할 게 있음. 도감/충전 보상 수령 가능, 미수령 우편 존재 등.
///   - Unread(NEW 뱃지): 로컬 "봤음"(UserDataManager JSON)과 현재 상태 비교. 가챠 로테이션 변경 / 상점 신규 오퍼 / 새 캐릭.
///
/// 도감 보상은 진행도(보유 캐릭/총 별레벨)가 스토어에서 즉시 바뀌므로 CharactersChanged 로 재계산(무통신),
/// 수령 여부/충전액/로테이션/오퍼는 RefreshAsync(서버 조회)로 최신화한다. 여러 배지가 동시에 호출해도 1회만 수행한다.
/// 우편은 MailService.UnreadCount(ReactiveProperty)를 구독해 값이 바뀌면(수령/최신화) 즉시 재계산한다.
///
/// 슬라이드 메뉴(햄버거) 안의 버튼 배지는 HamburgerMenuBadge 로 종합한다. 그 안 버튼 중 하나라도 켜지면
/// 메인 화면 햄버거 버튼에 같은 불이 켜진다(도감 배지가 메인에 뜨는 것과 동일한 "공용 소스 구독" 방식).
///
/// 이벤트 배지(EventBadgeService)도 여기로 통과시킨다. 계산은 그 서비스가 그대로 하고 여기서는 창구만 합쳐,
/// 버튼 쪽(BadgeView)이 "배지 상태는 BadgeCenter 하나만 보면 된다"가 되도록 한다.
/// 새 배지는 여기 상태 하나 + BadgeView.BadgeId 하나만 추가하면 된다(확장 포인트).
/// </summary>
public sealed class BadgeCenter : IDisposable {
    // 루트 스코프(씬보다 오래 사는) 서비스에 건 구독. 씬 스코프인 이 클래스가 스코프와 함께 사라질 때
    // 반드시 끊어야 한다. 끊지 않으면 메인 씬을 다시 열 때마다 죽은 BadgeCenter 가 하나씩 쌓여
    // 같은 통지에 여러 번 반응한다(씬 스코프 서비스는 스코프와 함께 사라지므로 여기 담지 않는다).
    private readonly CompositeDisposable _rootSubscriptions = new();
    private readonly IEncyclopediaService _encyclopedia;
    private readonly IChargeRewardService _charge;
    private readonly IShopService _shop;
    private readonly IMailService _mail;
    private readonly PlayerStateStore _store;
    private readonly IEventBadgeService _events;
    private readonly HqNodeDatabase _hqDb;
    private readonly CharacterDatabase _charDb;   // 도감 보상 단계 수(캐릭터 수에서 정해진다)
    private readonly IGuildService _guild;
    private readonly IFriendService _friend;
    private readonly IAchievementService _achievement;
    private readonly ITrainingService _training;
    private readonly IArenaService _arena;
    private readonly IDispatchService _dispatch;
    private readonly IDoomService _doom;
    private readonly IStoryService _story;
    private readonly IPassService _pass;
    private readonly IExpeditionService _expedition;
    private readonly ITowerService _tower;
    private readonly IHeroesService _heroes;
    private readonly IFrontlineService _frontline;

    private readonly Subject<Unit> _changed = new();

    // 서버 조회 캐시.
    private HashSet<int> _claimedCollection = new();
    private HashSet<int> _claimedStar = new();
    private HashSet<int> _claimedTiers = new();
    private int _cumulativeWon;
    private int _rotationCharId;
    private readonly List<(string id, string major)> _offers = new();   // 상점 오퍼 id + 대분류(카테고리별 New 판정용)
    private Dictionary<string, long> _freeNextUnix = new();             // 무료 뽑기 키 -> 다음 가능 시각(UTC epoch)
    private int _guildPendingApplications;   // 처리 대기 중인 가입 신청 수(운영진 이상일 때만 서버가 채운다)
    private int _guildRaidFreeEntries;       // 오늘 남은 무료 레이드 참여 횟수
    private bool _refreshing;
    private bool _loaded;   // 최초 서버 조회 완료 여부(그 전엔 도감 로컬 재계산으로 오탐 배지를 띄우지 않는다).

    public BadgeCenter(IEncyclopediaService encyclopedia, IChargeRewardService charge, IShopService shop,
                       IMailService mail, PlayerStateStore store, IEventBadgeService events,
                       HqNodeDatabase hqDb, CharacterDatabase charDb, IGuildService guild, IFriendService friend,
                       IAchievementService achievement, ITrainingService training, IArenaService arena,
                       IDispatchService dispatch, IDoomService doom, IStoryService story,
                       IPassService pass, IExpeditionService expedition, ITowerService tower,
                       IHeroesService heroes, IFrontlineService frontline) {
        _encyclopedia = encyclopedia;
        _charge = charge;
        _shop = shop;
        _mail = mail;
        _store = store;
        _events = events;
        _hqDb = hqDb;
        _charDb = charDb;
        _guild = guild;
        _friend = friend;
        _achievement = achievement;
        _training = training;
        _arena = arena;
        _dispatch = dispatch;
        _doom = doom;
        _story = story;
        _pass = pass;
        _expedition = expedition;
        _tower = tower;
        _heroes = heroes;
        _frontline = frontline;
        // 이벤트 배지가 바뀌면(읽음 처리/서버 최신화) 그대로 흘려보낸다. 구독자는 BadgeCenter 만 보면 된다.
        if (_events != null) _events.Changed.Subscribe(_ => _changed.OnNext(Unit.Default));
        // 보유 캐릭/별레벨/장비 변경 시 도감 + 덱 배지 재계산. 다이아 변동도 덱(별 승급 가능)에 영향.
        // 배지는 "무엇이 바뀌었는지"와 무관하게 항상 재계산하므로 CharacterChange 인자는 무시한다.
        if (_store != null) {
            _store.CharactersChanged += _ => OnStoreChanged();
            _store.Diamond.Subscribe(_ => OnStoreChanged());
            // 한계돌파권이 생기거나 쓰이면 한계돌파 가능 여부(덱 배지)가 달라진다.
            _store.LimitBreakTicket.Subscribe(_ => OnStoreChanged());
            // 의뢰해결권/명성은 재화라 값 구독으로, 본부 노드 레벨은 값이 아니라 목록이라 변경 통지로 받는다.
            _store.CommissionTicket.Subscribe(_ => { RecomputeCommission(); _changed.OnNext(Unit.Default); });
            _store.StageSweepTicket.Subscribe(_ => { RecomputeStageSweep(); _changed.OnNext(Unit.Default); });
            _store.Renown.Subscribe(_ => { RecomputeHeadquarters(); _changed.OnNext(Unit.Default); });
            _store.HeadquartersChanged += () => { RecomputeHeadquarters(); _changed.OnNext(Unit.Default); };
            // 스테이지를 처음 깨면 그 자리에서 열리는 스토리가 생긴다. 해금 판정은 서버가 하므로 진도가 오를 때
            // 다시 물어본다(팝업을 열어야 불이 들어오면 유저는 열린 줄을 모른다).
            // 최초 로드 중에는 건너뛴다. 그때는 RefreshAsync 가 이미 받아 온다.
            _store.HighestClearedDungeonIndex.Subscribe(_ => {
                if (_loaded && _story != null) _story.RefreshAsync().Forget();
            });
        }
        // 미수령 우편 수가 바뀌면(최신화/수령) 즉시 우편 배지 재계산 후 알린다(햄버거 종합 배지도 함께 갱신).
        if (_mail != null)
            _mail.UnreadCount.Subscribe(_ => { RecomputeMail(); _changed.OnNext(Unit.Default); });
        // 받은 친구 신청 수가 바뀌면(서버 푸시/수락/거절/목록 조회) 즉시 재계산한다.
        // 메인 친구 버튼과 친구 신청 탭 배지가 같은 값을 보므로 한쪽만 켜지는 어긋남이 생기지 않는다.
        if (_friend != null)
            _friend.PendingRequests.Subscribe(_ => { RecomputeFriend(); _changed.OnNext(Unit.Default); });
        // 기력 선물(보낼 친구 / 받을 기력)도 메인 친구 버튼 배지의 근거다.
        if (_friend != null)
            _friend.StaminaState.Subscribe(_ => { RecomputeFriend(); _changed.OnNext(Unit.Default); });
        // 수령 가능한 업적 보상이 생기거나 사라지면(조회/수령) 즉시 재계산한다.
        // 보상 탭 배지와 메인 업적 버튼 배지가 같은 값을 보므로 한쪽만 켜지는 어긋남이 없다.
        if (_achievement != null)
            _achievement.HasClaimableReward.Subscribe(_ => { RecomputeAchievement(); _changed.OnNext(Unit.Default); });
        // 완료 대기 중인 수련이 생기거나(시간이 다 됨) 사라지면(완료 수령) 즉시 재계산한다.
        // 판정은 TrainingService 가 시간으로 하므로 팝업이 열려 있지 않아도 배지가 저절로 켜진다.
        if (_training != null)
            _training.HasClaimable.Subscribe(_ => { RecomputeTraining(); _changed.OnNext(Unit.Default); });
        // 오늘 무료 매칭이 남아 있으면 쟁탈전 버튼에 불이 켜진다(매칭할 때마다 값이 줄어 저절로 꺼진다).
        if (_arena != null)
            _arena.HasFreeEntry.Subscribe(_ => { RecomputeArena(); _changed.OnNext(Unit.Default); });
        // 완료 대기 중인 파견이 생기거나 사라지면 즉시 재계산한다(수련과 같은 근거/같은 구조).
        if (_dispatch != null)
            _dispatch.HasClaimable.Subscribe(_ => { RecomputeDispatch(); _changed.OnNext(Unit.Default); });
        // 멸망전: 도달한 구간 중 안 받은 보상이 생기거나(판 종료) 사라지면(수령) 즉시 재계산한다.
        // 판정 근거인 canClaim 을 서버 응답마다 서비스가 갱신하므로, 재접속 없이 그 자리에서 불이 켜진다.
        // 오늘 무료 참여가 남아 있어도 켜진다(쟁탈전 무료 매칭과 같은 "놀고 있는 기회" 안내). 유료 참여는 근거가 아니다.
        if (_doom != null) {
            _doom.HasClaimable.Subscribe(_ => { RecomputeDoom(); _changed.OnNext(Unit.Default); });
            _doom.HasFreeEntry.Subscribe(_ => { RecomputeDoom(); _changed.OnNext(Unit.Default); });
        }
        // 스토리: 읽을 수 있는 편이 생기거나(스테이지 클리어) 미수령 보상이 생기면/사라지면 즉시 재계산한다.
        // StoryService 는 루트 스코프라(GameLifetimeScope) 구독을 반드시 끊어야 한다.
        if (_story != null)
            _story.HasBadge.Subscribe(_ => { RecomputeStory(); _changed.OnNext(Unit.Default); })
                  .AddTo(_rootSubscriptions);
        // 패스: 기력을 써서 새 레벨에 닿으면 서버가 현황을 밀어 주고(S_PassInfo), 서비스가 그 값으로 이 속성을 바꾼다.
        // 그래서 패스 화면을 열거나 재접속하지 않아도 조건을 채우는 순간 패스 버튼과 햄버거 버튼에 불이 켜진다.
        if (_pass != null)
            _pass.HasClaimable.Subscribe(_ => { RecomputePass(); _changed.OnNext(Unit.Default); });
        // 원정대: 오늘 칠 수 있으면 불이 켜지고, 출격하면(입장 응답) 그 자리에서 꺼진다.
        if (_expedition != null)
            _expedition.CanEnterToday.Subscribe(_ => { RecomputeExpedition(); _changed.OnNext(Unit.Default); });
        // 무한 타워: 오늘 오를 수 있으면 불이 켜지고, 등반하면(등반 응답) 그 자리에서 꺼진다.
        if (_tower != null)
            _tower.CanClimbToday.Subscribe(_ => { RecomputeTower(); _changed.OnNext(Unit.Default); });
        // 영웅전: 오늘 도전 횟수가 남아 있으면 불이 켜지고, 다 쓰면(입장 응답) 그 자리에서 꺼진다.
        if (_heroes != null)
            _heroes.CanEnterToday.Subscribe(_ => { RecomputeHeroes(); _changed.OnNext(Unit.Default); });
        // 전선: 오늘 남은 전선 기력(현황 응답마다 갱신)과 보유 기력(기력 변동마다 갱신) 둘 다 1회분 이상이면 켜진다.
        // 기력은 시간으로 차오르므로 전선 화면을 열지 않아도 1회분이 모이는 순간 불이 켜진다.
        if (_frontline != null)
            _frontline.Info.Subscribe(_ => { RecomputeFrontline(); _changed.OnNext(Unit.Default); });
        if (_store != null)
            _store.Stamina.Subscribe(_ => { RecomputeFrontline(); _changed.OnNext(Unit.Default); });
    }

    // 현재 계정의 New "봤음" 상태. 계정 분리는 로컬 세이브(UserDataManager)가 책임지므로 여기서는 창구만 쓴다.
    private AccountLocalData Seen => UserDataManager.Account;

    public EventBadge EncyclopediaBadge { get; private set; } = EventBadge.None;
    public EventBadge ChargeBadge { get; private set; } = EventBadge.None;
    public EventBadge GachaNewBadge { get; private set; } = EventBadge.None;
    public EventBadge ShopNewBadge { get; private set; } = EventBadge.None;
    public EventBadge DeckBadge { get; private set; } = EventBadge.None;   // 육성 가능(빨간 동그라미) 또는 새 캐릭(NEW). Claimable 우선.
    public EventBadge MailBadge { get; private set; } = EventBadge.None;   // 미수령 우편 존재(빨간 동그라미).
    public EventBadge CommissionBadge { get; private set; } = EventBadge.None;     // 의뢰해결권 보유(빨간 동그라미).
    public EventBadge StageSweepBadge { get; private set; } = EventBadge.None;     // 스테이지 소탕권 보유(빨간 동그라미).
    public EventBadge HeadquartersBadge { get; private set; } = EventBadge.None;   // 지금 올릴 수 있는 본부 노드 존재.
    /// <summary>
    /// 길드 버튼. 처리할 가입 신청이 있거나(운영진 이상. 권한 판단은 서버가 한다),
    /// 오늘 무료 레이드 참여가 남아 있으면 켜진다.
    /// </summary>
    public EventBadge GuildBadge { get; private set; } = EventBadge.None;

    /// <summary>길드 팝업의 컨텐츠 탭(무료 레이드 참여 가능).</summary>
    public EventBadge GuildContentBadge { get; private set; } = EventBadge.None;

    /// <summary>메인 친구 버튼(받은 친구 신청이 있으면 켜진다).</summary>
    public EventBadge FriendBadge { get; private set; } = EventBadge.None;

    /// <summary>친구 팝업의 친구 신청 탭. 지금은 메인 버튼과 근거가 같지만, 탭별로 갈라질 여지를 두고 분리한다.</summary>
    public EventBadge FriendRequestBadge { get; private set; } = EventBadge.None;

    /// <summary>메인 업적 버튼(수령할 수 있는 업적 점수 보상이 있으면 켜진다).</summary>
    public EventBadge AchievementBadge { get; private set; } = EventBadge.None;

    /// <summary>업적 팝업의 보상 탭. 지금은 메인 버튼과 근거가 같지만, 탭별로 갈라질 여지를 두고 분리한다.</summary>
    public EventBadge AchievementRewardBadge { get; private set; } = EventBadge.None;

    /// <summary>메인 수련 버튼(시간이 다 됐는데 완료 버튼을 누르지 않은 수련이 있으면 켜진다).</summary>
    public EventBadge TrainingBadge { get; private set; } = EventBadge.None;

    /// <summary>메인 쟁탈전 버튼(오늘 무료 매칭이 남아 있으면 켜진다).</summary>
    public EventBadge ArenaBadge { get; private set; } = EventBadge.None;

    /// <summary>원정대 버튼(이번 주 파티가 있고 오늘 아직 출격하지 않았으면 켜진다).</summary>
    public EventBadge ExpeditionBadge { get; private set; } = EventBadge.None;

    /// <summary>무한 타워 버튼(오늘 아직 한 층을 오르지 않았으면 켜진다).</summary>
    public EventBadge TowerBadge { get; private set; } = EventBadge.None;

    /// <summary>영웅전 버튼(오늘 도전 횟수가 남아 있고 끝까지 깨지 않았으면 켜진다).</summary>
    public EventBadge HeroesBadge { get; private set; } = EventBadge.None;

    /// <summary>전선 버튼(보유 기력과 오늘 남은 전선 기력이 모두 1회 참여분 이상이면 켜진다).</summary>
    public EventBadge FrontlineBadge { get; private set; } = EventBadge.None;

    /// <summary>파견 버튼(시간이 다 됐는데 완료 버튼을 누르지 않은 파견이 있으면 켜진다).</summary>
    public EventBadge DispatchBadge { get; private set; } = EventBadge.None;

    /// <summary>멸망전 버튼(받지 않은 구간 보상이 있거나 오늘 참여 횟수가 남아 있으면 켜진다).</summary>
    public EventBadge DoomBadge { get; private set; } = EventBadge.None;

    /// <summary>스토리 버튼(새로 읽을 수 있는 편이 있거나 미수령 완독 보상이 있으면 켜진다).</summary>
    public EventBadge StoryBadge { get; private set; } = EventBadge.None;

    /// <summary>패스 버튼(도달한 칸 중 받지 않은 보상이 있으면 켜진다. 평생 패스가 있으면 패스 줄까지 본다).</summary>
    public EventBadge PassBadge { get; private set; } = EventBadge.None;

    /// <summary>투자 버튼(별레벨이 올라 새로 열린 투자 슬롯을 아직 확인하지 않았으면 NEW).</summary>
    public EventBadge InvestmentBadge { get; private set; } = EventBadge.None;

    public IObservable<Unit> Changed => _changed;

    /// <summary>
    /// 씬 스코프가 사라질 때 루트 스코프 서비스에 건 구독을 끊는다(VContainer 가 불러 준다).
    /// 끊지 않으면 메인 씬을 다시 열 때마다 죽은 BadgeCenter 가 하나씩 쌓인다.
    /// </summary>
    public void Dispose() => _rootSubscriptions.Dispose();

    // 이벤트 배지(계산은 EventBadgeService 담당). 이벤트 버튼 전체 종합과 보상 키 1건.
    public EventBadge EventsBadge => _events != null ? _events.Aggregate : EventBadge.None;
    public EventBadge EventRewardBadge(string rewardKey) =>
        _events != null ? _events.GetRewardBadge(rewardKey) : EventBadge.None;

    /// <summary>
    /// 이벤트 충족 상태만 가볍게 다시 받아온다(공지 목록/상점/우편은 건드리지 않는다).
    /// 게임 플레이로 이벤트 미션이 달성되는 순간(장비 강화/각성, 접속 시간, 스테이지 클리어 등)에 부르는 용도다.
    /// 전체 갱신(RefreshAsync)은 상점 카탈로그와 공지 HTTP 까지 다시 치므로 그런 잦은 호출에는 맞지 않는다.
    /// 결과는 _events.Changed -> 이 클래스의 Changed 로 흘러 배지 뷰가 저절로 갱신된다.
    /// </summary>
    public UniTask RefreshEventClaimStatesAsync(CancellationToken ct = default) =>
        _events != null ? _events.RefreshClaimStatesAsync(ct) : UniTask.CompletedTask;

    /// <summary>
    /// 슬라이드 메뉴(햄버거) 버튼 종합: 메뉴 안 배지 중 하나라도 켜지면 메인 햄버거 버튼에 같은 불이 켜진다.
    /// 메뉴가 닫혀 있으면 안쪽 버튼이 보이지 않으므로, 여기에 빠진 배지는 유저에게 영영 도달하지 못한다.
    /// 슬라이드 메뉴에 버튼을 추가하면 반드시 그 배지도 이 목록에 넣을 것(확장 포인트).
    /// </summary>
    public EventBadge HamburgerMenuBadge {
        get {
            var badge = MailBadge;
            badge = Higher(badge, CommissionBadge);
            badge = Higher(badge, HeadquartersBadge);
            badge = Higher(badge, GuildBadge);
            badge = Higher(badge, TrainingBadge);
            badge = Higher(badge, ArenaBadge);
            badge = Higher(badge, ExpeditionBadge);
            badge = Higher(badge, TowerBadge);
            badge = Higher(badge, HeroesBadge);
            badge = Higher(badge, FrontlineBadge);
            badge = Higher(badge, DispatchBadge);
            badge = Higher(badge, DoomBadge);
            // 스토리: V1.12 에서 메뉴 버튼을 숨겼다(V1.13 리워크). 숨긴 버튼의 배지가 햄버거에만 켜지지 않도록 뺀다.
            // 버튼을 다시 열 때 이 줄을 되살릴 것: badge = Higher(badge, StoryBadge);
            badge = Higher(badge, PassBadge);
            badge = Higher(badge, InvestmentBadge);
            return badge;
        }
    }

    // 무료 뽑기 가능(빨간 동그라미). 가챠 팝업 진입 버튼/탭에 표시.
    public EventBadge GachaFreeBadge => AnyFreeAvailable() ? EventBadge.Claimable : EventBadge.None;

    // 가챠 메인 버튼: 무료 뽑기 가능(빨간 동그라미) 또는 로테이션 변경(NEW). Claimable 우선.
    public EventBadge GachaButtonBadge => Higher(GachaFreeBadge, GachaNewBadge);

    private bool AnyFreeAvailable() {
        long now = KstTime.NowUnix;
        foreach (var kv in _freeNextUnix) if (kv.Value <= now) return true;
        return false;
    }

    // 상점 메인 버튼: 충전 보상 수령가능 또는 신규 오퍼가 있으면 켠다(수령가능 Claimable 우선).
    public EventBadge ShopButtonBadge => Higher(ChargeBadge, ShopNewBadge);

    // 상점 카테고리 버튼: 해당 대분류의 신규 오퍼만 본다.
    // 충전 보상은 합산하지 않는다. 충전보상 버튼이 페이지에 상시 노출이라 그 버튼의 배지(BadgeId.ChargeReward)로
    // 바로 보이고, 카테고리까지 켜면 같은 알림이 중복된다(메인 상점 버튼 -> 충전보상 버튼 2단계로 충분).
    public EventBadge ShopCategoryBadge(string major) =>
        HasUnseen(major) ? EventBadge.Unread : EventBadge.None;

    private bool HasUnseen(string major) {
        var s = Seen;
        var seen = s != null && s.seenShopOfferIds != null ? new HashSet<string>(s.seenShopOfferIds) : new HashSet<string>();
        foreach (var o in _offers)
            if ((major == null || o.major == major) && !seen.Contains(o.id)) return true;
        return false;
    }

    // 우선순위: Claimable(빨간 동그라미) > Unread(NEW) > None (enum 값 순서와 일치).
    private static EventBadge Higher(EventBadge a, EventBadge b) => (EventBadge)System.Math.Max((int)a, (int)b);

    /// <summary>서버 상태(도감 수령/충전액/로테이션/오퍼)를 최신화하고 모든 배지를 재계산한다. 중복 호출은 1회로 합친다.</summary>
    public async UniTask RefreshAsync(CancellationToken ct = default) {
        if (_refreshing) return;
        _refreshing = true;
        try {
            try {
                var ency = await _encyclopedia.RequestRewardsAsync(ct);
                _claimedCollection = ency.ClaimedCollectionSteps ?? new HashSet<int>();
                _claimedStar = ency.ClaimedStarSteps ?? new HashSet<int>();
            } catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }

            try {
                var charge = await _charge.RequestStatusAsync(ct);
                _claimedTiers = charge.ClaimedTiers ?? new HashSet<int>();
                _cumulativeWon = charge.CumulativeWon;
            } catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }

            try {
                var catalog = await _shop.RequestShopAsync(ct);
                _rotationCharId = catalog.RotationCharId;
                _freeNextUnix = catalog.FreeGachaNextUnix ?? new Dictionary<string, long>();
                _offers.Clear();
                foreach (var o in catalog.Offers) _offers.Add((o.OfferId, o.Major));
            } catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }

            // 우편: 미수령 수만 최신화한다(UnreadCount 변경은 구독으로도 반영되나, 최초 진입 시 확실히 당겨온다).
            if (_mail != null) {
                try { await _mail.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 이벤트 충족 상태(서버). 계산/캐시는 서비스가 하고, 여기서는 최신화만 같이 태운다.
            if (_events != null) {
                try { await _events.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 길드: 처리할 가입 신청과 남은 무료 레이드 참여. 둘 다 서버가 권한/오늘 기준까지 보고 채운 값이라
            // 클라가 역할이나 날짜를 다시 따질 필요가 없다.
            if (_guild != null) {
                try {
                    var home = await _guild.RequestHomeAsync(ct);
                    _guildPendingApplications = home.PendingApplications;
                    _guildRaidFreeEntries = home.RaidFreeEntries;
                } catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 친구: 받은 신청 수만 최신화한다. 이후 변동은 서버 푸시(S_FriendNotify)와
            // 팝업의 수락/거절 응답이 같은 ReactiveProperty 를 갱신하므로 여기서 다시 조회할 일이 없다.
            if (_friend != null) {
                try { await _friend.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 업적: 수령 가능한 보상이 있는지만 최신화한다. 이후 변동은 업적 팝업의 조회/수령 응답이
            // 같은 ReactiveProperty 를 갱신하므로 여기서 다시 조회할 일이 없다.
            if (_achievement != null) {
                try { await _achievement.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 수련: 진행 중인 슬롯의 완료 시각을 받아 둔다. 이후 "시간이 다 됐는지"는 서비스가 스스로 다시 따지므로
            // 배지를 켜기 위해 주기적으로 서버를 두드리지 않는다.
            if (_training != null) {
                try { await _training.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 쟁탈전: 오늘 무료 매칭이 남았는지만 최신화한다. 이후 변동은 매칭 응답이 같은 값을 갱신한다.
            if (_arena != null) {
                try { await _arena.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 원정대: 오늘 칠 수 있는지만 최신화한다. 이후 변동은 출격 응답이 같은 값을 갱신한다.
            if (_expedition != null) {
                try { await _expedition.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 무한 타워: 오늘 오를 수 있는지만 최신화한다. 이후 변동은 등반 응답이 같은 값을 갱신한다.
            if (_tower != null) {
                try { await _tower.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 영웅전: 오늘 도전 횟수가 남았는지만 최신화한다. 이후 변동은 입장/클리어 응답이 같은 값을 갱신한다.
            if (_heroes != null) {
                try { await _heroes.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 전선: 오늘 남은 전선 기력을 최신화한다(자정 초기화 반영). 이후 변동은 입장/종료 응답이 같은 값을 갱신한다.
            if (_frontline != null) {
                try { await _frontline.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 파견: 수련과 같은 이유로 진행 중인 슬롯의 완료 시각만 받아 둔다(이후 판정은 서비스가 시간으로 한다).
            if (_dispatch != null) {
                try { await _dispatch.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 멸망전: 받지 않은 보상이 있는지만 받아 둔다. 이후 변동은 결과 보고/수령 응답이 같은 값을 갱신한다.
            if (_doom != null) {
                try { await _doom.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 스토리: 19편의 현황을 받아 둔다(해금 판정은 서버가 한다). 이후 변동은 완독/수령 응답이 갱신한다.
            if (_story != null) {
                try { await _story.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            // 패스: 받을 칸이 있는지만 받아 둔다. 이후 변동은 서버 푸시(새 레벨 도달, 평생 패스 구매)와 수령 응답이 갱신한다.
            if (_pass != null) {
                try { await _pass.RefreshAsync(ct); }
                catch (OperationCanceledException) { return; } catch (Exception e) { Debug.LogException(e); }
            }

            _loaded = true;
            RecomputeAll();
            _changed.OnNext(Unit.Default);
        } finally {
            _refreshing = false;
        }
    }

    /// <summary>가챠 로테이션을 "봤음"으로 기록(가챠 팝업 진입 시). rotationCharId 는 팝업이 받은 현재 값.</summary>
    public void MarkGachaSeen(int rotationCharId) {
        var s = Seen;
        if (s == null || s.seenRotationCharId == rotationCharId) return;
        s.seenRotationCharId = rotationCharId;
        UserDataManager.RequestSave();
        GachaNewBadge = EventBadge.None;
        _changed.OnNext(Unit.Default);
    }

    /// <summary>현재 상점 오퍼 전체를 "봤음"으로 기록(상점 팝업 진입 시).</summary>
    public void MarkShopSeen(IEnumerable<string> offerIds) {
        var s = Seen;
        if (s == null || offerIds == null) return;
        s.seenShopOfferIds = new List<string>(offerIds);
        UserDataManager.RequestSave();
        ShopNewBadge = EventBadge.None;
        _changed.OnNext(Unit.Default);
    }

    private void RecomputeAll() {
        RecomputeEncyclopedia();
        RecomputeCharge();
        RecomputeNew();
        RecomputeDeck();
        RecomputeMail();
        RecomputeCommission();
        RecomputeStageSweep();
        RecomputeHeadquarters();
        RecomputeGuild();
        RecomputeFriend();
        RecomputeAchievement();
        RecomputeTraining();
        RecomputeArena();
        RecomputeExpedition();
        RecomputeTower();
        RecomputeHeroes();
        RecomputeFrontline();
        RecomputeDispatch();
        RecomputeDoom();
        RecomputeStory();
        RecomputePass();
        RecomputeInvestment();
    }

    // 투자 배지: 지금 별레벨로 열린 슬롯 수가 마지막으로 확인한 수보다 많으면 NEW.
    // 해금 판정은 투자 팝업과 같은 규칙(InvestmentRules.UnlockedSlots, 보유 캐릭터 별레벨 총합)이라 화면과 어긋나지 않는다.
    // 최초 1회(기록 없음)는 지금 열린 수를 봤음으로 심어 New 를 띄우지 않는다(이후 새로 열린 칸만 New).
    // 보유 캐릭터를 받기 전에는 판정하지 않는다(총합 0 으로 심으면 접속 직후 전 슬롯이 New 로 보인다).
    private void RecomputeInvestment() {
        var s = Seen;
        if (_store == null || s == null || _store.Characters.Count == 0) { InvestmentBadge = EventBadge.None; return; }

        int unlocked = InvestmentRules.UnlockedSlots(TotalStarLevel());
        if (s.seenInvestmentSlots <= 0) {
            s.seenInvestmentSlots = unlocked;
            UserDataManager.RequestSave();
        }
        InvestmentBadge = unlocked > s.seenInvestmentSlots ? EventBadge.Unread : EventBadge.None;
    }

    /// <summary>지금 열린 투자 슬롯을 "봤음"으로 기록(투자 팝업 진입 시).</summary>
    public void MarkInvestmentSeen() {
        var s = Seen;
        if (_store == null || s == null || _store.Characters.Count == 0) return;
        int unlocked = InvestmentRules.UnlockedSlots(TotalStarLevel());
        if (s.seenInvestmentSlots == unlocked && InvestmentBadge == EventBadge.None) return;
        s.seenInvestmentSlots = unlocked;
        UserDataManager.RequestSave();
        RecomputeInvestment();
        _changed.OnNext(Unit.Default);
    }

    // 보유 캐릭터 별레벨 총합(투자 해금 기준, 서버 InvestmentRepository 와 같은 근거).
    private int TotalStarLevel() {
        int total = 0;
        foreach (var p in _store.Characters.Values) if (p != null) total += p.StarLevel;
        return total;
    }

    // 패스 배지: 받을 수 있는 칸이 있으면 빨간 동그라미. 판정은 서버가 하고(canClaim) 클라는 그 결과만 본다.
    private void RecomputePass() {
        bool claimable = _pass != null && _pass.HasClaimable.Value;
        PassBadge = claimable ? EventBadge.Claimable : EventBadge.None;
    }

    // 스토리 배지: 새로 읽을 수 있는 편 또는 미수령 완독 보상. 판정 근거는 서버가 내려 준 현황 하나뿐이다.
    private void RecomputeStory() {
        bool on = _story != null && _story.HasBadge.Value;
        StoryBadge = on ? EventBadge.Claimable : EventBadge.None;
    }

    // 멸망전 배지: 도달한 구간 중 아직 받지 않은 보상이 있거나 오늘 무료 참여가 남아 있으면 빨간 동그라미.
    // 무료분을 쓰면 다음 참여는 유료라 꺼진다. 판정은 서버가 하고(RewardsBetween / 다음 참여 비용)
    // 클라는 그 결과만 들고 있으므로 화면마다 어긋날 일이 없다.
    private void RecomputeDoom() {
        bool on = _doom != null && (_doom.HasClaimable.Value || _doom.HasFreeEntry.Value);
        DoomBadge = on ? EventBadge.Claimable : EventBadge.None;
    }

    // 쟁탈전 배지: 오늘 무료 매칭이 남아 있으면 빨간 동그라미(쓸 수 있는 기회가 놀고 있다는 안내).
    private void RecomputeArena() {
        bool free = _arena != null && _arena.HasFreeEntry.Value;
        ArenaBadge = free ? EventBadge.Claimable : EventBadge.None;
    }

    // 원정대 배지: 이번 주 파티가 있고 오늘 아직 출격하지 않았으면 빨간 동그라미(쟁탈전과 같은 "놀고 있는 기회" 안내).
    private void RecomputeExpedition() {
        bool can = _expedition != null && _expedition.CanEnterToday.Value;
        ExpeditionBadge = can ? EventBadge.Claimable : EventBadge.None;
    }

    // 무한 타워 배지: 오늘 아직 한 층을 오르지 않았으면 빨간 동그라미(쟁탈전과 같은 "놀고 있는 기회" 안내).
    private void RecomputeTower() {
        bool can = _tower != null && _tower.CanClimbToday.Value;
        TowerBadge = can ? EventBadge.Claimable : EventBadge.None;
    }

    // 영웅전 배지: 오늘 도전 횟수가 남아 있으면 빨간 동그라미(무한 타워와 같은 "놀고 있는 기회" 안내).
    private void RecomputeHeroes() {
        bool can = _heroes != null && _heroes.CanEnterToday.Value;
        HeroesBadge = can ? EventBadge.Claimable : EventBadge.None;
    }

    // 전선 배지: 지금 한 판을 깰 수 있는 기력이 있고(보유 기력) 오늘 전선에 더 쓸 수 있으면(하루 한도) 빨간 동그라미.
    // 1회분 기력은 서버가 현황으로 내려준 값을 쓴다. 현황을 아직 받기 전이면 판단하지 않는다(오탐 방지).
    private void RecomputeFrontline() {
        var info = _frontline != null ? _frontline.Info.Value : null;
        if (info == null || _store == null) { FrontlineBadge = EventBadge.None; return; }

        int cost = info.StaminaPerClear;
        bool can = info.DailyStaminaRemaining >= cost && _store.Stamina.Value >= cost;
        FrontlineBadge = can ? EventBadge.Claimable : EventBadge.None;
    }

    // 수련 배지: 완료 시각이 지났는데 아직 수령하지 않은 수련이 있으면 빨간 동그라미.
    // 시간 판정은 TrainingService 하나가 맡으므로 메인 버튼과 팝업이 같은 상태를 본다.
    private void RecomputeTraining() {
        bool claimable = _training != null && _training.HasClaimable.Value;
        TrainingBadge = claimable ? EventBadge.Claimable : EventBadge.None;
    }

    // 파견 배지: 완료 버튼을 누르지 않은 파견이 하나라도 있으면 빨간 동그라미.
    // 시간 판정은 DispatchService 하나가 맡으므로 슬롯 버튼/파견 버튼/햄버거 버튼이 같은 상태를 본다.
    private void RecomputeDispatch() {
        bool claimable = _dispatch != null && _dispatch.HasClaimable.Value;
        DispatchBadge = claimable ? EventBadge.Claimable : EventBadge.None;
    }

    // 업적 배지: 수령할 수 있는 점수 보상이 하나라도 있으면 빨간 동그라미.
    // 값의 출처는 AchievementService 하나뿐이라 메인 버튼과 보상 탭이 항상 같은 상태가 된다.
    private void RecomputeAchievement() {
        bool claimable = _achievement != null && _achievement.HasClaimableReward.Value;
        AchievementRewardBadge = claimable ? EventBadge.Claimable : EventBadge.None;
        AchievementBadge = AchievementRewardBadge;
    }

    // 친구 배지: 내가 받은(대기 중) 친구 신청이 하나라도 있으면 빨간 동그라미.
    // 값의 출처는 FriendService 하나뿐이라 메인 버튼과 신청 탭이 항상 같은 상태가 된다.
    // 메인 버튼은 기력 선물(보낼 친구 또는 받을 기력)이 있어도 켜진다. 신청 탭은 신청만 본다.
    private void RecomputeFriend() {
        bool pending = _friend != null && _friend.PendingRequests.Value > 0;
        bool stamina = _friend != null && _friend.StaminaState.Value.HasAction;
        FriendRequestBadge = pending ? EventBadge.Claimable : EventBadge.None;
        FriendBadge = pending || stamina ? EventBadge.Claimable : EventBadge.None;
    }

    // 길드 배지: 처리할 가입 신청(운영진 이상. 권한 판정은 서버가 하고 클라는 개수만 본다) 또는
    // 오늘 안 쓴 무료 레이드 참여가 있으면 빨간 동그라미.
    // 메인 길드 버튼은 둘을 합치고, 컨텐츠 탭은 레이드만 본다(그 탭에서 할 수 있는 일만 알린다).
    private void RecomputeGuild() {
        GuildContentBadge = _guildRaidFreeEntries > 0 ? EventBadge.Claimable : EventBadge.None;
        bool anyGuild = _guildPendingApplications > 0 || _guildRaidFreeEntries > 0;
        GuildBadge = anyGuild ? EventBadge.Claimable : EventBadge.None;
    }

    /// <summary>
    /// 대기 중인 가입 신청 수를 갱신한다. 신청을 승인/거절한 직후처럼 전체 갱신이 과한 자리에서 부른다.
    /// 값은 길드 팝업이 이미 받아 온 것을 그대로 넘겨 서버를 다시 두드리지 않는다.
    /// </summary>
    public void SetGuildPendingApplications(int count) {
        if (_guildPendingApplications == count) return;
        _guildPendingApplications = count;
        RecomputeGuild();
        _changed.OnNext(Unit.Default);
    }

    /// <summary>
    /// 오늘 남은 무료 레이드 참여 횟수를 갱신한다. 레이드에 참여한 직후처럼 레이드 화면이 이미 아는 값을
    /// 그대로 넘겨, 배지가 다음 전체 갱신까지 묵은 값으로 남지 않게 한다.
    /// </summary>
    public void SetGuildRaidFreeEntries(int count) {
        if (_guildRaidFreeEntries == count) return;
        _guildRaidFreeEntries = count;
        RecomputeGuild();
        _changed.OnNext(Unit.Default);
    }

    // 의뢰 배지: 의뢰해결권이 한 장이라도 있으면 빨간 동그라미(쓸 수 있는 입장권이 놀고 있다는 안내).
    private void RecomputeCommission() {
        CommissionBadge = _store != null && _store.CommissionTicket.Value > 0 ? EventBadge.Claimable : EventBadge.None;
    }

    // 소탕 배지: 스테이지 소탕권이 한 장이라도 있으면 빨간 동그라미(의뢰 배지와 같은 근거).
    // 잔량은 접속 시 상점 지갑 조회로 스토어에 들어오고, 이후 소탕/구매/보상 수령이 같은 값을 갱신한다.
    private void RecomputeStageSweep() {
        StageSweepBadge = _store != null && _store.StageSweepTicket.Value > 0 ? EventBadge.Claimable : EventBadge.None;
    }

    // 본부 배지: 지금 명성으로 올릴 수 있는 노드가 하나라도 있으면 빨간 동그라미.
    // 판정은 트리 화면과 같은 규칙(HqNodeRules)을 써서 배지와 화면이 어긋나지 않게 한다.
    private void RecomputeHeadquarters() {
        bool upgradable = _store != null &&
                          HqNodeRules.AnyUpgradable(_hqDb, _store.HqNodeLevels, _store.Renown.Value);
        HeadquartersBadge = upgradable ? EventBadge.Claimable : EventBadge.None;
    }

    // 우편 배지: 미수령 우편이 하나라도 있으면 빨간 동그라미(Claimable). 서버가 미수령만 내려주므로 UnreadCount > 0 로 판정.
    private void RecomputeMail() {
        MailBadge = _mail != null && _mail.UnreadCount.Value > 0 ? EventBadge.Claimable : EventBadge.None;
    }

    // 스토어 변경(캐릭/별레벨/다이아) 시: 덱은 즉시, 도감은 서버 수령여부 조회 후에만 재계산하고 알린다.
    private void OnStoreChanged() {
        RecomputeDeck();
        if (_loaded) RecomputeInvestment();   // 별 승급으로 새 슬롯이 열리면 그 자리에서 New
        if (_loaded) RecomputeEncyclopedia();   // 수령 여부(서버) 조회 전에는 도감 재계산 안 함(오탐 방지)
        _changed.OnNext(Unit.Default);
    }

    // 덱 배지: 육성 가능(스탯P/별승급, 빨간 동그라미) OR 새로 획득한 캐릭(NEW). 최초 1회는 현재 보유를 봤음으로 심어 New 를 띄우지 않는다.
    private void RecomputeDeck() {
        var s = Seen;

        // 육성 가능(빨간 동그라미): 보유 캐릭 중 하나라도 스탯포인트 보유 or 별 승급 가능.
        bool anyGrow = false;
        if (_store != null) {
            int ownedDia = _store.Diamond.Value;
            foreach (var kv in _store.Characters)
                if (CharacterUpgrade.CanGrow(_store, kv.Key, ownedDia)) { anyGrow = true; break; }
        }

        // 신규 캐릭(NEW): 로컬 "봤음"에 없는 보유 캐릭.
        bool deckNew = false;
        if (_store != null && s != null && _store.Characters.Count > 0) {
            if (s.seenCharIds == null || s.seenCharIds.Count == 0) {
                s.seenCharIds = new List<int>(_store.Characters.Keys);   // 최초 심기(New 없음)
                UserDataManager.RequestSave();
            } else {
                var seen = new HashSet<int>(s.seenCharIds);
                foreach (var id in _store.Characters.Keys) if (!seen.Contains(id)) { deckNew = true; break; }
            }
        }

        DeckBadge = Higher(anyGrow ? EventBadge.Claimable : EventBadge.None, deckNew ? EventBadge.Unread : EventBadge.None);
    }

    /// <summary>현재 보유 캐릭터 전체를 "봤음"으로 기록(덱 팝업 진입 시). 신규(NEW)만 해제되고 육성 가능(빨간 동그라미)은 유지된다.</summary>
    public void MarkDeckSeen() {
        var s = Seen;
        if (_store == null || s == null) return;
        s.seenCharIds = new List<int>(_store.Characters.Keys);
        UserDataManager.RequestSave();
        RecomputeDeck();
        _changed.OnNext(Unit.Default);
    }

    // 도감 보상: 진행도(보유 캐릭 수 / 총 별레벨)는 스토어에서, 수령 여부는 서버 캐시에서. 하나라도 수령 가능하면 빨간 동그라미(Claimable).
    private void RecomputeEncyclopedia() {
        int collectionCount = _store != null ? _store.Characters.Count : 0;
        int totalStar = 0;
        if (_store != null) foreach (var p in _store.Characters.Values) if (p != null) totalStar += p.StarLevel;

        // 단계 수는 캐릭터 수에서 정해진다(도감 보상 팝업과 같은 규칙).
        int characterCount = _charDb != null ? _charDb.All.Count : 0;
        int collectionSteps = EncyclopediaRewardRules.CollectionSteps(characterCount);
        int starSteps = EncyclopediaRewardRules.StarSteps(characterCount);

        bool claimable = false;
        for (int step = 1; step <= collectionSteps && !claimable; step++)
            if (collectionCount >= EncyclopediaRewardRules.CollectionThreshold(step) && !_claimedCollection.Contains(step)) claimable = true;
        for (int step = 1; step <= starSteps && !claimable; step++)
            if (totalStar >= EncyclopediaRewardRules.StarThreshold(step) && !_claimedStar.Contains(step)) claimable = true;

        EncyclopediaBadge = claimable ? EventBadge.Claimable : EventBadge.None;
    }

    private void RecomputeCharge() {
        var tiers = ChargeRewardRules.Tiers;
        bool claimable = false;
        for (int i = 0; i < tiers.Count && !claimable; i++)
            if (_cumulativeWon >= tiers[i].Threshold && !_claimedTiers.Contains(i + 1)) claimable = true;
        ChargeBadge = claimable ? EventBadge.Claimable : EventBadge.None;
    }

    // New(NEW 뱃지): 로컬 "봤음"과 비교. 최초 1회(봤음 기록 없음)는 현재 상태를 봤음으로 심어 New 를 띄우지 않는다(이후 변경분만 New).
    private void RecomputeNew() {
        var s = Seen;
        if (s == null) { GachaNewBadge = EventBadge.None; ShopNewBadge = EventBadge.None; return; }

        // 가챠 로테이션.
        if (_rotationCharId == 0) {
            GachaNewBadge = EventBadge.None;
        } else if (s.seenRotationCharId == 0) {
            s.seenRotationCharId = _rotationCharId;   // 최초 심기(New 없음)
            UserDataManager.RequestSave();
            GachaNewBadge = EventBadge.None;
        } else {
            GachaNewBadge = s.seenRotationCharId != _rotationCharId ? EventBadge.Unread : EventBadge.None;
        }

        // 상점 신규 오퍼(전체).
        if (_offers.Count == 0) {
            ShopNewBadge = EventBadge.None;
        } else if (s.seenShopOfferIds == null || s.seenShopOfferIds.Count == 0) {
            s.seenShopOfferIds = new List<string>(_offers.Count);   // 최초 심기(New 없음)
            foreach (var o in _offers) s.seenShopOfferIds.Add(o.id);
            UserDataManager.RequestSave();
            ShopNewBadge = EventBadge.None;
        } else {
            ShopNewBadge = HasUnseen(null) ? EventBadge.Unread : EventBadge.None;
        }
    }
}
