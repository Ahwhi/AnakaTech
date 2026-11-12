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
/// 길드 레이드 패널(길드 팝업의 컨텐츠 탭). 보스 현황과 내 참여 횟수를 보여 주고 레이드 씬으로 보낸다.
///
/// 참여 횟수는 서버가 입장 시점에 먼저 깎으므로, 성공 응답을 받으면 반드시 씬으로 넘어가야 한다
/// (여기서 중간에 멈추면 유저가 횟수만 잃는다).
/// 보스 체력은 길드원 전원이 함께 깎는 값이라 패널이 열려 있는 동안 주기적으로 다시 받아온다.
/// </summary>
public sealed class GuildRaidPanel : MonoBehaviour {
    /// <summary>현황 자동 갱신 주기(초). 다른 길드원이 깎은 체력이 화면에 따라오도록 한다.</summary>
    private const float PollInterval = 10f;

    [Header("Boss")]
    [SerializeField] private TMP_Text bossLevelText;
    [Tooltip("보스 체력. n / n 형태로 표시한다.")]
    [SerializeField] private TMP_Text bossHpText;
    [Tooltip("보스 체력 비율(백분율).")]
    [SerializeField] private TMP_Text bossHpPercentText;
    [SerializeField] private Slider bossHpSlider;
    [Tooltip("초기화까지 남은 시간(hh:mm:ss).")]
    [SerializeField] private TMP_Text resetTimerText;

    [Header("Entry")]
    [Tooltip("참여 가능 횟수. n / n 형태.")]
    [SerializeField] private TMP_Text entryCountText;
    [SerializeField] private Button enterButton;
    [Tooltip("입장 버튼 문구(참여 / 추가 참여 / 오늘 참여 완료). 비용은 costText 가 따로 보여 준다.")]
    [SerializeField] private TMP_Text enterButtonText;
    [Tooltip("유료 참여일 때만 켜는 비용 숫자. 무료로 참여할 수 있을 때는 아이콘과 함께 꺼진다.")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("유료 참여일 때 켜는 비용 재화 아이콘(선택). 스프라이트는 재화 카탈로그(CurrencyCatalog)에서 타입으로 가져오므로 직접 할당하지 않는다.")]
    [SerializeField] private Image costIcon;
    [Tooltip("무료로 참여할 수 있을 때 켜는 그룹(예: '무료' 표시). 비용 표시(costText/costIcon)와 서로 배타적이다.")]
    [SerializeField] private GameObject costFreeGroup;
    [Tooltip("다이아가 부족할 때 비용 문구에 적용할 색.")]
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.20f, 0.20f, 1f);
    [Tooltip("입장 버튼 묶음(ButtonPanel > Panel)의 LayoutAutoRebuild. 무료/비용/도전 중 표시를 켜고 끄는 것은 " +
             "문자열 변화가 아니라 자동 감지에 안 걸리므로 직접 알린다(전투 복귀 직후 현황이 늦게 도착할 때 정렬이 어긋났다).")]
    [SerializeField] private LayoutAutoRebuild enterLayoutRebuild;

    [Header("Contribution")]
    [Tooltip("오늘 기여도 목록이 생성될 부모.")]
    [SerializeField] private Transform contributionRoot;
    [SerializeField] private GuildRaidContributionSlot contributionSlotPrefab;
    [Tooltip("오늘 내 누적 기여 피해량.")]
    [SerializeField] private TMP_Text myDamageText;
    [Tooltip("아무도 도전하지 않았을 때 켜는 안내.")]
    [SerializeField] private GameObject emptyContributionLabel;

    [Tooltip("보상 정보 팝업을 여는 버튼. 보스 레벨별로 자정에 받는 보상을 보여 준다.")]
    [SerializeField] private Button rewardInfoButton;

    [Header("Occupied (V1.13)")]
    [Tooltip("선택. 다른 길드원이 도전 중일 때 켜는 묶음. 한 번에 한 명만 도전할 수 있다.")]
    [SerializeField] private GameObject occupiedGroup;
    [Tooltip("선택. 도전 중인 길드원 안내 문구(예: 'OO님이 도전 중').")]
    [SerializeField] private TMP_Text occupiedText;

    private MainHudContext _ctx;
    private GuildPopup _owner;
    private GuildRaidStateDto _state;
    private bool _busy;
    private readonly List<GuildRaidContributionSlot> _slots = new();
    private CompositeDisposable _subs;
    private CancellationToken _ct;
    // 비용 문구의 평상시 색. 프리팹에서 정한 색을 그대로 쓰려고 첫 표시 때 한 번만 기억한다.
    private Color _costDefaultColor = Color.white;
    private bool _costColorCaptured;

    // 파괴된 뒤에 처음 꺼내면 예외가 나는 토큰이라, 살아 있는 동안 한 번 받아 두고 계속 쓴다.
    private void Awake() => _ct = this.GetCancellationTokenOnDestroy();

    public void Initialize(MainHudContext ctx, GuildPopup owner) {
        _ctx = ctx;
        _owner = owner;

        if (enterButton != null) {
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(() => EnterAsync().Forget());
        }

        if (rewardInfoButton != null) {
            rewardInfoButton.onClick.RemoveAllListeners();
            rewardInfoButton.onClick.AddListener(() => OpenRewardInfoAsync().Forget());
        }

        // 탭이 닫혀 있는 동안에는 조회하지 않는다. 그 탭이 열리는 순간 OnEnable 이 시작한다.
        if (!isActiveAndEnabled) return;
        StartPolling();
        ReloadAsync().Forget();
    }

    private void OnEnable() {
        if (_ctx == null) return;   // 아직 초기화 전이면 Initialize 가 시작한다
        StartPolling();
        ReloadAsync().Forget();
    }

    private void OnDisable() => StopPolling();

    /// <summary>
    /// 화면에 떠 있는 동안에만 주기 조회를 돌린다.
    /// 구독을 Initialize 에서 걸면 꺼져 있는 패널에도 타이머가 붙는데, 꺼진 오브젝트는 OnDisable 을 다시 만나지 못하므로
    /// 그 타이머는 팝업이 사라진 뒤에도 계속 돌며 파괴된 오브젝트를 건드린다(길드 정보를 갱신할 때마다 하나씩 쌓인다).
    /// </summary>
    private void StartPolling() {
        if (_subs != null) return;
        _subs = new CompositeDisposable();
        // 보스 체력은 길드원 전원이 함께 깎으므로 켜져 있는 동안 주기적으로 다시 받아온다.
        Observable.Interval(TimeSpan.FromSeconds(PollInterval))
                  .Subscribe(_ => ReloadAsync().Forget())
                  .AddTo(_subs);
        // 다이아 보유량이 밖에서 바뀌어도 폴링을 기다리지 않고 비용 색과 버튼 활성이 따라오게 한다.
        _ctx.Store.Diamond.Subscribe(_ => { if (_state != null) Build(); }).AddTo(_subs);
    }

    private void StopPolling() {
        _subs?.Dispose();
        _subs = null;
    }

    private void Update() {
        // 초기화까지 남은 시간은 매 프레임 줄어드는 표시라 폴링과 별개로 로컬에서 센다.
        if (resetTimerText == null || _state == null || _state.ResetUnix <= 0) return;
        resetTimerText.text = KstTime.Countdown(_state.ResetUnix - KstTime.NowUnix);
    }

    private async UniTaskVoid ReloadAsync() {
        try {
            _state = await _ctx.Guild.RequestRaidStateAsync(_ct);
            Build();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void Build() {
        if (_state == null || !_state.HasGuild) return;

        if (bossLevelText != null) bossLevelText.text = $"Lv.{_state.BossLevel}";
        if (bossHpText != null) bossHpText.text = $"{_state.BossHp:N0} / {_state.BossMaxHp:N0}";
        if (bossHpPercentText != null) bossHpPercentText.text = $"{_state.HpRatio * 100f:F1}%";
        if (bossHpSlider != null) {
            bossHpSlider.minValue = 0f;
            bossHpSlider.maxValue = 1f;
            bossHpSlider.SetValueWithoutNotify(_state.HpRatio);
        }

        if (entryCountText != null) entryCountText.text = $"{_state.MaxEntries - _state.MyEntries} / {_state.MaxEntries}";
        if (myDamageText != null) myDamageText.text = _state.MyDamageToday.ToString("N0");

        // 입장 버튼: 무료분이 남았으면 "참여", 유료면 "추가 참여" + 비용, 다 썼거나 다이아가 모자라면 잠근다
        // (서버도 같은 규칙으로 막지만, 누를 수 없는 버튼을 보여 주지 않는 편이 낫다).
        bool paid = _state.NextEntryCost > 0;
        bool affordable = !paid || _ctx.Store.Diamond.Value >= _state.NextEntryCost;
        // 다른 길드원이 도전 중이면 끝날 때까지 들어갈 수 없다(주기 조회로 끝나는 즉시 풀린다).
        bool occupied = _state.IsOccupied;
        bool canEnter = _state.CanEnter && affordable && !occupied && !_busy;
        if (enterButton != null) enterButton.interactable = canEnter;
        if (enterButtonText != null)
            enterButtonText.text = !_state.CanEnter ? "오늘 완료" : occupied ? "도전 중" : paid ? "추가 참여" : "참여";
        if (occupiedGroup != null) occupiedGroup.SetActive(occupied);
        if (occupiedText != null) {
            occupiedText.gameObject.SetActive(occupied);
            if (occupied) occupiedText.text = $"{_state.OccupantNickname}님이 도전 중";
        }

        // 비용 표시는 유료로 들어갈 때만 낸다. 무료로 참여할 수 있는데 가격이 붙어 있으면 돈을 내는 것처럼 보인다.
        // 무료 표시는 그 반대 자리에 들어가며, 오늘 참여를 다 썼으면 둘 다 끈다.
        bool showCost = _state.CanEnter && paid;
        if (costFreeGroup != null) costFreeGroup.SetActive(_state.CanEnter && !paid);
        if (costText != null) {
            if (!_costColorCaptured) { _costDefaultColor = costText.color; _costColorCaptured = true; }
            costText.gameObject.SetActive(showCost);
            if (showCost) {
                costText.text = _state.NextEntryCost.ToString("N0");
                costText.color = affordable ? _costDefaultColor : insufficientColor;
            }
        }
        if (costIcon != null) {
            CurrencyIcon.Apply(costIcon, _ctx.Currencies, CurrencyType.Diamond);
            costIcon.gameObject.SetActive(showCost);
        }
        if (enterLayoutRebuild != null) enterLayoutRebuild.RebuildDeferred();

        // 무료 참여가 남았는지를 배지의 단일 소스(BadgeCenter)에 넘긴다.
        // 참여 버튼 배지 / 컨텐츠 탭 / 메인 길드 버튼이 모두 이 값 하나를 보므로 서로 어긋나지 않는다.
        _ctx.Badges?.SetGuildRaidFreeEntries(Mathf.Max(0, GuildRaidRules.FreeEntries - _state.MyEntries));

        BuildContributions();
    }

    private void BuildContributions() {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();

        if (contributionRoot != null && contributionSlotPrefab != null) {
            int rank = 1;
            foreach (var c in _state.Contributions) {
                var slot = Instantiate(contributionSlotPrefab, contributionRoot);
                slot.Bind(rank++, c, _state.BossMaxHp, _ctx);
                _slots.Add(slot);
            }
        }
        if (emptyContributionLabel != null) emptyContributionLabel.SetActive(_state.Contributions.Count == 0);
    }

    // 보상 정보: 지금 도전 중인 레벨부터 몇 단계 앞까지 자정에 받는 보상을 한 줄씩 보여 준다.
    // 어느 구간을 보여 줄지 정하는 것은 레이드 규칙(GuildRaidRules)이고 그리는 것은 공용 팝업이라,
    // 이 자리는 둘을 이어 주기만 한다.
    private async UniTaskVoid OpenRewardInfoAsync() {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);

        var reference = _ctx.Popups.RewardInfoPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        // 아직 현황을 못 받았으면 현재 레벨 표시만 빠지고, 표는 1레벨부터 시작한다.
        int currentLevel = _state != null ? _state.BossLevel : 0;
        int firstLevel = GuildRaidRules.RewardPreviewFirstLevel(currentLevel);
        int lastLevel = GuildRaidRules.RewardPreviewLastLevel(currentLevel);

        var rows = new List<RewardInfoPopup.Row>(lastLevel - firstLevel + 1);
        for (int lv = firstLevel; lv <= lastLevel; lv++)
            rows.Add(new RewardInfoPopup.Row($"Lv.{lv}", GuildRaidRules.DailyRewards(lv), lv == currentLevel));

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<RewardInfoPopup>(reference, _ct);
            popup.Show(_ctx, "레이드 보상",
                       "매일 자정에 그날 도달한 보스 레벨의 보상을 길드원 전원이 우편으로 받습니다.\n" +
                       $"보스 레벨은 {GuildRaidRules.MaxBossLevel}까지 오르며, 보상도 레벨에 비례해 늘어납니다.\n" +
                       $"{GuildRaidRules.RewardGrowthCapLevel}레벨 이후로는 길드 포인트만 늘어납니다.", rows);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // 유료 참여는 확인을 받고, 무료분은 바로 들어간다.
    private async UniTaskVoid EnterAsync() {
        if (_busy || _state == null || !_state.CanEnter) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        if (_state.IsOccupied) { _ctx.Ui.ShowToast(GuildRaidRules.OccupiedMessage(_state.OccupantNickname)); return; }

        if (_state.NextEntryCost > 0) {
            bool ok = await _owner.ConfirmAsync("레이드 추가 참여",
                $"다이아 {_state.NextEntryCost:N0}을 사용해 추가로 참여하시겠습니까?");
            if (!ok) return;
        }

        _busy = true;
        try {
            var res = await _ctx.Guild.EnterRaidAsync(_ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(res.FailReason == GuildRaidFail.Occupied
                    ? GuildRaidRules.OccupiedMessage(res.OccupantNickname)
                    : RaidFailMessage(res.FailReason));
                // 확인 팝업을 띄운 사이에 다른 길드원이 먼저 들어간 경우 등, 화면이 서버 상태와 어긋났으므로 다시 받는다.
                ReloadAsync().Forget();
                return;
            }

            if (res.NewDiamond > 0) _ctx.Store.Diamond.Value = res.NewDiamond;

            // 여기서부터는 참여 횟수가 이미 소모된 상태다. 반드시 씬으로 넘어간다.
            GuildRaidSession.Set(res.BossLevel, res.BossHp, res.BossMaxHp);

            // CloseAllPopups 는 이 패널이 속한 팝업까지 파괴하므로, 그 뒤에 쓸 것들을 먼저 지역 변수로 확보한다.
            var ui = _ctx.Ui;
            var sceneService = _ctx.SceneService;
            var sceneKey = _ctx.Scenes.GuildRaidScene.RuntimeKey;

            // UIRoot 는 씬 전환에도 살아남아 전투 화면을 가린다. 돌아왔을 때 보던 자리로 되돌아오도록 예약한 뒤 닫는다.
            MainPopupReturn.Request(_ctx.Popups.GuildPopup, GuildPopup.ContentTabIndex);
            ui.CloseAllPopups();

            // 전환 중 이 패널은 이미 파괴된 상태이므로 await 하지 않고 던진다.
            sceneService.LoadAddressableAsync(sceneKey).Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("레이드에 입장하지 못했습니다.");
        } finally {
            _busy = false;
        }
    }

    // 레이드 전용 실패 사유는 길드 공통 코드 뒤 대역을 쓴다.
    private static string RaidFailMessage(int failReason) => failReason switch {
        GuildRaidFail.NoEntriesLeft => "오늘 참여 횟수를 모두 사용했습니다.",
        GuildRaidFail.MaxLevel => "최고 레벨 보스까지 토벌했습니다.",
        _ => GuildRules.FailMessage(failReason)
    };
}
