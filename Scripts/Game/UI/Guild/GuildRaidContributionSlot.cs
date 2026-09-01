using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레이드 오늘 기여도 한 줄(순위 / 이름 / 피해량 / 참여 횟수).
/// 피해량 막대는 "보스 최대 체력 대비"라, 한 명이 보스 하나를 얼마나 녹였는지가 그대로 보인다.
/// </summary>
public sealed class GuildRaidContributionSlot : MonoBehaviour {
    [SerializeField] private TMP_Text rankText;
    [Tooltip("상위 순위 색. 위에서부터 1등, 2등, 3등... 순서다. 항목을 늘리면 그만큼 상위 순위가 더 구분된다.")]
    [SerializeField] private Color[] topRankColors = {
        new Color(1.00f, 0.84f, 0.30f, 1f),   // 1등
        new Color(0.78f, 0.82f, 0.88f, 1f),   // 2등
        new Color(0.85f, 0.60f, 0.38f, 1f),   // 3등
    };
    [Tooltip("topRankColors 범위 밖(기본 4등 이하)의 순위 색.")]
    [SerializeField] private Color otherRankColor = new Color(0.80f, 0.80f, 0.80f, 1f);
    [Tooltip("대표 캐릭터 일러 슬롯. 원본이 선언한 관심점으로 프레이밍되므로 raw Image 가 아니라 이 컴포넌트를 쓴다.")]
    [SerializeField] private CharacterIllustrationView portraitView;
    [Tooltip("프로필 테두리. 표시 규칙이 한 곳에 모이도록 공용 위젯을 쓴다.")]
    [SerializeField] private ProfileFrameView frameView;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text damageText;
    [Tooltip("참여 횟수 표시(선택).")]
    [SerializeField] private TMP_Text entriesText;
    [Tooltip("보스 최대 체력 대비 기여 비율 막대(선택).")]
    [SerializeField] private Image damageBar;

    /// <param name="hud">초상/테두리 카탈로그를 꺼낼 컨텍스트. 없으면 프로필 표시만 생략한다.</param>
    public void Bind(int rank, GuildRaidContributionDto data, long bossMaxHp, BattleHudContext hud) {
        if (data == null) return;

        if (rankText != null) {
            rankText.text = rank.ToString();
            rankText.color = RankColor(rank);
        }
        if (nicknameText != null) nicknameText.text = data.Nickname;

        // 다른 길드원의 테두리는 바뀌지 않는 고정값이므로 구독형(BindReactive)이 아니라 1회 표시로 붙인다.
        if (frameView != null && hud != null) frameView.Bind(hud.ProfileFrames, data.ProfileFrameId);
        if (portraitView != null) {
            var charData = hud != null && hud.Appearance != null
                ? hud.Appearance.Other(data.RepresentativeCharId, data.RepresentativeCostumeId)
                : default;
            // 원본(CharacterData)을 그대로 넘겨야 관심점/프레이밍 방식이 함께 적용된다.
            if (charData.Character != null) portraitView.Bind(charData);
            else portraitView.Clear();
        }
        if (damageText != null) damageText.text = data.Damage.ToString("N0");
        if (entriesText != null) entriesText.text = $"{data.Entries}회";
        if (damageBar != null)
            damageBar.fillAmount = bossMaxHp > 0 ? Mathf.Clamp01((float)data.Damage / bossMaxHp) : 0f;
    }

    // 1등부터 차례로 topRankColors 를 쓰고, 그 범위를 넘어가면 공용 색을 쓴다.
    private Color RankColor(int rank) {
        int index = rank - 1;   // rank 는 1-based
        return topRankColors != null && index >= 0 && index < topRankColors.Length
            ? topRankColors[index] : otherRankColor;
    }
}
