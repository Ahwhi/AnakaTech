using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 공지/이벤트 목록 팝업(UIView). 상태에 따라 로딩/목록/빈 목록/오류 중 하나를 보여준다.
/// 데이터 조회는 프레젠터가 담당하고, 이 뷰는 받은 목록을 그리고 글 선택을 콜백으로 알리는 표시 책임만 진다.
///
/// 표시 흐름:
///   ShowAsync(title)  -> 로딩 상태로 페이드 인(불러오고 있습니다 + 스피너)
///   SetItems(list)    -> 목록 채우기(비어 있으면 빈 목록 안내)
///   ShowError()       -> 오류 안내
/// </summary>
public sealed class NoticePopup : UIView {
    [Header("Header")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Button closeButton;

    [Header("List")]
    [Tooltip("ScrollRect 의 Content. 목록 아이템이 이 아래에 생성된다.")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private NoticeItemView itemPrefab;

    [Header("States (상호 배타적으로 켜진다)")]
    [Tooltip("불러오는 중 표시 그룹(문구 + 스피너).")]
    [SerializeField] private GameObject loadingGroup;
    [Tooltip("로딩 그룹 안의 회전 스피너(선택).")]
    [SerializeField] private RectTransform loadingSpinner;
    [SerializeField] private float spinSpeed = 220f;
    [Tooltip("게시글이 하나도 없을 때 안내(선택).")]
    [SerializeField] private GameObject emptyLabel;
    [Tooltip("불러오기 실패 시 안내(선택).")]
    [SerializeField] private GameObject errorLabel;

    private Action<NoticeData> _onItemSelected;
    private readonly List<NoticeItemView> _spawned = new();
    private bool _loading;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
    }

    private void Update() {
        // 타임스케일 영향 없이 로딩 중에만 스피너를 회전시킨다.
        if (_loading && loadingSpinner != null)
            loadingSpinner.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
    }

    /// <summary>프레젠터가 목록 글 선택 콜백을 주입한다(상세 팝업 열기 등).</summary>
    public void Initialize(Action<NoticeData> onItemSelected) => _onItemSelected = onItemSelected;

    /// <summary>제목을 세팅하고 로딩 상태로 페이드 인한다.</summary>
    public async UniTask ShowAsync(string title) {
        if (headerText != null) headerText.text = title;
        SetState(loading: true);
        await base.ShowAsync();
    }

    /// <summary>조회된 목록으로 화면을 채운다. 비어 있으면 빈 목록 안내를 표시한다.</summary>
    public void SetItems(IReadOnlyList<NoticeData> items) {
        ClearItems();

        if (items == null || items.Count == 0) {
            SetState(empty: true);
            return;
        }

        foreach (var data in items) {
            var item = Instantiate(itemPrefab, contentRoot);
            item.Bind(data, _onItemSelected);
            _spawned.Add(item);
        }
        SetState(); // 목록만 보이도록 상태 라벨을 모두 끈다.
    }

    /// <summary>조회 실패 시 오류 안내를 표시한다.</summary>
    public void ShowError() {
        ClearItems();
        SetState(error: true);
    }

    protected override void OnClosed() {
        ClearItems();
    }

    private void SetState(bool loading = false, bool empty = false, bool error = false) {
        _loading = loading;
        if (loadingGroup != null) loadingGroup.SetActive(loading);
        if (emptyLabel != null) emptyLabel.SetActive(empty);
        if (errorLabel != null) errorLabel.SetActive(error);
    }

    private void ClearItems() {
        foreach (var item in _spawned)
            if (item != null) Destroy(item.gameObject);
        _spawned.Clear();
    }
}
