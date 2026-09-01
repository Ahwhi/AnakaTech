using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using VContainer;
using static Enums;

/// <summary>
/// 타이틀 로딩 스플래시(씬 전용 UI, 하이어라키 배치).
/// 부트 진행 상태(IBootStatus)를 구독해 실제 모바일 게임처럼 단계 문구 + 점 애니메이션,
/// 다운로드 진행률 바, 버전, 팁을 표시한다. 부트 로직 자체는 갖지 않고 표시만 담당한다.
///
/// 상태별 동작:
///   - Initializing ~ DownloadingAddressable : 로딩 패널 표시(다운로드 단계에서만 진행률 바 노출)
///   - LoggingIn  : 로딩 패널을 숨긴다(LoginPanel 이 로그인 버튼을 대신 띄움)
///   - Ready      : "환영합니다" 후 페이드 아웃
///   - Failed     : 실패 문구 표시
/// </summary>
public class TitleSceneManager : MonoBehaviour {
    [Header("Loading Panel")]
    [SerializeField] private CanvasGroup loadingGroup;
    [SerializeField] private TextMeshProUGUI stateText;

    [Header("Download Progress")]
    [SerializeField] private GameObject progressGroup;        // 바 + 퍼센트 묶음(다운로드 때만 표시)
    [SerializeField] private Slider progressSlider;           // 진행 슬라이더(0~1)
    [SerializeField] private TextMeshProUGUI percentText;         // "123.4 MB / 456.7 MB (27%)" (받은/전체(퍼센트))

    [Header("Download Confirm")]
    [Tooltip("다운로드 동의 인씬 팝업. 어드레서블 다운로드 전에 떠야 하므로 씬 하이어라키에 배치한다.")]
    [SerializeField] private DownloadConfirmPanel downloadConfirmPanel;

    [Header("Press To Start")]
    [Tooltip("화면 전체를 덮는 클릭 영역(Raycast Target). 빈 곳 클릭으로 게임 시작. 버튼/패널은 이 위에 둔다.")]
    [SerializeField] private PressToStartArea pressToStartArea;
    [SerializeField] private CanvasGroup pressToStartGroup;   // "터치하여 게임시작" 묶음(로딩 완료 후 표시)
    [SerializeField] private float blinkMinAlpha = 0.2f;
    [SerializeField] private float blinkDuration = 0.8f;       // 한쪽 방향(밝아짐/어두워짐) 시간

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private string[] tips;

    [Header("Scene Flow")]
    // 하드코딩 대신 인스펙터에서 이동할 씬을 지정한다. Addressable 로 등록된 씬 에셋을 드래그한다.
    [SerializeField] private AssetReference nextScene;

    [Tooltip("인트로 스토리를 재생할 씬(StoryScene). 아직 인트로를 보지 않은 계정만 이 씬을 먼저 거친다. " +
             "비워 두면 인트로 없이 곧바로 nextScene 으로 넘어간다.")]
    [SerializeField] private AssetReference storyScene;

    [Header("Tuning")]
    [SerializeField] private float dotIntervalSeconds = 0.35f;
    [SerializeField] private float tipIntervalSeconds = 3f;
    [SerializeField] private float progressTweenSeconds = 0.3f;
    [SerializeField] private float readyHoldSeconds = 0.6f;

    private string _baseMessage = string.Empty;
    private int _dotCount;
    private bool _starting;

    private IBootStatus _boot;
    private IBootFlow _bootFlow;
    private IUIService _ui;
    private ISceneService _scene;
    private UIPopupCatalog _popups;
    private IVersionInfo _version;
    private IDownloadConsentPresenter _consent;
    private IStoryService _story;

    [Inject]
    public void Construct(IBootStatus boot, IBootFlow bootFlow, IUIService ui, ISceneService scene, UIPopupCatalog popups, IVersionInfo version, IDownloadConsentPresenter consent, IStoryService story) {
        _boot = boot;
        _bootFlow = bootFlow;
        _ui = ui;
        _scene = scene;
        _popups = popups;
        _version = version;
        _consent = consent;
        _story = story;
    }

    private void Start() {
        // 표시 버전은 ReactiveProperty 구독으로 반영한다. 처음엔 v{MAJOR}.{MINOR}, 버전 체크 후 v{MAJOR}.{MINOR}.{콘텐츠버전}으로 자동 갱신.
        if (versionText != null)
            _version.Display.Subscribe(v => versionText.text = v).AddTo(this);
        if (progressGroup != null) progressGroup.SetActive(false);
        HidePressToStart();
        SetProgress(0f, instant: true);

        // 빈 화면 클릭은 UI 영역(PressToStartArea)이 EventSystem 레이캐스트로 받는다.
        // 위에 있는 버튼/패널이 자기 클릭을 먼저 소비하므로 별도 분기 없이 깔끔하게 분리된다.
        if (pressToStartArea != null) pressToStartArea.Clicked += OnPressToStart;

        StartDotAnimation();
        StartTipRotation();

        // 타이틀 진입 시 부트를 (재)실행한다. 앱 첫 시작이든 재접속 복귀든 항상 여기서 시작된다.
        // 구독보다 "먼저" 실행하는 게 중요하다: RunAsync 는 첫 await 이전에 ResetForRerun 으로
        // 부트 상태를 None 으로 동기 초기화한다. 이전 부트가 Ready 로 끝났던 값이 BootStatus(싱글톤
        // ReactiveProperty)에 남아 있는데, 구독을 먼저 하면 구독 즉시 그 잔여 Ready 가 흘러나와
        // "터치하여 게임시작"이 잘못 노출되고(끊긴 상태에서도 씬 전환 가능) 버그가 된다.
        _bootFlow.RunAsync().Forget();

        // ReactiveProperty 는 구독 즉시 현재 값을 흘려주므로 별도의 "현재 상태 1회 반영"이 필요 없다.
        // AddTo(this) 로 이 오브젝트 파괴 시 자동 해지되어 수동 해제 코드도 사라진다.
        _boot.State.Subscribe(OnStateChanged).AddTo(this);
        _boot.Download.Subscribe(OnDownload).AddTo(this);

        // 다운로드 동의 요청(부트 스텝이 용량 알림) -> 인씬 팝업으로 물어보고 결과를 돌려준다.
        _consent.Requested.Subscribe(OnDownloadConsentRequested).AddTo(this);
    }

    // 다운로드 동의 요청: 용량/예상시간 팝업을 띄우고 사용자의 선택을 게이트에 응답한다.
    private void OnDownloadConsentRequested(long sizeBytes) => ShowDownloadConsentAsync(sizeBytes).Forget();

    private async UniTaskVoid ShowDownloadConsentAsync(long sizeBytes) {
        if (downloadConfirmPanel == null) { _consent.Respond(true); return; }   // 패널 미할당 시 막힘 방지(자동 동의)
        bool agreed = await downloadConfirmPanel.ShowAsync(sizeBytes);
        _consent.Respond(agreed);
    }

    // ── 상태 ────────────────────────────────────────────────
    private void OnStateChanged(BootState state) {
        switch (state) {
            case BootState.LoggingIn:
                // 로그인 게이트: 로딩 패널을 숨기고 LoginPanel 이 버튼을 띄우게 한다.
                SetLoadingVisible(false);
                break;

            case BootState.AgreeingTerms:
                // 약관 동의 게이트: 동의 팝업이 화면을 맡으므로 로딩 패널은 계속 숨겨 둔다.
                SetLoadingVisible(false);
                break;

            case BootState.Ready:
                _baseMessage = state.ToMessage();
                RefreshStateText();
                HideAfterReady();
                break;

            case BootState.Failed:
                SetLoadingVisible(true);
                // 실패 사유(점검/업데이트/연결 실패)를 명확히 표시한다. 일반 오류 문구는 분류되지 않은 예외에만 나온다.
                _baseMessage = _boot.FailReason.ToMessage();
                RefreshStateText();
                if (progressGroup != null) progressGroup.SetActive(false);
                ShowFailurePopupAsync().Forget();
                break;

            default:
                SetLoadingVisible(true);
                _baseMessage = state.ToMessage();
                RefreshStateText();
                if (progressGroup != null)
                    progressGroup.SetActive(state == BootState.DownloadingAddressable);
                break;
        }
    }

    private void OnDownload(DownloadInfo info) {
        SetProgress(info.Percent);
        if (percentText == null) return;
        // 받은/전체 (퍼센트)를 한 줄로 합쳐 표시. 받을 게 없으면(용량 0) 퍼센트만.
        percentText.text = info.TotalBytes > 0
            ? $"{DownloadInfo.FormatBytes(info.DownloadedBytes)} / {DownloadInfo.FormatBytes(info.TotalBytes)} ({info.Percent:P0})"
            : $"{info.Percent:P0}";
    }

    // 부트 실패 시 사유별 안내 팝업. 연결 실패는 재시도하면 부트를 다시 돌린다.
    private async UniTaskVoid ShowFailurePopupAsync() {
        var popup = await _ui.ShowPopupAsync<ConfirmPopup>(_popups.ConfirmPopup);

        if (_boot.FailReason == BootFailReason.ForceUpdate) {
            await popup.ShowAsync("업데이트 필요", "최신 버전으로 업데이트가 필요합니다.");
            // TODO: 스토어로 이동 또는 앱 종료
            return;
        }

        if (_boot.FailReason == BootFailReason.Maintenance) {
            // 점검은 재시도하지 않고 안내만 한다(확인 시 팝업만 닫힘). 점검 해제 후 사용자가 앱을 다시 켜서 진입한다.
            await popup.ShowAsync("서버 점검", "현재 서버 점검 중입니다.\n잠시 후 다시 시도해주세요.");
            return;
        }

        // 연결 실패만 재시도를 제공한다. 확인 시 부트를 다시 돌린다.
        // 실패한 단계를 함께 보여 준다. 이 사유는 응답 시간 초과와 예상 못 한 오류를 함께 묶으므로,
        // 단계 정보가 없으면 네트워크 문제가 아닌 경우(자산 로드 실패 등)에도 "서버 연결"만 들여다보게 된다.
        string detail = string.IsNullOrEmpty(_boot.FailMessage) ? string.Empty : $"\n({_boot.FailMessage})";
        await popup.ShowAsync("연결 실패", $"서버에 연결할 수 없습니다.{detail}\n확인을 누르면 다시 시도합니다.");
        _bootFlow.RunAsync().Forget();
    }

    // ── 진행률 ──────────────────────────────────────────────
    private void SetProgress(float progress, bool instant = false) {
        progress = Mathf.Clamp01(progress);
        if (progressSlider == null) return;

        progressSlider.DOKill();
        if (instant) progressSlider.value = progress;
        else progressSlider.DOValue(progress, progressTweenSeconds).SetUpdate(true);
    }

    // ── 문구 / 애니메이션 ───────────────────────────────────
    private void StartDotAnimation() {
        Observable.Interval(TimeSpan.FromSeconds(dotIntervalSeconds))
            .Subscribe(_ => {
                _dotCount = (_dotCount + 1) % 4;
                RefreshStateText();
            })
            .AddTo(this);
    }

    private void StartTipRotation() {
        if (tipText == null || tips == null || tips.Length == 0) return;

        tipText.text = tips[0];
        Observable.Interval(TimeSpan.FromSeconds(tipIntervalSeconds))
            .Subscribe(i => tipText.text = tips[(int)(i % tips.Length)])
            .AddTo(this);
    }

    private void RefreshStateText() {
        if (stateText == null) return;
        stateText.text = string.IsNullOrEmpty(_baseMessage)
            ? string.Empty
            : _baseMessage + new string('.', _dotCount);
    }

    // ── 표시 토글 ───────────────────────────────────────────
    private void SetLoadingVisible(bool on) {
        if (loadingGroup == null) return;
        loadingGroup.DOKill();
        loadingGroup.blocksRaycasts = on;
        loadingGroup.alpha = on ? 1f : 0f;
    }

    private void HideAfterReady() {
        if (loadingGroup == null) {
            ShowPressToStart();
            return;
        }
        loadingGroup.DOKill();
        loadingGroup.blocksRaycasts = false;
        DOTween.Sequence().SetUpdate(true)
            .AppendInterval(readyHoldSeconds)
            .Append(loadingGroup.DOFade(0f, 0.4f))
            .OnComplete(ShowPressToStart);   // 로딩 패널이 사라진 뒤 "터치하여 게임시작" 노출
    }

    // ── 터치하여 게임시작 ───────────────────────────────────
    private void HidePressToStart() {
        if (pressToStartArea != null) pressToStartArea.gameObject.SetActive(false);
        if (pressToStartGroup == null) return;
        pressToStartGroup.DOKill();
        pressToStartGroup.alpha = 0f;
        pressToStartGroup.gameObject.SetActive(false);
    }

    private void ShowPressToStart() {
        if (pressToStartArea != null) pressToStartArea.gameObject.SetActive(true);
        if (pressToStartGroup != null) {
            pressToStartGroup.gameObject.SetActive(true);
            pressToStartGroup.DOKill();
            // 1 -> blinkMinAlpha 를 왕복하며 깜빡인다(timeScale 영향 없이 동작).
            pressToStartGroup.alpha = 1f;
            pressToStartGroup.DOFade(blinkMinAlpha, blinkDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }
    }

    private void OnPressToStart() => StartGameAsync().Forget();

    // 첫 터치에만 반응하고(중복 전환 방지), 깜빡임/입력을 끈 뒤 다음 씬으로 전환한다.
    private async UniTaskVoid StartGameAsync() {
        if (_starting) return;
        // 클릭은 EventSystem 레이캐스트로 들어오므로 버튼/패널이 자기 클릭을 먼저 소비한다(빈 화면만 여기로 옴).
        // 팝업도 레이캐스트를 막아주지만, 안전망으로 한 번 더 확인한다.
        if (_ui != null && _ui.HasOpenPopup) return;
        if (nextScene == null || !nextScene.RuntimeKeyIsValid()) {
            Debug.LogError("[TitleSceneManager] nextScene 이 인스펙터에 할당되지 않았습니다.");
            return;
        }
        _starting = true;

        if (pressToStartArea != null) pressToStartArea.gameObject.SetActive(false);
        if (pressToStartGroup != null) pressToStartGroup.DOKill();

        // 인트로를 아직 보지 않았으면 스토리 씬을 먼저 거친다. 스토리 씬이 재생을 마친 뒤 nextScene 으로 넘겨 주므로
        // "인트로가 끝나야 게임이 시작된다"는 규칙이 씬 흐름 자체로 보장된다.
        if (await ShouldPlayIntroAsync()) {
            StoryPlaybackRequest.Request(StoryKeys.Intro, nextScene);
            await _scene.LoadAddressableAsync(storyScene.RuntimeKey);
            return;
        }

        await _scene.LoadAddressableAsync(nextScene.RuntimeKey);
    }

    /// <summary>
    /// 인트로를 재생해야 하는지. 계정당 1회이므로 판단 근거는 서버가 가진 "본 스토리 목록"뿐이다
    /// (기기에 남기면 재설치나 기기 변경으로 다시 나온다).
    ///
    /// 조회에 실패하면 StoryService 가 목록을 비우므로 인트로를 한 번 더 보게 된다.
    /// 봐야 할 인트로를 건너뛰는 것보다 이쪽이 낫다(스토리를 못 본 채 게임이 시작되면 맥락을 잃는다).
    /// </summary>
    private async UniTask<bool> ShouldPlayIntroAsync() {
        if (storyScene == null || !storyScene.RuntimeKeyIsValid()) return false;

        await _story.RefreshAsync();
        return !_story.IsCompleted(StoryKeys.Intro);
    }

    private void OnDestroy() {
        if (pressToStartArea != null) pressToStartArea.Clicked -= OnPressToStart;
    }
}
