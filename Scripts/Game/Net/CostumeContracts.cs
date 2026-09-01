using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

// 코스튬 통신 계약과 순수 DTO(생성 패킷 비의존). 코스튬 UI/상태는 이 타입들만 참조한다.

/// <summary>캐릭터별 장착 코스튬 1건.</summary>
public sealed class EquippedCostumeDto {
    public int CharDataId;
    public int CostumeId;
}

/// <summary>코스튬 보유/장착 스냅샷. 보유는 계정 단위, 장착은 캐릭터 단위라 한 번의 조회로 둘 다 받는다.</summary>
public sealed class CostumeInfoDto {
    /// <summary>보유 중인 코스튬 id 전체.</summary>
    public List<int> Owned = new();
    /// <summary>장착 중인 캐릭터만. 기본 일러스트인 캐릭터는 담기지 않는다.</summary>
    public List<EquippedCostumeDto> Equipped = new();
}

/// <summary>코스튬 장착/해제 결과.</summary>
public sealed class CostumeEquipResult {
    public bool IsSuccess;
    /// <summary>0=성공, 1=캐릭없음, 2=없는코스튬, 3=이캐릭터의코스튬아님, 4=미보유.</summary>
    public int FailReason;
    public int CharDataId;
    public int CostumeId;
}

/// <summary>코스튬 통신 추상화. UI 는 이 계약에만 의존한다.</summary>
public interface ICostumeService {
    UniTask<CostumeInfoDto> RequestInfoAsync(CancellationToken ct = default);

    /// <summary>코스튬 장착(costumeId=0 이면 기본 일러스트로 되돌린다). 보유/소유 판정은 서버가 최종 결정한다.</summary>
    UniTask<CostumeEquipResult> EquipAsync(int charDataId, int costumeId, CancellationToken ct = default);
}

/// <summary>코스튬 장착 실패 사유 문구. 서버 GameDataRepository.EquipCostume 의 실패 코드와 짝을 이룬다.</summary>
public static class CostumeFailMessage {
    public static string Of(int failReason) => failReason switch {
        1 => "보유하지 않은 캐릭터입니다.",
        2 => "존재하지 않는 코스튬입니다.",
        3 => "이 캐릭터의 코스튬이 아닙니다.",
        4 => "보유하지 않은 코스튬입니다.",
        _ => "코스튬을 변경하지 못했습니다."
    };
}
