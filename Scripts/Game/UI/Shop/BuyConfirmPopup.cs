using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 구매 확인 팝업. 선택한 상품 정보와 필요 재화량, 현재 보유 재화량, 구매 시 받을 구성품을 표시한다.
/// 수량 선택 가능 상품(offer.AllowQty)은 슬라이더/＋－ 버튼으로 수량을 조절하고 총가격/부족/구성품 수량을 실시간 갱신한다.
/// 보유 재화가 부족하면 보유량 텍스트를 부족 색(빨강)으로 바꾸고 확인 버튼을 비활성화한다.
/// 확인 시 선택 수량(>=1), 취소 시 0 을 await 로 돌려준다.
///   int qty = await popup.ShowAsync(hud, offer, icon, owned, balanceKnown);
/// </summary>
public sealed class BuyConfirmPopup : UIView {
    [Header("Header")]
    [Tooltip("선택. 상단 제목(예: 구매 확인).")]
    [SerializeField] private TMP_Text titleText;

    [Header("Item / Price")]
    [SerializeField] private TMP_Text itemNameText;
    [Tooltip("필요 재화량(수량 선택 시 총가격).")]
    [SerializeField] private TMP_Text priceText;
    [Tooltip("가격 옆 재화 아이콘.")]
    [SerializeField] private Image priceCurrencyIcon;

    [Header("Owned")]
    [Tooltip("현재 보유 재화량.")]
    [SerializeField] private TMP_Text ownedText;
    [Tooltip("선택. 보유량 옆 재화 아이콘(가격 아이콘과 동일 스프라이트).")]
    [SerializeField] private Image ownedCurrencyIcon;
    [Tooltip("선택. 재화 부족 시 노출할 안내 오브젝트(예: '재화가 부족합니다').")]
    [SerializeField] private GameObject insufficientHint;
    [Tooltip("재화 부족 시 보유량 텍스트 색.")]
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.20f, 0.20f, 1f);

    [Header("Quantity (수량 선택 상품 전용, 선택)")]
    [Tooltip("수량 선택 UI 루트. offer.AllowQty 가 아니면 비활성.")]
    [SerializeField] private GameObject quantityGroup;
    [SerializeField] private Slider quantitySlider;
    [SerializeField] private TMP_Text quantityValueText;
    [SerializeField] private Button quantityMinusButton;
    [SerializeField] private Button quantityPlusButton;
    [Tooltip("선택. 최대 수량 버튼(구매 가능 최대치로 설정).")]
    [SerializeField] private Button quantityMaxButton;

    [Header("Rewards (구매 시 획득 구성품, 선택)")]
    [Tooltip("구성품 슬롯 그룹 루트. 구성품이 없는 상품(가챠 등)에서는 자동으로 꺼진다.")]
    [SerializeField] private GameObject rewardGroup;
    [Tooltip("구성품 슬롯을 생성할 부모(rewardGroup 자식).")]
    [SerializeField] private Transform rewardRoot;
    [Tooltip("구성품 1개를 표시할 공용 슬롯 프리팹(RewardSlotView).")]
    [SerializeField] private RewardSlotView rewardSlotPrefab;

    [Header("Buttons")]
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    private UniTaskCompletionSource<int> _result;
    private Color _ownedDefaultColor = Color.white;
    private bool _capturedOwnedColor;

    private BattleHudContext _hud;   // 구성품 아이콘/이름 해석용 카탈로그 묶음(RewardVisualResolver 가 사용).
    private ShopOfferDto _offer;
    private int _owned;
    private bool _balanceKnown;
    private int _qty = 1;
    private int _effectiveMax = 1;   // 수량 상한(offer.MaxQty, 예산 클램프 적용 시 구매 가능 최대치).
    private readonly List<RewardSlotView> _rewardSlots = new();

    protected override void Awake() {
        base.Awake();
        if (ownedText != null && !_capturedOwnedColor) {
            _ownedDefaultColor = ownedText.color;
            _capturedOwnedColor = true;
        }
        if (okButton != null) okButton.onClick.AddListener(() => Resolve(_qty));
        if (cancelButton != null) cancelButton.onClick.AddListener(() => Resolve(0));
        if (quantityMinusButton != null) quantityMinusButton.onClick.AddListener(() => ChangeQty(-1));
        if (quantityPlusButton != null) quantityPlusButton.onClick.AddListener(() => ChangeQty(1));
        if (quantityMaxButton != null) quantityMaxButton.onClick.AddListener(() => SetQty(_effectiveMax));
        if (quantitySlider != null) quantitySlider.onValueChanged.AddListener(v => SetQty(Mathf.RoundToInt(v)));
    }

    /// <param name="hud">구성품 표시에 쓰는 카탈로그 묶음(아이콘/이름 해석).</param>
    /// <param name="owned">보유 재화량. balanceKnown=false 면 무시된다.</param>
    /// <param name="balanceKnown">보유량을 알 수 있는 재화인지(인앱결제(cash) 등은 false).</param>
    /// <param name="initialQty">시작 수량(예: 티켓 부족분). 상한 내로 클램프된다.</param>
    /// <param name="clampToAffordable">true 면 수량 상한을 보유 재화로 살 수 있는 최대치로 제한한다(티켓 구매 등).</param>
    /// <returns>확인 시 선택 수량(>=1), 취소/외부 닫힘 시 0.</returns>
    public async UniTask<int> ShowAsync(BattleHudContext hud, ShopOfferDto offer, Sprite currencyIcon, int owned, bool balanceKnown,
                                        int initialQty = 1, bool clampToAffordable = false) {
        _hud = hud;
        _offer = offer;
        _owned = owned;
        _balanceKnown = balanceKnown;

        if (titleText != null && string.IsNullOrEmpty(titleText.text)) titleText.text = "구매 확인";

        if (itemNameText != null) {
            itemNameText.text = offer.DisplayName;
            if (!string.IsNullOrEmpty(offer.NameColorHex) && ColorUtility.TryParseHtmlString(offer.NameColorHex, out var col))
                itemNameText.color = col;
        }
        SetIcon(priceCurrencyIcon, currencyIcon);
        SetIcon(ownedCurrencyIcon, currencyIcon);
        if (ownedCurrencyIcon != null) ownedCurrencyIcon.gameObject.SetActive(balanceKnown && ownedCurrencyIcon.sprite != null);

        // 수량 상한: offer.MaxQty. 예산 클램프 시 보유 재화로 살 수 있는 최대치로 더 제한.
        int hardMax = offer.AllowQty ? Mathf.Max(1, offer.MaxQty) : 1;
        _effectiveMax = hardMax;
        if (clampToAffordable && balanceKnown && offer.Price > 0)
            _effectiveMax = Mathf.Clamp(owned / offer.Price, 1, hardMax);

        // 수량 선택 UI 구성.
        bool allowQty = offer.AllowQty && _effectiveMax > 1;
        if (quantityGroup != null) quantityGroup.SetActive(allowQty);
        _qty = Mathf.Clamp(initialQty, 1, _effectiveMax);
        if (allowQty && quantitySlider != null) {
            quantitySlider.wholeNumbers = true;
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = _effectiveMax;
            quantitySlider.SetValueWithoutNotify(_qty);
        }

        BuildRewardSlots();
        Refresh();

        _result = new UniTaskCompletionSource<int>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void ChangeQty(int delta) => SetQty(_qty + delta);

    private void SetQty(int value) {
        _qty = Mathf.Clamp(value, 1, Mathf.Max(1, _effectiveMax));
        if (quantitySlider != null) quantitySlider.SetValueWithoutNotify(_qty);
        Refresh();
    }

    private void Refresh() {
        if (_offer == null) return;
        int total = _offer.Price * _qty;

        if (quantityValueText != null) quantityValueText.text = _qty.ToString();
        if (priceText != null) priceText.text = total.ToString("N0");

        bool insufficient = _balanceKnown && _owned < total;
        if (ownedText != null) {
            ownedText.gameObject.SetActive(_balanceKnown);
            if (_balanceKnown) {
                ownedText.text = _owned.ToString("N0");
                ownedText.color = insufficient ? insufficientColor : _ownedDefaultColor;
            }
        }
        if (insufficientHint != null) insufficientHint.SetActive(insufficient);
        if (okButton != null) okButton.interactable = !insufficient;

        RefreshRewardSlots();
    }

    // 구매 시 받을 구성품 슬롯을 상품에 맞게 생성한다(수량 배수는 RefreshRewardSlots 가 매번 반영).
    // 구성품은 서버 카탈로그가 내려준 값(ShopOfferDto.Grants)이라 상품이 늘어도 이 팝업은 수정할 필요가 없다.
    private void BuildRewardSlots() {
        foreach (var s in _rewardSlots) if (s != null) Destroy(s.gameObject);
        _rewardSlots.Clear();

        // 가챠는 결과가 확률로 정해져 미리 보여줄 구성품이 없다(확률 보기로 안내).
        // 상점 카드/상세와 같은 출처(미러 우선, 없으면 서버 지급 목록)를 쓴다.
        var grants = ShopPackageRewards.DisplayGrants(_offer);
        bool hasGrants = grants.Count > 0;
        if (rewardGroup != null) rewardGroup.SetActive(hasGrants);
        if (!hasGrants || rewardRoot == null || rewardSlotPrefab == null) return;

        for (int i = 0; i < grants.Count; i++)
            _rewardSlots.Add(Instantiate(rewardSlotPrefab, rewardRoot));
    }

    // 선택 수량 배수를 적용해 슬롯 표시를 갱신한다(수량이 바뀌어도 슬롯을 다시 만들지 않고 값만 다시 바인딩).
    private void RefreshRewardSlots() {
        var grants = ShopPackageRewards.DisplayGrants(_offer);
        for (int i = 0; i < _rewardSlots.Count && i < grants.Count; i++) {
            if (_rewardSlots[i] == null) continue;
            _rewardSlots[i].Bind(RewardVisualResolver.Resolve(_hud, grants[i].Multiplied(_qty)));
        }
    }

    private static void SetIcon(Image image, Sprite sprite) {
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private void Resolve(int qty) {
        _result?.TrySetResult(qty);
        CloseAsync().Forget();
    }

    // OK/취소 외의 경로(배경 탭, 외부 CloseAllPopups 등)로 닫혀도 반드시 결과를 확정해,
    // await 중인 호출부(상점 구매 흐름)가 영원히 멈춰 _busy 가 고착되는 것을 막는다(취소=0 으로 간주).
    protected override void OnClosed() {
        _result?.TrySetResult(0);
    }
}
