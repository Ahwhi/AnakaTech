using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 판매 아이템 카드. 이름(색상 적용)/가격/재화 아이콘과 선택 프레임을 표시한다(PLANNING 8).
/// 구성품 표시가 필요한 패키지/세트 카드(PackageItemCard)는 이 카드를 상속해 공용 바인딩을 재사용한다.
/// </summary>
public class ShopItemCard : MonoBehaviour {
    [Tooltip("상품 이미지. 서버 오퍼의 ImageKey 로 ShopIconCatalog 에서 조회한 스프라이트.")]
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;
    [Tooltip("한정 구매완료 시 노출할 오버레이(예: '구매완료'). 선택.")]
    [SerializeField] private GameObject soldOutOverlay;
    [Tooltip("판매기간 아님/선행단계 미구매 등 잠금 시 노출할 오버레이. 선택. 없으면 soldOutOverlay 를 대체 사용.")]
    [SerializeField] private GameObject lockedOverlay;
    [Tooltip("선택. 잠금 사유 문구(lockedOverlay 자식 권장). 길드 레벨 상품은 '길드 레벨 n이상', 그 외 잠금은 '잠금'.")]
    [SerializeField] private TMP_Text lockedText;

    [Header("Limit (한정 상품 전용, 선택)")]
    [Tooltip("한정 상품일 때만 노출할 잔여수량 루트(자식에 limitCountText 배치).")]
    [SerializeField] private GameObject limitGroup;
    [Tooltip("남은 구매 가능 횟수 텍스트(limitGroup 자식). 예: '구매가능횟수: 1'.")]
    [SerializeField] private TMP_Text limitCountText;

    [Tooltip("추천 상품(서버 카탈로그의 Hot)일 때만 켜는 HOT 태그 오브젝트. 선택.")]
    [SerializeField] private GameObject hotTag;

    [Header("Sale Period (기간제 상품 전용, 선택)")]
    [Tooltip("판매 종료일이 있는 기간제 상품일 때만 노출할 한정상품 그룹(자식에 salePeriodText 배치).")]
    [SerializeField] private GameObject salePeriodGroup;
    [Tooltip("남은 판매 기간 텍스트(salePeriodGroup 자식). 예: '3일 남음', '오늘 종료'.")]
    [SerializeField] private TMP_Text salePeriodText;

    public ShopOfferDto Offer { get; private set; }

    // 상품 그림을 일러스트로 잡을 때의 프레이밍(맥락 축). 카드가 작아 전신을 그대로 넣으면 인물이 뭉개지므로
    // 크게 확대해 관심점 주변만 보여 주고, 그 관심점을 슬롯 한가운데에 둔다.
    // 그림 안의 관심점 자체는 원본(IIllustrationSource.UiFocus)이 정하므로 여기서는 슬롯 쪽만 정한다.
    private const float IllustrationCoverScale = 5f;
    private static readonly Vector2 IllustrationFocusAnchor = new Vector2(0.5f, 0.5f);

    private CharacterIllustrationView _illustrationSlot;

    /// <param name="illustration">
    /// 상품 그림을 아이콘이 아니라 일러스트로 잡아야 하는 상품(코스튬 등)의 원본. null 이면 itemSprite 를
    /// 기존처럼 아이콘으로 표시한다. 일러스트 슬롯은 프리팹에 미리 두지 않고 여기서 만들어 붙인다.
    /// </param>
    public void Bind(ShopOfferDto offer, Sprite itemSprite, Sprite currencySprite, Action<ShopItemCard> onClick,
                     IIllustrationSource illustration = null) {
        Offer = offer;
        if (hotTag != null) hotTag.SetActive(offer.Hot);
        if (soldOutOverlay != null) soldOutOverlay.SetActive(offer.SoldOut);
        if (lockedOverlay != null) lockedOverlay.SetActive(offer.Locked);
        else if (soldOutOverlay != null && offer.Locked) soldOutOverlay.SetActive(true);
        if (lockedText != null) lockedText.text = offer.Locked ? ShopPurchaseHelper.LockedLabel(offer) : "";

        // 한정 상품(구매 제한 있음)만 잔여 구매 안내를 표시한다. 문구 규칙은 ShopLimitRules 가 정한다
        // (예: "일간 1/2", "월간 0/1", 구독형 진행 중이면 "만료 후 구매가능").
        string limitText = ShopLimitRules.Format(offer.LimitType, offer.Remaining, offer.LimitCount);
        bool limited = !string.IsNullOrEmpty(limitText);
        if (limitGroup != null) limitGroup.SetActive(limited);
        else if (limitCountText != null) limitCountText.gameObject.SetActive(limited);
        if (limited && limitCountText != null) limitCountText.text = limitText;

        // 판매 종료일이 있는 기간제 상품만 남은 기간을 표시한다(상시 상품/이미 종료된 상품은 숨김).
        // 문구는 이벤트 페이지와 같은 정책(EventPeriod)을 써서 앱 전체 표기를 통일한다.
        _saleEndKst = offer.SaleEndUnix > 0 ? KstTime(offer.SaleEndUnix) : DateTime.MinValue;
        RefreshSalePeriod();

        if (illustration != null) ShowIllustration(illustration);
        else ShowIcon(itemSprite);

        if (nameText != null) {
            nameText.text = offer.DisplayName;
            if (!string.IsNullOrEmpty(offer.NameColorHex) && ColorUtility.TryParseHtmlString(offer.NameColorHex, out var c))
                nameText.color = c;
        }
        if (priceText != null) priceText.text = offer.Price.ToString("N0");
        if (currencyIcon != null) {
            currencyIcon.sprite = currencySprite;
            currencyIcon.enabled = currencySprite != null;
        }
        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }

    // 상품 그림을 아이콘으로 표시(기본). 일러스트 슬롯이 이미 만들어져 있으면 비워 둔다
    // (카드는 재사용되므로 코스튬 자리에 다른 상품이 들어올 수 있다).
    private void ShowIcon(Sprite sprite) {
        if (_illustrationSlot != null) _illustrationSlot.Clear();
        if (itemImage == null) return;
        itemImage.sprite = sprite;
        itemImage.enabled = sprite != null;
    }

    // 상품 그림을 일러스트로 표시. 상품 이미지 자리를 그대로 슬롯으로 쓰고, 확대분이 카드 밖으로
    // 넘치지 않도록 같은 자리에 마스크를 함께 붙인다(프리팹 손질 없이 코드에서 구성).
    private void ShowIllustration(IIllustrationSource source) {
        var slot = EnsureIllustrationSlot();
        if (slot == null) { ShowIcon(null); return; }

        // 슬롯이 만든 자식 Image 가 그림을 그리므로, 원래의 상품 이미지는 꺼 둔다(두 장이 겹쳐 보이는 것 방지).
        if (itemImage != null) { itemImage.sprite = null; itemImage.enabled = false; }
        slot.Bind(source);
    }

    private CharacterIllustrationView EnsureIllustrationSlot() {
        if (_illustrationSlot != null) return _illustrationSlot;
        if (itemImage == null) return null;

        var go = itemImage.gameObject;
        if (go.GetComponent<RectMask2D>() == null) go.AddComponent<RectMask2D>();

        _illustrationSlot = go.GetComponent<CharacterIllustrationView>();
        if (_illustrationSlot == null) _illustrationSlot = go.AddComponent<CharacterIllustrationView>();
        _illustrationSlot.Configure(IllustrationCoverScale, IllustrationFocusAnchor);
        return _illustrationSlot;
    }

    // 남은 기간 표시. 24시간 이하로 남으면 EventPeriod 가 시:분:초를 돌려주므로 초마다 다시 그린다.
    private void RefreshSalePeriod() {
        string remainText = _saleEndKst != DateTime.MinValue
            ? EventPeriod.FormatRemainingUntil(_saleEndKst)
            : string.Empty;

        bool showPeriod = !string.IsNullOrEmpty(remainText);
        if (salePeriodGroup != null) salePeriodGroup.SetActive(showPeriod);
        else if (salePeriodText != null) salePeriodText.gameObject.SetActive(showPeriod);
        if (showPeriod && salePeriodText != null) salePeriodText.text = remainText;
    }

    // 카운트다운 구간(24시간 이하)일 때만 1초마다 갱신한다. 일 단위 표기는 하루에 한 번만 바뀌므로
    // 매 프레임은커녕 매 초도 다시 그릴 이유가 없다(상점은 카드가 수십 장 깔리는 화면이다).
    private void Update() {
        if (_saleEndKst == DateTime.MinValue || !EventPeriod.IsCountingDown(_saleEndKst)) return;

        _tick += Time.unscaledDeltaTime;
        if (_tick < 1f) return;
        _tick = 0f;
        RefreshSalePeriod();
    }

    // 서버가 내려준 UTC epoch(초) -> KST 시각. 기간 판정/표기는 기기 타임존과 무관하게 KST 로 맞춘다.
    private static DateTime KstTime(long unix) =>
        DateTimeOffset.FromUnixTimeSeconds(unix).ToOffset(TimeSpan.FromHours(9)).DateTime;

    private DateTime _saleEndKst = DateTime.MinValue;   // 이 상품의 판매 종료 시각(없으면 MinValue).
    private float _tick;                                 // 카운트다운 갱신 누적 시간(초).

    public void SetSelected(bool selected) {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }
}
