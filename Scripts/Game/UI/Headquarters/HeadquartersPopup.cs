using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 용병단 본부 팝업(계정 단위 성장). 노드 트리를 데이터로부터 생성해 배치하고, 노드 사이를 연결선으로 잇는다.
/// 노드를 올리면 보유한 모든 캐릭터의 능력치가 함께 올라간다(뽑지 못한 캐릭터가 있어도 계정 전력이 오르는 안전판).
///
/// 트리 화면을 손으로 배치하지 않는 이유: 노드가 늘어날 때마다 프리팹을 다시 짜야 하고, 선행 관계가 바뀌면
/// 연결선을 일일이 옮겨야 한다. 노드 좌표(HqNodeData.GridPos)와 선행 목록(Requires)만 데이터로 두면
/// 에셋 추가만으로 트리가 확장된다.
/// </summary>
public sealed class HeadquartersPopup : UIView, IHudPopup {
    [Header("Tree")]
    [Tooltip("노드/연결선이 생성될 컨테이너(스크롤뷰의 Content 를 지정). 앵커/피벗/크기는 코드가 강제로 맞추므로 " +
             "LayoutGroup 이나 ContentSizeFitter 를 붙이면 안 된다(붙이면 코드가 잡은 좌표를 덮어써 트리가 깨진다).")]
    [SerializeField] private RectTransform treeContent;
    [SerializeField] private HqNodeView nodePrefab;
    [Tooltip("연결선 프리팹(가로로 긴 Image + HqNodeLinkView).")]
    [SerializeField] private HqNodeLinkView linkPrefab;
    [Tooltip("계열 머리말 프리팹(전투/방어/기교 훈련). 계열마다 하나씩 자동 생성되어 해당 노드들 위에 놓인다. 선택.")]
    [SerializeField] private HqLineHeaderView lineHeaderPrefab;
    [Tooltip("격자 1칸의 픽셀 간격(x=열 간격, y=행 간격). 노드 좌표에 이 값을 곱해 배치한다.")]
    [SerializeField] private Vector2 gridSpacing = new Vector2(220f, 180f);
    [Tooltip("트리 가장자리 노드와 컨테이너 경계 사이의 여백. 노드 크기의 절반보다 크게 잡아야 끝 노드가 잘리지 않는다.")]
    [SerializeField] private Vector2 treePadding = new Vector2(160f, 140f);

    [Header("Detail")]
    [SerializeField] private TMP_Text detailNameText;
    [Tooltip("현재 레벨 기준 누적 효과.")]
    [SerializeField] private TMP_Text detailEffectText;
    [Tooltip("다음 레벨 기준 누적 효과(최대면 비운다).")]
    [SerializeField] private TMP_Text detailNextEffectText;
    [Tooltip("잠긴 노드의 선행 조건 안내(예: OO 먼저 해금 필요).")]
    [SerializeField] private TMP_Text requirementText;
    [Tooltip("레벨업 비용(명성). 부족하면 빨간색. 본부는 명성만 소모한다.")]
    [SerializeField] private TMP_Text renownCostText;
    [SerializeField] private Image renownIcon;
    [SerializeField] private Button upgradeButton;

    [Header("Header")]
    [Tooltip("본부 전체 진행도(모든 노드 레벨의 합 / 최대 합). 선택.")]
    [SerializeField] private TMP_Text hqLevelText;
    [Tooltip("상단 재화 패널(명성 CurrencyView 배치).")]
    [SerializeField] private CurrencyPanel currencyPanel;
    [Tooltip("목록을 채우는 동안 표시할 로딩 그룹(선택).")]
    [SerializeField] private GameObject loadingGroup;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private static readonly Color InsufficientColor = new Color(0.90f, 0.20f, 0.20f);
    // 선행 조건이 없거나 이미 충족한 노드의 요구사항 문구.
    private const string NoRequirement = "-";

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private bool _busy;

    private readonly List<HqNodeView> _nodes = new();
    private readonly List<HqNodeLinkView> _links = new();
    private readonly Dictionary<int, HqNodeView> _nodeById = new();
    private readonly Dictionary<HqNodeLine, HqLineHeaderView> _lineHeaders = new();
    // 계열별 진행도 누적용(갱신마다 비워 재사용한다).
    private readonly Dictionary<HqNodeLine, Vector2Int> _lineProgress = new();

    private HqNodeData _selected;
    private Color _costNormalColor = Color.white;
    private bool _costColorCaptured;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (renownCostText != null && !_costColorCaptured) {
            _costNormalColor = renownCostText.color;
            _costColorCaptured = true;
        }

        if (currencyPanel != null) currencyPanel.Initialize(ctx);
        CurrencyIcon.Apply(renownIcon, ctx.Currencies, CurrencyType.Renown);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); UpgradeAsync().Forget(); });

        BuildTree();
        SelectFirstNode();
        // 서버 응답 전(혹은 통신 실패)에도 트리가 이름/최대레벨까지 제대로 보이도록 스토어 값으로 먼저 그린다.
        RefreshTree();
        LoadAsync().Forget();
    }

    // 팝업을 열자마자 상세 패널이 비어 있지 않도록 첫 노드를 골라 둔다(데이터베이스 등록 순서 = 트리의 시작점).
    private void SelectFirstNode() {
        if (_selected != null || _ctx.HqDb == null) return;
        foreach (var data in _ctx.HqDb.All) {
            if (data == null) continue;
            _selected = data;
            return;
        }
    }

    private async UniTaskVoid LoadAsync() {
        if (loadingGroup != null) loadingGroup.SetActive(true);
        try {
            var info = await _ctx.Headquarters.RequestInfoAsync(_ct);
            _ctx.Store.ApplyHeadquarters(info);
            RefreshTree();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("본부 정보를 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
    }

    // ── 트리 생성 ──
    // 노드를 먼저 만들어 배치한 뒤, 선행 관계대로 연결선을 잇는다.
    // 선은 노드보다 뒤에 그려져야 하므로 컨테이너 자식 순서에서 앞쪽으로 보낸다.
    private void BuildTree() {
        ClearTree();

        if (nodePrefab == null || treeContent == null || _ctx.HqDb == null) return;
        if (!TryGetGridBounds(out var gridMin, out var gridMax)) return;

        // 계열 머리말은 각 계열의 가장 윗줄보다 한 칸 위에 놓이므로 그만큼 격자를 위로 넓힌다.
        if (lineHeaderPrefab != null) gridMin.y -= 1f;

        // 격자 좌표가 0 부터 시작하든 음수가 섞이든 트리가 컨테이너 한가운데 오도록 중심을 기준으로 배치한다.
        var gridCenter = (gridMin + gridMax) * 0.5f;
        ApplyContentSize(gridMax - gridMin);

        foreach (var data in _ctx.HqDb.All) {
            if (data == null) continue;
            var view = Instantiate(nodePrefab, treeContent);
            Place(view.Rect, data.GridPos, gridCenter);

            _nodes.Add(view);
            _nodeById[data.Id] = view;
        }

        BuildLineHeaders(gridCenter);
        BuildLinks();
    }

    // 계열마다 머리말 하나. 그 계열 노드들이 차지하는 열의 한가운데, 가장 윗줄 바로 위에 놓는다.
    // 계열 구성이 데이터에서 나오므로 노드를 옮기거나 추가해도 머리말이 알아서 따라간다.
    private void BuildLineHeaders(Vector2 gridCenter) {
        if (lineHeaderPrefab == null) return;

        var bounds = new Dictionary<HqNodeLine, (Vector2 min, Vector2 max)>();
        foreach (var data in _ctx.HqDb.All) {
            if (data == null) continue;
            bounds[data.Line] = bounds.TryGetValue(data.Line, out var b)
                ? (Vector2.Min(b.min, data.GridPos), Vector2.Max(b.max, data.GridPos))
                : (data.GridPos, data.GridPos);
        }

        foreach (var kv in bounds) {
            var header = Instantiate(lineHeaderPrefab, treeContent);
            var gridPos = new Vector2((kv.Value.min.x + kv.Value.max.x) * 0.5f, kv.Value.min.y - 1f);
            Place(header.Rect, gridPos, gridCenter);
            _lineHeaders[kv.Key] = header;
        }
    }

    // 격자 좌표에 맞춰 컨테이너 안에 배치한다. 앵커/피벗까지 코드가 정해 프리팹 설정에 좌우되지 않게 한다.
    private void Place(RectTransform rect, Vector2 gridPos, Vector2 gridCenter) {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = PositionFor(gridPos, gridCenter);
    }

    private void ClearTree() {
        foreach (var n in _nodes) if (n != null) Destroy(n.gameObject);
        foreach (var l in _links) if (l != null) Destroy(l.gameObject);
        foreach (var kv in _lineHeaders) if (kv.Value != null) Destroy(kv.Value.gameObject);
        _nodes.Clear();
        _links.Clear();
        _nodeById.Clear();
        _lineHeaders.Clear();
    }

    // 데이터에 들어 있는 격자 좌표의 최소/최대. 노드를 추가해도 트리 크기가 알아서 따라온다.
    private bool TryGetGridBounds(out Vector2 min, out Vector2 max) {
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);

        bool any = false;
        foreach (var data in _ctx.HqDb.All) {
            if (data == null) continue;
            min = Vector2.Min(min, data.GridPos);
            max = Vector2.Max(max, data.GridPos);
            any = true;
        }
        return any;
    }

    // 격자 좌표 -> 컨테이너 로컬 좌표. y 는 아래로 갈수록 커지는 격자라 화면 좌표계(위가 +)에 맞춰 부호를 뒤집는다.
    private Vector2 PositionFor(Vector2 gridPos, Vector2 gridCenter) =>
        new Vector2((gridPos.x - gridCenter.x) * gridSpacing.x,
                    -(gridPos.y - gridCenter.y) * gridSpacing.y);

    // 스크롤이 되도록 컨테이너 크기를 트리 크기에 맞춘다. ContentSizeFitter 를 쓰지 않는 이유는
    // 자식 좌표를 코드가 직접 정하기 때문이다(피터는 LayoutGroup 이 계산한 크기만 따라가므로 여기서는 쓸 수 없다).
    private void ApplyContentSize(Vector2 gridSpan) {
        treeContent.anchorMin = treeContent.anchorMax = treeContent.pivot = new Vector2(0.5f, 0.5f);
        treeContent.anchoredPosition = Vector2.zero;
        treeContent.sizeDelta = new Vector2(gridSpan.x * gridSpacing.x + treePadding.x * 2f,
                                            gridSpan.y * gridSpacing.y + treePadding.y * 2f);
    }

    private void BuildLinks() {
        if (linkPrefab == null) return;

        foreach (var data in _ctx.HqDb.All) {
            if (data == null || data.Requires == null) continue;
            if (!_nodeById.TryGetValue(data.Id, out var toView)) continue;

            foreach (int requireId in data.Requires) {
                if (!_nodeById.TryGetValue(requireId, out var fromView)) continue;

                var link = Instantiate(linkPrefab, treeContent);
                // 노드보다 먼저 그려지도록(= 뒤에 깔리도록) 형제 순서를 맨 앞으로 보낸다.
                link.transform.SetAsFirstSibling();
                link.Connect(fromView.Rect.anchoredPosition, toView.Rect.anchoredPosition, Color.gray);
                _links.Add(link);
            }
        }
    }

    // ── 상태 갱신 ──
    private void RefreshTree() {
        if (_ctx == null || _ctx.HqDb == null) return;

        int renown = _ctx.Store.Renown.Value;
        int totalLevel = 0;
        int totalMaxLevel = 0;
        _lineProgress.Clear();

        // 생성 순서가 아니라 데이터베이스를 기준으로 순회한다(뷰는 id 로 찾는다).
        // 뷰가 들고 있는 Data 를 되읽으면 첫 갱신 때는 아직 Bind 전이라 비어 있다.
        foreach (var data in _ctx.HqDb.All) {
            if (data == null || !_nodeById.TryGetValue(data.Id, out var view) || view == null) continue;

            int level = LevelOf(data.Id);
            totalLevel += level;
            totalMaxLevel += HqNodeRules.MaxLevel;

            _lineProgress.TryGetValue(data.Line, out var line);
            _lineProgress[data.Line] = new Vector2Int(line.x + level, line.y + HqNodeRules.MaxLevel);

            var state = ResolveState(data, level);
            bool upgradable = HqNodeRules.CanUpgrade(_ctx.Store.HqNodeLevels, data, renown);

            view.Bind(data, level, state, upgradable, OnNodeClicked);
            view.SetSelected(_selected != null && _selected.Id == data.Id);
        }

        RefreshLinkColors();

        foreach (var kv in _lineHeaders) {
            if (kv.Value == null) continue;
            _lineProgress.TryGetValue(kv.Key, out var p);
            kv.Value.Bind(kv.Key, p.x, p.y);
        }

        if (hqLevelText != null) hqLevelText.text = $"{totalLevel} / {totalMaxLevel}";
        RefreshDetail();
    }

    // 선은 "선행 노드가 열렸는지"를 보여준다. 열린 선행에서 뻗어 나온 선만 밝게 칠해 진행 경로가 눈에 들어오게 한다.
    private void RefreshLinkColors() {
        int index = 0;
        foreach (var data in _ctx.HqDb.All) {
            if (data == null || data.Requires == null) continue;
            if (!_nodeById.ContainsKey(data.Id)) continue;

            foreach (int requireId in data.Requires) {
                if (!_nodeById.TryGetValue(requireId, out var fromView)) continue;
                if (index >= _links.Count) return;

                bool active = LevelOf(requireId) >= HqNodeRules.PrerequisiteLevel;
                var color = active ? fromView.ColorForLink : new Color(0.3f, 0.3f, 0.35f);
                _links[index].Connect(fromView.Rect.anchoredPosition,
                                      _nodeById[data.Id].Rect.anchoredPosition, color);
                index++;
            }
        }
    }

    private int LevelOf(int nodeId) => HqNodeRules.LevelOf(_ctx.Store.HqNodeLevels, nodeId);

    private HqNodeState ResolveState(HqNodeData data, int level) {
        if (level >= HqNodeRules.MaxLevel) return HqNodeState.Maxed;
        if (level > 0) return HqNodeState.Owned;
        return PrerequisitesMet(data) ? HqNodeState.Available : HqNodeState.Locked;
    }

    // 해금/강화 판정은 알림 배지와 같은 규칙(HqNodeRules)을 쓴다. 두 곳이 따로 계산하면 표시가 어긋난다.
    private bool PrerequisitesMet(HqNodeData data) =>
        HqNodeRules.PrerequisitesMet(_ctx.Store.HqNodeLevels, data);

    private void OnNodeClicked(HqNodeView view) {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        _selected = view.Data;
        foreach (var n in _nodes)
            if (n != null) n.SetSelected(_selected != null && n.Data != null && n.Data.Id == _selected.Id);
        RefreshDetail();
    }

    private void RefreshDetail() {
        if (_selected == null) {
            if (detailNameText != null) detailNameText.text = "";
            if (detailEffectText != null) detailEffectText.text = "";
            if (detailNextEffectText != null) detailNextEffectText.text = "";
            if (requirementText != null) requirementText.text = NoRequirement;
            SetCostText(renownCostText, false, 0, 0);
            if (upgradeButton != null) upgradeButton.interactable = false;
            return;
        }

        int level = LevelOf(_selected.Id);
        bool maxed = level >= HqNodeRules.MaxLevel;
        bool unlocked = PrerequisitesMet(_selected);

        if (detailNameText != null) detailNameText.text = $"{_selected.DisplayName} ({level} / {HqNodeRules.MaxLevel})";
        if (detailEffectText != null) {
            string current = _selected.EffectSummary(level);
            detailEffectText.text = string.IsNullOrEmpty(current) ? "효과 없음" : current;
        }
        if (detailNextEffectText != null)
            detailNextEffectText.text = maxed ? "" : _selected.EffectSummary(level + 1);

        // 조건이 없을 때도 칸을 비우지 않고 "-" 를 둔다(줄이 사라지면 상세 패널 높이가 노드마다 들쭉날쭉해진다).
        if (requirementText != null)
            requirementText.text = unlocked ? NoRequirement : BuildRequirementText(_selected);

        int renownCost = _selected.UpgradeCost(level);
        int renown = _ctx.Store.Renown.Value;
        bool costShown = unlocked && !maxed;

        SetCostText(renownCostText, costShown, renownCost, renown);

        if (upgradeButton != null)
            upgradeButton.interactable = costShown && renown >= renownCost;
    }

    // 잠긴 노드의 안내 문구. 아직 조건에 못 미친 선행 노드를 진행도와 함께 보여준다(얼마나 남았는지가 바로 보이도록).
    // "무엇이 필요한지"를 알리는 머리말은 화면의 고정 라벨이 맡으므로 여기서는 노드 줄만 만든다.
    // 조건이 여럿이면 한 줄에 이어 붙이지 않고 줄바꿈한다. 노드 이름 길이가 제각각이라
    // 쉼표로 이으면 어디까지가 한 노드인지 읽어 내야 하지만, 줄이 나뉘면 그냥 보인다.
    private string BuildRequirementText(HqNodeData data) {
        var sb = new System.Text.StringBuilder();
        foreach (int id in data.Requires) {
            var req = _ctx.HqDb.Get(id);
            if (req == null) continue;

            int level = LevelOf(id);
            if (level >= HqNodeRules.PrerequisiteLevel) continue;

            if (sb.Length > 0) sb.Append('\n');
            sb.Append(req.DisplayName).Append(' ').Append(level).Append('/').Append(HqNodeRules.PrerequisiteLevel);
        }
        return sb.ToString();
    }

    private void SetCostText(TMP_Text text, bool show, int cost, int owned) {
        if (text == null) return;
        text.text = show ? cost.ToString("N0") : "-";
        text.color = show && owned < cost ? InsufficientColor : _costNormalColor;
    }

    private async UniTaskVoid UpgradeAsync() {
        if (_busy || _selected == null) return;
        _busy = true;
        try {
            var res = await _ctx.Headquarters.UpgradeNodeAsync(_selected.Id, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(FailMessage(res.FailReason));
                return;
            }
            _ctx.Store.ApplyHqNodeUpgrade(res.NodeId, res.Level, res.NewRenown);
            _ctx.Sound.PlaySfx(SfxKey.Purchase);
            RefreshTree();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("본부 강화에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    // 서버 UpgradeHqNode 의 실패 코드와 짝을 이룬다.
    private static string FailMessage(int fail) => fail switch {
        1 => "존재하지 않는 노드입니다.",
        2 => "이미 최대 레벨입니다.",
        3 => "선행 노드를 최대 레벨까지 올려야 합니다.",
        4 => "명성이 부족합니다.",
        _ => "강화할 수 없습니다."
    };

    protected override void OnClosed() => ClearTree();
}
