using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 미션형 이벤트 페이지. EventDetailView 가 이벤트 키로 불러오는 자족형 페이지 프리팹의 스크립트다(출석 페이지와 동일 구조).
/// 생성되면 스스로 상태를 조회해 미션 목록을 구성하고, 각 미션의 진행/보상/수령을 처리한다.
///
/// 어떤 이벤트를 그릴지는 프리팹의 eventKey 로만 정해진다. 오픈 기념 이벤트처럼 일일/누적 두 묶음이 있는 이벤트는
/// 탭 두 개로, 미소녀 수집/스테이지 진행/다이아 환급처럼 한 묶음뿐인 이벤트는 목록 하나로 자동 표시된다
/// (탭/패널을 비워 두면 단일 목록 페이지가 된다). 따라서 새 미션 이벤트는 프리팹 복제 + 키 변경으로 끝난다.
///
/// 이벤트 페이지는 EventDetailView 가 루트 스코프 컨테이너로 [Inject] 를 주입한다(이벤트/UI/배지 서비스는 루트 등록).
/// 보상 아이콘/재화 재동기화에 필요한 MainHudContext 와 스테이지 이름용 DungeonDatabase 는 루트에 없으므로
/// 현재 씬의 MainLifetimeScope 에서 직접 해석한다.
/// </summary>
public sealed class EventPage_Missions : MonoBehaviour {
    [Header("Event")]
    [Tooltip("이 페이지가 보여줄 이벤트 키(서버 EventKeys / 공지 DB 의 reward_key 와 같은 값). " +
             "open_event = 오픈 기념, collect_girls = 모아라! 미소녀, kotoha_motivation = 코토하의 동기 부여, dia_refund = 다이아 환급.")]
    [SerializeField] private string eventKey = EventMissionKeys.OpenEvent;
    [Tooltip("미션 기본 문구에서 캐릭터를 부르는 말(예: 소녀 모집 10회 진행). 비우면 \"소녀\".")]
    [SerializeField] private string characterNoun = "";

    [Header("Missions")]
    [Tooltip("일일 미션 칸이 생성될 부모.")]
    [SerializeField] private Transform dailyRoot;
    [Tooltip("누적 미션 칸이 생성될 부모.")]
    [SerializeField] private Transform cumulativeRoot;
    [SerializeField] private EventMissionCell cellPrefab;

    [Header("Tabs")]
    [Tooltip("일일 미션 탭 버튼.")]
    [SerializeField] private MissionTabButton dailyTab;
    [Tooltip("누적 미션 탭 버튼.")]
    [SerializeField] private MissionTabButton cumulativeTab;
    [Tooltip("일일 미션 패널(탭 선택 시 켜짐). 비우면 dailyRoot 를 사용한다.")]
    [SerializeField] private GameObject dailyPanel;
    [Tooltip("누적 미션 패널(탭 선택 시 켜짐). 비우면 cumulativeRoot 를 사용한다.")]
    [SerializeField] private GameObject cumulativePanel;

    [Tooltip("체크하면 탭 없이 일일 미션과 누적 미션을 한 화면에 함께 보여 준다(위의 탭 버튼은 쓰지 않는다). " +
             "7일 미션이 완주 보상을 항상 보여 주는 것과 같은 배치로, 일일 미션 + 기간 누적 보상 이벤트(가을 맞이 등)용이다. " +
             "기본은 끔이라 기존 페이지는 그대로 탭으로 동작한다.")]
    [SerializeField] private bool showAllTogether;

    [Header("Day Tabs (7일 미션 등 일차별 이벤트 전용. 비우면 위의 일일/누적 구성을 쓴다)")]
    [Tooltip("일차 탭 버튼들. 0번이 1일차다. 하나라도 넣으면 이 페이지는 일차 탭 모드로 동작한다.")]
    [SerializeField] private MissionTabButton[] dayTabs;
    [Tooltip("선택한 일차의 미션 칸이 생성될 부모. 탭을 바꾸면 이 자리만 다시 그린다.")]
    [SerializeField] private Transform dayRoot;
    [Tooltip("진행 현황 표시(예: 12 / 35). 전체 완주 보상은 분모에서 빠진다. 선택.")]
    [SerializeField] private TMP_Text totalProgressText;
    [Tooltip("아직 열리지 않은 일차를 눌렀을 때 띄울 문구.")]
    [SerializeField] private string lockedDayMessage = "아직 할 수 없는 미션입니다.";

    [Header("Scroll")]
    [Tooltip("체크하면 페이지를 처음 열 때 마지막으로 수령한 미션이 목록 맨 위에 오도록 스크롤한다. " +
             "코토하의 수련처럼 순서대로 받아 나가는 긴 목록용이다. 미션 칸 부모의 상위에 ScrollRect 가 있어야 한다.")]
    [SerializeField] private bool scrollToLastClaimed;

    [Header("States (선택)")]
    [SerializeField] private GameObject loadingGroup;
    [SerializeField] private RectTransform loadingSpinner;
    [SerializeField] private float spinSpeed = 220f;
    [SerializeField] private GameObject errorLabel;

    private IUIService _ui;
    private IEventMissionService _missionService;
    private IEventBadgeService _badges;
    private ISoundService _sound;
    private IEventRouletteService _roulette;
    private MainHudContext _hud;
    private DungeonDatabase _dungeonDb;

    private readonly List<EventMissionCell> _cells = new();
    private EventMissionCell _lastClaimedCell;   // 마지막으로 그린 칸 중 수령을 마친 것(처음 열 때 스크롤 기준)
    private bool _initialScrollDone;
    private bool _loading;
    private bool _busy;
    private bool _showDaily = true;   // 현재 선택된 탭(기본: 일일).

    // 일차 탭 모드 상태. 탭을 하나라도 배치하면 켜진다.
    private bool DayTabMode => dayTabs != null && dayTabs.Length > 0;
    private int _selectedDay = 1;
    private int _unlockedDay;
    private EventMissionStatus _status;

    [Inject]
    public void Construct(IUIService ui, IEventMissionService openEvent, IEventBadgeService badges, ISoundService sound,
                          IEventRouletteService roulette) {
        _ui = ui;
        _missionService = openEvent;
        _badges = badges;
        _sound = sound;
        _roulette = roulette;
    }

    private void Awake() {
        if (dailyTab != null) dailyTab.Init(() => SelectTab(daily: true));
        if (cumulativeTab != null) cumulativeTab.Init(() => SelectTab(daily: false));

        if (dayTabs != null)
            for (int i = 0; i < dayTabs.Length; i++) {
                int day = i + 1;   // 람다가 루프 변수를 잡지 않도록 복사한다.
                if (dayTabs[i] != null) dayTabs[i].Init(() => SelectDay(day));
            }
    }

    private void Start() {
        EnsureSceneDependencies();
        ApplyTabSelection();   // 데이터 도착 전에도 기본 탭이 보이도록 초기 상태를 맞춘다.
        LoadAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void Update() {
        if (_loading && loadingSpinner != null)
            loadingSpinner.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
    }

    // 씬 스코프에만 있는 의존(HUD/던전 데이터)을 한 번 해석한다. 없으면(다른 씬에서 열렸다면) 각 기능이 조용히 비활성된다.
    private void EnsureSceneDependencies() {
        if (_hud != null && _dungeonDb != null) return;
        var scope = FindAnyObjectByType<MainLifetimeScope>();
        if (scope == null || scope.Container == null) return;
        try { _hud = scope.Container.Resolve<MainHudContext>(); } catch { _hud = null; }
        try { _dungeonDb = scope.Container.Resolve<DungeonDatabase>(); } catch { _dungeonDb = null; }
    }

    private async UniTaskVoid LoadAsync(CancellationToken ct) {
        if (_missionService == null) return;

        SetState(loading: true);
        try {
            var status = await _missionService.GetStatusAsync(eventKey, ct);
            SetStatus(status);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogError($"[EventMission] 상태 조회 실패: {e.Message}");
            ShowError();
        }
    }

    private void SetStatus(EventMissionStatus status) {
        SetState();
        _status = status;

        UpdateTotalProgress(status);

        if (DayTabMode) {
            _unlockedDay = status.UnlockedDay;
            SelectDefaultDay(status.Missions);
            BuildMissions(status.Missions);
            UpdateDayTabs(status.Missions);
            return;
        }

        BuildMissions(status.Missions);
        UpdateTabBadges(status.Missions);
        SelectDefaultTab(status.Missions);
        ApplyTabSelection();
        ScrollToLastClaimedOnce();
    }

    // 처음 열 때 한 번만 한다. 수령 후 다시 그릴 때마다 위치를 옮기면 보던 자리가 튀어 버린다.
    private void ScrollToLastClaimedOnce() {
        if (_initialScrollDone) return;
        _initialScrollDone = true;
        if (!scrollToLastClaimed || _lastClaimedCell == null) return;
        ScrollToCellAsync(_lastClaimedCell, this.GetCancellationTokenOnDestroy()).Forget();
    }

    // 막 생성한 칸은 이번 프레임에 아직 크기가 정해지지 않았을 수 있어 레이아웃이 한 번 돈 뒤에 맞춘다.
    private static async UniTaskVoid ScrollToCellAsync(EventMissionCell cell, CancellationToken ct) {
        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, ct);
        if (cell == null) return;
        var scroll = cell.GetComponentInParent<ScrollRect>();
        if (scroll != null) scroll.ScrollTo((RectTransform)cell.transform, 0f);
    }

    /// <summary>
    /// 진행 현황 표시. 기간 누적 목표(일일 미션 n회 완료)가 있는 이벤트는 그 목표의 진행(예: 37 / 90)을,
    /// 없는 이벤트는 기존대로 달성한 미션 수 / 전체 미션 수(7일 미션 등)를 적는다.
    /// 누적 목표 이벤트에서 미션 개수를 적으면 일일 미션 몇 개짜리 이벤트인지만 보여 주게 되어 의미가 없다.
    /// </summary>
    private void UpdateTotalProgress(EventMissionStatus status) {
        if (totalProgressText == null) return;

        if (status.Missions != null)
            foreach (var m in status.Missions) {
                if ((EventMissionMetric)m.Metric != EventMissionMetric.DailyMissionClaimed) continue;
                totalProgressText.text = $"{Mathf.Min(m.Progress, m.Threshold):N0} / {m.Threshold:N0}";
                return;
            }

        if (status.TotalMissions > 0)
            totalProgressText.text = $"{status.AchievedMissions} / {status.TotalMissions}";
    }

    // 처음 열 때는 아직 할 일이 남은 가장 이른 일차를 보여준다. 전부 끝났으면 열린 마지막 일차에 머문다
    // (매번 1일차로 돌아가면 후반부 유저가 매번 탭을 눌러 이동해야 한다).
    private void SelectDefaultDay(IReadOnlyList<EventMission> missions) {
        int lastUnlocked = Mathf.Clamp(_unlockedDay, 1, Mathf.Max(1, dayTabs.Length));
        _selectedDay = lastUnlocked;

        if (missions == null) return;
        for (int day = 1; day <= lastUnlocked; day++) {
            foreach (var m in missions)
                if (m.Group == day && m.Claimable) { _selectedDay = day; return; }
        }
    }

    // 일차 탭 선택. 아직 열리지 않은 일차는 선택되지 않고 안내만 띄운다.
    private void SelectDay(int day) {
        if (day > _unlockedDay) {
            _ui?.ShowToast(lockedDayMessage);
            return;
        }
        if (_selectedDay == day) return;
        _selectedDay = day;
        BuildMissions(_status.Missions);
        UpdateDayTabs(_status.Missions);
    }

    // 일차 탭의 선택 표시와 배지. 잠긴 일차는 배지를 켜지 않는다(받을 수 있는 것이 없다).
    private void UpdateDayTabs(IReadOnlyList<EventMission> missions) {
        for (int i = 0; i < dayTabs.Length; i++) {
            var tab = dayTabs[i];
            if (tab == null) continue;
            int day = i + 1;

            tab.SetSelected(day == _selectedDay);

            bool claimable = false;
            if (missions != null && day <= _unlockedDay)
                foreach (var m in missions)
                    if (m.Group == day && m.Claimable) { claimable = true; break; }
            tab.SetBadge(claimable);
        }
    }

    // 일일 미션이 없는 이벤트(단일 목록)는 누적 쪽을 보여준다. 기본값(일일)을 그대로 두면 빈 화면이 된다.
    private void SelectDefaultTab(IReadOnlyList<EventMission> missions) {
        if (missions == null) return;

        bool hasDaily = false;
        foreach (var m in missions)
            if (m.Daily) { hasDaily = true; break; }

        if (!hasDaily) _showDaily = false;
    }

    // 탭 전환: 선택 상태만 바꾸고 패널 표시는 ApplyTabSelection 이 담당한다.
    private void SelectTab(bool daily) {
        if (showAllTogether) return;   // 한 화면 모드에는 탭이 없다(버튼이 배선돼 있어도 무시한다).
        if (_showDaily == daily) return;
        _showDaily = daily;
        ApplyTabSelection();
    }

    // 현재 선택(_showDaily)에 맞춰 패널 ON/OFF 와 탭 선택 표시를 반영한다.
    // 패널을 따로 지정하지 않았으면 미션 칸 부모(dailyRoot/cumulativeRoot)를 패널로 쓴다.
    private void ApplyTabSelection() {
        var daily = dailyPanel != null ? dailyPanel : (dailyRoot != null ? dailyRoot.gameObject : null);
        var cumulative = cumulativePanel != null ? cumulativePanel : (cumulativeRoot != null ? cumulativeRoot.gameObject : null);

        // 일차 탭 모드에는 일일/누적 탭이 없다. 누적 자리는 "어느 일차에도 속하지 않는 미션"(전체 완주 보상)의
        // 상시 노출 자리이므로 일차 선택과 무관하게 항상 켜 두고, 쓰지 않는 일일 자리는 끈다.
        // (탭 전환 규칙을 그대로 태우면 켤 사람이 없어 그 미션이 화면에 영영 나타나지 않는다.)
        if (DayTabMode) {
            if (daily != null) daily.SetActive(false);
            if (cumulative != null) cumulative.SetActive(true);
            return;
        }

        // 한 화면 모드: 두 자리를 모두 켜 둔다. 누적 보상이 일차 탭 모드의 완주 보상처럼 항상 보인다.
        if (showAllTogether) {
            if (daily != null) daily.SetActive(true);
            if (cumulative != null) cumulative.SetActive(true);
            return;
        }

        if (daily != null) daily.SetActive(_showDaily);
        if (cumulative != null) cumulative.SetActive(!_showDaily);

        if (dailyTab != null) dailyTab.SetSelected(_showDaily);
        if (cumulativeTab != null) cumulativeTab.SetSelected(!_showDaily);
    }

    // 카테고리별로 수령 가능한 미션이 하나라도 있으면 해당 탭 배지를 켠다.
    private void UpdateTabBadges(IReadOnlyList<EventMission> missions) {
        bool dailyClaimable = false, cumulativeClaimable = false;
        if (missions != null)
            foreach (var m in missions) {
                if (!m.Claimable) continue;
                if (m.Daily) dailyClaimable = true;
                else cumulativeClaimable = true;
            }

        if (dailyTab != null) dailyTab.SetBadge(dailyClaimable);
        if (cumulativeTab != null) cumulativeTab.SetBadge(cumulativeClaimable);
    }

    private void BuildMissions(IReadOnlyList<EventMission> missions) {
        ClearCells();
        if (missions == null || cellPrefab == null) return;

        foreach (var m in missions) {
            // 일차 탭 모드: 선택한 일차의 미션만 그리고, 일차에 속하지 않는 미션(전체 완주 보상 등)은
            // 항상 보이는 자리(cumulativeRoot)에 둔다. 그 자리를 배선하지 않은 페이지에서는 지금 보고 있는
            // 일차 자리에 함께 그린다. 자리가 없다고 칸을 빼면 그 미션은 화면에 영영 나타나지 않고,
            // 서버는 수령 가능으로 보고 있으므로 이벤트 배지가 꺼지지 않는다(7일 미션의 완주 보상이 그랬다).
            Transform parent;
            if (DayTabMode) {
                if (m.Group == 0) parent = cumulativeRoot != null ? cumulativeRoot : dayRoot;
                else if (m.Group == _selectedDay) parent = dayRoot;
                else continue;
            } else {
                parent = m.Daily ? dailyRoot : cumulativeRoot;
            }
            if (parent == null) continue;

            var cell = Instantiate(cellPrefab, parent);
            // 보상 해석은 상점/우편/도감과 같은 경로를 쓴다(품목이 늘어도 여기를 고칠 일이 없다).
            var visual = RewardVisualResolver.Resolve(_hud, RewardItem.FromGrant(m.Reward.Kind, m.Reward.Value, m.Reward.Qty, m.Reward.CurrencyKey));
            cell.Bind(m, visual.Icon, visual.Quantity, OnClaim, visual.Name, TitleFor(m, missions), characterNoun);
            _cells.Add(cell);
            if (m.Claimed) _lastClaimedCell = cell;
        }
    }

    // 칸이 스스로 만들 수 없는 문구만 여기서 만든다(스테이지 이름, 묶음 크기처럼 목록 전체를 봐야 아는 것).
    // 그 외는 null 로 두어 칸의 기본 문구를 쓴다.
    private string TitleFor(EventMission m, IReadOnlyList<EventMission> missions) {
        // 묶음 보상이 묶음 전체가 아니라 일부(n개)만 요구하면 "모든 미션 완료"가 틀린 말이 되므로 개수로 적는다.
        if ((EventMissionMetric)m.Metric == EventMissionMetric.MissionGroupCleared) {
            int groupSize = 0;
            foreach (var x in missions) {
                var metric = (EventMissionMetric)x.Metric;
                if (x.Group == m.Param && metric != EventMissionMetric.MissionGroupCleared
                    && metric != EventMissionMetric.MissionSetCleared) groupSize++;
            }
            return m.Threshold < groupSize ? $"미션 {m.Threshold:N0}개 완료" : null;
        }

        if ((EventMissionMetric)m.Metric != EventMissionMetric.StageCleared) return null;

        var dungeon = _dungeonDb != null ? _dungeonDb.Get(m.Param) : null;
        return dungeon != null ? $"스테이지 {dungeon.DisplayName} 클리어" : null;
    }

    private void OnClaim(int missionId) {
        if (_busy) return;
        ClaimAsync(missionId, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid ClaimAsync(int missionId, CancellationToken ct) {
        _busy = true;
        try {
            var result = await _missionService.ClaimAsync(missionId, ct);
            if (!result.Success) {
                _ui.ShowToast(FailMessage(result.FailReason));
                return;
            }

            await ResyncAsync(result, ct);
            _sound?.PlaySfx(SfxKey.Claim);
            _ui.ShowToast("보상을 받았습니다.");
            _badges.RefreshAsync(ct).Forget();

            // 수령으로 상태가 바뀌었으니 목록을 다시 받아 갱신한다.
            var status = await _missionService.GetStatusAsync(eventKey, ct);
            SetStatus(status);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogError($"[EventMission] 수령 실패: {e.Message}");
            _ui.ShowToast("수령에 실패했습니다.");
        } finally {
            _busy = false;
        }
    }

    // 지급된 재화/조각/티켓을 HUD 스토어에 반영(우편함/출석 수령과 동일 방식).
    private async UniTask ResyncAsync(EventMissionClaimResult result, CancellationToken ct) {
        // 이벤트 전용 재화(돌림판 다트 등)는 HUD 스토어가 아니라 돌림판 서비스가 보유량을 갖는다.
        // HUD 가 없는 씬에서도 맞춰야 하므로 아래 HUD 확인보다 먼저 처리한다.
        if (result.Granted != null && _roulette != null)
            foreach (var g in result.Granted) {
                var item = RewardItem.FromGrant(g.Kind, g.Value, g.Qty, g.CurrencyKey);
                if (item.Kind == RewardItemKind.EventToken) _roulette.RefreshAsync(item.CurrencyKey, ct).Forget();
            }

        if (_hud == null || _hud.Store == null) return;
        _hud.Store.Gold.Value = result.NewGold;
        _hud.Store.Diamond.Value = result.NewDiamond;
        _hud.Store.Stamina.Value = result.NewStamina;

        // 원시 kind 숫자로 갈라 두면 보상 종류가 늘 때마다 조용히 빠진다(랜덤 장비가 실제로 그래서
        // 수령해도 재접속 전까지 가방에 보이지 않았다). 표시 경로와 같은 타입으로 바꿔 판단한다.
        bool needInventory = false, needCurrency = false, needItem = false;
        if (result.Granted != null)
            foreach (var g in result.Granted) {
                switch (RewardItem.FromGrant(g.Kind, g.Value, g.Qty, g.CurrencyKey).Kind) {
                    case RewardItemKind.UniversalShard:
                    case RewardItemKind.RandomUniversalShard:
                    case RewardItemKind.RandomEquip:
                    case RewardItemKind.RandomGeneralEquip:
                        needInventory = true; break;      // 조각/장비 -> 인벤토리
                    case RewardItemKind.Currency:
                        needCurrency = true; break;       // 티켓/명성/각인석 -> 상점 재화
                    case RewardItemKind.Item:
                        needItem = true; break;           // 소모품(경험치 쿠폰 등) -> 아이템 가방
                }
            }

        if (needItem && _hud.Item != null) {
            var items = await _hud.Item.RequestInfoAsync(ct);
            _hud.Store.ApplyItems(items);
        }
        if (needInventory && _hud.Inventory != null) {
            var inv = await _hud.Inventory.RequestInventoryAsync(ct);
            _hud.Store.ApplyInventory(inv);
        }
        if (needCurrency && _hud.Shop != null) {
            var catalog = await _hud.Shop.RequestShopAsync(ct);
            _hud.Store.ApplyShopCurrencies(catalog.Currencies);
        }
    }

    private static string FailMessage(int fail) => fail switch {
        2 => "아직 달성하지 못했습니다.",
        3 => "이미 받았습니다.",
        4 => "이벤트가 종료되었습니다.",
        _ => "수령할 수 없습니다."
    };

    private void ShowError() {
        ClearCells();
        SetState(error: true);
    }

    private void SetState(bool loading = false, bool error = false) {
        _loading = loading;
        if (loadingGroup != null) loadingGroup.SetActive(loading);
        if (errorLabel != null) errorLabel.SetActive(error);
    }

    private void ClearCells() {
        foreach (var cell in _cells)
            if (cell != null) Destroy(cell.gameObject);
        _cells.Clear();
        _lastClaimedCell = null;
    }
}
