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

    // 선택 결과(Result)를 await 로 돌려주는 열기. base.ShowAsync()(UniTask 반환)와 매개변수가 달라 숨김이 아니라 오버로드다.
    // popup.ShowAsync() 처럼 인자 없이 불러도 C# 은 파생 클래스에 맞는 메서드가 있으면 그것을 고르므로 이 메서드가 불린다.
    /// <param name="allowRetry">
    /// 끄면 재도전 버튼을 숨긴다. 입장 횟수/보고가 걸린 컨텐츠(멸망전, 무한 타워, 전선, 영웅전)는 도중 재시작을 허용하지 않는다.
    /// 팝업은 재사용되므로 열 때마다 다시 정한다.
    /// </param>
    public async UniTask<Result> ShowAsync(bool allowRetry = true) {
        if (retryButton != null) retryButton.gameObject.SetActive(allowRetry);
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
