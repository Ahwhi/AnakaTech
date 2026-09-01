using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using static Enums;

/// <summary>
/// 스토리 씬 오케스트레이션. 예약(StoryPlaybackRequest)이 가리키는 스토리를 재생하고, 끝나면 서버에 완료를 보고한 뒤
/// 예약된 다음 씬으로 넘어간다. 재생 자체는 StoryPlayerView 가 하고 여기서는 흐름만 정한다.
///
/// 스토리를 별도 씬으로 둔 이유는 "스토리가 끝나야 게임이 진행된다"를 구조로 보장하기 위해서다.
/// 메인 씬 위에 얹으면 방치 전투가 뒤에서 이미 돌고 있게 되고, 팝업을 닫는 다른 경로(연결 끊김 정리 등)가
/// 스토리를 건너뛰게 만들 수 있다. 씬이 분리돼 있으면 다음 씬으로 넘어가는 길이 이 스크립트 하나뿐이다.
///
/// 게임 중간에 스토리를 넣을 때도 같은 방법을 쓴다. 예약을 남기고 이 씬을 열면 되고, 끝난 뒤 돌아갈 씬만 다르다.
///
/// 씬 배치: StoryPlayerView + 이 컴포넌트(둘 다 StoryLifetimeScope 가 주입).
/// </summary>
public sealed class StorySceneManager : MonoBehaviour {
    [Header("View")]
    [Tooltip("스토리를 그리는 뷰. 같은 씬 하이어라키에 배치한다.")]
    [SerializeField] private StoryPlayerView playerView;

    [Header("Fallback")]
    [Tooltip("예약이 없거나 스토리 데이터를 찾지 못했을 때 이동할 씬(보통 메인 씬). 흐름이 여기서 멈추지 않게 하는 안전망이다.")]
    [SerializeField] private AssetReference fallbackScene;

    private StoryDatabase _storyDb;
    private IStoryService _story;
    private ISceneService _scene;
    private IUIService _ui;
    private UIPopupCatalog _popups;
    private ISoundService _sound;

    private string _storyKey;
    private AssetReference _nextScene;
    private bool _leaving;
    private bool _bgmPlaying;   // 이 씬이 켠 BGM 이 울리는 중인지. 내가 켠 경우에만 끈다.

    [Inject]
    public void Construct(StoryDatabase storyDb, IStoryService story, ISceneService scene,
                          IUIService ui, UIPopupCatalog popups, ISoundService sound) {
        _storyDb = storyDb;
        _story = story;
        _scene = scene;
        _ui = ui;
        _popups = popups;
        _sound = sound;
    }

    private void Start() {
        RunAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid RunAsync(CancellationToken ct) {
        if (!StoryPlaybackRequest.Consume(out _storyKey, out _nextScene)) {
            Debug.LogError("[StorySceneManager] 재생 예약 없이 스토리 씬에 들어왔습니다. 대체 씬으로 넘어갑니다.");
            await LeaveAsync(fallbackScene, ct);
            return;
        }

        var story = _storyDb != null ? _storyDb.Get(_storyKey) : null;
        if (story == null || !story.IsPlayable) {
            // 데이터가 없다고 게임을 막을 수는 없다. 다음 씬으로 넘겨 흐름을 잇고 원인은 로그로 남긴다.
            Debug.LogError($"[StorySceneManager] 스토리 데이터를 찾을 수 없습니다: key={_storyKey}");
            await LeaveAsync(_nextScene, ct);
            return;
        }

        // 재생할 스토리가 확정된 뒤에만 BGM 을 켠다(예약이 없어 곧바로 넘어가는 경로에서 한 소절만 울리지 않도록).
        PlayBgm();

        if (playerView != null) {
            playerView.SkipRequested += OnSkipRequested;
            try {
                await playerView.PlayAsync(story, ct);
            } catch (OperationCanceledException) {
                return;
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                playerView.SkipRequested -= OnSkipRequested;
            }
        } else {
            Debug.LogError("[StorySceneManager] playerView 가 인스펙터에 할당되지 않았습니다.");
        }

        // 완료 보고는 실패해도 흐름을 막지 않는다(다음 접속에서 한 번 더 나올 뿐이고, 게임은 진행돼야 한다).
        await _story.CompleteAsync(_storyKey, ct);
        await LeaveAsync(_nextScene, ct);
    }

    // 스토리 전용 BGM. 씬이 살아 있는 동안만 울리고, 다음 씬은 자기 BGM 을 직접 시작한다.
    private void PlayBgm() {
        if (_sound == null || _bgmPlaying) return;
        _sound.PlayBgm(BgmKey.Story);
        _bgmPlaying = true;
    }

    private void OnDestroy() {
        if (_bgmPlaying) _sound?.StopBgm();
    }

    // 건너뛰기는 되돌릴 수 없으므로 한 번 물어본다(상용 게임의 공통 규약).
    private void OnSkipRequested() => ConfirmSkipAsync(this.GetCancellationTokenOnDestroy()).Forget();

    private async UniTaskVoid ConfirmSkipAsync(CancellationToken ct) {
        try {
            var popup = await _ui.ShowPopupAsync<ConfirmCancelPopup>(_popups.ConfirmCancelPopup, ct);
            bool skip = await popup.ShowAsync("건너뛰기", "스토리를 건너뛰시겠습니까?");
            if (skip && playerView != null) playerView.Skip();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private async UniTask LeaveAsync(AssetReference scene, CancellationToken ct) {
        if (_leaving) return;
        _leaving = true;

        if (scene == null || !scene.RuntimeKeyIsValid()) {
            Debug.LogError("[StorySceneManager] 이동할 씬이 없습니다(fallbackScene 미할당).");
            return;
        }

        try {
            await _scene.LoadAddressableAsync(scene.RuntimeKey, true, ct);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }
}
