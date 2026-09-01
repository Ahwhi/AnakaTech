using System.Collections.Generic;

/// <summary>
/// 의뢰 규칙/보상의 클라 미러(서버 CommissionCatalog 와 값이 일치해야 한다). 실제 지급/판정은 전부 서버가 하며
/// 여기 값은 목록 표시와 즉시해결 팝업의 "예정 보상" 미리보기에만 쓰인다.
/// 서버 보상표를 바꿀 때 이 파일도 함께 고칠 것(어긋나면 표시만 다르고 지급은 서버가 맞다).
///
/// 두 경로는 서로 다른 행동이고 보상표도 완전히 독립이다(한쪽이 다른 쪽의 부분집합이 아니다):
///  - 의뢰도전(ClearRewards) : 무료 진도. 각 단계를 처음 깰 때 1회만 받는 마일스톤 보상.
///  - 즉시해결(SweepRewards) : 의뢰해결권 1장당 "클리어한 최고 단계" 기준으로 받는 반복 수급 보상.
/// </summary>
public static class CommissionRewardRules {
    public const int TypeCount = 3;
    public const int MaxStage = 50;

    /// <summary>매일 자동 충전되는 의뢰해결권 수(KST 자정). 즉시해결 전용 재화이며 도전에는 쓰이지 않는다.</summary>
    public const int TicketDailyRefill = 3;

    public static string TypeName(CommissionType type) => type switch {
        CommissionType.Raid => "레이드 의뢰",
        CommissionType.Subjugation => "토벌 의뢰",
        CommissionType.Escort => "호위 의뢰",
        _ => "의뢰"
    };

    /// <summary>목록 카드에 붙는 한 줄 소개(무엇을 검증하는 컨텐츠인지).</summary>
    public static string TypeSummary(CommissionType type) => type switch {
        CommissionType.Raid => "제한 시간 안에 보스를 쓰러뜨리세요. 다이아와 경험치를 얻습니다.",
        CommissionType.Subjugation => "몰려오는 적을 목표 수만큼 처치하세요. 골드와 각인 재료를 얻습니다.",
        CommissionType.Escort => "의뢰인을 지키며 버티세요. 골드와 명성을 얻습니다.",
        _ => ""
    };

    /// <summary>도전 가능한 단계인지(1 ~ clearedStage+1).</summary>
    public static bool IsPlayable(int stage, int clearedStage) =>
        stage >= 1 && stage <= MaxStage && stage <= clearedStage + 1;

    /// <summary>다음에 도전할 단계(클리어한 최고 단계 + 1, 최대치에서 멈춤).</summary>
    public static int NextStage(int clearedStage) =>
        clearedStage >= MaxStage ? MaxStage : clearedStage + 1;

    /// <summary>
    /// 의뢰도전으로 그 단계를 처음 클리어했을 때 1회만 받는 보상(서버 CommissionCatalog.ClearRewards 미러).
    /// 각인 도면도 다른 보상과 같은 슬롯으로 나온다(대상은 서버가 미보유 중에서 뽑으므로 예고는 전용 아이콘으로 그린다).
    /// </summary>
    public static List<RewardItem> ClearRewards(CommissionType type, int stage) {
        int s = Clamp(stage);
        var list = new List<RewardItem>();

        switch (type) {
            case CommissionType.Raid:
                list.Add(RewardItem.Diamond(100 + 5 * s));
                list.Add(RewardItem.Exp(ExpPerCharacter(type, s)));
                list.Add(RewardItem.Item(ItemKeys.ExpCoupon10, 10 * s));
                break;

            case CommissionType.Subjugation:
                list.Add(RewardItem.Gold(10000 + 4000 * s));
                list.Add(RewardItem.Currency(CurrencyKeys.EngravingStone, 40 + 20 * s));
                break;

            case CommissionType.Escort:
                list.Add(RewardItem.Gold(10000 + 4000 * s));
                list.Add(RewardItem.Currency(CurrencyKeys.Renown, 40 + 20 * s));
                break;
        }

        if (GrantsBlueprint(type, s))
            list.Add(RewardItem.RandomEngraving());

        return list;
    }

    /// <summary>
    /// 즉시해결에서 의뢰해결권 1장당 받는 보상(서버 CommissionCatalog.SweepRewards 미러).
    /// 즉시해결 미리보기는 이 목록에 사용 장수를 곱해서 보여준다.
    /// </summary>
    public static List<RewardItem> SweepRewards(CommissionType type, int stage) {
        int s = Clamp(stage);
        var list = new List<RewardItem>();

        switch (type) {
            case CommissionType.Raid:
                list.Add(RewardItem.Diamond(10 + 5 * s));
                list.Add(RewardItem.Exp(ExpPerCharacter(type, s)));
                break;

            case CommissionType.Subjugation:
                list.Add(RewardItem.Gold(4000 + 1500 * s));
                list.Add(RewardItem.Currency(CurrencyKeys.EngravingStone, 15 + 8 * s));
                break;

            case CommissionType.Escort:
                list.Add(RewardItem.Gold(4000 + 1500 * s));
                list.Add(RewardItem.Currency(CurrencyKeys.Renown, 15 + 8 * s));
                break;
        }
        return list;
    }

    /// <summary>보상 목록에 배수를 적용한 사본(즉시해결 N장 미리보기).</summary>
    public static List<RewardItem> ScaleRewards(IReadOnlyList<RewardItem> rewards, int multiplier) {
        var list = new List<RewardItem>(rewards.Count);
        int m = multiplier < 1 ? 1 : multiplier;
        for (int i = 0; i < rewards.Count; i++) {
            var r = rewards[i];
            list.Add(Scale(r, m));
        }
        return list;
    }

    // 표시 수량만 늘린다. 종류별 수량 의미는 서버 ScaleGrant 와 같은 규칙이다.
    private static RewardItem Scale(RewardItem r, int m) => r.Kind switch {
        RewardItemKind.Gold => RewardItem.Gold(r.Amount * m),
        RewardItemKind.Diamond => RewardItem.Diamond(r.Amount * m),
        RewardItemKind.Stamina => RewardItem.Stamina(r.Amount * m),
        RewardItemKind.Exp => RewardItem.Exp(r.Amount * m),
        RewardItemKind.Currency => RewardItem.Currency(r.CurrencyKey, r.Amount * m),
        RewardItemKind.UniversalShard => RewardItem.Shard(r.Grade, r.Amount * m),
        RewardItemKind.RandomEquip => RewardItem.RandomEquip(r.Grade, r.Amount * m),
        _ => r
    };

    /// <summary>
    /// 의뢰 1회당 편성원 1명이 받는 캐릭터 경험치(서버 CommissionCatalog.ExpPerCharacter 미러).
    /// 경험치는 레이드 의뢰 전용이라 토벌/호위는 0 이며, 0 인 보상은 목록에 담기지 않는다.
    /// </summary>
    public static int ExpPerCharacter(CommissionType type, int stage) {
        if (type != CommissionType.Raid) return 0;
        int s = Clamp(stage);
        return 100 + 200 * s * s;
    }

    /// <summary>이 단계의 최초 클리어가 각인 도면을 주는지(서버 GrantsBlueprint 미러).</summary>
    public static bool GrantsBlueprint(CommissionType type, int stage) =>
        type == CommissionType.Subjugation && stage % 2 == 1;

    /// <summary>도전/즉시해결 실패 사유(서버 코드) -> 안내 문구. 2번(입장권 부족)은 즉시해결에서만 나온다.</summary>
    public static string FailMessage(int fail) => fail switch {
        1 => "의뢰 정보를 불러오지 못했습니다.",
        2 => "의뢰해결권이 부족합니다.",
        3 => "잘못된 의뢰입니다.",
        4 => "아직 도전할 수 없는 단계입니다.",
        _ => "의뢰를 진행할 수 없습니다."
    };

    private static int Clamp(int stage) => stage < 1 ? 1 : (stage > MaxStage ? MaxStage : stage);
}
