using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 메인 씬(전투/재화/덱) 관련 서버 통신 계약과 순수 DTO. 여기의 타입들은 생성 패킷(C_/S_)에 의존하지 않으므로
// 전투/UI/상태 계층이 자유롭게 참조한다. 패킷 <-> DTO 변환은 구현체(MainDataService)만 담당한다.

/// <summary>메인 진입 시 서버가 내려주는 계정 스냅샷.</summary>
public sealed class MainDataDto {
    public string Nickname = "";
    public int Gold;
    public int Diamond;
    public int Stamina;
    public int StaminaMax;
    public int StaminaBoostTicket;        // 보유 기력 부스트권(자정 자동 충전 반영 후)
    public int StaminaBoostTicketDaily;   // 일일 자동 충전 기준선. 부스트 버튼의 "N / 이 값" 표시에 쓴다.
    public int CurrentDungeonIndex;
    public int HighestClearedDungeonIndex;   // 클리어한 최고 던전 인덱스(0=없음). 해금 표시 기준(DungeonUnlockRule).
    public int EquippedProfileFrameId;
    public List<CharacterProgress> Characters = new();
    public List<int> DeckCharIds = new();
    public int RepresentativeCharId;
}

/// <summary>던전 시작(기력 소모) 결과.</summary>
public sealed class StartDungeonResult {
    public bool IsSuccess;
    public int FailReason;       // 0=성공, 1=기력부족, 2=잘못된던전
    public int RemainingStamina;
}

/// <summary>던전 클리어 보상(서버 확정). 기력은 시작이 아니라 이 시점에 소모된다.</summary>
public sealed class DungeonRewardDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=지갑/덱없음,2=잠긴던전,3=기력부족,4=부스트권부족.</summary>
    public int FailReason;
    /// <summary>이번 클리어를 몇 회분으로 쳤는지(기본 1, 부스트 10). 획득 연출의 배수 표시에 쓴다.</summary>
    public int ClearCount;
    public int GainedGold;
    public int NewGold;
    public int NewStamina;        // 소모 후 남은 기력(서버 확정)
    public int NewBoostTicket;    // 소모 후 남은 기력 부스트권
    /// <summary>기력이 모자란 채로 클리어해 보상이 깎였는지. 획득 표시에서 그 사실을 알린다.</summary>
    public bool StaminaShort;
    public int HighestClearedDungeonIndex;   // 이번 클리어 반영 후 해금 진행도(다음 던전 즉시 해금 표시에 사용)
    public List<CharExpGain> CharExp = new();
    public List<ItemDrop> Drops = new();
}

public sealed class CharExpGain {
    public int CharDataId;
    public int GainedExp;
    public int NewLevel;
    public int NewExp;
    public int MaxExp;   // 새 레벨 기준 필요 경험치(서버 확정). 클라가 성장 공식을 재계산하지 않는다.
    public int GainedStatPoints;
}

public sealed class ItemDrop {
    public int EquipDataId;
    public int Qty;
}

/// <summary>스테이지 소탕 결과. 소탕권 count 장을 써서 진행 중인 던전을 그만큼 클리어한 보상이 담긴다.</summary>
public sealed class StageSweepResult {
    public bool IsSuccess;
    public int FailReason;      // 0=성공,1=인증전/지갑없음,2=잘못된장수,3=소탕권부족,4=잠긴던전
    public int DungeonIndex;
    public int Count;
    public int GainedGold;
    public int NewGold;
    public int NewTicketQty;    // 소모 후 남은 소탕권
    public List<CharExpGain> CharExp = new();
}

/// <summary>
/// 던전 1개의 예상 보상(서버 계산, 지급 없음). 실제 지급(ClearDungeon)과 같은 서버 공식에서 나오므로
/// 표시값과 지급값이 어긋나지 않는다. 던전 선택 UI 등 "보상 미리보기"가 사용한다.
/// </summary>
public sealed class DungeonRewardPreview {
    public int DungeonIndex;
    public int Gold;
    public int ExpPerCharacter;
}

/// <summary>덱 변경 결과.</summary>
public sealed class ChangeDeckResult {
    public bool IsSuccess;
    public int FailReason;       // 0=성공, 1=잘못된구성
}

/// <summary>
/// 편성 프리셋 1칸. CharIds 가 비어 있으면 아직 저장하지 않은 빈 칸이며, UI 는 이때 "현재 스쿼드 저장"을 노출한다.
/// Name 이 비어 있으면 유저가 이름을 지정하지 않은 것이므로 기본 표시명("프리셋 N")으로 대신 보여준다.
/// </summary>
public sealed class DeckPresetDto {
    public int Slot;                     // 0-based
    public int RepresentativeCharId;
    public List<int> CharIds = new();
    public string Name = "";

    public bool IsEmpty => CharIds.Count == 0;

    /// <summary>화면에 쓸 이름. 지정한 이름이 없으면 슬롯 번호로 만든 기본 이름을 돌려준다.</summary>
    public string DisplayName => DeckPresetRules.DisplayName(Slot, Name);
}

/// <summary>프리셋 저장/적용 결과. 성공 시 확정된 구성(서버 값)을 함께 돌려준다.</summary>
public sealed class DeckPresetResult {
    public bool IsSuccess;
    public int FailReason;       // 0=성공,1=잘못된슬롯,2=빈프리셋,3=보유하지않은캐릭터포함
    public DeckPresetDto Preset = new();
}

/// <summary>육성(스탯 투자/별레벨 승급) 결과. 성공 시 갱신된 캐릭터 스냅샷을 담는다.</summary>
public sealed class GrowthResult {
    public bool IsSuccess;
    public int FailReason;
    public CharacterProgress Updated;   // 성공 시 서버가 내려준 최신 성장치
    public int NewDiamond;              // 별레벨 승급 시 차감 후 다이아(스탯 투자에는 미사용)
    public int NewLimitBreakTicket;     // 한계돌파 시 차감 후 한계돌파권(한계돌파 외에는 미사용)
}

/// <summary>
/// 메인 씬 서버 통신 추상화. AttendanceService 와 동일하게 WaitPacketAsync + Send + 타임아웃 패턴으로 구현한다.
/// </summary>
public interface IMainDataService {
    UniTask<MainDataDto> EnterMainAsync(CancellationToken ct = default);
    UniTask<StartDungeonResult> StartDungeonAsync(int dungeonIndex, CancellationToken ct = default);
    /// <summary>던전 클리어 확정. boost 면 기력 부스트권 1장 + 기력 10을 써서 10회분 보상을 한 번에 받는다.</summary>
    UniTask<DungeonRewardDto> ClearDungeonAsync(int dungeonIndex, bool boost, CancellationToken ct = default);

    /// <summary>여러 던전의 예상 보상을 한 번에 조회(표시 전용, 지급 없음).</summary>
    UniTask<List<DungeonRewardPreview>> GetDungeonRewardPreviewAsync(IReadOnlyList<int> dungeonIndexes, CancellationToken ct = default);

    /// <summary>소탕권 count 장을 써서 진행 중인 던전을 그만큼 즉시 클리어한다.</summary>
    UniTask<StageSweepResult> SweepStageAsync(int count, CancellationToken ct = default);
    UniTask<ChangeDeckResult> ChangeDeckAsync(IReadOnlyList<int> charIds, int representativeCharId, bool immediate, CancellationToken ct = default);

    /// <summary>스탯포인트 1 투자. statType 은 0~5(투자 가능 6대 스탯).</summary>
    /// <param name="count">한 번에 넣을 스탯포인트 수(일괄 투자). 가진 포인트보다 많으면 서버가 가진 만큼만 넣는다.</param>
    UniTask<GrowthResult> AllocateStatAsync(int charDataId, StatType statType, int count = 1,
                                            CancellationToken ct = default);

    /// <summary>투자한 스탯포인트를 전부 되돌린다(다이아 소모). 자동 투자 설정도 함께 해제된다.</summary>
    UniTask<GrowthResult> ResetStatsAsync(int charDataId, CancellationToken ct = default);

    /// <summary>스탯포인트 자동 투자 대상 설정. null 이면 해제(미설정).</summary>
    UniTask<GrowthResult> SetAutoStatAsync(int charDataId, StatType? statType, CancellationToken ct = default);

    /// <summary>별레벨 1 승급(조각 소모).</summary>
    UniTask<GrowthResult> UpgradeStarAsync(int charDataId, CancellationToken ct = default);

    /// <summary>한계돌파(한계돌파권 소모). 조건은 서버가 판정한다(LimitBreakRules 는 표시용 미러).</summary>
    UniTask<GrowthResult> LimitBreakAsync(int charDataId, CancellationToken ct = default);

    /// <summary>편성 프리셋 전체 조회(빈 칸은 목록에 없다).</summary>
    UniTask<List<DeckPresetDto>> GetDeckPresetsAsync(CancellationToken ct = default);

    /// <summary>현재 덱을 지정 슬롯에 저장(덮어쓰기).</summary>
    UniTask<DeckPresetResult> SaveDeckPresetAsync(int slot, CancellationToken ct = default);

    /// <summary>지정 슬롯의 프리셋을 현재 덱으로 적용(서버가 덱을 교체한다).</summary>
    UniTask<DeckPresetResult> ApplyDeckPresetAsync(int slot, CancellationToken ct = default);

    /// <summary>프리셋 칸의 이름 변경. 빈 이름을 보내면 기본 표시명("프리셋 N")으로 되돌아간다.</summary>
    UniTask<DeckPresetResult> RenameDeckPresetAsync(int slot, string name, CancellationToken ct = default);
}
