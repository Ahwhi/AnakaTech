using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원정대 파티원 1칸. 주인의 프로필(테두리 + 닉네임) + 매칭된 캐릭터 카드 + 전투력을 보여 준다.
///
/// 캐릭터 카드는 덱 팝업과 같은 DeckCharacterCard 컴포넌트를 그대로 쓴다(이름/레벨/등급/별/한계돌파 표시가 덱과 같아진다).
/// 서버가 준 표시값(레벨/별/한계돌파/코스튬)만으로 그리므로 남의 캐릭터도 내 캐릭터와 같은 모양으로 보인다.
/// </summary>
public sealed class ExpeditionMemberSlot : MonoBehaviour {
    [Header("Owner Profile")]
    [Tooltip("주인의 프로필 테두리. 표시 규칙이 한 곳에 모이도록 공용 위젯을 쓴다.")]
    [SerializeField] private ProfileFrameView frameView;
    [SerializeField] private TMP_Text nicknameText;
    [Tooltip("선택. 슬롯을 눌러 주인의 프로필 팝업을 연다.")]
    [SerializeField] private Button profileButton;

    [Header("Character")]
    [Tooltip("매칭된 캐릭터 카드(덱 팝업과 같은 프리팹을 자식으로 둔다).")]
    [SerializeField] private DeckCharacterCard characterCard;
    [SerializeField] private TMP_Text combatPowerText;

    [Header("State (선택)")]
    [Tooltip("나 자신일 때 켜는 그룹(테두리 강조 등).")]
    [SerializeField] private GameObject myGroup;
    [Tooltip("나 자신일 때 닉네임 색.")]
    [SerializeField] private Color myNicknameColor = new Color(0.30f, 0.85f, 0.40f, 1f);
    [Tooltip("용병(남는 자리를 채운 파티원)일 때 켜는 표시.")]
    [SerializeField] private GameObject mercenaryGroup;
    [Tooltip("오늘 이미 장소를 친 파티원에 켜는 표시.")]
    [SerializeField] private GameObject enteredTodayGroup;

    private int _accountId;
    private Color _nicknameDefaultColor = Color.white;
    private bool _nicknameColorCaptured;

    public void Bind(ExpeditionMemberDto data, BattleHudContext hud, bool isMe, Action<int> onProfile) {
        if (data == null) { gameObject.SetActive(false); return; }
        gameObject.SetActive(true);
        _accountId = data.AccountId;

        if (nicknameText != null) {
            if (!_nicknameColorCaptured) { _nicknameDefaultColor = nicknameText.color; _nicknameColorCaptured = true; }
            nicknameText.text = data.Nickname;
            nicknameText.color = isMe ? myNicknameColor : _nicknameDefaultColor;
        }
        if (frameView != null && hud != null) frameView.Bind(hud.ProfileFrames, data.ProfileFrameId);
        if (combatPowerText != null) combatPowerText.text = data.CombatPower.ToString("N0");

        if (characterCard != null && hud != null && hud.Appearance != null) {
            var appearance = hud.Appearance.Other(data.CharDataId, data.CostumeId);
            var progress = new CharacterProgress {
                CharDataId = data.CharDataId, Level = data.Level, StarLevel = data.StarLevel, LimitBroken = data.LimitBroken,
            };
            if (appearance.Character != null) characterCard.Bind(appearance, progress, null);
        }

        if (myGroup != null) myGroup.SetActive(isMe);
        if (mercenaryGroup != null) mercenaryGroup.SetActive(data.Mercenary);
        if (enteredTodayGroup != null) enteredTodayGroup.SetActive(data.EnteredToday);

        if (profileButton != null) {
            profileButton.onClick.RemoveAllListeners();
            profileButton.onClick.AddListener(() => onProfile?.Invoke(_accountId));
        }
    }
}
