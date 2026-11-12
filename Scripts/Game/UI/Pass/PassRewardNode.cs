using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 패스 보상 트랙의 칸 하나(레벨 하나). 위아래 두 줄의 보상을 보여 준다.
///   무료 줄 : 누구나 받는다.
///   패스 줄 : 평생 패스가 있어야 받는다(없으면 잠금 표시).
/// 두 줄은 같은 보상이다(평생 패스는 같은 보상을 한 번 더 받는 상품이다).
///
/// 멸망전 보상 점(DoomRewardNode)과 같은 원칙으로 도달 여부와 수령 여부를 따로 보여 준다.
/// 칸을 눌러서 받는 것이 아니라 수령 버튼 하나로 도달한 칸 전부를 받으므로 이 컴포넌트에는 버튼이 없다.
/// </summary>
public sealed class PassRewardNode : MonoBehaviour {
    [Tooltip("패스 레벨. 0 번 칸은 시작 보상이라 숫자 대신 startLabel 을 쓴다.")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("0 번 칸(패스 시작 보상)의 레벨 표시 문구.")]
    [SerializeField] private string startLabel = "시작";

    [Header("Rewards")]
    [Tooltip("무료 줄 보상 슬롯.")]
    [SerializeField] private RewardSlotView freeSlot;
    [Tooltip("패스 줄 보상 슬롯.")]
    [SerializeField] private RewardSlotView passSlot;

    [Header("Reach")]
    [Tooltip("도달 여부를 색으로만 나타내는 이미지(점/막대 등). 수령 여부와는 무관한 표시다.")]
    [SerializeField] private Image reachImage;
    [SerializeField] private Color reachedColor = new(1f, 0.84f, 0.35f);
    [SerializeField] private Color unreachedColor = new(0.35f, 0.35f, 0.40f);

    [Header("Free Row")]
    [FormerlySerializedAs("lockedGroup")]
    [Tooltip("무료 줄 잠금(아직 이 레벨에 도달하지 못함). 선택.")]
    [SerializeField] private GameObject freeLockedGroup;
    [Tooltip("무료 줄을 받을 수 있을 때 켜는 오브젝트. 선택.")]
    [SerializeField] private GameObject freeClaimableGroup;
    [Tooltip("무료 줄을 이미 받았을 때 켜는 오브젝트. 선택.")]
    [SerializeField] private GameObject freeClaimedGroup;

    [Header("Pass Row")]
    [Tooltip("패스 줄 잠금(평생 패스가 없거나 아직 이 레벨에 도달하지 못함). 선택.")]
    [SerializeField] private GameObject passLockedGroup;
    [Tooltip("패스 줄을 받을 수 있을 때 켜는 오브젝트. 선택.")]
    [SerializeField] private GameObject passClaimableGroup;
    [Tooltip("패스 줄을 이미 받았을 때 켜는 오브젝트. 선택.")]
    [SerializeField] private GameObject passClaimedGroup;

    public void Bind(BattleHudContext ctx, int level, PassInfoDto info) {
        if (levelText != null) levelText.text = level <= 0 ? startLabel : level.ToString("N0");

        var visual = RewardVisualResolver.Resolve(ctx, PassRules.RewardOf(level));
        if (freeSlot != null) freeSlot.Bind(visual);
        if (passSlot != null) passSlot.Bind(visual);

        int current = info != null ? info.Level : -1;
        bool hasPass = info != null && info.HasPass;
        bool reached = level <= current;
        bool freeClaimed = info != null && level < info.FreeClaimedCount;
        bool passClaimed = hasPass && level < info.PassClaimedCount;

        if (reachImage != null) reachImage.color = reached ? reachedColor : unreachedColor;
        // 각 줄은 잠금 / 수령 가능 / 수령 완료 중 하나만 켜진다.
        if (freeLockedGroup != null) freeLockedGroup.SetActive(!reached);
        if (freeClaimableGroup != null) freeClaimableGroup.SetActive(reached && !freeClaimed);
        if (freeClaimedGroup != null) freeClaimedGroup.SetActive(freeClaimed);

        if (passLockedGroup != null) passLockedGroup.SetActive(!hasPass || !reached);
        if (passClaimableGroup != null) passClaimableGroup.SetActive(hasPass && reached && !passClaimed);
        if (passClaimedGroup != null) passClaimedGroup.SetActive(passClaimed);
    }
}
