using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 딜량 그래프 패널. 현재 파티의 캐릭터를 이번 런 누적 딜량 내림차순으로 세로 정렬해
/// 초상화 + 막대 + 비율(%)로 보여주고, 순위가 바뀌면 두 행이 자리를 맞바꾸는 이동 연출을 낸다.
/// 접기/펴기는 공용 FoldablePanel 이 맡는다(접힘 상태는 로컬 JSON 에 저장된다).
///
/// 집계는 BattleDamageMeter(모델)가, 행의 표현/자기 연출은 DamageMeterRow 가 담당한다.
/// 이 패널은 그 사이에서 "누가 몇 위인지"와 "그 순위의 y 좌표"만 정한다.
///
/// 행은 세로 레이아웃 그룹을 쓰지 않고 순위 인덱스로 y 를 직접 계산해 배치한다.
/// (레이아웃 그룹은 순서가 바뀌는 순간 즉시 재배치해 버려 스왑 연출과 충돌한다.)
/// </summary>
public sealed class DamageMeterPanel : MonoBehaviour {
    [Header("Refs")]
    [Tooltip("행이 생성될 부모(세로로 쌓인다). 피벗을 위쪽(y=1)으로 두면 위에서부터 채워진다.")]
    [SerializeField] private RectTransform rowContainer;
    [Tooltip("행 프리팹(DamageMeterRow).")]
    [SerializeField] private DamageMeterRow rowPrefab;

    [Header("Layout")]
    [Tooltip("행 1개의 세로 간격(행 높이 + 여백).")]
    [SerializeField] private float rowSpacing = 56f;
    [Tooltip("첫 행의 y 오프셋(컨테이너 위쪽 기준, 보통 음수이거나 0).")]
    [SerializeField] private float firstRowOffset = 0f;

    [Header("Refresh")]
    [Tooltip("순위/수치를 다시 계산하는 주기(초). 매 프레임 재정렬하면 근소한 차이로 순위가 떨리므로 간격을 둔다.")]
    [SerializeField] private float refreshInterval = 0.25f;

    [Header("Animation")]
    [Tooltip("순위가 바뀐 행이 새 자리로 이동하는 시간(초).")]
    [SerializeField] private float swapDuration = 0.35f;
    [Tooltip("순위 이동 이징. OutBack 으로 두면 자리에 살짝 튕기며 안착한다.")]
    [SerializeField] private Ease swapEase = Ease.OutCubic;
    [Tooltip("막대/퍼센트가 새 값으로 차오르는 시간(초). 갱신 주기와 비슷하게 두면 끊김 없이 이어진다.")]
    [SerializeField] private float valueDuration = 0.25f;
    [Tooltip("행 등장/퇴장 페이드 시간(초).")]
    [SerializeField] private float rowFadeDuration = 0.2f;

    [Header("Fold (접기/펴기)")]
    [Tooltip("접기/펴기를 담당하는 공용 컴포넌트. 접힘 상태 저장 키도 거기서 지정한다. 비우면 항상 펼친 상태.")]
    [SerializeField] private FoldablePanel foldable;

    private BattleHudContext _ctx;
    private BattleDamageMeter _meter;

    // 행 풀(파티 최대 인원만큼만 쓰인다). 인덱스는 생성 순서이고 화면 순서는 MoveTo 가 정한다.
    private readonly List<DamageMeterRow> _rows = new();
    // 재사용 버퍼(매 갱신 할당 방지).
    private readonly List<int> _charIds = new();
    private readonly List<BattleDamageMeter.Entry> _ranking = new();

    private float _refreshTimer;
    private bool _pendingInstantLayout;   // 파티 재구성 직후 첫 배치는 트윈 없이 놓는다.

    // 접기 컴포넌트가 없으면 항상 펼쳐진 것으로 본다(접기 UI 없이 쓰는 배치도 허용).
    private bool Expanded => foldable == null || foldable.IsExpanded;

    public void Initialize(BattleHudContext ctx) {
        _ctx = ctx;
        _meter = ctx.Stage.DamageMeter;

        if (foldable != null) {
            foldable.Initialize(ctx.Sound);
            foldable.ExpandedChanged += OnExpandedChanged;
        }

        ctx.Stage.PartyChanged += RebuildRows;
        RebuildRows();
    }

    private void OnDestroy() {
        if (_ctx != null && _ctx.Stage != null) _ctx.Stage.PartyChanged -= RebuildRows;
        if (foldable != null) foldable.ExpandedChanged -= OnExpandedChanged;
    }

    // 접혀 있는 동안은 갱신을 멈추므로 순위가 밀려 있다. 다시 펼 때 트윈 없이 한 번에 맞춰 놓는다.
    private void OnExpandedChanged(bool expanded) {
        if (!expanded) return;
        _pendingInstantLayout = true;
        _refreshTimer = 0f;
        Refresh();
    }

    // 파티가 재구성되면(런 시작/덱 변경) 행을 파티 인원수에 맞추고 다시 바인딩한다.
    // 딜량은 런 시작 시 디렉터가 초기화하므로 여기서는 표시만 0 으로 되돌아간다.
    private void RebuildRows() {
        if (rowPrefab == null || rowContainer == null || _ctx == null) return;

        var party = _ctx.Stage.Party;
        int count = party != null ? party.Count : 0;

        while (_rows.Count < count) _rows.Add(Instantiate(rowPrefab, rowContainer));

        _charIds.Clear();
        for (int i = 0; i < _rows.Count; i++) {
            var row = _rows[i];
            if (i < count) {
                var data = party[i].Data;
                // 유닛이 외형의 단일 진실 원천이라 코스튬 판정을 여기서 다시 할 필요가 없다.
                row.Bind(party[i].Appearance);
                row.Show(true, rowFadeDuration);
                if (data != null) _charIds.Add(data.Id);
            } else {
                row.Clear();
                row.Show(false, rowFadeDuration);
            }
        }

        _pendingInstantLayout = true;   // 재구성 직후에는 스왑 연출 없이 제자리에 놓는다.
        _refreshTimer = 0f;
        Refresh();
    }

    private void Update() {
        if (_meter == null || !Expanded) return;   // 접혀 있으면 갱신하지 않는다(보이지 않는 트윈 비용 제거).

        _refreshTimer -= Time.unscaledDeltaTime;
        if (_refreshTimer > 0f) return;
        _refreshTimer = Mathf.Max(0.02f, refreshInterval);
        Refresh();
    }

    // 순위를 다시 계산해 각 행의 목표 위치와 수치를 갱신한다.
    private void Refresh() {
        if (_meter == null || _charIds.Count == 0) return;

        _meter.BuildRanking(_charIds, _ranking);

        bool instant = _pendingInstantLayout;
        _pendingInstantLayout = false;

        for (int rank = 0; rank < _ranking.Count; rank++) {
            var entry = _ranking[rank];
            var row = FindRow(entry.CharId);
            if (row == null) continue;

            row.MoveTo(rank, firstRowOffset - rank * rowSpacing, swapDuration, swapEase, instant);
            row.SetValues(entry.TopRatio, entry.Share, entry.Damage, instant ? 0f : valueDuration);
        }
    }

    private DamageMeterRow FindRow(int charId) {
        for (int i = 0; i < _rows.Count; i++)
            if (_rows[i].CharId == charId) return _rows[i];
        return null;
    }
}
