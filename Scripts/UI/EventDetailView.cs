using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// EventPopup 안에 상주하는 상세 영역(팝업이 아님). 선택된 이벤트마다 전용 페이지 프리팹을 인스턴스화해 보여준다.
/// 더 이상 제목/본문/이미지를 일반 텍스트로 출력하지 않으며, 표시 내용은 각 이벤트 페이지 프리팹이 스스로 책임진다.
///
/// 어떤 프리팹을 띄울지는 EventPageCatalog(eventKey -> Addressable 프리팹)에서 선택된 이벤트의 eventKey 로 찾는다.
/// 새 이벤트는 페이지 프리팹을 만들어 카탈로그에 등록하기만 하면 된다.
/// 페이지에 EventPageHeader 가 붙어 있으면 이벤트 이름/기간은 여기서 공통으로 바인딩한다(페이지 스크립트는 관여하지 않음).
/// 빠른 탭 전환 시 이전 로드는 취소하고 이전 인스턴스는 해제해 마지막 선택만 반영한다.
/// </summary>
public sealed class EventDetailView : MonoBehaviour {
    [Tooltip("이벤트별 페이지 프리팹이 생성될 부모.")]
    [SerializeField] private Transform pageRoot;
    [Tooltip("eventKey -> 페이지 프리팹(Addressable) 매핑 카탈로그.")]
    [SerializeField] private EventPageCatalog catalog;
    [Tooltip("매칭되는 페이지가 없을 때 보여줄 안내(선택).")]
    [SerializeField] private GameObject missingPageLabel;

    private IAssetService _asset;
    private IObjectResolver _resolver;
    private CancellationTokenSource _cts;
    private GameObject _current;

    /// <summary>
    /// 프레젠터가 자산 서비스(페이지 로드/해제용)와 DI 컨테이너를 주입한다.
    /// 컨테이너는 동적 생성된 페이지 프리팹의 [Inject] 컴포넌트(예: 페이지 안의 버튼 컨트롤러)에 의존성을 넣는 데 쓴다.
    /// </summary>
    public void Initialize(IAssetService asset, IObjectResolver resolver) {
        _asset = asset;
        _resolver = resolver;
    }

    /// <summary>선택된 이벤트의 전용 페이지로 교체한다.</summary>
    public void Show(NoticeData data) => ShowAsync(data).Forget();

    private async UniTaskVoid ShowAsync(NoticeData data) {
        // 이전 로드/인스턴스를 정리(빠른 탭 전환 시 마지막 선택만 반영).
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        ClearCurrent();

        var reference = catalog != null ? catalog.Resolve(data.eventKey) : null;
        bool hasPage = reference != null && reference.RuntimeKeyIsValid();
        if (missingPageLabel != null) missingPageLabel.SetActive(!hasPage);
        if (!hasPage || _asset == null) return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        var token = _cts.Token;

        GameObject go;
        try {
            go = await _asset.InstantiateAsync(reference, pageRoot, token);
        } catch (OperationCanceledException) {
            return;
        }

        // 로드 도중 다른 탭이 선택돼 취소됐다면, 뒤늦게 생성된 인스턴스를 버린다.
        if (token.IsCancellationRequested) {
            _asset.ReleaseInstance(go);
            return;
        }

        _current = go;

        // 이벤트 이름/기간은 모든 페이지가 공통으로 쓰는 정보라 페이지 스크립트 대신 헤더가 받는다.
        // 헤더를 붙이지 않은 페이지는 그냥 건너뛴다.
        // 페이지 내부 사정과 무관한 이 뷰의 책임이므로 주입보다 먼저 반영한다(주입이 실패해도 헤더는 항상 갱신된다).
        var header = go.GetComponentInChildren<EventPageHeader>(true);
        if (header != null) header.Bind(data);

        // 동적 생성 프리팹은 어떤 LifetimeScope 에도 속하지 않아 자동 주입이 안 된다.
        // 여기서 컨테이너로 주입해 페이지 프리팹에 붙은 [Inject] 컴포넌트(이벤트 스크립트 등)가 동작하게 한다.
        // 이 컨테이너가 모르는 의존성을 요구하는 컴포넌트가 섞여 있어도 페이지 표시 자체는 이어지도록 예외를 가둔다.
        try {
            _resolver?.InjectGameObject(go);
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    /// <summary>현재 페이지를 비운다(팝업이 파괴되기 전에 호출해 인스턴스를 안전하게 해제).</summary>
    public void Clear() {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        ClearCurrent();
    }

    private void ClearCurrent() {
        if (_current != null && _asset != null) _asset.ReleaseInstance(_current);
        _current = null;
    }

    private void OnDestroy() {
        _cts?.Cancel();
        _cts?.Dispose();
        ClearCurrent();
    }
}
