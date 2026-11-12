using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 짧은 안내 메시지(Overlay 레이어). 페이드 인 -> 유지 -> 페이드 아웃을 자동 수행한다.
/// 입력을 막지 않으며(blocksRaycasts off), 새 토스트가 오면 이전 것을 대체한다.
/// UIRoot 의 자식으로 씬에 배치하고, 표시는 UIRoot.ShowToast 를 통해서만 한다.
///
/// 활성 상태는 OverlayToastView 가 관리한다(표시 때 Root 를 켜고, 시퀀스 종료 후 끈다).
/// 토스트를 전용 Canvas 아래 두고 그 Canvas 를 꺼둔 채 시작하려면 activationRoot 에 그 Canvas 를 지정한다.
/// 따라서 하이어라키에서 활성/비활성 어느 상태로 시작하든 동일하게 동작한다.
/// </summary>
public class ToastView : OverlayToastView {
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    public void Show(string message, float duration) {
        if (canvasGroup == null) return;

        // 비활성으로 시작했어도 여기서 켠다. 전용 Canvas 를 꺼둔 채 시작한 경우 그 Canvas(Root)를 켜면
        // 자식인 이 오브젝트도 함께 활성화되며 Awake 가 이 시점에 호출된다.
        OpenRoot();
        if (messageText != null) messageText.text = message;
        Fade?.Kill();
        canvasGroup.alpha = 0f;
        Fade = DOTween.Sequence().SetUpdate(true)
            .Append(canvasGroup.DOFade(1f, fadeInDuration))
            .AppendInterval(duration)
            .Append(canvasGroup.DOFade(0f, fadeOutDuration))
            .OnComplete(CloseRoot);
    }
}
