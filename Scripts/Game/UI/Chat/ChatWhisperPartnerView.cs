using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 귓속말 대화 상대 칩 한 개(귓속말 탭 상단의 목록). 누르면 그 상대와의 대화만 보이고 입력 대상도 그 사람이 된다.
///
/// 여러 명과 동시에 귓말할 때 대화가 한 줄기로 섞이면 상대를 오갈 수 없다. 칩이 곧 대화방 역할을 하며,
/// 지금 보고 있는 상대는 선택 표시로, 아직 읽지 않은 귓속말이 온 상대는 배지로 구분한다.
///
/// 칩은 대화가 남아 있는 한 사라지지 않는다. 닫기 버튼을 눌러야 그 상대와의 대화를 지우고 칩도 없어진다
/// (지우는 것은 내 쪽뿐이고 상대의 대화창에는 그대로 남는다).
/// </summary>
public sealed class ChatWhisperPartnerView : MonoBehaviour {
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nicknameText;
    [Tooltip("지금 보고 있는 상대일 때 켜는 표시. 선택.")]
    [SerializeField] private GameObject selectedIndicator;
    [Tooltip("아직 읽지 않은 귓속말이 있을 때 켜는 배지. Source=Manual 로 두고 이 뷰가 켜고 끈다. 선택.")]
    [SerializeField] private BadgeView unreadBadge;
    [Tooltip("이 대화를 지우고 칩을 없애는 버튼(X). 배선하지 않으면 닫기 기능이 없다. 선택.")]
    [SerializeField] private Button closeButton;

    /// <summary>이 칩이 가리키는 상대의 계정 id.</summary>
    public int AccountId { get; private set; }

    /// <param name="onClose">이 대화를 지운다(상대 계정 id). 배선된 닫기 버튼이 부른다.</param>
    public void Bind(WhisperPartnerDto partner, bool selected, Action<int, string> onSelect,
                     Action<int, string> onClose = null) {
        if (partner == null) return;

        AccountId = partner.AccountId;
        if (nicknameText != null) nicknameText.text = partner.Nickname;
        if (selectedIndicator != null) selectedIndicator.SetActive(selected);
        // 보고 있는 상대의 안 읽음은 그 자리에서 지워지므로 배지를 켜지 않는다.
        if (unreadBadge != null) unreadBadge.SetOn(!selected && partner.Unread > 0);

        int accountId = partner.AccountId;
        string nickname = partner.Nickname;

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelect?.Invoke(accountId, nickname));
        }

        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => onClose?.Invoke(accountId, nickname));
        }
    }
}
