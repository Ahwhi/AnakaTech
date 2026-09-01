using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 채팅 패널의 유저 대화 한 줄. 프로필(테두리 + 대표 캐릭터) + 닉네임 + 내용을 보여 준다.
///
/// 프로필 표시는 다른 화면(길드/친구/랭킹)과 같은 컴포넌트를 쓰므로 장착 코스튬까지 그대로 반영된다.
/// 발언자 정보는 서버가 대화에 실어 보내 주므로 줄마다 따로 조회하지 않는다.
/// </summary>
public sealed class ChatUserRowView : ChatRowView {
    [Tooltip("대표 캐릭터 초상. 장착 코스튬이 반영된 외형이 들어간다.")]
    [SerializeField] private CharacterIllustrationView portraitView;
    [Tooltip("프로필 테두리.")]
    [SerializeField] private ProfileFrameView frameView;
    [Tooltip("닉네임.")]
    [SerializeField] private TMP_Text nicknameText;
    [Tooltip("대화 내용.")]
    [SerializeField] private TMP_Text messageText;
    [Tooltip("발언자 프로필을 여는 버튼. 선택.")]
    [SerializeField] private Button profileButton;

    [Header("Nickname Color")]
    [Tooltip("전체 채널 대화의 닉네임 색.")]
    [SerializeField] private Color worldNameColor = Color.white;
    [Tooltip("길드 채널 대화의 닉네임 색. 어느 채널의 말인지 한눈에 갈리게 한다.")]
    [SerializeField] private Color guildNameColor = new Color(0.55f, 0.9f, 0.55f);
    [Tooltip("내가 친 말의 닉네임 색. 채널보다 우선한다(내 말을 흐름 속에서 먼저 찾게 하는 것이 목적이라, " +
             "어느 채널에서 말했든 같은 색으로 보이는 편이 낫다).")]
    [SerializeField] private Color myNameColor = new Color(1f, 0.85f, 0.35f);
    [Tooltip("귓속말 줄의 이름 색. 전체/길드와 섞여 흐르지 않고 한눈에 구분되도록 따로 둔다.")]
    [SerializeField] private Color whisperNameColor = new Color(1f, 0.6f, 0.95f);
    [Tooltip("선택. 귓속말 줄에서만 켜는 답장 버튼(누르면 그 상대가 귓속말 대상이 된다).")]
    [SerializeField] private Button replyButton;
    [Tooltip("선택. 이 발언을 신고하는 버튼. 내 발언에는 켜지 않는다.")]
    [SerializeField] private Button reportButton;

    // 내 말이면 내 색, 아니면 채널 색. 내 발언을 찾는 것이 먼저라 채널 구분보다 우선한다.
    private Color NameColorOf(ChatMessageDto message) {
        // 귓속말은 보낸/받은 어느 쪽이든 같은 색으로 묶는다(내 말인지보다 귓속말인지가 먼저 보여야 한다).
        if (message.IsWhisper) return whisperNameColor;
        if (message.IsMine) return myNameColor;
        return message.Channel == ChatChannel.Guild ? guildNameColor : worldNameColor;
    }

    protected override void Apply(BattleHudContext hud, ChatMessageDto message, Action<int> onProfile,
                                  Action<int, string> onWhisperReply, Action<ChatMessageDto> onReport) {
        if (nicknameText != null) {
            // 언제나 발언자 이름만 적는다. 보낸 말과 받은 말은 프리팹이 갈려 배치로 구분되므로
            // 말머리를 덧붙이면 같은 정보를 두 번 말하는 셈이 된다.
            nicknameText.text = message.Nickname;
            nicknameText.color = NameColorOf(message);
        }
        if (messageText != null) messageText.text = message.Text;

        // 다른 유저의 테두리/초상은 이 줄이 떠 있는 동안 바뀌지 않으므로 구독형이 아니라 1회 표시로 붙인다.
        if (frameView != null && hud != null) frameView.Bind(hud.ProfileFrames, message.ProfileFrameId);
        if (portraitView != null) {
            var appearance = hud != null && hud.Appearance != null
                ? hud.Appearance.Other(message.RepresentativeCharId, message.RepresentativeCostumeId)
                : default;
            if (appearance.Character != null) portraitView.Bind(appearance);
            else portraitView.Clear();
        }

        if (profileButton != null) {
            profileButton.onClick.RemoveAllListeners();
            int accountId = message.AccountId;
            profileButton.onClick.AddListener(() => onProfile?.Invoke(accountId));
        }

        // 답장 버튼은 귓속말 줄에만 켠다. 상대는 내가 보낸 줄이면 받는 사람, 받은 줄이면 보낸 사람이다
        // (어느 쪽이든 "이 대화의 상대"가 답장 대상이다).
        if (replyButton != null) {
            bool canReply = message.IsWhisper && onWhisperReply != null;
            replyButton.gameObject.SetActive(canReply);
            replyButton.onClick.RemoveAllListeners();
            if (canReply) {
                int partnerId = message.IsMine ? message.TargetAccountId : message.AccountId;
                string partnerName = message.WhisperPartnerNickname;
                replyButton.onClick.AddListener(() => onWhisperReply(partnerId, partnerName));
            }
        }

        // 신고는 남의 발언에만 켠다. 내 말을 신고하는 버튼은 의미가 없고, 보관되지 않은 줄(MessageId 0)은
        // 서버가 무엇을 신고하는지 알 수 없어 접수할 수 없다.
        if (reportButton != null) {
            bool canReport = onReport != null && !message.IsMine && message.MessageId > 0;
            reportButton.gameObject.SetActive(canReport);
            reportButton.onClick.RemoveAllListeners();
            if (canReport) {
                var reported = message;
                reportButton.onClick.AddListener(() => onReport(reported));
            }
        }
    }
}
