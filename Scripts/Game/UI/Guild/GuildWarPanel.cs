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
/// 길드 대항전 패널(길드 팝업의 컨텐츠 탭). 양쪽 길드원 명단을 좌우로 늘어놓고 생존자 수를 시각화한다.
/// 상대 명단의 공격 버튼을 누르면 그 길드원의 덱과 싸우는 PvP 전투 씬으로 넘어간다.
///
/// 단계(대기/경기/정산/완료)에 따라 보여줄 것이 달라진다.
///   대기 : 경기 시작까지 카운트다운(공격 불가)
///   경기 : 종료까지 카운트다운 + 공격 가능
///   정산/완료 : 승패 결과
/// 공격 횟수는 서버가 입장 시점에 먼저 깎으므로, 성공 응답을 받으면 반드시 씬으로 넘어가야 한다.
/// </summary>
public sealed class GuildWarPanel : MonoBehaviour {
    /// <summary>현황 자동 갱신 주기(초). 다른 길드원의 공격 결과가 화면에 따라오도록 한다.</summary>
    private const float PollInterval = 15f;

    [Header("Match")]
    [Tooltip("매칭이 잡혔을 때 보여줄 그룹.")]
    [SerializeField] private GameObject matchGroup;
    [Tooltip("매칭 전에 보여줄 안내(다음 매칭 시각 등).")]
    [SerializeField] private GameObject noMatchGroup;
    [Tooltip("다음 매칭까지 남은 시간(NoMatchGroup 안). 매칭 시각 규칙은 서버가 정하므로 응답으로 받은 시각을 센다. " +
             "매칭이 진행 중일 때는 언제 끝날지 알 수 없으므로 통째로 숨긴다.")]
    [SerializeField] private GameObject nextMatchTimerGroup;
    [SerializeField] private TMP_Text nextMatchTimerText;
    [Tooltip("선택. 다음 매칭 시각 자체(NoMatchGroup 안). 남은 시간과 달리 매초 바뀌지 않아 한 번만 채운다.")]
    [SerializeField] private TMP_Text nextMatchDateText;
    [Tooltip("상대가 없는 이유 안내(NoMatchGroup 안). 문구는 GuildWarRules 가 정한다.")]
    [SerializeField] private TMP_Text noMatchMessageText;
    [Tooltip("현재 단계 문구(경기 대기중 / 경기 진행중 / 정산중 / 종료).")]
    [SerializeField] private TMP_Text phaseText;
    [Tooltip("타이머 묶음(문구 + 남은 시간). 셀 것이 없는 단계(정산/종료)에는 통째로 꺼진다. 비우면 timerText 만 끈다.")]
    [SerializeField] private GameObject timerGroup;
    [Tooltip("단계별 남은 시간(hh:mm:ss). 대기 중이면 시작까지, 경기 중이면 종료까지.")]
    [SerializeField] private TMP_Text timerText;
    [Tooltip("timerText 가 무엇까지의 시간인지 알려 주는 문구(경기 시작까지 / 경기 종료까지). 단계에 따라 코드가 채운다.")]
    [SerializeField] private TMP_Text timerLabelText;

    [Header("Score")]
    [SerializeField] private TMP_Text myGuildNameText;
    [SerializeField] private Image myMarkImage;
    [SerializeField] private TMP_Text myAliveText;
    [SerializeField] private TMP_Text enemyGuildNameText;
    [SerializeField] private Image enemyMarkImage;
    [SerializeField] private TMP_Text enemyAliveText;
    [Tooltip("양쪽 생존 비율 슬라이더(0=상대만 생존, 1=우리만 생존).")]
    [SerializeField] private Slider scoreSlider;

    [Header("My Status")]
    [Tooltip("남은 공격 횟수. n / n 형태.")]
    [SerializeField] private TMP_Text myAttacksText;
    [Tooltip("내가 격파당했을 때 켜는 안내(선택). 격파당해도 남은 공격 횟수는 그대로 쓸 수 있다.")]
    [SerializeField] private GameObject myDeadLabel;

    [Tooltip("교전 기록 팝업을 여는 버튼. 매칭이 없으면 볼 기록도 없어 잠긴다.")]
    [SerializeField] private Button warLogButton;

    [Tooltip("보상 정보 팝업을 여는 버튼. 승리/패배 시 받는 보상을 보여 준다(매칭 여부와 무관하게 항상 볼 수 있다).")]
    [SerializeField] private Button rewardInfoButton;

    [Header("Roster")]
    [SerializeField] private Transform myRosterRoot;
    [SerializeField] private Transform enemyRosterRoot;
    [SerializeField] private GuildWarMemberSlot memberSlotPrefab;

    [Header("Result")]
    [Tooltip("승패가 정해졌을 때 켜는 그룹.")]
    [SerializeField] private GameObject resultGroup;
    [SerializeField] private TMP_Text resultText;
    [Tooltip("승리했을 때 문구.")]
    [SerializeField] private string winText = "승리";
    [Tooltip("승리했을 때 문구 색.")]
    [SerializeField] private Color winColor = new Color(1f, 0.84f, 0.30f, 1f);
    [Tooltip("패배했을 때 문구.")]
    [SerializeField] private string loseText = "패배";
    [Tooltip("패배했을 때 문구 색.")]
    [SerializeField] private Color loseColor = new Color(0.62f, 0.64f, 0.70f, 1f);

    private MainHudContext _ctx;
    private GuildPopup _owner;
    private GuildWarStateDto _state;
    private bool _busy;
    private readonly List<GuildWarMemberSlot> _mySlots = new();
    private readonly List<GuildWarMemberSlot> _enemySlots = new();
    private CompositeDisposable _subs;
    private CancellationToken _ct;

    // 파괴된 뒤에 처음 꺼내면 예외가 나는 토큰이라, 살아 있는 동안 한 번 받아 두고 계속 쓴다.
    private void Awake() => _ct = this.GetCancellationTokenOnDestroy();

    public void Initialize(MainHudContext ctx, GuildPopup owner) {
        _ctx = ctx;
        _owner = owner;

        if (warLogButton != null) {
            warLogButton.onClick.RemoveAllListeners();
            warLogButton.onClick.AddListener(() => OpenWarLogAsync().Forget());
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
        Observable.Interval(TimeSpan.FromSeconds(PollInterval))
                  .Subscribe(_ => ReloadAsync().Forget())
                  .AddTo(_subs);
    }

    private void StopPolling() {
        _subs?.Dispose();
        _subs = null;
    }

    private void Update() {
        // 카운트다운은 매 프레임 줄어드는 표시라 폴링과 별개로 로컬에서 센다.
        if (_state == null) return;

        // 매칭 전에는 다음 매칭까지 남은 시간을 센다(안내 문구만 두면 화면이 비어 보인다).
        if (!_state.HasMatch) {
            // 매칭 진행 중에는 남을 시간을 알 수 없어 Build 가 카운트다운을 꺼 둔다.
            if (_state.NoMatchReason == GuildWarNoMatchReason.Matching) return;
            if (nextMatchTimerText != null && _state.NextMatchUnix > 0)
                nextMatchTimerText.text = KstTime.Countdown(_state.NextMatchUnix - KstTime.NowUnix);
            return;
        }

        if (timerText == null) return;
        long target = _state.Phase == GuildWarPhase.Waiting ? _state.BattleStartUnix
                    : _state.Phase == GuildWarPhase.Battle ? _state.BattleEndUnix
                    : 0;
        if (target <= 0) { timerText.text = "-"; return; }

        timerText.text = KstTime.Countdown(target - KstTime.NowUnix);
    }

    private async UniTaskVoid ReloadAsync() {
        try {
            _state = await _ctx.Guild.RequestWarStateAsync(_ct);
            Build();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void Build() {
        if (_state == null) return;

        bool hasMatch = _state.HasMatch;
        if (matchGroup != null) matchGroup.SetActive(hasMatch);
        if (noMatchGroup != null) noMatchGroup.SetActive(!hasMatch);
        // 매칭이 없으면 볼 기록도 없다(버튼이 MatchGroup 밖에 있어도 눌리지 않게 한다).
        if (warLogButton != null) warLogButton.interactable = hasMatch;
        if (!hasMatch) {
            // 매칭 시각이 지났는데 아직 짝이 안 잡힌 공백기에는 카운트다운을 숨긴다.
            // 그대로 두면 다음 회차(일주일 뒤)까지의 시간이 튀어나와, 매칭을 기다리는 사람에게 거짓 안내가 된다.
            bool matching = _state.NoMatchReason == GuildWarNoMatchReason.Matching;
            if (noMatchMessageText != null) noMatchMessageText.text = GuildWarRules.NoMatchMessage(_state.NoMatchReason);
            // 감쌀 그룹을 지정하지 않았으면 타이머 텍스트만이라도 끈다(라벨까지 숨기려면 그룹을 지정한다).
            var nextTimerRoot = nextMatchTimerGroup != null ? nextMatchTimerGroup
                              : nextMatchTimerText != null ? nextMatchTimerText.gameObject
                              : null;
            if (nextTimerRoot != null) nextTimerRoot.SetActive(!matching);
            // 매칭 시각 자체는 매초 바뀌지 않으므로 여기서 한 번만 채운다(남은 시간은 Update 가 센다).
            if (nextMatchDateText != null) nextMatchDateText.text = KstTime.Format(_state.NextMatchUnix);
            return;
        }

        if (phaseText != null) phaseText.text = PhaseName(_state.Phase);
        if (timerLabelText != null) timerLabelText.text = TimerLabel(_state.Phase);

        // 정산/종료 단계에는 셀 것이 없다. 0 이나 "-" 를 남겨 두면 아직 뭔가 진행 중인 것처럼 보이므로 통째로 끈다.
        bool counting = HasCountdown(_state.Phase);
        var timerRoot = timerGroup != null ? timerGroup
                      : timerText != null ? timerText.gameObject
                      : null;
        if (timerRoot != null) timerRoot.SetActive(counting);

        ApplySide(myGuildNameText, myMarkImage, myAliveText, _state.Mine);
        ApplySide(enemyGuildNameText, enemyMarkImage, enemyAliveText, _state.Enemy);

        if (scoreSlider != null) {
            int total = _state.Mine.AliveCount + _state.Enemy.AliveCount;
            scoreSlider.minValue = 0f;
            scoreSlider.maxValue = 1f;
            scoreSlider.SetValueWithoutNotify(total > 0 ? (float)_state.Mine.AliveCount / total : 0.5f);
        }

        if (myAttacksText != null)
            myAttacksText.text = $"{_state.MyAttacksLeft} / {GuildWarRules.AttacksPerMember}";
        if (myDeadLabel != null) myDeadLabel.SetActive(_state.MyDead);

        bool hasResult = _state.HasResult;
        if (resultGroup != null) resultGroup.SetActive(hasResult);
        // 문구와 색은 인스펙터에서 정한다(연출은 화면 몫이라 코드에 박아 두지 않는다).
        if (resultText != null && hasResult) {
            bool won = _state.IWon;
            resultText.text = won ? winText : loseText;
            resultText.color = won ? winColor : loseColor;
        }

        BuildRoster(myRosterRoot, _mySlots, _state.Mine.Members, enemy: false);
        BuildRoster(enemyRosterRoot, _enemySlots, _state.Enemy.Members, enemy: true);
    }

    private void ApplySide(TMP_Text nameText, Image markImage, TMP_Text aliveText, GuildWarSideDto side) {
        if (nameText != null) nameText.text = side.GuildName;
        if (aliveText != null) aliveText.text = $"{side.AliveCount} / {side.TotalCount}";
        if (markImage != null) {
            var marks = _owner != null ? _owner.Marks : null;
            markImage.sprite = marks != null ? marks.Icon(side.MarkId) : null;
            markImage.enabled = markImage.sprite != null;
        }
    }

    // 상대 명단에만 공격 버튼이 붙는다. 지금 때릴 수 있는지는 단계/승부 여부/남은 횟수/대상 격파 여부로 정해진다.
    private void BuildRoster(Transform root, List<GuildWarMemberSlot> slots,
                             List<GuildWarParticipantDto> members, bool enemy) {
        if (root == null || memberSlotPrefab == null) return;

        SortRoster(members, enemy);

        // 명단은 갱신 주기마다 다시 그린다. 매번 전부 파괴하고 새로 만들면 Destroy 가 프레임 끝까지 미뤄지는 탓에
        // 그 프레임에 목록이 두 벌로 겹쳐 흔들리고 버튼이 스쳐 보인다. 있는 슬롯은 다시 쓰고 개수 차이만 메운다.
        while (slots.Count < members.Count) slots.Add(Instantiate(memberSlotPrefab, root));
        for (int i = slots.Count - 1; i >= members.Count; i--) {
            if (slots[i] != null) Destroy(slots[i].gameObject);
            slots.RemoveAt(i);
        }

        for (int i = 0; i < members.Count; i++) {
            if (slots[i] == null) slots[i] = Instantiate(memberSlotPrefab, root);
            var m = members[i];
            bool attackable = enemy && _state.CanAttack && !m.Dead;
            bool isMe = !enemy && m.AccountId == _state.MyAccountId;
            slots[i].Bind(m, _ctx, enemy, isMe, attackable, _owner.OpenUserProfile, OnAttack);
        }
    }

    /// <summary>
    /// 명단 정렬. 우리 팀은 내가 항상 맨 위에 오고(내 상태를 먼저 확인해야 한다),
    /// 그 다음은 양쪽 모두 생존자 우선 -> 전투력 높은 순이다(때릴 만한 상대가 위에 모인다).
    /// 서버가 준 목록을 그대로 정렬한다. 갱신마다 새로 받는 목록이라 원본을 지킬 이유가 없다.
    /// </summary>
    private void SortRoster(List<GuildWarParticipantDto> members, bool enemy) {
        int myId = _state.MyAccountId;
        members.Sort((a, b) => {
            if (!enemy) {
                bool aMe = a.AccountId == myId;
                bool bMe = b.AccountId == myId;
                if (aMe != bMe) return aMe ? -1 : 1;
            }
            if (a.Dead != b.Dead) return a.Dead ? 1 : -1;   // 생존자 먼저
            return b.CombatPower.CompareTo(a.CombatPower);  // 전투력 높은 순
        });
    }

    private async UniTaskVoid OpenWarLogAsync() {
        if (_state == null || !_state.HasMatch) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);

        var reference = _ctx.Popups.GuildWarLogPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<GuildWarLogPopup>(reference, _ct);
            await popup.ShowAsync(_ctx, _state.Mine.GuildId, _state.MyAccountId);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    // 보상 정보: 대항전은 조건이 승패 둘뿐이라 두 줄짜리 표다.
    // 표를 만드는 것은 대항전 규칙(GuildWarRules)이고 그리는 것은 공용 팝업이라, 이 자리는 둘을 이어 주기만 한다.
    private async UniTaskVoid OpenRewardInfoAsync() {
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);

        var reference = _ctx.Popups.RewardInfoPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) { _ctx.Ui.ShowToast("준비 중입니다."); return; }

        var rows = new List<RewardInfoPopup.Row> {
            new("승리", GuildWarRules.WinRewards),
            new("패배", GuildWarRules.LoseRewards),
        };

        try {
            var popup = await _ctx.Ui.ShowPopupAsync<RewardInfoPopup>(reference, _ct);
            popup.Show(_ctx, "대항전 보상",
                       "경기가 끝나면 승패에 따라 길드원 전원이 우편으로 보상을 받습니다.", rows);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void OnAttack(int targetAccountId) {
        if (_busy) return;
        AttackAsync(targetAccountId).Forget();
    }

    private async UniTaskVoid AttackAsync(int targetAccountId) {
        _busy = true;
        try {
            _ctx.Sound.PlaySfx(SfxKey.ButtonClick);

            // 적 파티 구성을 먼저 받아 둔다. 공격 횟수를 소모한 뒤에 실패하면 그 횟수가 그냥 날아간다.
            var enemyDeck = await _ctx.Guild.RequestUserDeckAsync(targetAccountId, _ct);
            if (enemyDeck == null || enemyDeck.Members.Count == 0) {
                _ctx.Ui.ShowToast("상대의 편성 정보를 불러오지 못했습니다.");
                return;
            }

            var begin = await _ctx.Guild.BeginWarAttackAsync(targetAccountId, _ct);
            if (!begin.IsSuccess) { _ctx.Ui.ShowToast(GuildWarRules.FailMessage(begin.FailReason)); return; }

            // 여기서부터는 공격 횟수가 이미 소모된 상태다. 반드시 씬으로 넘어간다.
            PvpBattleSession.Set(targetAccountId, enemyDeck);

            // CloseAllPopups 는 이 패널이 속한 팝업까지 파괴하므로, 그 뒤에 쓸 것들을 먼저 지역 변수로 확보한다.
            var ui = _ctx.Ui;
            var sceneService = _ctx.SceneService;
            var sceneKey = _ctx.Scenes.PvpBattleScene.RuntimeKey;

            // UIRoot 는 씬 전환에도 살아남아 전투 화면을 가린다. 돌아왔을 때 보던 자리로 되돌아오도록 예약한 뒤 닫는다.
            MainPopupReturn.Request(_ctx.Popups.GuildPopup, GuildPopup.ContentTabIndex);
            ui.CloseAllPopups();

            // 전환 중 이 패널은 이미 파괴된 상태이므로 await 하지 않고 던진다.
            sceneService.LoadAddressableAsync(sceneKey).Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("공격을 시작하지 못했습니다.");
        } finally {
            _busy = false;
        }
    }

    private static string PhaseName(GuildWarPhase phase) => phase switch {
        GuildWarPhase.Waiting => "경기 대기 중",
        GuildWarPhase.Battle => "경기 진행 중",
        GuildWarPhase.Settle => "정산 중",
        GuildWarPhase.Done => "종료",
        _ => "매칭 대기"
    };

    // 셀 대상이 있는 단계인지. 정산/종료는 남은 시간이라는 개념이 없다.
    private static bool HasCountdown(GuildWarPhase phase) =>
        phase == GuildWarPhase.Waiting || phase == GuildWarPhase.Battle;

    // timerText 가 무엇까지의 시간인지 알려 준다.
    private static string TimerLabel(GuildWarPhase phase) => phase switch {
        GuildWarPhase.Waiting => "경기 시작까지 남은 시간",
        GuildWarPhase.Battle => "경기 종료까지 남은 시간",
        _ => ""
    };
}
