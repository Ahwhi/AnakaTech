using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 팝업/뷰의 베이스. 페이드 인/아웃과 닫힘 대기를 공통 제공한다.
/// 프리팹에 CanvasGroup 이 반드시 있어야 하며(페이드 대상), 시작 시 숨김(alpha 0) 상태로 둔다.
/// Addressable 로 인스턴스화된 뷰의 해제 콜백은 UIRoot 가 BindCloseHandler 로 주입한다.
///
/// "그만 보겠다"는 요청(배경 탭 / 안드로이드 뒤로가기)은 여기서 한 경로(Dismiss)로 모은다.
/// 팝업마다 배경 버튼 필드를 따로 선언하고 리스너를 다시 다는 일이 없도록, 그 필드를 이 베이스가 갖는다.
/// 프리팹에서 배경 버튼을 할당한 팝업만 배경 탭으로 닫히고, 비워 둔 팝업은 닫히지 않는다(할당 = 사용).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class UIView : MonoBehaviour {
    [SerializeField] private float fadeDuration = 0.2f;

    [Tooltip("배경 탭으로 닫기(선택). 팝업 뒤를 덮는 배경 이미지(보통 Dim)를 그대로 끌어다 놓으면 " +
             "그 바깥을 눌러 닫을 수 있다. 클릭 처리는 코드가 붙이므로 Button 컴포넌트를 따로 달 필요가 없다. " +
             "비워 두면 이 팝업은 배경 탭으로 닫히지 않는다(할당 = 사용).")]
    [SerializeField] private Graphic tapToCloseArea;

    private CanvasGroup _canvasGroup;
    private Action _onClosed;                 // 인스턴스 해제(Addressable) 콜백
    private UniTaskCompletionSource _closedTcs;
    // 닫힘 연출(페이드)이 진행 중인지. 연출이 끝나야 실제 해제가 일어나므로 그 사이에 닫기 요청이
    // 한 번 더 들어올 수 있다(뒤로가기 연타, 닫기 버튼 더블 탭 등). 두 번 처리하면 연출이 겹치고
    // OnClosed 도 두 번 불린다.
    private bool _closing;
    // 페이드 외 추가 연출(슬라이드 등)을 담당하는 선택 컴포넌트들. 같은 오브젝트에 부착만 하면 자동 참여한다.
    private IViewTransitionElement[] _transitionElements;

    protected CanvasGroup CanvasGroup => _canvasGroup != null ? _canvasGroup : (_canvasGroup = GetComponent<CanvasGroup>());

    protected virtual void Awake() {
        // 설정(텍스트 세팅 등)이 끝나기 전 한 프레임 깜빡임을 막기 위해 숨김으로 시작.
        CanvasGroup.alpha = 0f;
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;

        // 부착된 추가 연출 컴포넌트들을 모아 숨김 시작 상태로 준비시킨다.
        _transitionElements = GetComponents<IViewTransitionElement>();
        foreach (var e in _transitionElements) e.OnViewPrepare();

        BindTapToClose();
    }

    // 배경 이미지를 눌러 닫는 배선. 실제 배선은 공용 창구(TapToClose)가 한다.
    // UIView 가 아닌 도킹 패널도 같은 창구를 쓰므로 "배경 탭으로 닫기"가 화면마다 다르게 구현되지 않는다.
    private void BindTapToClose() => TapToClose.Bind(tapToCloseArea, Dismiss);

    /// <summary>
    /// 지금 닫기 요청을 받아도 되는지. 기본은 언제나 가능.
    /// 연출이 끝나기 전에 닫히면 안 되는 뷰(가챠 결과 등)나 반드시 선택을 받아야 하는 뷰가 덮어쓴다.
    /// </summary>
    protected virtual bool CanDismiss => true;

    /// <summary>
    /// "그만 보겠다"는 요청(배경 탭 / 뒤로가기)의 단일 경로. CanDismiss 가 false 면 조용히 무시한다.
    /// 무시하는 것도 처리로 친다(뒤로가기가 뒤 팝업이나 게임 종료로 새어 나가지 않는다).
    /// </summary>
    public void Dismiss() {
        if (!CanDismiss) return;
        CloseAsync().Forget();
    }

    /// <summary>UIRoot 전용: 닫힐 때 인스턴스를 해제하도록 콜백을 건다.</summary>
    internal void BindCloseHandler(Action onClosed) => _onClosed = onClosed;

    /// <summary>페이드 인(+슬라이드 인). 내용 세팅 후 호출한다.</summary>
    public virtual async UniTask ShowAsync() {
        _closing = false;   // 재사용되는 내장 패널이 다시 열릴 때를 위해 초기화한다.
        gameObject.SetActive(true);
        CanvasGroup.interactable = true;
        CanvasGroup.blocksRaycasts = true;
        await TransitionAsync(true);
        OnShown();
    }

    /// <summary>
    /// 페이드 아웃(+슬라이드 아웃) 후 닫힘 처리.
    /// Addressable 로 띄운 뷰는 인스턴스를 해제하고, 부모 화면에 내장된(BindCloseHandler 미주입) 패널은
    /// 파괴 대신 비활성화해 다시 열 수 있게 재사용 가능한 상태로 남긴다.
    /// </summary>
    public async UniTask CloseAsync() {
        if (_closing) return;   // 이미 닫히는 중이면 그대로 둔다(연출/해제가 두 번 일어나지 않게).
        _closing = true;

        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
        await TransitionAsync(false);
        OnClosed();

        // 재사용(내장 패널) 시 다음 열림에서 새 대기 토큰을 만들도록 초기화한다.
        var tcs = _closedTcs;
        _closedTcs = null;
        tcs?.TrySetResult();

        if (_onClosed != null) _onClosed.Invoke();     // Addressable 인스턴스 해제.
        else gameObject.SetActive(false);              // 내장 패널: 비활성화로 숨김(재사용).
    }

    /// <summary>팝업이 닫힐 때까지 대기(결과 없는 단순 팝업용).</summary>
    public UniTask WaitUntilClosedAsync() {
        _closedTcs ??= new UniTaskCompletionSource();
        return _closedTcs.Task;
    }

    /// <summary>
    /// 안드로이드 뒤로가기가 이 뷰에 도착했을 때의 처리(BackKeyRouter -> IUIService 가 가장 위 팝업에만 전달한다).
    /// 기본 동작은 배경 탭과 같은 닫기 요청(Dismiss)이며, 이 지점에서 뒤로가기는 소비된다.
    ///
    /// 닫히기만 하면 되는 뷰는 CanDismiss 만 덮어쓰면 되고, 이 메서드는
    /// 자체 뒤로가기 대상이 있는 뷰(웹뷰 히스토리, 하위 페이지 등)만 덮어쓴다.
    /// </summary>
    public virtual void OnBackKey() => Dismiss();

    protected virtual void OnShown() { }
    protected virtual void OnClosed() { }

    // DOTween 비동기 브리지 의존 없이 OnComplete 로 안전하게 await. 페이드와 (있으면) 추가 연출을 함께 재생한다.
    private UniTask TransitionAsync(bool show) {
        var tcs = new UniTaskCompletionSource();
        var seq = DOTween.Sequence()
            .SetUpdate(true);   // 타임스케일 0 에서도 동작(로딩/일시정지 중 UI)
        seq.Join(CanvasGroup.DOFade(show ? 1f : 0f, fadeDuration));
        if (_transitionElements != null)
            foreach (var e in _transitionElements) e.AppendTransition(seq, show, fadeDuration);
        seq.OnComplete(() => tcs.TrySetResult());
        return tcs.Task;
    }
}

/// <summary>
/// UIView 의 표시/숨김 트랜지션에 추가 연출을 참여시키는 선택 컴포넌트용 인터페이스.
/// 슬라이드 등 필요한 연출을 별도 컴포넌트로 만들어 뷰 오브젝트에 부착하면(예: UISlideTransition)
/// UIView 가 자동으로 감지해 페이드와 함께 재생한다. 뷰마다 기능을 상속으로 떠안지 않도록 컴포지션한다.
/// </summary>
public interface IViewTransitionElement {
    /// <summary>최초 표시 전(뷰 Awake) 숨김 시작 상태로 준비한다.</summary>
    void OnViewPrepare();
    /// <summary>표시(show=true)/숨김(show=false) 트랜지션 트윈을 시퀀스에 추가한다.</summary>
    void AppendTransition(Sequence seq, bool show, float duration);
}
