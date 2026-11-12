using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>패스 계정 현황. 레벨과 수령 판정은 서버가 확정한 값이다.</summary>
public sealed class PassInfoDto {
    /// <summary>패스가 생긴 뒤 쓴 기력 누계(다음 레벨까지의 게이지 표시용).</summary>
    public long StaminaSpent;
    public int Level;
    /// <summary>무료 줄에서 앞에서부터 받은 칸 수(0 = 시작 보상도 안 받음).</summary>
    public int FreeClaimedCount;
    /// <summary>패스 줄에서 앞에서부터 받은 칸 수.</summary>
    public int PassClaimedCount;
    /// <summary>평생 패스 보유.</summary>
    public bool HasPass;
    /// <summary>지금 받을 수 있는 칸이 있는지(버튼/배지 활성).</summary>
    public bool CanClaim;
}

/// <summary>보상 수령 결과. 도달한 칸 전부가 한 번에 들어온다(같은 종류는 서버가 합쳐 보낸다).</summary>
public sealed class PassClaimResultDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=받을것없음,2=계정없음</summary>
    public int FailReason;
    public PassInfoDto Info = new();
    /// <summary>수령 후 골드/다이아/기력 잔액. 서버가 확정한 값이므로 화면이 다시 더하지 않는다.</summary>
    public int NewGold;
    public int NewDiamond;
    public int NewStamina;
    public List<RewardItem> Granted = new();
    /// <summary>받은 장비 개체(굴린 능력치 포함). 장비 가챠와 같은 연출로 보여 주고 가방에 바로 넣는다.</summary>
    public List<EquipRollDto> EquipRolls = new();
}

public interface IPassService {
    /// <summary>
    /// 최신 패스 현황(아직 받지 못했으면 null). 조회 응답과 서버 푸시(새 레벨 도달, 평생 패스 구매)가 같은 값을 갱신하므로
    /// 패스 화면이 열려 있는 동안에도 그 자리에서 바뀐다.
    /// </summary>
    IReadOnlyReactiveProperty<PassInfoDto> Info { get; }

    /// <summary>지금 받을 수 있는 보상이 있는지. 배지가 구독하므로 조건을 채우는 순간 불이 켜진다.</summary>
    IReadOnlyReactiveProperty<bool> HasClaimable { get; }

    UniTask<PassInfoDto> RequestInfoAsync(CancellationToken ct = default);

    /// <summary>현황만 가볍게 다시 받아온다(배지 갱신용). 실패해도 예외를 밖으로 내지 않는다.</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>도달한 칸의 보상을 두 줄 모두 한 번에 받는다(평생 패스가 없으면 무료 줄만).</summary>
    UniTask<PassClaimResultDto> ClaimAsync(CancellationToken ct = default);
}
