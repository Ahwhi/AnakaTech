using UnityEngine;

/// <summary>
/// 계정 진행 지표(총 전투력/캐릭터 수집/총 별레벨) 계산 유틸. 정보 패널(HUD)과 프로필 팝업 등
/// 여러 화면이 동일한 값을 표시하도록 계산을 한 곳에 모은다. 순수 계산만 하며 상태 변경/통신은 하지 않는다.
/// </summary>
public static class AccountStats {
    /// <summary>덱에 편성된 캐릭터 전원의 전투력 합(장비 보정 포함).</summary>
    public static float TotalDeckCombatPower(MainHudContext ctx) {
        if (ctx == null) return 0f;
        float total = 0f;
        foreach (int charId in ctx.Store.Deck.SelectedCharIds) {
            var data = ctx.CharDb.Get(charId);
            var progress = ctx.Store.GetProgress(charId);
            if (data == null || progress == null) continue;
            var bonus = ctx.Store.TotalBonusFor(charId, ctx.EquipDb);
            total += new CharacterRuntime(data, progress, ctx.Settings, bonus).CombatPower;
        }
        return total;
    }

    /// <summary>보유 캐릭터 수(도감 수집 현황의 분자).</summary>
    public static int OwnedCharacterCount(MainHudContext ctx) =>
        ctx != null ? ctx.Store.Characters.Count : 0;

    /// <summary>게임 전체 캐릭터 수(도감 수집 현황의 분모).</summary>
    public static int TotalCharacterCount(MainHudContext ctx) {
        if (ctx == null || ctx.CharDb == null) return 0;
        int count = 0;
        var all = ctx.CharDb.All;
        for (int i = 0; i < all.Count; i++) if (all[i] != null) count++;
        return count;
    }

    /// <summary>보유 캐릭터들의 별레벨 총합(총 별 등급의 분자).</summary>
    public static int TotalStarLevel(MainHudContext ctx) {
        if (ctx == null) return 0;
        int total = 0;
        foreach (var p in ctx.Store.Characters.Values) if (p != null) total += p.StarLevel;
        return total;
    }

    /// <summary>전체 캐릭터가 모두 최대 별레벨일 때의 별레벨 총합(총 별 등급의 분모).</summary>
    public static int MaxTotalStarLevel(MainHudContext ctx) =>
        TotalCharacterCount(ctx) * GrowthRules.MaxStarLevel;
}
