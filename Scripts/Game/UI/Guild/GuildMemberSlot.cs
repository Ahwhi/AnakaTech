using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 길드원 한 줄(길드원 탭 / 관리 탭 / 가입 신청 목록 공용).
/// 프로필 테두리 + 대표 캐릭터 일러 + 이름 + 역할 + 전투력을 보여준다.
///
/// 오른쪽 조작부(역할 드롭다운 / 추방 / 승인 / 거절)는 전부 선택이며, 쓰는 화면이 필요한 것만 켠다.
/// 무엇을 할지는 콜백을 받은 쪽이 정하므로 이 컴포넌트는 표시와 입력 전달만 한다.
/// </summary>
public sealed class GuildMemberSlot : MonoBehaviour {
    [Header("Profile")]
    [Tooltip("대표 캐릭터 일러 슬롯. 원본이 선언한 관심점(UiFocus)으로 프레이밍되므로 raw Image 가 아니라 이 컴포넌트를 쓴다.")]
    [SerializeField] private CharacterIllustrationView portraitView;
    [Tooltip("프로필 테두리. 표시 규칙(스프라이트/숨김)이 한 곳에 모이도록 공용 위젯을 쓴다.")]
    [SerializeField] private ProfileFrameView frameView;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text combatPowerText;
    [Tooltip("선택. 가입일(yyyy. MM. dd).")]
    [SerializeField] private TMP_Text joinedText;
    [Tooltip("선택. 이 길드에서 쌓은 공헌도. 가입 신청 목록에서는 아직 길드원이 아니므로 꺼진다.")]
    [SerializeField] private TMP_Text contributionText;
    [Tooltip("선택. 최근 접속(오늘 / n일 전). 기록이 없으면 꺼진다.")]
    [SerializeField] private TMP_Text lastLoginText;
    [Tooltip("선택. 슬롯 전체를 덮는 버튼. 누르면 그 유저의 프로필 팝업을 연다.")]
    [SerializeField] private Button profileButton;

    [Header("Presence (선택)")]
    [Tooltip("접속 중일 때 켜는 그룹.")]
    [SerializeField] private GameObject onlineGroup;
    [Tooltip("접속 중이 아닐 때 켜는 그룹.")]
    [SerializeField] private GameObject offlineGroup;

    [Header("Manage (관리 탭 전용, 선택)")]
    [Tooltip("역할 변경 드롭다운. 항목은 코드에서 채운다.")]
    [SerializeField] private TMP_Dropdown roleDropdown;
    [SerializeField] private Button kickButton;

    [Header("Application (가입 신청 탭 전용, 선택)")]
    [SerializeField] private Button approveButton;
    [SerializeField] private Button rejectButton;

    [Header("Pick (선택 목록 전용, 선택)")]
    [Tooltip("이 줄을 고르는 버튼(길드장 위임 대상 선택 등). 고르는 화면에서만 켜진다.")]
    [SerializeField] private Button pickButton;

    // 드롭다운 항목 순서 = 이 배열의 순서. 길드장은 위임으로만 넘기므로 여기 없다.
    private static readonly GuildRole[] AssignableRoles = { GuildRole.Member, GuildRole.Officer, GuildRole.ViceMaster };

    private int _accountId;
    // 드롭다운의 현재 선택 위치. 같은 값을 다시 고른 경우를 걸러 불필요한 요청을 막는다.
    private int _roleDropdownIndex;

    /// <summary>길드원 표시(길드원 탭). 관리 조작부는 모두 꺼진다.</summary>
    public void Bind(GuildMemberDto member, BattleHudContext hud, Action<int> onProfile) {
        _accountId = member.AccountId;
        ApplyProfile(member.Nickname, member.ProfileFrameId, member.RepresentativeCharId,
                     member.RepresentativeCostumeId, member.CombatPower, hud);
        if (roleText != null) roleText.text = GuildRules.RoleName(member.Role);
        ApplyMembership(member.JoinedUnix, member.Contribution, member.LastLoginUnix);
        ApplyPresence(true, member.IsOnline);

        SetActive(roleDropdown != null ? roleDropdown.gameObject : null, false);
        SetActive(kickButton, false);
        SetActive(approveButton, false);
        SetActive(rejectButton, false);
        SetActive(pickButton, false);
        BindProfileButton(onProfile);
    }

    /// <summary>
    /// 길드원 관리 표시. 내 역할보다 낮은 상대에게만 조작부를 연다(서버도 같은 규칙으로 막지만,
    /// 누를 수 없는 버튼을 보여 주지 않는 편이 낫다).
    /// </summary>
    public void BindManage(GuildMemberDto member, BattleHudContext hud, GuildRole myRole,
                           Action<int> onProfile, Action<int, GuildRole> onRoleChange, Action<int> onKick) {
        Bind(member, hud, onProfile);

        bool canManage = myRole >= GuildRole.ViceMaster && member.Role < myRole;
        SetActive(roleDropdown != null ? roleDropdown.gameObject : null, canManage);
        SetActive(kickButton, canManage);
        if (!canManage) return;

        if (roleDropdown != null) {
            var options = new System.Collections.Generic.List<string>(AssignableRoles.Length);
            foreach (var r in AssignableRoles) options.Add(GuildRules.RoleName(r));
            _roleDropdownIndex = Array.IndexOf(AssignableRoles, member.Role);
            if (_roleDropdownIndex < 0) _roleDropdownIndex = 0;

            roleDropdown.ClearOptions();
            roleDropdown.AddOptions(options);
            roleDropdown.SetValueWithoutNotify(_roleDropdownIndex);
            roleDropdown.RefreshShownValue();
            roleDropdown.onValueChanged.RemoveAllListeners();
            roleDropdown.onValueChanged.AddListener(i => {
                if (i == _roleDropdownIndex) return;   // 같은 값 재선택은 무시
                onRoleChange?.Invoke(_accountId, AssignableRoles[Mathf.Clamp(i, 0, AssignableRoles.Length - 1)]);
            });
        }

        if (kickButton != null) {
            kickButton.onClick.RemoveAllListeners();
            kickButton.onClick.AddListener(() => onKick?.Invoke(_accountId));
        }
    }

    /// <summary>
    /// 고르기 표시(길드장 위임 대상 선택 등). 관리 조작부는 모두 끄고 고르기 버튼만 연다.
    /// 무엇을 고르는지는 콜백을 받는 쪽이 정하므로 이 컴포넌트는 "눌렸다" 만 알린다.
    /// </summary>
    public void BindPick(GuildMemberDto member, BattleHudContext hud, Action<int> onPick) {
        Bind(member, hud, onProfile: null);

        SetActive(pickButton, true);
        if (pickButton != null) {
            pickButton.onClick.RemoveAllListeners();
            pickButton.onClick.AddListener(() => onPick?.Invoke(_accountId));
        }
    }

    /// <summary>가입 신청 표시. 승인/거절 버튼만 연다.</summary>
    public void BindApplication(GuildApplicationDto app, BattleHudContext hud,
                                Action<int> onProfile, Action<int> onApprove, Action<int> onReject) {
        _accountId = app.AccountId;
        ApplyProfile(app.Nickname, app.ProfileFrameId, app.RepresentativeCharId,
                     app.RepresentativeCostumeId, app.CombatPower, hud);
        if (roleText != null) roleText.text = "가입 신청";
        ApplyMembership(0, 0, 0);   // 아직 길드원이 아니라 가입일/공헌도가 없다
        ApplyPresence(false, false);

        SetActive(roleDropdown != null ? roleDropdown.gameObject : null, false);
        SetActive(kickButton, false);
        SetActive(approveButton, true);
        SetActive(rejectButton, true);
        BindProfileButton(onProfile);

        if (approveButton != null) {
            approveButton.onClick.RemoveAllListeners();
            approveButton.onClick.AddListener(() => onApprove?.Invoke(_accountId));
        }
        if (rejectButton != null) {
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(() => onReject?.Invoke(_accountId));
        }
    }

    // 접속 표시. 접속 여부를 모르는 줄(가입 신청)에서는 양쪽 다 끈다.
    // "모름"을 오프라인으로 그리면 실제로 접속 중인 상대에게도 접속 안 함 표시가 붙는다.
    private void ApplyPresence(bool known, bool online) {
        if (onlineGroup != null) onlineGroup.SetActive(known && online);
        if (offlineGroup != null) offlineGroup.SetActive(known && !online);
    }

    // 가입일/공헌도/최근 접속은 길드원에게만 있는 값이다. 가입 신청 줄에서는 아직 길드원이 아니므로 통째로 끈다.
    private void ApplyMembership(long joinedUnix, int contribution, long lastLoginUnix) {
        bool isMember = joinedUnix > 0;

        if (joinedText != null) {
            joinedText.gameObject.SetActive(isMember);
            if (isMember) joinedText.text = KstTime.From(joinedUnix).ToString("yyyy. MM. dd");
        }
        if (contributionText != null) {
            contributionText.gameObject.SetActive(isMember);
            if (isMember) contributionText.text = contribution.ToString("N0");
        }
        if (lastLoginText != null) {
            // 접속 기록이 없는 계정도 있다(옛 데이터). 그때는 줄을 비워 두는 대신 통째로 끈다.
            bool hasLogin = isMember && lastLoginUnix > 0;
            lastLoginText.gameObject.SetActive(hasLogin);
            if (hasLogin) lastLoginText.text = GuildRules.LastLoginText(lastLoginUnix);
        }
    }

    private void ApplyProfile(string nickname, int frameId, int repCharId, int repCostumeId,
                              long combatPower, BattleHudContext hud) {
        if (nicknameText != null) nicknameText.text = nickname;
        if (combatPowerText != null) combatPowerText.text = combatPower.ToString("N0");

        // 다른 유저의 테두리는 바뀌지 않는 고정값이므로 구독형(BindReactive)이 아니라 1회 표시로 붙인다.
        if (frameView != null && hud != null) frameView.Bind(hud.ProfileFrames, frameId);
        if (portraitView != null) {
            // 상대가 끼운 코스튬까지 반영한 외형을 넘겨야 관심점/프레이밍이 그 그림에 맞게 적용된다.
            var appearance = hud != null && hud.Appearance != null
                ? hud.Appearance.Other(repCharId, repCostumeId) : default;
            if (appearance.Character != null) portraitView.Bind(appearance);
            else portraitView.Clear();
        }
    }

    private void BindProfileButton(Action<int> onProfile) {
        if (profileButton == null) return;
        profileButton.onClick.RemoveAllListeners();
        profileButton.onClick.AddListener(() => onProfile?.Invoke(_accountId));
    }

    private static void SetActive(Button button, bool on) {
        if (button != null) button.gameObject.SetActive(on);
    }

    private static void SetActive(GameObject go, bool on) {
        if (go != null) go.SetActive(on);
    }
}
