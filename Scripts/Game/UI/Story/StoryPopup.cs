using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 스토리 목록(메인 HUD 의 스토리 버튼). 19편을 카드로 늘어놓고, 읽기와 보상 수령을 처리한다.
///
/// 해금 판정은 서버가 한다. 진도(클리어한 최고 스테이지)와 직전 편 완독을 서버가 보고 내려 주므로
/// 이 화면은 받은 상태를 그리기만 한다. 클라가 같은 판정을 한 번 더 하면 두 규칙이 어긋날 때
/// "열려 보이는데 눌리면 거부되는" 카드가 생긴다.
///
/// 읽기는 스토리 씬으로 넘어간다. 돌아올 곳을 예약해 두므로(StoryPlaybackRequest) 재생이 끝나면
/// 메인 씬으로 복귀하며, 완독 보고도 스토리 씬이 한다(StorySceneManager).
/// </summary>
public sealed class StoryPopup : UIView, IHudPopup {
    [Header("List")]
    [Tooltip("카드가 생성될 부모(Horizontal/Vertical/Grid Layout 아무거나). 스크롤뷰의 Content 로 둔다.")]
    [SerializeField] private Transform content;
    [Tooltip("편 카드 프리팹(StoryEntryCard).")]
    [SerializeField] private StoryEntryCard cardPrefab;
    [Tooltip("목록을 채우는 동안 켜는 로딩 그룹(선택).")]
    [SerializeField] private GameObject loadingGroup;
    [Tooltip("목록 스크롤뷰(선택). 열 때 지금 할 일이 있는 편으로 자동 스크롤한다. 가로/세로 어느 쪽이든 맞춰 준다.")]
    [SerializeField] private ScrollRect scroll;

    [Header("Header")]
    [Tooltip("진행 현황(예: 7 / 19).")]
    [SerializeField] private TMP_Text progressText;
    [Tooltip("상단 재화 패널(선택). 보상으로 받은 다이아가 바로 보이게 한다.")]
    [SerializeField] private CurrencyPanel currencyPanel;
    [SerializeField] private Button closeButton;

    [Header("Data")]
    [Tooltip("편 제목/대표 이미지 카탈로그. 대사 내용은 스토리 씬의 StoryDatabase 가 갖는다.")]
    [SerializeField] private StoryIndex storyIndex;

    private MainHudContext _ctx;
    private CancellationToken _ct;
    private readonly List<StoryEntryCard> _cards = new();
    private bool _busy;
    private bool _wired;

    public void Initialize(MainHudContext ctx) {
        _ctx = ctx;
        _ct = this.GetCancellationTokenOnDestroy();

        if (currencyPanel != null) currencyPanel.Initialize(ctx);
        if (!_wired) {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { _ctx.Sound.PlaySfx(SfxKey.ButtonClick); CloseAsync().Forget(); });
            _wired = true;
        }

        LoadAsync().Forget();
    }

    private async UniTaskVoid LoadAsync() {
        if (loadingGroup != null) loadingGroup.SetActive(true);
        try {
            // 팝업을 열 때마다 받아 온다. 스테이지를 깨고 바로 들어오는 흐름이 흔해서,
            // 캐시를 그대로 쓰면 방금 열린 편이 잠긴 것으로 보인다.
            await _ctx.Story.RefreshAsync(_ct);
            Rebuild();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("스토리 정보를 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }
    }

    // 카드 수는 서버가 내려 준 편 수가 정한다. 편이 늘어도 화면은 손댈 것이 없다.
    private void Rebuild() {
        var chapters = _ctx.Story.Chapters;
        if (content == null || cardPrefab == null) return;

        while (_cards.Count < chapters.Count) {
            var card = Instantiate(cardPrefab, content);
            card.gameObject.SetActive(true);
            card.Initialize(OnRead, OnClaim);
            _cards.Add(card);
        }
        for (int i = 0; i < _cards.Count; i++) _cards[i].gameObject.SetActive(i < chapters.Count);

        int cleared = _ctx.Store.HighestClearedDungeonIndex.Value;
        int completed = 0;
        for (int i = 0; i < chapters.Count; i++) {
            // 직전 편의 완독 여부는 카드가 "잠긴 이유"를 가르는 데만 쓴다(1편은 직전이 없다).
            bool previousCompleted = i > 0 && chapters[i - 1].Completed;
            _cards[i].Bind(chapters[i], storyIndex, cleared, previousCompleted);
            if (chapters[i].Completed) completed++;
        }

        if (progressText != null) progressText.text = $"진행도: {completed}/{chapters.Count}";

        ScrollToFocusAsync(chapters).Forget();
    }

    /// <summary>
    /// 지금 할 일이 있는 편이 보이도록 맞춘다. 19편이라 스크롤을 직접 내려야 하면 무엇이 열렸는지 못 보고 닫는다.
    ///
    /// 우선순위는 수령 > 읽기 > 마지막 완료 편이다. 받을 보상이 있으면 그것이 가장 급하고,
    /// 없으면 새로 읽을 편, 그것도 없으면 진행한 끝자리를 보여 준다.
    /// 칸이 실제로 배치된 뒤라야 위치를 계산할 수 있어 한 프레임 뒤에 적용한다(업적 보상 화면과 같은 방식).
    /// </summary>
    private async UniTaskVoid ScrollToFocusAsync(IReadOnlyList<StoryChapterStateDto> chapters) {
        if (scroll == null || chapters.Count <= 1) return;

        int focus = -1;
        for (int i = 0; i < chapters.Count; i++)
            if (chapters[i].CanClaim) { focus = i; break; }
        if (focus < 0)
            for (int i = 0; i < chapters.Count; i++)
                if (chapters[i].CanRead) { focus = i; break; }
        if (focus < 0)
            for (int i = chapters.Count - 1; i >= 0; i--)
                if (chapters[i].Completed) { focus = i; break; }
        if (focus < 0) return;

        try {
            await UniTask.NextFrame(_ct);
        } catch (OperationCanceledException) {
            return;
        }
        if (scroll == null) return;
        Canvas.ForceUpdateCanvases();

        float t = Mathf.Clamp01((float)focus / (chapters.Count - 1));

        // 목록을 세로로 쌓을지 가로로 늘어놓을지는 프리팹이 정한다. 축을 코드에 박아 두면
        // 레이아웃을 바꾼 순간 스크롤이 조용히 엉뚱한 축으로 가서 아무 일도 하지 않는 것처럼 보인다.
        // 그래서 ScrollRect 가 어느 축을 쓰는지 보고 맞춘다.
        if (scroll.horizontal) scroll.horizontalNormalizedPosition = t;          // 0 = 맨 왼쪽
        if (scroll.vertical) scroll.verticalNormalizedPosition = 1f - t;         // 1 = 맨 위라 뒤집는다
    }

    // ── 읽기: 스토리 씬으로 넘어간다 ──
    private void OnRead(StoryChapterStateDto state) {
        if (_busy || state == null || !state.Unlocked) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);

        if (_ctx.Scenes == null || _ctx.Scenes.StoryScene == null || !_ctx.Scenes.StoryScene.RuntimeKeyIsValid()) {
            Debug.LogError("[StoryPopup] SceneCatalog 에 스토리 씬이 할당되지 않았습니다.");
            _ctx.Ui.ShowToast("스토리를 열 수 없습니다.");
            return;
        }
        if (_ctx.Scenes.MainScene == null || !_ctx.Scenes.MainScene.RuntimeKeyIsValid()) {
            Debug.LogError("[StoryPopup] SceneCatalog 에 메인 씬이 할당되지 않았습니다(복귀할 곳이 없습니다).");
            _ctx.Ui.ShowToast("스토리를 열 수 없습니다.");
            return;
        }

        // CloseAllPopups 는 이 팝업 자신도 파괴하므로, 그 뒤에 쓸 것을 먼저 지역 변수로 확보한다.
        var ui = _ctx.Ui;
        var sceneService = _ctx.SceneService;
        var storyScene = _ctx.Scenes.StoryScene.RuntimeKey;

        // 재생이 끝나면 메인 씬으로 돌아온다. 완독 보고는 스토리 씬이 한다.
        StoryPlaybackRequest.Request(state.StoryKey, _ctx.Scenes.MainScene);

        // UIRoot 는 씬 전환에도 살아남는다. 닫지 않으면 스토리 화면 위에 이 팝업이 그대로 떠 있다.
        ui.CloseAllPopups();
        sceneService.LoadAddressableAsync(storyScene).Forget();
    }

    // ── 보상 수령: 버튼으로만 받는다 ──
    private void OnClaim(StoryChapterStateDto state) {
        if (_busy || state == null || !state.CanClaim) return;
        _ctx.Sound.PlaySfx(SfxKey.ButtonClick);
        ClaimAsync(state).Forget();
    }

    private async UniTaskVoid ClaimAsync(StoryChapterStateDto state) {
        _busy = true;
        SetCardsInteractable(false);
        try {
            var res = await _ctx.Story.ClaimRewardAsync(state.StoryKey, _ct);
            if (!res.IsSuccess) { _ctx.Ui.ShowToast(FailMessage(res.FailReason)); return; }

            // 다이아는 서버가 확정한 잔액을 그대로 쓴다(화면이 더하지 않는다).
            _ctx.Store.Diamond.Value = res.NewDiamond;
            _ctx.Sound.PlaySfx(SfxKey.Claim);
            _ctx.Ui.ShowToast($"다이아 {res.Diamond:N0}을 받았습니다.");

            if (_ctx.Badges != null) _ctx.Badges.RefreshAsync(_ct).Forget();
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("보상을 받지 못했습니다.");
        } finally {
            _busy = false;
            Rebuild();   // 서비스 캐시가 이미 갱신돼 있어 다시 그리면 수령 버튼이 사라진다.
            SetCardsInteractable(true);
        }
    }

    private void SetCardsInteractable(bool on) {
        for (int i = 0; i < _cards.Count; i++)
            if (_cards[i] != null) _cards[i].SetInteractable(on);
    }

    private static string FailMessage(int fail) => fail switch {
        2 => "아직 읽지 않은 스토리입니다.",
        3 => "이미 받은 보상입니다.",
        4 => "보상이 없는 스토리입니다.",
        _ => "보상을 받지 못했습니다."
    };
}
