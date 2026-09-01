using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

// 파견 통신 계약과 순수 DTO(생성 패킷 비의존). 파견 UI/상태는 이 타입들만 참조한다.

/// <summary>파견 쓰기 작업 종류. 값은 서버 Room.DispatchAction 의 action 상수와 일치해야 한다.</summary>
public enum DispatchActionType {
    Send = 0,      // 편성을 넣어 파견 보내기
    Refresh = 1,   // 슬롯의 조건을 다른 조건으로 새로고침
    Claim = 2      // 완료된 파견의 보상 수령
}

/// <summary>파견 조건의 종류(서버 DispatchConditionKind 와 값 일치. 새 종류는 항상 끝에 추가).</summary>
public enum DispatchConditionKind {
    WeaponType = 0,   // Param=직업(0=검..4=너클), Threshold=최소 인원
    StarTotal = 1,    // Threshold=별레벨 총합 하한
    GradeCount = 2,   // Param=등급(0=N,1=R,2=SR), Threshold=최소 인원
    LevelTotal = 3    // Threshold=레벨 총합 하한
}

/// <summary>파견 조건 1개(서버 카탈로그 그대로). 화면은 이 값으로 문구를 만들고 판정도 이 값으로 미리 본다.</summary>
public sealed class DispatchConditionDto {
    public int ConditionId;
    public DispatchConditionKind Kind;
    public int Param;
    public int Threshold;
    public int Tier;              // DispatchRewardTier(표시/보상 조회용)
    public int DurationSeconds;
}

/// <summary>파견 슬롯 1칸의 상태. Members 가 비어 있으면 대기 중(조건만 걸려 있음)이다.</summary>
public sealed class DispatchSlotDto {
    public int SlotIndex;
    public int ConditionId;
    public List<int> Members = new();
    public long StartUnix;
    public long EndUnix;

    /// <summary>파견을 보낸 상태인지(완료 대기 포함).</summary>
    public bool IsRunning => Members.Count > 0;

    /// <summary>시간이 다 돼서 완료 버튼을 누를 수 있는 상태인지.</summary>
    public bool IsFinished(long nowUnix) => IsRunning && EndUnix > 0 && EndUnix <= nowUnix;
}

/// <summary>파견 팝업 진입 스냅샷. 조건표와 보상 구성까지 서버가 함께 내려주므로 화면이 수치를 만들지 않는다.</summary>
public sealed class DispatchInfoDto {
    public int RefreshCharges;
    public int MaxRefreshCharges;
    public int MembersPerSlot;
    public List<DispatchSlotDto> Slots = new();
    public List<DispatchConditionDto> Conditions = new();
    /// <summary>보상 등급 -> 그 등급의 구성품. 슬롯 카드가 RewardSlotView 로 그린다.</summary>
    public Dictionary<int, List<RewardItem>> TierGrants = new();

    public DispatchConditionDto Condition(int conditionId) {
        foreach (var c in Conditions) if (c.ConditionId == conditionId) return c;
        return null;
    }

    /// <summary>조건이 걸린 슬롯이 완료 시 받을 보상. 조건을 모르면 빈 목록.</summary>
    public IReadOnlyList<RewardItem> RewardsFor(int conditionId) {
        var condition = Condition(conditionId);
        if (condition != null && TierGrants.TryGetValue(condition.Tier, out var list)) return list;
        return System.Array.Empty<RewardItem>();
    }
}

/// <summary>파견 쓰기 작업 결과. 어떤 작업이든 바뀐 슬롯 하나가 통째로 담겨 온다.</summary>
public sealed class DispatchActionResultDto {
    public bool IsSuccess;
    public int FailReason;
    public DispatchActionType Action;
    public int SlotIndex;
    public int ConditionId;
    public List<int> Members = new();
    public long StartUnix;
    public long EndUnix;
    public int RefreshCharges;
    public int NewGold;
    /// <summary>완료 수령에서 실제로 지급된 보상(그 외 작업에서는 비어 있다).</summary>
    public List<RewardItem> Granted = new();
}

/// <summary>파견 통신 추상화. UI 는 이 계약에만 의존한다.</summary>
public interface IDispatchService {
    /// <summary>
    /// 수령할 수 있는(시간이 다 된) 파견이 하나라도 있는지. 파견/햄버거 버튼 배지가 구독한다.
    /// 조회/작업 응답이 갱신하고, 진행 중인 파견이 도중에 끝나는 것도 서비스가 스스로 감지해 켠다.
    /// </summary>
    IReadOnlyReactiveProperty<bool> HasClaimable { get; }

    /// <summary>
    /// 서버가 파견 상태를 바꿔 알려 왔을 때 발생한다(운영 도구/개발 명령 등). 열려 있는 화면이
    /// 이 신호를 받아 다시 조회하면, 서버에서만 일어난 변경도 화면에 바로 따라온다.
    /// </summary>
    IObservable<Unit> ChangedByServer { get; }

    UniTask<DispatchInfoDto> RequestInfoAsync(CancellationToken ct = default);

    /// <summary>파견 쓰기 작업 공용 창구. 작업에 따라 쓰는 인자가 다르다.</summary>
    UniTask<DispatchActionResultDto> ActAsync(DispatchActionType action, int slotIndex = 0,
                                              IReadOnlyList<int> members = null, CancellationToken ct = default);

    /// <summary>수령 가능 여부만 최신화한다(배지 초기 로드용). 실패해도 조용히 넘어간다.</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// 배지 판정 근거(진행 중인 슬롯의 완료 시각)를 갱신한다. 파견 작업 직후처럼 화면이 이미 아는 최신 상태를
    /// 그대로 넘겨, 배지를 맞추려고 서버를 다시 두드리지 않게 한다.
    /// </summary>
    void TrackEndTimes(IReadOnlyList<DispatchSlotDto> slots);
}
