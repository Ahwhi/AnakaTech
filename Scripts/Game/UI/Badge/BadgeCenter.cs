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
public sealed class BadgeCenter {
    private readonly IEncyclopediaService _encyclopedia;
    private readonly IChargeRewardService _charge;
    private readonly IShopService _shop;
    private readonly IMailService _mail;
    private readonly PlayerStateStore _store;
    private readonly IEventBadgeService _events;
    private readonly HqNodeDatabase _hqDb;
    private readonly IGuildService _guild;
    private readonly IFriendService _friend;
    private readonly IAchievementService _achievement;
    private readonly ITrainingService _training;
    private readonly IArenaService _arena;
    private readonly IDispatchService _dispatch;

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
                       HqNodeDatabase hqDb, IGuildService guild, IFriendService friend,
                       IAchievementService achievement, ITrainingService training, IArenaService arena,
                       IDispatchService dispatch) {
        _encyclopedia = encyclopedia;
        _charge = charge;
        _shop = shop;
        _mail = mail;
        _store = store;
        _events = events;
        _hqDb = hqDb;
        _guild = guild;
        _friend = friend;
        _achievement = achievement;
        _training = training;
        _arena = arena;
        _dispatch = dispatch;
        // 이벤트 배지가 바뀌면(읽음 처리/서버 최신화) 그대로 흘려보낸다. 구독자는 BadgeCenter 만 보면 된다.
        if (_events != null) _events.Changed.Subscribe(_ => _changed.OnNext(Unit.Default));
        // 보유 캐릭/별레벨/장비 변경 시 도감 + 덱 배지 재계산. 다이아 변동도 덱(별 승급 가능)에 영향.
        // 배지는 "무엇이 바뀌었는지"와 무관하게 항상 재계산하므로 CharacterChange 인자는 무시한다.
        if (_store != null) {
            _store.CharactersChanged += _ => OnStoreChanged();
            _store.Diamond.Subscribe(_ => OnStoreChanged());
            // 의뢰해결권/명성은 재화라 값 구독으로, 본부 노드 레벨은 값이 아니라 목록이라 변경 통지로 받는다.
            _store.CommissionTicket.Subscribe(_ => { RecomputeCommission(); _changed.OnNext(Unit.Default); });
            _store.Renown.Subscribe(_ => { RecomputeHeadquarters(); _changed.OnNext(Unit.Default); });
            _store.HeadquartersChanged += () => { RecomputeHeadquarters(); _changed.OnNext(Unit.Default); };
        }
        // 미수령 우편 수가 바뀌면(최신화/수령) 즉시 우편 배지 재계산 후 알린다(햄버거 종합 배지도 함께 갱신).
        if (_mail != null)
            _mail.UnreadCount.Subscribe(_ => { RecomputeMail(); _changed.OnNext(Unit.Default); });
        // 받은 친구 신청 수가 바뀌면(서버 푸시/수락/거절/목록 조회) 즉시 재계산한다.
        // 메인 친구 버튼과 친구 신청 탭 배지가 같은 값을 보므로 한쪽만 켜지는 어긋남이 생기지 않는다.
        if (_friend != null)
            _friend.PendingRequests.Subscribe(_ => { RecomputeFriend(); _changed.OnNext(Unit.Default); });
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

    /// <summary>파견 버튼(시간이 다 됐는데 완료 버튼을 누르지 않은 파견이 있으면 켜진다).</summary>
    public EventBadge DispatchBadge { get; private set; } = EventBadge.None;

    public IObservable<Unit> Changed => _changed;

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
            badge = Higher(badge, DispatchBadge);
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

            // 파견: 수련과 같은 이유로 진행 중인 슬롯의 완료 시각만 받아 둔다(이후 판정은 서비스가 시간으로 한다).
            if (_dispatch != null) {
                try { await _dispatch.RefreshAsync(ct); }
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
        RecomputeHeadquarters();
        RecomputeGuild();
        RecomputeFriend();
        RecomputeAchievement();
        RecomputeTraining();
        RecomputeArena();
        RecomputeDispatch();
    }

    // 쟁탈전 배지: 오늘 무료 매칭이 남아 있으면 빨간 동그라미(쓸 수 있는 기회가 놀고 있다는 안내).
    private void RecomputeArena() {
        bool free = _arena != null && _arena.HasFreeEntry.Value;
        ArenaBadge = free ? EventBadge.Claimable : EventBadge.None;
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
    private void RecomputeFriend() {
        bool pending = _friend != null && _friend.PendingRequests.Value > 0;
        FriendRequestBadge = pending ? EventBadge.Claimable : EventBadge.None;
        FriendBadge = FriendRequestBadge;
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

        bool claimable = false;
        for (int step = 1; step <= EncyclopediaRewardRules.CollectionSteps && !claimable; step++)
            if (collectionCount >= EncyclopediaRewardRules.CollectionThreshold(step) && !_claimedCollection.Contains(step)) claimable = true;
        for (int step = 1; step <= EncyclopediaRewardRules.StarSteps && !claimable; step++)
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
