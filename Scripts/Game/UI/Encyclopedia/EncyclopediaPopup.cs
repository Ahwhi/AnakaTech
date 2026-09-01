using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 도감 팝업(PLANNING 9). 게임의 모든 캐릭터를 그리드에 표시하고, 미보유는 잠금 처리한다.
/// 이름 검색 + 정렬 드롭뷰(무기타입/이름/등급) + 오름/내림 드롭뷰로 재정렬한다. 보유 캐릭터 클릭 시 상세 팝업을 연다.
/// 서버 통신 없이 CharacterDatabase(전체) + PlayerStateStore(보유 여부)만으로 동작한다.
/// </summary>
public sealed class EncyclopediaPopup : UIView, IHudPopup {
    private enum SortMode { Grade = 0, Weapon = 1, Name = 2 }

    [Header("Grid")]
    [SerializeField] private Transform gridContent;
    [SerializeField] private EncyclopediaCard cardPrefab;

    [Header("Filter / Sort")]
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button searchButton;
    [Tooltip("0=등급순, 1=무기타입, 2=이름순.")]
    [SerializeField] private TMP_Dropdown sortDropdown;
    [Tooltip("0=오름차순, 1=내림차순.")]
    [SerializeField] private TMP_Dropdown orderDropdown;

    [Header("Reward")]
    [Tooltip("도감 보상(수집/별레벨) 팝업을 여는 버튼.")]
    [SerializeField] private Button rewardButton;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    [Header("Loading")]
    [Tooltip("그리드를 채우는 동안 표시할 로딩 그룹(문구/스피너). 비우면 표시 없이 채운다.")]
    [SerializeField] private GameObject loadingGroup;

    // 한 프레임에 생성할 카드 수(나머지는 다음 프레임으로 분산해 렉을 막는다).
    private const int BuildPerFrame = 8;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private CancellationTokenSource _buildCts;   // 재구성(검색/정렬) 겹칠 때 이전 생성 취소용.
    private readonly List<EncyclopediaCard> _cards = new();
    private bool _building;                       // 카드 생성이 프레임 분산으로 진행 중인지(집합 확정 전).

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (searchButton != null) searchButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); Rebuild(); });
        if (searchInput != null) searchInput.onSubmit.AddListener(_ => Rebuild());
        // 정렬 기준/방향은 표시 집합이 그대로라 카드 재생성 없이 순서만 다시 배치한다(로딩 깜빡임 없음).
        if (sortDropdown != null) sortDropdown.onValueChanged.AddListener(_ => Resort());
        if (orderDropdown != null) orderDropdown.onValueChanged.AddListener(_ => Resort());
        if (rewardButton != null) rewardButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenRewardAsync().Forget(); });
        if (closeButton != null) closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });

        Rebuild();
    }

    private void Rebuild() => RebuildAsync().Forget();

    private async UniTaskVoid RebuildAsync() {
        // 재구성이 겹치면 이전 생성을 취소한다(취소된 쪽은 아래에서 새로 만든 카드를 건드리지 않는다).
        _buildCts?.Cancel();
        _buildCts?.Dispose();
        _buildCts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var ct = _buildCts.Token;

        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();

        _building = true;
        if (loadingGroup != null) loadingGroup.SetActive(true);

        string query = searchInput != null ? searchInput.text.Trim() : "";

        var list = new List<CharacterData>();
        var all = _ctx.CharDb.All;
        for (int i = 0; i < all.Count; i++) {
            var d = all[i];
            if (d == null) continue;
            if (query.Length > 0 && d.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
            list.Add(d);
        }
        list.Sort(Compare);

        try {
            await AsyncBuild.SpawnChunkedAsync(list, BuildPerFrame, d => {
                var card = Instantiate(cardPrefab, gridContent);
                bool owned = _ctx.Store.Characters.ContainsKey(d.Id);
                card.Bind(d, owned, OnCardClicked);
                _cards.Add(card);
            }, ct);
        } catch (OperationCanceledException) {
            return;   // 뒤이은 재구성이 로딩 표시/정리와 _building 해제를 이어받는다.
        }

        _building = false;
        if (loadingGroup != null) loadingGroup.SetActive(false);
    }

    // 정렬 기준/방향만 바뀐 경우: 표시 집합은 동일하므로 카드를 파괴/재생성하지 않고 순서만 다시 배치한다.
    // 로딩 그룹을 켜지 않아 화면 깜빡임이 없다. 생성이 진행 중이면(집합 확정 전) 전체 재구성으로 정합성을 맞춘다
    // (이때 로딩 그룹은 이미 켜져 있어 새 깜빡임은 없다).
    private void Resort() {
        if (_building) { Rebuild(); return; }
        if (_cards.Count <= 1) return;
        _cards.Sort((a, b) => Compare(a.Data, b.Data));
        for (int i = 0; i < _cards.Count; i++)
            if (_cards[i] != null) _cards[i].transform.SetSiblingIndex(i);
    }

    private int Compare(CharacterData a, CharacterData b) {
        var mode = sortDropdown != null ? (SortMode)sortDropdown.value : SortMode.Grade;
        int cmp = mode switch {
            SortMode.Name => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal),
            SortMode.Grade => a.Grade.CompareTo(b.Grade),
            _ => a.WeaponType.CompareTo(b.WeaponType)
        };
        if (cmp == 0) cmp = string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);

        bool ascending = orderDropdown == null || orderDropdown.value == 0;
        return ascending ? cmp : -cmp;
    }

    private void OnCardClicked(EncyclopediaCard card) {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        OpenDetailAsync(card.Data).Forget();
    }

    private async UniTaskVoid OpenDetailAsync(CharacterData data) {
        var popup = await _ctx.Ui.ShowPopupAsync<CharacterDetailPopup>(_ctx.Popups.CharacterDetailPopup, _ct);
        popup.Initialize(_ctx, data);
        await popup.ShowAsync();
    }

    private async UniTaskVoid OpenRewardAsync() {
        var reference = _ctx.Popups.EncyclopediaRewardPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<EncyclopediaRewardPopup>(reference, _ct);
            popup.Initialize(_ctx);
            await popup.ShowAsync();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    protected override void OnClosed() {
        _buildCts?.Cancel();
        _buildCts?.Dispose();
        _buildCts = null;
        foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
        _cards.Clear();
    }
}
