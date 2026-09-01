using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

/// <summary>
/// 다른 유저의 프로필 팝업(길드원 목록 / 친구 목록 등에서 연다). 본인용 ProfilePopup 과 프리팹/응답을 나눠 두었으며,
/// 서버도 공개해도 되는 값만 담아 보낸다(보유 재화/결제/변경권 수량 등은 애초에 오지 않는다).
/// 따라서 이 팝업에는 편집 기능이 없고 표시만 한다.
///
/// 예외가 관계 조작(친구 / 차단)이다. 이건 "그 유저에 대해" 하는 일이라 유저를 보고 있는 이 화면이 제자리이며,
/// 친구 버튼 하나가 관계에 따라 신청/취소/수락/삭제를 겸한다(문구와 동작은 FriendRules 가 같은 값에서 뽑는다).
/// 내 프로필을 열었을 때(relation = Self)는 두 버튼 모두 감춘다.
/// </summary>
public sealed class UserProfilePopup : UIView {
    [Header("Profile")]
    [Tooltip("대표 캐릭터 일러 슬롯(관심점 프레이밍 적용). 본인 프로필 팝업과 같은 컴포넌트를 쓴다.")]
    [SerializeField] private CharacterIllustrationView portraitView;
    [Tooltip("프로필 테두리. 본인 프로필 팝업과 같은 공용 위젯을 쓴다(타인은 값이 바뀌지 않으므로 1회 표시).")]
    [SerializeField] private ProfileFrameView frameView;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text combatPowerText;
    [Tooltip("도달한 최고 스테이지.")]
    [SerializeField] private TMP_Text stageText;
    [Tooltip("계정 생성일.")]
    [SerializeField] private TMP_Text createdText;
    [Tooltip("업적 총점.")]
    [SerializeField] private TMP_Text achievementScoreText;
    [Tooltip("선택. 유저 랭킹(전투력) 순위(예: '3위'). 순위 구간별 색은 RankColorCatalog 가 정한다.")]
    [SerializeField] private RankText combatPowerRankText;
    [Tooltip("선택. 업적 랭킹 순위(예: '5위'). 순위 구간별 색은 RankColorCatalog 가 정한다.")]
    [SerializeField] private RankText achievementRankText;

    [Header("Deck")]
    [Tooltip("편성 미리보기 5칸(일러 + 별레벨 + 레벨). 편성은 1~5명이라 남는 칸은 자동으로 꺼진다. 첫 칸이 대표다.")]
    [SerializeField] private DeckMemberPreview[] deckSlots;

    [Header("Guild")]
    [Tooltip("길드 정보 묶음. 길드가 없어도 길드명 자리에 안내를 띄우므로 끄지 않는다.")]
    [SerializeField] private GameObject guildGroup;
    [Tooltip("길드 마크. 길드가 없으면 숨긴다.")]
    [SerializeField] private Image guildMarkImage;
    [Tooltip("길드명. 길드가 없으면 이 자리에 안내 문구를 대신 넣는다(별도 안내 오브젝트를 두지 않는다).")]
    [SerializeField] private TMP_Text guildNameText;
    [Tooltip("길드 내 역할. 길드가 없으면 비운다.")]
    [SerializeField] private TMP_Text guildRoleText;

    [Header("Relation (친구 / 차단)")]
    [Tooltip("친구 버튼. 관계에 따라 친구 신청/신청 취소/친구 수락/친구 삭제로 바뀐다.")]
    [SerializeField] private Button friendButton;
    [Tooltip("친구 버튼의 문구. 비우면 버튼 하위의 TMP_Text 를 자동으로 찾는다.")]
    [SerializeField] private TMP_Text friendButtonText;
    [Tooltip("차단 버튼. 이미 차단한 상대면 차단 해제로 바뀐다.")]
    [SerializeField] private Button blockButton;
    [Tooltip("차단 버튼의 문구. 비우면 버튼 하위의 TMP_Text 를 자동으로 찾는다.")]
    [SerializeField] private TMP_Text blockButtonText;
    [Tooltip("귓속말 버튼. 본인 프로필이거나 상대가 귓속말을 받지 않으면 숨긴다. 선택.")]
    [SerializeField] private Button whisperButton;

    [Header("Loading")]
    [SerializeField] private GameObject loadingGroup;

    [SerializeField] private Button closeButton;

    /// <summary>길드가 없는 유저의 길드명 자리에 넣는 문구.</summary>
    private const string NoGuildText = "길드 없음";

    private UniTaskCompletionSource _closed;
    private MainHudContext _ctx;
    private int _accountId;
    private string _nickname = "";
    private FriendRelation _relation = FriendRelation.Self;
    private bool _blocked;
    private bool _busy;
    /// <summary>이 팝업에서 관계를 바꿨는지. 연 쪽(친구 팝업 등)이 목록을 다시 받을지 판단하는 근거다.</summary>
    private bool _relationChanged;

    protected override void Awake() {
        base.Awake();
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseAsync().Forget());
        if (friendButton != null) friendButton.onClick.AddListener(() => OnFriendButton());
        if (whisperButton != null) whisperButton.onClick.AddListener(() => OnWhisperButton());
        if (blockButton != null) blockButton.onClick.AddListener(() => OnBlockButton());
    }

    /// <summary>
    /// 조회 후 표시하고, 닫힐 때까지 기다린다.
    /// </summary>
    /// <returns>이 팝업에서 친구/차단 관계를 바꿨으면 true. 연 화면이 목록을 다시 받을지 판단한다.</returns>
    public async UniTask<bool> ShowAsync(MainHudContext ctx, int accountId, GuildMarkCatalog marks) {
        _ctx = ctx;
        _accountId = accountId;
        _relationChanged = false;
        Clear();
        if (loadingGroup != null) loadingGroup.SetActive(true);

        _closed = new UniTaskCompletionSource();
        await base.ShowAsync();

        try {
            var dto = await ctx.Guild.RequestUserProfileAsync(accountId, this.GetCancellationTokenOnDestroy());
            Apply(dto, ctx, marks);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            ctx.Ui.ShowToast("프로필을 불러오지 못했습니다.");
        } finally {
            if (loadingGroup != null) loadingGroup.SetActive(false);
        }

        await _closed.Task;
        return _relationChanged;
    }

    private void Apply(UserProfileDto dto, MainHudContext ctx, GuildMarkCatalog marks) {
        if (dto == null) return;

        _nickname = dto.Nickname;
        if (nicknameText != null) nicknameText.text = dto.Nickname;
        if (combatPowerText != null) combatPowerText.text = dto.CombatPower.ToString("N0");
        if (stageText != null) stageText.text = dto.HighestClearedDungeonIndex > 0
            ? $"{dto.HighestClearedDungeonIndex} 스테이지" : "-";
        if (createdText != null) createdText.text = dto.CreatedUnix > 0
            ? DateTimeOffset.FromUnixTimeSeconds(dto.CreatedUnix).ToLocalTime().ToString("yyyy. MM. dd") : "-";
        if (achievementScoreText != null)
            achievementScoreText.text = AchievementRules.ScoreText(dto.AchievementScore);
        if (combatPowerRankText != null) combatPowerRankText.SetRank(dto.CombatPowerRank);
        if (achievementRankText != null) achievementRankText.SetRank(dto.AchievementRank);

        if (frameView != null) frameView.Bind(ctx.ProfileFrames, dto.ProfileFrameId);
        if (portraitView != null) {
            var appearance = ctx.Appearance.Other(dto.RepresentativeCharId, dto.RepresentativeCostumeId);
            if (appearance.Character != null) portraitView.Bind(appearance);
            else portraitView.Clear();
        }

        ApplyDeck(dto.DeckMembers, ctx.Appearance);
        ApplyGuild(dto, marks);
        ApplyRelation(dto.Relation, dto.Blocked);

        // 귓속말 버튼은 보낼 수 있는 상대에게만 낸다. 판정은 서버가 실제 발송과 같은 규칙으로 미리 해 준다
        // (눌러 보고 "받지 않는 유저입니다" 를 듣는 것보다, 애초에 버튼이 없는 편이 낫다).
        if (whisperButton != null) whisperButton.gameObject.SetActive(dto.CanWhisper);
    }

    // 편성은 1~5명이라 칸 수와 인원이 다르다. 채운 만큼만 켜고 나머지는 끈다(빈 칸이 남아 어색해지지 않게).
    // 코스튬은 편성원마다 다를 수 있으므로 서버가 준 캐릭터별 코스튬 id 로 각각 해석한다.
    private void ApplyDeck(List<UserProfileDeckMemberDto> members, CharacterAppearanceService appearance) {
        if (deckSlots == null) return;
        for (int i = 0; i < deckSlots.Length; i++) {
            var slot = deckSlots[i];
            if (slot == null) continue;

            if (members != null && i < members.Count)
                slot.Bind(appearance.Other(members[i].CharDataId, members[i].CostumeId),
                          members[i].StarLevel, members[i].Level);
            else slot.Clear();
        }
    }

    // 길드가 없어도 묶음을 통째로 끄지 않고 길드명 자리에 안내를 넣는다. 마크/역할처럼 채울 값이 없는 것만 비운다.
    private void ApplyGuild(UserProfileDto dto, GuildMarkCatalog marks) {
        bool hasGuild = dto.HasGuild;
        if (guildGroup != null) guildGroup.SetActive(true);
        if (guildNameText != null) guildNameText.text = hasGuild ? dto.GuildName : NoGuildText;
        if (guildRoleText != null) guildRoleText.text = hasGuild ? GuildRules.RoleName(dto.GuildRole) : "";
        if (guildMarkImage != null) {
            guildMarkImage.sprite = hasGuild && marks != null ? marks.Icon(dto.GuildMarkId) : null;
            guildMarkImage.enabled = guildMarkImage.sprite != null;
        }
    }

    // 관계는 서버가 요청자 기준으로 계산해 보내 준다. 클라는 그 값으로 버튼 문구만 정한다.
    private void ApplyRelation(FriendRelation relation, bool blocked) {
        _relation = relation;
        _blocked = blocked;

        // 내 프로필에는 관계 조작이 없다.
        bool isOther = relation != FriendRelation.Self;
        // 차단해 둔 상대에게 친구 신청을 걸 수는 없지만, 이미 친구라면 삭제는 할 수 있어야 한다.
        bool showFriend = isOther && (!blocked || relation == FriendRelation.Friend);

        if (friendButton != null) {
            friendButton.gameObject.SetActive(showFriend);
            friendButton.interactable = !_busy;
        }
        if (blockButton != null) {
            blockButton.gameObject.SetActive(isOther);
            blockButton.interactable = !_busy;
        }

        SetButtonLabel(friendButtonText, friendButton, FriendRules.FriendButtonText(relation));
        SetButtonLabel(blockButtonText, blockButton, FriendRules.BlockButtonText(blocked));
    }

    // 귓속말 버튼. 이 팝업을 닫고 채팅창을 귓속말 탭 + 이 상대로 열어 준다.
    // 프로필 위에 채팅창을 겹쳐 띄우면 뒤에 남은 프로필이 대화를 가려 쓸모가 없다.
    private void OnWhisperButton() {
        Click();
        OpenWhisperAsync().Forget();
    }

    private async UniTaskVoid OpenWhisperAsync() {
        var ctx = _ctx;
        int accountId = _accountId;
        string nickname = _nickname;
        await CloseAsync();

        try {
            // 이 팝업은 방금 닫혀 파괴되는 중이라 자기 토큰을 쓸 수 없다(열자마자 취소된다).
            // 채팅창이 이미 떠 있으면(그 창에서 이 프로필을 연 경우) 새로 만들지 않고 그 창이 이 상대로 옮겨 간다.
            await ChatPopup.OpenAsync(ctx, ctx.Popups.ChatPopup, accountId, nickname);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void OnFriendButton() {
        Click();
        RunAsync(FriendRules.FriendAction(_relation)).Forget();
    }

    private void OnBlockButton() {
        Click();
        RunAsync(_blocked ? FriendActionType.Unblock : FriendActionType.Block).Forget();
    }

    /// <summary>
    /// 관계 조작 공통 실행부. 되돌리기 번거로운 조작(친구 삭제 / 차단)만 확인을 받고,
    /// 성공하면 서버가 함께 보내 준 새 관계로 버튼을 바로 갱신한다(재조회 없음).
    /// </summary>
    private async UniTaskVoid RunAsync(FriendActionType action) {
        if (_busy || _ctx == null || _accountId <= 0) return;

        if (FriendRules.NeedsConfirm(action)) {
            bool ok = await ConfirmAsync(FriendRules.ConfirmTitle(action),
                                         FriendRules.ConfirmMessage(action, _nickname));
            if (!ok) return;
        }

        _busy = true;
        SetInteractable(false);
        try {
            var res = await _ctx.Friend.ActAsync(action, _accountId, this.GetCancellationTokenOnDestroy());
            if (!res.IsSuccess) {
                _ctx.Ui.ShowToast(FriendRules.FailMessage(res.FailReason));
            } else {
                _relationChanged = true;
                _ctx.Ui.ShowToast(SuccessMessage(action));
            }
            // 실패했더라도 서버가 보고 있는 현재 관계로 맞춘다(버튼이 옛 상태로 남지 않도록).
            ApplyRelation(res.Relation, res.Blocked);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Debug.LogException(e);
            _ctx.Ui.ShowToast("요청을 처리하지 못했습니다.");
        } finally {
            _busy = false;
            SetInteractable(true);
        }
    }

    private static string SuccessMessage(FriendActionType action) => action switch {
        FriendActionType.Request => "친구 신청을 보냈습니다.",
        FriendActionType.Accept => "친구가 되었습니다.",
        FriendActionType.CancelRequest => "친구 신청을 취소했습니다.",
        FriendActionType.Remove => "친구를 삭제했습니다.",
        FriendActionType.Block => "차단했습니다.",
        FriendActionType.Unblock => "차단을 해제했습니다.",
        _ => "처리했습니다."
    };

    private async UniTask<bool> ConfirmAsync(string title, string message) {
        var reference = _ctx.Popups.ConfirmCancelPopup;
        if (reference == null || !reference.RuntimeKeyIsValid()) return true;
        var popup = await _ctx.Ui.ShowPopupAsync<ConfirmCancelPopup>(reference, this.GetCancellationTokenOnDestroy());
        return await popup.ShowAsync(title, message);
    }

    private void SetInteractable(bool on) {
        if (friendButton != null) friendButton.interactable = on;
        if (blockButton != null) blockButton.interactable = on;
    }

    // 문구 필드를 비워 둬도 동작하도록 버튼 하위의 텍스트를 찾아 쓴다(프리팹마다 구조가 달라도 인스펙터 할당이 하나 준다).
    private static void SetButtonLabel(TMP_Text label, Button button, string text) {
        if (label == null && button != null) label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = text;
    }

    private void Clear() {
        if (nicknameText != null) nicknameText.text = "";
        if (combatPowerText != null) combatPowerText.text = "";
        if (achievementScoreText != null) achievementScoreText.text = "";
        if (combatPowerRankText != null) combatPowerRankText.SetPlaceholder("");
        if (achievementRankText != null) achievementRankText.SetPlaceholder("");
        ApplyDeck(null, null);   // 앞서 연 유저의 편성이 잠깐 남아 보이지 않게 비운다
        if (guildNameText != null) guildNameText.text = "";
        if (guildRoleText != null) guildRoleText.text = "";
        if (guildMarkImage != null) guildMarkImage.enabled = false;
        // 관계를 모르는 동안(조회 전)에는 조작 버튼을 감춘다. Self 가 곧 "버튼 없음"이다.
        ApplyRelation(FriendRelation.Self, false);
    }

    private void Click() => _ctx?.Sound.PlaySfx(SfxKey.ButtonClick);

    protected override void OnClosed() => _closed?.TrySetResult();
}
