using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 약관 관련 표시 흐름. "필요하면 동의를 받고", "언제든 문서를 보여 주고", "선택 동의를 바꾼다"는
/// 세 가지 의도만 노출한다. 부트 스텝과 설정 화면은 이 계약에만 의존하므로
/// 팝업/통신/문서 저장소를 전혀 모른다.
/// </summary>
public interface ITermsConsentFlow {
    /// <summary>필수 약관 동의를 보장한다. true = 통과(이미 동의했거나 방금 동의), false = 사용자가 거부.</summary>
    UniTask<bool> EnsureAgreedAsync(CancellationToken ct = default);

    /// <summary>현재 동의 상태와 상시 공개 문서 목록을 가져온다. 설정 화면이 목록을 구성할 때 쓴다.</summary>
    UniTask<TermsStatusDto> GetStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// 문서 전문(공식 홈페이지)을 보여 준다. 동의 여부와 무관하게 언제든 열 수 있다.
    /// 주소는 서버가 내려준 값을 그대로 넘긴다(화면이 주소를 만들지 않는다).
    /// </summary>
    UniTask ShowDocumentAsync(string url, string title, CancellationToken ct = default);

    /// <summary>
    /// 선택 동의 한 건을 바꾼다(동의/철회). 성공하면 처리 결과를 사용자에게 알린다.
    /// 전제 항목을 철회하면 딸린 항목도 서버가 함께 해제하며, 그 사실도 안내에 포함된다.
    /// </summary>
    UniTask<bool> SetOptionalConsentAsync(string key, int version, bool agreed, CancellationToken ct = default);
}
