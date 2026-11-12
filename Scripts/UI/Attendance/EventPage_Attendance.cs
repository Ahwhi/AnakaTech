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
/// 출석체크 이벤트 페이지. EventDetailView 가 이벤트 키로 불러오는 자족형 페이지 프리팹의 스크립트다.
/// 생성되면 스스로 진행 정보를 조회해 일차 그리드(일자/상태/보상)를 구성하고 수령 버튼을 처리한다.
///
/// 같은 페이지가 두 종류를 맡는다. Event Key 를 비우면 월간 출석(IAttendanceService), 채우면 그 키의
/// 출석형 이벤트(IAttendanceEventService, 예: first_purchase_attendance)다. 출석형 이벤트는 참여 조건을 채우지 못하면
/// 잠김으로 내려오며, 이때 그리드는 전부 잠김 칸이 되고 Locked Group 이 켜진다.
///
/// 동적 생성 프리팹이라 EventDetailView 가 루트 스코프 컨테이너로 [Inject] 를 주입한다(출석/UI/배지 서비스).
/// 보상 아이콘/재화 재동기화에 필요한 메인 씬 의존성(MainHudContext)은 루트 스코프에 없으므로,
/// BadgeView 와 동일하게 현재 씬의 MainLifetimeScope 컨테이너에서 직접 해석한다(메인 씬에서만 열리므로 안전).
/// </summary>
public sealed class EventPage_Attendance : MonoBehaviour {
    [Header("Source")]
    [Tooltip("비우면 월간 출석. 출석형 이벤트 페이지면 서버 이벤트 키를 넣는다(예: first_purchase_attendance).")]
    [SerializeField] private string eventKey = "";

    [Header("Grid")]
    [Tooltip("일자 칸이 생성될 부모(Grid Layout Group 권장).")]
    [SerializeField] private Transform gridRoot;
    [SerializeField] private AttendanceDayCell cellPrefab;

    [Header("Claim")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TextMeshProUGUI claimButtonText;
    [Tooltip("잠겼을 때 수령 버튼 문구(출석형 이벤트 전용).")]
    [SerializeField] private string lockedButtonLabel = "결제 후 오픈";

    [Header("Locked (출석형 이벤트 전용, 선택)")]
    [Tooltip("참여 조건을 채우지 못했을 때 켜지는 묶음(잠금 오버레이/안내 문구 등). 꺼진 채로 둔다.")]
    [SerializeField] private GameObject lockedGroup;

    [Header("States (선택)")]
    [SerializeField] private GameObject loadingGroup;
    [SerializeField] private RectTransform loadingSpinner;
    [SerializeField] private float spinSpeed = 220f;
    [SerializeField] private GameObject errorLabel;

    private IUIService _ui;
    private IAttendanceService _attendance;
    private IAttendanceEventService _attendanceEvent;
    private IEventBadgeService _badges;
    private ISoundService _sound;
    private MainHudContext _hud;   // 보상 아이콘/재동기화용(메인 씬 스코프에서 해석).

    private readonly List<AttendanceDayCell> _cells = new();
    private readonly Dictionary<int, AttendanceReward> _rewardByDay = new();
    private bool _loading;
    private bool _locked;
    private int _claimedDays;

    [Inject]
    public void Construct(IUIService ui, IAttendanceService attendance, IAttendanceEventService attendanceEvent,
                          IEventBadgeService badges, ISoundService sound) {
        _ui = ui;
        _attendance = attendance;
        _attendanceEvent = attendanceEvent;
        _badges = badges;
        _sound = sound;
    }

    private void Awake() {
        if (claimButton != null)
            claimButton.onClick.AddListener(() => ClaimAsync(this.GetCancellationTokenOnDestroy()).Forget());
    }

    private void Start() {
        EnsureHud();
        LoadAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void Update() {
        if (_loading && loadingSpinner != null)
            loadingSpinner.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
    }

    // 메인 씬 스코프에서 MainHudContext 를 해석한다(보상 아이콘/재화 재동기화용). 없으면 아이콘/재동기화만 생략.
    private void EnsureHud() {
        if (_hud != null) return;
        var scope = FindAnyObjectByType<MainLifetimeScope>();
        if (scope == null || scope.Container == null) return;
        try { _hud = scope.Container.Resolve<MainHudContext>(); } catch { _hud = null; }
    }

    // 진행 정보 조회 -> 그리드 구성. 의존성 주입 전이면(예외 상황) 안전하게 빠진다.
    private async UniTaskVoid LoadAsync(CancellationToken ct) {
        if (!HasSource) return;

        SetState(loading: true);
        try {
            var info = await RequestInfoAsync(ct);
            SetInfo(info);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogError($"[Attendance] 정보 조회 실패: {e.Message}");
            ShowError();
        }
    }

    private async UniTaskVoid ClaimAsync(CancellationToken ct) {
        if (!HasSource) return;

        try {
            var result = await RequestClaimAsync(ct);
            if (result.Success) {
                ApplyResult(result);
                await ResyncAsync(result, ct);   // 지급 재화/조각/티켓을 HUD(스토어)에 즉시 반영.
                _sound?.PlaySfx(SfxKey.Claim);
                _ui.ShowToast("출석 완료!");
                // 수령으로 충족 상태가 바뀌었으니 이벤트 버튼 알림등을 갱신한다.
                _badges.RefreshAsync(ct).Forget();
            } else {
                _ui.ShowToast(FailMessage(result.FailReason));
            }
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogError($"[Attendance] 수령 실패: {e.Message}");
            _ui.ShowToast("출석 처리에 실패했습니다.");
        }
    }

    private void SetInfo(AttendanceInfo info) {
        SetState();
        _rewardByDay.Clear();
        if (info.Rewards != null)
            foreach (var r in info.Rewards)
                if (!_rewardByDay.ContainsKey(r.Day)) _rewardByDay[r.Day] = r;   // 일차당 첫 보상만 칸에 표시.

        _locked = info.Locked;
        if (lockedGroup != null) lockedGroup.SetActive(_locked);
        BuildGrid(info.MaxDays, info.ClaimedDays, info.CanClaimToday);
        SetClaimButton(info.CanClaimToday, info.ClaimedDays, info.MaxDays);
    }

    private void ApplyResult(AttendanceClaimResult result) {
        int maxDays = _cells.Count > 0 ? _cells.Count : result.ClaimedDays;
        BuildGrid(maxDays, result.ClaimedDays, result.CanClaimToday);
        SetClaimButton(result.CanClaimToday, result.ClaimedDays, maxDays);
    }

    private void ShowError() {
        ClearCells();
        SetState(error: true);
        SetClaimButton(false, 0, 0);
    }

    // 이벤트 키 유무로 통신 창구를 고른다. 페이지의 나머지(그리드/수령/재동기화)는 어느 쪽이든 같다.
    private bool IsAttendanceEvent => !string.IsNullOrEmpty(eventKey);
    private bool HasSource => IsAttendanceEvent ? _attendanceEvent != null : _attendance != null;

    private UniTask<AttendanceInfo> RequestInfoAsync(CancellationToken ct) =>
        IsAttendanceEvent ? _attendanceEvent.GetInfoAsync(eventKey, ct) : _attendance.GetInfoAsync(ct);

    private UniTask<AttendanceClaimResult> RequestClaimAsync(CancellationToken ct) =>
        IsAttendanceEvent ? _attendanceEvent.ClaimAsync(eventKey, ct) : _attendance.ClaimAsync(ct);

    // 월간 출석의 실패(2=수령 불가)와 출석형 이벤트의 실패(2=잠김, 3=오늘 이미/모두 받음)는 번호가 다르다.
    private string FailMessage(int fail) {
        if (!IsAttendanceEvent) return "오늘은 이미 출석했습니다.";
        return fail switch {
            2 => "아직 참여할 수 없습니다.",
            3 => _cells.Count > 0 && _cells.Count <= _claimedDays ? "모든 보상을 받았습니다." : "오늘은 이미 출석했습니다.",
            _ => "출석 처리에 실패했습니다."
        };
    }

    private void BuildGrid(int maxDays, int claimedDays, bool canClaimToday) {
        ClearCells();
        for (int day = 1; day <= maxDays; day++) {
            AttendanceDayCell.CellState state;
            if (day <= claimedDays) state = AttendanceDayCell.CellState.Claimed;
            else if (day == claimedDays + 1 && canClaimToday) state = AttendanceDayCell.CellState.Claimable;
            else state = AttendanceDayCell.CellState.Locked;

            Sprite icon = null;
            string qty = "";
            string rewardName = null;
            if (_rewardByDay.TryGetValue(day, out var reward)) {
                // 보상 해석은 상점/우편/도감과 같은 경로를 쓴다(품목이 늘어도 여기를 고칠 일이 없다).
                var visual = RewardVisualResolver.Resolve(_hud, RewardItem.FromGrant(reward.Kind, reward.Value, reward.Qty, reward.CurrencyKey));
                icon = visual.Icon;
                qty = visual.Quantity;
                rewardName = visual.Name;
            }

            var cell = Instantiate(cellPrefab, gridRoot);
            cell.Set(day, state, icon, qty, rewardName);
            _cells.Add(cell);
        }
    }

    // 지급된 재화/조각/티켓/소모품을 HUD 스토어에 반영(우편함 수령과 동일 방식).
    // 지갑(골드/다이아/기력)은 응답에 잔액이 실려 오지만, 그 밖의 자산은 종류별로 다시 조회해야 한다.
    private async UniTask ResyncAsync(AttendanceClaimResult result, CancellationToken ct) {
        if (_hud == null || _hud.Store == null) return;
        _hud.Store.Gold.Value = result.NewGold;
        _hud.Store.Diamond.Value = result.NewDiamond;
        _hud.Store.Stamina.Value = result.NewStamina;

        // 원시 kind 숫자로 갈라 두면 보상 종류가 늘 때마다 조용히 빠진다(이벤트 미션 쪽에서 실제로 그랬다).
        // 표시 경로와 같은 타입으로 바꿔 판단한다. 지금 출석에 장비 보상은 없지만 넣어도 그대로 동작한다.
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

        if (needInventory && _hud.Inventory != null) {
            var inv = await _hud.Inventory.RequestInventoryAsync(ct);
            _hud.Store.ApplyInventory(inv);
        }
        if (needCurrency && _hud.Shop != null) {
            var catalog = await _hud.Shop.RequestShopAsync(ct);
            _hud.Store.ApplyShopCurrencies(catalog.Currencies);
        }
        if (needItem && _hud.Item != null) {
            var items = await _hud.Item.RequestInfoAsync(ct);
            _hud.Store.ApplyItems(items);
        }
    }

    private void SetClaimButton(bool canClaim, int claimedDays, int maxDays) {
        _claimedDays = claimedDays;
        if (claimButton != null) claimButton.interactable = canClaim;
        if (claimButtonText == null) return;
        // 월간 출석은 달이 바뀌면 다시 받으므로 "모두 받음"을 쓰지 않는다(오늘 완료로 충분).
        claimButtonText.text = _locked ? lockedButtonLabel
                             : canClaim ? "출석하기"
                             : IsAttendanceEvent && maxDays > 0 && claimedDays >= maxDays ? "모두 받음"
                             : "오늘 완료";
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
    }
}
