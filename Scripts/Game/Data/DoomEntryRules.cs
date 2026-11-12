/// <summary>
/// 멸망전 참여 안내 문구(V1.13). 비용 계산은 서버(DoomCatalog.EntryCost)가 하고 클라는 받은 값만 쓴다.
/// 입구 팝업과 결과 팝업(재도전)이 같은 문구를 쓰도록 한 곳에 둔다.
/// </summary>
public static class DoomEntryRules {
    public const string PaidEntryTitle = "멸망전 참여";

    /// <summary>유료 참여 확인 문구.</summary>
    public static string PaidEntryConfirmMessage(int cost) =>
        $"오늘 무료 참여를 모두 사용했습니다.\n다이아 {cost:N0}을(를) 사용해 참여하시겠습니까?";

    public const string NotEnoughDiamondMessage = "다이아가 부족합니다.";

    /// <summary>입장 실패 안내.</summary>
    public static string EnterFailMessage(int fail) => fail switch {
        DoomEnterFail.NotEnoughDiamond => NotEnoughDiamondMessage,
        _ => "멸망전에 입장하지 못했습니다."
    };
}
