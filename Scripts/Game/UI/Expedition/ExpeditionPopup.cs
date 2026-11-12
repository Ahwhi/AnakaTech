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
/// 원정대 팝업. 이번 주 파티원 5명과 장소 5곳의 진행을 보여 주고, 장소 카드를 눌러 원정대 씬으로 보낸다.
///
/// 입장은 서버가 오늘 횟수를 먼저 쓰고 장소를 점유하므로, 성공 응답을 받으면 반드시 씬으로 넘어가야 한다
/// (여기서 멈추면 유저가 횟수만 잃는다). 다른 파티원이 장소를 치는 중인지는 팝업이 열려 있는 동안 주기적으로 다시 받아온다.
/// </summary>
public sealed class ExpeditionPopup : UIView, IHudPopup {
    /// <summary>현황 자동 갱신 주기(초). 다른 파티원이 치고 있는 장소 표시가 따라오도록 한다.</summary>
    private const float PollInterval = 10f;

    [Header("Countdown")]
    [Tooltip("새 매칭까지 남은 시간. 문구와 숫자를 아래 스타일로 나눠 리치 텍스트로 그린다.")]
    [SerializeField] private TMP_Text nextMatchText;
    [SerializeField] private string countdownLabel = "새 매칭까지";
    [SerializeField] private Color countdownLabelColor = new Color(0.80f, 0.80f, 0.80f, 1f);
    [Tooltip("문구 크기(기본 글자 크기 대비 %).")]
    [SerializeField, Range(10, 300)] private int countdownLabelSizePercent = 80;
    [SerializeField] private Color countdownNumberColor = new Color(1f, 0.84f, 0.30f, 1f);
    [Tooltip("숫자 크기(기본 글자 크기 대비 %).")]
    [SerializeField, Range(10, 300)] private int countdownNumberSizePercent = 120;

    [Header("Matched")]
    [Tooltip("매칭됐을 때만 켜는 묶음(파티원 그리드 + 장소 카드).")]
    [SerializeField] private GameObject matchedGroup;
    [Tooltip("파티원 슬롯이 생성될 그리드.")]
    [SerializeField] private Transform memberRoot;
    [SerializeField] private ExpeditionMemberSlot memberSlotPrefab;
    [Tooltip("장소 카드. ExpeditionLocation 순서(통신탑/보급창고/모집사무실/금고/장터)대로 배치한다.")]
    [SerializeField] private ExpeditionLocationCard[] locationCards;
    [Tooltip("오늘 남은 참여 횟수(n/1회). 라벨은 프리팹에서 따로 둔다.")]
    [SerializeField] private TMP_Text entryStatusText;

    [Header("Not Matched")]
    [Tooltip("이번 주 파티가 없을 때 켜는 안내 묶음.")]
    [SerializeField] private GameObject notMatchedGroup;
    [Tooltip("안내 문구(참여 조건 + 내 대표 캐릭터 전투력).")]
    [SerializeField] private TMP_Text notMatchedText;

    [Header("Buttons")]
    [SerializeField] private Button rewardInfoButton;
    [SerializeField] private Button logButton;
    [SerializeField] private Button closeButton;

    [Header("Data")]
    [Tooltip("장소 이름/썸네일.")]
    [SerializeField] private ExpeditionDatabase expeditionDatabase;
    [Tooltip("파티원 프로필 팝업의 길드 마크 표시에 쓴다.")]
    [SerializeField] private GuildMarkCatalog markCatalog;

    [Header("Loading")]
    [SerializeField] private GameObject loadingGroup;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private ExpeditionHomeDto _home;
    private bool _busy;
    private readonly List<ExpeditionMemberSlot> _memberSlots = new();
    private long _shownCountdown = -1;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (closeButton != null)
            closeButton.onClick.AddListener(() => { Click(); CloseAsync().Forget(); });
        if (rewardInfoButton != null)
            rewardInfoButton.onClick.AddListener(() => { Click(); OpenRewardInfoAsync().Forget(); });
        if (logButton != null)
            logButton.onClick.AddListener(() => { Click(); OpenLogAsync().Forget(); });

        // 화면이 떠 있는 동안 다른 파티원의 점유/진행이 따라오도록 주기적으로 다시 받는다.
        Observable.Interval(TimeSpan.FromSeconds(PollInterval))
                  .Subscribe(_ => { if (!_busy) ReloadAsync().Forget(); })
                  .AddTo(this);

        if (matchedGroup != null) matchedGroup.SetActive(false);
        if (notMatchedGroup != null) notMatchedGroup.SetActive(false);
        if (loadingGroup != null) loadingGroup.SetActive(true);
        ReloadAsync().Forget();
    }

    private void Click() => _ctx.Sound.PlaySfx(SfxKey.ButtonClick);

    private void Update() {
        // 새 매칭까지 남은 시간은 매초 줄어드는 표시라 조회와 별개로 로컬에서 센다. 초가 바뀔 때만 다시 그린다.
        if (nextMatchText == null || _home == null || _home.NextMatchUnix <= 0) return;
        long remain = Math.Max(0, _home.NextMatchUnix - KstTime.NowUnix);
        if (remain == _shownCountdown) return;
        _shownCountdown = remain;
        nextMatchText.text = CountdownRichText(remain);
    }

    // "새 매칭까지 n일 00:00:00". 하루 미만이면 일 표기를 빼고 "00:00:00" 만 그린다.
    private string CountdownRichText(long remainSeconds) {
        long d = remainSeconds / 86400;
        long h = remainSeconds / 3600 % 24;
        long m = remainSeconds / 60 % 60;
        long s = remainSeconds % 60;
        string time = d > 0 ? $"{d}일 {h:00}:{m:00}:{s:00}" : $"{h:00}:{m:00}:{s:00}";
        string label = ColorUtility.ToHtmlStringRGBA(countdownLabelColor);
        string number = ColorUtility.ToHtmlStringRGBA(countdownNumberColor);
        return $"<size={countdownLabelSizePercent}%><color=#{label}>{countdownLabel}</color></size> " +
               $"<size={countdownNumberSizePercent}%><color=#{number}>{time}</color></size>";
    }

    private async UniTask ReloadAsync() {
        try {
            _home = await _ctx.Expedition.RequestHomeAsync(_ct);
            _shownCountdown = -1;
            Build();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
    }

    private void Build() {
        if (_home == null) return;
        if (matchedGroup != null) matchedGroup.SetActive(_home.Matched);
        if (notMatchedGroup != null) notMatchedGroup.SetActive(!_home.Matched);

        if (!_home.Matched) {
            BuildNotMatched();
            return;
        }

        BuildMembers();
        BuildLocations();
        if (entryStatusText != null) {
            int remaining = _home.EnteredToday ? 0 : ExpeditionRules.DailyEntries;
            entryStatusText.text = $"{remaining}/{ExpeditionRules.DailyEntries}회";
        }
    }

    private void BuildNotMatched() {
        if (notMatchedText == null) return;
        long power = (long)AccountStats.CharacterCombatPower(_ctx, _ctx.Store.Deck.RepresentativeCharId);
        notMatchedText.text =
            $"대표 캐릭터 전투력이 {ExpeditionRules.MinCombatPower:N0} 이상이면 다음 매칭에 참여합니다.\n" +
            $"현재 대표 캐릭터 전투력: {power:N0}";
    }

    private void BuildMembers() {
        foreach (var s in _memberSlots) if (s != null) Destroy(s.gameObject);
        _memberSlots.Clear();
        if (memberRoot == null || memberSlotPrefab == null) return;

        foreach (var m in _home.Members) {
            var slot = Instantiate(memberSlotPrefab, memberRoot);
            bool isMe = m.AccountId == _home.MyAccountId && !m.Mercenary;
            slot.Bind(m, _ctx, isMe, OpenUserProfile);
            _memberSlots.Add(slot);
        }
    }

    private void BuildLocations() {
        if (locationCards == null) return;
        for (int i = 0; i < locationCards.Length; i++) {
            var card = locationCards[i];
            if (card == null) continue;
            if (i >= ExpeditionRules.Locations.Length) { card.gameObject.SetActive(false); continue; }

            var location = ExpeditionRules.Locations[i];
            var data = _home.LocationOf(location);
            var visual = expeditionDatabase != null ? expeditionDatabase.Get(location) : null;
            string name = expeditionDatabase != null ? expeditionDatabase.NameOf(location) : location.ToString();
            bool canEnter = data != null && !_home.EnteredToday && !data.IsCleared && !data.IsBusy && !_busy;
            card.Bind(data, name, visual != null ? visual.thumbnail : null, canEnter, OnLocationClicked);
        }
    }

    // ── 입장 ───────────────────────────────────────
    private void OnLocationClicked(ExpeditionLocation location) {
        Click();
        EnterAsync(location).Forget();
    }

    private async UniTaskVoid EnterAsync(ExpeditionLocation location) {
        if (_busy || _home == null || !_home.Matched) return;
        var data = _home.LocationOf(location);
        if (data == null) return;

        string name = expeditionDatabase != null ? expeditionDatabase.NameOf(location) : location.ToString();
        try {
            var confirm = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(_ctx.Popups.ConfirmCancelPopup, _ct);
            bool ok = await confirm.ShowAsync("원정대 출격",
                $"{name} {ExpeditionRules.LevelText(data.Level)}에 출격하시겠습니까?\n원정대는 하루에 한 번만 출격할 수 있습니다.");
            if (!ok) return;
        } catch (OperationCanceledException) {
            return;
        }

        _busy = true;
        BuildLocations();
        try {
            var res = await _ctx.Expedition.EnterAsync(location, _ct);
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(ExpeditionRules.FailMessage(res.FailReason, res.BusyNickname));
                _busy = false;
                await ReloadAsync();
                return;
            }

            // 여기서부터는 오늘 횟수가 이미 소모된 상태다. 반드시 씬으로 넘어간다.
            EnterBattle(res);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("원정대에 출격하지 못했습니다.");
        } finally {
            _busy = false;
        }
    }

    private void EnterBattle(ExpeditionEnterDto entry) {
        if (_ctx.Scenes == null || _ctx.Scenes.ExpeditionScene == null || !_ctx.Scenes.ExpeditionScene.RuntimeKeyIsValid()) {
            Debug.LogError("[ExpeditionPopup] SceneCatalog 에 원정대 씬이 할당되지 않았습니다.");
            _ctx.Ui.ShowToast("전투 씬을 열 수 없습니다.");
            return;
        }

        ExpeditionSession.Set(entry, _home);

        // CloseAllPopups 는 이 팝업 자신도 파괴하므로, 그 뒤에 쓸 것들을 먼저 지역 변수로 확보한다.
        var ui = _ctx.Ui;
        var sceneService = _ctx.SceneService;
        var sceneKey = _ctx.Scenes.ExpeditionScene.RuntimeKey;

        // 돌아왔을 때 이 팝업이 다시 열리도록 예약한 뒤 닫는다.
        MainPopupReturn.Request(_ctx.Popups.ExpeditionPopup);
        ui.CloseAllPopups();
        sceneService.LoadAddressableAsync(sceneKey).Forget();
    }

    // ── 보상 정보 / 기록 / 프로필 ─────────────────────
    // 표를 만드는 것은 원정대 규칙(ExpeditionRules)이고 그리는 것은 공용 팝업이라, 이 자리는 둘을 이어 주기만 한다.
    private async UniTaskVoid OpenRewardInfoAsync() {
        var reference = _ctx.Popups.RewardInfoPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        var rows = new List<RewardInfoPopup.Row>();
        foreach (var location in ExpeditionRules.Locations)
            rows.Add(new RewardInfoPopup.Row($"[참여] {NameOf(location)}", ExpeditionRules.AttackRewards(location)));

        // 주간 보상은 지금 도달한 레벨 기준으로 보여 준다(매칭 전이면 1레벨 기준).
        foreach (var location in ExpeditionRules.Locations) {
            int level = _home != null && _home.Matched ? (_home.LocationOf(location)?.Level ?? 1) : 1;
            rows.Add(new RewardInfoPopup.Row($"[주간] {NameOf(location)} {ExpeditionRules.LevelText(level)}",
                                             ExpeditionRules.WeeklyRewards(location, level)));
        }

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<RewardInfoPopup>(reference, _ct);
            popup.Show(_ctx, "원정대 보상",
                       "장소에 출격하면 성공 여부와 관계없이 참여 보상을 바로 받습니다(하루 1회).\n" +
                       "주간 보상은 매주 수요일 00:00에 장소별 도달 레벨 x 기준량으로 우편 지급되며, " +
                       $"클리어한 장소는 {ExpeditionRules.ClearedRewardLevel}레벨로 계산합니다.", rows);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private async UniTaskVoid OpenLogAsync() {
        var reference = _ctx.Popups.ExpeditionLogPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<ExpeditionLogPopup>(reference, _ct);
            await popup.ShowAsync(_ctx, expeditionDatabase);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void OpenUserProfile(int accountId) {
        Click();
        OpenUserProfileAsync(accountId).Forget();
    }

    private async UniTaskVoid OpenUserProfileAsync(int accountId) {
        var reference = _ctx.Popups.UserProfilePopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return;
        try {
            var popup = await _ctx.Ui.ShowPopupAsync<UserProfilePopup>(reference, _ct);
            await popup.ShowAsync(_ctx, accountId, markCatalog);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private string NameOf(ExpeditionLocation location) =>
        expeditionDatabase != null ? expeditionDatabase.NameOf(location) : location.ToString();
}
