using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이벤트 전용 팝업(UIView). 공지와 달리 한 화면에 목록(아이템=탭, 상태 배지)과 상세를 함께 보여주는
/// 마스터-디테일 구조다. 열면 맨 위(최신) 이벤트를 자동 선택하고, 아이템을 누르면 같은 팝업 안에서
/// 상세 영역만 교체한다. 재오픈 시에는 직전에 선택했던 이벤트를 우선 선택한다.
///
/// 배지 계산과 선택 시 부수처리(읽음 기록 등)는 프레젠터가 콜백으로 주입한다. 이 뷰는 표시 책임만 진다.
/// </summary>
public sealed class EventPopup : UIView {
    [Header("Header")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Button closeButton;

    [Header("List (Master)")]
    [Tooltip("아이템(탭)이 생성될 부모. 보통 세로 Layout Group.")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private EventItemView itemPrefab;

    [Header("Detail")]
    [Tooltip("선택된 이벤트의 상세를 보여주는 영역(팝업 안에 상주).")]
    [SerializeField] private EventDetailView detailView;

    [Header("States (상호 배타적으로 켜진다)")]
    [SerializeField] private GameObject loadingGroup;
    [SerializeField] private RectTransform loadingSpinner;
    [SerializeField] private float spinSpeed = 220f;
    [Tooltip("이벤트가 하나도 없을 때 안내.")]
    [SerializeField] private GameObject emptyLabel;
    [SerializeField] private GameObject errorLabel;

    private Func<NoticeData, EventBadge> _badgeProvider;
    private Action<NoticeData> _onItemSelected;
    private readonly List<EventItemView> _spawned = new();
    private readonly Dictionary<long, EventItemView> _byId = new();
    private IReadOnlyList<NoticeData> _currentData;
    private EventItemView _selected;
    private bool _loading;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    private void Update() {
        if (_loading && loadingSpinner != null)
            loadingSpinner.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
    }

    /// <summary>프레젠터가 배지 계산 함수와 선택 콜백(읽음 처리 등), 페이지 로드용 자산 서비스와 DI 컨테이너를 주입한다.</summary>
    public void Initialize(Func<NoticeData, EventBadge> badgeProvider, Action<NoticeData> onItemSelected,
                           IAssetService asset, VContainer.IObjectResolver resolver) {
        _badgeProvider = badgeProvider;
        _onItemSelected = onItemSelected;
        if (detailView != null) detailView.Initialize(asset, resolver);
    }

    /// <summary>제목을 세팅하고 로딩 상태로 페이드 인한다.</summary>
    public async UniTask ShowAsync(string title) {
        if (headerText != null) headerText.text = title;
        SetState(loading: true);
        await base.ShowAsync();
    }

    /// <summary>
    /// 조회된 이벤트 목록으로 화면을 채운다. preferredId 가 목록에 있으면 그 항목을, 없으면 맨 위(최신)를 선택한다.
    /// </summary>
    public void SetItems(IReadOnlyList<NoticeData> items, long preferredId = -1) {
        ClearItems();

        if (items == null || items.Count == 0) {
            SetState(empty: true);
            return;
        }

        _currentData = items;

        NoticeData target = items[0];
        foreach (var data in items) {
            var item = Instantiate(itemPrefab, contentRoot);
            item.Bind(data, ResolveBadge(data), Select);
            _spawned.Add(item);
            _byId[data.id] = item;
            if (data.id == preferredId) target = data;
        }

        SetState();          // 목록/상세 표시 상태로
        Select(target);      // 기본 선택 -> 상세 노출
    }

    /// <summary>특정 이벤트의 배지를 다시 계산해 갱신한다(읽음 처리 직후 등).</summary>
    public void RefreshBadge(NoticeData data) {
        if (_byId.TryGetValue(data.id, out var item) && item != null)
            item.SetBadge(ResolveBadge(data));
    }

    /// <summary>현재 표시 중인 모든 항목의 배지를 다시 계산한다(서버 상태 수신 후 등).</summary>
    public void RefreshAllBadges() {
        if (_currentData == null) return;
        foreach (var data in _currentData)
            RefreshBadge(data);
    }

    public void ShowError() {
        ClearItems();
        SetState(error: true);
    }

    protected override void OnClosed() {
        if (detailView != null) detailView.Clear();
        ClearItems();
    }

    // 아이템(탭) 선택: 강조를 옮기고, 상세를 교체하고, 프레젠터에 알린다(읽음 처리/선택 기억).
    private void Select(NoticeData data) {
        if (_byId.TryGetValue(data.id, out var item)) {
            if (_selected != null) _selected.SetSelected(false);
            _selected = item;
            item.SetSelected(true);
        }

        if (detailView != null) detailView.Show(data);
        _onItemSelected?.Invoke(data);
    }

    private EventBadge ResolveBadge(NoticeData data)
        => _badgeProvider != null ? _badgeProvider(data) : EventBadge.None;

    private void SetState(bool loading = false, bool empty = false, bool error = false) {
        _loading = loading;
        if (loadingGroup != null) loadingGroup.SetActive(loading);
        if (emptyLabel != null) emptyLabel.SetActive(empty);
        if (errorLabel != null) errorLabel.SetActive(error);

        // 상세는 실제 목록이 있을 때(어느 상태도 아닐 때)만 보여준다.
        bool showDetail = !loading && !empty && !error;
        if (detailView != null) detailView.gameObject.SetActive(showDetail);
    }

    private void ClearItems() {
        foreach (var item in _spawned)
            if (item != null) Destroy(item.gameObject);
        _spawned.Clear();
        _byId.Clear();
        _currentData = null;
        _selected = null;
    }
}
