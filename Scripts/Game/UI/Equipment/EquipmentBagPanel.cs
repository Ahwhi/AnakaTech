using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 가방 패널(보유 장비 목록). EquipmentPopup 내부에서 켜고 끄는 패널이며, 부위 라디오 버튼(전체/무기/방어구/신발/악세서리)으로
/// 필터한다. 무기 탭은 선택 캐릭터가 장착할 수 있는 무기 종류만 노출하고(장착 불가 무기를 보여줘도 의미가 없다),
/// 하나도 없으면 안내 문구를 표시한다.
///
/// 카드 클릭은 상위(EquipmentPopup)에 통지만 하고, 상세 패널 표시/장착 처리는 상위가 담당한다(패널은 목록만 책임).
/// </summary>
public sealed class EquipmentBagPanel : MonoBehaviour {
    /// <summary>부위 필터. 값(0~4)은 인스펙터 filterToggles 배열 인덱스와 1:1로 대응해야 한다.</summary>
    public enum Filter { All = 0, Weapon = 1, Armor = 2, Shoes = 3, Accessory = 4 }

    [Header("List")]
    [SerializeField] private Transform grid;
    [SerializeField] private EquipmentCard cardPrefab;
    [Tooltip("가방을 채우는 동안 표시할 로딩 그룹(문구/스피너). 비우면 표시 없이 채운다. 선택.")]
    [SerializeField] private GameObject loadingGroup;
    [Tooltip("생성이 이 시간(초)을 넘길 때만 로딩 그룹을 띄운다. 각성/레벨업 후 재구성처럼 금방 끝나는 경우에 " +
             "로딩이 번쩍이는 것을 막는다. 0 이하면 지연 없이 항상 표시.")]
    [SerializeField] private float loadingDelay = 0.2f;
    [Tooltip("표시할 장비가 없을 때 켜는 오브젝트. grid 의 자식이 아니라 형제로 배치할 것. 선택.")]
    [SerializeField] private GameObject emptyGroup;
    [Tooltip("표시할 장비가 없을 때의 문구. 무기 탭에서는 '착용 가능한 무기가 없습니다.' 로 바뀐다. 선택.")]
    [SerializeField] private TMP_Text emptyText;

    [Header("Filter (라디오 버튼)")]
    [Tooltip("부위 필터 토글. 순서 고정: 0=전체, 1=무기, 2=방어구, 3=신발, 4=악세서리. "
           + "선택 표시는 Toggle 의 Graphic(체크마크)이 담당하므로 별도 하이라이트 오브젝트가 필요 없다.")]
    [SerializeField] private Toggle[] filterToggles;
    [Tooltip("위 토글들을 묶는 ToggleGroup. 할당하면 상호배타(라디오)가 자동 보장된다. "
           + "ToggleGroup 의 Allow Switch Off 는 꺼두어야 항상 하나가 선택된 상태가 유지된다.")]
    [SerializeField] private ToggleGroup filterGroup;

    private const int BuildPerFrame = 8;   // 한 프레임에 생성할 카드 수(나머지는 다음 프레임으로 분산).
    private const string EmptyDefault = "보유한 장비가 없습니다.";
    private const string EmptyWeapon = "착용 가능한 무기가 없습니다.";

    private MainHudContext _hud;
    private int _charId;
    private CharacterType _charType;
    private bool _charTypeKnown;
    private Action<EquipmentCard> _onCardClicked;
    private CancellationToken _ct;
    private CancellationTokenSource _buildCts;   // 필터 전환이 겹칠 때 이전 생성 취소용.

    // 카드 풀. 인덱스 순서가 곧 표시 순서이며, 목록보다 남는 카드는 파괴하지 않고 비활성으로 대기시킨다.
    private readonly List<EquipmentCard> _cards = new();
    private Filter _filter = Filter.All;
    private long _selectedInstanceId;
    private long _bestUpgradeInstanceId;   // 이번 목록에서 전투력 상승이 가장 큰 장비(0=없음).
    private bool _wired;
    private bool _gridCleaned;

    /// <summary>현재 필터.</summary>
    public Filter CurrentFilter => _filter;

    public void Initialize(MainHudContext hud, int charDataId, Action<EquipmentCard> onCardClicked) {
        _hud = hud;
        _charId = charDataId;
        _onCardClicked = onCardClicked;
        _ct = this.GetCancellationTokenOnDestroy();

        var ch = _hud.CharDb != null ? _hud.CharDb.Get(charDataId) : null;
        _charTypeKnown = ch != null;
        if (_charTypeKnown) _charType = ch.Type;

        // 리스너는 최초 1회만(패널 재사용이라 중복 등록 방지).
        if (!_wired && filterToggles != null) {
            for (int i = 0; i < filterToggles.Length; i++) {
                if (filterToggles[i] == null) continue;
                if (filterGroup != null) filterToggles[i].group = filterGroup;
                var f = (Filter)i;
                // 토글은 켜질 때/꺼질 때 모두 콜백이 온다. 꺼지는 쪽(직전 선택)은 무시해야 재구성이 두 번 돌지 않는다.
                filterToggles[i].onValueChanged.AddListener(on => {
                    if (!on || _filter == f) return;
                    _hud.Sound.PlaySfx(SfxKey.ButtonClick);
                    SetFilter(f);
                });
            }
            _wired = true;
        }
    }

    /// <summary>필터를 지정하고 목록을 다시 만든다. 토글을 코드로 맞출 때도 이 경로를 쓴다.</summary>
    public void SetFilter(Filter filter) {
        _filter = filter;
        SyncFilterToggles(filter);
        Rebuild();
    }

    /// <summary>부위로 필터를 맞춘다(슬롯을 눌러 가방을 열 때).</summary>
    public void SetFilterBySlot(EquipmentSlot slot) => SetFilter(slot switch {
        EquipmentSlot.Weapon => Filter.Weapon,
        EquipmentSlot.Armor => Filter.Armor,
        EquipmentSlot.Shoes => Filter.Shoes,
        _ => Filter.Accessory
    });

    /// <summary>선택 표시할 인스턴스(0=선택 없음). 목록을 다시 만들지 않고 프레임만 갱신한다.</summary>
    public void SetSelected(long instanceId) {
        _selectedInstanceId = instanceId;
        foreach (var c in _cards) {
            // 풀에 남아 있는(현재 표시되지 않는) 카드는 옛 인스턴스를 들고 있으므로 건드리지 않는다.
            if (c == null || !c.gameObject.activeSelf || c.Inst == null) continue;
            c.SetSelected(c.Inst.InstanceId == instanceId);
        }
    }

    public void Rebuild() => RebuildAsync().Forget();

    private async UniTaskVoid RebuildAsync() {
        _buildCts?.Cancel();
        _buildCts?.Dispose();
        _buildCts = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        var ct = _buildCts.Token;

        CleanStrayChildrenOnce();

        var list = CollectVisible();
        list.Sort(Compare);

        // 정렬을 끝낸 뒤에 최고 상승 장비를 고른다(상승치가 같은 후보가 여럿이면 등급/강화가 앞선 쪽이 뽑힌다).
        _bestUpgradeInstanceId = _hud.Store.BestUpgradeInstanceId(
            _hud.CharDb != null ? _hud.CharDb.Get(_charId) : null, list, _hud.EquipDb, _hud.Settings);
        MoveBestToFront(list);

        // 새 목록보다 많은 카드는 먼저 숨긴다. 생성이 중간에 취소돼도 옛 카드가 남아 보이지 않는다.
        for (int i = list.Count; i < _cards.Count; i++)
            if (_cards[i] != null && _cards[i].gameObject.activeSelf) _cards[i].gameObject.SetActive(false);

        // 로딩 그룹은 "생성이 실제로 오래 걸릴 때"만 띄운다. 각성/레벨업 직후의 재구성처럼 금방 끝나는
        // 경우에는 지연 시간 안에 끝나 아예 표시되지 않으므로 깜빡임이 생기지 않는다.
        if (loadingGroup != null) loadingGroup.SetActive(false);
        if (emptyGroup != null) emptyGroup.SetActive(false);

        var loadingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try {
            ShowLoadingAfterDelayAsync(loadingCts.Token).Forget();

            // 카드를 파괴/재생성하지 않고 풀에서 꺼내 내용만 다시 채운다(각성/레벨업 직후 재구성이 잦다).
            int index = 0;
            await AsyncBuild.SpawnChunkedAsync(list, BuildPerFrame, inst => {
                var card = GetOrCreateCard(index++);
                card.Bind(inst, _hud.EquipDb.Get(inst.EquipDataId), _charId, _onCardClicked);
                card.SetSelected(inst.InstanceId == _selectedInstanceId);
                card.SetBestUpgrade(inst.InstanceId == _bestUpgradeInstanceId);
            }, ct);
        } catch (OperationCanceledException) {
            return;   // 다음 재구성이 표시 상태를 이어받는다(여기서 로딩을 끄면 새 빌드 표시를 지운다).
        } finally {
            // 생성이 끝났으면(또는 취소됐으면) 대기 중인 지연 표시를 취소해 뒤늦게 켜지지 않게 한다.
            loadingCts.Cancel();
            loadingCts.Dispose();
        }

        if (loadingGroup != null) loadingGroup.SetActive(false);
        ShowEmpty(list.Count == 0);   // 풀 크기가 아니라 이번에 표시한 개수 기준.
    }

    // 풀에서 index 번째 카드를 꺼낸다(없으면 하나 만들어 채운다). 재사용 카드는 다시 켜준다.
    // 항상 index 순서대로 꺼내므로 카드의 형제 순서가 곧 표시 순서가 되어 별도 정렬이 필요 없다.
    private EquipmentCard GetOrCreateCard(int index) {
        if (index < _cards.Count) {
            var pooled = _cards[index];
            if (pooled != null) {
                if (!pooled.gameObject.activeSelf) pooled.gameObject.SetActive(true);
                return pooled;
            }
            // 어떤 이유로 파괴된 슬롯이면 그 자리에 새로 만들어 채운다.
            // 새 오브젝트는 grid 의 맨 뒤에 붙으므로 형제 순서를 인덱스 자리로 되돌려 표시 순서를 지킨다.
            var replaced = Instantiate(cardPrefab, grid);
            replaced.transform.SetSiblingIndex(index);
            _cards[index] = replaced;
            return replaced;
        }

        var created = Instantiate(cardPrefab, grid);
        _cards.Add(created);
        return created;
    }

    // 지연 시간이 지나도 생성이 끝나지 않았을 때만 로딩 그룹을 켠다(끝났으면 토큰이 취소되어 켜지지 않는다).
    private async UniTaskVoid ShowLoadingAfterDelayAsync(CancellationToken ct) {
        if (loadingGroup == null) return;
        try {
            if (loadingDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(loadingDelay), ignoreTimeScale: true, cancellationToken: ct);
            if (!ct.IsCancellationRequested) loadingGroup.SetActive(true);
        } catch (OperationCanceledException) {
            // 지연 안에 생성이 끝난 정상 경로.
        }
    }

    // 표시 대상 수집: 가방(미장착)만, 부위 필터 통과, 무기는 캐릭터가 장착 가능한 종류만.
    private List<EquipmentInstanceDto> CollectVisible() {
        var list = new List<EquipmentInstanceDto>();
        foreach (var kv in _hud.Store.Equipment) {
            var inst = kv.Value;
            if (inst.IsEquipped) continue;
            var data = _hud.EquipDb.Get(inst.EquipDataId);
            if (data == null) continue;                       // 아직 SO 가 없는 장비 id 는 표시하지 않는다.
            if (!PassesFilter(data)) continue;
            list.Add(inst);
        }
        return list;
    }

    // 전투력 상승이 가장 큰 장비를 맨 앞으로 당겨 온다(나머지 카드의 상대 순서는 그대로 유지).
    private void MoveBestToFront(List<EquipmentInstanceDto> list) {
        if (_bestUpgradeInstanceId == 0) return;
        for (int i = 1; i < list.Count; i++) {
            if (list[i].InstanceId != _bestUpgradeInstanceId) continue;
            var best = list[i];
            list.RemoveAt(i);
            list.Insert(0, best);
            return;
        }
    }

    private bool PassesFilter(EquipmentData data) {
        // 무기는 어느 탭에서 보든 장착 가능한 종류만 노출한다(전체 탭 포함 - 못 끼는 무기는 보여줄 의미가 없다).
        if (data.Slot == EquipmentSlot.Weapon && _charTypeKnown && !data.CanBeEquippedBy(_charType)) return false;

        return _filter switch {
            Filter.All => true,
            Filter.Weapon => data.Slot == EquipmentSlot.Weapon,
            Filter.Armor => data.Slot == EquipmentSlot.Armor,
            Filter.Shoes => data.Slot == EquipmentSlot.Shoes,
            Filter.Accessory => data.Slot == EquipmentSlot.Accessory,
            _ => true
        };
    }

    // 정렬: 등급 내림차순 -> 별레벨 내림차순 -> 레벨 내림차순 -> 이름. 좋은 장비가 위로 오게 한다.
    private int Compare(EquipmentInstanceDto a, EquipmentInstanceDto b) {
        var da = _hud.EquipDb.Get(a.EquipDataId);
        var db = _hud.EquipDb.Get(b.EquipDataId);
        if (da == null || db == null) return 0;

        int cmp = db.Grade.CompareTo(da.Grade);
        if (cmp != 0) return cmp;
        cmp = b.StarLevel.CompareTo(a.StarLevel);
        if (cmp != 0) return cmp;
        cmp = b.Level.CompareTo(a.Level);
        if (cmp != 0) return cmp;
        cmp = da.Slot.CompareTo(db.Slot);
        if (cmp != 0) return cmp;
        return string.Compare(da.DisplayName, db.DisplayName, StringComparison.Ordinal);
    }

    private void ShowEmpty(bool empty) {
        if (emptyText != null && empty)
            emptyText.text = _filter == Filter.Weapon ? EmptyWeapon : EmptyDefault;
        if (emptyGroup != null) emptyGroup.SetActive(empty);
        else if (emptyText != null) emptyText.gameObject.SetActive(empty);
    }

    // 토글 상태를 현재 필터에 맞춘다. SetIsOnWithoutNotify 를 써서 onValueChanged 로 되돌아와
    // 재구성이 중복 실행되는 것을 막는다(슬롯 클릭으로 필터를 코드에서 바꿀 때 특히 중요).
    // ToggleGroup 이 할당돼 있으면 켜는 순간 나머지가 자동으로 꺼지고, 없으면 여기서 직접 꺼준다.
    private void SyncFilterToggles(Filter filter) {
        if (filterToggles == null) return;
        for (int i = 0; i < filterToggles.Length; i++) {
            if (filterToggles[i] == null) continue;
            bool on = i == (int)filter;
            if (on || filterGroup == null) filterToggles[i].SetIsOnWithoutNotify(on);
        }
    }

    // 프리팹에 남아 있는 디자인타임 카드를 최초 1회만 걷어낸다.
    // 이후로는 카드를 파괴하지 않고 풀(_cards)에서 재사용하므로 매번 훑을 필요가 없다.
    private void CleanStrayChildrenOnce() {
        if (_gridCleaned || grid == null) return;
        _gridCleaned = true;

        // Destroy 는 프레임 끝에 처리되므로 먼저 계층/레이아웃에서 즉시 떼어낸다(파괴 대기 카드가 겹쳐 보이는 것 방지).
        for (int i = grid.childCount - 1; i >= 0; i--) {
            var child = grid.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
        _cards.Clear();
    }

    private void OnDisable() {
        _buildCts?.Cancel();
    }

    private void OnDestroy() {
        _buildCts?.Cancel();
        _buildCts?.Dispose();
        _buildCts = null;
    }
}
