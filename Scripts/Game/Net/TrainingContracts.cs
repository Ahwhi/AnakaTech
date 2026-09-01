using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

// 수련(집중 수련) 통신 계약과 순수 DTO(생성 패킷 비의존). 수련 UI/상태는 이 타입들만 참조한다.

/// <summary>수련 쓰기 작업 종류. 값은 서버 Room.Training 의 action 상수와 일치해야 한다.</summary>
public enum TrainingActionType {
    Start = 0,        // 슬롯에 소녀를 넣어 수련 시작
    Cancel = 1,       // 진행 중인 수련 중단(효과 없음)
    Claim = 2,        // 끝난 수련 수령(이 시점에 효과가 확정된다)
    Instant = 3,      // 수련 완료권으로 남은 시간을 즉시 당김
    UnlockSlot = 4    // 다음 슬롯을 다이아로 해금
}

/// <summary>수련 슬롯 1칸의 진행 상태. 비어 있는 슬롯은 목록에 담기지 않는다.</summary>
public sealed class TrainingSlotDto {
    public int SlotIndex;
    public int CharDataId;
    public long StartUnix;
    public long EndUnix;
}

/// <summary>소녀 1명이 보유한 수련 효과(캐릭터당 최대 1개).</summary>
public struct TrainingResultDto {
    public int CharDataId;
    public int Effect;        // TrainingEffect
    public int ValueTenth;    // 효과 수치 x 10
}

/// <summary>
/// 확률표 1줄(확률 보기). 서버가 실제 추첨에 쓰는 카탈로그를 그대로 내려주므로 화면이 수치를 만들지 않는다.
/// 확률 계산은 이 값들만으로 정확히 유도된다(반올림 없는 유리수).
/// </summary>
public struct TrainingRateDto {
    public int Effect;
    public int Weight;        // 이 효과가 뽑힐 가중치
    public int TotalWeight;   // 전체 가중치 합(등장 확률의 분모)
    public int MinTenth;
    public int MaxTenth;
    public int StepTenth;

    /// <summary>뽑을 수 있는 수치 칸의 개수(각 칸의 추첨 확률 = 1 / 이 값).</summary>
    public int ValueCount => StepTenth <= 0 ? 1 : (MaxTenth - MinTenth) / StepTenth + 1;

    /// <summary>효과 종류가 등장할 확률(0~1).</summary>
    public double EffectRate => TotalWeight <= 0 ? 0d : (double)Weight / TotalWeight;

    /// <summary>수치 1칸이 뽑힐 확률(0~1). 효과 안에서는 균등이다.</summary>
    public double ValueRate => ValueCount <= 0 ? 0d : 1d / ValueCount;

    /// <summary>"이 효과의 이 수치"가 나올 최종 확률(0~1).</summary>
    public double FinalRate => EffectRate * ValueRate;
}

/// <summary>수련 팝업 진입 스냅샷.</summary>
public sealed class TrainingInfoDto {
    public int UnlockedSlots;
    public int MaxSlots;
    /// <summary>다음 슬롯 해금 비용(다이아). 0 이면 더 해금할 슬롯이 없다.</summary>
    public int NextUnlockCost;
    public int DurationSeconds;
    public int TrainingTicket;
    public int InstantTicketCost;
    public List<TrainingSlotDto> Slots = new();
    public List<TrainingResultDto> Results = new();
    public List<TrainingRateDto> Rates = new();
}

/// <summary>수련 쓰기 작업 결과. 작업에 따라 채워지는 칸이 다르다(Effect 가 -1 이면 새 효과 없음).</summary>
public sealed class TrainingActionResultDto {
    public bool IsSuccess;
    public int FailReason;
    public TrainingActionType Action;
    public int SlotIndex;
    public int CharDataId;
    public int NewDiamond;
    public int NewTrainingTicket;
    public int UnlockedSlots;
    public long StartUnix;
    public long EndUnix;
    public int Effect = -1;
    public int ValueTenth;
}

/// <summary>수련 통신 추상화. UI 는 이 계약에만 의존한다.</summary>
public interface ITrainingService {
    /// <summary>
    /// 수령할 수 있는(시간이 다 된) 수련이 하나라도 있는지. 메인 수련 버튼 배지가 구독한다.
    /// 조회/작업 응답이 갱신하고, 진행 중인 수련이 도중에 끝나는 것도 서비스가 스스로 감지해 켠다.
    /// </summary>
    IReadOnlyReactiveProperty<bool> HasClaimable { get; }

    UniTask<TrainingInfoDto> RequestInfoAsync(CancellationToken ct = default);

    /// <summary>수련 쓰기 작업 공용 창구. 작업에 따라 쓰는 인자가 다르다(해금은 둘 다 쓰지 않는다).</summary>
    UniTask<TrainingActionResultDto> ActAsync(TrainingActionType action, int slotIndex = 0, int charDataId = 0,
                                              CancellationToken ct = default);

    /// <summary>수령 가능 여부만 최신화한다(배지 초기 로드용). 실패해도 조용히 넘어간다.</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// 배지 판정 근거(진행 중인 슬롯의 완료 시각)를 갱신한다. 수련 작업 직후처럼 화면이 이미 아는 최신 상태를
    /// 그대로 넘겨, 배지를 맞추려고 서버를 다시 두드리지 않게 한다.
    /// </summary>
    void TrackEndTimes(IReadOnlyList<TrainingSlotDto> slots);
}
