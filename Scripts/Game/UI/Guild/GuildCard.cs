using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 길드 목록 한 줄(추천/검색 공용). 이름/마크/레벨/전투력/인원/가입 방식을 보여주고 가입 버튼을 낸다.
/// 버튼 문구와 활성 상태는 길드 상태(즉시 가입/신청형, 정원)와 내 신청 여부로 정해지며,
/// 실제 처리는 콜백을 받은 쪽(GuildPopup / GuildSearchPopup)이 한다(이 컴포넌트는 표시만 한다).
/// </summary>
public sealed class GuildCard : MonoBehaviour {
    [Header("Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image markImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text combatPowerText;
    [Tooltip("길드 인원. n / n 형태로 표시한다.")]
    [SerializeField] private TMP_Text memberCountText;
    [Tooltip("가입 방식 표시(즉시 가입 / 가입 신청).")]
    [SerializeField] private TMP_Text joinTypeText;
    [Tooltip("선택. 길드장 닉네임. 누가 이끄는 길드인지 목록에서 바로 보이게 한다.")]
    [SerializeField] private TMP_Text masterNameText;
    [Tooltip("선택. 길드 소개 한 줄.")]
    [SerializeField] private TMP_Text introText;

    [Header("Action")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    private GuildSummaryDto _data;
    private Action<GuildSummaryDto> _onAction;

    public GuildSummaryDto Data => _data;

    /// <param name="applied">내가 이 길드에 가입 신청을 넣어 둔 상태인지(신청형에서 버튼이 취소로 바뀐다).</param>
    /// <param name="onAction">가입/신청/신청취소 버튼을 눌렀을 때. 어떤 동작인지는 받는 쪽이 길드 상태로 판단한다.</param>
    public void Bind(GuildSummaryDto data, GuildMarkCatalog marks, bool applied, Action<GuildSummaryDto> onAction) {
        _data = data;
        _onAction = onAction;
        if (data == null) return;

        if (nameText != null) nameText.text = data.Name;
        if (markImage != null) {
            markImage.sprite = marks != null ? marks.Icon(data.MarkId) : null;
            markImage.enabled = markImage.sprite != null;
        }
        if (levelText != null) levelText.text = $"Lv.{data.Level}";
        if (combatPowerText != null) combatPowerText.text = data.CombatPower.ToString("N0");
        if (memberCountText != null) memberCountText.text = $"{data.MemberCount} / {GuildRules.MaxMembers}";
        if (joinTypeText != null) joinTypeText.text = GuildRules.JoinTypeName(data.JoinType);
        if (masterNameText != null) masterNameText.text = data.MasterNickname;
        if (introText != null) introText.text = data.Intro;

        RefreshAction(applied);

        if (actionButton != null) {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => _onAction?.Invoke(_data));
        }
    }

    // 문구와 활성은 길드 프로필 팝업과 같은 규칙을 쓴다(GuildRules).
    private void RefreshAction(bool applied) {
        if (actionButtonText != null) actionButtonText.text = GuildRules.JoinActionLabel(_data, applied);
        if (actionButton != null) actionButton.interactable = GuildRules.CanPressJoin(_data, applied);
    }
}
