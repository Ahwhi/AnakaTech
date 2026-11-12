using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 친구 목록 한 줄(친구 탭 / 친구 신청 탭 / 차단 유저 탭 공용).
/// 표시는 프로필 테두리 + 대표 캐릭터 일러 + 닉네임 + 전투력뿐이고, 그 외 정보는 슬롯을 눌러 여는 프로필 팝업이 맡는다.
///
/// 오른쪽 조작부(수락/거절/차단해제)는 전부 선택이며 쓰는 탭이 필요한 것만 켠다.
/// 무엇을 할지는 콜백을 받은 쪽이 정하므로 이 컴포넌트는 표시와 입력 전달만 한다(GuildMemberSlot 과 같은 역할 분담).
/// </summary>
public sealed class FriendSlot : MonoBehaviour {
    [Header("Profile")]
    [Tooltip("대표 캐릭터 일러 슬롯. 원본이 선언한 관심점(UiFocus)으로 프레이밍되므로 raw Image 가 아니라 이 컴포넌트를 쓴다.")]
    [SerializeField] private CharacterIllustrationView portraitView;
    [Tooltip("프로필 테두리. 표시 규칙(스프라이트/숨김)이 한 곳에 모이도록 공용 위젯을 쓴다.")]
    [SerializeField] private ProfileFrameView frameView;
    [SerializeField] private TMP_Text nicknameText;
    [Tooltip("선택. 상대의 전투력. 서버가 마지막으로 보고받은 값이라 상대가 지금 접속 중이 아니어도 표시된다.")]
    [SerializeField] private TMP_Text combatPowerText;
    [Tooltip("선택. 슬롯 전체를 덮는 버튼. 누르면 그 유저의 프로필 팝업을 연다.")]
    [SerializeField] private Button profileButton;

    [Header("Presence (선택)")]
    [Tooltip("접속 중일 때 켜는 그룹.")]
    [SerializeField] private GameObject onlineGroup;
    [Tooltip("접속 중이 아닐 때 켜는 그룹.")]
    [SerializeField] private GameObject offlineGroup;

    [Header("Stamina Gift (선택)")]
    [Tooltip("오늘 이 친구가 나에게 기력을 보냈으면 켜는 그룹(받았든 안 받았든). 친구 탭에서만 켜진다. 자정에 초기화.")]
    [SerializeField] private GameObject staminaGroup;

    [Header("Request Tab (선택)")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    [Header("Blocked Tab (선택)")]
    [SerializeField] private Button unblockButton;

    [Header("Search (선택)")]
    [Tooltip("검색 결과에서 친구 신청을 보내는 버튼. 이미 친구이거나 신청 중인 상대에게는 꺼진다.")]
    [SerializeField] private Button requestButton;
    [Tooltip("신청할 수 없는 상대의 사유 표시(이미 친구 / 신청함 / 차단함). 신청 가능하면 꺼진다.")]
    [SerializeField] private TMP_Text relationText;

    private int _accountId;

    /// <summary>친구 탭. 조작부는 모두 꺼지고, 삭제/차단은 슬롯을 눌러 여는 프로필 팝업에서 한다.</summary>
    public void BindFriend(FriendUserDto user, BattleHudContext hud, Action<int> onProfile) {
        Apply(user, hud, onProfile);
        SetActive(acceptButton, false);
        SetActive(declineButton, false);
        SetActive(unblockButton, false);
        HideSearchParts();
        if (staminaGroup != null) staminaGroup.SetActive(user.StaminaSentToMe);
    }

    /// <summary>친구 신청 탭. 수락/거절 버튼만 연다.</summary>
    public void BindRequest(FriendUserDto user, BattleHudContext hud, Action<int> onProfile,
                            Action<int> onAccept, Action<int> onDecline) {
        Apply(user, hud, onProfile);
        SetActive(acceptButton, true);
        SetActive(declineButton, true);
        SetActive(unblockButton, false);
        BindButton(acceptButton, onAccept);
        BindButton(declineButton, onDecline);
        HideSearchParts();
    }

    /// <summary>차단 유저 탭. 차단 해제 버튼만 연다.</summary>
    public void BindBlocked(FriendUserDto user, BattleHudContext hud, Action<int> onProfile, Action<int> onUnblock) {
        Apply(user, hud, onProfile);
        SetActive(acceptButton, false);
        SetActive(declineButton, false);
        SetActive(unblockButton, true);
        BindButton(unblockButton, onUnblock);
        HideSearchParts();
    }

    // 검색 전용 표시는 다른 탭에서 반드시 꺼야 한다. 같은 프리팹을 네 곳이 돌려 쓰므로
    // 켠 채로 두면 친구 목록에 신청 버튼이 남는다.
    private void HideSearchParts() {
        SetActive(requestButton, false);
        if (relationText != null) relationText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 검색 결과 한 줄. 신청할 수 있으면 신청 버튼을, 아니면 그 이유를 보여 준다.
    /// 이미 친구인 상대도 결과에 남기므로(서버가 걸러 내지 않는다) 여기서 상태를 그대로 드러낸다.
    /// </summary>
    public void BindSearch(FriendSearchEntry entry, BattleHudContext hud, Action<int> onProfile, Action<int> onRequest) {
        Apply(entry.User, hud, onProfile);
        SetActive(acceptButton, false);
        SetActive(declineButton, false);
        SetActive(unblockButton, false);

        bool canRequest = entry.CanRequest;
        SetActive(requestButton, canRequest);
        if (canRequest) BindButton(requestButton, onRequest);

        if (relationText == null) return;
        string label = FriendSearchRules.RelationLabel(entry);
        relationText.gameObject.SetActive(!canRequest && !string.IsNullOrEmpty(label));
        relationText.text = label;
    }

    private void Apply(FriendUserDto user, BattleHudContext hud, Action<int> onProfile) {
        _accountId = user.AccountId;
        // 기력 선물 표시는 친구 탭(BindFriend)만 켠다. 같은 프리팹을 네 곳이 돌려 쓰므로 기본은 끈다.
        if (staminaGroup != null) staminaGroup.SetActive(false);

        if (nicknameText != null) nicknameText.text = user.Nickname;
        // 표기는 길드원 목록(GuildMemberSlot)과 같은 규칙을 따른다.
        if (combatPowerText != null) combatPowerText.text = user.CombatPower.ToString("N0");
        // 다른 유저의 테두리는 이 화면에 떠 있는 동안 바뀌지 않으므로 구독형이 아니라 1회 표시로 붙인다.
        if (frameView != null && hud != null) frameView.Bind(hud.ProfileFrames, user.ProfileFrameId);
        if (portraitView != null) {
            // 상대가 끼운 코스튬까지 반영한 외형을 넘겨야 관심점/프레이밍이 그 그림에 맞게 적용된다.
            var appearance = hud != null && hud.Appearance != null
                ? hud.Appearance.Other(user.RepresentativeCharId, user.RepresentativeCostumeId)
                : default;
            if (appearance.Character != null) portraitView.Bind(appearance);
            else portraitView.Clear();
        }

        BindButton(profileButton, onProfile);
        ApplyPresence(user.HasPresence, user.IsOnline);
    }

    // 접속 표시. 접속 여부를 모르는 목록(신청/차단 탭)에서는 양쪽 다 끈다.
    // "모름"을 오프라인으로 그리면 실제로 접속 중인 상대에게도 접속 안 함 표시가 붙는다.
    private void ApplyPresence(bool known, bool online) {
        if (onlineGroup != null) onlineGroup.SetActive(known && online);
        if (offlineGroup != null) offlineGroup.SetActive(known && !online);
    }

    private void BindButton(Button button, Action<int> callback) {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => callback?.Invoke(_accountId));
    }

    private static void SetActive(Button button, bool on) {
        if (button != null) button.gameObject.SetActive(on);
    }
}
