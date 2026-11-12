using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 계정 진행 지표(총 전투력/캐릭터 수집/총 별레벨) 계산 유틸. 정보 패널(HUD)과 프로필 팝업 등
/// 여러 화면이 동일한 값을 표시하도록 계산을 한 곳에 모은다. 순수 계산만 하며 상태 변경/통신은 하지 않는다.
/// </summary>
public static class AccountStats {
    /// <summary>
    /// 덱에 편성된 캐릭터 전원의 전투력 합(장비 보정 포함).
    /// 합은 double 로 더한다. float 은 21억대에서 128 단위로만 구분되어 작은 상승이 사라지고, 표시는 long 으로 해야
    /// int 최대값(약 21억)을 넘어도 숫자가 깨지지 않는다.
    /// </summary>
    public static double TotalDeckCombatPower(MainHudContext ctx) {
        if (ctx == null) return 0d;
        double total = 0d;
        foreach (int charId in ctx.Store.Deck.SelectedCharIds) total += CharacterCombatPower(ctx, charId);
        return total;
    }

    /// <summary>
    /// 덱이 아닌 편성(전선 스쿼드 등)의 전투력 합. 시너지도 그 편성으로 판정하므로 그 편성이 실제로 싸울 때와 같은 값이다.
    /// 보유하지 않은 캐릭터는 빠진다.
    /// </summary>
    public static double SquadCombatPower(BattleHudContext ctx, IReadOnlyList<int> members) {
        if (ctx == null || members == null) return 0d;
        double total = 0d;
        foreach (int id in members) {
            var data = ctx.CharDb.Get(id);
            var progress = ctx.Store.GetProgress(id);
            if (data == null || progress == null) continue;
            total += ctx.Store.RuntimeFor(data, progress, ctx.Settings, ctx.EquipDb, members).CombatPower;
        }
        return total;
    }

    /// <summary>캐릭터 1명의 전투력(덱 합산과 같은 계산). 보유하지 않은 캐릭터면 0.</summary>
    public static double CharacterCombatPower(MainHudContext ctx, int charId) {
        if (ctx == null || charId <= 0) return 0d;
        var data = ctx.CharDb.Get(charId);
        var progress = ctx.Store.GetProgress(charId);
        if (data == null || progress == null) return 0d;
        return ctx.Store.RuntimeFor(data, progress, ctx.Settings, ctx.EquipDb).CombatPower;
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
