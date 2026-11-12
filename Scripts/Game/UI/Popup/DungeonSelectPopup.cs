using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 선택 팝업(PLANNING 2-6). 그룹 드롭다운으로 던전 그룹을 고르고, 그 그룹의 던전 목록을 항목으로 생성한다.
/// 열릴 때 현재 플레이 중인 던전의 그룹이 자동 선택된다. 항목 선택 시 콜백 후 닫힌다.
///
/// 예상 보상은 서버 권위라 서버 미리보기(GetDungeonRewardPreviewAsync)를 한 번에 받아 캐시하고 항목에 채운다.
/// 목록/몬스터는 클라 SO 만으로 즉시 그리므로 응답을 기다리지 않고, 보상 수치는 도착하는 대로 채워진다.
///
/// 잠금/선택 표시는 스토어를 구독해 갱신한다. 방치형이라 팝업이 열려 있는 동안에도 전투가 계속 클리어되므로,
/// 다음 던전이 해금되면 목록을 다시 만들지 않고 오버레이만 즉시 바뀐다(스크롤 위치 유지).
/// </summary>
public sealed class DungeonSelectPopup : UIView {
    [SerializeField] private Transform contentRoot;
    [SerializeField] private DungeonSelectItem itemPrefab;

    [Header("Group")]
    [Tooltip("던전 그룹 선택 드롭다운. 옵션은 DungeonDatabase 의 그룹에서 자동 생성된다. 그룹이 없으면 숨기고 전체 던전을 보여준다.")]
    [SerializeField] private TMP_Dropdown groupDropdown;

    [Header("Best")]
    [Tooltip("누르면 지금 도전할 수 있는 가장 높은 던전을 고른 것과 같다(목록에서 그 항목을 누른 것과 같은 동작). 선택.")]
    [SerializeField] private Button bestButton;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private Action<int> _onSelect;

    private readonly List<DungeonGroupData> _groups = new();
    private readonly List<DungeonSelectItem> _spawned = new();
    // dungeonIndex -> 서버가 내려준 예상 보상. 팝업이 열려 있는 동안 재사용한다(그룹 전환 시 재요청 없음).
    private readonly Dictionary<int, DungeonRewardPreview> _previews = new();
    private readonly CompositeDisposable _disposables = new();

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
        if (bestButton != null) bestButton.onClick.AddListener(SelectBest);
        // 그룹 목록을 펼치면 지금 고른 그룹이 목록 가운데 보이게 한다(장이 늘어 그룹이 많다).
        if (groupDropdown != null) groupDropdown.ScrollToSelectedOnExpand().AddTo(this);
    }

    public void Setup(MainHudContext ctx, Action<int> onSelect) {
        _ctx = ctx;
        _onSelect = onSelect;
        _previews.Clear();
        _disposables.Clear();

        if (ctx == null || ctx.DungeonDb == null || itemPrefab == null || contentRoot == null) return;

        BuildGroupOptions(ctx.Store.CurrentDungeonIndex.Value);
        Rebuild();
        LoadPreviewsAsync().Forget();

        // 해금 진행도/현재 던전이 바뀌면 오버레이만 갱신한다(전투 중 목록 재생성 방지).
        ctx.Store.HighestClearedDungeonIndex.Subscribe(_ => RefreshStates()).AddTo(_disposables);
        ctx.Store.CurrentDungeonIndex.Subscribe(_ => RefreshStates()).AddTo(_disposables);
    }

    // 드롭다운 옵션을 그룹 목록으로 채우고, 현재 플레이 중인 던전이 속한 그룹을 선택 상태로 만든다.
    private void BuildGroupOptions(int currentDungeonIndex) {
        _groups.Clear();
        _groups.AddRange(_ctx.DungeonDb.Groups);

        if (groupDropdown == null) return;

        // 그룹이 하나도 없으면(모든 던전 그룹 미할당) 드롭다운을 숨기고 전체 목록으로 동작한다.
        groupDropdown.gameObject.SetActive(_groups.Count > 0);
        if (_groups.Count == 0) return;

        var options = new List<string>(_groups.Count);
        // 그룹이 장이라 표시 순서 번호를 앞에 붙인다(예: "21. 페리의 방"). 그룹이 많아도 몇 장인지 바로 읽힌다.
        for (int i = 0; i < _groups.Count; i++) options.Add($"{i + 1}. {_groups[i].DisplayName}");

        groupDropdown.onValueChanged.RemoveAllListeners();
        groupDropdown.ClearOptions();
        groupDropdown.AddOptions(options);

        var current = _ctx.DungeonDb.GroupOf(currentDungeonIndex);
        int selected = current != null ? _groups.IndexOf(current) : -1;
        groupDropdown.SetValueWithoutNotify(selected >= 0 ? selected : 0);
        groupDropdown.RefreshShownValue();
        groupDropdown.onValueChanged.AddListener(_ => Rebuild());
    }

    private void Rebuild() {
        ClearItems();
        if (_ctx == null || itemPrefab == null || contentRoot == null) return;

        foreach (var dungeon in SelectedDungeons()) {
            if (dungeon == null) continue;

            var item = Instantiate(itemPrefab, contentRoot);
            _previews.TryGetValue(dungeon.DungeonIndex, out var preview);
            item.Bind(_ctx, dungeon, preview, Select);
            _spawned.Add(item);
        }
        RefreshStates();
        ScrollToCurrentAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // 지금 플레이 중인 던전이 목록 가운데 오도록 스크롤한다. 고른 그룹에 그 던전이 없으면 맨 위로 돌린다
    // (그룹을 바꿨는데 이전 그룹의 스크롤 위치가 남아 있으면 엉뚱한 중간부터 보인다).
    // 막 생성한 항목은 이번 프레임에 아직 크기가 정해지지 않았을 수 있어 레이아웃이 한 번 돈 뒤에 맞춘다.
    private async UniTaskVoid ScrollToCurrentAsync(CancellationToken ct) {
        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, ct);
        if (_ctx == null || contentRoot == null) return;

        var scroll = contentRoot.GetComponentInParent<ScrollRect>();
        if (scroll == null) return;

        int current = _ctx.Store.CurrentDungeonIndex.Value;
        foreach (var item in _spawned) {
            if (item == null || item.DungeonIndex != current) continue;
            scroll.ScrollTo((RectTransform)item.transform);
            return;
        }
        scroll.ScrollToStart();
    }

    // 던전 선택 확정. 목록 항목과 최고 단계 버튼이 같은 경로를 지난다.
    private void Select(int dungeonIndex) {
        _onSelect?.Invoke(dungeonIndex);
        CloseAsync().Forget();
    }

    // 지금 도전할 수 있는 가장 높은 던전을 고른다. 그룹 필터와 무관하게 전체 던전에서 찾는다
    // (해금 범위는 진행도 + 1 이지만 데이터가 그보다 짧을 수 있어 실제로 있는 던전 중에서 고른다).
    private void SelectBest() {
        if (_ctx == null || _ctx.DungeonDb == null) return;

        int highestCleared = _ctx.Store.HighestClearedDungeonIndex.Value;
        int best = 0;
        foreach (var dungeon in _ctx.DungeonDb.All) {
            if (dungeon == null || dungeon.DungeonIndex <= best) continue;
            if (DungeonUnlockRule.IsUnlocked(dungeon.DungeonIndex, highestCleared)) best = dungeon.DungeonIndex;
        }
        if (best > 0) Select(best);
    }

    // 잠금/선택 오버레이 갱신. 잠금 판정은 서버가 내려준 진행도 + 공용 규칙(DungeonUnlockRule)으로 한다.
    private void RefreshStates() {
        if (_ctx == null) return;

        int highestCleared = _ctx.Store.HighestClearedDungeonIndex.Value;
        int current = _ctx.Store.CurrentDungeonIndex.Value;

        foreach (var item in _spawned) {
            if (item == null) continue;
            item.ApplyState(DungeonUnlockRule.IsUnlocked(item.DungeonIndex, highestCleared),
                            item.DungeonIndex == current);
        }
    }

    // 선택된 그룹의 던전. 그룹이 하나도 없거나 드롭다운이 없으면 필터 없이 전체 던전(기존 동작).
    private IReadOnlyList<DungeonData> SelectedDungeons() {
        if (_groups.Count == 0 || groupDropdown == null) return _ctx.DungeonDb.All;

        int index = groupDropdown.value;
        if (index < 0 || index >= _groups.Count) index = 0;
        return _ctx.DungeonDb.InGroup(_groups[index]);
    }

    // 전체 던전의 예상 보상을 한 패킷으로 받아 캐시한 뒤, 이미 생성된 항목의 보상 슬롯을 채운다.
    // 실패(타임아웃/취소)해도 목록 자체는 그대로 쓸 수 있으므로 보상만 비워 둔다.
    private async UniTaskVoid LoadPreviewsAsync() {
        var all = _ctx.DungeonDb.All;
        var indexes = new List<int>(all.Count);
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null) indexes.Add(all[i].DungeonIndex);
        if (indexes.Count == 0) return;

        List<DungeonRewardPreview> previews;
        try {
            previews = await _ctx.MainData.GetDungeonRewardPreviewAsync(indexes, this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException) { return; }
        catch (Exception e) {
            Debug.LogWarning($"[DungeonSelectPopup] 던전 보상 미리보기 실패: {e.Message}");
            return;
        }

        foreach (var p in previews) _previews[p.DungeonIndex] = p;

        foreach (var item in _spawned) {
            if (item == null) continue;
            item.ApplyRewardPreview(_ctx, _previews.TryGetValue(item.DungeonIndex, out var v) ? v : null);
        }
    }

    private void ClearItems() {
        foreach (var item in _spawned) if (item != null) Destroy(item.gameObject);
        _spawned.Clear();
    }

    protected override void OnClosed() {
        _disposables.Clear();
        ClearItems();
    }

    private void OnDestroy() => _disposables.Dispose();
}
