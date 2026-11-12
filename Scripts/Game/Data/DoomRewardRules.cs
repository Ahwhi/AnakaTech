using System.Collections.Generic;

/// <summary>
/// 멸망전 보상표(표시용 미러). 실제 지급은 전부 서버 Server.Data.DoomCatalog 가 확정하며,
/// 여기는 보상 트랙에 무엇이 걸려 있는지 그리기 위한 사본이다. 두 표의 점수와 내용이 같아야 한다.
///
/// 수령은 구간을 하나씩 누르는 방식이 아니다. "어디까지 받았는가"를 점수로 기억하고,
/// 최고 점수가 올라간 만큼만 다음에 더 받는다(ClaimedScore 초과 ~ BestScore 이하).
/// </summary>
public static class DoomRewardRules {
    public readonly struct Tier {
        public readonly int Score;
        public readonly RewardItem Reward;
        public Tier(int score, RewardItem reward) { Score = score; Reward = reward; }
    }

    /// <summary>보상이 걸린 마지막 점수. 트랙이 이 값을 끝으로 그려진다.</summary>
    public const int MaxScore = EndlessLastScore;

    // V1.12 확장 구간(서버 DoomCatalog 와 같은 규칙). 2,300 부터 100 점마다 아래 보상을 돌려 가며 10,000 점까지.
    private const int EndlessFirstScore = 2300;
    private const int EndlessScoreStep = 100;
    private const int EndlessLastScore = 10000;

    private static readonly RewardItem[] EndlessRewardCycle = {
        RewardItem.Currency(CurrencyKeys.CommissionTicket, 10),
        RewardItem.Item(ItemKeys.ExpCoupon100, 100),
        RewardItem.Diamond(5000),
        RewardItem.Shard(Grade.SSR, 20),
    };

    /// <summary>해골 프로필 테두리 id. 서버 ProfileFrameCatalog.Skull 과 같아야 한다.</summary>
    public const int SkullProfileFrameId = 11;

    /// <summary>흑백 앨범 프로필 테두리 id. 서버 ProfileFrameCatalog.MonoAlbum 과 같아야 한다.</summary>
    public const int MonoAlbumProfileFrameId = 21;

    // 정적 필드는 선언 순서대로 초기화되므로(아래 AuthoredTiers 가 아직 null 일 수 있다) 처음 읽을 때 만든다.
    private static List<Tier> _tiers;
    public static IReadOnlyList<Tier> Tiers => _tiers ??= BuildTiers();

    private static List<Tier> BuildTiers() {
        var list = new List<Tier>(AuthoredTiers);
        int cycle = 0;
        for (int score = EndlessFirstScore; score <= EndlessLastScore; score += EndlessScoreStep)
            list.Add(new Tier(score, EndlessRewardCycle[cycle++ % EndlessRewardCycle.Length]));
        return list;
    }

    // 직접 정한 구간(1~22 페이즈). 그 뒤는 BuildTiers 가 규칙으로 이어 붙인다.
    private static readonly Tier[] AuthoredTiers = {
        new(40,   RewardItem.Diamond(1000)),
        new(80,   RewardItem.Gold(100000)),
        new(120,  RewardItem.Item(ItemKeys.ExpCoupon10, 100)),
        new(160,  RewardItem.Diamond(2000)),
        new(200,  RewardItem.Gold(500000)),
        new(240,  RewardItem.Item(ItemKeys.ExpCoupon20, 100)),
        new(280,  RewardItem.Shard(Grade.N, 50)),
        new(320,  RewardItem.Diamond(3000)),
        new(360,  RewardItem.Currency(CurrencyKeys.CommissionTicket, 10)),
        new(400,  RewardItem.Currency(CurrencyKeys.DispatchCertificate, 500)),
        new(440,  RewardItem.Diamond(5000)),
        new(480,  RewardItem.Gold(1000000)),
        new(520,  RewardItem.Shard(Grade.R, 50)),
        new(560,  RewardItem.Item(ItemKeys.ExpCoupon30, 100)),
        new(600,  RewardItem.Currency(CurrencyKeys.NormalTicket, 100)),
        new(640,  RewardItem.Shard(Grade.SR, 50)),
        new(680,  RewardItem.Diamond(10000)),
        new(720,  RewardItem.Gold(2000000)),
        new(760,  RewardItem.Item(ItemKeys.ExpCoupon50, 100)),
        new(800,  RewardItem.ProfileFrame(SkullProfileFrameId)),
        new(840,  RewardItem.Shard(Grade.SSR, 50)),
        new(880,  RewardItem.Currency(CurrencyKeys.DispatchCertificate, 500)),
        new(920,  RewardItem.Item(ItemKeys.ExpCoupon50, 200)),
        new(960,  RewardItem.Currency(CurrencyKeys.StageSweepTicket, 500)),
        new(1000, RewardItem.Currency(CurrencyKeys.CommissionTicket, 30)),
        new(1040, RewardItem.Diamond(10000)),
        new(1080, RewardItem.Gold(5000000)),
        new(1120, RewardItem.Currency(CurrencyKeys.NormalTicket, 100)),
        new(1160, RewardItem.Currency(CurrencyKeys.SpecialTicket, 10)),
        new(1200, RewardItem.Currency(CurrencyKeys.GachaMileage, 10000)),
        // 13~20 페이즈 확장분.
        new(1240, RewardItem.Item(ItemKeys.ExpCoupon50, 200)),
        new(1280, RewardItem.Gold(5000000)),
        new(1320, RewardItem.Item(ItemKeys.ExpCoupon30, 500)),
        new(1360, RewardItem.Currency(CurrencyKeys.StageSweepTicket, 1000)),
        new(1400, RewardItem.Shard(Grade.SR, 100)),
        new(1440, RewardItem.Shard(Grade.SSR, 50)),
        new(1480, RewardItem.Currency(CurrencyKeys.DispatchCertificate, 500)),
        new(1520, RewardItem.Currency(CurrencyKeys.NormalTicket, 100)),
        new(1560, RewardItem.Currency(CurrencyKeys.SpecialTicket, 10)),
        new(1600, RewardItem.Item(ItemKeys.ExpCoupon50, 500)),
        new(1640, RewardItem.Currency(CurrencyKeys.CommissionTicket, 30)),
        new(1680, RewardItem.Diamond(10000)),
        new(1720, RewardItem.Gold(5000000)),
        new(1760, RewardItem.Currency(CurrencyKeys.SpecialTicket, 10)),
        new(1800, RewardItem.Currency(CurrencyKeys.GachaMileage, 5000)),
        new(1840, RewardItem.Currency(CurrencyKeys.GachaMileage, 10000)),
        new(1880, RewardItem.Diamond(15000)),
        new(1920, RewardItem.Shard(Grade.SSR, 100)),
        new(1960, RewardItem.Currency(CurrencyKeys.CommissionTicket, 50)),
        new(2000, RewardItem.Currency(CurrencyKeys.GachaMileage, 20000)),
        new(2040, RewardItem.Item(ItemKeys.ExpCoupon50, 500)),
        new(2080, RewardItem.Currency(CurrencyKeys.SpecialTicket, 20)),
        new(2120, RewardItem.Shard(Grade.SR, 100)),
        new(2160, RewardItem.Diamond(30000)),
        new(2200, RewardItem.ProfileFrame(MonoAlbumProfileFrameId)),
    };
}
