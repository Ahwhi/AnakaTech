using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 의뢰 씬 일시정지 팝업(우측 상단 햄버거 버튼). 계속/재도전/나가기를 await 로 돌려준다.
/// 전투를 실제로 멈추거나 다시 시작하는 것은 씬 컨트롤러이며, 이 팝업은 선택지만 받는다(역할 분리).
/// </summary>
public sealed class CommissionPausePopup : UIView {
    /// <summary>일시정지 메뉴 선택 결과.</summary>
    public enum Result { Resume, Retry, Exit }

    [SerializeField] private Button resumeButton;
    [Tooltip("현재 단계를 처음부터 다시 시작한다(도전은 무료라 재화 소모가 없다). 선택.")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button exitButton;

    private UniTaskCompletionSource<Result> _result;

    protected override void Awake() {
        base.Awake();
        if (resumeButton != null) resumeButton.onClick.AddListener(() => Resolve(Result.Resume));
        if (retryButton != null) retryButton.onClick.AddListener(() => Resolve(Result.Retry));
        if (exitButton != null) exitButton.onClick.AddListener(() => Resolve(Result.Exit));
    }

    // base.ShowAsync()(UniTask 반환)를 의도적으로 숨겨 선택 결과(Result)를 await 로 돌려준다(하이딩, override 아님).
    public new async UniTask<Result> ShowAsync() {
        _result = new UniTaskCompletionSource<Result>();
        await base.ShowAsync();
        return await _result.Task;
    }

    private void Resolve(Result value) {
        _result?.TrySetResult(value);
        CloseAsync().Forget();
    }

    // 버튼 외의 경로(배경 탭, 연결 끊김에 의한 일괄 닫힘 등)로 닫혀도 반드시 결과를 확정한다.
    // 확정하지 않으면 await 중인 씬 컨트롤러가 멈춘 채 전투가 영영 일시정지로 남는다.
    protected override void OnClosed() {
        _result?.TrySetResult(Result.Resume);
    }
}
