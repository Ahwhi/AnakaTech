using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>
/// 스토리 통신 추상화. 스토리의 내용은 전부 클라 에셋(StoryDatabase)이 갖고, 서버가 가진 것은
/// "이 계정이 저 스토리를 끝까지 봤는가" 하나뿐이라 계약도 조회와 완료 보고 둘로 끝난다.
///
/// 본 목록은 서비스가 들고 있는다. 타이틀에서 인트로 재생 여부를 묻고, 스토리 씬이 끝나면 같은 값을 갱신하며,
/// 이후 어느 화면이 다시 물어도 서버를 두 번 두드리지 않게 하기 위함이다.
/// </summary>
public interface IStoryService {
    /// <summary>이 계정이 이미 끝까지 본 스토리인지(RefreshAsync 로 받아 둔 목록 기준).</summary>
    bool IsCompleted(string storyKey);

    /// <summary>
    /// 장 스토리 19편의 현황(RefreshAsync 로 받아 둔 것). 스토리 팝업과 배지가 같은 목록을 본다.
    /// 아직 받아 오지 않았으면 빈 목록이다.
    /// </summary>
    IReadOnlyList<StoryChapterStateDto> Chapters { get; }

    /// <summary>현황이 바뀔 때마다 오르는 번호. 화면이 다시 그릴지 판단하는 데 쓴다.</summary>
    int Version { get; }

    /// <summary>받을 수 있는 보상이 있거나 새로 읽을 수 있는 편이 있는지(메인 버튼 배지).</summary>
    IReadOnlyReactiveProperty<bool> HasBadge { get; }

    /// <summary>완독 보상을 받는다(버튼). 읽지 않았거나 이미 받은 편은 서버가 거부한다.</summary>
    UniTask<StoryClaimResultDto> ClaimRewardAsync(string storyKey, CancellationToken ct = default);

    /// <summary>본 스토리 목록을 서버에서 받아 온다. 실패하면 목록을 비운 채로 둔다(스토리를 한 번 더 보는 쪽이 안전하다).</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// 스토리를 끝까지 봤다고 보고한다. 성공하면 본 목록에 즉시 반영되어 같은 접속에서 다시 재생되지 않는다.
    /// 실패해도 재생 자체는 이미 끝났으므로 흐름을 막지 않는다(다음 접속에서 한 번 더 나올 뿐이다).
    /// </summary>
    UniTask<bool> CompleteAsync(string storyKey, CancellationToken ct = default);
}

/// <summary>
/// 장 스토리 한 편의 계정 현황. 해금 판정은 서버가 하고(진도 + 직전 편 완독) 클라는 받아서 그린다.
/// 두 곳에서 따로 계산하면 규칙이 어긋나 "열려 보이는데 눌리면 거부되는" 카드가 생긴다.
/// </summary>
public sealed class StoryChapterStateDto {
    public string StoryKey = "";
    /// <summary>1-based 장 번호. 키에서 뽑아 두어 화면이 문자열을 다시 파싱하지 않게 한다.</summary>
    public int Chapter;
    public bool Completed;
    public bool RewardClaimed;
    public bool Unlocked;

    /// <summary>지금 읽을 수 있는 편인지(열렸지만 아직 안 읽음).</summary>
    public bool CanRead => Unlocked && !Completed;
    /// <summary>받을 수 있는 보상이 있는지(읽었지만 아직 안 받음).</summary>
    public bool CanClaim => Completed && !RewardClaimed;
}

/// <summary>완독 보상 수령 결과.</summary>
public sealed class StoryClaimResultDto {
    public bool IsSuccess;
    /// <summary>0=성공,1=없는스토리,2=아직안읽음,3=이미받음,4=보상없음,5=지갑없음</summary>
    public int FailReason;
    public string StoryKey = "";
    public int Diamond;
    public int NewDiamond;
}
