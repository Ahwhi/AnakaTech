using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스토리(연출 대화) 재생 화면. 대사를 한 글자씩 드러내고, 화면을 누르면 "다 보여주기 -> 다음 줄"로 넘어간다.
/// 재생이 끝날 때까지 await 로 기다릴 수 있어, 스토리가 끝나야 다음 흐름이 이어진다는 규칙이 호출부에 그대로 드러난다.
///
/// 이 뷰는 "대사 목록을 그리는 일"만 한다. 어떤 스토리를 재생하는지, 끝나면 어디로 가는지, 서버에 무엇을 보고하는지는
/// 전부 StorySceneManager 가 정한다(다른 곳에서 이 뷰를 그대로 재사용할 수 있게 하기 위함이다).
///
/// 표시 규약:
///  - 배경은 줄에 지정된 것이 있을 때만 바뀐다(비어 있으면 직전 배경 유지).
///  - 암전(BlackBackground)도 하나의 배경으로 취급해 같은 페이드 연출로 오간다.
///  - 화자가 없는 줄(나레이션)은 화자 묶음 자체를 끈다.
/// </summary>
public sealed class StoryPlayerView : MonoBehaviour {
    [Header("Background")]
    [Tooltip("배경 이미지. 대사에 배경이 지정된 줄에서만 바뀐다.")]
    [SerializeField] private Image backgroundImage;
    [Tooltip("배경이 바뀔 때의 페이드 시간(초). 0 이면 즉시 교체한다.")]
    [SerializeField, Min(0f)] private float backgroundFadeSeconds = 0.25f;

    [Header("Speaker")]
    [Tooltip("화자 묶음(이름표 + 초상). 화자가 없는 줄에서는 통째로 꺼진다.")]
    [SerializeField] private GameObject speakerGroup;
    [SerializeField] private TMP_Text speakerNameText;
    [Tooltip("화자 초상(선택). 이름만 있는 화자도 있으므로 없으면 이 이미지만 끈다.")]
    [SerializeField] private Image speakerPortrait;

    [Header("Dialogue")]
    [SerializeField] private TMP_Text dialogueText;
    [Tooltip("한 글자가 나타나는 간격(초). 작을수록 빠르다.")]
    [SerializeField, Min(0f)] private float charIntervalSeconds = 0.03f;
    [Tooltip("한 줄이 다 나온 뒤 켜지는 '다음' 표시(선택). 화면을 눌러야 넘어간다는 신호다.")]
    [SerializeField] private GameObject nextIndicator;

    [Header("Input")]
    [Tooltip("화면 전체를 덮는 진행 버튼. 누르면 '다 보여주기 -> 다음 줄' 순으로 진행한다.")]
    [SerializeField] private Button advanceButton;
    [Tooltip("건너뛰기 버튼. 스토리가 건너뛰기를 허용하지 않으면 자동으로 꺼진다.")]
    [SerializeField] private Button skipButton;

    /// <summary>
    /// 건너뛰기 버튼을 눌렀다. 정말 건너뛸지 물어보는 것은 이 뷰의 일이 아니므로(팝업은 화면 밖의 관심사다)
    /// 요청만 알리고, 확정은 구독자가 Skip 을 불러 준다.
    /// </summary>
    public event Action SkipRequested;

    // 지금 줄을 "다 보여달라"는 요청. 화면을 누르면 서고, 한 글자씩 그리던 루프가 이 값을 보고 즉시 끝낸다.
    private bool _revealRequested;
    // 다 나온 줄에서 화면을 눌렀다는 신호. 다음 줄로 넘어가는 대기를 푼다.
    private bool _advanceRequested;
    // 건너뛰기 확정. 남은 줄을 그리지 않고 재생을 끝낸다.
    private bool _skipConfirmed;

    // 지금 배경이 암전 상태인지. 암전은 "스프라이트 없음"으로 표현되는데, 대사에서 스프라이트를 비우는 것은
    // "직전 배경 유지"를 뜻하므로 둘을 구분할 별도의 상태가 필요하다.
    private bool _blackBackground;
    // 프리팹에 지정된 배경 이미지의 원래 색조. 암전에서 일반 배경으로 돌아올 때 이 값으로 되돌린다.
    private Color _baseBackgroundColor = Color.white;

    private void Awake() {
        if (backgroundImage != null) _baseBackgroundColor = backgroundImage.color;
        if (advanceButton != null) advanceButton.onClick.AddListener(OnAdvance);
        if (skipButton != null) skipButton.onClick.AddListener(() => SkipRequested?.Invoke());
        if (nextIndicator != null) nextIndicator.SetActive(false);
    }

    /// <summary>건너뛰기를 확정한다. 남은 줄을 그리지 않고 재생이 끝난다.</summary>
    public void Skip() => _skipConfirmed = true;

    /// <summary>
    /// 스토리를 처음부터 끝까지 재생한다. 건너뛰기로 끝나도 정상 종료이며, 어느 쪽이든 "봤다"로 친다
    /// (건너뛰기는 유저가 내용을 확인하지 않겠다고 고른 것이지 재생이 실패한 것이 아니다).
    /// </summary>
    public async UniTask PlayAsync(StoryData story, CancellationToken ct) {
        if (story == null || !story.IsPlayable) return;

        _skipConfirmed = false;
        if (skipButton != null) skipButton.gameObject.SetActive(story.Skippable);

        var lines = story.Lines;
        for (int i = 0; i < lines.Count; i++) {
            if (_skipConfirmed) break;

            var line = lines[i];
            if (line == null) continue;

            await ApplyBackgroundAsync(line, ct);
            ApplySpeaker(line);
            await RevealAsync(line.Text ?? string.Empty, ct);

            if (_skipConfirmed) break;
            await WaitForAdvanceAsync(ct);
        }

        if (nextIndicator != null) nextIndicator.SetActive(false);
    }

    // ── 배경 ────────────────────────────────────────────────
    /// <summary>
    /// 이 줄이 요구하는 배경으로 바꾼다. 암전과 일반 배경을 한 경로에서 다루므로 둘 사이를 오가도 연출이 같다.
    ///
    /// 배경 교체는 세 가지 경우뿐이다:
    ///   - 암전 지정      : 스프라이트를 비우고 색을 검게 한다(빈 Image 는 색만 남는다).
    ///   - 스프라이트 지정: 그 스프라이트로 바꾸고 색을 원래 색조로 되돌린다.
    ///   - 둘 다 없음     : 직전 배경을 그대로 유지한다(한 장면이 여러 줄로 이어지는 것이 보통이다).
    /// </summary>
    private async UniTask ApplyBackgroundAsync(StoryLine line, CancellationToken ct) {
        if (backgroundImage == null) return;

        bool toBlack = line.BlackBackground;
        var toSprite = toBlack ? null : line.Background;

        // 바뀌는 것이 없으면 연출도 하지 않는다(같은 배경에서 화면이 한 번 어두워졌다 밝아지면 장면이 끊겨 보인다).
        if (toBlack) {
            if (_blackBackground) return;
        } else {
            if (toSprite == null) return;
            if (!_blackBackground && backgroundImage.sprite == toSprite) return;
        }

        if (backgroundFadeSeconds <= 0f) {
            SetBackground(toSprite, toBlack);
            return;
        }

        // 같은 이미지를 어둡게 -> 교체 -> 밝게 하는 것으로 장면 전환을 표현한다.
        // 이미지 하나로 끝나므로 프리팹에 교차 페이드용 레이어를 따로 두지 않아도 된다.
        await FadeBackgroundAsync(0f, ct);
        SetBackground(toSprite, toBlack);
        await FadeBackgroundAsync(1f, ct);
    }

    // 스프라이트와 색조만 바꾼다(투명도는 페이드가 관리하므로 건드리지 않는다).
    // 암전은 스프라이트 없는 Image 를 검은색으로 칠하는 것이고, 일반 배경은 프리팹에 지정된 원래 색조로 돌린다.
    private void SetBackground(Sprite sprite, bool black) {
        backgroundImage.sprite = sprite;
        var tint = black ? Color.black : _baseBackgroundColor;
        backgroundImage.color = new Color(tint.r, tint.g, tint.b, backgroundImage.color.a);
        _blackBackground = black;
    }

    private async UniTask FadeBackgroundAsync(float targetAlpha, CancellationToken ct) {
        var from = backgroundImage.color;
        float start = from.a;
        float elapsed = 0f;
        float duration = backgroundFadeSeconds * 0.5f;

        while (elapsed < duration) {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            // 색조는 그 사이에 바뀔 수 있으므로(교체 직후) 매 프레임 현재 색에서 투명도만 갈아 끼운다.
            var now = backgroundImage.color;
            backgroundImage.color = new Color(now.r, now.g, now.b, Mathf.Lerp(start, targetAlpha, t));
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        var last = backgroundImage.color;
        backgroundImage.color = new Color(last.r, last.g, last.b, targetAlpha);
    }

    // ── 화자 ────────────────────────────────────────────────
    private void ApplySpeaker(StoryLine line) {
        bool hasSpeaker = line.HasSpeaker;
        if (speakerGroup != null) speakerGroup.SetActive(hasSpeaker);

        if (speakerNameText != null) speakerNameText.text = hasSpeaker ? line.SpeakerName ?? "" : "";
        if (speakerPortrait != null) {
            speakerPortrait.sprite = line.SpeakerPortrait;
            speakerPortrait.enabled = line.SpeakerPortrait != null;
        }
    }

    // ── 대사 ────────────────────────────────────────────────
    // 글자를 하나씩 드러낸다. 문자열을 잘라 붙이지 않고 maxVisibleCharacters 를 올리는 이유는
    // 줄바꿈 위치가 처음부터 확정돼 글이 늘어나며 줄이 밀리는 현상이 없기 때문이다.
    private async UniTask RevealAsync(string text, CancellationToken ct) {
        if (dialogueText == null) return;
        if (nextIndicator != null) nextIndicator.SetActive(false);

        _revealRequested = false;
        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int total = dialogueText.textInfo.characterCount;
        float elapsed = 0f;

        for (int shown = 0; shown < total; ) {
            if (_revealRequested || _skipConfirmed) break;
            ct.ThrowIfCancellationRequested();

            await UniTask.Yield(PlayerLoopTiming.Update, ct);

            // 프레임 시간이 글자 간격보다 길 수 있으므로 누적 시간으로 몇 글자를 더 드러낼지 정한다
            // (프레임당 1글자로 하면 저사양 기기에서 대사가 눈에 띄게 느려진다).
            elapsed += Time.unscaledDeltaTime;
            if (charIntervalSeconds <= 0f) { shown = total; }
            else {
                shown = Mathf.Min(total, Mathf.FloorToInt(elapsed / charIntervalSeconds));
            }
            dialogueText.maxVisibleCharacters = shown;
        }

        dialogueText.maxVisibleCharacters = total;
        _revealRequested = false;
        if (nextIndicator != null && !_skipConfirmed) nextIndicator.SetActive(true);
    }

    private async UniTask WaitForAdvanceAsync(CancellationToken ct) {
        _advanceRequested = false;
        await UniTask.WaitUntil(() => _advanceRequested || _skipConfirmed, cancellationToken: ct);
        _advanceRequested = false;
        if (nextIndicator != null) nextIndicator.SetActive(false);
    }

    // 화면 탭 한 번이 상황에 따라 다른 뜻을 갖는다: 그리는 중이면 "다 보여줘", 다 나왔으면 "다음 줄".
    // 상용 게임의 대화 연출이 공통으로 쓰는 규약이라 버튼을 둘로 나누지 않는다.
    private void OnAdvance() {
        if (dialogueText != null && dialogueText.maxVisibleCharacters < dialogueText.textInfo.characterCount) {
            _revealRequested = true;
            return;
        }
        _advanceRequested = true;
    }
}
