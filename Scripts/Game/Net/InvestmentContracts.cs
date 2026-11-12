using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 투자 통신 계약과 순수 DTO(생성 패킷 비의존). 투자 화면과 상태는 이 타입만 참조한다.

/// <summary>
/// 투자 슬롯이 무엇을 올려 주는지. 값은 서버 InvestmentEffect 와 일치해야 한다(패킷에 실리지는 않지만
/// 슬롯 정의가 양쪽에 있으므로 값이 어긋나면 표시와 실제가 달라진다).
/// </summary>
public enum InvestmentEffect {
    StageExp = 0,
    OfflineExp = 1,
    RaidCommissionExp = 2,
    SkillGaugeStart = 3,
    SquadRegen = 4,
    // 전투 스탯 효과. 캐릭터 전원의 스탯 보정으로 합산된다(InvestmentRules.StatOf).
    DoubleAttackChance = 5,
    EvasionChance = 6,
    CritRate = 7,
    MinDamageRatio = 8,
    MaxDamageRatio = 9,
    DamageTakenReduction = 10,
    FinalDamage = 11,
    DoomExp = 12,
    FrontlineExp = 13
}

/// <summary>투자 현황. 슬롯 해금은 별레벨 총합으로 그때그때 정해지므로 총합을 함께 받는다.</summary>
public sealed class InvestmentDto {
    /// <summary>슬롯 번호 -> 레벨. 담기지 않은 슬롯은 0 레벨이다.</summary>
    public Dictionary<int, int> Levels = new();
    /// <summary>보유 캐릭터의 별레벨 총합(서버 판정 기준).</summary>
    public int StarTotal;
    public int NewGold;

    public int LevelOf(int slotIndex) => Levels.TryGetValue(slotIndex, out int lv) ? lv : 0;
}

/// <summary>슬롯 업그레이드 결과. 실패해도 현재 값이 담겨 오므로 화면이 다시 물어보지 않는다.</summary>
public sealed class InvestmentUpgradeResultDto {
    public bool IsSuccess;
    public int FailReason;
    public int SlotIndex;
    public int NewLevel;
    public int SpentGold;
    public int NewGold;
    public int SpentDiamond;
    public int NewDiamond;
    public int StarTotal;
}

/// <summary>
/// 투자 통신 추상화. 현황 조회와 업그레이드 둘뿐이며, 효과 계산은 클라가 하지 않는다
/// (경험치 증폭은 서버가, 전투 효과는 레벨을 근거로 전투 시뮬레이션이 적용한다).
/// </summary>
public interface IInvestmentService {
    UniTask<InvestmentDto> RequestAsync(CancellationToken ct = default);
    /// <param name="expectedLevel">화면에서 보고 누른 현재 레벨. 서버 레벨과 다르면 서버가 거부한다(실패 7).</param>
    UniTask<InvestmentUpgradeResultDto> UpgradeAsync(int slotIndex, int expectedLevel, CancellationToken ct = default);
}
