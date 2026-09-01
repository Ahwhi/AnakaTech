using System;
using System.Collections.Generic;
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
        for (int i = 0; i < _groups.Count; i++) options.Add(_groups[i].DisplayName);

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
            item.Bind(_ctx, dungeon, preview, index => {
                _onSelect?.Invoke(index);
                CloseAsync().Forget();
            });
            _spawned.Add(item);
        }
        RefreshStates();
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
