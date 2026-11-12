/// <summary>
/// 캐릭터 육성 가능 여부 판정(순수 계산, 스토어 기반). 덱 배지에서 사용한다(장비는 배지 대상 아님).
///   - CanGrow : 투자할 스탯포인트 있음 OR 별 승급 가능(조각+다이아 충분) OR 한계돌파 가능.
/// 서버 통신 없이 판단하며, GrowthPopup 의 실제 가능 조건과 동일하게 맞춘다.
/// </summary>
public static class CharacterUpgrade {
    public static bool CanGrow(PlayerStateStore store, int charId, int ownedDia) {
        var p = store != null ? store.GetProgress(charId) : null;
        if (p == null) return false;
        if (p.StatPoints > 0) return true;                       // 투자할 스탯포인트 있음
        if (LimitBreakRules.CanLimitBreak(p, store.LimitBreakTicket.Value)) return true;   // 한계돌파 가능(15성이라 승급 판정 전에 본다)
        if (p.StarLevel >= GrowthRules.MaxStarLevel) return false;
        return p.Shards >= GrowthRules.RequiredShardsForStar(p.StarLevel)
            && ownedDia >= GrowthRules.RequiredDiaForStar(p.StarLevel);   // 별 승급 가능
    }
}
