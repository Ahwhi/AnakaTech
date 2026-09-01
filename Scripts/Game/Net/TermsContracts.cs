using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 약관 동의 통신 계약과 순수 DTO(생성 패킷 비의존). 동의 화면은 이 타입들만 참조한다.

/// <summary>
/// 약관 한 건의 표시/판정 정보. 무엇에 동의해야 하는지는 서버(TermsCatalog)가 정하며 여기에는 그대로 옮겨 담는다.
/// 본문(장문)은 담기지 않는다. 상세 화면이 Url 을 앱 내부 웹뷰로 열어 공식 홈페이지의 문서를 보여 준다.
/// </summary>
public sealed class TermsItemDto {
    public string Key = "";
    public int Version;
    public bool IsRequired;
    public string Title = "";
    /// <summary>선행 동의 항목의 키(없으면 빈 문자열). 예: 야간 푸시는 마케팅 수신 동의가 전제다.</summary>
    public string PrerequisiteKey = "";
    /// <summary>현재 버전 기준 동의 여부. 약관이 개정되면 예전 동의는 미동의로 내려온다.</summary>
    public bool IsAgreed;
    /// <summary>문서 전문 주소(서버가 내려준다). 비어 있으면 상세 보기를 노출하지 않는다.</summary>
    public string Url = "";

    public bool HasPrerequisite => !string.IsNullOrEmpty(PrerequisiteKey);
    public bool HasDocument => !string.IsNullOrEmpty(Url);
}

/// <summary>
/// 동의 대상이 아니라 상시 공개하는 문서(이용약관 열람, 개인정보처리방침).
/// 가입 화면의 체크박스와 분리해 설정 화면에서 언제든 열람할 수 있어야 한다.
/// </summary>
public sealed class TermsDocumentDto {
    public string Key = "";
    public int Version;
    public string Title = "";
    /// <summary>문서 전문 주소(서버가 내려준다).</summary>
    public string Url = "";
}

/// <summary>약관 게이트 상태. NeedAgreement 가 true 면 동의 화면을 띄워야 한다.</summary>
public sealed class TermsStatusDto {
    public bool NeedAgreement;
    public List<TermsItemDto> Items = new();
    public List<TermsDocumentDto> Documents = new();

    /// <summary>선택(임의) 동의 항목만. 설정 화면이 이 목록을 그대로 노출한다.</summary>
    public IEnumerable<TermsItemDto> OptionalItems {
        get {
            foreach (var item in Items)
                if (!item.IsRequired) yield return item;
        }
    }

    public TermsItemDto Find(string key) {
        foreach (var item in Items)
            if (item.Key == key) return item;
        return null;
    }

    /// <summary>상시 공개 문서를 키로 찾는다(예: 개인정보처리방침). 없으면 null.</summary>
    public TermsDocumentDto FindDocument(string key) {
        foreach (var doc in Documents)
            if (doc.Key == key) return doc;
        return null;
    }
}

/// <summary>사용자가 화면에서 고른 항목별 동의 결과(제출 단위).</summary>
public readonly struct TermsConsent {
    public string Key { get; }
    public int Version { get; }
    public bool IsAgreed { get; }

    public TermsConsent(string key, int version, bool isAgreed) {
        Key = key;
        Version = version;
        IsAgreed = isAgreed;
    }
}

/// <summary>동의 제출 결과. 판정은 서버가 하며 NeedAgreement 가 곧 게이트 통과 여부다.</summary>
public sealed class TermsSubmitResultDto {
    public bool IsSuccess;
    public int FailReason;
    public bool NeedAgreement;
}

/// <summary>회원 탈퇴 결과. 판정과 삭제는 전부 서버가 한다.</summary>
public sealed class WithdrawResultDto {
    public bool IsSuccess;
    public int FailReason;
}

/// <summary>약관 동의 통신 추상화. 화면/흐름은 이 계약에만 의존한다.</summary>
public interface ITermsService {
    UniTask<TermsStatusDto> GetStatusAsync(CancellationToken ct = default);
    UniTask<TermsSubmitResultDto> SubmitAsync(IReadOnlyList<TermsConsent> consents, CancellationToken ct = default);
}

/// <summary>회원 탈퇴 통신 추상화. 약관과 생명주기가 달라 계약을 분리한다.</summary>
public interface IAccountService {
    UniTask<WithdrawResultDto> WithdrawAsync(CancellationToken ct = default);
}
