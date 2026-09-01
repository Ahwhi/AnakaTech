using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 전역 로딩 오버레이(System 레이어). 참조 카운트로 동작해
/// 동시에 여러 작업이 로딩을 요청해도 모두 끝나야 닫힌다.
/// 화면 전체를 덮어 입력을 막는 블로커 역할도 한다(blocksRaycasts).
/// UIRoot 의 자식으로 씬에 배치하며, 표시는 UIRoot.ShowLoading 을 통해서만 한다.
///
/// GameObject 활성 상태를 스스로 관리한다(표시 때 켜고, 페이드 아웃 완료 후 끈다).
/// 따라서 하이어라키에서 활성/비활성 어느 상태로 시작하든 동일하게 동작한다.
/// </summary>
public class LoadingOverlay : MonoBehaviour {
    /// <summary>
    /// 문구를 지정하지 않은 요청이 쓰는 기본 문구.
    /// 문구를 남겨 두면 직전 요청("로그인 중입니다..." 등)의 글자가 다음 로딩에 그대로 남아 상황과 어긋난다.
    /// 그래서 문구 없는 요청은 반드시 이 값으로 되돌린다.
    /// </summary>
    public const string DefaultMessage = "불러오는 중입니다...";

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private RectTransform spinner;       // 선택: 회전 스피너
    [SerializeField] private float spinSpeed = 220f;      // deg/sec
    [SerializeField] private float fadeDuration = 0.15f;

    private int _refCount;

    /// <summary>지금 화면을 덮고 있는지(요청이 하나라도 살아 있는지). 뒤로가기 등 전역 입력이 참고한다.</summary>
    public bool IsVisible => _refCount > 0;

    // 비활성으로 시작하면 Awake 는 첫 Show 의 SetActive(true) 시점에 호출된다.
    // 여기서는 초기 숨김 값만 세팅하고, 활성/비활성 토글은 Show/Hide 가 담당한다.
    private void Awake() {
        if (canvasGroup != null) {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void Update() {
        if (_refCount > 0 && spinner != null)
            spinner.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
    }

    public void Show(string message) {
        // 문구는 요청마다 반드시 다시 정한다(빈 값이면 기본 문구). 겹쳐 뜬 요청 중에는 마지막 문구가 남는데,
        // 그 상태에서 참조가 하나 줄어도 이전 문구로 되돌리지는 않는다(어차피 곧 닫히거나 다음 요청이 다시 정한다).
        if (messageText != null) messageText.text = string.IsNullOrEmpty(message) ? DefaultMessage : message;
        _refCount++;
        if (_refCount != 1) return;

        gameObject.SetActive(true);   // 비활성으로 시작했어도 여기서 켠다
        FadeTo(1f);
    }

    public void Hide() {
        if (_refCount == 0) return;
        _refCount--;
        if (_refCount == 0) FadeTo(0f, deactivateOnComplete: true);
    }

    private void FadeTo(float target, bool deactivateOnComplete = false) {
        if (canvasGroup == null) return;
        canvasGroup.blocksRaycasts = target > 0f;
        canvasGroup.DOKill();
        var tween = canvasGroup.DOFade(target, fadeDuration).SetUpdate(true);
        if (deactivateOnComplete) tween.OnComplete(() => gameObject.SetActive(false));
    }
}
