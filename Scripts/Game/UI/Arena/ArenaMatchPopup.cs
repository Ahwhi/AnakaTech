using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 쟁탈전 매칭 연출 팝업. 두 장면을 순서대로 보여 준다.
///   1) 매칭중 : 상대를 찾는 동안의 대기 연출
///   2) 매칭완료 : 내 편성 vs 상대 편성을 전투력과 함께 나란히 보여 주고 전투 시작을 기다린다
///
/// 통신은 하지 않는다. 매칭 요청과 씬 전환은 쟁탈전 팝업이 맡고, 여기는 "무엇을 보여 줄지"만 안다.
/// 그래서 매칭 규칙이 바뀌어도 이 화면은 그대로다.
/// </summary>
public sealed class ArenaMatchPopup : UIView {
    [Header("Searching")]
    [Tooltip("상대를 찾는 동안 보여줄 그룹(안내 문구 + 스피너 등).")]
    [SerializeField] private GameObject searchingGroup;
    [Tooltip("매칭중 안내 문구. 비우면 프리팹 문구를 그대로 쓴다.")]
    [SerializeField] private TMP_Text searchingText;
    [Tooltip("매칭중 동안 커졌다 작아지는 연출 대상(선택). 비우면 연출 없이 그룹만 켜진다.")]
    [SerializeField] private RectTransform searchingPulseTarget;
    [Tooltip("맥동 배율(1 = 원래 크기).")]
    [SerializeField] private float pulseScale = 1.08f;
    [Tooltip("맥동 한 방향 시간(초).")]
    [SerializeField] private float pulseDuration = 0.5f;
    [Tooltip("매칭 취소 버튼. 상대를 찾기 전에만 누를 수 있고, 요청이 나간 뒤에는 잠긴다(그 뒤엔 되돌릴 수 없다).")]
    [SerializeField] private Button cancelButton;

    [Header("Result")]
    [Tooltip("매칭이 끝난 뒤 켜는 그룹(양쪽 편성 비교).")]
    [SerializeField] private GameObject resultGroup;

    [Header("Result - Mine")]
    [SerializeField] private TMP_Text myNameText;
    [SerializeField] private TMP_Text myPowerText;
    [Tooltip("내 편성 미리보기 칸(최대 5). 편성 순서대로 채우고 남는 칸은 꺼진다.")]
    [SerializeField] private DeckMemberPreview[] myMembers;

    [Header("Result - Opponent")]
    [SerializeField] private TMP_Text opponentNameText;
    [SerializeField] private TMP_Text opponentPowerText;
    [Tooltip("상대의 쟁탈전 점수(선택).")]
    [SerializeField] private TMP_Text opponentScoreText;
    [Tooltip("상대 프로필 테두리(선택).")]
    [SerializeField] private ProfileFrameView opponentFrameView;
    [SerializeField] private DeckMemberPreview[] opponentMembers;

    [Header("Countdown")]
    [Tooltip("자동 시작까지 남은 초. 매초 숫자가 바뀌며 튀는 연출이 들어간다.")]
    [SerializeField] private TMP_Text countdownText;
    [Tooltip("카운트다운 연출 대상(보통 countdownText 의 RectTransform). 비우면 숫자만 바뀐다.")]
    [SerializeField] private RectTransform countdownPunchTarget;
    [Tooltip("한 칸 줄어들 때 튀는 크기(0.4 = 40% 확대 후 복귀).")]
    [SerializeField] private float countdownPunch = 0.4f;
    [Tooltip("튀는 연출 시간(초). 1초보다 짧아야 다음 숫자와 겹치지 않는다.")]
    [SerializeField] private float countdownPunchDuration = 0.35f;
    [Tooltip("여유 있을 때(숫자가 클 때) 색.")]
    [SerializeField] private Color countdownCalmColor = new Color(1f, 1f, 1f, 1f);
    [Tooltip("임박했을 때 색. 남은 초가 줄수록 이 색으로 물든다.")]
    [SerializeField] private Color countdownUrgentColor = new Color(1f, 0.30f, 0.25f, 1f);

    [Header("Buttons")]
    [Tooltip("전투 시작 버튼. 카운트다운을 기다리지 않고 바로 넘어가고 싶을 때 쓴다(선택).")]
    [SerializeField] private Button startButton;

    // true = 전투를 시작한다(버튼 또는 카운트다운 종료), false = 그 외의 경로로 닫혔다.
    private UniTaskCompletionSource<bool> _startTcs;
    // 취소 신호. 상대를 찾기 전(요청을 보내기 전) 구간에서만 쓰인다.
    private UniTaskCompletionSource<bool> _cancelTcs;
    private Tween _pulse;
    private Tween _countdownTween;
    // 카운트다운 소리용. 이 팝업은 컨테이너 주입 대상이 아니라, 결과를 넘겨주는 상위가 함께 건네준 묶음에서 받아 둔다.
    private ISoundService _sound;

    // 시작 신호. 카운트다운과 버튼이 같은 것을 공유해야 하므로 처음 필요해질 때 만들어 둔다
    // (결과 화면을 먼저 띄우고 호출부가 나중에 기다려도 신호를 놓치지 않는다).
    private UniTaskCompletionSource<bool> Started => _startTcs ??= new UniTaskCompletionSource<bool>();

    protected override void Awake() {
        base.Awake();
        if (startButton != null) startButton.onClick.AddListener(() => Started.TrySetResult(true));
        if (cancelButton != null) cancelButton.onClick.AddListener(() => _cancelTcs?.TrySetResult(true));
    }

    /// <summary>매칭중 화면으로 연다. 상대를 찾는 동안 이 상태로 떠 있는다.</summary>
    public void ShowSearching(string message = null) {
        if (searchingGroup != null) searchingGroup.SetActive(true);
        if (resultGroup != null) resultGroup.SetActive(false);
        if (searchingText != null && !string.IsNullOrEmpty(message)) searchingText.text = message;
        if (countdownText != null) countdownText.text = "";

        _cancelTcs = new UniTaskCompletionSource<bool>();
        if (cancelButton != null) {
            cancelButton.gameObject.SetActive(true);
            cancelButton.interactable = true;
        }

        StartPulse();
        ShowAsync().Forget();
    }

    /// <summary>
    /// 취소할 수 있는 대기 구간. seconds 만큼 기다리되 그 사이에 취소를 누르면 즉시 true 로 돌아온다.
    ///
    /// 매칭 요청을 보내기 "전"에 이 구간을 두는 이유: 요청이 나가는 순간 서버가 횟수를 깎고 상대를 잡아
    /// 진행 중으로 표시하므로, 그 뒤의 취소는 이미 되돌릴 수 없다(도망치면 패배로 굳는다).
    /// 그래서 되돌릴 수 있는 시점에만 취소를 열어 준다.
    /// </summary>
    public async UniTask<bool> WaitCancellableAsync(float seconds, CancellationToken ct) {
        var delay = UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale: true, cancellationToken: ct);
        int finished = await UniTask.WhenAny(delay, _cancelTcs.Task);
        return finished == 1;   // 1 = 취소 신호가 먼저 왔다
    }

    /// <summary>취소를 잠근다(매칭 요청을 보낸 뒤 호출). 이 시점부터는 되돌릴 수 없다.</summary>
    public void LockSearching() {
        _cancelTcs?.TrySetResult(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 매칭 결과 화면으로 바꾼다. 양쪽 편성을 같은 위젯(DeckMemberPreview)으로 그리므로
    /// 내 파티와 상대 파티가 같은 방식으로 보인다.
    /// </summary>
    /// <param name="myName">내 닉네임.</param>
    /// <param name="myPower">내 덱 전투력.</param>
    /// <param name="myDeck">내 편성(캐릭터 id 순서 그대로).</param>
    /// <param name="match">매칭 응답(상대 표시 값).</param>
    /// <param name="opponentDeck">상대 편성(전투 재현용으로 받아 온 원본).</param>
    public void ShowResult(MainHudContext ctx, string myName, long myPower, System.Collections.Generic.IReadOnlyList<int> myDeck,
                           ArenaMatchResultDto match, UserDeckDto opponentDeck) {
        StopPulse();
        _sound = ctx != null ? ctx.Sound : null;
        if (searchingGroup != null) searchingGroup.SetActive(false);
        if (resultGroup != null) resultGroup.SetActive(true);

        if (myNameText != null) myNameText.text = myName ?? "";
        if (myPowerText != null) myPowerText.text = myPower.ToString("N0");
        BindMyMembers(ctx, myDeck);

        if (opponentNameText != null) opponentNameText.text = match != null ? match.OpponentNickname : "";
        if (opponentPowerText != null) opponentPowerText.text = match != null ? match.OpponentCombatPower.ToString("N0") : "0";
        if (opponentScoreText != null) opponentScoreText.text = match != null ? ArenaRules.ScoreText(match.OpponentScore) : "0";
        if (opponentFrameView != null && match != null)
            opponentFrameView.Bind(ctx != null ? ctx.ProfileFrames : null, match.OpponentProfileFrameId);
        BindOpponentMembers(ctx, opponentDeck);

        CountdownAsync().Forget();
    }

    /// <summary>
    /// 전투 시작을 기다린다. true = 시작한다(카운트다운 종료 또는 버튼), false = 그 외의 경로로 닫혔다.
    /// 닫힘도 결과로 확정하므로 기다리던 호출부가 영원히 멈추지 않는다.
    /// </summary>
    public UniTask<bool> WaitForStartAsync() => Started.Task;

    /// <summary>
    /// 자동 시작 카운트다운. 숫자가 줄 때마다 튀고, 남은 시간이 짧을수록 색이 임박한 쪽으로 물든다.
    /// 0 이 되면 시작 신호를 낸다(버튼을 먼저 눌렀거나 팝업이 닫혔으면 그대로 멎는다).
    /// </summary>
    private async UniTaskVoid CountdownAsync() {
        var ct = this.GetCancellationTokenOnDestroy();
        int remain = ArenaRules.MatchCountdownSeconds;

        try {
            while (remain > 0) {
                SetCountdown(remain);
                await UniTask.Delay(TimeSpan.FromSeconds(1), ignoreTimeScale: true, cancellationToken: ct);
                // 그 사이에 버튼을 눌렀거나 닫혔으면 더 셀 이유가 없다.
                if (Started.Task.Status.IsCompleted()) return;
                remain--;
            }
            SetCountdown(0);
            Started.TrySetResult(true);
        } catch (OperationCanceledException) {
        }
    }

    // 남은 초를 그리고 튀는 연출을 넣는다. 색은 남은 비율로 섞어 0 에 가까울수록 임박한 색이 된다.
    private void SetCountdown(int remain) {
        // 숫자가 바뀌는 순간마다 한 번씩(5,4,3,2,1). 0 은 시작 시점이라 초읽기 소리를 내지 않는다.
        if (remain > 0) _sound?.PlaySfx(SfxKey.Countdown);

        if (countdownText != null) {
            countdownText.text = remain.ToString();
            float total = Mathf.Max(1, ArenaRules.MatchCountdownSeconds);
            countdownText.color = Color.Lerp(countdownUrgentColor, countdownCalmColor, remain / total);
        }

        if (countdownPunchTarget == null) return;
        _countdownTween?.Kill();
        countdownPunchTarget.localScale = Vector3.one;
        _countdownTween = countdownPunchTarget
            .DOPunchScale(Vector3.one * countdownPunch, countdownPunchDuration, vibrato: 1, elasticity: 0.5f)
            .SetUpdate(true);   // 타임스케일 0 에서도 도는 연출
    }

    // 내 편성은 스토어의 성장치를 그대로 읽어 별/레벨까지 보여 준다.
    private void BindMyMembers(MainHudContext ctx, System.Collections.Generic.IReadOnlyList<int> deck) {
        if (myMembers == null) return;
        for (int i = 0; i < myMembers.Length; i++) {
            var slot = myMembers[i];
            if (slot == null) continue;
            if (deck == null || i >= deck.Count) { slot.Clear(); continue; }

            int charId = deck[i];
            var progress = ctx != null ? ctx.Store.GetProgress(charId) : null;
            slot.Bind(ctx != null ? ctx.Appearance.Mine(charId) : default,
                      progress != null ? progress.StarLevel : 0,
                      progress != null ? progress.Level : 0,
                      progress != null && progress.LimitBroken);
        }
    }

    // 상대 편성은 서버가 준 원본(UserDeckDto)에 별/레벨이 함께 실려 온다.
    private void BindOpponentMembers(MainHudContext ctx, UserDeckDto deck) {
        if (opponentMembers == null) return;
        for (int i = 0; i < opponentMembers.Length; i++) {
            var slot = opponentMembers[i];
            if (slot == null) continue;
            if (deck == null || i >= deck.Members.Count) { slot.Clear(); continue; }

            var m = deck.Members[i];
            // 상대가 끼운 코스튬은 서버가 준 id 로 해석한다(내 화면에서도 그 코스튬 일러스트로 보인다).
            slot.Bind(ctx != null ? ctx.Appearance.Other(m.CharDataId, m.CostumeId) : default,
                      m.StarLevel, m.Level, m.LimitBroken);
        }
    }

    private void StartPulse() {
        StopPulse();
        if (searchingPulseTarget == null) return;
        searchingPulseTarget.localScale = Vector3.one;
        _pulse = searchingPulseTarget
            .DOScale(Vector3.one * pulseScale, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);   // 타임스케일 0 에서도 도는 연출(팝업 위 대기 표시)
    }

    private void StopPulse() {
        _pulse?.Kill();
        _pulse = null;
        if (searchingPulseTarget != null) searchingPulseTarget.localScale = Vector3.one;
    }

    // 전투 시작 외의 경로(배경 탭/연결 끊김 정리 등)로 닫혀도 대기 중인 호출부가 멈추지 않도록 반드시 풀어 준다.
    protected override void OnClosed() {
        StopPulse();
        StopCountdownTween();
        // 대기 중인 신호를 전부 확정한다(TrySet 이라 이미 값이 있으면 그대로 둔다).
        // 그러지 않으면 기다리던 호출부가 영원히 멈춘다.
        _cancelTcs?.TrySetResult(true);   // 닫힘 = 취소로 본다
        _startTcs?.TrySetResult(false);
        _startTcs = null;
    }

    private void StopCountdownTween() {
        _countdownTween?.Kill();
        _countdownTween = null;
        if (countdownPunchTarget != null) countdownPunchTarget.localScale = Vector3.one;
    }

    private void OnDestroy() {
        StopPulse();
        StopCountdownTween();
    }
}
