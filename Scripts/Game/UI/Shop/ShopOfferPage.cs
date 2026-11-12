using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 상점 대분류 1개의 페이지. 중분류 탭(+선택 시 전체 탭) + 판매 아이템 그리드 + 선택 아이템 상세/구매를 담당한다.
/// 하나의 스크립트로 여러 대분류 프리팹을 구성할 수 있도록 상세/스텝업/충전보상 패널 등은 모두 널 세이프다.
/// 오퍼는 구매확인 팝업(수량 선택 지원) 후 구매한다. 한정/스텝업은 구매완료/잠금 상태를 표시한다.
/// 가챠(뽑기)는 상점이 아니라 전용 가챠 팝업(GachaMenuPopup)에서 다룬다.
/// </summary>
public sealed class ShopOfferPage : MonoBehaviour {
    [Header("Sub Category")]
    [SerializeField] private Transform subTabBar;
    [SerializeField] private ShopSubTab subTabPrefab;

    [Header("Item Grid")]
    [SerializeField] private Transform itemGrid;
    [SerializeField] private ShopItemCard itemCardPrefab;
    [Tooltip("패키지/세트 상품(스텝업, 한정 최적화 세트 등) 전용 카드 프리팹. 구성품을 함께 표시한다. 비우면 일반 카드로 대체.")]
    [FormerlySerializedAs("stepupItemCardPrefab")]
    [SerializeField] private PackageItemCard packageItemCardPrefab;

    [Header("Detail / Buy")]
    [SerializeField] private TMP_Text detailNameText;
    [Tooltip("선택한 상품의 설명(상세 영역 전용). 상품 카드에는 표시하지 않는다.")]
    [SerializeField] private TMP_Text detailDescText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyPriceText;
    [Tooltip("보유 재화가 부족할 때 가격 텍스트에 적용할 색.")]
    [SerializeField] private Color insufficientPriceColor = new Color(0.90f, 0.20f, 0.20f, 1f);
    [SerializeField] private Image buyCurrencyIcon;

    [Header("Detail Rewards (구매 시 획득 구성품, 선택)")]
    [Tooltip("구성품 슬롯 그룹 루트(헤더 포함). 지급 구성품이 있는 상품일 때만 켠다. 비우면 슬롯 부모만 사용.")]
    [SerializeField] private GameObject detailRewardGroup;
    [Tooltip("구성품 슬롯을 생성할 부모(detailRewardGroup 자식).")]
    [SerializeField] private Transform detailRewardRoot;
    [Tooltip("구성품 1개를 표시할 공용 슬롯 프리팹(RewardSlotView).")]
    [SerializeField] private RewardSlotView detailRewardSlotPrefab;

    [Header("Rates (선택)")]
    [Tooltip("확률 보기 버튼. 선택 상품이 확률 표기 가능(HasRates: 가챠/랜덤 등급 상품)일 때만 노출.")]
    [SerializeField] private Button ratesButton;

    [Header("Costume Preview (선택)")]
    [Tooltip("코스튬 미리보기 버튼. 선택 상품의 구성품이 코스튬 1개일 때만 노출하고, 누르면 미리보기 팝업을 연다.")]
    [SerializeField] private Button costumePreviewButton;

    [Header("Withdrawal (현금 결제 상품 전용, 선택)")]
    [Tooltip("청약철회 안내 문구 + 규정 보기 버튼을 담는 루트. 현금(cash) 결제 상품을 선택했을 때만 켠다.")]
    [SerializeField] private GameObject withdrawalGroup;
    [Tooltip("청약철회 안내 문서를 여는 버튼(withdrawalGroup 자식). 홈페이지 문서를 앱 내부 웹뷰로 연다.")]
    [SerializeField] private Button withdrawalPolicyButton;

    [Header("Step-up (특수>스텝업 페이지 전용, 선택)")]
    [Tooltip("스텝업 판매기간 텍스트 루트. 현재 서브탭이 stepup 일 때만 노출.")]
    [SerializeField] private GameObject stepupPeriodRoot;
    [SerializeField] private TMP_Text stepupPeriodText;

    [Header("Charge Reward (선택)")]
    [Tooltip("누적 충전 보상 팝업을 여는 버튼. 할당한 페이지에서 서브탭과 무관하게 상시 노출한다. 필요 없는 페이지는 비워 둔다.")]
    [SerializeField] private Button chargeRewardButton;

    private const int BuildPerFrame = 8;   // 한 프레임에 생성할 상품 카드 수(나머지는 다음 프레임으로 분산).

    private MainHudContext _hud;
    private List<ShopOfferDto> _offers;
    private ShopCatalogDto _catalog;
    private bool _hasAllTab;
    private CancellationToken _ct;
    private CancellationTokenSource _itemCts;   // 중분류 전환/재구성 겹칠 때 이전 생성 취소용.

    private readonly List<ShopSubTab> _subTabs = new();
    private readonly List<ShopItemCard> _cards = new();
    private readonly List<RewardSlotView> _detailRewardSlots = new();   // 선택 상품의 구성품 슬롯(선택 바뀔 때마다 재생성)
    private string _currentSub;
    private string _initialSub;                  // 진입 시 바로 선택할 중분류(없으면 첫 탭).
    private Action<string, string> _onNavigate;  // 부모 상점에 대분류/중분류 이동 요청(재화 부족 유도 등).
    private Action<ShopCatalogDto> _onCatalogUpdated;  // 구매로 카탈로그가 갱신되면 부모 상점에 최신본 반영(카테고리 이동 시 stale 방지).

    private ShopOfferDto _selected;
    private Color _priceDefaultColor;   // 가격 텍스트 원래 색 캐시(부족 시 빨강에서 복원용).
    private bool _capturedPriceColor;
    private bool _busy;

    private const string AllSub = "all";

    public void Initialize(MainHudContext hud, List<ShopOfferDto> offersInMajor, bool hasAllTab, ShopCatalogDto catalog,
                           string initialSub = null, Action<string, string> onNavigate = null,
                           Action<ShopCatalogDto> onCatalogUpdated = null) {
        _hud = hud;
        _offers = offersInMajor ?? new List<ShopOfferDto>();
        _hasAllTab = hasAllTab;
        _catalog = catalog;
        _initialSub = initialSub;
        _onNavigate = onNavigate;
        _onCatalogUpdated = onCatalogUpdated;
        _ct = this.GetCancellationTokenOnDestroy();

        if (buyButton != null) buyButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); OnBuy(); });
        if (ratesButton != null) ratesButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); OpenRatesAsync().Forget(); });
        if (costumePreviewButton != null)
            costumePreviewButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); OpenCostumePreviewAsync().Forget(); });
        // 충전보상 버튼은 서브탭과 무관하게 상시 노출한다(프리팹에서 꺼둔 채 저장돼도 켜지도록 여기서 확정).
        if (chargeRewardButton != null) {
            chargeRewardButton.gameObject.SetActive(true);
            chargeRewardButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); OpenChargeRewardAsync().Forget(); });
        }
        if (withdrawalPolicyButton != null) withdrawalPolicyButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); OpenWithdrawalPolicyAsync().Forget(); });

        BuildSubTabs();
    }

    private void BuildSubTabs() {
        foreach (var t in _subTabs) if (t != null) Destroy(t.gameObject);
        _subTabs.Clear();

        // 서브탭 목록: hasAllTab 이면 맨 앞에 가상 "전체" 탭. 이어서 오퍼 등장 순의 중분류들.
        var seen = new List<string>();
        if (_hasAllTab) seen.Add(AllSub);
        foreach (var o in _offers) if (!seen.Contains(o.Sub)) seen.Add(o.Sub);

        if (subTabBar != null) subTabBar.gameObject.SetActive(true);
        if (subTabPrefab != null && subTabBar != null) {
            foreach (var sub in seen) {
                var tab = Instantiate(subTabPrefab, subTabBar);
                var captured = sub;
                tab.Bind(sub, SubDisplayName(sub), _ => SelectSub(captured));
                _subTabs.Add(tab);
            }
        }

        if (seen.Count > 0) {
            string start = !string.IsNullOrEmpty(_initialSub) && seen.Contains(_initialSub) ? _initialSub : seen[0];
            SelectSub(start);
        }
    }

    private void SelectSub(string sub) {
        _currentSub = sub;
        foreach (var t in _subTabs) t.SetSelected(t.Sub == sub);

        // "전체" 탭은 필터 없이 대분류 전 항목, 그 외는 해당 중분류만.
        var list = new List<ShopOfferDto>();
        foreach (var o in _offers) if (sub == AllSub || o.Sub == sub) list.Add(o);

        UpdateSubPanels(sub);
        BuildItems(list);
    }

    // 스텝업 판매기간 등 서브탭 전용 패널 노출/구성.
    // 충전보상 버튼은 서브탭과 무관하게 상시 노출이라 여기서 다루지 않는다(노출 페이지는 프리팹 할당으로 결정).
    private void UpdateSubPanels(string sub) {
        if (stepupPeriodRoot != null) {
            bool show = sub == "stepup";
            stepupPeriodRoot.SetActive(show);
            if (show) ApplyStepupPeriod();
        }
    }

    private async UniTaskVoid OpenChargeRewardAsync() {
        var reference = _hud.Popups.ChargeRewardPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _hud.Ui.ShowToast("준비 중입니다."); return; }
        try {
            var popup = await _hud.Ui.ShowPopupAsync<ChargeRewardPopup>(reference, _ct);
            popup.Initialize(_hud);
            await popup.ShowAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void ApplyStepupPeriod() {
        // 스텝업 오퍼의 판매기간(첫 오퍼 기준)을 노출.
        foreach (var o in _offers) {
            if (o.Sub == "stepup" && (o.SaleStartUnix != 0 || o.SaleEndUnix != 0)) {
                if (stepupPeriodText != null)
                    stepupPeriodText.text = $"{KstText(o.SaleStartUnix)} ~\n{KstText(o.SaleEndUnix)}";
                return;
            }
        }
    }

    // UTC epoch(초) -> KST 표기 문자열.
    private static string KstText(long unix) {
        if (unix <= 0) return "-";
        var kst = DateTimeOffset.FromUnixTimeSeconds(unix).ToOffset(TimeSpan.FromHours(9));
        return kst.ToString("yyyy. MM. dd HH:mm") + " KST";
    }

    private void BuildItems(List<ShopOfferDto> offers) => BuildItemsAsync(offers).Forget();

    private async UniTaskVoid BuildItemsAsync(List<ShopOfferDto> offers) {
        _itemCts?.Cancel();
        _itemCts?.Dispose();
        _itemCts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var ct = _itemCts.Token;

        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();

        if (itemCardPrefab == null || itemGrid == null || offers == null) return;

        ShopOfferDto first = offers.Count > 0 ? offers[0] : null;

        try {
            await AsyncBuild.SpawnChunkedAsync(offers, BuildPerFrame, o => {
                var card = SpawnCard(o);
                card.Bind(o, ItemSpriteFor(o), IconFor(o.Currency), OnCardClick, IllustrationFor(o));
                // 패키지 카드는 공용 바인딩 외에 구성품(구성 미러)까지 추가로 채운다.
                if (card is PackageItemCard package) package.BindPackage(_hud, PackageInfoFor(o));
                _cards.Add(card);
            }, ct);
        } catch (OperationCanceledException) {
            return;
        }

        if (first != null) SelectItem(first);
    }

    // 오퍼 종류에 맞는 카드 프리팹을 생성한다. 구성품 미러에 등록된 패키지/세트 오퍼, 한정 판매 상품,
    // 구성품이 둘 이상인 상품은 구성품 표시가 가능한 전용 카드를 쓴다. 마지막 조건 덕분에 서버에 새 패키지를
    // 추가해도 클라 미러를 고치지 않고 구성품 카드로 나타난다(구성은 서버가 보낸 지급 목록으로 그린다).
    private ShopItemCard SpawnCard(ShopOfferDto o) =>
        packageItemCardPrefab != null && UsesPackageCard(o)
            ? Instantiate(packageItemCardPrefab, itemGrid)
            : Instantiate(itemCardPrefab, itemGrid);

    private static bool UsesPackageCard(ShopOfferDto o) =>
        o.Flash || ShopPackageRewards.IsPackage(o.OfferId) || (!o.IsGacha && o.Grants.Count >= 2);

    // 카드에 그릴 구성품. 미러에 등록된 상품은 그 값을 쓰고, 한정 판매처럼 등록되지 않은 상품은
    // 서버가 함께 보내 준 지급 목록을 그대로 쓴다(새 상품이 늘어도 클라를 고칠 일이 없다).
    private static ShopPackageRewards.PackageInfo PackageInfoFor(ShopOfferDto o) =>
        ShopPackageRewards.Get(o.OfferId) ?? ShopPackageRewards.FromGrants(o.Grants);

    private void OnCardClick(ShopItemCard card) {
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        SelectItem(card.Offer);
    }

    private void OnDestroy() {
        _itemCts?.Cancel();
        _itemCts?.Dispose();
        _itemCts = null;
    }

    private void SelectItem(ShopOfferDto offer) {
        _selected = offer;
        foreach (var c in _cards) c.SetSelected(c.Offer == offer);

        if (detailNameText != null) {
            detailNameText.text = offer.DisplayName;
            if (!string.IsNullOrEmpty(offer.NameColorHex) && ColorUtility.TryParseHtmlString(offer.NameColorHex, out var col))
                detailNameText.color = col;
        }
        if (detailDescText != null) detailDescText.text = offer.Description ?? "";

        if (buyPriceText != null) {
            if (!_capturedPriceColor) { _priceDefaultColor = buyPriceText.color; _capturedPriceColor = true; }
            buyPriceText.text = offer.Price.ToString("N0");
            int owned = OwnedOf(offer.Currency, out bool balanceKnown);
            bool insufficient = balanceKnown && owned < offer.Price;
            buyPriceText.color = insufficient ? insufficientPriceColor : _priceDefaultColor;
        }
        SetBuyIcon(IconFor(offer.Currency));

        BuildDetailRewards(offer);

        // 확률 표기 가능 상품(가챠/랜덤 등급 상품)만 확률 보기 버튼 노출.
        if (ratesButton != null) ratesButton.gameObject.SetActive(offer.HasRates);

        // 코스튬 상품만 미리보기 버튼 노출.
        if (costumePreviewButton != null) costumePreviewButton.gameObject.SetActive(CostumeOf(offer) != null);

        // 현금(인앱) 결제 상품만 청약철회 안내/규정 버튼 노출.
        bool cash = offer.Currency == CurrencyIcon.CashKey;
        if (withdrawalGroup != null) withdrawalGroup.SetActive(cash);
        else if (withdrawalPolicyButton != null) withdrawalPolicyButton.gameObject.SetActive(cash);

        // 구매완료/잠금 상품은 구매 버튼 비활성.
        if (buyButton != null) buyButton.interactable = !(offer.SoldOut || offer.Locked);
    }

    // 선택 상품의 구매 시 획득 구성품을 공용 슬롯(RewardSlotView)으로 채운다.
    // 구성품 해석은 RewardVisualResolver 가 카탈로그로 처리하므로 품목이 늘어도 이 페이지는 수정할 필요가 없다.
    private void BuildDetailRewards(ShopOfferDto offer) {
        foreach (var s in _detailRewardSlots) if (s != null) Destroy(s.gameObject);
        _detailRewardSlots.Clear();

        // 가챠는 결과가 확률로 정해져 구성품이 없다(확률 보기 버튼으로 안내).
        // 구성품은 카드 미리보기와 같은 출처(미러 우선, 없으면 서버 지급 목록)를 써서 카드와 상세가 어긋나지 않게 한다.
        var grants = ShopPackageRewards.DisplayGrants(offer);
        bool hasGrants = grants.Count > 0;
        if (detailRewardGroup != null) detailRewardGroup.SetActive(hasGrants);
        if (!hasGrants || detailRewardRoot == null || detailRewardSlotPrefab == null) return;

        foreach (var item in grants) {
            var slot = Instantiate(detailRewardSlotPrefab, detailRewardRoot);
            slot.Bind(RewardVisualResolver.Resolve(_hud, item));
            _detailRewardSlots.Add(slot);
        }
    }

    // 청약철회 안내는 약관과 같은 성격의 고지 문서다. 문구가 법령/운영 변경으로 자주 바뀌는데 앱에 넣어 두면
    // 스토어 심사를 거쳐야 반영되므로, 홈페이지 문서를 약관과 똑같이 앱 내부 웹뷰로 보여 준다(주소는 서버가 내려준다).
    private async UniTaskVoid OpenWithdrawalPolicyAsync() {
        try {
            TermsDocumentDto doc;
            using (_hud.Ui.ShowLoading()) {
                var status = await _hud.Terms.GetStatusAsync(_ct);
                doc = status?.FindDocument(TermsRules.WithdrawalPolicyKey);
            }
            if (doc == null) { _hud.Ui.ShowToast("준비 중입니다."); return; }
            await _hud.Terms.ShowDocumentAsync(doc.Url, doc.Title, _ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("문서를 불러오지 못했습니다.");
        }
    }

    private async UniTaskVoid OpenRatesAsync() {
        if (_selected == null || !_selected.HasRates) return;
        try {
            var rates = await _hud.Shop.RequestGachaRatesAsync(_selected.OfferId, _ct);
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

    private void OnBuy() {
        if (_selected == null || _busy) return;
        if (_selected.SoldOut) { _hud.Ui.ShowToast("이미 구매한 상품입니다."); return; }
        if (_selected.Locked) {
            _hud.Ui.ShowToast(_selected.RequiredGuildLevel > 0
                ? ShopPurchaseHelper.GuildLevelRequiredMessage(_selected.RequiredGuildLevel)
                : "지금은 구매할 수 없습니다.");
            return;
        }

        ConfirmThenBuyAsync(_selected).Forget();
    }

    // ── 비가챠: 구매확인(수량 선택) 후 구매 ──
    // 재화 결제 상품은 보유량으로 살 수 있는 최대치까지만 슬라이더 범위를 준다(예산 클램프: 300짜리를 700 보유면 1~2).
    // 1개도 못 사면 구매확인을 열지 않고 "부족" 안내 + 해당 재화 페이지로 유도한다(가챠와 동일). 현금 결제는 언제든 구매확인을 연다.
    private async UniTaskVoid ConfirmThenBuyAsync(ShopOfferDto offer) {
        _busy = true;
        try {
            int owned = OwnedOf(offer.Currency, out bool balanceKnown);

            // 재화 결제인데 1개도 못 사면: 다이아/골드면 부족 안내 후 해당 재화 페이지로 유도하고 종료.
            // (라우팅 불가한 재화(티켓 등)면 아래로 내려가 구매확인에서 부족 상태를 보여준다.)
            if (balanceKnown && offer.Price > 0 && owned < offer.Price) {
                if (await TryRouteInsufficientAsync(offer.OfferId)) return;
            }

            var popup = await _hud.Ui.ShowPopupAsync<BuyConfirmPopup>(_hud.Popups.BuyConfirmPopup, _ct);
            int qty = await popup.ShowAsync(_hud, offer, IconFor(offer.Currency), owned, balanceKnown, clampToAffordable: true);
            if (qty <= 0) return;

            await DoBuyAsync(offer, qty);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("구매에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    private async UniTask DoBuyAsync(ShopOfferDto offer, int qty) {
        var res = await BuyAndApplyAsync(offer.OfferId, qty);
        if (res == null) return;   // 실패(토스트는 BuyAndApplyAsync 내부 처리)

        // 한정 판매는 사고 나면 "살 수 있는 상품"이 하나 줄어든다. 메인 화면 버튼의 개수 배지가
        // 서버 기준으로 바로 맞도록 목록을 다시 받는다(다 사면 버튼이 사라진다).
        if (offer.Flash) _hud.FlashSale.RefreshAsync(_ct).Forget();

        if (res.Rolls.Count > 0) {
            await OpenGachaResultAsync(res.Rolls);
        } else if (res.EquipRolls.Count > 0) {
            await OpenEquipGachaResultAsync(res.EquipRolls);
        } else {
            await ResyncInventoryAsync();
            // 보유 자체가 바뀌는 지급(각인 도면/소모품/코스튬)은 인벤토리 스냅샷에 없으므로 지급 목록으로 직접 반영한다.
            _hud.Store.ApplyGrantedRewards(res.Grants);
            RebuildFromCatalog();   // 한정 구매완료/잠금 상태 반영(재화 동기화에서 갱신된 카탈로그 사용)
            await ShowPurchaseCompleteAsync(offer, qty, res);
        }
    }

    // 구매 완료 표시. 무엇을 받았는지 획득 슬롯으로 보여준다(구매확인 팝업과 같은 표시 규칙).
    // 보여줄 항목이 없거나 팝업 프리팹을 할당하지 않은 경우에는 기존처럼 토스트로 알린다
    // (표시 수단이 없다고 해서 구매 결과를 아무것도 알리지 않게 되면 안 된다).
    private async UniTask ShowPurchaseCompleteAsync(ShopOfferDto offer, int qty, BuyResultDto result) {
        var rewards = GrantedRewards(offer, qty, result);
        var reference = _hud.Popups.PurchaseCompletePopup;
        if (rewards.Count == 0 || reference == null || !reference.RuntimeKeyIsValid()) {
            _hud.Ui.ShowToast(PurchaseCompletePopup.PurchaseMessage);
            return;
        }

        var popup = await _hud.Ui.ShowPopupAsync<PurchaseCompletePopup>(reference, _ct);
        await popup.ShowAsync(_hud, offer, rewards);
    }

    // 표시할 획득 항목. 서버가 실제로 지급한 목록을 우선 쓴다. 랜덤이 걸린 상품(랜덤 등급 만능조각 등)은
    // 이 목록에만 확정 결과가 담기므로, 카탈로그의 예고 구성품으로는 무엇을 받았는지 알 수 없다.
    // 서버가 목록을 주지 않는 경우에만(구버전 서버 등) 예고 구성품 x 구매 수량으로 대신한다.
    private static List<RewardItem> GrantedRewards(ShopOfferDto offer, int qty, BuyResultDto result) {
        if (result != null && result.Grants.Count > 0) return result.Grants;

        var list = new List<RewardItem>();
        if (offer == null || offer.Grants == null) return list;

        int multiplier = Mathf.Max(1, qty);
        foreach (var g in offer.Grants) list.Add(g.Multiplied(multiplier));
        return list;
    }

    // 실제 구매 + 결과 반영(공용 경로). 성공 시 결과 DTO, 실패 시 null 반환. 재화 부족(다이아/골드)은 확인 후 해당 페이지로 유도.
    private async UniTask<BuyResultDto> BuyAndApplyAsync(string offerId, int qty = 1) {
        // 결제 수단을 함께 넘긴다. 현금 상품이면 공용 경로가 스토어 결제부터 태운다.
        var offer = _catalog?.Offers?.FirstOrDefault(o => o.OfferId == offerId);
        var r = await ShopPurchaseHelper.BuyAndApplyAsync(_hud, offerId, qty, _ct, currency: offer?.Currency,
                                                          nonConsumable: offer != null && offer.NonConsumable);
        if (!r.Result.IsSuccess) {
            // 사용자가 스토어 결제 창을 스스로 닫은 경우는 실패 안내를 띄우지 않는다(정상 경로다).
            if (r.Result.FailReason == ShopPurchaseHelper.CancelledFail) return null;
            if (r.Result.FailReason != 2 || !await TryRouteInsufficientAsync(offerId))
                _hud.Ui.ShowToast(ShopPurchaseHelper.BuyFailMessage(r.Result.FailReason));
            return null;
        }
        if (r.Catalog != null) {
            _catalog = r.Catalog;
            _onCatalogUpdated?.Invoke(_catalog);   // 부모 상점 카탈로그도 최신화(다른 카테고리 이동 시 SoldOut/Locked 유지)
        }
        return r.Result;
    }

    // 재화 부족 시: 다이아/골드로 결제하는 오퍼면 "부족합니다. 구매하시겠습니까?" 확인 후 상점의 해당 재화 페이지로 이동한다.
    // 이동을 수행하면 true(호출부는 토스트 생략). 그 외(cash/티켓 등)는 false.
    private async UniTask<bool> TryRouteInsufficientAsync(string offerId) {
        var offer = FindOffer(offerId);
        if (offer == null || _onNavigate == null) return false;
        string sub = offer.Currency == "diamond" ? "diamond" : offer.Currency == "gold" ? "gold" : null;
        if (sub == null) return false;

        var reference = _hud.Popups.ConfirmCancelPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return false;
        string name = CurrencyShortcut.DisplayName(offer.Currency);
        var confirm = await _hud.Ui.ShowPopupAsync<ConfirmCancelPopup>(reference, _ct);
        bool yes = await confirm.ShowAsync($"{name} 부족", $"{name}가 부족합니다. 구매하시겠습니까?");
        if (!yes) return true;   // 취소도 "처리됨"으로 보고 토스트를 생략.
        _onNavigate.Invoke("currency", sub);
        return true;
    }

    private ShopOfferDto FindOffer(string offerId) {
        if (_catalog != null && _catalog.Offers != null)
            foreach (var o in _catalog.Offers) if (o.OfferId == offerId) return o;
        foreach (var o in _offers) if (o.OfferId == offerId) return o;
        return null;
    }

    // 구매로 캐릭터/장비가 지급된 경우(예: 패키지)의 획득 연출. 가챠 연출 팝업을 결과 표시 전용으로 재사용한다.
    private async UniTask OpenGachaResultAsync(List<GachaRollDto> rolls) {
        var popup = await _hud.Ui.ShowPopupAsync<GachaPopup>(_hud.Popups.GachaPopup, _ct);
        popup.Play(rolls, _hud);
        await popup.WaitUntilClosedAsync();
    }

    private async UniTask OpenEquipGachaResultAsync(List<EquipRollDto> rolls) {
        var reference = _hud.Popups.EquipGachaPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) {
            _hud.Ui.ShowToast("장비를 획득했습니다.");
            return;
        }
        var popup = await _hud.Ui.ShowPopupAsync<EquipGachaPopup>(reference, _ct);
        popup.Play(rolls, _hud);
        await popup.WaitUntilClosedAsync();
    }

    private async UniTask ResyncInventoryAsync() {
        try {
            var inv = await _hud.Inventory.RequestInventoryAsync(_ct);
            _hud.Store.ApplyInventory(inv);
        } catch (OperationCanceledException) {
        }
    }

    // 최신 카탈로그(_catalog: 재화 동기화에서 갱신됨)로 현재 대분류 오퍼를 재구성해 구매완료/잠금 상태를 반영한다.
    private void RebuildFromCatalog() {
        if (_catalog == null || _offers == null) return;
        string major = _offers.Count > 0 ? _offers[0].Major : null;
        if (major == null) return;
        var updated = new List<ShopOfferDto>();
        foreach (var o in _catalog.Offers) if (o.Major == major) updated.Add(o);
        _offers = updated;
        SelectSub(_currentSub);   // 그리드 재구성(오버레이/버튼 상태 반영)
    }

    // 재화 문자열 -> 현재 보유량. balanceKnown=false 면 보유량을 알 수 없는 재화(인앱결제 등).
    private int OwnedOf(string currency, out bool balanceKnown) {
        var type = CurrencyTypeOf(currency);
        if (type.HasValue && _hud != null) {
            var rp = _hud.Store.Currency(type.Value);
            if (rp != null) { balanceKnown = true; return rp.Value; }
        }
        balanceKnown = false;
        return 0;
    }

    // 재화 키 -> 재화 종류 매핑은 CurrencyKeys 가 단일 소스다(여기서 다시 나열하면 새 재화마다 두 곳을 고쳐야 한다).
    private static CurrencyType? CurrencyTypeOf(string currency) => CurrencyKeys.ToType(currency);

    private void SetBuyIcon(Sprite icon) {
        if (buyCurrencyIcon == null) return;
        buyCurrencyIcon.sprite = icon;
        buyCurrencyIcon.enabled = icon != null;
    }

    // 상품 카드 이미지. 전용 상품 이미지가 있으면 그것을 쓰고, 없으면 지급 구성의 아이콘으로 대신한다.
    // 구성품이 하나뿐인 상품은 그 품목이 곧 상품이라 같은 그림이 맞고(상세 영역도 같은 아이콘을 쓴다),
    // 덕분에 단순 지급 상품을 새로 추가할 때 전용 이미지를 따로 만들지 않아도 빈 칸이 나오지 않는다.
    // 구성품이 여럿인 패키지는 무엇을 대표로 삼을지 정할 수 없으므로 폴백하지 않는다(패키지 카드가 구성품을 따로 보여준다).
    private Sprite ItemSpriteFor(ShopOfferDto offer) {
        var sprite = _hud != null && _hud.ShopIcons != null ? _hud.ShopIcons.Get(offer.ImageKey) : null;
        if (sprite != null) return sprite;

        if (offer.Grants != null && offer.Grants.Count == 1)
            return RewardVisualResolver.Resolve(_hud, offer.Grants[0]).Icon;
        return null;
    }

    // 카드 그림을 아이콘이 아니라 일러스트로 잡아야 하는 상품의 원본. 코스튬은 상품 그림이 전신 일러스트라
    // 아이콘처럼 슬롯에 맞춰 넣으면 인물이 작게 뭉개진다. 원본을 그대로 넘겨 카드가 관심점 기준으로
    // 크롭/확대하게 한다(그림 안의 어디를 볼지는 CostumeData 가 스스로 정한다).
    // 전용 상품 이미지를 등록해 둔 상품은 그 그림이 카드에 맞게 그려진 것이므로 아이콘 그대로 쓴다.
    private IIllustrationSource IllustrationFor(ShopOfferDto offer) {
        if (_hud == null || _hud.ShopIcons == null) return null;
        if (_hud.ShopIcons.Get(offer.ImageKey) != null) return null;

        return CostumeOf(offer);
    }

    // 구성품이 코스튬 1개인 상품의 코스튬(그 외 상품이면 null). 카드 그림과 미리보기 버튼이 같은 판정을 쓴다.
    private CostumeData CostumeOf(ShopOfferDto offer) {
        if (offer == null || offer.Grants == null || offer.Grants.Count != 1) return null;
        if (offer.Grants[0].Kind != RewardItemKind.Costume) return null;
        if (_hud == null || _hud.CostumeDb == null) return null;

        return _hud.CostumeDb.Get(offer.Grants[0].RefId);
    }

    // 선택한 코스튬 상품의 미리보기(보기 전용). 구매는 이 페이지의 구매 버튼이 그대로 맡는다.
    private async UniTaskVoid OpenCostumePreviewAsync() {
        var costume = CostumeOf(_selected);
        if (costume == null) return;

        var reference = _hud.Popups.CostumePreviewPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;
        try {
            var popup = await _hud.Ui.ShowPopupAsync<CostumePreviewPopup>(reference, _ct);
            popup.Bind(_hud, costume.Id);
            await popup.ShowAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private Sprite IconFor(string currency) => CurrencyIcon.ForKey(_hud, currency);

    private static string SubDisplayName(string sub) => sub switch {
        "all" => "전체",
        "character" => "캐릭터",
        "equip" => "장비",
        "diamond" => "다이아",
        "gold" => "골드",
        "ticket" => "모집권",
        "shard" => "만능조각",
        "stamina" => "기력",
        "etc" => "기타",
        "profile_frame" => "테두리",
        "costume" => "코스튬",
        "unit_skin" => "아웃핏",
        "stepup" => "스텝업",
        "daily" => "일간",
        "weekly" => "주간",
        "monthly" => "월간",
        "account" => "계정",
        _ => sub
    };

}
