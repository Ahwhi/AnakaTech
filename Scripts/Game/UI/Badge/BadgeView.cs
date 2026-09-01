using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// 알림 배지(빨간 점 / NEW)를 표시하는 단일 컴포넌트. 프로젝트의 모든 배지는 이 하나로 통일한다.
///
/// 역할 분담:
///   - 이 컴포넌트   : "어디에 놓을지"(슬롯) + "언제 켤지"(상태 구독)만 담당한다.
///   - BadgeCatalog : "무엇을 보여줄지"(상태별 프리팹). 아트 교체는 SO 한 곳에서 끝난다.
///   - BadgeCenter  : 배지 상태 계산의 단일 소스(서버/로컬 조건 집계).
/// 크기를 프리팹이 들고 있으므로 Claimable(빨간 동그라미)과 Unread(NEW)의 크기가 달라도 각자 제 크기로 나온다.
///
/// 배치 방법(둘 다 지원):
///   (1) 버튼에 이 컴포넌트를 붙이고 Slot 에 "배지 자리를 잡아둔 기존 Image" 를 넣는다.
///       런타임에는 그 Image 를 끄고 같은 자리에 카탈로그 프리팹을 생성한다. 기존 배치를 그대로 재사용하는 방식.
///   (2) 배지 오브젝트 자체에 이 컴포넌트를 붙이고 Slot 을 비운다. 그 오브젝트 자리에 생성한다.
///
/// 상태 소스(Source):
///   - Center : BadgeCenter 에서 Id 로 조회하고 변경을 구독한다(대부분의 버튼).
///   - Manual : 소유 스크립트가 SetOn/SetState 로 직접 넣는다(카드/탭처럼 그 화면에서만 계산되는 조건).
///
/// 의존성: Source=Center 일 때만 BadgeCenter 가 필요하며, 활성화/시작 시 현재 씬 스코프에서 직접 해석한다.
/// 컨테이너 주입([Inject])에 의존하지 않으므로 LifetimeScope 의 "Auto Inject GameObjects" 에 등록하지 않아도 되고,
/// 비활성으로 두었다가 켜도, 동적 생성 프리팹(이벤트 페이지 등)에 들어가 있어도 안전하다.
/// (동적 프리팹을 주입하는 컨테이너가 BadgeCenter 를 등록한 스코프라는 보장이 없다.)
/// </summary>
public sealed class BadgeView : MonoBehaviour {
    /// <summary>배지 상태를 어디서 받아올지.</summary>
    public enum Source {
        Center,   // BadgeCenter 에서 Id 로 조회(구독 자동)
        Manual    // 소유 스크립트가 SetOn/SetState 로 직접 지정
    }

    /// <summary>
    /// BadgeCenter 가 계산하는 배지 종류. 값(정수 인덱스)이 프리팹/씬에 직렬화되므로
    /// 순서를 바꾸지 말고 새 배지는 항상 아래에 추가한다.
    /// </summary>
    public enum BadgeId {
        EncyclopediaReward,   // 0 도감 보상 수령가능
        ChargeReward,         // 1 충전 보상 수령가능
        GachaNew,             // 2 가챠 로테이션 변경(NEW)
        GachaButton,          // 3 가챠 메인 버튼 집계(무료 뽑기 가능 OR 로테이션 변경)
        ShopNew,              // 4 상점 신규 오퍼(전체)
        ShopButton,           // 5 상점 메인 버튼 집계(충전 수령가능 OR 신규 오퍼)
        ShopCategory,         // 6 상점 카테고리 버튼. Key = 대분류(special/limited/consumable/currency)
        Deck,                 // 7 덱 메인 버튼(육성/장비 업그레이드 가능 OR 새 캐릭)
        Mail,                 // 8 우편함 버튼(미수령 우편)
        HamburgerMenu,        // 9 메인 햄버거 버튼(슬라이드 메뉴 안 배지 종합)
        Events,               // 10 이벤트 버튼(모든 이벤트 종합)
        EventReward,          // 11 특정 보상 이벤트. Key = reward_key(예: attendance)
        Commission,           // 12 의뢰 버튼(의뢰해결권 보유)
        Headquarters,         // 13 용병단 본부 버튼(명성으로 올릴 수 있는 노드 존재)
        Guild,                // 14 길드 버튼(처리할 가입 신청 또는 무료 레이드 참여 가능)
        GuildContent,         // 15 길드 컨텐츠 탭(무료 레이드 참여 가능)
        Friend,               // 16 친구 버튼(받은 친구 신청)
        FriendRequest,        // 17 친구 팝업의 친구 신청 탭(받은 친구 신청)
        Achievement,          // 18 업적 버튼(수령 가능한 업적 점수 보상)
        AchievementReward,    // 19 업적 팝업의 보상 탭(수령 가능한 업적 점수 보상)
        Training,             // 20 수련 버튼(완료 버튼을 누르지 않은 수련이 있음)
        Arena,                // 21 쟁탈전 버튼(오늘 무료 매칭이 남아 있음)
        Dispatch              // 22 파견 버튼(완료 버튼을 누르지 않은 파견이 있음)
    }

    /// <summary>생성한 배지 프리팹을 슬롯에 어떻게 맞출지.</summary>
    public enum Fit {
        PrefabSize,   // 프리팹이 들고 온 크기 그대로, 슬롯 중앙에 배치(기본)
        MatchSlot     // 슬롯 영역에 꽉 채움(슬롯 크기가 곧 배지 크기)
    }

    [Header("Source")]
    [Tooltip("Center = BadgeCenter 에서 Id 로 조회. Manual = 소유 스크립트가 SetOn/SetState 로 지정.")]
    [SerializeField] private Source source = Source.Center;
    [FormerlySerializedAs("kind")]
    [Tooltip("Source=Center 일 때 표시할 배지 종류.")]
    [SerializeField] private BadgeId id;
    [FormerlySerializedAs("categoryKey")]
    [Tooltip("Id=ShopCategory 는 상점 대분류 키, Id=EventReward 는 보상 키(reward_key). 그 외에는 비워 둔다.")]
    [SerializeField] private string key;

    [Header("Placement")]
    [FormerlySerializedAs("badgeImage")]
    [Tooltip("배지 자리를 잡아둔 기존 Image. 런타임에 이 Image 는 꺼지고 같은 자리에 프리팹이 생성된다. 비우면 이 오브젝트 자리에 생성한다.")]
    [SerializeField] private Image slotImage;
    [Tooltip("PrefabSize = 프리팹 크기 그대로 슬롯 중앙에. MatchSlot = 슬롯 크기에 꽉 채움.")]
    [SerializeField] private Fit fit = Fit.PrefabSize;
    [Tooltip("슬롯 기준 미세 위치 보정(px). 기본 0.")]
    [SerializeField] private Vector2 offset;
    [Tooltip("이 배지만 다른 카탈로그를 쓰고 싶을 때. 비우면 앱이 등록한 기본 카탈로그를 쓴다(개별 할당 불필요).")]
    [SerializeField] private BadgeCatalog catalogOverride;

    private BadgeCenter _center;
    private bool _centerResolved;

    private EventBadge _state = EventBadge.None;
    private GameObject _instance;
    private GameObject _instancePrefab;
    private Graphic _slotGraphic;   // 자리만 잡아둔 기존 표시(런타임에 꺼둔다)

    private BadgeCatalog Catalog => catalogOverride != null ? catalogOverride : BadgeCatalog.Default;

    private RectTransform Slot => slotImage != null ? slotImage.rectTransform : transform as RectTransform;

    /// <summary>현재 표시 중인 배지 상태.</summary>
    public EventBadge State => _state;

    private void Awake() => HideSlotGraphic();

    // 슬롯은 "자리"만 잡는 역할이므로 슬롯 자신의 표시는 끈다(그 자리를 카탈로그 프리팹이 대신한다).
    // Slot 을 비우고 배지 오브젝트에 직접 붙인 경우도 같은 오브젝트의 그래픽을 찾아 끈다.
    private void HideSlotGraphic() {
        if (_slotGraphic == null) {
            if (slotImage != null) {
                _slotGraphic = slotImage;
            } else if (GetComponent<Selectable>() == null) {
                // 버튼 위에 잘못 붙인 경우 버튼 배경까지 꺼버리지 않도록, 자동 탐지는 버튼이 아닐 때만 한다.
                _slotGraphic = GetComponent<Graphic>();
            }
        }
        if (_slotGraphic != null) _slotGraphic.enabled = false;
    }

    private void OnEnable() => EnsureCenter();

    private void Start() {
        if (source != Source.Center) return;

        EnsureCenter();
        if (_center == null) return;

        _center.Changed.Subscribe(_ => Refresh()).AddTo(this);
        Refresh();
        // 최초 진입 시 서버 상태를 한 번 최신화(여러 배지가 호출해도 BadgeCenter 가 1회로 합친다).
        _center.RefreshAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>Source=Manual 배지를 켜고 끈다(켜면 Claimable = 빨간 동그라미).</summary>
    public void SetOn(bool on) => SetState(on ? EventBadge.Claimable : EventBadge.None);

    /// <summary>
    /// Source=Manual 배지에 개수를 표시한다(Counter 배지). 0 이하면 배지를 끈다.
    /// 숫자는 생성된 배지 프리팹 안의 TMP_Text 에 들어가므로, 카탈로그의 Counter 프리팹에 텍스트가 있어야 한다.
    /// </summary>
    public void SetCount(int count) {
        _count = count;
        SetState(count > 0 ? EventBadge.Counter : EventBadge.None);
        ApplyCount();
    }

    private int _count;

    // 생성된 배지 안의 텍스트에 숫자를 넣는다. 프리팹 교체 없이 숫자만 바뀌는 경우도 여기로 들어온다.
    private void ApplyCount() {
        if (_instance == null || _state != EventBadge.Counter) return;
        var text = _instance.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (text != null) text.text = _count.ToString();
    }

    /// <summary>
    /// Source=Manual 배지의 상태를 직접 지정한다.
    /// Source=Center 배지에 호출하면 무시하고 경고한다. 두 소스가 같은 배지를 서로 다른 값으로 밀면
    /// 배지가 주기적으로 켜졌다 꺼졌다 하는 형태로만 드러나 원인을 찾기 어렵기 때문에, 조용히 지지 않고 알린다.
    /// </summary>
    public void SetState(EventBadge state) {
        if (source != Source.Manual) {
            WarnManualMisuse();
            return;
        }
        _state = state;
        Apply(state);
    }

    private bool _warnedManualMisuse;

    private void WarnManualMisuse() {
        if (_warnedManualMisuse) return;   // 매 프레임 갱신되는 화면도 있어 1회만 남긴다.
        _warnedManualMisuse = true;
        Debug.LogWarning(
            $"[BadgeView] '{name}' 는 Source=Center 인데 스크립트가 SetOn/SetState 로 직접 제어하려 했습니다. " +
            "이 배지는 소유 스크립트가 상태를 계산하므로 인스펙터에서 Source 를 Manual 로 바꿔야 합니다.", this);
    }

    /// <summary>Source=Center 배지를 다시 계산해 반영한다.</summary>
    private void Refresh() {
        _state = Evaluate();
        Apply(_state);
    }

    private EventBadge Evaluate() => _center == null ? EventBadge.None : id switch {
        BadgeId.EncyclopediaReward => _center.EncyclopediaBadge,
        BadgeId.ChargeReward => _center.ChargeBadge,
        BadgeId.GachaNew => _center.GachaNewBadge,
        BadgeId.GachaButton => _center.GachaButtonBadge,
        BadgeId.ShopNew => _center.ShopNewBadge,
        BadgeId.ShopButton => _center.ShopButtonBadge,
        BadgeId.ShopCategory => _center.ShopCategoryBadge(key),
        BadgeId.Deck => _center.DeckBadge,
        BadgeId.Mail => _center.MailBadge,
        BadgeId.HamburgerMenu => _center.HamburgerMenuBadge,
        BadgeId.Events => _center.EventsBadge,
        BadgeId.EventReward => _center.EventRewardBadge(key),
        BadgeId.Commission => _center.CommissionBadge,
        BadgeId.Headquarters => _center.HeadquartersBadge,
        BadgeId.Guild => _center.GuildBadge,
        BadgeId.GuildContent => _center.GuildContentBadge,
        BadgeId.Friend => _center.FriendBadge,
        BadgeId.FriendRequest => _center.FriendRequestBadge,
        BadgeId.Achievement => _center.AchievementBadge,
        BadgeId.AchievementReward => _center.AchievementRewardBadge,
        BadgeId.Training => _center.TrainingBadge,
        BadgeId.Arena => _center.ArenaBadge,
        BadgeId.Dispatch => _center.DispatchBadge,
        _ => EventBadge.None
    };

    // 상태에 해당하는 프리팹으로 교체한다. 같은 프리팹이면 아무것도 하지 않으므로
    // 변경 통지가 자주 와도(다이아 변동 등) 재생성 비용이 없다.
    private void Apply(EventBadge state) {
        var catalog = Catalog;
        var prefab = catalog != null ? catalog.Prefab(state) : null;
        if (prefab == _instancePrefab) return;

        if (_instance != null) Destroy(_instance);
        _instance = null;
        _instancePrefab = prefab;
        if (prefab == null) return;

        // 기존 배치는 배지를 SetActive(false) 로 숨겨 왔으므로, 표시할 때 이 오브젝트와 슬롯을 살린다.
        // (여기서 Awake 가 실행되며 슬롯 표시가 꺼진다. 이미 실행됐다면 아래 호출이 한 번 더 보장한다.)
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        HideSlotGraphic();

        var slot = Slot;
        if (slot == null) return;
        if (!slot.gameObject.activeSelf) slot.gameObject.SetActive(true);

        // worldPositionStays=false: UI 는 부모의 스케일/캔버스 기준을 그대로 따라야 한다(true 면 스케일이 틀어진다).
        _instance = Instantiate(prefab, slot, false);
        Place(_instance, prefab);
        ApplyCount();   // 숫자 배지는 프리팹을 새로 만든 직후에도 값이 채워져 있어야 한다.
    }

    // 생성한 배지를 슬롯에 맞춘다. 위치는 슬롯이, 크기는 (PrefabSize 일 때) 프리팹이 결정한다.
    private void Place(GameObject go, GameObject prefab) {
        // 배지가 버튼 클릭을 가로채지 않도록 한다(알림 표시는 입력 대상이 아니다).
        foreach (var g in go.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

        var rt = go.transform as RectTransform;
        if (rt == null) return;

        if (fit == Fit.MatchSlot) {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return;
        }

        // 프리팹 에셋의 RectTransform 에서 원래 크기를 읽는다(부모에 붙은 뒤의 rect 는 앵커에 따라 달라진다).
        var size = prefab.transform is RectTransform prefabRt ? prefabRt.rect.size : rt.rect.size;

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
    }

    // 상태 소스를 현재 씬의 LifetimeScope 에서 직접 해석한다(AutoInject 목록에 없어도, 동적 생성이어도 동작).
    // Source=Manual 배지는 BadgeCenter 를 쓰지 않으므로 아예 해석하지 않는다.
    private void EnsureCenter() {
        if (_centerResolved || source != Source.Center) return;
        var scope = FindAnyObjectByType<MainLifetimeScope>();
        if (scope == null || scope.Container == null) return;
        try { _center = scope.Container.Resolve<BadgeCenter>(); } catch { _center = null; }
        _centerResolved = _center != null;
    }
}
