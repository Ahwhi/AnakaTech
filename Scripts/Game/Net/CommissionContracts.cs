using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 의뢰 / 각인 / 용병단 본부 관련 서버 통신 계약과 순수 DTO. 생성 패킷(C_/S_)에 의존하지 않으므로
// 전투/UI/상태 계층이 자유롭게 참조한다. 패킷 <-> DTO 변환은 구현체(각 Service)만 담당한다.

// ── 의뢰 ────────────────────────────────────────────────

/// <summary>의뢰 종류 1개의 진행도.</summary>
public struct CommissionStageDto {
    public int CommissionType;
    public int ClearedStage;
}

/// <summary>의뢰 팝업 진입 시 받는 스냅샷(입장권/재화/진행도).</summary>
public sealed class CommissionInfoDto {
    public int Tickets;
    public int TicketMax;
    public int Renown;
    public int EngravingStone;
    public List<CommissionStageDto> Stages = new();
}

/// <summary>
/// 의뢰 도전 시작 결과. 의뢰도전은 무료라 입장권을 쓰지 않으며, 서버는 "그 단계를 지금 도전할 수 있는지"만 확정한다.
/// RemainingTickets 는 소모 결과가 아니라 접근 시점의 최신 보유량(지연 충전 반영)이다.
/// </summary>
public sealed class StartCommissionResult {
    public bool IsSuccess;
    public int FailReason;       // 0=성공,1=상태없음,3=잘못된의뢰,4=미해금단계
    public int RemainingTickets;
}

/// <summary>
/// 의뢰 보상 수령 결과(클리어/즉시해결 공용). 지급은 이미 서버에서 끝났고 여기 값은 반영/표시용이다.
/// NewTickets 가 음수면 "이 응답은 입장권을 건드리지 않음"을 뜻한다(클리어 보고는 입장권을 쓰지 않는다).
/// </summary>
public sealed class CommissionRewardDto {
    public bool IsSuccess;
    public int FailReason;       // 0=성공,1=상태없음,2=입장권부족,3=잘못된의뢰,4=미해금단계
    public int CommissionType;
    public int ClearedStage;
    public int NewTickets = -1;
    public int NewGold;
    public int NewDiamond;
    public int NewRenown;
    public int NewEngravingStone;
    public List<RewardItem> Granted = new();
    public List<CharExpGain> CharExp = new();
}

/// <summary>의뢰 컨텐츠 서버 통신 추상화.</summary>
public interface ICommissionService {
    UniTask<CommissionInfoDto> RequestInfoAsync(CancellationToken ct = default);

    /// <summary>도전 시작(무료). 성공해야 의뢰 씬으로 넘어간다. 입장권은 소모되지 않는다.</summary>
    UniTask<StartCommissionResult> StartAsync(CommissionType type, int stage, CancellationToken ct = default);

    /// <summary>
    /// 클리어 보고. 그 단계를 처음 깼을 때만 1회분 보상 + 최초 보상/각인 도면을 받는다
    /// (이미 깬 단계를 다시 깨면 보상이 없다 - 무료 반복 파밍 차단).
    /// </summary>
    UniTask<CommissionRewardDto> ClearAsync(CommissionType type, int stage, CancellationToken ct = default);

    /// <summary>즉시해결. 의뢰해결권 count 장을 쓰고 클리어한 최고 단계의 1회분 보상을 count 배로 받는다.</summary>
    UniTask<CommissionRewardDto> SweepAsync(CommissionType type, int count, CancellationToken ct = default);

    /// <summary>
    /// 도전 종료 보고(응답 없음). 승패와 무관하게 "끝까지 싸웠다"를 알린다. 메인 퀘스트의 "의뢰 1회 도전"이
    /// 실패도 인정하기 때문에 필요하며, 도중에 나가면 보내지 않으므로 그때는 인정되지 않는다.
    /// 보상/진행도와는 무관하다(그쪽은 ClearAsync 가 담당한다).
    /// </summary>
    void ReportAttempt(CommissionType type);
}

// ── 각인 ────────────────────────────────────────────────

public struct OwnedEngravingDto {
    public int EngravingId;
    public int Level;
}

public struct EquippedEngravingDto {
    public int CharDataId;
    public int SlotIndex;
    public int EngravingId;
}

public sealed class EngravingInfoDto {
    public int EngravingStone;
    public List<OwnedEngravingDto> Owned = new();
    public List<EquippedEngravingDto> Equipped = new();
}

public sealed class EngravingUpgradeResult {
    public bool IsSuccess;
    public int FailReason;       // 0=성공,1=미보유,2=최대레벨,3=각인석부족,4=골드부족,5=잘못된id
    public int EngravingId;
    public int Level;
    public int NewEngravingStone;
}

public sealed class EngravingEquipResult {
    public bool IsSuccess;
    public int FailReason;       // 0=성공,1=캐릭없음,2=잘못된슬롯,3=슬롯미해금,4=미보유각인,5=중복장착
    public int CharDataId;
    public int SlotIndex;
    public int EngravingId;
}

/// <summary>각인 서버 통신 추상화. 보유/레벨은 계정 공용이고 장착만 캐릭터별이다.</summary>
public interface IEngravingService {
    UniTask<EngravingInfoDto> RequestInfoAsync(CancellationToken ct = default);
    UniTask<EngravingUpgradeResult> UpgradeAsync(int engravingId, CancellationToken ct = default);

    /// <summary>슬롯에 각인을 장착한다. engravingId=0 이면 해제.</summary>
    UniTask<EngravingEquipResult> EquipAsync(int charDataId, int slotIndex, int engravingId, CancellationToken ct = default);
}

// ── 용병단 본부 ──────────────────────────────────────────

public struct HqNodeDto {
    public int NodeId;
    public int Level;
}

public sealed class HeadquartersDto {
    public int Renown;
    public List<HqNodeDto> Nodes = new();
}

public sealed class HqUpgradeResult {
    public bool IsSuccess;
    public int FailReason;       // 0=성공,1=잘못된노드,2=최대레벨,3=선행노드미해금,4=명성부족
    public int NodeId;
    public int Level;
    public int NewRenown;
}

/// <summary>용병단 본부(계정 단위 성장 노드) 서버 통신 추상화.</summary>
public interface IHeadquartersService {
    UniTask<HeadquartersDto> RequestInfoAsync(CancellationToken ct = default);
    UniTask<HqUpgradeResult> UpgradeNodeAsync(int nodeId, CancellationToken ct = default);
}
