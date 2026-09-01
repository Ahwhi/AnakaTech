/// <summary>
/// 소모품 아이템 규칙의 클라 미러. 보유/효과/수량 판정은 전부 서버가 하므로
/// 여기에는 실패 사유 문구처럼 표시에만 쓰이는 것만 둔다.
/// </summary>
public static class ItemRules {
    /// <summary>아이템 사용 실패 사유(서버 코드) -> 안내 문구. 서버 UseItem 과 짝을 이룬다.</summary>
    public static string UseFailMessage(int fail) => fail switch {
        1 => "사용할 수 없는 아이템입니다.",
        2 => "보유 수량이 부족합니다.",
        3 => "대상 캐릭터를 찾을 수 없습니다.",
        4 => "이 아이템은 이렇게 사용할 수 없습니다.",
        _ => "아이템을 사용하지 못했습니다."
    };
}
