using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 장비 인벤토리 통신 계약과 DTO(생성 패킷 비의존).

/// <summary>
/// 보유 장비 인스턴스 1개(서버 권위 미러). 같은 장비 id 여도 인스턴스마다 수치가 다른 개체다.
/// 최종 스탯은 EquipmentRules.ResolveStats(부위, 등급, Level, MainPoints, OptionPoints) 로 계산한다.
/// </summary>
public sealed class EquipmentInstanceDto {
    public long InstanceId;
    public int EquipDataId;

    /// <summary>각성 단계(1~15). 최대 레벨 상한 = StarLevel x 10.</summary>
    public int StarLevel = 1;
    /// <summary>레벨(1~150). 레벨업으로 주스탯/옵션이 확정 상승한다.</summary>
    public int Level = 1;

    /// <summary>드랍 시 굴린 주스탯 기본 포인트(성장분 제외).</summary>
    public int MainPoints;
    /// <summary>드랍 시 굴린 1~4옵션 기본 포인트(성장분 제외). 등급이 개방한 개수까지만 유효하다.</summary>
    public readonly int[] OptionPoints = new int[EquipmentStatLayout.MaxOptionSlots];

    /// <summary>장착 중인 캐릭터 id(0=가방).</summary>
    public int EquippedCharDataId;

    public bool IsEquipped => EquippedCharDataId != 0;
}

public sealed class InventoryDto {
    public List<EquipmentInstanceDto> Equipment = new();
    public Dictionary<int, int> UniversalShards = new();   // grade -> qty
}

public sealed class ConvertShardResult {
    public bool IsSuccess;
    public int FailReason;
    public int CharDataId;
    public int NewCharShards;
    public int Grade;
    public int NewUniversalQty;
}

public sealed class EquipActionResult {
    public bool IsSuccess;
    public int FailReason;
}

/// <summary>각성(별레벨 상승) 결과. 재료 장비는 서버가 소멸시키므로 성공 시 인벤토리를 다시 받아야 한다.</summary>
public sealed class AwakenResultDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=인스턴스없음,2=재료부족/부적합,3=다이아부족,4=최대별레벨,5=메타없음</summary>
    public int FailReason;
    public int NewStarLevel;
    public int NewDiamond;
}

/// <summary>레벨업 결과. 요청한 횟수 중 실제로 오른 만큼만 반영된다(골드/상한에서 멈춤).</summary>
public sealed class LevelUpResultDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=인스턴스없음,2=골드부족,3=최대레벨,4=메타없음</summary>
    public int FailReason;
    public int NewLevel;
    public int NewGold;
    /// <summary>실제로 오른 레벨 수(0이면 한 단계도 오르지 못함).</summary>
    public int GainedLevels;
}

public interface IInventoryService {
    UniTask<InventoryDto> RequestInventoryAsync(CancellationToken ct = default);
    UniTask<EquipActionResult> EquipAsync(long instanceId, int charDataId, CancellationToken ct = default);
    UniTask<EquipActionResult> UnequipAsync(long instanceId, CancellationToken ct = default);

    /// <summary>같은 장비를 재료로 소모해 각성(별레벨 +1)한다. 재료 수/다이아는 EquipmentRules 규칙을 서버가 재검증한다.</summary>
    UniTask<AwakenResultDto> AwakenAsync(long instanceId, IReadOnlyList<long> materialInstanceIds, CancellationToken ct = default);

    /// <summary>골드로 레벨업한다. count 만큼 시도하고 골드/상한에 걸리면 그 지점까지만 올린다.</summary>
    UniTask<LevelUpResultDto> LevelUpAsync(long instanceId, int count, CancellationToken ct = default);

    /// <summary>만능조각을 캐릭터 조각으로 전환(등급 일치).</summary>
    UniTask<ConvertShardResult> ConvertUniversalShardAsync(int charDataId, int amount, CancellationToken ct = default);
}
