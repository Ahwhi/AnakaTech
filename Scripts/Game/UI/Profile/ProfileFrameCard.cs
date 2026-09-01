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

    public int ProfileFrameId { get; private set; }

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
