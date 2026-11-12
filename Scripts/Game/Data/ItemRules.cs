using System.Collections.Generic;

/// <summary>
/// 소모품 아이템 규칙의 클라 미러. 보유/효과/수량 판정은 전부 서버가 하므로
/// 여기에는 실패 사유 문구처럼 표시에만 쓰이는 것만 둔다.
/// </summary>
public static class ItemRules {
    /// <summary>아이템 사용 실패 사유(서버 코드) -> 안내 문구. 서버 GameDataRepository.ItemUseFail 과 짝을 이룬다.</summary>
    public static string UseFailMessage(int fail) => fail switch {
        1 => "사용할 수 없는 아이템입니다.",
        2 => "보유 수량이 부족합니다.",
        3 => "대상 캐릭터를 찾을 수 없습니다.",
        4 => "이 아이템은 이렇게 사용할 수 없습니다.",
        5 => "이 아이템은 한 번에 하나씩만 사용할 수 있습니다.",
        6 => "추출할 경험치가 없습니다.",
        7 => "경험치 캡슐을 더 보유할 수 없습니다.",
        8 => "이미 최대 레벨입니다.",
        9 => "한계돌파한 소녀는 경험치를 추출할 수 없습니다.",
        10 => "초기화할 성장이 없습니다.",
        _ => "아이템을 사용하지 못했습니다."
    };

    private const string ExtractAmountColor = "#40CC59";
    private const string ExtractWarningColor = "#FF4D4D";

    /// <summary>확인이 필요한 아이템(ItemData.RequiresConfirm)의 사용 확인 문구. 수치는 전부 서버 미리보기 값이다.</summary>
    public static string ConfirmMessage(ItemData item, ItemUsePreview preview) => item.Kind switch {
        ItemKind.ExpExtract => ExtractConfirmMessage(preview),
        ItemKind.ExpCapsule => CapsuleConfirmMessage(preview),
        ItemKind.GrowthReset => GrowthResetConfirmMessage(preview),
        _ => $"{item.DisplayName}을(를) 사용하시겠습니까?"
    };

    /// <summary>
    /// 사용 확인 팝업에 보상 칸으로 보여 줄 "받게 될 품목"(없으면 null). 수치는 전부 서버 미리보기 값이다.
    /// 성장 초기화권 = 그 소녀의 조각 / 다이아 / 한계돌파권 / 경험치 캡슐, 경험치 추출권 = 경험치 캡슐.
    /// 캡슐은 서버가 큰 단위부터 나눈 종류별 개수(GrantItems)를 그대로 쓴다. 0 인 항목은 팝업이 그리지 않는다.
    /// </summary>
    public static List<RewardItem> ConfirmRewards(ItemData item, ItemUsePreview preview, int charDataId) {
        if (item == null || preview == null) return null;
        if (item.Kind != ItemKind.GrowthReset && item.Kind != ItemKind.ExpExtract) return null;

        var list = new List<RewardItem>();
        if (item.Kind == ItemKind.GrowthReset) {
            list.Add(RewardItem.CharacterShard(charDataId, preview.Refund.Shards));
            list.Add(RewardItem.Diamond(preview.Refund.Diamond));
            list.Add(RewardItem.Currency(CurrencyKeys.LimitBreakTicket, preview.Refund.LimitBreakTicket));
        }
        foreach (var g in preview.GrantItems) list.Add(RewardItem.Item(g.ItemId, g.Count));
        return list;
    }

    /// <summary>
    /// 돌려받은 것 요약(예: "조각 1,050 / 다이아 10,500 / 한계돌파권 1 / 경험치 캡슐 123,456"). 0 인 항목은 뺀다.
    /// 성장 초기화를 마친 뒤의 안내 토스트에 쓴다(확인 단계는 보상 칸으로 보여 준다).
    /// </summary>
    public static string RefundText(GrowthRefund refund, long exp) {
        var parts = new List<string>(4);
        if (refund.Shards > 0) parts.Add($"조각 {refund.Shards:N0}");
        if (refund.Diamond > 0) parts.Add($"다이아 {refund.Diamond:N0}");
        if (refund.LimitBreakTicket > 0) parts.Add($"한계돌파권 {refund.LimitBreakTicket:N0}");
        if (exp > 0) parts.Add($"경험치 캡슐 {exp:N0}");
        return string.Join(" / ", parts);
    }

    /// <summary>경험치 캡슐 1개의 경험치(서버 ItemCatalog.ExpPerCapsule 미러). 캡슐이 아니면 0.</summary>
    public static long ExpPerCapsule(ItemData item) {
        if (item == null || item.Kind != ItemKind.ExpCapsule) return 0;
        return item.Id == ItemKeys.ExpCapsuleLarge ? ItemKeys.LargeCapsuleExp : 1;
    }

    /// <summary>
    /// 지금 쓸 수 있는 캡슐 최대 개수. 최대 레벨까지 필요한 만큼(큰 단위는 올림)과 보유량 중 작은 쪽이다
    /// (서버 CapsuleUsableCount 와 같은 규칙. 넘게 보내도 서버가 필요한 만큼만 쓴다).
    /// </summary>
    public static int CapsuleUsableCount(ItemData item, CharacterProgress progress, int owned) {
        long unit = ExpPerCapsule(item);
        if (unit <= 0 || progress == null || owned < 1) return 0;
        long toMax = GrowthRules.ExpToMaxLevel(progress.Level, progress.Exp);
        long need = (toMax + unit - 1) / unit;
        return (int)System.Math.Min(owned, need);
    }

    /// <summary>
    /// 캡슐 수량 선택 중에 보여 줄 결과(최대 3줄). 들어갈 경험치, 레벨 변화, 최대 레벨 도달 여부.
    /// 수치는 서버 공식의 미러로 계산한 예상값이고 실제 결과는 서버가 정한다.
    /// </summary>
    public static string CapsuleQuantityMessage(ItemData item, CharacterProgress progress, int count) {
        long unit = ExpPerCapsule(item);
        if (unit <= 0 || progress == null) return "";
        long toMax = GrowthRules.ExpToMaxLevel(progress.Level, progress.Exp);
        long exp = System.Math.Min(count * unit, toMax);
        int after = GrowthRules.LevelOfCumulative(GrowthRules.CumulativeExp(progress.Level, progress.Exp) + exp);
        return ExpGainMessage(progress.Level, exp, after);
    }

    /// <summary>
    /// 수량 선택 중에 보여 줄 결과 문구(경험치를 주는 아이템만, 나머지는 null). 캡슐과 경험치 쿠폰을 지원한다.
    /// 수량 선택 팝업이 이 결과로 안내 문구를 바꿀지 판단한다.
    /// </summary>
    public static string QuantityMessage(ItemData item, CharacterProgress progress, int count) {
        if (item == null || progress == null) return null;
        return item.Kind switch {
            ItemKind.ExpCapsule => CapsuleQuantityMessage(item, progress, count),
            ItemKind.ExpCoupon => CouponQuantityMessage(item, progress, count),
            _ => null
        };
    }

    /// <summary>수량을 고를 때 결과 문구를 보여 줄 수 있는 아이템인지.</summary>
    public static bool HasQuantityMessage(ItemData item) =>
        item != null && (item.Kind == ItemKind.ExpCapsule || item.Kind == ItemKind.ExpCoupon);

    /// <summary>경험치 쿠폰 1장의 효율(현재 레벨 필요 경험치의 %). 서버 ItemCatalog 의 Value 미러. 쿠폰이 아니면 0.</summary>
    public static int CouponPercent(ItemData item) {
        if (item == null || item.Kind != ItemKind.ExpCoupon) return 0;
        return item.Id switch {
            ItemKeys.ExpCoupon10 => 10,
            ItemKeys.ExpCoupon20 => 20,
            ItemKeys.ExpCoupon30 => 30,
            ItemKeys.ExpCoupon50 => 50,
            ItemKeys.ExpCoupon100 => 100,
            _ => 0
        };
    }

    /// <summary>
    /// 경험치 쿠폰 count 장을 쓴 결과(예상값). 서버처럼 한 장씩 적용한다. 지급량이 "그때 레벨의 필요 경험치" 비례라
    /// 장수를 곱하면 레벨업으로 늘어난 요구치가 빠진다. 최대 레벨에 닿으면 남은 쿠폰은 효과가 없으므로 거기서 멈춘다.
    /// </summary>
    public static string CouponQuantityMessage(ItemData item, CharacterProgress progress, int count) {
        int percent = CouponPercent(item);
        if (percent <= 0 || progress == null) return "";

        int level = progress.Level;
        long exp = progress.Exp;
        long gained = 0;
        for (int i = 0; i < count && !GrowthRules.IsMaxLevel(level); i++) {
            long gain = GrowthRules.MaxExpForLevel(level) * percent / 100;
            if (gain < 1) gain = 1;
            gained += gain;
            exp += gain;
            while (!GrowthRules.IsMaxLevel(level) && exp >= GrowthRules.MaxExpForLevel(level)) {
                exp -= GrowthRules.MaxExpForLevel(level);
                level++;
            }
        }
        return ExpGainMessage(progress.Level, gained, level);
    }

    // 경험치 아이템 수량 선택의 공통 표기(최대 3줄): 들어갈 경험치 / 레벨 변화 / 최대 레벨 도달.
    private static string ExpGainMessage(int fromLevel, long exp, int toLevel) {
        string text = $"경험치 <color={ExtractAmountColor}>+{exp:N0}</color>\n" +
                      $"Lv.{fromLevel:N0} -> <color={ExtractAmountColor}>Lv.{toLevel:N0}</color>";
        if (GrowthRules.IsMaxLevel(toLevel)) text += $"\n<color={ExtractWarningColor}>최대 레벨에 도달합니다.</color>";
        return text;
    }

    /// <summary>"Lv.A -> Lv.B" 레벨 변화 표기. 사용 확인과 사용 결과 안내가 같은 표기를 쓴다.</summary>
    public static string LevelChange(int from, int to) => $"Lv.{from:N0} -> Lv.{to:N0}";

    // 추출량은 서버 미리보기 값을 그대로 받는다(클라는 경험치 공식을 갖지 않는다).
    // 성장 초기화와 같은 형식(레벨 변화 + 경고 한 줄, 받을 캡슐은 보상 칸으로 보여 준다).
    private static string ExtractConfirmMessage(ItemUsePreview preview) =>
        $"<color={ExtractWarningColor}>Lv.{preview.CurrentLevel:N0} -> Lv.1 (스탯 포함)\n" +
        $"되돌릴 수 없습니다.</color> <color={ExtractAmountColor}>경험치 {preview.Value:N0}</color>을 추출합니다.";

    // 캡슐은 보유량 전체를 쓰되 최대 레벨을 넘칠 만큼은 남기므로, 실제로 들어갈 양과 레벨 변화를 함께 보여 준다.
    private static string CapsuleConfirmMessage(ItemUsePreview preview) =>
        $"<color={ExtractAmountColor}>경험치 {preview.Value:N0}</color>을 사용합니다.\n" +
        $"{LevelChange(preview.CurrentLevel, preview.ResultLevel)}\n" +
        "경험치 캡슐을 사용하시겠습니까?";

    // 성장 초기화는 별레벨/레벨/한계돌파가 한꺼번에 되돌아가므로 되돌아갈 상태를 밝힌다(돌려받을 품목은 보상 칸으로 보여 준다).
    private static string GrowthResetConfirmMessage(ItemUsePreview preview) =>
        $"<color={ExtractWarningColor}>{preview.CurrentStar}성 Lv.{preview.CurrentLevel:N0} -> 1성 Lv.1 (스탯/한계돌파 포함)\n" +
        "되돌릴 수 없습니다.</color> 아래 품목을 돌려받습니다.";
}
