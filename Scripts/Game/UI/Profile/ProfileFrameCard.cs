using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프로필 테두리 선택 그리드의 한 칸. 테두리 스프라이트 + 이름 + 미보유(잠금)/장착중 표시.
/// 테두리는 가운데가 비어 있는 액자라 칸 안에서도 그대로 그려 장착 모습을 미리 보여준다.
/// </summary>
public sealed class ProfileFrameCard : MonoBehaviour {
    [Tooltip("테두리 스프라이트.")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("미보유 시 노출할 잠금 오버레이(선택).")]
    [SerializeField] private GameObject lockedOverlay;
    [Tooltip("장착 중일 때 노출할 표시(선택).")]
    [SerializeField] private GameObject equippedIndicator;
    [SerializeField] private Button button;
    [Tooltip("선택. 기간제 테두리의 남은 기간(예: 3일 남음). 영구 테두리/미보유면 꺼진다.")]
    [SerializeField] private TMP_Text expiryText;
    [Tooltip("선택. 남은 기간 표시를 감싸는 부모(배경 등). 넣으면 텍스트 대신 이 그룹을 켜고 끈다.")]
    [SerializeField] private GameObject expiryGroup;

    public int ProfileFrameId { get; private set; }

    /// <summary>기간제 테두리의 만료 시각(UTC epoch). 0 이하면 영구(표시를 끈다).</summary>
    public void SetExpiry(long expiresUnix) {
        string remain = expiresUnix > 0 ? KstTime.Remaining(expiresUnix) : "";
        bool show = remain.Length > 0;
        var toggle = expiryGroup != null ? expiryGroup : expiryText != null ? expiryText.gameObject : null;
        if (toggle != null) toggle.SetActive(show);
        if (expiryText != null)
            expiryText.text = show ? $"{remain}\n<size=80%>~{KstTime.From(expiresUnix):MM.dd HH:mm}</size>" : "";
    }

    public void Bind(int profileFrameId, string displayName, Sprite sprite, bool owned, bool equipped, Action<ProfileFrameCard> onClick) {
        ProfileFrameId = profileFrameId;
        if (icon != null) { icon.sprite = sprite; icon.enabled = sprite != null; }
        if (nameText != null) nameText.text = displayName;
        if (lockedOverlay != null) lockedOverlay.SetActive(!owned);
        if (equippedIndicator != null) equippedIndicator.SetActive(equipped);
        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }
}
