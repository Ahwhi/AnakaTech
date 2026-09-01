using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 한정 판매 안내 팝업. 지금 이 계정에 열려 있는 상품들을 보여 주고, 구매하러 상점으로 보낸다.
///
/// 여기서 결제하지 않는 이유: 상품을 파는 자리를 상점 하나로 유지해야 결제 검증/구매 한도/영수증 처리가
/// 한 경로에만 있게 된다. 이 팝업은 "무엇이 열렸고 언제 끝나는지"를 알리는 안내창이다.
///
/// 상품 정보(이름/가격/구성품)는 상점 카탈로그에서 가져온다. 한정 판매 응답은 "무엇이 언제까지"만 담고 있어
/// 두 곳에 같은 상품 정보를 두지 않는다(한쪽만 바뀌어 화면과 결제가 어긋나는 일이 없다).
///
/// 메인 화면 버튼(FlashSaleButton)이 열지만, 새로 열렸을 때 자동으로 띄우는 것도 같은 팝업이다
/// (MainSceneManager 가 서버 알림을 받아 연다). 어느 컨텐츠가 발동시켰든 화면은 하나다.
/// </summary>
public sealed class FlashSalePopup : UIView, IHudPopup {
    [Header("List")]
    [Tooltip("상품 줄이 생성될 부모.")]
    [SerializeField] private Transform content;
    [Tooltip("상품 줄 프리팹(FlashSaleRowView).")]
    [SerializeField] private FlashSaleRowView rowPrefab;
    [Tooltip("열려 있는 상품이 없을 때 켜는 안내 묶음. 선택.")]
    [SerializeField] private GameObject emptyGroup;

    [Header("Header")]
    [Tooltip("안내 문구(예: 지금만 살 수 있는 한정 패키지). 선택.")]
    [SerializeField] private TMP_Text titleText;

    [Header("Buttons")]
    [Tooltip("상점으로 이동하는 공용 버튼(줄마다 버튼을 두지 않는 구성일 때). 선택.")]
    [SerializeField] private Button moveToShopButton;
    [SerializeField] private Button closeButton;

    // 한정 판매 상품이 놓이는 상점 위치. 서버 카탈로그의 Major/Sub 와 반드시 같아야 한다.
    private const string ShopMajor = "limited";
    private const string ShopSub = "account";

    private MainHudContext _hud;
    private CancellationToken _ct;
    private readonly List<FlashSaleRowView> _rows = new();
    private bool _wired;

    public void Initialize(MainHudContext ctx) {
        _hud = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (!_wired) {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            if (moveToShopButton != null)
                moveToShopButton.onClick.AddListener(MoveToShop);
            _wired = true;
        }

        BuildAsync().Forget();
    }

    private async UniTaskVoid BuildAsync() {
        ClearRows();

        var active = _hud.FlashSale.Active.Value;
        if (active == null || active.Count == 0) { ApplyEmpty(true); return; }

        // 상품 정보는 상점 카탈로그가 권위다. 한정 판매 목록은 id 와 기간만 갖고 있다.
        ShopCatalogDto catalog;
        try {
            catalog = await _hud.Shop.RequestShopAsync(_ct);
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("상점 정보를 불러오지 못했습니다.");
            return;
        }

        var byId = new Dictionary<string, ShopOfferDto>(catalog.Offers.Count);
        foreach (var o in catalog.Offers) byId[o.OfferId] = o;

        foreach (var sale in active) {
            // 카탈로그에 없으면 이미 끝났거나 내려간 상품이다(안내할 것이 없다).
            if (!byId.TryGetValue(sale.OfferId, out var offer)) continue;

            // 이미 산 상품은 아예 싣지 않는다. 이 팝업은 "지금 살 수 있는 한정 상품이 있다"는 안내가 목적이라,
            // 구매 완료 줄이 남아 있으면 살 것이 없는데도 열려 알림처럼 보인다.
            // 상점에서는 구매 완료 표시로 남겨 둔다(거기서는 목록을 훑는 것이 목적이라 상태가 정보가 된다).
            if (offer.SoldOut) continue;

            var row = Instantiate(rowPrefab, content);
            row.Bind(_hud, offer, sale, MoveToShop);
            _rows.Add(row);
        }
        ApplyEmpty(_rows.Count == 0);
    }

    private void ApplyEmpty(bool empty) {
        if (emptyGroup != null) emptyGroup.SetActive(empty);
        if (moveToShopButton != null) moveToShopButton.gameObject.SetActive(!empty);
    }

    // 상점의 한정 > 계정 탭으로 보낸다. 한정 판매 상품이 거기 놓이므로 유저가 목록을 뒤질 필요가 없다.
    private void MoveToShop() {
        _hud.Sound.PlaySfx(SfxKey.ButtonClick);
        MoveToShopAsync().Forget();
    }

    private async UniTaskVoid MoveToShopAsync() {
        var reference = _hud.Popups.ShopPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;

        // 상점을 먼저 연 뒤에 이 안내창을 닫는다.
        //
        // 반대로 하면 안 된다. 이 팝업을 닫는 순간 인스턴스가 해제되면서 여기 쓰는 취소 토큰
        // (GetCancellationTokenOnDestroy)이 끊기고, 상점을 여는 작업이 조용히 취소된다.
        // 화면에는 "안내창만 닫히고 아무 일도 안 일어나는" 것으로 보인다.
        try {
            var shop = await _hud.Ui.ShowPopupAsync<ShopPopup>(reference, _ct);
            shop.SetEntry(ShopMajor, ShopSub);
            shop.Initialize(_hud);
            await shop.ShowAsync();
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _hud.Ui.ShowToast("상점을 열지 못했습니다.");
            return;
        }

        // 상점이 다 열린 뒤에 닫는다. 겹쳐 두면 구매 후 뒤로 나올 때 지나간 안내가 다시 보인다.
        CloseAsync().Forget();
    }

    private void ClearRows() {
        foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
        _rows.Clear();
    }

    protected override void OnClosed() => ClearRows();
}
