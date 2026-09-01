using UnityEngine.AddressableAssets;

/// <summary>
/// 스토리 씬에 "무엇을 재생하고 끝나면 어디로 갈지"를 넘기는 정적 전달 지점(MainPopupReturn 과 같은 성격).
///
/// 씬이 바뀌면 DI 컨테이너도 함께 파괴되어 인자를 넘길 수 없으므로 정적으로 둔다.
/// 스토리 씬은 이 예약만 보고 동작하므로, 나중에 게임 중간에 스토리를 끼워 넣을 때도
/// "예약하고 스토리 씬을 연다" 한 가지 방법만 있으면 된다(인트로 전용 경로를 따로 만들지 않는다).
///
/// 규약:
///  - 스토리 씬으로 보내는 쪽이 Request 로 예약한다.
///  - 스토리 씬이 Consume 으로 한 번만 읽어 가고 즉시 비운다(다음 진입에 옛 예약이 되살아나지 않게).
/// </summary>
public static class StoryPlaybackRequest {
    private static string _storyKey;
    private static AssetReference _nextScene;

    /// <param name="storyKey">재생할 스토리(StoryKeys). StoryDatabase 에 같은 키의 에셋이 있어야 한다.</param>
    /// <param name="nextScene">재생이 끝난 뒤 이동할 씬.</param>
    public static void Request(string storyKey, AssetReference nextScene) {
        _storyKey = storyKey;
        _nextScene = nextScene;
    }

    /// <summary>예약을 꺼내고 비운다. 예약이 없으면 false.</summary>
    public static bool Consume(out string storyKey, out AssetReference nextScene) {
        storyKey = _storyKey;
        nextScene = _nextScene;
        bool had = !string.IsNullOrEmpty(_storyKey) && _nextScene != null && _nextScene.RuntimeKeyIsValid();
        _storyKey = null;
        _nextScene = null;
        return had;
    }

    /// <summary>남은 예약을 버린다(로그아웃/연결 끊김 등 스토리 씬으로 가지 않게 된 경우).</summary>
    public static void Clear() {
        _storyKey = null;
        _nextScene = null;
    }
}
