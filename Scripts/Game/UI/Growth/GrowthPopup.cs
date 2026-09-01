using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 캐릭터 육성 팝업(PLANNING 10 육성 부분). 스탯포인트 투자 페이지와 별레벨 승급 페이지를 버튼으로 전환한다.
/// 스탯/별 승급은 서버 권위(AllocateStat/UpgradeStar)로 처리하고 응답으로 받은 최신 성장치를 스토어에 반영한다.
/// 조각/만능조각 팝업은 이후 마일스톤(현재 조각량/필요 조각량 표시까지).
/// </summary>
public sealed class GrowthPopup : UIView {
    [Header("Page Switch")]
    [SerializeField] private Button statPageButton;
    [SerializeField] private Button starPageButton;
    [SerializeField] private GameObject statPanel;
    [SerializeField] private GameObject starPanel;
    [Tooltip("스탯 페이지 버튼 배지: 투자할 스탯포인트가 있으면 표시(선택). BadgeView, Source=Manual.")]
    [SerializeField] private BadgeView statBadge;
    [Tooltip("별 승급 버튼 배지: 승급 가능(조각+다이아 충분)하면 표시(선택). BadgeView, Source=Manual.")]
    [SerializeField] private BadgeView starBadge;

    [Header("Stat Page")]
    [SerializeField] private TMP_Text statPointsText;
    [SerializeField] private Transform statRowParent;   // rows container
    [SerializeField] private GrowthStatRow statRowPrefab;
    [Tooltip("스탯 초기화 버튼(다이아 소모). 투자한 포인트가 없으면 비활성.")]
    [SerializeField] private Button resetStatButton;
    [Tooltip("초기화 비용 표시(선택, 숫자만). 보유 다이아가 부족하면 빨간색.")]
    [SerializeField] private TMP_Text resetCostText;
    [Tooltip("초기화 비용 아이콘(선택). 재화 카탈로그에서 다이아 아이콘을 자동 표시.")]
    [SerializeField] private Image resetCostIcon;
    [Tooltip("스탯포인트 자동 투자 대상 드롭다운. 항목은 코드가 채운다(첫 항목 '미설정' + 투자 가능 스탯).")]
    [SerializeField] private TMP_Dropdown autoStatDropdown;
    [Tooltip("일괄 투자 버튼. 스탯과 수량을 골라 한 번에 투자하는 팝업을 연다. 투자할 포인트가 없으면 비활성.")]
    [SerializeField] private Button bulkAllocateButton;
    [Tooltip("스탯별 투자(+) 버튼의 기본색. 적지 않은 스탯은 프리팹에 설정된 색을 그대로 쓴다. " +
             "줄이 여섯이라 색으로 구분되면 글자를 읽지 않고도 어느 스탯인지 알 수 있다.")]
    [SerializeField] private PlusButtonColor[] plusButtonColors;

    /// <summary>스탯 하나와 그 줄의 투자 버튼 기본색. 순서가 아니라 스탯으로 짝지어 두어 행 순서가 바뀌어도 어긋나지 않는다.</summary>
    [Serializable]
    private sealed class PlusButtonColor {
        [Tooltip("색을 지정할 스탯.")]
        public StatType stat = StatType.Attack;
        [Tooltip("그 스탯 줄의 투자 버튼 기본색.")]
        public Color normal = Color.white;
    }

    [Header("Star Page")]
    [Tooltip("현재 별레벨(숫자).")]
    [SerializeField] private TMP_Text currentLevelText;
    [Tooltip("승급 후 별레벨(숫자). 최대치면 현재와 동일.")]
    [SerializeField] private TMP_Text nextLevelText;
    [Tooltip("현재 별레벨을 별 그리드로 표시(StarLevelView, 최대 15레벨). 비우면 표시 안 함.")]
    [SerializeField] private StarLevelView currentStarView;
    [Tooltip("승급 후(다음) 별레벨을 별 그리드로 표시. 최대치면 현재와 동일하게 표시.")]
    [SerializeField] private StarLevelView nextStarView;
    [Tooltip("조각 보유량/필요량을 한 텍스트에 '보유 / 필요' 형태로 표시. 보유가 필요 이상이면 초록, 부족하면 빨강.")]
    [SerializeField] private TMP_Text shardCountText;
    [Tooltip("조각 이름 표시(캐릭터명 + ' 조각'). 색은 바꾸지 않는다.")]
    [SerializeField] private TMP_Text shardNameText;
    [Tooltip("현재 캐릭터 등급에 해당하는 보유 만능조각 수 표시(예: 'SSR 만능조각\n3개'). 수량 부분만 색을 입힌다.")]
    [SerializeField] private TMP_Text universalShardText;
    [Tooltip("만능조각 수량을 그 등급의 색(GradeColorCatalog)으로 적을지. 이 버튼 배경에서 잘 읽히지 않는 " +
             "등급색이 있으면 꺼서 아래 고정색을 쓴다. 카탈로그를 고치면 등급색을 쓰는 다른 화면까지 함께 " +
             "바뀌므로 이 표시만 예외로 두려면 여기서 끄는 쪽이 안전하다.")]
    [SerializeField] private bool universalShardUseGradeColor = true;
    [Tooltip("위 옵션을 껐을 때 만능조각 수량에 쓸 색. 등급과 무관하게 항상 이 색으로 적는다.")]
    [SerializeField] private Color universalShardCountColor = Color.white;
    [Tooltip("현재 승급에 필요한 다이아 수(숫자만). 보유 다이아보다 많으면 빨간색으로 표시.")]
    [SerializeField] private TMP_Text requiredDiaText;
    [Tooltip("이번 승급 시 상승하는 능력치 미리보기(예: '공격력 +10\\n방어력 +10'). 비우면 표시 안 함.")]
    [SerializeField] private TMP_Text nextStarGrowthText;
    [Tooltip("별 성장표 SO(BattleSettings 와 동일 에셋 할당). 승급 미리보기 텍스트에 사용. 비우면 미리보기 생략.")]
    [SerializeField] private StarGrowthTable starGrowthTable;
    [Tooltip("필요 다이아 아이콘. 재화 카탈로그(CurrencyCatalog SO)에서 타입으로 조회해 자동 표시(선택).")]
    [SerializeField] private Image requiredDiaIcon;
    [SerializeField] private Button upgradeButton;
    [Tooltip("조각/만능조각 전환 팝업 열기(PLANNING 5-12).")]
    [SerializeField] private Button shardButton;
    [Tooltip("별 페이지에서 현재 선택 캐릭터 일러스트를 표시(추가 위치 1). 비우면 표시 안 함.")]
    [SerializeField] private CharacterIllustrationView starIllustrationView1;
    [Tooltip("별 페이지에서 현재 선택 캐릭터 일러스트를 표시(추가 위치 2). 비우면 표시 안 함.")]
    [SerializeField] private CharacterIllustrationView starIllustrationView2;
    [Tooltip("조각 일러스트 슬롯(ShardIllust). 현재 선택 캐릭터 일러스트를 표시. 비우면 표시 안 함.")]
    [SerializeField] private CharacterIllustrationView shardIllustrationView;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private MainHudContext _ctx;
    private int _charId;
    private CancellationToken _ct;
    private bool _busy;
    private bool _wired;   // 버튼/행 1회 구성 가드(내장 패널로 재사용되므로 중복 리스너 방지).

    // 조각/다이아 텍스트의 원래 색을 캐시한다. 최대 별레벨(승급 불가)일 때만 이 원래 색을 쓰고,
    // 그 외에는 충분하면 초록, 부족하면 빨강으로 표시한다.
    private Color _shardNormalColor = Color.white;
    private Color _diaNormalColor = Color.white;
    private Color _resetCostNormalColor = Color.white;
    private bool _starColorsCaptured;

    // 필요량 이상(충분)일 때 표시할 초록색.
    private static readonly Color SufficientColor = new Color(0.25f, 0.8f, 0.35f);

    private readonly List<GrowthStatRow> _rows = new();

    // 현재 보고 있는 페이지(0=스탯, 1=별). 캐릭터를 바꿔 재초기화해도 보던 페이지를 유지하기 위해 보존한다.
    private int _currentPage;

    public void Initialize(MainHudContext ctx, int charDataId) {
        _ctx = ctx;
        _charId = charDataId;
        _ct = this.GetCancellationTokenOnDestroy();

        // 리스너/행 구성은 최초 1회만. 재사용(재오픈) 시에는 표시 상태만 갱신한다.
        if (!_wired) {
            if (statPageButton != null) statPageButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); ShowPage(0); });
            if (starPageButton != null) starPageButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); ShowPage(1); });
            if (upgradeButton != null) upgradeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); UpgradeStarAsync().Forget(); });
            if (shardButton != null) shardButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenShardAsync().Forget(); });
            if (closeButton != null) closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            if (resetStatButton != null) resetStatButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); ResetStatsAsync().Forget(); });
            if (bulkAllocateButton != null) bulkAllocateButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); BulkAllocateAsync().Forget(); });
            BuildStatRows();
            BuildAutoStatDropdown();
            CaptureStarColors();

            // 다이아 보유량이 밖에서 바뀌어도(상점 구매/우편 수령 등) 필요 다이아 색과 승급/초기화 버튼이
            // 즉시 따라오게 한다. 이 팝업 위에 상점을 띄워 다이아를 사 온 경우가 여기에 해당한다.
            _ctx.Store.Diamond.Subscribe(_ => OnDiamondChanged()).AddTo(this);
            _wired = true;
        }

        // 필요 다이아 아이콘은 재화 카탈로그(SO)에서 타입으로 조회한다(재화 종류 고정이라 1회 설정).
        CurrencyIcon.Apply(requiredDiaIcon, _ctx.Currencies, CurrencyType.Diamond);
        CurrencyIcon.Apply(resetCostIcon, _ctx.Currencies, CurrencyType.Diamond);

        ApplySelectedIllustration();
        ShowPage(_currentPage);   // 캐릭터를 바꿔도 보던 페이지 유지(최초 오픈 시에는 기본 0=스탯).
        Refresh();
    }

    // 별 페이지의 일러스트 뷰들에 현재 선택 캐릭터 일러스트를 표시한다(선택 캐릭터가 바뀌면 재바인딩됨).
    private void ApplySelectedIllustration() {
        var appearance = _ctx.Appearance.Mine(_charId);
        var data = appearance.Character;
        BindIllustration(starIllustrationView1, appearance);
        BindIllustration(starIllustrationView2, appearance);
        BindIllustration(shardIllustrationView, appearance);
        // 조각 이름(캐릭터명 + " 조각"). 색은 바꾸지 않는다.
        if (shardNameText != null) shardNameText.text = data != null ? $"{data.DisplayName} 조각" : string.Empty;
    }

    private static void BindIllustration(CharacterIllustrationView view, CharacterAppearance appearance) {
        if (view == null) return;
        if (appearance.Character != null) view.Bind(appearance);
        else view.Clear();
    }

    private void BuildStatRows() {
        if (statRowPrefab == null || statRowParent == null) return;
        foreach (var stat in GameEnumRules.InvestableStats) {
            var row = Instantiate(statRowPrefab, statRowParent);
            var captured = stat;
            row.Setup(GrowthRules.StatName(stat), () => AllocateStatAsync(captured).Forget());
            if (TryPlusButtonColor(stat, out Color color)) row.SetPlusNormalColor(color);
            _rows.Add(row);
        }
    }

    // 지정하지 않은 스탯은 프리팹 색을 그대로 둔다(색을 정하지 않은 것과 검정을 칠하는 것은 다르다).
    private bool TryPlusButtonColor(StatType stat, out Color color) {
        if (plusButtonColors != null) {
            foreach (var entry in plusButtonColors) {
                if (entry == null || entry.stat != stat) continue;
                color = entry.normal;
                return true;
            }
        }
        color = Color.white;
        return false;
    }

    // 드롭다운 항목: [0]=미설정, [1..]=투자 가능 스탯(행 순서와 동일).
    // 항목을 코드가 채우므로 투자 가능 스탯이 늘어도 프리팹을 손댈 필요가 없다.
    private void BuildAutoStatDropdown() {
        if (autoStatDropdown == null) return;

        var options = new List<string> { AutoStatNoneLabel };
        foreach (var stat in GameEnumRules.InvestableStats) options.Add(GrowthRules.StatName(stat));
        autoStatDropdown.ClearOptions();
        autoStatDropdown.AddOptions(options);
        autoStatDropdown.onValueChanged.AddListener(OnAutoStatChanged);
    }

    private const string AutoStatNoneLabel = "미설정";

    // 드롭다운 인덱스 <-> StatType. 0 은 미설정이라 실제 스탯 목록보다 한 칸 앞선다.
    private static int AutoStatToIndex(int autoStatType) {
        var stats = GameEnumRules.InvestableStats;
        for (int i = 0; i < stats.Length; i++)
            if ((int)stats[i] == autoStatType) return i + 1;
        return 0;
    }

    private static StatType? IndexToAutoStat(int index) {
        var stats = GameEnumRules.InvestableStats;
        return index >= 1 && index - 1 < stats.Length ? stats[index - 1] : (StatType?)null;
    }

    private void ShowPage(int index) {
        _currentPage = index;
        if (statPanel != null) statPanel.SetActive(index == 0);
        if (starPanel != null) starPanel.SetActive(index == 1);
        // 현재 페이지 버튼을 비활성(선택 표시). 별도 하이라이트가 있으면 인스펙터에서 추가 가능.
        if (statPageButton != null) statPageButton.interactable = index != 0;
        if (starPageButton != null) starPageButton.interactable = index != 1;
    }

    // 보유 다이아만 바뀐 경우. 표시 값은 전부 스토어에서 다시 읽으므로 그대로 Refresh 를 태운다
    // (행을 다시 만들지 않아 스크롤/드롭다운 상태가 유지된다).
    private void OnDiamondChanged() {
        if (_ctx == null || _charId == 0 || !gameObject.activeInHierarchy) return;
        Refresh();
    }

    private void Refresh() {
        var p = _ctx.Store.GetProgress(_charId);
        if (p == null) return;

        // 스탯 페이지
        if (statPointsText != null) statPointsText.text = $"{p.StatPoints}";
        bool canInvest = p.StatPoints > 0;
        for (int i = 0; i < _rows.Count; i++) {
            var stat = GameEnumRules.InvestableStats[i];
            _rows[i].SetValue(p.Invested(stat), canInvest);
        }

        // 초기화: 되돌릴 포인트가 있고 다이아가 충분해야 누를 수 있다.
        int investedTotal = 0;
        foreach (var stat in GameEnumRules.InvestableStats) investedTotal += p.Invested(stat);
        bool enoughResetDia = _ctx.Store.Diamond.Value >= GrowthRules.StatResetDia;
        if (resetStatButton != null) resetStatButton.interactable = investedTotal > 0 && enoughResetDia;
        if (bulkAllocateButton != null) bulkAllocateButton.interactable = canInvest;
        if (resetCostText != null) {
            resetCostText.text = GrowthRules.StatResetDia.ToString("N0");
            resetCostText.color = enoughResetDia ? _resetCostNormalColor : Color.red;
        }

        // 자동 투자 드롭다운은 서버 값으로 맞추되 콜백을 다시 부르지 않는다(설정 요청이 되돌아 울리는 것 방지).
        if (autoStatDropdown != null)
            autoStatDropdown.SetValueWithoutNotify(AutoStatToIndex(p.AutoStatType));

        // 별 페이지
        int required = GrowthRules.RequiredShardsForStar(p.StarLevel);
        int diaRequired = GrowthRules.RequiredDiaForStar(p.StarLevel);
        int ownedDia = _ctx.Store.Diamond.Value;
        bool maxed = p.StarLevel >= GrowthRules.MaxStarLevel;

        // 현재/다음 별레벨 숫자 + 별 그리드(최대치면 다음도 현재와 동일).
        int nextStar = maxed ? p.StarLevel : p.StarLevel + 1;
        if (currentLevelText != null) currentLevelText.text = p.StarLevel.ToString();
        if (nextLevelText != null) nextLevelText.text = nextStar.ToString();
        if (currentStarView != null) currentStarView.SetStarLevel(p.StarLevel);
        if (nextStarView != null) nextStarView.SetStarLevel(nextStar);

        // 이번 승급 시 상승 능력치 미리보기(성장표에서 현재 별레벨의 델타를 읽어 요약).
        if (nextStarGrowthText != null) {
            nextStarGrowthText.text = maxed || starGrowthTable == null
                ? string.Empty
                : StatFormat.DeltaSummary(starGrowthTable.StepDelta(p.StarLevel));
        }

        // 조각 보유량/필요량을 한 텍스트에 "보유 / 필요"로 표시(충분하면 초록, 부족하면 빨강, 최대치면 원래 색).
        if (shardCountText != null) {
            shardCountText.text = maxed ? $"{p.Shards} / -" : $"{p.Shards} / {required}";
            shardCountText.color = maxed ? _shardNormalColor : (p.Shards >= required ? SufficientColor : Color.red);
        }

        // 현재 캐릭터 등급의 보유 만능조각 수(전환 등으로 변할 수 있어 매번 갱신).
        // 예: "SSR 만능조각\n3개". 만능조각이 없으면 조각 팝업 진입 버튼도 비활성화.
        var data = _ctx.CharDb.Get(_charId);
        int uni = (data != null && _ctx.Store.UniversalShards.TryGetValue((int)data.Grade, out var q)) ? q : 0;
        // 수량은 기본적으로 그 등급의 색으로 적는다. 색은 GradeColorCatalog 한 곳에서만 정해지므로
        // 다른 등급 표시와 어긋나지 않는다. 다만 이 버튼 배경에서는 읽히지 않는 등급색이 있어,
        // 이 표시에 한해 인스펙터에서 고정색으로 바꿀 수 있게 열어 둔다(카탈로그를 고치면 등급색을 쓰는
        // 다른 화면까지 함께 바뀌므로, 예외는 카탈로그가 아니라 쓰는 쪽에서 만든다).
        // 등급을 모르는 경우(캐릭터 자료가 없는 상태)에는 고를 색이 없으므로 색 없이 적는다.
        if (universalShardText != null) {
            if (data == null) {
                universalShardText.text = $"{uni}개";
            }
            else {
                string count = universalShardUseGradeColor
                    ? GradeText.Colorize($"{uni}개", data.Grade)
                    : $"<color=#{ColorUtility.ToHtmlStringRGB(universalShardCountColor)}>{uni}개</color>";
                universalShardText.text = $"{data.Grade} 만능조각\n{count}";
            }
        }
        if (shardButton != null) shardButton.interactable = uni > 0;

        // 필요 다이아 수(보유 다이아가 충분하면 초록, 부족하면 빨강, 최대치면 원래 색).
        if (requiredDiaText != null) {
            requiredDiaText.text = maxed ? "-" : diaRequired.ToString();
            requiredDiaText.color = maxed ? _diaNormalColor : (ownedDia >= diaRequired ? SufficientColor : Color.red);
        }

        // 승급 가능 조건: 최대치 아님 + 조각 충분 + 다이아 충분.
        bool canStarUp = !maxed && p.Shards >= required && ownedDia >= diaRequired;
        if (upgradeButton != null) upgradeButton.interactable = canStarUp;

        // 페이지 버튼 배지: 스탯 투자 가능 / 별 승급 가능(스프라이트는 SO 공용).
        SetBadge(statBadge, canInvest);
        SetBadge(starBadge, canStarUp);
    }

    private static void SetBadge(BadgeView badge, bool on) {
        if (badge != null) badge.SetOn(on);
    }

    /// <param name="count">한 번에 넣을 포인트 수. 일괄 투자에서만 1 보다 크다.</param>
    private async UniTask AllocateStatAsync(StatType stat, int count = 1) {
        if (_busy) return;
        _busy = true;
        try {
            var res = await _ctx.MainData.AllocateStatAsync(_charId, stat, count, _ct);
            if (res.IsSuccess) {
                _ctx.Store.ApplyCharacterSnapshot(res.Updated);
                Refresh();
            } else {
                _ctx.Ui.ShowToast(res.FailReason == 2 ? "스탯포인트가 부족합니다." : "투자할 수 없습니다.");
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("스탯 투자에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    /// <summary>
    /// 일괄 투자. 스탯과 수량을 고르는 팝업을 띄우고, 고른 대로 한 번의 요청으로 투자한다.
    ///
    /// 포인트가 많이 쌓였을 때 + 버튼을 반복해 누르는 것은 실기기에서 특히 힘들다. 한 번 누를 때마다
    /// 서버 왕복이 한 번씩 필요해, 연타를 해도 왕복 시간만큼만 들어가기 때문이다.
    /// </summary>
    private async UniTaskVoid BulkAllocateAsync() {
        if (_busy) return;

        var p = _ctx.Store.GetProgress(_charId);
        if (p == null || p.StatPoints <= 0) { _ctx.Ui.ShowToast("투자할 스탯포인트가 없습니다."); return; }

        var reference = _ctx.Popups.GrowthStatBulkPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<GrowthStatBulkPopup>(reference, _ct);
            var request = await popup.ShowAsync(p);
            if (request == null) return;   // 취소하거나 그냥 닫았다

            await AllocateStatAsync(request.Value.Stat, request.Value.Count);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("일괄 투자에 실패했습니다.");
        }
    }

    // 스탯 초기화: 되돌릴 양이 크고 다이아까지 드는 되돌릴 수 없는 조작이라 한 번 확인받는다.
    private async UniTaskVoid ResetStatsAsync() {
        if (_busy) return;
        try {
            var reference = _ctx.Popups.ConfirmCancelPopup;
            if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

            var confirm = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(reference, _ct);
            bool ok = await confirm.ShowAsync("스탯 초기화",
                $"다이아 {GrowthRules.StatResetDia:N0}을 사용해 투자한 스탯포인트를 모두 되돌립니다.\n자동 투자 설정도 함께 해제됩니다.");
            if (!ok) return;

            _busy = true;
            var res = await _ctx.MainData.ResetStatsAsync(_charId, _ct);
            if (res.IsSuccess) {
                _ctx.Store.ApplyCharacterSnapshot(res.Updated);
                _ctx.Store.Diamond.Value = res.NewDiamond;
                _ctx.Sound.PlaySfx(SfxKey.Purchase);
                Refresh();
            } else {
                _ctx.Ui.ShowToast(res.FailReason switch {
                    2 => "되돌릴 스탯포인트가 없습니다.",
                    4 => "다이아가 부족합니다.",
                    _ => "초기화할 수 없습니다."
                });
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("스탯 초기화에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    // 자동 투자 대상 변경. 실패하면 서버 값 기준으로 드롭다운을 되돌린다(화면과 서버가 어긋나지 않게).
    private void OnAutoStatChanged(int index) {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        SetAutoStatAsync(IndexToAutoStat(index)).Forget();
    }

    private async UniTaskVoid SetAutoStatAsync(StatType? stat) {
        try {
            var res = await _ctx.MainData.SetAutoStatAsync(_charId, stat, _ct);
            if (res.IsSuccess && res.Updated != null)
                _ctx.Store.ApplyAutoStatType(_charId, res.Updated.AutoStatType);
            else
                _ctx.Ui.ShowToast("자동 투자를 설정할 수 없습니다.");
        } catch (OperationCanceledException) {
            return;
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("자동 투자 설정에 실패했습니다.");
        }
        Refresh();
    }

    private async UniTaskVoid OpenShardAsync() {
        var popup = await _ctx.Ui.ShowPopupAsync<ShardPopup>(_ctx.Popups.ShardPopup, _ct);
        popup.Initialize(_ctx, _charId);
        await popup.ShowAsync();
        await popup.WaitUntilClosedAsync();
        Refresh();   // 조각 수량 변동 반영
    }

    // 조각/다이아 텍스트의 원래 색을 1회 캐시(부족 시 빨강에서 원래 색으로 복원하기 위함).
    private void CaptureStarColors() {
        if (_starColorsCaptured) return;
        if (shardCountText != null) _shardNormalColor = shardCountText.color;
        if (requiredDiaText != null) _diaNormalColor = requiredDiaText.color;
        if (resetCostText != null) _resetCostNormalColor = resetCostText.color;
        _starColorsCaptured = true;
    }

    // 별 승급은 오픈 기념 이벤트 누적 미션의 진행 지표(CharStarUpTotal)다. 달성되는 순간 메인 화면 이벤트 버튼에도
    // 불이 들어오도록 충족 상태만 가볍게 다시 받아온다(공지 목록/상점은 캐시를 그대로 쓴다).
    private void RefreshEventBadges() {
        if (_ctx != null && _ctx.Badges != null) _ctx.Badges.RefreshEventClaimStatesAsync(_ct).Forget();
    }

    private async UniTaskVoid UpgradeStarAsync() {
        if (_busy) return;
        _busy = true;
        try {
            var res = await _ctx.MainData.UpgradeStarAsync(_charId, _ct);
            if (res.IsSuccess) {
                _ctx.Store.ApplyCharacterSnapshot(res.Updated);
                _ctx.Store.Diamond.Value = res.NewDiamond;   // 승급에 소모된 다이아 반영(서버 권위 최신값)
                _ctx.Sound.PlaySfx(SfxKey.StarUp);
                Refresh();
                RefreshEventBadges();
            } else {
                _ctx.Ui.ShowToast(res.FailReason switch {
                    2 => "이미 최대 별레벨입니다.",
                    3 => "조각이 부족합니다.",
                    4 => "다이아가 부족합니다.",
                    _ => "승급할 수 없습니다."
                });
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("별레벨 승급에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }
}
