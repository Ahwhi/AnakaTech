using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using static Enums;

/// <summary>
/// 단일 재화 표시 위젯(텍스트 + 아이콘 + 선택적 +버튼). 인스펙터에서 CurrencyType 만 지정하면
/// PlayerStateStore 의 해당 값을 구독해 표시하고, 아이콘은 CurrencyCatalog 에서 타입으로 자동 조회한다.
/// +버튼에 팝업 프리팹(AssetReference)을 할당하면 클릭 시 그 팝업을 연다(IHudPopup 이면 ctx 로 초기화).
///
/// 어느 씬/팝업이든 이 위젯을 배치하고 MainHudContext 로 Initialize 하면 재사용된다. 여러 개를
/// CurrencyPanel 아래 두면 CurrencyPanel 이 일괄 초기화한다. 순수 뷰라 스토어만 구독한다.
/// </summary>
public sealed class CurrencyView : MonoBehaviour {
    [Tooltip("표시할 재화 종류. 이 값만 정하면 값/아이콘/+버튼 동작이 결정된다.")]
    [SerializeField] private CurrencyType type;
    [SerializeField] private TMP_Text valueText;
    [Tooltip("선택: 재화 아이콘. 비우거나 카탈로그에 아이콘이 없으면 갱신하지 않는다.")]
    [SerializeField] private Image iconImage;
    [Tooltip("선택: +버튼. 비우면 구매/충전 진입 없음.")]
    [SerializeField] private Button plusButton;
    [Tooltip("선택: +버튼으로 열 팝업 프리팹. 비우면 +버튼을 숨긴다. IHudPopup 구현 팝업이면 자동 초기화된다.")]
    [SerializeField] private AssetReferenceGameObject plusPopup;
    [Tooltip("선택: 아이콘 클릭용 버튼. 누르면 이 재화의 설명 팝업이 열린다. " +
             "보통 아이콘 Image 에 Button 을 붙여 연결한다. 비우면 설명 팝업 진입이 없다.")]
    [SerializeField] private Button infoButton;

    // 메인 HUD 일 때만 채워진다(+버튼/설명 팝업 전용). 값 표시는 _battleCtx 로 한다.
    private MainHudContext _ctx;
    private BattleHudContext _battleCtx;
    private readonly CompositeDisposable _disposables = new();

    public CurrencyType Type => type;

    /// <summary>
    /// 값/아이콘은 어느 화면에서든 같다. +버튼(상점)과 설명 팝업은 메인 HUD 의 팝업들이라 메인 화면(MainHudContext)에서만 연결하고,
    /// 전투 씬(전선 등)에서는 +버튼을 숨기고 아이콘 클릭을 끈다.
    /// </summary>
    public void Initialize(BattleHudContext ctx) {
        _ctx = ctx as MainHudContext;
        _battleCtx = ctx;
        _disposables.Clear();

        // 아이콘: 카탈로그에서 타입으로 조회(있을 때만 갱신, 없으면 프리팹 아이콘 유지).
        if (iconImage != null && ctx.Currencies != null) {
            var icon = ctx.Currencies.Icon(type);
            if (icon != null) { iconImage.sprite = icon; iconImage.enabled = true; }
        }

        // 값: 스토어의 해당 재화를 구독.
        var prop = ctx.Store.Currency(type);
        if (prop != null) prop.Subscribe(UpdateValue).AddTo(_disposables);

        // +버튼: 팝업이 할당된 경우만 노출/연결.
        if (plusButton != null) {
            bool hasPopup = _ctx != null && plusPopup != null && plusPopup.RuntimeKeyIsValid();
            plusButton.gameObject.SetActive(hasPopup);
            if (hasPopup) {
                plusButton.onClick.RemoveListener(OnPlusClicked);
                plusButton.onClick.AddListener(OnPlusClicked);
            }
        }

        // 아이콘 클릭: 이 재화의 설명 팝업. 카탈로그에 설명이 없는 재화는 띄울 내용이 없으므로 연결하지 않는다.
        if (infoButton != null) {
            bool hasDescription = _ctx != null && ctx.Currencies != null && !string.IsNullOrEmpty(ctx.Currencies.Description(type));
            infoButton.interactable = hasDescription;
            infoButton.onClick.RemoveListener(OnInfoClicked);
            if (hasDescription) infoButton.onClick.AddListener(OnInfoClicked);
        }
    }

    private void OnInfoClicked() {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenInfoPopupAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid OpenInfoPopupAsync(CancellationToken ct) {
        var popup = await _ctx.Ui.ShowPopupAsync<CurrencyInfoPopup>(_ctx.Popups.CurrencyInfoPopup, ct);
        popup.SetCurrency(type);   // Initialize 전에 지정해야 표시에 반영된다.
        popup.Initialize(_ctx);
        await popup.ShowAsync();
    }

    private void UpdateValue(int value) {
        if (valueText == null) return;
        // 상한이 있는 재화(기력/의뢰해결권)는 "현재/최대" 형식으로 보여준다(최대치는 스토어 권위).
        // 그 외는 천 단위 구분만 적용한다.
        valueText.text = type switch {
            CurrencyType.Stamina => $"{value} / {_battleCtx.Store.StaminaMax}",
            CurrencyType.CommissionTicket => $"{value} / {_battleCtx.Store.CommissionTicketMax}",
            _ => value.ToString("N0")
        };
    }

    private void OnPlusClicked() {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenPlusPopupAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid OpenPlusPopupAsync(CancellationToken ct) {
        var view = await _ctx.Ui.ShowPopupAsync<UIView>(plusPopup, ct);
        // 상점 팝업이면 이 재화에 맞는 페이지(대/중분류)로 바로 진입시킨다(Initialize 전에 지정해야 반영됨).
        if (view is ShopPopup shop && TryShopEntry(type, out var major, out var sub))
            shop.SetEntry(major, sub);
        if (view is IHudPopup hudPopup) hudPopup.Initialize(_ctx);
        await view.ShowAsync();
    }

    // 재화 타입 -> 상점 진입 페이지(대분류/중분류). 상점에 해당 페이지가 없는 재화면 false(기본 페이지로 진입).
    private static bool TryShopEntry(CurrencyType type, out string major, out string sub) {
        switch (type) {
            case CurrencyType.Gold:          major = "currency";   sub = "gold";    return true;
            case CurrencyType.Diamond:       major = "currency";   sub = "diamond"; return true;
            case CurrencyType.Stamina:       major = "consumable"; sub = "stamina"; return true;
            case CurrencyType.NormalTicket:
            case CurrencyType.SpecialTicket: major = "consumable"; sub = "ticket";  return true;
            default:                         major = null;         sub = null;      return false;
        }
    }

    private void OnDestroy() => _disposables.Dispose();
}
