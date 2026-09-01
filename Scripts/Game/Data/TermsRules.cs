using System;

/// <summary>
/// 약관 동의 화면의 표시 규칙(서버 TermsCatalog 미러). 필수/선택 판정과 저장은 전부 서버가 하며,
/// 여기에는 화면 문구와 서버 실패 코드 해석만 둔다.
///
/// 키 상수는 "마케팅 수신 동의 결과 안내" 처럼 특정 항목을 알아야 하는 표시에만 쓴다.
/// 목록 구성/순서/필수 여부는 서버가 내려준 값을 그대로 따르므로 여기에 미러하지 않는다.
/// </summary>
public static class TermsRules {
    /// <summary>광고성 정보 수신 동의 항목. 동의 결과를 사용자에게 알려야 해서 키를 안다.</summary>
    public const string MarketingKey = "marketing";
    /// <summary>야간 광고성 정보 수신 동의 항목(마케팅 수신 동의가 전제).</summary>
    public const string NightPushKey = "night_push";

    public const string PopupTitle = "약관 동의";
    public const string PopupDescription = "아나카 서비스 이용을 위해 아래 약관에 동의해 주세요.";
    /// <summary>선택 항목까지 한 번에 동의하고 바로 시작하는 버튼의 문구.</summary>
    public const string AgreeAllLabel = "전체 동의하고 시작하기";
    /// <summary>화면에 체크한 그대로 시작하는 버튼의 문구.</summary>
    public const string ConfirmLabel = "동의하고 시작하기";

    /// <summary>목록에 노출할 한 줄 제목. 필수/선택 표기는 서버 값으로 붙인다.</summary>
    public static string DisplayTitle(TermsItemDto item) =>
        item == null ? string.Empty : $"({(item.IsRequired ? "필수" : "선택")}) {item.Title}";

    /// <summary>상시 공개 문서(개인정보처리방침) 키. 설정 화면이 동의 항목과 구분해 노출한다.</summary>
    public const string PrivacyPolicyKey = "privacy_policy";
    /// <summary>이용약관 키. 동의 항목이면서 동시에 상시 열람 대상이다.</summary>
    public const string ServiceKey = "service";
    /// <summary>청약철회 안내 문서 키. 상점의 현금 결제 상품 상세에서 연다.</summary>
    public const string WithdrawalPolicyKey = "withdrawal_policy";

    /// <summary>선행 동의가 필요한 항목의 잠금 안내(예: 야간 푸시).</summary>
    public const string PrerequisiteHint = "광고성 정보 앱 푸시 수신에 동의해야 선택할 수 있습니다.";

    // 설정 화면(계정 탭) ------------------------------------------------------
    public const string AccountTabTitle = "계정";
    /// <summary>선택 동의를 나중에 바꿀 수 있다는 안내. 가입 때 거부했어도 여기서 다시 동의할 수 있다.</summary>
    public const string OptionalConsentHint = "선택 항목은 언제든지 동의하거나 철회할 수 있습니다.";
    public const string DocumentSectionTitle = "약관 및 정책";
    public const string ConsentSectionTitle = "선택 동의";

    /// <summary>수신 동의 변경을 저장하지 못했을 때.</summary>
    public const string ConsentSaveFailed = "동의 설정을 저장하지 못했습니다. 잠시 후 다시 시도해 주세요.";

    /// <summary>약관 문서(홈페이지)를 앱 안에서 열지 못했을 때. 문서를 못 본 것과 동의 여부는 무관하다.</summary>
    public const string DocumentLoadFailed = "문서를 불러오지 못했습니다.\n네트워크 상태를 확인한 뒤 다시 시도해 주세요.";

    // 회원 탈퇴 ---------------------------------------------------------------
    public const string WithdrawTitle = "회원 탈퇴";
    public const string WithdrawConfirm =
        "탈퇴하면 계정과 모든 게임 데이터가 삭제되며 복구할 수 없습니다.\n" +
        "결제한 재화와 아이템도 함께 사라집니다.\n\n" +
        "정말 탈퇴하시겠습니까?";
    public const string WithdrawDone = "탈퇴가 완료되었습니다. 그동안 이용해 주셔서 감사합니다.";

    /// <summary>서버 AccountWithdrawalRepository.Fail 값 -> 안내 문구. 값이 서버와 일치해야 한다.</summary>
    public static string WithdrawFailMessage(int failReason) => failReason switch {
        1 => "계정 정보를 찾을 수 없습니다. 다시 로그인한 뒤 시도해 주세요.",
        3 => "길드장은 탈퇴할 수 없습니다. 길드를 위임하거나 해체한 뒤 다시 시도해 주세요.",
        _ => "탈퇴를 처리하지 못했습니다. 잠시 후 다시 시도해 주세요.",
    };

    /// <summary>필수 항목을 다 체크하지 않고 시작을 눌렀을 때의 안내.</summary>
    public const string RequiredHint = "필수 항목에 모두 동의해야 시작할 수 있습니다.";

    /// <summary>동의를 취소(뒤로가기)했을 때의 안내.</summary>
    public const string CancelledMessage = "필수 약관에 동의해야 게임을 이용할 수 있습니다.";

    /// <summary>서버 TermsRepository.Fail 값 -> 안내 문구. 값이 서버와 일치해야 한다.</summary>
    public static string FailMessage(int failReason) => failReason switch {
        1 => RequiredHint,
        2 => PrerequisiteHint,
        3 => "약관 정보가 최신이 아닙니다. 게임을 다시 시작해 주세요.",
        _ => "약관 동의를 저장하지 못했습니다. 잠시 후 다시 시도해 주세요.",
    };

    /// <summary>
    /// 광고성 정보 수신 동의/철회 처리 결과 안내 문구. 수신 동의를 받은 사실과 시점을 사용자에게 알려야 하므로
    /// 동의 저장 직후 한 번 표시한다(정보통신망법상 수신 동의 처리 결과 통지에 해당).
    /// </summary>
    public static string MarketingNotice(bool marketingAgreed, bool nightPushAgreed, DateTime dateKst) {
        string date = dateKst.ToString("yyyy년 MM월 dd일");
        if (!marketingAgreed)
            return $"[아나카] 광고성 정보 앱 푸시 수신에 동의하지 않았습니다. ({date})";

        return nightPushAgreed
            ? $"[아나카] 광고성 정보 앱 푸시 수신(야간 포함)에 동의하셨습니다. ({date})"
            : $"[아나카] 광고성 정보 앱 푸시 수신에 동의하셨습니다. ({date})";
    }

    /// <summary>
    /// 수신 동의를 철회했을 때의 처리 결과 안내. 동의뿐 아니라 철회도 처리 결과를 알려야 한다.
    /// 야간 동의는 일반 동의를 전제로 하므로, 일반 동의를 철회하면 야간 동의도 함께 풀렸음을 함께 알린다.
    /// </summary>
    public static string MarketingWithdrawNotice(bool nightPushAlsoCleared, DateTime dateKst) {
        string date = dateKst.ToString("yyyy년 MM월 dd일");
        return nightPushAlsoCleared
            ? $"[아나카] 광고성 정보 앱 푸시 수신 동의를 철회했습니다. 야간 수신 동의도 함께 해제되었습니다. ({date})"
            : $"[아나카] 광고성 정보 앱 푸시 수신 동의를 철회했습니다. ({date})";
    }

    /// <summary>야간 수신 동의만 철회했을 때의 처리 결과 안내.</summary>
    public static string NightPushWithdrawNotice(DateTime dateKst) =>
        $"[아나카] 야간 광고성 정보 앱 푸시 수신 동의를 철회했습니다. ({dateKst:yyyy년 MM월 dd일})";
}
