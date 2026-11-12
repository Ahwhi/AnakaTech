using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보상 트랙 위의 점 하나. 점수와 그 구간의 보상을 보여 주고, 지금 어떤 상태인지 알린다.
///
/// 두 가지를 따로 보여 준다. 섞으면 "받을 수 있는데 이미 받은" 구간이 어떻게 보여야 하는지가 모호해진다.
///   도달 여부 : 최고 점수가 이 구간에 닿았는가. 수령과 무관한 순수 표시(reachImage 의 색).
///   수령 여부 : 이미 받았는가(잠금/수령가능/받음 그룹).
///
/// 점을 눌러서 받는 것이 아니라 버튼으로만 받으므로 이 컴포넌트에는 버튼이 없다.
/// </summary>
public sealed class DoomRewardNode : MonoBehaviour {
    [Tooltip("이 구간을 여는 점수.")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("보상 아이콘/수량을 그리는 슬롯. 공용 RewardSlotView 를 그대로 쓴다.")]
    [SerializeField] private RewardSlotView rewardSlot;

    [Header("Reach")]
    [Tooltip("도달 여부를 색으로만 나타내는 이미지(점/막대 등). 수령 여부와는 무관한 표시다.")]
    [SerializeField] private Image reachImage;
    [Tooltip("최고 점수가 이 구간에 닿았을 때의 색.")]
    [SerializeField] private Color reachedColor = new(1f, 0.84f, 0.35f);
    [Tooltip("아직 닿지 못했을 때의 색.")]
    [SerializeField] private Color unreachedColor = new(0.35f, 0.35f, 0.40f);

    [Header("Claim State")]
    [Tooltip("아직 도달하지 못했을 때 켜는 오브젝트(잠금 표시 등). 선택.")]
    [SerializeField] private GameObject lockedGroup;
    [Tooltip("받을 수 있을 때 켜는 오브젝트(반짝임 등). 선택.")]
    [SerializeField] private GameObject claimableGroup;
    [Tooltip("이미 받았을 때 켜는 오브젝트(체크 표시 등). 선택.")]
    [SerializeField] private GameObject claimedGroup;

    public void Bind(BattleHudContext ctx, int score, RewardItem reward, int bestScore, int claimedScore) {
        if (scoreText != null) scoreText.text = score.ToString("N0");
        if (rewardSlot != null) rewardSlot.Bind(RewardVisualResolver.Resolve(ctx, reward));

        bool reached = bestScore >= score;
        bool claimed = claimedScore >= score;

        // 도달 색은 최고 점수만 본다. 받았든 안 받았든 "거기까지 갔다"는 사실은 달라지지 않는다.
        if (reachImage != null) reachImage.color = reached ? reachedColor : unreachedColor;

        if (lockedGroup != null) lockedGroup.SetActive(!reached);
        if (claimableGroup != null) claimableGroup.SetActive(reached && !claimed);
        if (claimedGroup != null) claimedGroup.SetActive(claimed);
    }
}
