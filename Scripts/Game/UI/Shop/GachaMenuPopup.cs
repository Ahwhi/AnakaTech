using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 가챠 전용 팝업(메인 메뉴 진입). 상점과 분리된 뽑기 화면.
/// 뽑기 라인은 평평한 배너 탭 하나로 나열한다(특수 캐릭터 / 일반 캐릭터 / 특수 장비 / 일반 장비).
/// 탭은 서버 오퍼의 PoolKey 로 라인을 찾으므로, 라인을 늘리려면 서버 카탈로그에 뽑기를 추가하고
/// 여기 배열에 탭 항목 하나만 더 배치하면 된다(이 스크립트는 수정 불필요).
///
/// 각 라인은 1회 / 10회 버튼을 가지며, 모집권 아이콘 + 사용 매수를 표시한다(빨강/비활성 없음).
/// 모집권이 충분하면 바로 뽑고(연출은 GachaPopup/EquipGachaPopup 위임), 부족하면 해당 모집권을 사는
/// 수량 선택 구매 팝업을 연다(예산 내 조절). 로테이션 배경/기간과 천장 문구는 서버가 내려주는
/// HasPickup 이 켜진 라인에서만 노출한다(클라가 "특수" 같은 문자열로 라인을 구분하지 않는다).
/// 가챠 오퍼 데이터/결제/천장은 서버 상점 카탈로그와 동일 로직을 재사용한다(상점 대분류에서만 숨김).
/// </summary>
public sealed class GachaMenuPopup : UIView, IHudPopup {
    [Serializable]
    public struct TabButton {
        [Tooltip("서버 가챠 오퍼의 PoolKey. gacha_special_char / gacha_normal_char / gacha_special_equip / gacha_normal_equip")]
        public string key;
        public Button button;
        public GameObject selectedIndicator;
        [Tooltip("무료 뽑기 가능 시 켜지는 배지(BadgeView, Source=Manual). 해당 라인이 무료 뽑기 대상일 때만 켜진다.")]
        public BadgeView badge;
        [Tooltip("이 라인의 픽업 일러스트 슬롯(선택). 픽업이 걸린 라인(특수 캐릭/특수 장비)에만 할당한다. " +
                 "캐릭터 라인은 로테이션 캐릭터, 장비 라인은 로테이션 무기를 자동으로 바인딩한다. " +
                 "픽업이 없는 라인에 할당하면 비워진 채로 유지된다.")]
        public CharacterIllustrationView pickupIllustration;
        [Tooltip("이 라인 전용 콘텐츠 패널(선택, 여러 개 지정 가능). 선택된 탭의 것만 켜지고 나머지는 꺼진다. " +
                 "라인마다 다른 배경/안내/장식을 계층 위치가 달라도 각각 붙일 수 있다. " +
                 "여러 탭이 같은 오브젝트를 공유해도 되며(공통 배경 등), 그때는 어느 한 탭이 선택되면 켜진 상태로 남는다.")]
        public GameObject[] contentRoots;
        [Tooltip("이 라인의 픽업 종료까지 남은 기간(선택). '3일 남음' 형태로 표시한다. " +
                 "픽업이 없거나 이미 끝난 라인에서는 오브젝트를 끈다.")]
        public TMP_Text remainingText;
    }

    [Header("Tabs")]
    [Tooltip("뽑기 라인 탭. 배치 순서가 곧 화면 순서이며, 첫 항목이 열 때 기본 선택된다.")]
    [SerializeField] private TabButton[] bannerTabs;

    [Header("Selected Info (선택)")]
    [SerializeField] private TMP_Text titleText;

    [Header("Currency Panel (선택)")]
    [Tooltip("보유 재화(골드/다이아/모집권) 표시. 모집권 보유량도 여기서 함께 노출하므로 별도 표시를 두지 않는다.")]
    [SerializeField] private CurrencyPanel currencyPanel;

    [Header("Pull Buttons")]
    [Tooltip("뽑기 버튼은 모집권 아이콘 + 사용 매수(x1/x10)만 표시한다. 부족하면 눌렀을 때 모집권 구매 팝업으로 유도.")]
    [SerializeField] private Button pull1Button;
    [SerializeField] private Image pull1TicketIcon;
    [SerializeField] private TMP_Text pull1TicketText;
    [SerializeField] private Button pull10Button;
    [SerializeField] private Image pull10TicketIcon;
    [SerializeField] private TMP_Text pull10TicketText;
    [Tooltip("1연 버튼: 무료 뽑기 가능 시 노출할 '무료' 표시(선택). 켜지면 모집권/다이아 표시는 숨긴다.")]
    [SerializeField] private GameObject pull1FreeLabel;
    [Tooltip("1연 버튼의 무료 가능 배지(BadgeView, Source=Manual). 현재 탭에서 무료 뽑기가 가능할 때만 켜진다. 선택.")]
    [SerializeField] private BadgeView pull1Badge;

    [Header("Free Gacha (무료 대상 라인의 1연, 24시간 1회)")]
    [Tooltip("무료 뽑기 안내 루트(선택). 있으면 이 그룹을 통째로 노출/숨김(자식에 freeTimerText 배치). 비우면 freeTimerText 만 토글.")]
    [SerializeField] private GameObject freeTimerGroup;
    [Tooltip("무료 뽑기 안내/남은 시간 텍스트(선택). 무료 대상 라인에서 '무료 뽑기 가능' 또는 '무료까지 HH:MM:SS'.")]
    [SerializeField] private TMP_Text freeTimerText;

    [Header("Rates")]
    [Tooltip("확률 보기 버튼(선택).")]
    [SerializeField] private Button ratesButton;
    [Tooltip("로테이션 정보 버튼(선택). 픽업 라인에서만 노출하며 현재/다음 픽업과 기간을 보여준다.")]
    [SerializeField] private Button rotationInfoButton;

    [Header("Rotation (픽업 라인 전용, 선택)")]
    [Tooltip("픽업 안내 루트. 픽업 라인에서 캐릭터는 로테이션 캐릭, 장비는 로테이션 무기를 보여준다.")]
    [SerializeField] private GameObject rotationRoot;
    [Tooltip("선택된 라인의 픽업 일러스트 슬롯(선택). 관심점/프레이밍을 아트가 정하므로 캐릭터와 장비가 함께 들어와도 올바르게 잡힌다.")]
    [SerializeField] private CharacterIllustrationView rotationIllustration;
    [Tooltip("픽업 대상 이름(한국어). 등급은 아래 rotationGradeText 가 따로 표시한다.")]
    [SerializeField] private TMP_Text rotationNameText;
    [Tooltip("픽업 대상 등급(선택). 공용 GradeText 컴포넌트라 등급별 색이 자동 적용된다.")]
    [SerializeField] private GradeText rotationGradeText;
    [Tooltip("픽업 대상 영문명(선택). 캐릭터에만 있으며 비어 있으면 오브젝트를 끈다.")]
    [SerializeField] private TMP_Text rotationEnglishNameText;
    [Tooltip("픽업 대상 일본어명(선택). 캐릭터에만 있으며 비어 있으면 오브젝트를 끈다.")]
    [SerializeField] private TMP_Text rotationJapaneseNameText;
    [SerializeField] private TMP_Text rotationPeriodText;
    [Tooltip("픽업 상세 보기 버튼(돋보기, 선택). 픽업 캐릭터의 도감 상세 팝업을 연다. " +
             "장비 라인에서는 그 전용무기의 주인 캐릭터를 연다(전용무기는 캐릭터와 1:1이라 주인이 확정된다).")]
    [SerializeField] private Button rotationDetailButton;
    [Tooltip("픽업 캐릭터의 무기 타입 아이콘(선택). 비우면 표시 안 함.")]
    [SerializeField] private Image rotationWeaponIcon;
    [Tooltip("무기 타입별 아이콘 공용 테이블(SO). 비우면 이미지 숨김.")]
    [SerializeField] private WeaponIconTable weaponIconTable;


    [Header("Pity (픽업 라인 전용, 선택)")]
    [Tooltip("천장 안내 루트. 픽업 라인에서만 노출한다(자식에 pityText 배치).")]
    [SerializeField] private GameObject pityGroup;
    [Tooltip("천장 안내 텍스트(pityGroup 자식). 'N회 뽑기 내 SSR OOO 확정 획득 (획득 시 100회로 초기화)'.")]
    [SerializeField] private TMP_Text pityText;
    [Tooltip("천장 안내에서 남은 횟수 숫자에 강조할 색.")]
    [SerializeField] private Color pityCountColor = new Color(1f, 0.78f, 0.25f);

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private const int PityThreshold = 100;   // 서버 GameDataRepository.PityThreshold 와 일치.

    private MainHudContext _hud;
    private CancellationToken _ct;
    private ShopCatalogDto _catalog;
    private string _poolKey = "";   // 현재 선택된 뽑기 라인(= 서버 오퍼의 PoolKey)
    private ShopOfferDto _single;   // 현재 라인의 1연 오퍼
    private ShopOfferDto _ten;      // 현재 라인의 10연 오퍼
    private bool _busy;
    private bool _wired;
    private float _nextFreeTick;    // 무료 남은시간 UI 다음 갱신 시각(unscaled).

    public void Initialize(MainHudContext hud) {
        _hud = hud;
        _ct = this.GetCancellationTokenOnDestroy();

        if (!_wired) {
            WireTabs(bannerTabs, key => SelectBanner(key));
            if (pull1Button != null) pull1Button.onClick.AddListener(() => OnPull(1));
            if (pull10Button != null) pull10Button.onClick.AddListener(() => OnPull(10));
            if (ratesButton != null) ratesButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); OpenRatesAsync().Forget(); });
            if (rotationInfoButton != null) rotationInfoButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); OpenRotationInfoAsync().Forget(); });
            if (rotationDetailButton != null) rotationDetailButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); OpenPickupDetailAsync().Forget(); });
            if (closeButton != null) closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            _wired = true;
        }

        if (currencyPanel != null) currencyPanel.Initialize(hud);

        LoadAsync().Forget();
    }

    private void WireTabs(TabButton[] tabs, Action<string> onSelect) {
        if (tabs == null) return;
        foreach (var t in tabs) {
            if (t.button == null) continue;
            var key = t.key;
            t.button.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); onSelect(key); });
        }
    }

    private async UniTaskVoid LoadAsync() {
        try {
            using (_hud.Ui.ShowLoading()) {
                _catalog = await _hud.Shop.RequestShopAsync(_ct);
                _hud.Store.Gold.Value = _catalog.Gold;
                _hud.Store.Diamond.Value = _catalog.Diamond;
                _hud.Store.ApplyShopCurrencies(_catalog.Currencies);
            }
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("가챠 정보를 불러오지 못했습니다.");
            return;
        }

        // 가챠 진입 = 현재 로테이션을 "봤음"으로 기록(가챠 New 배지 해제).
        if (_hud.Badges != null) _hud.Badges.MarkGachaSeen(_catalog.RotationCharId);

        // 열 때마다 첫 번째 탭(배치 순서상 맨 앞)부터 시작.
        _poolKey = FirstKey(bannerTabs);
        Refresh();
    }

    private static string FirstKey(TabButton[] tabs) =>
        tabs != null && tabs.Length > 0 ? tabs[0].key ?? "" : "";

    private void SelectBanner(string key) {
        _poolKey = key;
        Refresh();
    }

    private void Refresh() {
        SetSelected(bannerTabs, _poolKey);

        (_single, _ten) = FindOffers(_poolKey);

        var head = _single ?? _ten;
        if (titleText != null) titleText.text = head != null ? head.DisplayName : "";

        UpdateTabVisuals();
        UpdateRotationPanel();
        UpdatePityText(head);

        ConfigPull(pull1Button, _single, pull1TicketIcon, pull1TicketText);
        ConfigPull(pull10Button, _ten, pull10TicketIcon, pull10TicketText);

        if (ratesButton != null) ratesButton.gameObject.SetActive(_single != null || _ten != null);
        // 로테이션 버튼은 픽업이 실제로 걸린 라인에서만 의미가 있다.
        if (rotationInfoButton != null) rotationInfoButton.gameObject.SetActive(CurrentPickup().Has);

        UpdateFreeUI();
    }

    // 뽑기 버튼 표시: 모집권 아이콘 + 사용 매수(x1/x10) 고정. 다이아 표시/빨강/비활성 없음(부족은 눌렀을 때 구매로 유도).
    private void ConfigPull(Button btn, ShopOfferDto offer, Image ticketIcon, TMP_Text ticketText) {
        if (btn != null) btn.gameObject.SetActive(offer != null);
        if (offer == null) return;

        SetIcon(ticketIcon, IconFor(offer.TicketKey));
        // 무료 표시 때 껐던 텍스트를 항상 다시 켠다(무료 사용 후 x1 표기가 사라지는 문제 방지). 무료면 UpdateFreeUI 가 다시 끈다.
        if (ticketText != null) { ticketText.gameObject.SetActive(true); ticketText.text = "× " + offer.GachaCount; }
        if (btn != null) btn.interactable = true;
    }

    // 현재 라인의 픽업 정보(이름/등급/일러스트/기간). 캐릭터 라인이면 캐릭터 트랙, 장비 라인이면 무기 트랙을 본다.
    // 픽업이 걸리지 않은 라인이거나 트랙이 비어 있으면 Has=false.
    private readonly struct Pickup {
        public readonly bool Has;
        public readonly string Name;
        public readonly Grade Grade;
        // 원본 데이터를 그대로 들고 있어 일러스트 슬롯이 관심점/프레이밍(Crop/Contain)을 스스로 정하게 한다.
        // 캐릭터는 크롭 초상, 장비 아이콘은 전체 표시로 각자 올바르게 잡힌다.
        public readonly IIllustrationSource Art;
        public readonly long StartUnix;
        public readonly long EndUnix;
        // 아래 둘은 캐릭터 픽업에만 있다(장비 픽업이면 빈 문자열). 표시측이 빈 값이면 해당 줄을 숨긴다.
        public readonly string EnglishName;
        public readonly string JapaneseName;

        public Pickup(string name, Grade grade, IIllustrationSource art, long startUnix, long endUnix,
                      string englishName = "", string japaneseName = "") {
            Has = true; Name = name; Grade = grade; Art = art; StartUnix = startUnix; EndUnix = endUnix;
            EnglishName = englishName ?? ""; JapaneseName = japaneseName ?? "";
        }
    }

    // 해당 오퍼(뽑기 라인)의 픽업 정보. 픽업 노출 여부는 서버가 내려준 HasPickup 하나로 판단한다
    // (라인 이름/티어 문자열에 의존하지 않는다). 탭마다 자기 픽업을 그릴 수 있도록 오퍼를 받는다.
    private Pickup PickupFor(ShopOfferDto offer) {
        if (offer == null || !offer.HasPickup || _catalog == null) return default;

        if (offer.GachaIsEquip) {
            if (_catalog.RotationEquipId == 0) return default;
            var d = _hud.EquipDb != null ? _hud.EquipDb.Get(_catalog.RotationEquipId) : null;
            if (d == null) return default;
            return new Pickup(d.DisplayName, d.Grade, d, _catalog.RotationEquipStartUnix, _catalog.RotationEquipEndUnix);
        }

        if (_catalog.RotationCharId == 0) return default;
        var c = _hud.CharDb != null ? _hud.CharDb.Get(_catalog.RotationCharId) : null;
        if (c == null) return default;
        return new Pickup(c.DisplayName, c.Grade, c, _catalog.RotationStartUnix, _catalog.RotationEndUnix,
                          c.EnglishName, c.JapaneseName);
    }

    private Pickup CurrentPickup() => PickupFor(_single ?? _ten);

    // 탭별 표시를 갱신한다. 콘텐츠 패널은 선택된 라인의 것만 켜고(라인마다 패널을 여러 개 둘 수 있다),
    // 픽업 일러스트와 남은 기간은 각 탭이 자기 라인의 픽업을 기준으로 채운다.
    private void UpdateTabVisuals() {
        if (bannerTabs == null) return;

        UpdateContentRoots();

        foreach (var t in bannerTabs) {
            // 픽업 조회는 오퍼 목록을 훑으므로, 이 탭이 실제로 픽업 표시를 쓸 때만 한다.
            if (t.pickupIllustration == null && t.remainingText == null) continue;

            var (single, ten) = FindOffers(t.key);
            var pickup = PickupFor(single ?? ten);

            if (t.pickupIllustration != null) {
                if (pickup.Has) t.pickupIllustration.Bind(pickup.Art);
                else t.pickupIllustration.Clear();
            }

            // 픽업이 없는 일반 라인은 종료 기한 자체가 없으므로 빈 문자열이 되어 오브젝트가 꺼진다.
            SetOptionalText(t.remainingText, pickup.Has ? KstTime.Remaining(pickup.EndUnix, NowUnix) : "");
        }
    }

    // 탭별 콘텐츠 패널을 켜고 끈다. 한 탭이 여러 패널을 가질 수 있고, 서로 다른 탭이 같은 패널을 공유할 수도 있다.
    // 그래서 끄기와 켜기를 두 번에 나눠 돈다. 한 번에 처리하면 공유 패널이 뒤에 순회된 비선택 탭에 의해 다시 꺼진다.
    private void UpdateContentRoots() {
        foreach (var t in bannerTabs)
            if (t.key != _poolKey) SetActiveAll(t.contentRoots, false);

        foreach (var t in bannerTabs)
            if (t.key == _poolKey) SetActiveAll(t.contentRoots, true);
    }

    private static void SetActiveAll(GameObject[] targets, bool on) {
        if (targets == null) return;
        foreach (var go in targets)
            if (go != null) go.SetActive(on);
    }

    private void UpdateRotationPanel() {
        // 공용 픽업 슬롯은 루트가 없어도(탭에만 일러스트를 두는 구성) 동작해야 하므로 따로 갱신한다.
        var current = CurrentPickup();
        if (rotationIllustration != null) {
            if (current.Has) rotationIllustration.Bind(current.Art);
            else rotationIllustration.Clear();
        }

        // 픽업 대상이 있어야 열 상세가 있다. 캐릭터를 찾지 못하는 데이터 상태에서도 빈 팝업이 뜨지 않게 막는다.
        if (rotationDetailButton != null)
            rotationDetailButton.gameObject.SetActive(PickupCharacter() != null);

        if (rotationRoot == null) return;
        var pickup = current;
        rotationRoot.SetActive(pickup.Has);
        if (!pickup.Has) return;

        if (rotationNameText != null) rotationNameText.text = pickup.Name;
        if (rotationGradeText != null) rotationGradeText.SetGrade(pickup.Grade);

        // 영문/일본어명은 캐릭터 픽업에만 있다. 장비 픽업에서 빈 줄이 남지 않도록 값이 없으면 오브젝트를 끈다.
        SetOptionalText(rotationEnglishNameText, pickup.EnglishName);
        SetOptionalText(rotationJapaneseNameText, pickup.JapaneseName);

        // 무기 타입 아이콘은 픽업 캐릭터의 직업에서 나온다. 장비 라인은 픽업 무기의 주인 캐릭터를 기준으로 하므로
        // 두 특수 라인이 같은 아이콘을 가리킨다(전용무기는 주인의 무기 타입을 그대로 쓴다).
        if (rotationWeaponIcon != null) {
            var owner = PickupCharacter();
            var weaponIcon = owner != null && weaponIconTable != null ? weaponIconTable.Get(owner.WeaponType) : null;
            rotationWeaponIcon.sprite = weaponIcon;
            rotationWeaponIcon.enabled = weaponIcon != null;
        }

        if (rotationPeriodText != null)
            rotationPeriodText.text = KstTime.Period(pickup.StartUnix, pickup.EndUnix);
    }

    // 값이 있으면 표시하고, 비어 있으면 오브젝트째 끈다(레이아웃 그룹에서 빈 줄이 자리를 차지하지 않게 한다).
    private static void SetOptionalText(TMP_Text label, string value) {
        if (label == null) return;
        bool has = !string.IsNullOrEmpty(value);
        label.gameObject.SetActive(has);
        if (has) label.text = value;
    }

    // 천장 안내(픽업 라인 전용): "N회 뽑기 내 SSR OOO 확정 획득 (획득 시 100회로 초기화)".
    // N = 남은 횟수 = 천장 - 현재 카운트. 확정 대상/등급은 현재 픽업에서 가져오므로 등급 개편에도 문구가 따라간다.
    private void UpdatePityText(ShopOfferDto head) {
        var pickup = CurrentPickup();
        bool show = pickup.Has && head != null;
        if (pityGroup != null) pityGroup.SetActive(show);
        else if (pityText != null) pityText.gameObject.SetActive(show);
        if (!show || pityText == null) return;

        int count = _catalog != null && _catalog.Pities != null && _catalog.Pities.TryGetValue(head.PoolKey, out var c) ? c : 0;
        int remaining = Mathf.Clamp(PityThreshold - count, 1, PityThreshold);

        // 남은 횟수 숫자와 확정 대상 이름에 각각 색을 입힌다(숫자=강조색, 이름=등급색으로 앱과 통일).
        string countTag = Colorize(remaining.ToString(), pityCountColor);
        string nameTag = GradeText.DefaultCatalog != null && GradeText.DefaultCatalog.TryGetColor(pickup.Grade, out var gradeColor)
            ? Colorize(pickup.Name, gradeColor)
            : pickup.Name;
        pityText.text = $"{countTag}회 모집 내 {pickup.Grade} {nameTag} 확정 모집";
    }

    // TMP 리치텍스트로 문자열에 색을 입힌다(pityText 는 Rich Text 활성 필요).
    private static string Colorize(string text, Color color) =>
        $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";

    private void OnPull(int count) {
        if (_busy) return;
        var offer = count >= 10 ? _ten : _single;
        if (offer == null) return;
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);

        // 1연: 무료 뽑기(무료 대상 라인, 24시간 1회)가 가능하면 무료로 진행.
        if (count == 1 && FreeAvailable(FreeKeyFor(_poolKey))) { FreePullAsync(offer).Forget(); return; }

        int ownedTickets = TicketBalance(offer.TicketKey);
        if (ownedTickets >= count) PullAndAnimateAsync(offer).Forget();
        else OpenTicketBuyAsync(offer, count - ownedTickets).Forget();
    }

    private async UniTaskVoid FreePullAsync(ShopOfferDto offer) {
        _busy = true;
        try {
            var r = await ShopPurchaseHelper.BuyAndApplyAsync(_hud, offer.OfferId, 1, _ct, free: true);
            if (!r.Result.IsSuccess) { _hud.Ui.ShowToast(ShopPurchaseHelper.BuyFailMessage(r.Result.FailReason)); return; }
            if (r.Catalog != null) _catalog = r.Catalog;

            if (offer.GachaIsEquip) await AnimateEquipAsync(r.Result.EquipRolls);
            else await AnimateCharAsync(r.Result.Rolls);

            if (_hud.Badges != null) _hud.Badges.RefreshAsync(_ct).Forget();   // 무료 뽑기 배지 갱신
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("뽑기에 실패했습니다.");
        } finally {
            _busy = false;
            Refresh();
        }
    }

    // ── 무료 뽑기 표시/판정 (무료 대상 라인의 1연, 24시간 1회) ──
    private void UpdateFreeUI() {
        if (_catalog == null) return;

        // 탭 배지: 각 탭이 자기 라인의 무료 가능 여부만 본다(무료 대상이 아닌 라인은 서버가 키를 안 보내 항상 false).
        if (bannerTabs != null)
            foreach (var t in bannerTabs)
                if (t.badge != null) t.badge.SetOn(FreeAvailable(FreeKeyFor(t.key)));

        // 현재 선택 라인의 무료 표시.
        string cur = FreeKeyFor(_poolKey);
        bool curFree = FreeAvailable(cur);

        if (pull1FreeLabel != null) pull1FreeLabel.SetActive(curFree);
        // 1연 버튼 배지도 탭 배지와 같은 판정을 쓴다(무료 가능 여부는 이 화면에서만 계산되므로 Source=Manual).
        if (pull1Badge != null) pull1Badge.SetOn(curFree);
        if (curFree) {   // 무료면 1연 버튼의 모집권/매수 표시를 숨긴다(ConfigPull 이 켜둔 것을 덮어씀).
            SetIcon(pull1TicketIcon, null);
            if (pull1TicketText != null) pull1TicketText.gameObject.SetActive(false);
        }

        bool showTimer = FreeSupported(cur);
        if (freeTimerGroup != null) freeTimerGroup.SetActive(showTimer);
        else if (freeTimerText != null) freeTimerText.gameObject.SetActive(showTimer);
        if (showTimer && freeTimerText != null)
            freeTimerText.text = curFree ? "무료 뽑기 가능" : $"무료까지 {FormatRemaining(FreeRemaining(cur))}";
    }

    // 1초 주기로 무료 남은 시간/가능 여부를 갱신(쿨다운이 끝나면 배지/문구가 자동으로 켜진다).
    private void Update() {
        if (_catalog == null) return;
        if (Time.unscaledTime < _nextFreeTick) return;
        _nextFreeTick = Time.unscaledTime + 1f;
        UpdateFreeUI();
    }

    // 뽑기 라인(PoolKey) -> 무료 뽑기 키. 서버 PoolKey 는 "gacha_" 접두사만 붙은 형태라(gacha_normal_char)
    // 접두사를 떼면 무료 뽑기 키(normal_char)가 된다. 무료 대상이 아닌 라인은 서버가 그 키를 아예 보내지 않으므로
    // 아래 판정이 자연히 false 가 된다(어느 라인이 무료인지는 서버가 정한다).
    private const string PoolKeyPrefix = "gacha_";

    private static string FreeKeyFor(string poolKey) =>
        !string.IsNullOrEmpty(poolKey) && poolKey.StartsWith(PoolKeyPrefix)
            ? poolKey.Substring(PoolKeyPrefix.Length)
            : poolKey;

    private static long NowUnix => KstTime.NowUnix;

    // 서버가 이 키를 무료 뽑기 대상으로 관리하는지(대상이면 남은 시간 안내를 노출한다).
    private bool FreeSupported(string key) =>
        !string.IsNullOrEmpty(key) && _catalog?.FreeGachaNextUnix != null && _catalog.FreeGachaNextUnix.ContainsKey(key);

    private bool FreeAvailable(string key) {
        if (string.IsNullOrEmpty(key) || _catalog == null || _catalog.FreeGachaNextUnix == null) return false;
        // 서버가 해당 키를 보냈을 때만 판정한다(미전송 시 오판으로 배지가 켜지는 것 방지). 신규 계정은 서버가 0을 보낸다.
        return _catalog.FreeGachaNextUnix.TryGetValue(key, out var next) && next <= NowUnix;
    }

    private long FreeRemaining(string key) {
        if (string.IsNullOrEmpty(key) || _catalog == null || _catalog.FreeGachaNextUnix == null) return 0;
        return _catalog.FreeGachaNextUnix.TryGetValue(key, out var next) ? Math.Max(0, next - NowUnix) : 0;
    }

    private static string FormatRemaining(long sec) {
        long h = sec / 3600, m = (sec % 3600) / 60, s = sec % 60;
        return $"{h:00}:{m:00}:{s:00}";
    }

    private async UniTaskVoid PullAndAnimateAsync(ShopOfferDto offer) {
        _busy = true;
        try {
            var r = await ShopPurchaseHelper.BuyAndApplyAsync(_hud, offer.OfferId, 1, _ct);
            if (!r.Result.IsSuccess) {
                // 재화 부족(2)이면 이 모집권을 사는 재화 페이지로 유도한다. 모집권 수량은 눌리기 전에 확인하므로
                // 여기 도달하는 경우는 드물지만, 서버가 최종 권위라 실패 응답은 항상 처리한다.
                string payCurrency = FindOffer(offer.TicketOfferId)?.Currency;
                if (r.Result.FailReason == 2 && !string.IsNullOrEmpty(payCurrency))
                    await RouteToCurrencyAsync(payCurrency);
                else
                    _hud.Ui.ShowToast(ShopPurchaseHelper.BuyFailMessage(r.Result.FailReason));
                return;
            }
            if (r.Catalog != null) _catalog = r.Catalog;

            if (offer.GachaIsEquip) await AnimateEquipAsync(r.Result.EquipRolls);
            else await AnimateCharAsync(r.Result.Rolls);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("뽑기에 실패했습니다.");
        } finally {
            _busy = false;
            Refresh();
        }
    }

    private async UniTask AnimateCharAsync(List<GachaRollDto> rolls) {
        var popup = await _hud.Ui.ShowPopupAsync<GachaPopup>(_hud.Popups.GachaPopup, _ct);
        popup.Play(rolls, _hud);
        await popup.WaitUntilClosedAsync();
    }

    private async UniTask AnimateEquipAsync(List<EquipRollDto> rolls) {
        var reference = _hud.Popups.EquipGachaPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _hud.Ui.ShowToast("장비를 획득했습니다."); return; }
        var popup = await _hud.Ui.ShowPopupAsync<EquipGachaPopup>(reference, _ct);
        popup.Play(rolls, _hud);
        await popup.WaitUntilClosedAsync();
    }

    // 소환권 부족: 해당 소환권을 파는 오퍼로 수량 선택 구매 팝업(예산 내). 결제 재화는 그 오퍼가 정한 것을 쓰므로
    // 일반 소환권(골드)과 특수 소환권(다이아)이 같은 경로로 처리된다. 결제 재화도 부족하면 그 재화 구매로 유도한다.
    private async UniTaskVoid OpenTicketBuyAsync(ShopOfferDto gacha, int shortfall) {
        if (_busy) return;
        _busy = true;
        try {
            var offer = FindOffer(gacha.TicketOfferId);
            if (offer == null) { _hud.Ui.ShowToast("모집권을 판매하지 않습니다."); return; }

            int owned = CurrencyBalance(offer.Currency);
            if (owned < offer.Price) { await RouteToCurrencyAsync(offer.Currency); return; }

            var reference = _hud.Popups.BuyConfirmPopup;
            if (reference == null || !reference.RuntimeKeyIsValid()) { _hud.Ui.ShowToast("준비 중입니다."); return; }
            var popup = await _hud.Ui.ShowPopupAsync<BuyConfirmPopup>(reference, _ct);
            int qty = await popup.ShowAsync(_hud, offer, IconFor(offer.Currency), owned, true, Mathf.Max(1, shortfall), clampToAffordable: true);
            if (qty <= 0) return;

            var r = await ShopPurchaseHelper.BuyAndApplyAsync(_hud, offer.OfferId, qty, _ct);
            if (!r.Result.IsSuccess) {
                if (r.Result.FailReason == 2) await RouteToCurrencyAsync(offer.Currency);
                else _hud.Ui.ShowToast(ShopPurchaseHelper.BuyFailMessage(r.Result.FailReason));
                return;
            }
            if (r.Catalog != null) _catalog = r.Catalog;
            _hud.Ui.ShowToast("모집권을 구매했습니다.");
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("구매에 실패했습니다.");
        } finally {
            _busy = false;
            Refresh();
        }
    }

    private async UniTaskVoid OpenRatesAsync() {
        var offer = _single ?? _ten;
        if (offer == null) return;
        try {
            var rates = await _hud.Shop.RequestGachaRatesAsync(offer.OfferId, _ct);
            var reference = _hud.Popups.GachaRatesPopup;
            if (reference == null || !reference.RuntimeKeyIsValid()) return;
            var popup = await _hud.Ui.ShowPopupAsync<GachaRatesPopup>(reference, _ct);
            popup.Show(rates, _hud.CharDb, _hud.EquipDb);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("확률 정보를 불러오지 못했습니다.");
        }
    }

    // 현재 라인의 픽업이 가리키는 캐릭터. 캐릭터 라인은 픽업 캐릭터 자신, 장비 라인은 그 전용무기의 주인이다
    // (전용무기 id = 캐릭터 id + 오프셋 규약이라 주인이 확정된다). 픽업이 없거나 찾지 못하면 null.
    private CharacterData PickupCharacter() {
        var offer = _single ?? _ten;
        if (offer == null || !offer.HasPickup || _catalog == null || _hud.CharDb == null) return null;

        int charId = offer.GachaIsEquip
            ? EquipmentRules.OwnerCharOf(_catalog.RotationEquipId)
            : _catalog.RotationCharId;
        return charId != 0 ? _hud.CharDb.Get(charId) : null;
    }

    private async UniTaskVoid OpenPickupDetailAsync() {
        var data = PickupCharacter();
        if (data == null) return;

        var reference = _hud.Popups.CharacterDetailPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _hud.Ui.ShowToast("준비 중입니다."); return; }
        try {
            var popup = await _hud.Ui.ShowPopupAsync<CharacterDetailPopup>(reference, _ct);
            popup.Initialize(_hud, data);
            await popup.ShowAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("캐릭터 정보를 불러오지 못했습니다.");
        }
    }

    private async UniTaskVoid OpenRotationInfoAsync() {
        try {
            var info = await _hud.Shop.RequestRotationInfoAsync(_ct);
            var reference = _hud.Popups.RotationInfoPopup;
            if (reference == null || !reference.RuntimeKeyIsValid()) return;
            var popup = await _hud.Ui.ShowPopupAsync<RotationInfoPopup>(reference, _ct);
            popup.Show(info, _hud.CharDb, _hud.EquipDb);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("로테이션 정보를 불러오지 못했습니다.");
        }
    }

    // 한 뽑기 라인(PoolKey)의 1연/10연 오퍼를 카탈로그에서 찾는다(가챠 뽑기수 최소=1연, 최대=10연).
    // 라인을 PoolKey 하나로만 식별하므로 캐릭/장비나 티켓 종류를 여기서 따질 필요가 없다.
    private (ShopOfferDto single, ShopOfferDto ten) FindOffers(string poolKey) {
        ShopOfferDto single = null, ten = null;
        if (!string.IsNullOrEmpty(poolKey) && _catalog != null && _catalog.Offers != null) {
            foreach (var o in _catalog.Offers) {
                if (!o.IsGacha || o.PoolKey != poolKey) continue;
                if (single == null || o.GachaCount < single.GachaCount) single = o;
                if (ten == null || o.GachaCount > ten.GachaCount) ten = o;
            }
        }
        return (single, ten);
    }

    private ShopOfferDto FindOffer(string offerId) {
        if (string.IsNullOrEmpty(offerId) || _catalog == null || _catalog.Offers == null) return null;
        foreach (var o in _catalog.Offers) if (o.OfferId == offerId) return o;
        return null;
    }

    // 재화 부족 유도. 상점이 페이지를 가진 재화(골드/다이아)면 그리로 보내고, 아니면 최소한 안내라도 띄운다.
    // 조용히 아무 일도 일어나지 않으면 버튼이 고장난 것처럼 보이므로 어떤 재화든 반응은 남긴다.
    private async UniTask RouteToCurrencyAsync(string currency) {
        if (currency == "gold" || currency == "diamond") {
            await CurrencyShortcut.RouteToShopAsync(_hud, currency, _ct);
            return;
        }
        _hud.Ui.ShowToast($"{CurrencyShortcut.DisplayName(currency)}이(가) 부족합니다.");
    }

    // 보유량 조회는 재화 키 -> 재화 종류 매핑(CurrencyKeys)을 거쳐 스토어에서 읽는다.
    // 모집권이든 골드/다이아든 같은 경로라 재화가 늘어도 여기를 고칠 일이 없다.
    private int CurrencyBalance(string currencyKey) {
        var type = CurrencyKeys.ToType(currencyKey);
        if (!type.HasValue) return 0;
        var rp = _hud.Store.Currency(type.Value);
        return rp != null ? rp.Value : 0;
    }

    private int TicketBalance(string ticketKey) => CurrencyBalance(ticketKey);

    private Sprite IconFor(string currency) => CurrencyIcon.ForKey(_hud, currency);

    private static void SetSelected(TabButton[] tabs, string key) {
        if (tabs == null) return;
        foreach (var t in tabs)
            if (t.selectedIndicator != null) t.selectedIndicator.SetActive(t.key == key);
    }

    private static void SetIcon(Image img, Sprite sprite) {
        if (img == null) return;
        img.sprite = sprite;
        img.enabled = sprite != null;
    }

}
