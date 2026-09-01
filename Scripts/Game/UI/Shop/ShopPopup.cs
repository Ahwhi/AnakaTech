using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 상점 팝업(PLANNING 8). 위치 고정된 대분류 버튼들 + 대분류별 페이지 프리팹을 콘텐츠 Rect 에 인스턴스화한다.
/// 입장 시 서버 카탈로그(로딩 대기)를 받아 재화를 갱신하고 제일 위 대분류를 선택한다. 재화 패널도 공유 표시한다.
/// </summary>
public sealed class ShopPopup : UIView, IHudPopup {
    [Serializable]
    public struct MajorEntry {
        public string major;
        public Button button;
        [Tooltip("선택. 버튼 표시명 텍스트. 서버 카탈로그의 대분류 이름으로 설정된다(미할당 시 프리팹 기본 텍스트 유지).")]
        public TMP_Text label;
        public GameObject selectedIndicator;
        public ShopOfferPage pagePrefab;
    }

    [SerializeField] private MajorEntry[] majors;
    [Tooltip("선택한 대분류 페이지 프리팹이 인스턴스화될 부모 Rect.")]
    [SerializeField] private Transform contentRoot;
    [Tooltip("상점에서도 재화 패널을 표시(PLANNING 2-2). 선택.")]
    [SerializeField] private CurrencyPanel currencyPanel;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private MainHudContext _hud;
    private CancellationToken _ct;
    private ShopCatalogDto _catalog;
    private ShopOfferPage _currentPage;
    private string _entryMajor;   // 진입 시 바로 열 대분류(없으면 첫 대분류).
    private string _entrySub;     // 진입 시 바로 선택할 중분류(없으면 첫 중분류).

    /// <summary>Initialize 전에 호출하면 해당 대분류/중분류 페이지로 바로 진입한다(재화 부족 유도 등).</summary>
    public void SetEntry(string major, string sub) { _entryMajor = major; _entrySub = sub; }

    public void Initialize(MainHudContext hud) {
        _hud = hud;
        _ct = this.GetCancellationTokenOnDestroy();

        for (int i = 0; i < majors.Length; i++) {
            int index = i;
            if (majors[i].button != null)
                majors[i].button.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); SelectMajor(index); });
        }

        if (currencyPanel != null) currencyPanel.Initialize(hud);
        if (closeButton != null) closeButton.onClick.AddListener(() => { _hud.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });

        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        using (_hud.Ui.ShowLoading()) {
            try {
                _catalog = await _hud.Shop.RequestShopAsync(_ct);
                _hud.Store.Gold.Value = _catalog.Gold;
                _hud.Store.Diamond.Value = _catalog.Diamond;
                _hud.Store.ApplyShopCurrencies(_catalog.Currencies);

                // 상점 진입 = 현재 오퍼 전체를 "봤음"으로 기록(상점 New 배지 해제).
                if (_hud.Badges != null) {
                    var ids = new List<string>(_catalog.Offers.Count);
                    foreach (var o in _catalog.Offers) ids.Add(o.OfferId);
                    _hud.Badges.MarkShopSeen(ids);
                }
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
                _hud.Ui.ShowToast("상점 정보를 불러오지 못했습니다.");
                return;
            }
        }

        ApplyMajorNames();
        if (!string.IsNullOrEmpty(_entryMajor)) NavigateTo(_entryMajor, _entrySub);
        else if (majors.Length > 0) SelectMajor(0);
    }

    // 대분류 키로 페이지를 열고(없으면 첫 대분류) 지정 중분류를 선택한다. 페이지가 부모에게 이동을 요청할 때도 사용.
    private void NavigateTo(string major, string sub) {
        int index = 0;
        for (int i = 0; i < majors.Length; i++)
            if (majors[i].major == major) { index = i; break; }
        SelectMajor(index, sub);
    }

    // 대분류 버튼 라벨을 서버 카탈로그의 이름으로 설정한다(라벨 미할당 버튼은 프리팹 기본 텍스트 유지).
    private void ApplyMajorNames() {
        if (_catalog == null) return;
        for (int i = 0; i < majors.Length; i++) {
            if (majors[i].label == null) continue;
            if (_catalog.CategoryNames.TryGetValue(majors[i].major, out var name) && !string.IsNullOrEmpty(name))
                majors[i].label.text = name;
        }
    }

    private void SelectMajor(int index, string sub = null) {
        if (_catalog == null || index < 0 || index >= majors.Length) return;

        for (int i = 0; i < majors.Length; i++)
            if (majors[i].selectedIndicator != null) majors[i].selectedIndicator.SetActive(i == index);

        if (_currentPage != null) Destroy(_currentPage.gameObject);

        var entry = majors[index];
        if (entry.pagePrefab == null || contentRoot == null) return;

        _currentPage = Instantiate(entry.pagePrefab, contentRoot);

        // 해당 대분류의 오퍼만 넘긴다. "전체"는 대분류 내 첫 서브탭으로 페이지가 자체 구성한다(hasAllTab).
        var list = new List<ShopOfferDto>();
        foreach (var o in _catalog.Offers)
            if (o.Major == entry.major) list.Add(o);

        bool hasAllTab = _catalog.CategoryHasAllTab.TryGetValue(entry.major, out var h) && h;
        // 구매로 카탈로그가 갱신되면 여기(부모)도 최신본으로 교체해, 다른 카테고리로 이동해 페이지를 새로 만들 때 SoldOut/Locked 상태가 유지되게 한다.
        _currentPage.Initialize(_hud, list, hasAllTab, _catalog, sub, NavigateTo, updated => _catalog = updated);
    }
}
