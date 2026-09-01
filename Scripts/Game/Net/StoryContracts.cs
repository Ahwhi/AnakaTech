using System.Threading;
using Cysharp.Threading.Tasks;

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

    /// <summary>본 스토리 목록을 서버에서 받아 온다. 실패하면 목록을 비운 채로 둔다(스토리를 한 번 더 보는 쪽이 안전하다).</summary>
    UniTask RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// 스토리를 끝까지 봤다고 보고한다. 성공하면 본 목록에 즉시 반영되어 같은 접속에서 다시 재생되지 않는다.
    /// 실패해도 재생 자체는 이미 끝났으므로 흐름을 막지 않는다(다음 접속에서 한 번 더 나올 뿐이다).
    /// </summary>
    UniTask<bool> CompleteAsync(string storyKey, CancellationToken ct = default);
}
