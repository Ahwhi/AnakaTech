using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 짧은 안내 메시지(Overlay 레이어). 페이드 인 -> 유지 -> 페이드 아웃을 자동 수행한다.
/// 입력을 막지 않으며(blocksRaycasts off), 새 토스트가 오면 이전 것을 대체한다.
/// UIRoot 의 자식으로 씬에 배치하고, 표시는 UIRoot.ShowToast 를 통해서만 한다.
///
/// 활성 상태를 스스로 관리한다(표시 때 Root 를 켜고, 시퀀스 종료 후 끈다). Root 는 activationRoot(비우면 자신)이며,
/// 토스트를 전용 Canvas 아래 두고 그 Canvas 를 꺼둔 채 시작하려면 activationRoot 에 그 Canvas 를 지정한다.
/// 따라서 하이어라키에서 활성/비활성 어느 상태로 시작하든 동일하게 동작한다.
/// </summary>
public class ToastView : MonoBehaviour {
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [Tooltip("표시할 때 켜고 끌 대상(보통 토스트 전용 Canvas). 비우면 이 오브젝트 자신을 켜고 끈다. 부모 Canvas 가 꺼져 있으면 자식만 켜도 렌더되지 않으므로, 그 Canvas 를 여기에 지정한다.")]
    [SerializeField] private GameObject activationRoot;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private Sequence _sequence;

    private GameObject Root => activationRoot != null ? activationRoot : gameObject;

    // 비활성으로 시작하면 Awake 는 첫 Show 의 SetActive(true) 시점에 호출된다.
    private void Awake() {
        if (canvasGroup != null) {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void Show(string message, float duration) {
        if (canvasGroup == null) return;

        // 비활성으로 시작했어도 여기서 켠다. 전용 Canvas 를 꺼둔 채 시작한 경우 그 Canvas(Root)를 켜면
        // 자식인 이 오브젝트도 함께 활성화되며 Awake 가 이 시점에 호출된다.
        Root.SetActive(true);
        if (messageText != null) messageText.text = message;
        _sequence?.Kill();
        canvasGroup.alpha = 0f;
        _sequence = DOTween.Sequence().SetUpdate(true)
            .Append(canvasGroup.DOFade(1f, fadeInDuration))
            .AppendInterval(duration)
            .Append(canvasGroup.DOFade(0f, fadeOutDuration))
            .OnComplete(() => Root.SetActive(false));
    }
}
