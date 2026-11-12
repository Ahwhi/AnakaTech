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
/// 쟁탈전 팝업. 두 탭으로 나뉜다.
///   쟁탈전 : 내 점수/전적/순위, 남은 매칭 횟수, 시즌 카운트다운, 내 편성 미리보기, 매칭 버튼
///   상점   : 쟁탈전 코인으로 사는 상품(공용 상점 페이지를 그대로 재사용한다)
///
/// 매칭은 서버가 횟수를 먼저 깎으므로, 성공 응답을 받으면 반드시 전투 씬으로 넘어가야 한다
/// (여기서 중간에 멈추면 유저가 횟수만 잃는다). 그래서 매칭 -> 상대 편성 조회 -> 씬 전환을 한 흐름으로 둔다.
///
/// 상점 탭은 길드 팝업과 같은 방식이다. 서버 상점 카탈로그에서 쟁탈전 대분류만 걸러 공용 ShopOfferPage 에 넘기므로
/// 구매/한도/재화 차감이 전부 기존 경로를 그대로 탄다(상품을 늘려도 이 화면은 고칠 것이 없다).
/// </summary>
public sealed class ArenaPopup : UIView, IHudPopup, IHudPopupTab {
    /// <summary>쟁탈전 탭 인덱스. 전투 씬을 다녀온 뒤 이 탭으로 돌아온다.</summary>
    public const int ArenaTabIndex = 0;
    private const int ShopTabIndex = 1;

    [Header("Tabs")]
    [Tooltip("탭 버튼(쟁탈전 / 상점 순).")]
    [SerializeField] private UITabButton[] tabButtons;
    [Tooltip("탭 페이지(탭 버튼과 같은 순서).")]
    [SerializeField] private GameObject[] tabPages;

    [Header("My Info")]
    [Tooltip("쟁탈전 점수.")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("전적(예: 12전 8승 4패 (66.7%)).")]
    [SerializeField] private TMP_Text recordText;
    [Tooltip("내 순위. 순위권 밖이면 안내 문구가 들어간다.")]
    [SerializeField] private TMP_Text rankText;
    [Tooltip("시즌 종료까지 남은 시간(카운트다운).")]
    [SerializeField] private TMP_Text seasonTimerText;

    [Header("My Squad")]
    [Tooltip("내 편성 미리보기 칸(최대 5). 편성 순서대로 채우고 남는 칸은 꺼진다.")]
    [SerializeField] private DeckMemberPreview[] myMembers;
    [Tooltip("내 덱 전투력.")]
    [SerializeField] private TMP_Text myPowerText;

    [Header("Match")]
    [SerializeField] private Button matchButton;
    [Tooltip("매칭 버튼 문구(매칭 / 추가 매칭 / 오늘 완료).")]
    [SerializeField] private TMP_Text matchButtonText;
    [Tooltip("남은 무료 매칭 횟수(n / 3 형태).")]
    [SerializeField] private TMP_Text freeEntryText;
    [Tooltip("유료 매칭일 때만 켜는 비용 숫자.")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("유료 매칭일 때 켜는 비용 재화 아이콘. 스프라이트는 재화 카탈로그에서 가져오므로 비워 둔다.")]
    [SerializeField] private Image costIcon;
    [Tooltip("무료로 매칭할 수 있을 때 켜는 그룹(예: '무료' 표시). 비용 표시와 서로 배타적이다.")]
    [SerializeField] private GameObject costFreeGroup;
    [Tooltip("다이아가 부족할 때 비용 문구에 적용할 색.")]
    [SerializeField] private Color insufficientColor = new Color(0.90f, 0.20f, 0.20f, 1f);

    [Header("Runes")]
    [Tooltip("장착 중인 룬 효과 줄이 생성될 부모(VerticalLayoutGroup 권장). 장착한 계열마다 한 줄.")]
    [SerializeField] private Transform runeEffectContent;
    [SerializeField] private ArenaRuneEffectRow runeEffectRowPrefab;
    [Tooltip("계열별 룬 아이콘 에셋(룬 장착 팝업과 같은 것을 넣는다).")]
    [SerializeField] private ArenaRuneIconTable runeIconTable;
    [Tooltip("선택. 장착한 룬이 하나도 없을 때 켜는 안내.")]
    [SerializeField] private GameObject runeEmptyNotice;
    [Tooltip("RunePanel 의 LayoutAutoRebuild. 효과 줄 생성/켜고 끄기는 문자열 변화가 아니라 자동 감지에 안 걸리므로 직접 알린다.")]
    [SerializeField] private LayoutAutoRebuild runeLayoutRebuild;
    [Tooltip("선택. 사용 코스트(예: 7 / 7).")]
    [SerializeField] private TMP_Text runeCostText;
    [Tooltip("룬 장착 팝업을 여는 버튼.")]
    [SerializeField] private Button runeButton;

    [Header("Buttons")]
    [Tooltip("역대 전적 팝업을 여는 버튼.")]
    [SerializeField] private Button historyButton;
    [Tooltip("보상 정보 팝업(공용)을 여는 버튼.")]
    [SerializeField] private Button rewardInfoButton;
    [Tooltip("쟁탈전 내역 팝업을 여는 버튼.")]
    [SerializeField] private Button logButton;
    [Tooltip("내역 버튼의 알림 배지(BadgeView, Source=Manual). 누가 나를 쳤을 때만 켜진다. " +
             "쟁탈전 버튼 배지(Center/Arena)와는 근거가 다르므로 반드시 Manual 로 둔다.")]
    [SerializeField] private BadgeView logBadge;
    [SerializeField] private Button closeButton;

    [Header("Currency")]
    [Tooltip("상단 재화 패널. 다이아 / 쟁탈전 코인 CurrencyView 를 자식으로 배치한다.")]
    [SerializeField] private CurrencyPanel currencyPanel;

    [Header("Shop Tab")]
    [Tooltip("상점 페이지(공용 ShopOfferPage). 쟁탈전 대분류 상품만 걸러 넘긴다.")]
    [SerializeField] private ShopOfferPage shopPage;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private ArenaHomeDto _home;
    private bool _busy;
    private bool _shopBuilt;
    private readonly List<ArenaRuneEffectRow> _runeRows = new();
    // 비용 문구의 평상시 색. 프리팹에서 정한 색을 그대로 쓰려고 첫 표시 때 한 번만 기억한다.
    private Color _costDefaultColor = Color.white;
    private bool _costColorCaptured;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (currencyPanel != null) currencyPanel.Initialize(ctx);
        WireButtons();
        BuildTabs();
        SelectTab(ArenaTabIndex);

        // 다이아가 밖에서 바뀌어도 비용 색과 버튼 활성이 따라오게 한다.
        _ctx.Store.Diamond.Subscribe(_ => { if (_home != null) BuildArena(); }).AddTo(this);

        LoadAsync().Forget();
    }

    private void WireButtons() {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
        if (matchButton != null)
            matchButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); MatchAsync().Forget(); });
        if (rewardInfoButton != null)
            rewardInfoButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenRewardInfoAsync().Forget(); });
        if (logButton != null)
            logButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenLogAsync().Forget(); });
        if (runeButton != null)
            runeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenRunesAsync().Forget(); });
        if (historyButton != null)
            historyButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); OpenHistoryAsync().Forget(); });
    }

    private void BuildTabs() {
        if (tabButtons == null) return;
        for (int i = 0; i < tabButtons.Length; i++) {
            if (tabButtons[i] == null) continue;
            int index = i;
            tabButtons[i].Bind(null, () => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); SelectTab(index); });
        }
    }

    /// <summary>탭 전환(IHudPopupTab). 전투 씬을 다녀온 뒤 보던 탭으로 되돌릴 때도 이 경로를 쓴다.</summary>
    public void SelectTab(int index) {
        if (tabButtons == null || index < 0 || index >= tabButtons.Length) return;

        for (int i = 0; i < tabButtons.Length; i++)
            if (tabButtons[i] != null) tabButtons[i].SetSelected(i == index);
        if (tabPages != null)
            for (int i = 0; i < tabPages.Length; i++)
                if (tabPages[i] != null) tabPages[i].SetActive(i == index);

        // 상점은 처음 열 때 한 번만 만든다(탭을 오갈 때마다 카탈로그를 다시 받지 않는다).
        if (index == ShopTabIndex && !_shopBuilt) BuildShopAsync().Forget();
    }

    // ── 쟁탈전 탭 ──────────────────────────────────
    private async UniTaskVoid LoadAsync() {
        try {
            _home = await _ctx.Arena.RequestHomeAsync(_ct);
            _ctx.Store.SetCurrency(CurrencyType.ArenaCoin, _home.Coin);
            BuildArena();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("쟁탈전 정보를 불러오지 못했습니다.");
        }
    }

    private void BuildArena() {
        if (_home == null) return;

        if (scoreText != null) scoreText.text = ArenaRules.ScoreText(_home.Score);
        if (recordText != null) recordText.text = ArenaRules.RecordText(_home.Wins, _home.Losses);
        if (rankText != null) rankText.text = RankingRules.RankText(_home.Rank);
        if (freeEntryText != null) freeEntryText.text = ArenaRules.FreeEntryText(_home.UsedEntries);

        BuildMySquad();
        BuildRunes();

        // 매칭 버튼: 무료분이 남았으면 "매칭", 유료면 "추가 매칭" + 비용, 다 썼거나 다이아가 모자라면 잠근다
        // (서버도 같은 규칙으로 막지만, 누를 수 없는 버튼을 보여 주지 않는 편이 낫다).
        bool paid = _home.NextEntryCost > 0;
        bool affordable = !paid || _ctx.Store.Diamond.Value >= _home.NextEntryCost;
        if (matchButton != null) matchButton.interactable = _home.CanMatch && affordable && !_busy;
        if (matchButtonText != null)
            matchButtonText.text = !_home.CanMatch ? "오늘 완료" : paid ? "추가 매칭" : "매칭";

        bool showCost = _home.CanMatch && paid;
        if (costFreeGroup != null) costFreeGroup.SetActive(_home.CanMatch && !paid);
        if (costText != null) {
            if (!_costColorCaptured) { _costDefaultColor = costText.color; _costColorCaptured = true; }
            costText.gameObject.SetActive(showCost);
            if (showCost) {
                costText.text = _home.NextEntryCost.ToString("N0");
                costText.color = affordable ? _costDefaultColor : insufficientColor;
            }
        }
        if (costIcon != null) {
            CurrencyIcon.Apply(costIcon, _ctx.Currencies, CurrencyType.Diamond);
            costIcon.gameObject.SetActive(showCost);
        }

        RefreshLogBadge();
    }

    /// <summary>
    /// 내역 버튼 알림: 마지막으로 내역을 본 뒤에 내가 방어자인 기록이 새로 생겼으면 켠다.
    ///
    /// 쟁탈전 버튼 배지(BadgeCenter 의 Arena)와 별개로 둔다. 그쪽은 "오늘 무료 매칭이 남았다"는 안내라
    /// 근거도 수명도 다르고, 이 알림은 쟁탈전 화면 안에서만 의미가 있어 메인까지 올라갈 이유가 없다.
    /// "봤음" 기준은 서버에 두지 않고 로컬(계정별 세이브)에 남긴다. 화면 하나를 위해 서버에 쓰기를 만들지 않기 위함이다.
    /// </summary>
    private void RefreshLogBadge() {
        if (logBadge == null) return;
        var seen = UserDataManager.Account;
        long seenId = seen != null ? seen.seenArenaDefenseLogId : 0;
        long latest = _home != null ? _home.LastDefenseLogId : 0;
        logBadge.SetOn(latest > seenId);
    }

    // 내역을 열면 지금까지의 방어 기록을 본 것으로 기록한다(디스크 쓰기는 오토세이버에 맡긴다).
    private void MarkLogSeen() {
        var seen = UserDataManager.Account;
        if (seen == null || _home == null) return;
        if (_home.LastDefenseLogId <= seen.seenArenaDefenseLogId) return;

        seen.seenArenaDefenseLogId = _home.LastDefenseLogId;
        UserDataManager.RequestSave();
        RefreshLogBadge();
    }

    // 내 편성은 매칭 화면과 같은 위젯으로 그린다(어느 화면에서 보든 같은 모습이 되도록).
    private void BuildMySquad() {
        var deck = _ctx.Store.Deck.SelectedCharIds;
        if (myMembers != null) {
            for (int i = 0; i < myMembers.Length; i++) {
                var slot = myMembers[i];
                if (slot == null) continue;
                if (i >= deck.Count) { slot.Clear(); continue; }

                int charId = deck[i];
                var progress = _ctx.Store.GetProgress(charId);
                slot.Bind(_ctx.Appearance.Mine(charId),
                          progress != null ? progress.StarLevel : 0,
                          progress != null ? progress.Level : 0,
                          progress != null && progress.LimitBroken);
            }
        }
        if (myPowerText != null)
            myPowerText.text = AccountStats.TotalDeckCombatPower(_ctx).ToString("N0");
    }

    private void Update() {
        // 시즌 종료까지 남은 시간은 매 프레임 줄어드는 표시라 조회와 별개로 로컬에서 센다.
        if (seasonTimerText == null || _home == null || _home.SeasonEndUnix <= 0) return;
        seasonTimerText.text = $"시즌 종료까지 {KstTime.Countdown(_home.SeasonEndUnix - KstTime.NowUnix)}";
    }

    // ── 매칭 ───────────────────────────────────────
    private async UniTaskVoid MatchAsync() {
        if (_busy || _home == null || !_home.CanMatch) return;

        // 유료 매칭만 확인을 받는다(무료분은 바로 들어간다).
        if (_home.NextEntryCost > 0) {
            if (_ctx.Store.Diamond.Value < _home.NextEntryCost) { _ctx.Ui.ShowToast("다이아가 부족합니다."); return; }
            var confirm = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(_ctx.Popups.ConfirmCancelPopup, _ct);
            if (!await confirm.ShowAsync("추가 매칭", ArenaRules.PaidMatchConfirmMessage(_home.NextEntryCost))) return;
        }

        _busy = true;
        BuildArena();

        ArenaMatchPopup matchPopup = null;
        try {
            var reference = _ctx.Popups.ArenaMatchPopup;
            if (reference == null || !reference.RuntimeKeyIsValid()) {
                Debug.LogError("[ArenaPopup] UIPopupCatalog 에 쟁탈전 매칭 팝업이 할당되지 않았습니다.");
                _ctx.Ui.ShowToast("준비 중입니다.");
                return;
            }

            matchPopup = await _ctx.Ui.ShowPopupAsync<ArenaMatchPopup>(reference, _ct);
            matchPopup.ShowSearching("매칭중입니다...");

            // 요청을 보내기 전에 취소할 수 있는 구간을 둔다. 이 구간에서 취소하면 아무것도 소모되지 않는다.
            // 요청이 나가는 순간 서버가 횟수를 깎고 상대를 잡으므로, 그 뒤의 취소는 이미 되돌릴 수 없다.
            if (await matchPopup.WaitCancellableAsync(MatchAnimationSeconds, _ct)) {
                await matchPopup.CloseAsync();
                return;
            }
            matchPopup.LockSearching();

            var match = await _ctx.Arena.MatchAsync(_ct);
            if (!match.IsSuccess) {
                await matchPopup.CloseAsync();
                _ctx.Ui.ShowToast(ArenaRules.FailMessage(match.FailReason));
                await ReloadAsync();
                return;
            }

            // 여기서부터 매칭 횟수가 이미 소모된 상태다. 남은 값들을 화면에 먼저 반영해 둔다.
            _ctx.Store.Diamond.Value = match.NewDiamond;
            if (_home != null) {
                _home.UsedEntries = match.UsedEntries;
                _home.NextEntryCost = match.NextEntryCost;
            }

            // 상대 편성은 대항전과 같은 경로(C_UserDeck)로 받는다. 전투 재현에 필요한 원본이 그대로 온다.
            var opponentDeck = await _ctx.Guild.RequestUserDeckAsync(match.OpponentAccountId, _ct);

            if (opponentDeck == null || opponentDeck.Members.Count == 0) {
                await matchPopup.CloseAsync();
                _ctx.Ui.ShowToast("상대의 편성을 불러오지 못했습니다.");
                await ReloadAsync();
                return;
            }

            matchPopup.ShowResult(_ctx, _ctx.Store.Nickname.Value,
                                  (long)AccountStats.TotalDeckCombatPower(_ctx),
                                  _ctx.Store.Deck.SelectedCharIds, match, opponentDeck);

            // 카운트다운이 끝나면 저절로 true 가 온다(버튼은 그 전에 넘어가고 싶을 때 쓴다).
            if (!await matchPopup.WaitForStartAsync()) {
                // 카운트다운 안에 팝업을 닫았다. 매칭 시점에 서버가 이미 "진행 중"으로 잡아 두었으므로
                // 이 판은 제한 시간이 지나면 패배로 굳는다(피한다고 없던 일이 되지 않는다).
                await ReloadAsync();
                return;
            }

            EnterBattle(match, opponentDeck);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("매칭에 실패했습니다.");
            if (matchPopup != null) await matchPopup.CloseAsync();
        } finally {
            _busy = false;
            BuildArena();
        }
    }

    /// <summary>
    /// 매칭 요청을 보내기 전 대기 시간(초). 매칭중 연출을 보여 주는 구간이자 취소할 수 있는 유일한 구간이다.
    /// 너무 짧으면 취소를 누를 수 없고, 길면 매칭이 느리게 느껴진다.
    /// </summary>
    private const float MatchAnimationSeconds = 1.5f;

    // PvP 씬으로 넘어간다. 씬 경계를 넘는 인자는 컨테이너로 전달할 수 없으므로 세션에 담는다.
    private void EnterBattle(ArenaMatchResultDto match, UserDeckDto opponentDeck) {
        if (_ctx.Scenes == null || _ctx.Scenes.PvpBattleScene == null || !_ctx.Scenes.PvpBattleScene.RuntimeKeyIsValid()) {
            Debug.LogError("[ArenaPopup] SceneCatalog 에 PvP 전투 씬이 할당되지 않았습니다.");
            _ctx.Ui.ShowToast("전투 씬을 열 수 없습니다.");
            return;
        }

        // 양 팀 룬은 매칭 시점 서버 값이다(이 판 도중 상대가 룬을 바꿔도 이 판은 그대로).
        PvpBattleSession.Set(PvpBattleSession.OriginType.Arena, match.OpponentAccountId, opponentDeck,
                             match.MyRunes, match.OpponentRunes);

        // CloseAllPopups 는 이 팝업 자신도 파괴하므로, 그 뒤에 쓸 것들을 먼저 지역 변수로 확보한다.
        var ui = _ctx.Ui;
        var sceneService = _ctx.SceneService;
        var sceneKey = _ctx.Scenes.PvpBattleScene.RuntimeKey;

        // UIRoot 는 씬 전환에도 살아남아 전투 화면을 가린다. 돌아왔을 때 보던 자리로 되돌아오도록 예약한 뒤 닫는다.
        MainPopupReturn.Request(_ctx.Popups.ArenaPopup, ArenaTabIndex);
        ui.CloseAllPopups();

        // 전환 중 이 팝업은 이미 파괴된 상태이므로 await 하지 않고 던진다.
        sceneService.LoadAddressableAsync(sceneKey).Forget();
    }

    private async UniTask ReloadAsync() {
        try {
            _home = await _ctx.Arena.RequestHomeAsync(_ct);
            _ctx.Store.SetCurrency(CurrencyType.ArenaCoin, _home.Coin);
            BuildArena();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // ── 상점 탭 ────────────────────────────────────
    private async UniTaskVoid BuildShopAsync() {
        if (shopPage == null) return;
        try {
            var catalog = await _ctx.Shop.RequestShopAsync(_ct);
            // 응답에 실려 온 재화 잔량을 스토어에 반영한다(재화 패널이 스토어를 구독하므로 빠뜨리면 0 으로 남는다).
            _ctx.Store.Gold.Value = catalog.Gold;
            _ctx.Store.Diamond.Value = catalog.Diamond;
            _ctx.Store.ApplyShopCurrencies(catalog.Currencies);

            var offers = new List<ShopOfferDto>();
            foreach (var o in catalog.Offers) if (o.Major == ArenaRules.ShopMajor) offers.Add(o);

            shopPage.Initialize(_ctx, offers, hasAllTab: true, catalog);
            _shopBuilt = true;
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("상점을 불러오지 못했습니다.");
        }
    }

    // ── 룬 / 역대 전적 ──────────────────────────────
    // 효과 줄은 계열마다 한 번만 만들어 두고, 구성이 바뀌면 장착 여부에 따라 켜고 끈다.
    private void BuildRunes() {
        var runes = _home != null ? _home.Runes : null;
        if (_runeRows.Count == 0 && runeEffectContent != null && runeEffectRowPrefab != null) {
            for (int f = 0; f < ArenaRuneRules.FamilyCount; f++) {
                var family = (ArenaRuneRules.Family)f;
                var row = Instantiate(runeEffectRowPrefab, runeEffectContent);
                row.Bind(family, runeIconTable != null ? runeIconTable.IconOf(family) : null);
                _runeRows.Add(row);
            }
        }
        foreach (var row in _runeRows) if (row != null) row.SetLevel(runes != null ? runes.LevelOf(row.Family) : 0);
        int cost = runes != null ? runes.TotalCost : 0;
        if (runeEmptyNotice != null) runeEmptyNotice.SetActive(cost == 0);
        if (runeCostText != null) runeCostText.text = ArenaRuneRules.CostText(cost);
        if (runeLayoutRebuild != null) runeLayoutRebuild.RebuildDeferred();
    }

    // 룬 팝업이 저장까지 마치고 새 구성을 돌려준다. 바뀌었으면 이 화면의 효과 문구만 다시 그린다.
    private async UniTaskVoid OpenRunesAsync() {
        if (_home == null) return;
        var reference = _ctx.Popups.ArenaRunePopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<ArenaRunePopup>(reference, _ct);
            var saved = await popup.ShowAsync(_ctx, _home.Runes);
            if (saved == null || _home == null) return;
            _home.Runes = saved;
            BuildRunes();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private async UniTaskVoid OpenHistoryAsync() {
        var reference = _ctx.Popups.ArenaHistoryPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<ArenaHistoryPopup>(reference, _ct);
            await popup.ShowAsync(_ctx);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // ── 보상 정보 / 내역 ────────────────────────────
    // 보상 표를 만드는 것은 쟁탈전 규칙(ArenaRules)이고 그리는 것은 공용 팝업이라, 이 자리는 둘을 이어 주기만 한다.
    private async UniTaskVoid OpenRewardInfoAsync() {
        var reference = _ctx.Popups.RewardInfoPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        var rows = new List<RewardInfoPopup.Row>();
        foreach (var (label, rewards) in ArenaRules.RewardTable())
            rows.Add(new RewardInfoPopup.Row(label, rewards));

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<RewardInfoPopup>(reference, _ct);
            popup.Show(_ctx, "쟁탈전 보상",
                       "승패 보상은 전투 직후, 일일 보상은 매일 자정, 주간 순위 보상은 매주 월요일 자정에 우편으로 받습니다.",
                       rows);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private async UniTaskVoid OpenLogAsync() {
        var reference = _ctx.Popups.ArenaLogPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        MarkLogSeen();   // 여는 순간 읽은 것으로 본다(목록을 끝까지 내렸는지까지 따지지 않는다).

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<ArenaLogPopup>(reference, _ct);
            await popup.ShowAsync(_ctx);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }
}
