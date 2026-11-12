using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스토리 목록의 한 편. 대표 이미지 + 제목 + 상태를 보여 주고, 버튼 하나로 읽기와 수령을 겸한다.
///
/// 상태는 넷이며 서로 배타적이다. 카드가 스스로 판정하지 않고 서버가 내려 준 값을 그대로 쓴다.
///   잠김        진도가 모자라거나 직전 편을 안 읽었다. 무엇을 깨야 열리는지 안내하고 버튼을 잠근다.
///   읽기 가능   열렸는데 아직 안 읽었다(배지 대상).
///   보상 대기   읽었는데 보상을 안 받았다(배지 대상). 버튼이 수령으로 바뀐다.
///   완료        읽고 보상까지 받았다. 다시 보기만 가능하다.
///
/// 버튼을 하나만 두는 이유는 한 카드에서 할 수 있는 일이 언제나 하나뿐이기 때문이다.
/// 읽지 않았으면 읽는 것, 보상이 남았으면 받는 것, 다 끝났으면 다시 보는 것밖에 없다.
/// 두 버튼을 나란히 두면 그중 하나는 항상 비활성이라 자리만 차지한다.
/// </summary>
public sealed class StoryEntryCard : MonoBehaviour {
    /// <summary>버튼을 눌렀을 때 일어날 일. 상태에서 하나로 정해진다.</summary>
    private enum CardAction { None, Read, Claim, Reread }

    [Header("Content")]
    [Tooltip("대표 이미지(StoryIndex 의 썸네일).")]
    [SerializeField] private Image thumbnail;
    [Tooltip("편 제목(예: 도심).")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("편 번호(예: 1장).")]
    [SerializeField] private TMP_Text chapterText;
    [Tooltip("상태 문구. 잠김이면 해금 조건, 그 외에는 진행 상태를 쓴다.")]
    [SerializeField] private TMP_Text stateText;

    [Header("State Groups (선택)")]
    [Tooltip("잠겼을 때 켜는 오브젝트(자물쇠 등).")]
    [SerializeField] private GameObject lockedGroup;
    [Tooltip("읽고 보상까지 받았을 때 켜는 오브젝트(체크 등).")]
    [SerializeField] private GameObject completedGroup;

    [Header("Action Button")]
    [Tooltip("읽기 / 수령 / 다시 보기를 겸하는 버튼 하나. 잠긴 편에서는 잠긴다.")]
    [SerializeField] private Button actionButton;
    [Tooltip("버튼 문구. 상태에 따라 읽기 / 수령 / 다시 보기로 바뀐다.")]
    [SerializeField] private TMP_Text actionText;
    [Tooltip("버튼 안의 보상 표시 묶음(다이아 아이콘 + 수량). 수령 상태일 때만 켜진다. " +
             "내용은 프리팹에 그려 두면 되고, 이 컴포넌트는 켜고 끄기만 한다.")]
    [SerializeField] private GameObject rewardGroup;
    [Tooltip("선택. 읽을 수 있거나 받을 보상이 있을 때 켜지는 배지.")]
    [SerializeField] private BadgeView badge;

    [Header("Labels")]
    [SerializeField] private string readLabel = "읽기";
    [SerializeField] private string claimLabel = "수령";
    [SerializeField] private string rereadLabel = "다시 보기";

    [Header("Colors")]
    [SerializeField] private Color lockedColor = new(0.55f, 0.55f, 0.60f);
    [SerializeField] private Color readableColor = new(1f, 0.84f, 0.35f);
    [SerializeField] private Color claimableColor = new(0.40f, 0.95f, 0.70f);
    [SerializeField] private Color doneColor = new(0.70f, 0.70f, 0.75f);

    private Action<StoryChapterStateDto> _onRead;
    private Action<StoryChapterStateDto> _onClaim;
    private StoryChapterStateDto _state;
    private CardAction _action = CardAction.None;
    private bool _wired;

    /// <summary>이 카드가 들고 있는 편.</summary>
    public StoryChapterStateDto State => _state;

    public void Initialize(Action<StoryChapterStateDto> onRead, Action<StoryChapterStateDto> onClaim) {
        _onRead = onRead;
        _onClaim = onClaim;

        if (_wired) return;
        if (actionButton != null) actionButton.onClick.AddListener(OnActionClicked);
        _wired = true;
    }

    // 버튼이 하나라 무엇을 할지는 Bind 가 정해 둔 것에 따른다.
    private void OnActionClicked() {
        switch (_action) {
            case CardAction.Read:
            case CardAction.Reread: _onRead?.Invoke(_state); break;
            case CardAction.Claim: _onClaim?.Invoke(_state); break;
        }
    }

    /// <param name="clearedDungeonIndex">계정이 클리어한 최고 던전 인덱스. 잠긴 이유를 가르는 데만 쓴다.</param>
    /// <param name="previousCompleted">직전 편을 읽었는지. 잠긴 이유를 가르는 데만 쓴다.</param>
    public void Bind(StoryChapterStateDto state, StoryIndex index,
                     int clearedDungeonIndex, bool previousCompleted) {
        _state = state;
        if (state == null) return;

        int chapter = state.Chapter;

        // 수령을 먼저 본다. 읽었고 보상이 남은 편은 다시 보기보다 받는 것이 먼저다.
        _action = !state.Unlocked ? CardAction.None
                : state.CanClaim ? CardAction.Claim
                : state.Completed ? CardAction.Reread
                : CardAction.Read;

        if (thumbnail != null) {
            var sprite = index != null ? index.Thumbnail(chapter) : null;
            thumbnail.sprite = sprite;
            thumbnail.enabled = sprite != null;
            // 잠긴 편은 그림을 어둡게 해서 열린 편과 한눈에 갈린다.
            thumbnail.color = state.Unlocked ? Color.white : lockedColor;
        }
        if (titleText != null) titleText.text = index != null ? index.Title(chapter) : StoryRules.ChapterLabel(chapter);
        if (chapterText != null) chapterText.text = StoryRules.ChapterLabel(chapter);

        if (lockedGroup != null) lockedGroup.SetActive(!state.Unlocked);
        if (completedGroup != null) completedGroup.SetActive(state.Completed && state.RewardClaimed);

        if (actionButton != null) actionButton.interactable = _action != CardAction.None;
        if (actionText != null) actionText.text = _action switch {
            CardAction.Claim => claimLabel,
            CardAction.Reread => rereadLabel,
            _ => readLabel,
        };
        // 보상 표시는 받을 수 있을 때만. 잠긴 편이나 이미 받은 편에 다이아가 붙어 있으면 아직 받을 수 있는 것처럼 보인다.
        if (rewardGroup != null) rewardGroup.SetActive(_action == CardAction.Claim);
        if (badge != null) badge.SetOn(state.CanClaim || state.CanRead);

        if (stateText != null) {
            if (!state.Unlocked) {
                stateText.text = StoryRules.LockReasonText(chapter, clearedDungeonIndex, previousCompleted);
                stateText.color = lockedColor;
            } else if (state.CanClaim) {
                stateText.text = "보상 수령 가능";
                stateText.color = claimableColor;
            } else if (!state.Completed) {
                stateText.text = "읽을 수 있습니다";
                stateText.color = readableColor;
            } else {
                stateText.text = "완료";
                stateText.color = doneColor;
            }
        }
    }

    /// <summary>처리 중에는 눌리지 않게 잠근다(연타로 두 번 수령되지 않게). 잠긴 편은 계속 잠겨 있다.</summary>
    public void SetInteractable(bool on) {
        if (actionButton != null) actionButton.interactable = on && _action != CardAction.None;
    }
}
