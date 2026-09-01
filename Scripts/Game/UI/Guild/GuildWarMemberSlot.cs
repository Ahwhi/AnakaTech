using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대항전 명단 한 줄(양쪽 공용). 프로필 테두리 + 대표 캐릭터 일러 + 이름 + 덱 전투력을 보여 주고,
/// 상대 명단일 때만 공격 버튼을 낸다.
///
/// 쓰러진 참가자는 흐리게 표시하고 조작을 막는다(서버도 같은 규칙으로 막지만, 누를 수 없는 버튼을 보여 주지 않는 편이 낫다).
/// </summary>
public sealed class GuildWarMemberSlot : MonoBehaviour {
    [Header("Profile")]
    [Tooltip("대표 캐릭터 일러 슬롯(관심점 프레이밍 적용).")]
    [SerializeField] private CharacterIllustrationView portraitView;
    [Tooltip("선택. 쓰러졌을 때 초상 위를 어둡게 덮는 별도 그래픽. 초상 오브젝트(PortraitView) 자체를 넣으면 안 된다. " +
             "일러스트 Image 는 CharacterIllustrationView 가 내부에서 만들어 관리하므로, 색을 직접 건드리지 않고 위에 덮는다.")]
    [SerializeField] private Graphic portraitTint;
    [Tooltip("프로필 테두리. 표시 규칙이 한 곳에 모이도록 공용 위젯을 쓴다.")]
    [SerializeField] private ProfileFrameView frameView;
    [SerializeField] private TMP_Text nicknameText;
    [Tooltip("나 자신일 때 켜는 그룹(테두리 강조 등). 선택.")]
    [SerializeField] private GameObject myGroup;
    [Tooltip("나 자신일 때 닉네임 색.")]
    [SerializeField] private Color myNicknameColor = new Color(0.30f, 0.85f, 0.40f, 1f);
    [SerializeField] private TMP_Text combatPowerText;
    [Tooltip("선택. 슬롯을 눌러 그 유저의 프로필 팝업을 연다.")]
    [SerializeField] private Button profileButton;

    [Header("State")]
    [Tooltip("쓰러진 참가자에 켜지는 오버레이(회색/X 표시 등). 이 오브젝트는 꺼진 채로 시작하므로 초상/테두리를 여기 넣으면 안 된다.")]
    [SerializeField] private GameObject deadOverlay;
    [Tooltip("portraitTint 를 쓸 때 적용할 색(알파 포함).")]
    [SerializeField] private Color deadTint = new Color(0f, 0f, 0f, 0.55f);

    [Header("Attack (상대 명단 전용)")]
    [SerializeField] private Button attackButton;

    private int _accountId;
    private bool _warnedTintSetup;
    // 닉네임의 평상시 색. 프리팹에서 정한 색을 그대로 쓰려고 첫 표시 때 한 번만 기억한다.
    private Color _nicknameDefaultColor = Color.white;
    private bool _nicknameColorCaptured;

    // 흔한 배선 실수: portraitTint 에 초상 오브젝트를 그대로 넣는 것. 그러면 살아 있는 참가자의 초상이 흐려지거나
    // 사라져 원인을 찾기 어렵다. 명단은 갱신마다 다시 만들어지므로 슬롯당 한 번만 알린다.
    private void WarnIfTintIsPortrait() {
        if (_warnedTintSetup || portraitView == null) return;
        if (portraitTint.gameObject != portraitView.gameObject) return;

        _warnedTintSetup = true;
        Debug.LogWarning(
            $"[GuildWarMemberSlot] '{name}' 의 Portrait Tint 가 초상 오브젝트에 그대로 할당되어 있습니다. " +
            "쓰러짐 표시는 초상 위에 덮는 별도 그래픽이어야 합니다(초상 자식으로 Image 를 하나 만들어 할당하세요).", this);
    }

    /// <param name="enemy">상대 길드 소속인지(표시 구분용).</param>
    /// <param name="isMe">이 줄이 나 자신인지(강조 표시).</param>
    /// <param name="attackable">지금 이 대상을 때릴 수 있는지(경기 중 + 내가 살아 있음 + 대상 생존). 공격 버튼 노출 조건이다.</param>
    public void Bind(GuildWarParticipantDto data, BattleHudContext hud, bool enemy, bool isMe, bool attackable,
                     Action<int> onProfile, Action<int> onAttack) {
        if (data == null) return;
        _accountId = data.AccountId;

        if (nicknameText != null) {
            if (!_nicknameColorCaptured) { _nicknameDefaultColor = nicknameText.color; _nicknameColorCaptured = true; }
            nicknameText.text = data.Nickname;
            nicknameText.color = isMe ? myNicknameColor : _nicknameDefaultColor;
        }
        if (myGroup != null) myGroup.SetActive(isMe);
        if (combatPowerText != null) combatPowerText.text = data.CombatPower.ToString("N0");

        if (frameView != null && hud != null) frameView.Bind(hud.ProfileFrames, data.ProfileFrameId);
        if (portraitView != null) {
            var charData = hud != null && hud.Appearance != null
                ? hud.Appearance.Other(data.RepresentativeCharId, data.RepresentativeCostumeId)
                : default;
            if (charData.Character != null) portraitView.Bind(charData);
            else portraitView.Clear();
        }
        // 일러스트 Image 는 CharacterIllustrationView 가 내부에서 만들어 관리하므로 색을 직접 바꾸지 않고,
        // 위에 덮는 그래픽으로 쓰러짐을 표현한다(프레이밍 로직과 표시 상태를 섞지 않는다).
        // 오브젝트가 아니라 그래픽만 켜고 끈다. 잘못 할당하더라도 초상 전체가 사라지지는 않게 하기 위함이다.
        if (portraitTint != null) {
            WarnIfTintIsPortrait();
            portraitTint.color = deadTint;
            portraitTint.enabled = data.Dead;
        }
        if (deadOverlay != null) deadOverlay.SetActive(data.Dead);

        if (profileButton != null) {
            profileButton.onClick.RemoveAllListeners();
            profileButton.onClick.AddListener(() => onProfile?.Invoke(_accountId));
        }

        // 때릴 수 있을 때만 버튼을 낸다. 경기 시간이 아니거나 이미 쓰러진 상대에게 비활성 버튼을 남겨 두면
        // 누를 수 있는 것처럼 보이고, 명단이 갱신될 때마다 잠깐씩 스쳐 지나가 눈에 거슬린다.
        if (attackButton != null) {
            attackButton.gameObject.SetActive(attackable);
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(() => onAttack?.Invoke(_accountId));
        }
    }
}
