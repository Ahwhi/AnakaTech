using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 전환 전용 전체 화면 로딩(System 레이어). 진행도 막대와 팁 문구를 함께 보여 준다.
///
/// 팝업들이 쓰는 전역 오버레이(LoadingOverlay)와 일부러 분리했다. 그쪽은 "잠깐 기다려라"를 알리는
/// 참조 카운트 오버레이라 수십 군데에서 겹쳐 부르는데, 씬 전환은 한 번에 하나뿐이고 몇 초를 붙잡아
/// 두므로 진행도와 읽을거리가 필요하다. 한 컴포넌트에 두 성격을 섞으면 팝업 로딩에까지
/// 진행도 막대가 딸려 다니게 되므로 화면 자체를 나눈다.
///
/// UIRoot 의 자식(Canvas_System)으로 두고, 표시는 UIRoot.ShowSceneLoading 을 통해서만 한다.
/// GameObject 활성 상태를 스스로 관리하므로 하이어라키에서 꺼 둔 채로 시작해도 된다.
/// </summary>
public sealed class SceneLoadingScreen : MonoBehaviour {
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Progress")]
    [Tooltip("진행도 막대. 0~1 로 채워진다. 비우면 진행도를 그리지 않는다.")]
    [SerializeField] private Slider progressBar;
    [Tooltip("선택. 진행도를 채우는 Image(Filled 타입). 막대 대신 쓰거나 함께 써도 된다.")]
    [SerializeField] private Image progressFill;
    [Tooltip("선택. 진행도 백분율 문구(예: '72%').")]
    [SerializeField] private TMP_Text progressText;

    [Header("Label")]
    [Tooltip("로딩 문구. 뒤에 점이 하나씩 늘어난다(NOW LOADING. -> .. -> ...). 비우면 문구를 쓰지 않는다.")]
    [SerializeField] private TMP_Text loadingLabelText;
    [Tooltip("점을 뺀 본문. 여기 적은 글자 뒤에 점이 붙는다.")]
    [SerializeField] private string loadingLabel = "NOW LOADING";
    [Tooltip("점이 하나 늘어나는 간격(초).")]
    [SerializeField] private float dotInterval = 0.35f;
    [Tooltip("점의 최대 개수. 이만큼 찬 뒤에는 다시 한 개로 돌아간다.")]
    [SerializeField] private int maxDots = 3;

    [Header("Tip")]
    [Tooltip("로딩 중에 하나를 골라 보여 줄 팁 문구들. 여기 적은 것 중에서 무작위로 하나가 뜬다. " +
             "비워 두면 팁 문구 오브젝트를 끈다(타이틀 화면의 팁과는 별개 목록이다).")]
    [TextArea(2, 4)]
    [SerializeField] private string[] tips;
    [Tooltip("팁 문구를 표시할 텍스트. 비우면 팁을 쓰지 않는다.")]
    [SerializeField] private TMP_Text tipText;

    // 겹쳐 부른 요청 수. 모두 끝나야 닫힌다(씬 로드 + 그 씬의 초기 데이터 로드).
    private int _refCount;

    private int _lastTipIndex = -1;

    // 점 애니메이션 상태. 화면이 꺼져 있는 동안은 Update 가 돌지 않으므로 따로 멈출 필요가 없다.
    private int _dotCount = 1;
    private float _dotTimer;

    private void Awake() {
        if (canvasGroup != null) {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 화면을 띄운다. 진행도와 팁은 처음 뜰 때만 새로 잡는다(로딩 중에 바뀌면 읽다 만다).
    ///
    /// 참조 카운트를 쓰는 이유: 씬 전환은 "씬 파일 로드"로 끝나지 않는다. 새 씬이 올라온 뒤
    /// 그 씬의 매니저가 자기 데이터를 받아 오는 동안도 유저에게는 같은 전환이다. 둘을 따로 세면
    /// 그 사이에 화면이 한 번 닫혔다 다시 열려 깜빡이고, 다른 로딩 화면이 겹쳐 보인다.
    /// </summary>
    public void Show() {
        _refCount++;
        if (_refCount != 1) return;

        SetProgress(0f);
        PickTip();

        // 점은 언제나 하나에서 시작한다(직전 전환이 끝난 개수에서 이어지면 어색하다).
        _dotCount = 1;
        _dotTimer = 0f;
        ApplyLabel();

        gameObject.SetActive(true);
        FadeTo(1f);
    }

    // 점을 하나씩 늘렸다가 되돌린다. 씬 전환 중에는 시간이 멈춰 있을 수 있어 실시간으로 센다.
    private void Update() {
        if (loadingLabelText == null || dotInterval <= 0f) return;

        _dotTimer += Time.unscaledDeltaTime;
        if (_dotTimer < dotInterval) return;

        _dotTimer = 0f;
        _dotCount = _dotCount % Mathf.Max(1, maxDots) + 1;
        ApplyLabel();
    }

    private void ApplyLabel() {
        if (loadingLabelText == null) return;
        loadingLabelText.text = loadingLabel + new string('.', Mathf.Clamp(_dotCount, 0, Mathf.Max(1, maxDots)));
    }

    /// <summary>지금 화면을 덮고 있는지(요청이 하나라도 살아 있는지).</summary>
    public bool IsVisible => _refCount > 0;

    /// <summary>진행도 표시(0~1). 씬 로딩이 알려 주는 값을 그대로 받는다.</summary>
    public void SetProgress(float progress01) {
        float value = Mathf.Clamp01(progress01);

        if (progressBar != null) progressBar.value = value;
        if (progressFill != null) progressFill.fillAmount = value;
        if (progressText != null) progressText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    public void Hide() {
        if (_refCount == 0) return;
        _refCount--;
        if (_refCount != 0) return;

        // 사라지기 직전에는 다 찬 상태로 보여 준다(90%에서 툭 끊기는 것처럼 보이지 않게).
        SetProgress(1f);
        FadeTo(0f, deactivateOnComplete: true);
    }

    // 팁은 매번 새로 고른다. 바로 직전에 본 것은 피해서, 연달아 전환할 때 같은 문구가 반복되지 않게 한다
    // (문구가 하나뿐이면 피할 것이 없으므로 그대로 쓴다).
    private void PickTip() {
        if (tipText == null) return;

        bool has = tips != null && tips.Length > 0;
        tipText.gameObject.SetActive(has);
        if (!has) return;

        int index = Random.Range(0, tips.Length);
        if (tips.Length > 1 && index == _lastTipIndex) index = (index + 1) % tips.Length;

        _lastTipIndex = index;
        tipText.text = tips[index];
    }

    private void FadeTo(float target, bool deactivateOnComplete = false) {
        if (canvasGroup == null) return;

        canvasGroup.blocksRaycasts = target > 0f;
        canvasGroup.DOKill();
        // 씬 전환 중에는 시간이 멈춰 있을 수 있으므로 실시간으로 돌린다(로딩 오버레이와 같은 이유).
        var tween = canvasGroup.DOFade(target, fadeDuration).SetUpdate(true);
        if (deactivateOnComplete) tween.OnComplete(() => gameObject.SetActive(false));
    }
}
